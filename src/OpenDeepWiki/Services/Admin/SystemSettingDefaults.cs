using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenDeepWiki.Agents;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Wiki;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// 系统设置帮助类，用于从环境变量初始化默认设置
/// </summary>
public static class SystemSettingDefaults
{
    /// <summary>
    /// Wiki生成器相关的默认设置
    /// </summary>
    public static readonly (string Key, string Category, string Description)[] WikiGeneratorDefaults = 
    [
        ("WIKI_CATALOG_MODEL", "ai", "目录生成使用的AI模型"),
        ("WIKI_CATALOG_ENDPOINT", "ai", "目录生成API端点"),
        ("WIKI_CATALOG_API_KEY", "ai", "目录生成API密钥"),
        ("WIKI_CATALOG_REQUEST_TYPE", "ai", "目录生成请求类型"),
        ("WIKI_CONTENT_MODEL", "ai", "内容生成使用的AI模型"),
        ("WIKI_CONTENT_ENDPOINT", "ai", "内容生成API端点"),
        ("WIKI_CONTENT_API_KEY", "ai", "内容生成API密钥"),
        ("WIKI_CONTENT_REQUEST_TYPE", "ai", "内容生成请求类型"),
        ("WIKI_TRANSLATION_MODEL", "ai", "翻译使用的AI模型"),
        ("WIKI_TRANSLATION_ENDPOINT", "ai", "翻译API端点"),
        ("WIKI_TRANSLATION_API_KEY", "ai", "翻译API密钥"),
        ("WIKI_TRANSLATION_REQUEST_TYPE", "ai", "翻译请求类型"),
        ("WIKI_LANGUAGES", "ai", "支持的语言列表（逗号分隔）"),
        ("WIKI_PARALLEL_COUNT", "ai", "并行生成文档数量"),
        ("WIKI_MAX_OUTPUT_TOKENS", "ai", "最大输出Token数量"),
        ("WIKI_DOCUMENT_GENERATION_TIMEOUT_MINUTES", "ai", "文档生成超时时间（分钟）"),
        ("WIKI_TRANSLATION_TIMEOUT_MINUTES", "ai", "翻译超时时间（分钟）"),
        ("WIKI_TITLE_TRANSLATION_TIMEOUT_MINUTES", "ai", "标题翻译超时时间（分钟）"),
        ("WIKI_README_MAX_LENGTH", "ai", "README内容最大长度"),
        ("WIKI_DIRECTORY_TREE_MAX_DEPTH", "ai", "目录树最大深度"),
        ("WIKI_MAX_RETRY_ATTEMPTS", "ai", "最大重试次数"),
        ("WIKI_RETRY_DELAY_MS", "ai", "重试延迟时间（毫秒）"),
        ("WIKI_PROMPTS_DIRECTORY", "ai", "提示模板目录")
    ];

    /// <summary>
    /// 初始化系统设置默认值（仅在数据库中不存在对应设置时生效）
    /// </summary>
    public static async Task InitializeDefaultsAsync(IConfiguration configuration, IContext context)
    {
        var existingKeys = await context.SystemSettings
            .Where(s => !s.IsDeleted)
            .Select(s => s.Key)
            .ToListAsync();

        var settingsToAdd = new List<SystemSetting>();

        // 准备 WikiGeneratorOptions 的默认值，方便在没有环境变量时也能写入
        var wikiOptionDefaults = new WikiGeneratorOptions();
        var wikiSection = configuration.GetSection(WikiGeneratorOptions.SectionName);
        wikiSection.Bind(wikiOptionDefaults);

        // 将 PostConfigure 内的环境变量覆盖也应用到默认选项，确保与运行时一致
        ApplyEnvironmentOverrides(wikiOptionDefaults, configuration);

        // 处理Wiki生成器相关设置
        foreach (var (key, category, description) in WikiGeneratorDefaults)
        {
            if (!existingKeys.Contains(key))
            {
                var envValue = GetEnvironmentOrConfigurationValue(configuration, key);
                var fallbackValue = GetOptionDefaultValue(wikiOptionDefaults, key);
                var valueToUse = envValue ?? fallbackValue ?? string.Empty;

                settingsToAdd.Add(new SystemSetting
                {
                    Id = Guid.NewGuid().ToString(),
                    Key = key,
                    Value = valueToUse,
                    Description = description,
                    Category = category,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (settingsToAdd.Count > 0)
        {
            context.SystemSettings.AddRange(settingsToAdd);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// 从环境变量或配置中获取值
    /// </summary>
    private static string? GetEnvironmentOrConfigurationValue(IConfiguration configuration, string key)
    {
        // 优先从环境变量获取
        var envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(envValue))
        {
            return envValue;
        }

        // 从配置中获取（支持appsettings.json等）
        return configuration[key];
    }

    /// <summary>
    /// 将环境变量中的值应用到 WikiGeneratorOptions，以保持与运行时一致
    /// </summary>
    private static void ApplyEnvironmentOverrides(WikiGeneratorOptions options, IConfiguration configuration)
    {
        foreach (var (key, _, _) in WikiGeneratorDefaults)
        {
            var envValue = GetEnvironmentOrConfigurationValue(configuration, key);
            if (!string.IsNullOrWhiteSpace(envValue))
            {
                ApplySettingToOption(options, key, envValue);
            }
        }
    }

    /// <summary>
    /// 将系统设置应用到WikiGeneratorOptions
    /// </summary>
    public static async Task ApplyToWikiGeneratorOptions(WikiGeneratorOptions options, IAdminSettingsService settingsService)
    {
        foreach (var def in WikiGeneratorDefaults)
        {
            var setting = await settingsService.GetSettingByKeyAsync(def.Key);
            if (setting?.Value != null)
            {
                ApplySettingToOption(options, def.Key, setting.Value);
            }
        }
    }

    /// <summary>
    /// 从 WikiGeneratorOptions 中获取默认值（字符串）
    /// </summary>
    private static string? GetOptionDefaultValue(WikiGeneratorOptions options, string key)
    {
        return key switch
        {
            "WIKI_CATALOG_MODEL" => options.CatalogModel,
            "WIKI_CATALOG_ENDPOINT" => options.CatalogEndpoint,
            "WIKI_CATALOG_API_KEY" => options.CatalogApiKey,
            "WIKI_CATALOG_REQUEST_TYPE" => options.CatalogRequestType?.ToString(),
            "WIKI_CONTENT_MODEL" => options.ContentModel,
            "WIKI_CONTENT_ENDPOINT" => options.ContentEndpoint,
            "WIKI_CONTENT_API_KEY" => options.ContentApiKey,
            "WIKI_CONTENT_REQUEST_TYPE" => options.ContentRequestType?.ToString(),
            "WIKI_TRANSLATION_MODEL" => options.TranslationModel,
            "WIKI_TRANSLATION_ENDPOINT" => options.TranslationEndpoint,
            "WIKI_TRANSLATION_API_KEY" => options.TranslationApiKey,
            "WIKI_TRANSLATION_REQUEST_TYPE" => options.TranslationRequestType?.ToString(),
            "WIKI_LANGUAGES" => options.Languages,
            "WIKI_PARALLEL_COUNT" => options.ParallelCount.ToString(),
            "WIKI_MAX_OUTPUT_TOKENS" => options.MaxOutputTokens.ToString(),
            "WIKI_DOCUMENT_GENERATION_TIMEOUT_MINUTES" => options.DocumentGenerationTimeoutMinutes.ToString(),
            "WIKI_TRANSLATION_TIMEOUT_MINUTES" => options.TranslationTimeoutMinutes.ToString(),
            "WIKI_TITLE_TRANSLATION_TIMEOUT_MINUTES" => options.TitleTranslationTimeoutMinutes.ToString(),
            "WIKI_README_MAX_LENGTH" => options.ReadmeMaxLength.ToString(),
            "WIKI_DIRECTORY_TREE_MAX_DEPTH" => options.DirectoryTreeMaxDepth.ToString(),
            "WIKI_MAX_RETRY_ATTEMPTS" => options.MaxRetryAttempts.ToString(),
            "WIKI_RETRY_DELAY_MS" => options.RetryDelayMs.ToString(),
            "WIKI_PROMPTS_DIRECTORY" => options.PromptsDirectory,
            _ => null
        };
    }

    /// <summary>
    /// 将单个设置应用到WikiGeneratorOptions
    /// </summary>
    public static void ApplySettingToOption(WikiGeneratorOptions options, string key, string value)
    {
        switch (key)
        {
            case "WIKI_CATALOG_MODEL":
                options.CatalogModel = value;
                break;
            case "WIKI_CATALOG_ENDPOINT":
                options.CatalogEndpoint = value;
                break;
            case "WIKI_CATALOG_API_KEY":
                options.CatalogApiKey = value;
                break;
            case "WIKI_CATALOG_REQUEST_TYPE" when Enum.TryParse<AiRequestType>(value, true, out var catalogType):
                options.CatalogRequestType = catalogType;
                break;
            case "WIKI_CONTENT_MODEL":
                options.ContentModel = value;
                break;
            case "WIKI_CONTENT_ENDPOINT":
                options.ContentEndpoint = value;
                break;
            case "WIKI_CONTENT_API_KEY":
                options.ContentApiKey = value;
                break;
            case "WIKI_CONTENT_REQUEST_TYPE" when Enum.TryParse<AiRequestType>(value, true, out var contentType):
                options.ContentRequestType = contentType;
                break;
            case "WIKI_TRANSLATION_MODEL":
                options.TranslationModel = value;
                break;
            case "WIKI_TRANSLATION_ENDPOINT":
                options.TranslationEndpoint = value;
                break;
            case "WIKI_TRANSLATION_API_KEY":
                options.TranslationApiKey = value;
                break;
            case "WIKI_TRANSLATION_REQUEST_TYPE" when Enum.TryParse<AiRequestType>(value, true, out var translationType):
                options.TranslationRequestType = translationType;
                break;
            case "WIKI_LANGUAGES":
                options.Languages = value;
                break;
            case "WIKI_PARALLEL_COUNT" when int.TryParse(value, out var parallelCount):
                options.ParallelCount = parallelCount;
                break;
            case "WIKI_MAX_OUTPUT_TOKENS" when int.TryParse(value, out var maxTokens):
                options.MaxOutputTokens = maxTokens;
                break;
            case "WIKI_DOCUMENT_GENERATION_TIMEOUT_MINUTES" when int.TryParse(value, out var docTimeout):
                options.DocumentGenerationTimeoutMinutes = docTimeout;
                break;
            case "WIKI_TRANSLATION_TIMEOUT_MINUTES" when int.TryParse(value, out var transTimeout):
                options.TranslationTimeoutMinutes = transTimeout;
                break;
            case "WIKI_TITLE_TRANSLATION_TIMEOUT_MINUTES" when int.TryParse(value, out var titleTimeout):
                options.TitleTranslationTimeoutMinutes = titleTimeout;
                break;
            case "WIKI_README_MAX_LENGTH" when int.TryParse(value, out var readmeLength):
                options.ReadmeMaxLength = readmeLength;
                break;
            case "WIKI_DIRECTORY_TREE_MAX_DEPTH" when int.TryParse(value, out var treeDepth):
                options.DirectoryTreeMaxDepth = treeDepth;
                break;
            case "WIKI_MAX_RETRY_ATTEMPTS" when int.TryParse(value, out var retryAttempts):
                options.MaxRetryAttempts = retryAttempts;
                break;
            case "WIKI_RETRY_DELAY_MS" when int.TryParse(value, out var retryDelay):
                options.RetryDelayMs = retryDelay;
                break;
            case "WIKI_PROMPTS_DIRECTORY":
                options.PromptsDirectory = value;
                break;
        }
    }
}
