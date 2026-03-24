Searched for files: *

The prompts are in `src/OpenDeepWiki/prompts/`. There are **4 prompt files**, one for each pipeline phase:

| Prompt File | Used By | Purpose |
|---|---|---|
| [catalog-generator.md](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/prompts/catalog-generator.md) | `GenerateCatalogAsync()` | Analyzes repo structure and generates the wiki's table of contents / catalog hierarchy |
| [content-generator.md](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/prompts/content-generator.md) | `GenerateDocumentsAsync()` | Generates the detailed Markdown documentation for each catalog item |
| [mindmap-generator.md](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/prompts/mindmap-generator.md) | `GenerateMindMapAsync()` | Creates the architecture mind map visualization |
| [incremental-updater.md](file:///Users/jcachat/code_sandbox/antigravity-deepwiki/src/OpenDeepWiki/prompts/incremental-updater.md) | `IncrementalUpdateAsync()` | Updates only the docs affected by changed files |

These are loaded by the `PromptPlugin` service and passed as the system prompt to the Gemini model during each phase. Want me to open any of them for you to review or customize?