# Remove Microsoft.Agents.AI — Replace with Native Google GenAI + IChatClient

## Problem Statement

The `Microsoft.Agents.AI` package (`ChatClientAgent`, `AgentSession`, `AsAIAgent()`) wraps `IChatClient` with its own `FunctionInvokingChatClient` that references `FunctionApprovalRequestContent` — a type that **does not exist** in the `Microsoft.Extensions.AI.Abstractions` v10.4.0 shipped with .NET 10. This causes a **fatal `TypeLoadException`** at runtime whenever any tool-calling agent runs.

Google's ADK is Python-only. The C# equivalent is using `Google.GenAI` (v1.5.0) with the standard `IChatClient` interface from `Microsoft.Extensions.AI`. The `Google.GenAI` SDK already implements `IChatClient` natively.

## User Review Required

> [!IMPORTANT]
> This removes **all** `Microsoft.Agents.AI` packages and their abstraction layer. The OpenAI, Anthropic, and Gemini providers will all use `IChatClient` directly with manual function-calling loops instead of the `ChatClientAgent` wrapper.

> [!WARNING]  
> The `Anthropic` provider currently uses `Microsoft.Agents.AI.Anthropic` for its `AsAIAgent()` extension. After this change, Anthropic support will need a separate `IChatClient` adapter (similar to [GeminiChatClient](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Agents/GeminiChatClient.cs#23-29)), or must be accessed via OpenAI-compatible endpoint. **For now, only Gemini is the target — Anthropic/OpenAI paths will be preserved but use `IChatClient` directly.**

---

## Proposed Changes

### Core Abstraction Layer

We replace `ChatClientAgent` + `AgentSession` + `RunStreamingAsync()` with a thin `AgentRunner` class that wraps any `IChatClient` and implements the tool-calling loop directly.

#### [NEW] [AgentRunner.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Agents/AgentRunner.cs)

A lightweight agent runner that replaces `ChatClientAgent`. It:
- Takes an `IChatClient` + options (system prompt, tools, max tokens)
- Implements `RunStreamingAsync()` — streams responses, detects `FunctionCallContent`, invokes tools, appends `FunctionResultContent`, and re-calls the model in a loop
- No Microsoft.Agents dependency — pure `Microsoft.Extensions.AI` types

---

### AgentFactory

#### [MODIFY] [AgentFactory.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Agents/AgentFactory.cs)

- Remove `using Microsoft.Agents.AI`
- Remove all `AsAIAgent()` calls
- Change return type from `ChatClientAgent` → `IChatClient`
- [CreateChatClientWithTools()](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Agents/AgentFactory.cs#190-215) returns [(IChatClient, IList<AITool>)](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/web/app/api/%5B...path%5D/route.ts#81-85) instead of [(ChatClientAgent, IList<AITool>)](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/web/app/api/%5B...path%5D/route.ts#81-85)
- [CreateSimpleChatClient()](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Agents/AgentFactory.cs#216-239) returns `IChatClient`
- Gemini path: `new GeminiChatClient(geminiClient, model)`
- OpenAI path: `openAiClient.GetChatClient(model).AsIChatClient()`
- Anthropic path: preserved via OpenAI-compat or separate adapter

---

### WikiGenerator (primary pipeline — catalog & content generation)

#### [MODIFY] [WikiGenerator.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Services/Wiki/WikiGenerator.cs)

- Remove `using Microsoft.Agents.AI`
- In [ExecuteAgentWithRetryAsync()](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Services/Wiki/WikiGenerator.cs#796-1035): replace `agent.CreateSessionAsync()` + `agent.RunStreamingAsync()` with `AgentRunner.RunStreamingAsync(chatClient, messages, options)`
- Token usage tracking stays the same (via `UsageContent`)

---

### ChatAssistantService (interactive chat)

#### [MODIFY] [ChatAssistantService.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Services/Chat/ChatAssistantService.cs)

- Remove `using Microsoft.Agents.AI`
- Replace `ChatClientAgentOptions` with `AgentRunnerOptions` (our new type)
- Replace `agent.CreateSessionAsync()` + `agent.RunStreamingAsync()` with `AgentRunner.RunStreamingAsync()`

---

### AgentExecutor (messaging providers)

#### [MODIFY] [AgentExecutor.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Chat/Execution/AgentExecutor.cs)

- Same pattern: replace `ChatClientAgent` usage with `AgentRunner`

---

### EmbedService

#### [MODIFY] [EmbedService.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/Services/Chat/EmbedService.cs)

- Same pattern: replace `ChatClientAgent` usage with `AgentRunner`

---

### McpRepositoryTools

#### [MODIFY] [McpRepositoryTools.cs](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/MCP/McpRepositoryTools.cs)

- Remove `using Microsoft.Agents.AI`
- Replace agent creation + streaming with `AgentRunner`

---

### Package References

#### [MODIFY] [OpenDeepWiki.csproj](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/OpenDeepWiki.csproj)

Remove these packages:
```diff
-<PackageReference Include="Microsoft.Agents.AI" />
-<PackageReference Include="Microsoft.Agents.AI.Anthropic" />
-<PackageReference Include="Microsoft.Agents.AI.AzureAI" />
-<PackageReference Include="Microsoft.Agents.AI.OpenAI" />
```

Keep: `Google.GenAI`, `Microsoft.Extensions.AI`, `OpenAI`

#### [MODIFY] [Directory.Packages.props](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/Directory.Packages.props)

Remove `Microsoft.Agents.AI.*` version entries.

---

## Verification Plan

### Automated Tests
```bash
# 1. Build in Docker to confirm zero compilation errors
docker run --rm -v $(pwd):/app -w /app/src/OpenDeepWiki mcr.microsoft.com/dotnet/sdk:10.0 dotnet build

# 2. Full Docker integration test
docker compose down && docker compose up --build
```

### Manual Verification
- Process a local repository through the pipeline and confirm catalog generation succeeds
- Verify no `TypeLoadException` or `FunctionApprovalRequestContent` errors in logs
