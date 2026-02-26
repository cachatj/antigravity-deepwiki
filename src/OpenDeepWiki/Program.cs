using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenDeepWiki.Agents;
using OpenDeepWiki.Cache.DependencyInjection;
using OpenDeepWiki.Chat;
using OpenDeepWiki.MCP;
using OpenDeepWiki.Endpoints;
using OpenDeepWiki.Endpoints.Admin;
using OpenDeepWiki.Infrastructure;
using OpenDeepWiki.Services.Admin;
using OpenDeepWiki.Services.Auth;
using OpenDeepWiki.Services.Chat;
using OpenDeepWiki.Services.MindMap;
using OpenDeepWiki.Services.Notifications;
using OpenDeepWiki.Services.OAuth;
using OpenDeepWiki.Services.Organizations;
using OpenDeepWiki.Services.Prompts;
using OpenDeepWiki.Services.Recommendation;
using OpenDeepWiki.Services.Repositories;
using OpenDeepWiki.Services.Translation;
using OpenDeepWiki.Services.Mcp;
using OpenDeepWiki.Services.UserProfile;
using OpenDeepWiki.Services.Wiki;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap logger for startup error capture
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // ASCII Art Banner
    var banner = """
    
     ██████╗ ██████╗ ███████╗███╗   ██╗██████╗ ███████╗███████╗██████╗ ██╗    ██╗██╗██╗  ██╗██╗
    ██╔═══██╗██╔══██╗██╔════╝████╗  ██║██╔══██╗██╔════╝██╔════╝██╔══██╗██║    ██║██║██║ ██╔╝██║
    ██║   ██║██████╔╝█████╗  ██╔██╗ ██║██║  ██║█████╗  █████╗  ██████╔╝██║ █╗ ██║██║█████╔╝ ██║
    ██║   ██║██╔═══╝ ██╔══╝  ██║╚██╗██║██║  ██║██╔══╝  ██╔══╝  ██╔═══╝ ██║███╗██║██║██╔═██╗ ██║
    ╚██████╔╝██║     ███████╗██║ ╚████║██████╔╝███████╗███████╗██║     ╚███╔███╔╝██║██║  ██╗██║
     ╚═════╝ ╚═╝     ╚══════╝╚═╝  ╚═══╝╚═════╝ ╚══════╝╚══════╝╚═╝      ╚══╝╚══╝ ╚═╝╚═╝  ╚═╝╚═╝
                                                                                    
                             ██████╗  ██████╗ ██╗
                            ██╔════╝ ██╔═══██╗██║
                            ██║  ███╗██║   ██║██║
                            ██║   ██║██║   ██║╚═╝
                            ╚██████╔╝╚██████╔╝██╗
                             ╚═════╝  ╚═════╝ ╚═╝
    
    """;
    Console.WriteLine(banner);
    
    Log.Information("Starting OpenDeepWiki application");

    var builder = WebApplication.CreateBuilder(args);

    // Load .env file into Configuration
    LoadEnvFile(builder.Configuration);

    // Add Serilog logging
    builder.AddSerilogLogging();

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddMiniApis();

    // Add database services based on configuration
    builder.Services.AddDatabase(builder.Configuration);

    // Configure JWT
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection("Jwt"))
        .PostConfigure(options =>
        {
            if (string.IsNullOrWhiteSpace(options.SecretKey))
            {
                options.SecretKey = builder.Configuration["JWT_SECRET_KEY"]
                    ?? throw new InvalidOperationException("JWT secret key not configured");
            }
        });

    // Add JWT authentication
    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
    var secretKey = jwtOptions.SecretKey;
    if (string.IsNullOrWhiteSpace(secretKey))
    {
        secretKey = builder.Configuration["JWT_SECRET_KEY"]
            ?? "OpenDeepWiki-Default-Secret-Key-Please-Change-In-Production-Environment-2024";
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });

    // Register auth services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IOAuthService, OAuthService>();
    builder.Services.AddScoped<IUserContext, UserContext>();

    // Add HttpClient
    builder.Services.AddHttpClient();

    // Register Git platform services
    builder.Services.AddScoped<IGitPlatformService, GitPlatformService>();

    builder.Services.AddOptions<AiRequestOptions>()
        .Bind(builder.Configuration.GetSection("AI"))
        .PostConfigure(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = builder.Configuration["CHAT_API_KEY"];
            }

            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                options.Endpoint = builder.Configuration["ENDPOINT"];
            }

            if (!options.RequestType.HasValue)
            {
                var requestType = builder.Configuration["CHAT_REQUEST_TYPE"];
                if (Enum.TryParse<AiRequestType>(requestType, true, out var parsed))
                {
                    options.RequestType = parsed;
                }
            }
        });

    builder.Services
        .AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policyBuilder => policyBuilder
                    .SetIsOriginAllowed(_ => true)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials());
        });
    builder.Services.AddSingleton<AgentFactory>();

    // Configure Repository Analyzer
    builder.Services.AddOptions<RepositoryAnalyzerOptions>()
        .Bind(builder.Configuration.GetSection("RepositoryAnalyzer"))
        .PostConfigure(options =>
        {
            var repoDir = builder.Configuration["REPOSITORIES_DIRECTORY"];
            if (!string.IsNullOrWhiteSpace(repoDir))
            {
                options.RepositoriesDirectory = repoDir;
            }
        });
    builder.Services.AddScoped<IRepositoryAnalyzer, RepositoryAnalyzer>();

    // Configure Wiki Generator
    builder.Services.AddOptions<WikiGeneratorOptions>()
        .Bind(builder.Configuration.GetSection(WikiGeneratorOptions.SectionName))
        .PostConfigure(options =>
        {
            // Catalog config
            var catalogModel = builder.Configuration["WIKI_CATALOG_MODEL"];
            if (!string.IsNullOrWhiteSpace(catalogModel))
            {
                options.CatalogModel = catalogModel;
            }

            var catalogEndpoint = builder.Configuration["WIKI_CATALOG_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(catalogEndpoint))
            {
                options.CatalogEndpoint = catalogEndpoint;
            }

            var catalogApiKey = builder.Configuration["WIKI_CATALOG_API_KEY"];
            if (!string.IsNullOrWhiteSpace(catalogApiKey))
            {
                options.CatalogApiKey = catalogApiKey;
            }

            var catalogRequestType = builder.Configuration["WIKI_CATALOG_REQUEST_TYPE"];
            if (Enum.TryParse<AiRequestType>(catalogRequestType, true, out var catalogParsed))
            {
                options.CatalogRequestType = catalogParsed;
            }

            // Content config
            var contentModel = builder.Configuration["WIKI_CONTENT_MODEL"];
            if (!string.IsNullOrWhiteSpace(contentModel))
            {
                options.ContentModel = contentModel;
            }

            var contentEndpoint = builder.Configuration["WIKI_CONTENT_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(contentEndpoint))
            {
                options.ContentEndpoint = contentEndpoint;
            }

            var contentApiKey = builder.Configuration["WIKI_CONTENT_API_KEY"];
            if (!string.IsNullOrWhiteSpace(contentApiKey))
            {
                options.ContentApiKey = contentApiKey;
            }

            var contentRequestType = builder.Configuration["WIKI_CONTENT_REQUEST_TYPE"];
            if (Enum.TryParse<AiRequestType>(contentRequestType, true, out var contentParsed))
            {
                options.ContentRequestType = contentParsed;
            }

            // Translation config (optional; falls back to Content config if not set)
            var translationModel = builder.Configuration["WIKI_TRANSLATION_MODEL"];
            if (!string.IsNullOrWhiteSpace(translationModel))
            {
                options.TranslationModel = translationModel;
            }

            var translationEndpoint = builder.Configuration["WIKI_TRANSLATION_ENDPOINT"];
            if (!string.IsNullOrWhiteSpace(translationEndpoint))
            {
                options.TranslationEndpoint = translationEndpoint;
            }

            var translationApiKey = builder.Configuration["WIKI_TRANSLATION_API_KEY"];
            if (!string.IsNullOrWhiteSpace(translationApiKey))
            {
                options.TranslationApiKey = translationApiKey;
            }

            var translationRequestType = builder.Configuration["WIKI_TRANSLATION_REQUEST_TYPE"];
            if (Enum.TryParse<AiRequestType>(translationRequestType, true, out var translationParsed))
            {
                options.TranslationRequestType = translationParsed;
            }

            // Multi-language config
            var languages = builder.Configuration["WIKI_LANGUAGES"];
            if (!string.IsNullOrWhiteSpace(languages))
            {
                options.Languages = languages;
            }
        });

    // Register Prompt Plugin
    builder.Services.AddSingleton<IPromptPlugin>(sp =>
    {
        var options = sp.GetRequiredService<IOptions<WikiGeneratorOptions>>().Value;
        var promptsDir = Path.Combine(AppContext.BaseDirectory, options.PromptsDirectory);

        // Fallback to current directory if base directory doesn't have prompts
        if (!Directory.Exists(promptsDir))
        {
            promptsDir = Path.Combine(Directory.GetCurrentDirectory(), options.PromptsDirectory);
        }

        return new FilePromptPlugin(promptsDir);
    });

    // Register Wiki Generator
    builder.Services.AddScoped<IWikiGenerator, WikiGenerator>();

    // Register cache framework (default in-memory implementation)
    builder.Services.AddOpenDeepWikiCache();

    // Register processing log service (Singleton because it uses IServiceScopeFactory internally)
    builder.Services.AddSingleton<IProcessingLogService, ProcessingLogService>();

    // Register admin services
    builder.Services.AddScoped<IAdminStatisticsService, AdminStatisticsService>();
    builder.Services.AddScoped<IAdminRepositoryService, AdminRepositoryService>();
    builder.Services.AddScoped<IAdminUserService, AdminUserService>();
    builder.Services.AddScoped<IAdminRoleService, AdminRoleService>();
    builder.Services.AddScoped<IAdminDepartmentService, AdminDepartmentService>();
    builder.Services.AddScoped<IAdminToolsService, AdminToolsService>();
    builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();
    builder.Services.AddScoped<IAdminChatAssistantService, AdminChatAssistantService>();
    builder.Services.AddScoped<IOrganizationService, OrganizationService>();

    // Register dynamic config manager
    builder.Services.AddScoped<IDynamicConfigManager, DynamicConfigManager>();

    // Register recommendation service
    builder.Services.AddScoped<RecommendationService>();

    // Register user profile service
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();

    // Register translation service
    builder.Services.AddScoped<ITranslationService, TranslationService>();
    builder.Services.AddScoped<WikiExportService>();

    builder.Services.AddHostedService<RepositoryProcessingWorker>();
    builder.Services.AddHostedService<TranslationWorker>();
    builder.Services.AddHostedService<MindMapWorker>();

    // Configure incremental update options
    // Requirements: 6.2, 6.3, 6.6 - Configurable update interval
    builder.Services.AddOptions<IncrementalUpdateOptions>()
        .Bind(builder.Configuration.GetSection(IncrementalUpdateOptions.SectionName));

    // Register incremental update service
    // Requirements: 2.1 - Incremental update service interface
    builder.Services.AddScoped<IIncrementalUpdateService, IncrementalUpdateService>();

    // Register subscriber notification service (null implementation)
    // Requirements: 4.1 - Subscriber notification service interface
    builder.Services.AddScoped<ISubscriberNotificationService, NullSubscriberNotificationService>();

    // Register incremental update background worker
    // Requirements: 1.1 - Standalone incremental update background worker
    builder.Services.AddHostedService<IncrementalUpdateWorker>();

    // Register Chat system services
    // Requirements: 2.2, 2.4 - Auto-discover and load providers via DI
    builder.Services.AddChatServices(builder.Configuration);

    // Register chat assistant services
    // Requirements: 2.4, 3.1, 9.1 - Chat assistant API services
    builder.Services.AddScoped<IMcpToolConverter, McpToolConverter>();
    builder.Services.AddScoped<ISkillToolConverter, SkillToolConverter>();
    builder.Services.AddScoped<IChatAssistantService, ChatAssistantService>();
    builder.Services.AddScoped<IChatShareService, ChatShareService>();

    // Register user app management services
    // Requirements: 12.2, 12.6, 12.7 - User app CRUD and key management
    builder.Services.AddScoped<IChatAppService, ChatAppService>();

    // Register app statistics service
    // Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.7 - Statistics recording and querying
    builder.Services.AddScoped<IAppStatisticsService, AppStatisticsService>();

    // Register chat log service
    // Requirements: 16.1, 16.2, 16.3, 16.4, 16.5 - Chat log recording and querying
    builder.Services.AddScoped<IChatLogService, ChatLogService>();

    // Register embed service
    // Requirements: 13.5, 13.6, 14.2, 14.7, 17.1, 17.2, 17.4 - Embed script validation and chat
    builder.Services.AddScoped<IEmbedService, EmbedService>();

    // Register MCP provider management service
    builder.Services.AddScoped<IAdminMcpProviderService, AdminMcpProviderService>();
    builder.Services.AddScoped<IMcpUsageLogService, McpUsageLogService>();
    builder.Services.AddHostedService<OpenDeepWiki.MCP.McpStatisticsAggregationService>();

    // MCP server registration (official MCP server + scope via ConfigureSessionOptions)
    var mcpEnabled = builder.Configuration.GetValue("MCP_ENABLED", true);
    if (mcpEnabled)
    {
        builder.Services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.ConfigureSessionOptions = (httpContext, mcpServer, _) =>
                {
                    var segments = httpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                   ?? Array.Empty<string>();
                    string? owner = null;
                    string? repo = null;
                    if (segments.Length >= 4
                        && string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(segments[1], "mcp", StringComparison.OrdinalIgnoreCase))
                    {
                        owner = Uri.UnescapeDataString(segments[2]);
                        repo = Uri.UnescapeDataString(segments[3]);
                    }

                    McpRepositoryScopeAccessor.SetScope(mcpServer, owner, repo);
                    return Task.CompletedTask;
                };
            })
            .WithTools<McpRepositoryTools>();
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    app.UseCors("AllowAll");

    // Add static file support (for Skill assets, etc.)
    app.UseStaticFiles();

    // Configure forwarded headers for reverse proxy scenarios
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                           | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                           | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference("/v1/scalar");
    }

    app.UseAuthentication();
    app.UseAuthorization();

    // MCP server endpoints (official MCP server + scope via ConfigureSessionOptions)
    if (mcpEnabled)
    {
        app.UseMcpUsageLogging("/api/mcp");
        app.UseSseKeepAlive("/api/mcp");
        app.MapMcp("/api/mcp");
        app.MapMcp("/api/mcp/{owner}/{repo}");
    }

    app.MapMiniApis();
    app.MapAuthEndpoints();
    app.MapOAuthEndpoints();
    app.MapAdminEndpoints();
    app.MapOrganizationEndpoints();
    app.MapChatAssistantEndpoints();
    app.MapChatAppEndpoints();
    app.MapEmbedEndpoints();

    app.MapSystemEndpoints();
    app.MapIncrementalUpdateEndpoints();
    app.MapMcpProviderEndpoints();
    app.MapWikiExportEndpoints();

    // Initialize database (create default data)
    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.InitializeAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Load .env file into Configuration
/// Supports loading .env from current directory or application directory
/// </summary>
static void LoadEnvFile(IConfigurationBuilder configuration)
{
    // Try multiple paths to find .env file
    var envPaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".env"),
    };

    foreach (var envPath in envPaths)
    {
        if (!File.Exists(envPath)) continue;
        
        Log.Information("Loading .env file from: {EnvPath}", envPath);
        var envVars = new Dictionary<string, string?>();
        
        foreach (var line in File.ReadAllLines(envPath))
        {
            var trimmedLine = line.Trim();
            
            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                continue;

            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex <= 0) continue;
            
            var key = trimmedLine[..separatorIndex].Trim();
            var value = trimmedLine[(separatorIndex + 1)..].Trim();
            
            // Remove quotes
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }
            
            envVars[key] = value;
            
            // Also set as environment variable for use elsewhere
            Environment.SetEnvironmentVariable(key, value);
        }
        
        // Add to Configuration
        configuration.AddInMemoryCollection(envVars);
        Log.Information("Loaded {Count} environment variables from .env file", envVars.Count);
        return;
    }
    
    Log.Information("No .env file found, using system environment variables only");
}