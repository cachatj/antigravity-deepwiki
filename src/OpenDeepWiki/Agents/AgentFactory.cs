using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;

#pragma warning disable OPENAI001

namespace OpenDeepWiki.Agents
{
    public enum AiRequestType
    {
        OpenAI,
        AzureOpenAI,
        OpenAIResponses,
        Anthropic,
        Gemini
    }

    public class AiRequestOptions
    {
        public string? Endpoint { get; set; }
        public string? ApiKey { get; set; }
        public AiRequestType? RequestType { get; set; }
    }

    /// <summary>
    /// Options for creating an AI agent.
    /// </summary>
    public class AgentCreationOptions
    {
        /// <summary>
        /// The system instructions for the agent.
        /// </summary>
        public string? Instructions { get; set; }

        /// <summary>
        /// The tools available to the agent.
        /// </summary>
        public IEnumerable<AIFunction>? Tools { get; set; }

        /// <summary>
        /// The name of the agent.
        /// </summary>
        public string? Name { get; set; }
    }

    public class AgentFactory(IOptions<AiRequestOptions> options)
    {
        private const string DefaultEndpoint = "https://api.routin.ai/v1";
        private readonly AiRequestOptions? _options = options?.Value;

        /// <summary>
        /// Creates an HttpClient with logging/interception capabilities.
        /// </summary>
        private static HttpClient CreateHttpClient()
        {
            var handler = new LoggingHttpHandler();
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(300)
            };
        }

        public static IChatClient CreateChatClientInternal(
            string model,
            AiRequestOptions options)
        {
            var option = ResolveOptions(options, true);
            var httpClient = CreateHttpClient();

            if (option.RequestType == AiRequestType.Gemini)
            {
                var modelToUse = string.IsNullOrEmpty(model) ? "gemini-2.5-flash" : model;
                var geminiClient = new Google.GenAI.Client(apiKey: option.ApiKey ?? string.Empty);
                return new GeminiChatClient(geminiClient, modelToUse);
            }

            if (option.RequestType == AiRequestType.OpenAI || 
                option.RequestType == AiRequestType.Anthropic ||
                option.RequestType == AiRequestType.AzureOpenAI)
            {
                // All OpenAI-compatible providers (including Anthropic via compat endpoint)
                var clientOptions = new OpenAIClientOptions()
                {
                    Endpoint = new Uri(option.Endpoint ?? DefaultEndpoint),
                    Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient)
                };

                var openAiClient = new OpenAIClient(
                    new ApiKeyCredential(option.ApiKey ?? string.Empty),
                    clientOptions);

                return openAiClient.GetChatClient(model).AsIChatClient();
            }

            // Fallback: use OpenAI-compatible endpoint
            var fallbackOptions = new OpenAIClientOptions()
            {
                Endpoint = new Uri(option.Endpoint ?? DefaultEndpoint),
                Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient)
            };

            var fallbackClient = new OpenAIClient(
                new ApiKeyCredential(option.ApiKey ?? string.Empty),
                fallbackOptions);

            return fallbackClient.GetChatClient(model).AsIChatClient();
        }

        private static AiRequestOptions ResolveOptions(
            AiRequestOptions? options,
            bool allowEnvironmentFallback)
        {
            var resolved = new AiRequestOptions
            {
                ApiKey = options?.ApiKey,
                Endpoint = options?.Endpoint,
                RequestType = options?.RequestType
            };

            if (allowEnvironmentFallback)
            {
                if (string.IsNullOrWhiteSpace(resolved.ApiKey))
                {
                    resolved.ApiKey = Environment.GetEnvironmentVariable("CHAT_API_KEY");
                }

                if (string.IsNullOrWhiteSpace(resolved.Endpoint))
                {
                    resolved.Endpoint = Environment.GetEnvironmentVariable("ENDPOINT");
                }

                if (!resolved.RequestType.HasValue)
                {
                    resolved.RequestType = TryParseRequestType(Environment.GetEnvironmentVariable("CHAT_REQUEST_TYPE"));
                }
            }

            if (string.IsNullOrWhiteSpace(resolved.Endpoint))
            {
                resolved.Endpoint = DefaultEndpoint;
            }

            if (!resolved.RequestType.HasValue)
            {
                resolved.RequestType = AiRequestType.OpenAI;
            }

            return resolved;
        }

        private static AiRequestType? TryParseRequestType(string? requestType)
        {
            if (string.IsNullOrWhiteSpace(requestType))
            {
                return null;
            }

            return Enum.TryParse<AiRequestType>(requestType, true, out var parsed)
                ? parsed
                : null;
        }

        /// <summary>
        /// Creates an IChatClient with the specified tools.
        /// </summary>
        /// <param name="model">The model name to use.</param>
        /// <param name="tools">The AI tools to make available to the agent.</param>
        /// <param name="runOptions">Options for the agent run.</param>
        /// <param name="requestOptions">Optional request options override.</param>
        /// <returns>A tuple containing the IChatClient and the tools list.</returns>
        public (IChatClient Client, IList<AITool> Tools) CreateChatClientWithTools(
            string model,
            AITool[] tools,
            AgentRunOptions runOptions,
            AiRequestOptions? requestOptions = null)
        {
            var option = ResolveOptions(requestOptions ?? _options, true);

            // Ensure tools are set in chat options
            runOptions.ChatOptions ??= new ChatOptions();
            runOptions.ChatOptions.Tools = tools;
            runOptions.ChatOptions.ToolMode = ChatToolMode.Auto;
            var client = CreateChatClientInternal(model, option);

            return (client, tools);
        }

        /// <summary>
        /// Creates a simple IChatClient without tools for translation tasks.
        /// </summary>
        /// <param name="model">The model name to use.</param>
        /// <param name="maxToken"></param>
        /// <param name="requestOptions">Optional request options override.</param>
        /// <returns>The IChatClient.</returns>
        public IChatClient CreateSimpleChatClient(
            string model,
            int maxToken = 32000,
            AiRequestOptions? requestOptions = null)
        {
            var option = ResolveOptions(requestOptions ?? _options, true);
            return CreateChatClientInternal(model, option);
        }
    }
}