```diff
diff --git a/.claude/settings.local.json b/.claude/settings.local.json
index 91a39b5..63efab4 100644
--- a/.claude/settings.local.json
+++ b/.claude/settings.local.json
@@ -21,7 +21,8 @@
       "Bash(bash:*)",
       "mcp__MCP_DOCKER__code-mode",
       "Bash(List files in repo root)",
-      "Bash(rg:*)"
+      "Bash(rg:*)",
+      "Bash(cd:*)"
     ],
     "deny": [],
     "ask": []
diff --git a/README.md b/README.md
index eea6f8d..7568f88 100644
--- a/README.md
+++ b/README.md
@@ -1,22 +1,5 @@
 # OpenDeepWiki
 
-[中文](README.zh-CN.md) | [English](README.md)
-
-<div align="center">
-  <img src="/img/favicon.png" alt="OpenDeepWiki Logo" width="220" />
-  <h3>AI-Driven Code Knowledge Base</h3>
-</div>
-
----
-
-# enterprise service
-
-[Pricing of enterprise services](https://docs.opendeep.wiki/pricing)
-
-Our enterprise service offers comprehensive support and flexibility for businesses seeking professional AI solutions.
-
----
-
 # Features
 
 - **Quick Conversion**: Supports converting all GitHub, GitLab, Gitee, Gitea and other code repositories into knowledge bases within minutes.
diff --git a/README_JC_walkthrough.md b/README_JC_walkthrough.md
new file mode 100644
index 0000000..0b5a8eb
--- /dev/null
+++ b/README_JC_walkthrough.md
@@ -0,0 +1,45 @@
+# OpenDeepWiki Verification Walkthrough
+
+I've completed the cleanup, localization, and feature enhancement for your OpenDeepWiki setup.
+
+## Key Changes Implemented
+
+- **Native Gemini Support**: Added built-in support for Google Gemini via [AgentFactory.cs](file:///Users/jc-vht/code_sandbox/OpenDeepWiki/src/OpenDeepWiki/Agents/AgentFactory.cs).
+- **Documentation Export**: Added a new service to export organized documentation as a ZIP archive of Markdown files.
+- **English Default**: Fixed the UI and generator to default to English.
+- **Path Cleanup**: Purged stale dot-notation paths and Chinese content from the database.
+- **Context Awareness**: Enhanced AI prompts with BigQuery and Airflow domain knowledge.
+
+## How to Verify
+
+### 1. English UI & Local Repository
+1. Visit `http://localhost:3000`. The UI should now be in English by default.
+2. Log in using the local admin credentials (created during DB initialization):
+   - **Email**: `admin@opendeepwiki.com`
+   - **Password**: `123456`
+3. Submit your workspace repository: `/Users/jc-vht/code_sandbox/OpenDeepWiki`.
+
+### 2. Gemini 2.5 Pro Optimization
+With the latest rebuild, Gemini is now the active provider. Here is how to get the best results based on the `ai.google.dev` documentation:
+
+- **Thinking/Reasoning**: Gemini 2.5 Pro (and Gemini 3) has built-in thinking tokens. This is exceptionally powerful for the `WikiGenerator` as it allows the model to "reason" about your folder structure and code dependencies before outputting the catalog.
+- **Safety Filters**: If you see empty pages or "Service Error" during generation, check the logs (`docker logs opendeepwiki-opendeepwiki-1`). Gemini's safety filters can sometimes be oversensitive with code analysis.
+- **System Instructions**: I've ensured that all prompts are passed as `System Instructions`, which is Google's preferred way to ground Gemini's behavior.
+
+### 3. Export Documentation
+Once your documentation is generated, you can download a portable snapshot:
+1. Use the following API endpoint (or browser URL):
+   `http://localhost:5000/api/v1/wiki/local/OpenDeepWiki/export`
+2. This will return a ZIP file (`local_OpenDeepWiki_docs.zip`) containing all your generated Markdown files in their respective folders.
+
+### 4. Path Fixes
+Verify that new paths use slashes (e.g., `1-project-overview/1-introduction`) instead of dots. The updated prompts now strictly enforce this.
+
+### Troubleshooting
+- **404 Errors**: I've removed the hardcoded `api.routin.ai` defaults that were overriding your environment. The latest build (Step 1281) has applied this fix.
+- **Safety Blocks**: If a specific file fails, it might be due to Gemini's safety policy.
+- **Manual Reset**: If you still see old paths or Chinese content, run:
+  ```bash
+  sqlite3 ./data/opendeepwiki.db "DELETE FROM DocCatalogs; DELETE FROM DocFiles; DELETE FROM BranchLanguages;"
+  ```
+  Then restart the app to trigger a full rescan.
diff --git a/compose.yaml b/compose.yaml
index 94874d8..988f2f6 100644
--- a/compose.yaml
+++ b/compose.yaml
@@ -31,21 +31,33 @@
       - REPOSITORIES_DIRECTORY=/data
 
       # AI 服务配置 (全局默认，用于 Chat 等功能)
-      - CHAT_API_KEY=your-chat-api-key
-      - ENDPOINT=https://api.openai.com/v1
-      - CHAT_REQUEST_TYPE=OpenAI
+      - CHAT_API_KEY=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
+      - ENDPOINT=https://api.anthropic.com
+      - CHAT_REQUEST_TYPE=Anthropic
 
       # Wiki 生成器配置 - 目录生成
-      - WIKI_CATALOG_MODEL=gpt-4o
-      - WIKI_CATALOG_ENDPOINT=https://api.openai.com/v1
-      - WIKI_CATALOG_API_KEY=your-catalog-api-key
-      - WIKI_CATALOG_REQUEST_TYPE=OpenAI
+      # Option 1: Claude (Anthropic) - Commented out
+      # - WIKI_CATALOG_MODEL=claude-3-5-sonnet-20240620
+      # - WIKI_CATALOG_ENDPOINT=https://api.anthropic.com
+      # - WIKI_CATALOG_API_KEY=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
+      # - WIKI_CATALOG_REQUEST_TYPE=Anthropic
+
+      # Option 2: Gemini (Active)
+      - WIKI_CATALOG_MODEL=gemini-2.5-pro
+      - WIKI_CATALOG_API_KEY=AIzaSyCNXub_eEERwI9GiVWoTKD-uVPBjKadE5M
+      - WIKI_CATALOG_REQUEST_TYPE=Gemini
 
       # Wiki 生成器配置 - 内容生成
-      - WIKI_CONTENT_MODEL=gpt-4o
-      - WIKI_CONTENT_ENDPOINT=https://api.openai.com/v1
-      - WIKI_CONTENT_API_KEY=your-content-api-key
-      - WIKI_CONTENT_REQUEST_TYPE=OpenAI
+      # Option 1: Claude (Anthropic) - Commented out
+      # - WIKI_CONTENT_MODEL=claude-3-5-sonnet-20240620
+      # - WIKI_CONTENT_ENDPOINT=https://api.anthropic.com
+      # - WIKI_CONTENT_API_KEY=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
+      # - WIKI_CONTENT_REQUEST_TYPE=Anthropic
+
+      # Option 2: Gemini (Active)
+      - WIKI_CONTENT_MODEL=gemini-2.5-pro
+      - WIKI_CONTENT_API_KEY=AIzaSyCNXub_eEERwI9GiVWoTKD-uVPBjKadE5M
+      - WIKI_CONTENT_REQUEST_TYPE=Gemini
 
       # Wiki 生成器配置 - 翻译 (可选，不配置则使用内容生成配置)
       # - WIKI_TRANSLATION_MODEL=gpt-4o
@@ -57,9 +69,11 @@
       - WIKI_PARALLEL_COUNT=5
 
       # 多语言支持 (逗号分隔，如: en,zh,ja,ko)
-      - WIKI_LANGUAGES=en,zh
+      - WIKI_LANGUAGES=en
     volumes:
       - ./data:/data
+      - /Users/jc-vht/code_sandbox/edna_phm_sow_airflow_antigravity:/sow:ro
+      - /Users/jc-vht/code_sandbox/AZURE-payer_propensity_models-data_processing_to_prediction:/AZURE_data_fabric:ro
 
   web:
     image: crpi-j9ha7sxwhatgtvj4.cn-shenzhen.personal.cr.aliyuncs.com/open-deepwiki/opendeepwiki-web
@@ -67,7 +81,7 @@
       context: ./web
       dockerfile: Dockerfile
     ports:
-      - "3000:3000"
+      - "3001:3000"
     restart: unless-stopped
     depends_on:
       opendeepwiki:
diff --git a/data/local/local-repo/tree b/data/local/local-repo/tree
new file mode 160000
index 0000000..2df9209
--- /dev/null
+++ b/data/local/local-repo/tree
@@ -0,0 +1 @@
+Subproject commit 2df92098d66b985d92fb50c3c4acf6f5346b4be0
diff --git a/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260212195105_Initial.Designer.cs b/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260223135805_Initial.Designer.cs
similarity index 91%
rename from src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260212195105_Initial.Designer.cs
rename to src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260223135805_Initial.Designer.cs
index d4c1074..3d22369 100644
--- a/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260212195105_Initial.Designer.cs
+++ b/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260223135805_Initial.Designer.cs
@@ -12,7 +12,7 @@ using OpenDeepWiki.Postgresql;
 namespace OpenDeepWiki.Postgresql.Migrations
 {
     [DbContext(typeof(PostgresqlDbContext))]
-    [Migration("20260212195105_Initial")]
+    [Migration("20260223135805_Initial")]
     partial class Initial
     {
         /// <inheritdoc />
@@ -1011,6 +1011,233 @@ namespace OpenDeepWiki.Postgresql.Migrations
                     b.ToTable("McpConfigs");
                 });
 
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpDailyStatistics", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("text");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime>("Date")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<long>("ErrorCount")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("InputTokens")
+                        .HasColumnType("bigint");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("boolean");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<long>("OutputTokens")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("RequestCount")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("SuccessCount")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("TotalDurationMs")
+                        .HasColumnType("bigint");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("bytea");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("Date");
+
+                    b.HasIndex("McpProviderId", "Date")
+                        .IsUnique();
+
+                    b.ToTable("McpDailyStatistics");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpProvider", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("text");
+
+                    b.Property<string>("AllowedTools")
+                        .HasMaxLength(2000)
+                        .HasColumnType("character varying(2000)");
+
+                    b.Property<string>("ApiKeyObtainUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<string>("Description")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<string>("IconUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<bool>("IsActive")
+                        .HasColumnType("boolean");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("boolean");
+
+                    b.Property<int>("MaxRequestsPerDay")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("ModelConfigId")
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<string>("Name")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<string>("RequestTypes")
+                        .HasMaxLength(2000)
+                        .HasColumnType("character varying(2000)");
+
+                    b.Property<bool>("RequiresApiKey")
+                        .HasColumnType("boolean");
+
+                    b.Property<string>("ServerUrl")
+                        .IsRequired()
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<int>("SortOrder")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("SystemApiKey")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<string>("TransportType")
+                        .IsRequired()
+                        .HasMaxLength(50)
+                        .HasColumnType("character varying(50)");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("bytea");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("IsActive");
+
+                    b.HasIndex("Name")
+                        .IsUnique();
+
+                    b.HasIndex("SortOrder");
+
+                    b.ToTable("McpProviders");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpUsageLog", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("text");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<long>("DurationMs")
+                        .HasColumnType("bigint");
+
+                    b.Property<string>("ErrorMessage")
+                        .HasMaxLength(2000)
+                        .HasColumnType("character varying(2000)");
+
+                    b.Property<int>("InputTokens")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("IpAddress")
+                        .HasMaxLength(45)
+                        .HasColumnType("character varying(45)");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("boolean");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<int>("OutputTokens")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("RequestSummary")
+                        .HasMaxLength(1000)
+                        .HasColumnType("character varying(1000)");
+
+                    b.Property<int>("ResponseStatus")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("ToolName")
+                        .IsRequired()
+                        .HasMaxLength(200)
+                        .HasColumnType("character varying(200)");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<string>("UserAgent")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<string>("UserId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("bytea");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("CreatedAt");
+
+                    b.HasIndex("ResponseStatus");
+
+                    b.HasIndex("ToolName");
+
+                    b.HasIndex("McpProviderId", "CreatedAt");
+
+                    b.HasIndex("UserId", "CreatedAt");
+
+                    b.ToTable("McpUsageLogs");
+                });
+
             modelBuilder.Entity("OpenDeepWiki.Entities.ModelConfig", b =>
                 {
                     b.Property<string>("Id")
diff --git a/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260212195105_Initial.cs b/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260223135805_Initial.cs
similarity index 91%
rename from src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260212195105_Initial.cs
rename to src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260223135805_Initial.cs
index e8cdbe3..b9e878f 100644
--- a/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260212195105_Initial.cs
+++ b/src/EFCore/OpenDeepWiki.Postgresql/Migrations/20260223135805_Initial.cs
@@ -250,6 +250,87 @@ namespace OpenDeepWiki.Postgresql.Migrations
                     table.PrimaryKey("PK_McpConfigs", x => x.Id);
                 });
 
+            migrationBuilder.CreateTable(
+                name: "McpDailyStatistics",
+                columns: table => new
+                {
+                    Id = table.Column<string>(type: "text", nullable: false),
+                    McpProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
+                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
+                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
+                    SuccessCount = table.Column<long>(type: "bigint", nullable: false),
+                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
+                    TotalDurationMs = table.Column<long>(type: "bigint", nullable: false),
+                    InputTokens = table.Column<long>(type: "bigint", nullable: false),
+                    OutputTokens = table.Column<long>(type: "bigint", nullable: false),
+                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
+                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
+                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
+                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
+                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
+                },
+                constraints: table =>
+                {
+                    table.PrimaryKey("PK_McpDailyStatistics", x => x.Id);
+                });
+
+            migrationBuilder.CreateTable(
+                name: "McpProviders",
+                columns: table => new
+                {
+                    Id = table.Column<string>(type: "text", nullable: false),
+                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
+                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
+                    ServerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
+                    TransportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
+                    RequiresApiKey = table.Column<bool>(type: "boolean", nullable: false),
+                    ApiKeyObtainUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
+                    SystemApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
+                    ModelConfigId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
+                    RequestTypes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
+                    AllowedTools = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
+                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
+                    SortOrder = table.Column<int>(type: "integer", nullable: false),
+                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
+                    MaxRequestsPerDay = table.Column<int>(type: "integer", nullable: false),
+                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
+                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
+                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
+                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
+                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
+                },
+                constraints: table =>
+                {
+                    table.PrimaryKey("PK_McpProviders", x => x.Id);
+                });
+
+            migrationBuilder.CreateTable(
+                name: "McpUsageLogs",
+                columns: table => new
+                {
+                    Id = table.Column<string>(type: "text", nullable: false),
+                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
+                    McpProviderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
+                    ToolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
+                    RequestSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
+                    ResponseStatus = table.Column<int>(type: "integer", nullable: false),
+                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
+                    InputTokens = table.Column<int>(type: "integer", nullable: false),
+                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
+                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
+                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
+                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
+                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
+                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
+                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
+                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
+                    Version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
+                },
+                constraints: table =>
+                {
+                    table.PrimaryKey("PK_McpUsageLogs", x => x.Id);
+                });
+
             migrationBuilder.CreateTable(
                 name: "ModelConfigs",
                 columns: table => new
@@ -1185,6 +1266,58 @@ namespace OpenDeepWiki.Postgresql.Migrations
                 column: "Name",
                 unique: true);
 
+            migrationBuilder.CreateIndex(
+                name: "IX_McpDailyStatistics_Date",
+                table: "McpDailyStatistics",
+                column: "Date");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpDailyStatistics_McpProviderId_Date",
+                table: "McpDailyStatistics",
+                columns: new[] { "McpProviderId", "Date" },
+                unique: true);
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpProviders_IsActive",
+                table: "McpProviders",
+                column: "IsActive");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpProviders_Name",
+                table: "McpProviders",
+                column: "Name",
+                unique: true);
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpProviders_SortOrder",
+                table: "McpProviders",
+                column: "SortOrder");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_CreatedAt",
+                table: "McpUsageLogs",
+                column: "CreatedAt");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_McpProviderId_CreatedAt",
+                table: "McpUsageLogs",
+                columns: new[] { "McpProviderId", "CreatedAt" });
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_ResponseStatus",
+                table: "McpUsageLogs",
+                column: "ResponseStatus");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_ToolName",
+                table: "McpUsageLogs",
+                column: "ToolName");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_UserId_CreatedAt",
+                table: "McpUsageLogs",
+                columns: new[] { "UserId", "CreatedAt" });
+
             migrationBuilder.CreateIndex(
                 name: "IX_ModelConfigs_Name",
                 table: "ModelConfigs",
@@ -1391,6 +1524,15 @@ namespace OpenDeepWiki.Postgresql.Migrations
             migrationBuilder.DropTable(
                 name: "McpConfigs");
 
+            migrationBuilder.DropTable(
+                name: "McpDailyStatistics");
+
+            migrationBuilder.DropTable(
+                name: "McpProviders");
+
+            migrationBuilder.DropTable(
+                name: "McpUsageLogs");
+
             migrationBuilder.DropTable(
                 name: "ModelConfigs");
 
diff --git a/src/EFCore/OpenDeepWiki.Postgresql/Migrations/PostgresqlDbContextModelSnapshot.cs b/src/EFCore/OpenDeepWiki.Postgresql/Migrations/PostgresqlDbContextModelSnapshot.cs
index f5adb08..c8d96cf 100644
--- a/src/EFCore/OpenDeepWiki.Postgresql/Migrations/PostgresqlDbContextModelSnapshot.cs
+++ b/src/EFCore/OpenDeepWiki.Postgresql/Migrations/PostgresqlDbContextModelSnapshot.cs
@@ -1008,6 +1008,233 @@ namespace OpenDeepWiki.Postgresql.Migrations
                     b.ToTable("McpConfigs");
                 });
 
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpDailyStatistics", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("text");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime>("Date")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<long>("ErrorCount")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("InputTokens")
+                        .HasColumnType("bigint");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("boolean");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<long>("OutputTokens")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("RequestCount")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("SuccessCount")
+                        .HasColumnType("bigint");
+
+                    b.Property<long>("TotalDurationMs")
+                        .HasColumnType("bigint");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("bytea");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("Date");
+
+                    b.HasIndex("McpProviderId", "Date")
+                        .IsUnique();
+
+                    b.ToTable("McpDailyStatistics");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpProvider", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("text");
+
+                    b.Property<string>("AllowedTools")
+                        .HasMaxLength(2000)
+                        .HasColumnType("character varying(2000)");
+
+                    b.Property<string>("ApiKeyObtainUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<string>("Description")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<string>("IconUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<bool>("IsActive")
+                        .HasColumnType("boolean");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("boolean");
+
+                    b.Property<int>("MaxRequestsPerDay")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("ModelConfigId")
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<string>("Name")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<string>("RequestTypes")
+                        .HasMaxLength(2000)
+                        .HasColumnType("character varying(2000)");
+
+                    b.Property<bool>("RequiresApiKey")
+                        .HasColumnType("boolean");
+
+                    b.Property<string>("ServerUrl")
+                        .IsRequired()
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<int>("SortOrder")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("SystemApiKey")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<string>("TransportType")
+                        .IsRequired()
+                        .HasMaxLength(50)
+                        .HasColumnType("character varying(50)");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("bytea");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("IsActive");
+
+                    b.HasIndex("Name")
+                        .IsUnique();
+
+                    b.HasIndex("SortOrder");
+
+                    b.ToTable("McpProviders");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpUsageLog", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("text");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<long>("DurationMs")
+                        .HasColumnType("bigint");
+
+                    b.Property<string>("ErrorMessage")
+                        .HasMaxLength(2000)
+                        .HasColumnType("character varying(2000)");
+
+                    b.Property<int>("InputTokens")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("IpAddress")
+                        .HasMaxLength(45)
+                        .HasColumnType("character varying(45)");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("boolean");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<int>("OutputTokens")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("RequestSummary")
+                        .HasMaxLength(1000)
+                        .HasColumnType("character varying(1000)");
+
+                    b.Property<int>("ResponseStatus")
+                        .HasColumnType("integer");
+
+                    b.Property<string>("ToolName")
+                        .IsRequired()
+                        .HasMaxLength(200)
+                        .HasColumnType("character varying(200)");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("timestamp with time zone");
+
+                    b.Property<string>("UserAgent")
+                        .HasMaxLength(500)
+                        .HasColumnType("character varying(500)");
+
+                    b.Property<string>("UserId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("character varying(100)");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("bytea");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("CreatedAt");
+
+                    b.HasIndex("ResponseStatus");
+
+                    b.HasIndex("ToolName");
+
+                    b.HasIndex("McpProviderId", "CreatedAt");
+
+                    b.HasIndex("UserId", "CreatedAt");
+
+                    b.ToTable("McpUsageLogs");
+                });
+
             modelBuilder.Entity("OpenDeepWiki.Entities.ModelConfig", b =>
                 {
                     b.Property<string>("Id")
diff --git a/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260212193648_Initial.Designer.cs b/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260223135846_Initial.Designer.cs
similarity index 91%
rename from src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260212193648_Initial.Designer.cs
rename to src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260223135846_Initial.Designer.cs
index 6d914d1..892af97 100644
--- a/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260212193648_Initial.Designer.cs
+++ b/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260223135846_Initial.Designer.cs
@@ -11,7 +11,7 @@ using OpenDeepWiki.Sqlite;
 namespace OpenDeepWiki.Sqlite.Migrations
 {
     [DbContext(typeof(SqliteDbContext))]
-    [Migration("20260212193648_Initial")]
+    [Migration("20260223135846_Initial")]
     partial class Initial
     {
         /// <inheritdoc />
@@ -1006,6 +1006,233 @@ namespace OpenDeepWiki.Sqlite.Migrations
                     b.ToTable("McpConfigs");
                 });
 
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpDailyStatistics", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("Date")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<long>("ErrorCount")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("InputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<long>("OutputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("RequestCount")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("SuccessCount")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("TotalDurationMs")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("BLOB");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("Date");
+
+                    b.HasIndex("McpProviderId", "Date")
+                        .IsUnique();
+
+                    b.ToTable("McpDailyStatistics");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpProvider", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("AllowedTools")
+                        .HasMaxLength(2000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("ApiKeyObtainUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("Description")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("IconUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<bool>("IsActive")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<int>("MaxRequestsPerDay")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ModelConfigId")
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("Name")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("RequestTypes")
+                        .HasMaxLength(2000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<bool>("RequiresApiKey")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ServerUrl")
+                        .IsRequired()
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("SortOrder")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("SystemApiKey")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("TransportType")
+                        .IsRequired()
+                        .HasMaxLength(50)
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("BLOB");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("IsActive");
+
+                    b.HasIndex("Name")
+                        .IsUnique();
+
+                    b.HasIndex("SortOrder");
+
+                    b.ToTable("McpProviders");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpUsageLog", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<long>("DurationMs")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ErrorMessage")
+                        .HasMaxLength(2000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("InputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("IpAddress")
+                        .HasMaxLength(45)
+                        .HasColumnType("TEXT");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("OutputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("RequestSummary")
+                        .HasMaxLength(1000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("ResponseStatus")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ToolName")
+                        .IsRequired()
+                        .HasMaxLength(200)
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("UserAgent")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("UserId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("BLOB");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("CreatedAt");
+
+                    b.HasIndex("ResponseStatus");
+
+                    b.HasIndex("ToolName");
+
+                    b.HasIndex("McpProviderId", "CreatedAt");
+
+                    b.HasIndex("UserId", "CreatedAt");
+
+                    b.ToTable("McpUsageLogs");
+                });
+
             modelBuilder.Entity("OpenDeepWiki.Entities.ModelConfig", b =>
                 {
                     b.Property<string>("Id")
diff --git a/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260212193648_Initial.cs b/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260223135846_Initial.cs
similarity index 91%
rename from src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260212193648_Initial.cs
rename to src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260223135846_Initial.cs
index d34c7fa..25ffc75 100644
--- a/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260212193648_Initial.cs
+++ b/src/EFCore/OpenDeepWiki.Sqlite/Migrations/20260223135846_Initial.cs
@@ -250,6 +250,87 @@ namespace OpenDeepWiki.Sqlite.Migrations
                     table.PrimaryKey("PK_McpConfigs", x => x.Id);
                 });
 
+            migrationBuilder.CreateTable(
+                name: "McpDailyStatistics",
+                columns: table => new
+                {
+                    Id = table.Column<string>(type: "TEXT", nullable: false),
+                    McpProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
+                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
+                    RequestCount = table.Column<long>(type: "INTEGER", nullable: false),
+                    SuccessCount = table.Column<long>(type: "INTEGER", nullable: false),
+                    ErrorCount = table.Column<long>(type: "INTEGER", nullable: false),
+                    TotalDurationMs = table.Column<long>(type: "INTEGER", nullable: false),
+                    InputTokens = table.Column<long>(type: "INTEGER", nullable: false),
+                    OutputTokens = table.Column<long>(type: "INTEGER", nullable: false),
+                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
+                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
+                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
+                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
+                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
+                },
+                constraints: table =>
+                {
+                    table.PrimaryKey("PK_McpDailyStatistics", x => x.Id);
+                });
+
+            migrationBuilder.CreateTable(
+                name: "McpProviders",
+                columns: table => new
+                {
+                    Id = table.Column<string>(type: "TEXT", nullable: false),
+                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
+                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
+                    ServerUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
+                    TransportType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
+                    RequiresApiKey = table.Column<bool>(type: "INTEGER", nullable: false),
+                    ApiKeyObtainUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
+                    SystemApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
+                    ModelConfigId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
+                    RequestTypes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
+                    AllowedTools = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
+                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
+                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
+                    IconUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
+                    MaxRequestsPerDay = table.Column<int>(type: "INTEGER", nullable: false),
+                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
+                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
+                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
+                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
+                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
+                },
+                constraints: table =>
+                {
+                    table.PrimaryKey("PK_McpProviders", x => x.Id);
+                });
+
+            migrationBuilder.CreateTable(
+                name: "McpUsageLogs",
+                columns: table => new
+                {
+                    Id = table.Column<string>(type: "TEXT", nullable: false),
+                    UserId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
+                    McpProviderId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
+                    ToolName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
+                    RequestSummary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
+                    ResponseStatus = table.Column<int>(type: "INTEGER", nullable: false),
+                    DurationMs = table.Column<long>(type: "INTEGER", nullable: false),
+                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
+                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
+                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 45, nullable: true),
+                    UserAgent = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
+                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
+                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
+                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
+                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
+                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
+                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
+                },
+                constraints: table =>
+                {
+                    table.PrimaryKey("PK_McpUsageLogs", x => x.Id);
+                });
+
             migrationBuilder.CreateTable(
                 name: "ModelConfigs",
                 columns: table => new
@@ -1185,6 +1266,58 @@ namespace OpenDeepWiki.Sqlite.Migrations
                 column: "Name",
                 unique: true);
 
+            migrationBuilder.CreateIndex(
+                name: "IX_McpDailyStatistics_Date",
+                table: "McpDailyStatistics",
+                column: "Date");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpDailyStatistics_McpProviderId_Date",
+                table: "McpDailyStatistics",
+                columns: new[] { "McpProviderId", "Date" },
+                unique: true);
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpProviders_IsActive",
+                table: "McpProviders",
+                column: "IsActive");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpProviders_Name",
+                table: "McpProviders",
+                column: "Name",
+                unique: true);
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpProviders_SortOrder",
+                table: "McpProviders",
+                column: "SortOrder");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_CreatedAt",
+                table: "McpUsageLogs",
+                column: "CreatedAt");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_McpProviderId_CreatedAt",
+                table: "McpUsageLogs",
+                columns: new[] { "McpProviderId", "CreatedAt" });
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_ResponseStatus",
+                table: "McpUsageLogs",
+                column: "ResponseStatus");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_ToolName",
+                table: "McpUsageLogs",
+                column: "ToolName");
+
+            migrationBuilder.CreateIndex(
+                name: "IX_McpUsageLogs_UserId_CreatedAt",
+                table: "McpUsageLogs",
+                columns: new[] { "UserId", "CreatedAt" });
+
             migrationBuilder.CreateIndex(
                 name: "IX_ModelConfigs_Name",
                 table: "ModelConfigs",
@@ -1391,6 +1524,15 @@ namespace OpenDeepWiki.Sqlite.Migrations
             migrationBuilder.DropTable(
                 name: "McpConfigs");
 
+            migrationBuilder.DropTable(
+                name: "McpDailyStatistics");
+
+            migrationBuilder.DropTable(
+                name: "McpProviders");
+
+            migrationBuilder.DropTable(
+                name: "McpUsageLogs");
+
             migrationBuilder.DropTable(
                 name: "ModelConfigs");
 
diff --git a/src/EFCore/OpenDeepWiki.Sqlite/Migrations/SqliteDbContextModelSnapshot.cs b/src/EFCore/OpenDeepWiki.Sqlite/Migrations/SqliteDbContextModelSnapshot.cs
index 427bd0d..c13846f 100644
--- a/src/EFCore/OpenDeepWiki.Sqlite/Migrations/SqliteDbContextModelSnapshot.cs
+++ b/src/EFCore/OpenDeepWiki.Sqlite/Migrations/SqliteDbContextModelSnapshot.cs
@@ -1003,6 +1003,233 @@ namespace OpenDeepWiki.Sqlite.Migrations
                     b.ToTable("McpConfigs");
                 });
 
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpDailyStatistics", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("Date")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<long>("ErrorCount")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("InputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<long>("OutputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("RequestCount")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("SuccessCount")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<long>("TotalDurationMs")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("BLOB");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("Date");
+
+                    b.HasIndex("McpProviderId", "Date")
+                        .IsUnique();
+
+                    b.ToTable("McpDailyStatistics");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpProvider", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("AllowedTools")
+                        .HasMaxLength(2000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("ApiKeyObtainUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("Description")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("IconUrl")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<bool>("IsActive")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<int>("MaxRequestsPerDay")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ModelConfigId")
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("Name")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("RequestTypes")
+                        .HasMaxLength(2000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<bool>("RequiresApiKey")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ServerUrl")
+                        .IsRequired()
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("SortOrder")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("SystemApiKey")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("TransportType")
+                        .IsRequired()
+                        .HasMaxLength(50)
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("BLOB");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("IsActive");
+
+                    b.HasIndex("Name")
+                        .IsUnique();
+
+                    b.HasIndex("SortOrder");
+
+                    b.ToTable("McpProviders");
+                });
+
+            modelBuilder.Entity("OpenDeepWiki.Entities.McpUsageLog", b =>
+                {
+                    b.Property<string>("Id")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime>("CreatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("DeletedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<long>("DurationMs")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ErrorMessage")
+                        .HasMaxLength(2000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("InputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("IpAddress")
+                        .HasMaxLength(45)
+                        .HasColumnType("TEXT");
+
+                    b.Property<bool>("IsDeleted")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("McpProviderId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("OutputTokens")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("RequestSummary")
+                        .HasMaxLength(1000)
+                        .HasColumnType("TEXT");
+
+                    b.Property<int>("ResponseStatus")
+                        .HasColumnType("INTEGER");
+
+                    b.Property<string>("ToolName")
+                        .IsRequired()
+                        .HasMaxLength(200)
+                        .HasColumnType("TEXT");
+
+                    b.Property<DateTime?>("UpdatedAt")
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("UserAgent")
+                        .HasMaxLength(500)
+                        .HasColumnType("TEXT");
+
+                    b.Property<string>("UserId")
+                        .IsRequired()
+                        .HasMaxLength(100)
+                        .HasColumnType("TEXT");
+
+                    b.Property<byte[]>("Version")
+                        .IsConcurrencyToken()
+                        .ValueGeneratedOnAddOrUpdate()
+                        .HasColumnType("BLOB");
+
+                    b.HasKey("Id");
+
+                    b.HasIndex("CreatedAt");
+
+                    b.HasIndex("ResponseStatus");
+
+                    b.HasIndex("ToolName");
+
+                    b.HasIndex("McpProviderId", "CreatedAt");
+
+                    b.HasIndex("UserId", "CreatedAt");
+
+                    b.ToTable("McpUsageLogs");
+                });
+
             modelBuilder.Entity("OpenDeepWiki.Entities.ModelConfig", b =>
                 {
                     b.Property<string>("Id")
diff --git a/src/OpenDeepWiki.EFCore/MasterDbContext.cs b/src/OpenDeepWiki.EFCore/MasterDbContext.cs
index 52e4e81..e9f2101 100644
--- a/src/OpenDeepWiki.EFCore/MasterDbContext.cs
+++ b/src/OpenDeepWiki.EFCore/MasterDbContext.cs
@@ -44,6 +44,9 @@ public interface IContext : IDisposable
     DbSet<ChatLog> ChatLogs { get; set; }
     DbSet<TranslationTask> TranslationTasks { get; set; }
     DbSet<IncrementalUpdateTask> IncrementalUpdateTasks { get; set; }
+    DbSet<McpProvider> McpProviders { get; set; }
+    DbSet<McpUsageLog> McpUsageLogs { get; set; }
+    DbSet<McpDailyStatistics> McpDailyStatistics { get; set; }
 
     Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
 }
@@ -91,6 +94,9 @@ public abstract class MasterDbContext : DbContext, IContext
     public DbSet<ChatLog> ChatLogs { get; set; } = null!;
     public DbSet<TranslationTask> TranslationTasks { get; set; } = null!;
     public DbSet<IncrementalUpdateTask> IncrementalUpdateTasks { get; set; } = null!;
+    public DbSet<McpProvider> McpProviders { get; set; } = null!;
+    public DbSet<McpUsageLog> McpUsageLogs { get; set; } = null!;
+    public DbSet<McpDailyStatistics> McpDailyStatistics { get; set; } = null!;
 
     protected override void OnModelCreating(ModelBuilder modelBuilder)
     {
@@ -277,5 +283,104 @@ public abstract class MasterDbContext : DbContext, IContext
         // IncrementalUpdateTask 优先级和创建时间索引（用于按优先级排序处理）
         modelBuilder.Entity<IncrementalUpdateTask>()
             .HasIndex(t => new { t.Priority, t.CreatedAt });
+
+        // McpProvider 表配置
+        modelBuilder.Entity<McpProvider>(builder =>
+        {
+            builder.Property(m => m.Name)
+                .IsRequired()
+                .HasMaxLength(100);
+
+            builder.Property(m => m.Description)
+                .HasMaxLength(500);
+
+            builder.Property(m => m.ServerUrl)
+                .IsRequired()
+                .HasMaxLength(500);
+
+            builder.Property(m => m.TransportType)
+                .IsRequired()
+                .HasMaxLength(50);
+
+            builder.Property(m => m.ApiKeyObtainUrl)
+                .HasMaxLength(500);
+
+            builder.Property(m => m.SystemApiKey)
+                .HasMaxLength(500);
+
+            builder.Property(m => m.RequestTypes)
+                .HasMaxLength(2000);
+
+            builder.Property(m => m.AllowedTools)
+                .HasMaxLength(2000);
+
+            builder.Property(m => m.IconUrl)
+                .HasMaxLength(500);
+
+            // 名称唯一索引
+            builder.HasIndex(m => m.Name)
+                .IsUnique();
+
+            // 排序索引
+            builder.HasIndex(m => m.SortOrder);
+
+            // 启用状态索引
+            builder.HasIndex(m => m.IsActive);
+        });
+
+        // McpUsageLog 表配置
+        modelBuilder.Entity<McpUsageLog>(builder =>
+        {
+            builder.Property(l => l.UserId)
+                .IsRequired()
+                .HasMaxLength(100);
+
+            builder.Property(l => l.McpProviderId)
+                .IsRequired()
+                .HasMaxLength(100);
+
+            builder.Property(l => l.ToolName)
+                .IsRequired()
+                .HasMaxLength(200);
+
+            builder.Property(l => l.RequestSummary)
+                .HasMaxLength(1000);
+
+            builder.Property(l => l.ErrorMessage)
+                .HasMaxLength(2000);
+
+            builder.Property(l => l.IpAddress)
+                .HasMaxLength(45);
+
+            // 用户ID和创建时间索引
+            builder.HasIndex(l => new { l.UserId, l.CreatedAt });
+
+            // 提供商ID和创建时间索引
+            builder.HasIndex(l => new { l.McpProviderId, l.CreatedAt });
+
+            // 工具名索引
+            builder.HasIndex(l => l.ToolName);
+
+            // 状态索引（基于 HTTP 状态码判断成功）
+            builder.HasIndex(l => l.ResponseStatus);
+
+            // 创建时间索引
+            builder.HasIndex(l => l.CreatedAt);
+        });
+
+        // McpDailyStatistics 表配置
+        modelBuilder.Entity<McpDailyStatistics>(builder =>
+        {
+            builder.Property(s => s.McpProviderId)
+                .IsRequired()
+                .HasMaxLength(100);
+
+            // 提供商ID和日期唯一索引
+            builder.HasIndex(s => new { s.McpProviderId, s.Date })
+                .IsUnique();
+
+            // 日期索引
+            builder.HasIndex(s => s.Date);
+        });
     }
 }
diff --git a/src/OpenDeepWiki.Entities/Mcp/McpDailyStatistics.cs b/src/OpenDeepWiki.Entities/Mcp/McpDailyStatistics.cs
new file mode 100644
index 0000000..0acd3d1
--- /dev/null
+++ b/src/OpenDeepWiki.Entities/Mcp/McpDailyStatistics.cs
@@ -0,0 +1,47 @@
+namespace OpenDeepWiki.Entities;
+
+/// <summary>
+/// MCP 每日统计聚合实体
+/// </summary>
+public class McpDailyStatistics : AggregateRoot<string>
+{
+    /// <summary>
+    /// 提供商 ID
+    /// </summary>
+    public string? McpProviderId { get; set; }
+
+    /// <summary>
+    /// 统计日期
+    /// </summary>
+    public DateTime Date { get; set; }
+
+    /// <summary>
+    /// 请求总数
+    /// </summary>
+    public long RequestCount { get; set; }
+
+    /// <summary>
+    /// 成功数
+    /// </summary>
+    public long SuccessCount { get; set; }
+
+    /// <summary>
+    /// 错误数
+    /// </summary>
+    public long ErrorCount { get; set; }
+
+    /// <summary>
+    /// 总耗时（毫秒）
+    /// </summary>
+    public long TotalDurationMs { get; set; }
+
+    /// <summary>
+    /// 输入 Token 总量
+    /// </summary>
+    public long InputTokens { get; set; }
+
+    /// <summary>
+    /// 输出 Token 总量
+    /// </summary>
+    public long OutputTokens { get; set; }
+}
diff --git a/src/OpenDeepWiki.Entities/Mcp/McpProvider.cs b/src/OpenDeepWiki.Entities/Mcp/McpProvider.cs
new file mode 100644
index 0000000..fc253b8
--- /dev/null
+++ b/src/OpenDeepWiki.Entities/Mcp/McpProvider.cs
@@ -0,0 +1,90 @@
+using System.ComponentModel.DataAnnotations;
+
+namespace OpenDeepWiki.Entities;
+
+/// <summary>
+/// MCP 提供商配置实体（管理员管理）
+/// </summary>
+public class McpProvider : AggregateRoot<string>
+{
+    /// <summary>
+    /// 提供商名称
+    /// </summary>
+    [Required]
+    [StringLength(100)]
+    public string Name { get; set; } = string.Empty;
+
+    /// <summary>
+    /// 提供商描述
+    /// </summary>
+    [StringLength(500)]
+    public string? Description { get; set; }
+
+    /// <summary>
+    /// MCP 服务端点地址
+    /// </summary>
+    [Required]
+    [StringLength(500)]
+    public string ServerUrl { get; set; } = string.Empty;
+
+    /// <summary>
+    /// 传输方式：sse | streamable_http
+    /// </summary>
+    [Required]
+    [StringLength(50)]
+    public string TransportType { get; set; } = "streamable_http";
+
+    /// <summary>
+    /// 是否需要用户提供 API Key
+    /// </summary>
+    public bool RequiresApiKey { get; set; } = true;
+
+    /// <summary>
+    /// 用户获取 API Key 的地址（管理员填写）
+    /// </summary>
+    [StringLength(500)]
+    public string? ApiKeyObtainUrl { get; set; }
+
+    /// <summary>
+    /// 系统级 API Key（RequiresApiKey=false 时系统自动带上）
+    /// </summary>
+    [StringLength(500)]
+    public string? SystemApiKey { get; set; }
+
+    /// <summary>
+    /// 关联的 AI 模型配置 ID（FK → ModelConfig）
+    /// </summary>
+    [StringLength(100)]
+    public string? ModelConfigId { get; set; }
+
+    /// <summary>
+    /// 管理员配置的请求类型 JSON 数组
+    /// </summary>
+    public string? RequestTypes { get; set; }
+
+    /// <summary>
+    /// 允许暴露的工具 JSON 数组
+    /// </summary>
+    public string? AllowedTools { get; set; }
+
+    /// <summary>
+    /// 是否启用
+    /// </summary>
+    public bool IsActive { get; set; } = true;
+
+    /// <summary>
+    /// 排序
+    /// </summary>
+    public int SortOrder { get; set; } = 0;
+
+    /// <summary>
+    /// 提供商图标 URL
+    /// </summary>
+    [StringLength(500)]
+    public string? IconUrl { get; set; }
+
+    /// <summary>
+    /// 每日请求限额（0=无限制）
+    /// </summary>
+    public int MaxRequestsPerDay { get; set; } = 0;
+}
diff --git a/src/OpenDeepWiki.Entities/Mcp/McpUsageLog.cs b/src/OpenDeepWiki.Entities/Mcp/McpUsageLog.cs
new file mode 100644
index 0000000..061c5bb
--- /dev/null
+++ b/src/OpenDeepWiki.Entities/Mcp/McpUsageLog.cs
@@ -0,0 +1,72 @@
+using System.ComponentModel.DataAnnotations;
+
+namespace OpenDeepWiki.Entities;
+
+/// <summary>
+/// MCP 使用日志实体
+/// </summary>
+public class McpUsageLog : AggregateRoot<string>
+{
+    /// <summary>
+    /// 用户 ID（从 Bearer Token 解析）
+    /// </summary>
+    [StringLength(100)]
+    public string? UserId { get; set; }
+
+    /// <summary>
+    /// 提供商 ID
+    /// </summary>
+    [StringLength(100)]
+    public string? McpProviderId { get; set; }
+
+    /// <summary>
+    /// 调用的工具名
+    /// </summary>
+    [Required]
+    [StringLength(200)]
+    public string ToolName { get; set; } = string.Empty;
+
+    /// <summary>
+    /// 请求摘要
+    /// </summary>
+    [StringLength(1000)]
+    public string? RequestSummary { get; set; }
+
+    /// <summary>
+    /// HTTP 状态码
+    /// </summary>
+    public int ResponseStatus { get; set; }
+
+    /// <summary>
+    /// 响应耗时（毫秒）
+    /// </summary>
+    public long DurationMs { get; set; }
+
+    /// <summary>
+    /// 输入 Token 数
+    /// </summary>
+    public int InputTokens { get; set; }
+
+    /// <summary>
+    /// 输出 Token 数
+    /// </summary>
+    public int OutputTokens { get; set; }
+
+    /// <summary>
+    /// 请求 IP
+    /// </summary>
+    [StringLength(50)]
+    public string? IpAddress { get; set; }
+
+    /// <summary>
+    /// 客户端 User-Agent
+    /// </summary>
+    [StringLength(500)]
+    public string? UserAgent { get; set; }
+
+    /// <summary>
+    /// 错误信息
+    /// </summary>
+    [StringLength(2000)]
+    public string? ErrorMessage { get; set; }
+}
diff --git a/src/OpenDeepWiki/Agents/AgentFactory.cs b/src/OpenDeepWiki/Agents/AgentFactory.cs
index c9f0030..3155b2c 100644
--- a/src/OpenDeepWiki/Agents/AgentFactory.cs
+++ b/src/OpenDeepWiki/Agents/AgentFactory.cs
@@ -17,7 +17,8 @@ namespace OpenDeepWiki.Agents
         OpenAI,
         AzureOpenAI,
         OpenAIResponses,
-        Anthropic
+        Anthropic,
+        Gemini
     }
 
     public class AiRequestOptions
@@ -118,6 +119,34 @@ namespace OpenDeepWiki.Agents
                 var anthropicClient = client.AsAIAgent(clientAgentOptions);
                 return anthropicClient;
             }
+            else if (option.RequestType == AiRequestType.Gemini)
+            {
+                // Ensure we use the Gemini endpoint even if a global Anthropic endpoint is set in environment
+                var geminiEndpoint = option.Endpoint;
+                if (string.IsNullOrEmpty(geminiEndpoint) || geminiEndpoint == DefaultEndpoint || geminiEndpoint.Contains("anthropic.com"))
+                {
+                    geminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/";
+                }
+                
+                if (!geminiEndpoint.EndsWith("/"))
+                {
+                    geminiEndpoint += "/";
+                }
+
+                var clientOptions = new OpenAIClientOptions()
+                {
+                    Endpoint = new Uri(geminiEndpoint),
+                    Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(httpClient)
+                };
+
+                var openAiClient = new OpenAIClient(
+                    new ApiKeyCredential(option.ApiKey ?? string.Empty),
+                    clientOptions);
+
+                var openAIClient = openAiClient.GetChatClient(model);
+
+                return openAIClient.AsAIAgent(clientAgentOptions);
+            }
 
             throw new NotSupportedException("Unknown AI request type.");
         }
diff --git a/src/OpenDeepWiki/Dockerfile b/src/OpenDeepWiki/Dockerfile
index 9fce829..a281f11 100644
--- a/src/OpenDeepWiki/Dockerfile
+++ b/src/OpenDeepWiki/Dockerfile
@@ -1,4 +1,6 @@
 ﻿FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
+USER root
+RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
 USER $APP_UID
 WORKDIR /app
 EXPOSE 8080
diff --git a/src/OpenDeepWiki/Endpoints/Admin/AdminEndpoints.cs b/src/OpenDeepWiki/Endpoints/Admin/AdminEndpoints.cs
index 67a8233..d02e186 100644
--- a/src/OpenDeepWiki/Endpoints/Admin/AdminEndpoints.cs
+++ b/src/OpenDeepWiki/Endpoints/Admin/AdminEndpoints.cs
@@ -20,6 +20,7 @@ public static class AdminEndpoints
         adminGroup.MapAdminToolsEndpoints();
         adminGroup.MapAdminSettingsEndpoints();
         adminGroup.MapAdminChatAssistantEndpoints();
+        adminGroup.MapAdminMcpProviderEndpoints();
 
         return app;
     }
diff --git a/src/OpenDeepWiki/Endpoints/Admin/AdminMcpProviderEndpoints.cs b/src/OpenDeepWiki/Endpoints/Admin/AdminMcpProviderEndpoints.cs
new file mode 100644
index 0000000..13dc39d
--- /dev/null
+++ b/src/OpenDeepWiki/Endpoints/Admin/AdminMcpProviderEndpoints.cs
@@ -0,0 +1,78 @@
+using Microsoft.AspNetCore.Mvc;
+using OpenDeepWiki.Models.Admin;
+using OpenDeepWiki.Services.Admin;
+
+namespace OpenDeepWiki.Endpoints.Admin;
+
+/// <summary>
+/// 管理端 MCP 提供商端点
+/// </summary>
+public static class AdminMcpProviderEndpoints
+{
+    public static RouteGroupBuilder MapAdminMcpProviderEndpoints(this RouteGroupBuilder group)
+    {
+        var mcpGroup = group.MapGroup("/mcp-providers")
+            .WithTags("管理端-MCP提供商");
+
+        mcpGroup.MapGet("/", async ([FromServices] IAdminMcpProviderService service) =>
+        {
+            var result = await service.GetProvidersAsync();
+            return Results.Ok(new { success = true, data = result });
+        }).WithName("AdminGetMcpProviders");
+
+        mcpGroup.MapPost("/", async (
+            [FromBody] McpProviderRequest request,
+            [FromServices] IAdminMcpProviderService service) =>
+        {
+            var result = await service.CreateProviderAsync(request);
+            return Results.Ok(new { success = true, data = result });
+        }).WithName("AdminCreateMcpProvider");
+
+        mcpGroup.MapPut("/{id}", async (
+            string id,
+            [FromBody] McpProviderRequest request,
+            [FromServices] IAdminMcpProviderService service) =>
+        {
+            var result = await service.UpdateProviderAsync(id, request);
+            return result
+                ? Results.Ok(new { success = true })
+                : Results.NotFound(new { success = false });
+        }).WithName("AdminUpdateMcpProvider");
+
+        mcpGroup.MapDelete("/{id}", async (
+            string id,
+            [FromServices] IAdminMcpProviderService service) =>
+        {
+            var result = await service.DeleteProviderAsync(id);
+            return result
+                ? Results.Ok(new { success = true })
+                : Results.NotFound(new { success = false });
+        }).WithName("AdminDeleteMcpProvider");
+
+        mcpGroup.MapGet("/usage-logs", async (
+            [FromQuery] string? mcpProviderId,
+            [FromQuery] string? userId,
+            [FromQuery] string? toolName,
+            [FromQuery] int page,
+            [FromQuery] int pageSize,
+            [FromServices] IAdminMcpProviderService service) =>
+        {
+            if (page <= 0) page = 1;
+            if (pageSize <= 0) pageSize = 20;
+            if (pageSize > 100) pageSize = 100;
+
+            var filter = new McpUsageLogFilter
+            {
+                McpProviderId = mcpProviderId,
+                UserId = userId,
+                ToolName = toolName,
+                Page = page,
+                PageSize = pageSize
+            };
+            var result = await service.GetUsageLogsAsync(filter);
+            return Results.Ok(new { success = true, data = result });
+        }).WithName("AdminGetMcpUsageLogs");
+
+        return group;
+    }
+}
diff --git a/src/OpenDeepWiki/Endpoints/Admin/AdminStatisticsEndpoints.cs b/src/OpenDeepWiki/Endpoints/Admin/AdminStatisticsEndpoints.cs
index 8c8960f..87c86a2 100644
--- a/src/OpenDeepWiki/Endpoints/Admin/AdminStatisticsEndpoints.cs
+++ b/src/OpenDeepWiki/Endpoints/Admin/AdminStatisticsEndpoints.cs
@@ -37,6 +37,18 @@ public static class AdminStatisticsEndpoints
         .WithName("GetTokenUsageStatistics")
         .WithSummary("获取 Token 消耗统计");
 
+        // 获取 MCP 使用统计
+        statisticsGroup.MapGet("/mcp-usage", async (
+            [FromQuery] int days,
+            [FromServices] IAdminMcpProviderService mcpService) =>
+        {
+            if (days <= 0) days = 7;
+            var result = await mcpService.GetMcpUsageStatisticsAsync(days);
+            return Results.Ok(new { success = true, data = result });
+        })
+        .WithName("GetMcpUsageStatistics")
+        .WithSummary("获取 MCP 使用统计");
+
         return group;
     }
 }
diff --git a/src/OpenDeepWiki/Endpoints/AuthEndpoints.cs b/src/OpenDeepWiki/Endpoints/AuthEndpoints.cs
index c6b4173..3eeab9d 100644
--- a/src/OpenDeepWiki/Endpoints/AuthEndpoints.cs
+++ b/src/OpenDeepWiki/Endpoints/AuthEndpoints.cs
@@ -31,6 +31,7 @@ public static class AuthEndpoints
                 return Results.BadRequest(new { success = false, message = ex.Message });
             }
         })
+        .AllowAnonymous()
         .WithName("Login")
         .WithSummary("用户登录")
         .Produces<LoginResponse>(200)
@@ -54,6 +55,7 @@ public static class AuthEndpoints
                 return Results.BadRequest(new { success = false, message = ex.Message });
             }
         })
+        .AllowAnonymous()
         .WithName("Register")
         .WithSummary("用户注册")
         .Produces<LoginResponse>(200)
diff --git a/src/OpenDeepWiki/Endpoints/McpProviderEndpoints.cs b/src/OpenDeepWiki/Endpoints/McpProviderEndpoints.cs
new file mode 100644
index 0000000..fef11b9
--- /dev/null
+++ b/src/OpenDeepWiki/Endpoints/McpProviderEndpoints.cs
@@ -0,0 +1,44 @@
+using Microsoft.EntityFrameworkCore;
+using OpenDeepWiki.EFCore;
+
+namespace OpenDeepWiki.Endpoints;
+
+/// <summary>
+/// 公开 MCP 提供商端点（无需鉴权）
+/// </summary>
+public static class McpProviderEndpoints
+{
+    private const string RepositoryScopedMcpPathTemplate = "/api/mcp/{owner}/{repo}";
+
+    public static void MapMcpProviderEndpoints(this WebApplication app)
+    {
+        var group = app.MapGroup("/api/mcp-providers")
+            .WithTags("MCP Providers");
+
+        // 获取所有启用的 MCP 提供商（公开，无需登录）
+        group.MapGet("/", async (IContext context) =>
+        {
+            var providers = await context.McpProviders
+                .Where(p => p.IsActive && !p.IsDeleted)
+                .OrderBy(p => p.SortOrder)
+                .ThenBy(p => p.Name)
+                .Select(p => new
+                {
+                    p.Id,
+                    p.Name,
+                    p.Description,
+                    ServerUrl = RepositoryScopedMcpPathTemplate,
+                    p.TransportType,
+                    p.RequiresApiKey,
+                    p.ApiKeyObtainUrl,
+                    p.IconUrl,
+                    p.MaxRequestsPerDay,
+                    p.AllowedTools,
+                })
+                .ToListAsync();
+
+            return Results.Ok(new { success = true, data = providers });
+        }).WithName("GetPublicMcpProviders")
+          .WithSummary("获取公开 MCP 提供商列表");
+    }
+}
diff --git a/src/OpenDeepWiki/Endpoints/WikiExportEndpoints.cs b/src/OpenDeepWiki/Endpoints/WikiExportEndpoints.cs
new file mode 100644
index 0000000..4060865
--- /dev/null
+++ b/src/OpenDeepWiki/Endpoints/WikiExportEndpoints.cs
@@ -0,0 +1,40 @@
+namespace OpenDeepWiki.Endpoints;
+
+using OpenDeepWiki.Services.Wiki;
+using Microsoft.AspNetCore.Mvc;
+
+public static class WikiExportEndpoints
+{
+    public static void MapWikiExportEndpoints(this WebApplication app)
+    {
+        var group = app.MapGroup("/api/v1/wiki")
+            .WithTags("Wiki Export");
+
+        group.MapGet("/{org}/{repo}/export", async (
+            string org, 
+            string repo, 
+            WikiExportService exportService) =>
+        {
+            try
+            {
+                var zipContent = await exportService.ExportWikiAsync(org, repo);
+                var fileName = $"{org}_{repo}_docs.zip";
+                return Results.File(zipContent, "application/zip", fileName);
+            }
+            catch (KeyNotFoundException ex)
+            {
+                return Results.NotFound(new { message = ex.Message });
+            }
+            catch (InvalidOperationException ex)
+            {
+                return Results.BadRequest(new { message = ex.Message });
+            }
+            catch (Exception ex)
+            {
+                return Results.Problem(ex.Message);
+            }
+        })
+        .WithSummary("Export Wiki Documentation")
+        .WithDescription("Generates and downloads a ZIP archive of the repository documentation in Markdown format.");
+    }
+}
diff --git a/src/OpenDeepWiki/Infrastructure/DbInitializer.cs b/src/OpenDeepWiki/Infrastructure/DbInitializer.cs
index 955edb8..f433b45 100644
--- a/src/OpenDeepWiki/Infrastructure/DbInitializer.cs
+++ b/src/OpenDeepWiki/Infrastructure/DbInitializer.cs
@@ -159,4 +159,5 @@ public static class DbInitializer
 
         await context.SaveChangesAsync();
     }
+
 }
diff --git a/src/OpenDeepWiki/MCP/IMcpScopeProvider.cs b/src/OpenDeepWiki/MCP/IMcpScopeProvider.cs
new file mode 100644
index 0000000..00b053b
--- /dev/null
+++ b/src/OpenDeepWiki/MCP/IMcpScopeProvider.cs
@@ -0,0 +1,166 @@
+using System.Text.Json.Nodes;
+using ModelContextProtocol.Protocol;
+using ModelContextProtocol.Server;
+
+namespace OpenDeepWiki.MCP;
+
+public static class McpRepositoryScopeAccessor
+{
+    public const string ScopeKey = "repositoryScope";
+    public const string OwnerKey = "owner";
+    public const string RepoKey = "repo";
+
+    public static void SetScope(McpServer mcpServer, string? owner, string? repo)
+    {
+        ArgumentNullException.ThrowIfNull(mcpServer);
+        SetScope(mcpServer.ServerOptions, owner, repo);
+    }
+
+    public static void SetScope(McpServerOptions options, string? owner, string? repo)
+    {
+        ArgumentNullException.ThrowIfNull(options);
+
+        var experimental = EnsureExperimental(options);
+        if (experimental == null)
+        {
+            return;
+        }
+
+        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
+        {
+            ClearScope(experimental);
+            return;
+        }
+
+        ApplyScope(experimental, owner, repo);
+    }
+
+    public static (string? Owner, string? Repo) GetScope(McpServer mcpServer)
+    {
+        ArgumentNullException.ThrowIfNull(mcpServer);
+        return GetScope(mcpServer.ServerOptions);
+    }
+
+    public static (string? Owner, string? Repo) GetScope(McpServerOptions options)
+    {
+        ArgumentNullException.ThrowIfNull(options);
+
+        var experimental = options.Capabilities?.Experimental;
+        if (experimental == null)
+        {
+            return (null, null);
+        }
+
+        return ExtractScope(experimental);
+    }
+
+    private static object? EnsureExperimental(McpServerOptions options)
+    {
+        options.Capabilities ??= new ServerCapabilities();
+        options.Capabilities.Experimental = new Dictionary<string, object>();
+        return options.Capabilities.Experimental;
+    }
+
+    private static void ApplyScope(object experimental, string owner, string repo)
+    {
+        if (experimental is JsonObject jsonObject)
+        {
+            jsonObject[ScopeKey] = new JsonObject
+            {
+                [OwnerKey] = owner,
+                [RepoKey] = repo
+            };
+            return;
+        }
+
+        if (experimental is IDictionary<string, JsonNode?> jsonNodeMap)
+        {
+            jsonNodeMap[ScopeKey] = new JsonObject
+            {
+                [OwnerKey] = owner,
+                [RepoKey] = repo
+            };
+            return;
+        }
+
+        if (experimental is IDictionary<string, object?> dict)
+        {
+            dict[ScopeKey] = new Dictionary<string, string>
+            {
+                [OwnerKey] = owner,
+                [RepoKey] = repo
+            };
+        }
+    }
+
+    private static void ClearScope(object experimental)
+    {
+        if (experimental is JsonObject jsonObject)
+        {
+            jsonObject.Remove(ScopeKey);
+            return;
+        }
+
+        if (experimental is IDictionary<string, JsonNode?> jsonNodeMap)
+        {
+            jsonNodeMap.Remove(ScopeKey);
+            return;
+        }
+
+        if (experimental is IDictionary<string, object?> dict)
+        {
+            dict.Remove(ScopeKey);
+        }
+    }
+
+    private static (string? Owner, string? Repo) ExtractScope(object experimental)
+    {
+        if (experimental is JsonObject jsonObject)
+        {
+            return ExtractScopeFromJsonNode(jsonObject[ScopeKey]);
+        }
+
+        if (experimental is IDictionary<string, JsonNode?> jsonNodeMap
+            && jsonNodeMap.TryGetValue(ScopeKey, out var node))
+        {
+            return ExtractScopeFromJsonNode(node);
+        }
+
+        if (experimental is IDictionary<string, object?> dict
+            && dict.TryGetValue(ScopeKey, out var value))
+        {
+            if (value is JsonObject innerJson)
+            {
+                return ExtractScopeFromJsonNode(innerJson);
+            }
+
+            if (value is IDictionary<string, object?> innerObject)
+            {
+                innerObject.TryGetValue(OwnerKey, out var ownerObj);
+                innerObject.TryGetValue(RepoKey, out var repoObj);
+                return (ownerObj as string, repoObj as string);
+            }
+
+            if (value is IDictionary<string, string> innerString)
+            {
+                innerString.TryGetValue(OwnerKey, out var owner);
+                innerString.TryGetValue(RepoKey, out var repo);
+                return (owner, repo);
+            }
+        }
+
+        return (null, null);
+    }
+
+    private static (string? Owner, string? Repo) ExtractScopeFromJsonNode(JsonNode? node)
+    {
+        if (node is JsonObject jsonObject)
+        {
+            var owner = jsonObject[OwnerKey]?.GetValue<string>();
+            var repo = jsonObject[RepoKey]?.GetValue<string>();
+            return (owner, repo);
+        }
+
+        return (null, null);
+    }
+}
diff --git a/src/OpenDeepWiki/MCP/McpRepositoryTools.cs b/src/OpenDeepWiki/MCP/McpRepositoryTools.cs
index dca6beb..f8370fa 100644
--- a/src/OpenDeepWiki/MCP/McpRepositoryTools.cs
+++ b/src/OpenDeepWiki/MCP/McpRepositoryTools.cs
@@ -1,291 +1,503 @@
 using System.ComponentModel;
 using System.Text;
 using System.Text.Json;
-using Microsoft.AspNetCore.Http;
+using Microsoft.Agents.AI;
+using Microsoft.Extensions.AI;
+using Microsoft.Extensions.Options;
 using Microsoft.EntityFrameworkCore;
 using ModelContextProtocol.Server;
+using OpenDeepWiki.Agents;
+using OpenDeepWiki.Agents.Tools;
 using OpenDeepWiki.EFCore;
+using OpenDeepWiki.Entities;
+using OpenDeepWiki.Services.Repositories;
 
 namespace OpenDeepWiki.MCP;
 
 /// <summary>
 /// MCP tools that expose repository documentation to AI clients (Claude, Cursor, etc.).
-/// All tools verify user access through department assignments before returning data.
+/// Repository scope is resolved from /api/mcp/{owner}/{repo} via ConfigureSessionOptions.
 /// </summary>
 [McpServerToolType]
 public class McpRepositoryTools
 {
-    [McpServerTool, Description("List all repositories you have access to. Returns repository names, owners, and status.")]
-    public static async Task<string> ListRepositories(
-        IHttpContextAccessor httpContextAccessor,
-        IMcpUserResolver userResolver)
+    [McpServerTool, Description("Search documentation within the current GitHub repository and return summarized insights.")]
+    public static async Task<string> SearchDoc(
+        IContext context,
+        AgentFactory agentFactory,
+        McpServer mcpServer,
+        IOptions<RepositoryAnalyzerOptions> repoOptions,
+        [Description("Search query or question to answer.")] string query,
+        [Description("Maximum number of documents to return (default: 5, max: 20)")] int maxResults = 5,
+        [Description("Language code (default: en)")] string language = "en",
+        CancellationToken cancellationToken = default)
     {
-        var user = await ResolveUserOrThrow(httpContextAccessor, userResolver);
-        var repos = await userResolver.GetAccessibleRepositoriesAsync(user.UserId);
+        var repositoryScopeError = ValidateAndResolveRepositoryScope(mcpServer, out var resolvedOwner, out var resolvedName);
+        if (repositoryScopeError != null)
+            return JsonSerializer.Serialize(new { error = true, message = repositoryScopeError });
 
-        if (repos.Count == 0)
-            return JsonSerializer.Serialize(new { message = "No repositories available. You may not be assigned to any department." });
+        if (string.IsNullOrWhiteSpace(query))
+            return JsonSerializer.Serialize(new { error = true, message = "Search query is required" });
 
-        return JsonSerializer.Serialize(new
-        {
-            count = repos.Count,
-            repositories = repos.Select(r => new
-            {
-                owner = r.Owner,
-                name = r.Name,
-                status = r.Status,
-                department = r.Department
-            })
-        });
-    }
+        if (maxResults <= 0) maxResults = 5;
+        if (maxResults > 20) maxResults = 20;
 
-    [McpServerTool, Description("Get the document catalog (table of contents) for a repository. Use this to discover available documentation paths before reading documents.")]
-    public static async Task<string> GetDocumentCatalog(
-        IHttpContextAccessor httpContextAccessor,
-        IMcpUserResolver userResolver,
-        IContext context,
-        [Description("Repository owner/organization name")] string owner,
-        [Description("Repository name")] string name,
-        [Description("Language code (default: en)")] string language = "en")
-    {
-        var user = await ResolveUserOrThrow(httpContextAccessor, userResolver);
+        var normalizedQuery = query.Trim();
+        if (normalizedQuery.Length == 0)
+            return JsonSerializer.Serialize(new { error = true, message = "Search query is required" });
 
-        if (!await userResolver.CanAccessRepositoryAsync(user.UserId, owner, name))
-            return JsonSerializer.Serialize(new { error = true, message = $"Access denied to {owner}/{name}" });
+        query = normalizedQuery;
+        var loweredQuery = normalizedQuery.ToLowerInvariant();
 
         var repository = await context.Repositories
-            .FirstOrDefaultAsync(r => r.OrgName == owner && r.RepoName == name && !r.IsDeleted);
+            .FirstOrDefaultAsync(r => r.OrgName == resolvedOwner && r.RepoName == resolvedName && !r.IsDeleted, cancellationToken);
 
         if (repository == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"Repository {owner}/{name} not found" });
+            return JsonSerializer.Serialize(new { error = true, message = $"Repository {resolvedOwner}/{resolvedName} not found" });
 
-        // Get default branch
         var branch = await context.RepositoryBranches
-            .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && !b.IsDeleted);
+            .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && !b.IsDeleted, cancellationToken);
 
         if (branch == null)
             return JsonSerializer.Serialize(new { error = true, message = "No branch found for this repository" });
 
         var branchLanguage = await context.BranchLanguages
             .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branch.Id &&
-                                       bl.LanguageCode == language && !bl.IsDeleted);
+                                       bl.LanguageCode == language && !bl.IsDeleted, cancellationToken);
 
         if (branchLanguage == null)
             return JsonSerializer.Serialize(new { error = true, message = $"No documentation in language '{language}'" });
 
-        var catalogs = await context.DocCatalogs
+        var tools = new List<AITool>();
+        var repoPath = BuildRepositoryPath(repoOptions.Value, resolvedOwner!, resolvedName!);
+        if (Directory.Exists(repoPath))
+        {
+            try
+            {
+                var gitTool = new GitTool(repoPath);
+                tools.AddRange(gitTool.GetTools());
+            }
+            catch
+            {
+                // Ignore GitTool init failures; doc search still works without code tools.
+            }
+        }
+
+        var matchingDocs = await context.DocCatalogs
             .Where(c => c.BranchLanguageId == branchLanguage.Id &&
                         !c.IsDeleted && !string.IsNullOrEmpty(c.DocFileId))
-            .OrderBy(c => c.Order)
-            .Select(c => new { c.Title, c.Path, c.Order, c.ParentId })
-            .ToListAsync();
+            .Join(context.DocFiles.Where(d => !d.IsDeleted),
+                  c => c.DocFileId, d => d.Id,
+                  (c, d) => new { Catalog = c, DocFile = d })
+            .Where(x => (!string.IsNullOrEmpty(x.DocFile.Content) &&
+                         x.DocFile.Content.ToLower().Contains(loweredQuery))
+                     || (!string.IsNullOrEmpty(x.Catalog.Title) &&
+                         x.Catalog.Title.ToLower().Contains(loweredQuery)))
+            .Select(x => new
+            {
+                x.Catalog.Title,
+                x.Catalog.Path,
+                x.DocFile.Content
+            })
+            .Take(maxResults)
+            .ToListAsync(cancellationToken);
 
-        if (catalogs.Count == 0)
-            return JsonSerializer.Serialize(new { error = true, message = "No documents available for this repository" });
+        var matches = matchingDocs.Select(doc =>
+        {
+            var lines = doc.Content.Split('\n');
+            var matchLine = -1;
+            for (var i = 0; i < lines.Length; i++)
+            {
+                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
+                {
+                    matchLine = i + 1;
+                    break;
+                }
+            }
+
+            var snippetStart = Math.Max(0, (matchLine > 0 ? matchLine - 1 : 0) - 2);
+            var snippet = string.Join("\n", lines.Skip(snippetStart).Take(5));
+
+            return new DocSearchMatch
+            {
+                Title = doc.Title,
+                Path = doc.Path,
+                MatchLine = matchLine,
+                Snippet = snippet.Length > 500 ? snippet[..500] + "..." : snippet
+            };
+        }).ToList();
+
+        var summary = await BuildSearchSummaryAsync(
+            context,
+            agentFactory,
+            resolvedOwner!,
+            resolvedName!,
+            query,
+            matches,
+            tools,
+            cancellationToken);
+
+        var results = matches.Select(m => new
+        {
+            title = m.Title,
+            path = m.Path,
+            matchLine = m.MatchLine,
+            snippet = m.Snippet
+        });
 
         return JsonSerializer.Serialize(new
         {
-            repository = $"{owner}/{name}",
+            repository = $"{resolvedOwner}/{resolvedName}",
             branch = branch.BranchName,
             language,
-            documentCount = catalogs.Count,
-            documents = catalogs.Select(c => new
-            {
-                title = c.Title,
-                path = c.Path,
-                order = c.Order,
-                hasParent = c.ParentId != null
-            })
+            query,
+            matchCount = matches.Count,
+            results,
+            summary
         });
     }
 
-    [McpServerTool, Description("Read a specific document from repository documentation. Use GetDocumentCatalog first to find available paths.")]
-    public static async Task<string> ReadDocument(
-        IHttpContextAccessor httpContextAccessor,
-        IMcpUserResolver userResolver,
+    [McpServerTool, Description("Get the repository directory structure. Useful for understanding module layout.")]
+    public static async Task<string> GetRepoStructure(
         IContext context,
-        [Description("Repository owner/organization name")] string owner,
-        [Description("Repository name")] string name,
-        [Description("Document path from the catalog")] string path,
-        [Description("Starting line number (1-based, inclusive). Default: 1")] int startLine = 1,
-        [Description("Ending line number (1-based, inclusive). Max 200 lines per request. Default: 200")] int endLine = 200,
-        [Description("Language code (default: en)")] string language = "en")
+        McpServer mcpServer,
+        IOptions<RepositoryAnalyzerOptions> repoOptions,
+        [Description("Optional subdirectory relative to repo root, default is repository root.")] string? path = null,
+        [Description("Maximum depth to traverse (default: 3)")] int maxDepth = 3,
+        [Description("Maximum entries to return (default: 200)")] int maxEntries = 200,
+        CancellationToken cancellationToken = default)
     {
-        var user = await ResolveUserOrThrow(httpContextAccessor, userResolver);
+        var repositoryScopeError = ValidateAndResolveRepositoryScope(mcpServer, out var resolvedOwner, out var resolvedName);
+        if (repositoryScopeError != null)
+            return JsonSerializer.Serialize(new { error = true, message = repositoryScopeError });
 
-        if (!await userResolver.CanAccessRepositoryAsync(user.UserId, owner, name))
-            return JsonSerializer.Serialize(new { error = true, message = $"Access denied to {owner}/{name}" });
-
-        if (startLine < 1) startLine = 1;
-        if (endLine < startLine) endLine = startLine;
-        if (endLine - startLine > 200) endLine = startLine + 200;
+        if (maxDepth <= 0) maxDepth = 1;
+        if (maxEntries <= 0) maxEntries = 200;
 
         var repository = await context.Repositories
-            .FirstOrDefaultAsync(r => r.OrgName == owner && r.RepoName == name && !r.IsDeleted);
+            .FirstOrDefaultAsync(r => r.OrgName == resolvedOwner && r.RepoName == resolvedName && !r.IsDeleted, cancellationToken);
 
         if (repository == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"Repository {owner}/{name} not found" });
-
-        var branch = await context.RepositoryBranches
-            .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && !b.IsDeleted);
+            return JsonSerializer.Serialize(new { error = true, message = $"Repository {resolvedOwner}/{resolvedName} not found" });
 
-        if (branch == null)
-            return JsonSerializer.Serialize(new { error = true, message = "No branch found" });
+        var repoPath = BuildRepositoryPath(repoOptions.Value, resolvedOwner!, resolvedName!);
+        if (!Directory.Exists(repoPath))
+            return JsonSerializer.Serialize(new { error = true, message = "Repository workspace not found on server" });
 
-        var branchLanguage = await context.BranchLanguages
-            .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branch.Id &&
-                                       bl.LanguageCode == language && !bl.IsDeleted);
+        var normalizedPath = NormalizeRelativePath(path);
+        var targetPath = string.IsNullOrEmpty(normalizedPath)
+            ? repoPath
+            : Path.Combine(repoPath, normalizedPath);
 
-        if (branchLanguage == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"No documentation in language '{language}'" });
+        if (!targetPath.StartsWith(repoPath, StringComparison.OrdinalIgnoreCase))
+            return JsonSerializer.Serialize(new { error = true, message = "Invalid path" });
 
-        var catalog = await context.DocCatalogs
-            .FirstOrDefaultAsync(c => c.BranchLanguageId == branchLanguage.Id &&
-                                      c.Path == path && !c.IsDeleted);
+        if (!Directory.Exists(targetPath))
+            return JsonSerializer.Serialize(new { error = true, message = $"Path '{normalizedPath}' does not exist" });
 
-        if (catalog == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"Document '{path}' not found" });
+        var entries = await Task.Run(() => BuildDirectoryTree(targetPath, maxDepth, maxEntries), cancellationToken);
+        var truncated = entries.Count >= maxEntries;
 
-        if (string.IsNullOrEmpty(catalog.DocFileId))
-            return JsonSerializer.Serialize(new { error = true, message = $"Document '{path}' has no content" });
+        return JsonSerializer.Serialize(new
+        {
+            repository = $"{resolvedOwner}/{resolvedName}",
+            root = string.IsNullOrEmpty(normalizedPath) ? "/" : normalizedPath,
+            depth = maxDepth,
+            entryCount = entries.Count,
+            truncated,
+            entries
+        });
+    }
 
-        var docFile = await context.DocFiles
-            .FirstOrDefaultAsync(d => d.Id == catalog.DocFileId && !d.IsDeleted);
+    [McpServerTool, Description("Read a file from the current repository. Returns file content with line numbers.")]
+    public static async Task<string> ReadFile(
+        IContext context,
+        McpServer mcpServer,
+        IOptions<RepositoryAnalyzerOptions> repoOptions,
+        [Description("Relative file path from repository root")] string path,
+        [Description("Line number to start reading from (1-based). Default: 1")] int offset = 1,
+        [Description("Maximum number of lines to read. Default: 2000")] int limit = 2000,
+        CancellationToken cancellationToken = default)
+    {
+        var repositoryScopeError = ValidateAndResolveRepositoryScope(mcpServer, out var resolvedOwner, out var resolvedName);
+        if (repositoryScopeError != null)
+            return JsonSerializer.Serialize(new { error = true, message = repositoryScopeError });
 
-        if (docFile == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"Document content not found" });
+        if (string.IsNullOrWhiteSpace(path))
+            return JsonSerializer.Serialize(new { error = true, message = "File path is required" });
 
-        var allLines = docFile.Content.Split('\n');
-        var totalLines = allLines.Length;
-        var actualEndLine = Math.Min(endLine, totalLines);
+        var repository = await context.Repositories
+            .FirstOrDefaultAsync(r => r.OrgName == resolvedOwner && r.RepoName == resolvedName && !r.IsDeleted, cancellationToken);
 
-        if (startLine > totalLines)
-            return JsonSerializer.Serialize(new { error = true, message = $"startLine ({startLine}) exceeds total lines ({totalLines})" });
+        if (repository == null)
+            return JsonSerializer.Serialize(new { error = true, message = $"Repository {resolvedOwner}/{resolvedName} not found" });
 
-        var selectedLines = allLines.Skip(startLine - 1).Take(actualEndLine - startLine + 1);
-        var content = string.Join("\n", selectedLines);
+        var repoPath = BuildRepositoryPath(repoOptions.Value, resolvedOwner!, resolvedName!);
+        if (!Directory.Exists(repoPath))
+            return JsonSerializer.Serialize(new { error = true, message = "Repository workspace not found on server" });
 
-        List<string>? sourceFiles = null;
-        if (!string.IsNullOrEmpty(docFile.SourceFiles))
-        {
-            try { sourceFiles = JsonSerializer.Deserialize<List<string>>(docFile.SourceFiles); }
-            catch { /* ignore parse failure */ }
-        }
+        var gitTool = new GitTool(repoPath);
+        var content = await gitTool.ReadAsync(path, offset, limit, cancellationToken);
 
         return JsonSerializer.Serialize(new
         {
-            repository = $"{owner}/{name}",
+            repository = $"{resolvedOwner}/{resolvedName}",
             path,
-            title = catalog.Title,
-            content,
-            startLine,
-            endLine = actualEndLine,
-            totalLines,
-            sourceFiles
+            content
         });
     }
 
-    [McpServerTool, Description("Search across all documents in a repository for content matching a query. Returns matching document paths and snippets.")]
-    public static async Task<string> SearchDocuments(
-        IHttpContextAccessor httpContextAccessor,
-        IMcpUserResolver userResolver,
+    private static string? ValidateAndResolveRepositoryScope(
+        McpServer mcpServer,
+        out string? resolvedOwner,
+        out string? resolvedName)
+    {
+        var scope = McpRepositoryScopeAccessor.GetScope(mcpServer);
+        resolvedOwner = scope.Owner;
+        resolvedName = scope.Repo;
+
+        if (string.IsNullOrWhiteSpace(resolvedOwner) || string.IsNullOrWhiteSpace(resolvedName))
+        {
+            return "Repository scope is required. Call MCP via /api/mcp/{owner}/{repo}.";
+        }
+
+        return null;
+    }
+
+    private static async Task<string?> BuildSearchSummaryAsync(
         IContext context,
-        [Description("Repository owner/organization name")] string owner,
-        [Description("Repository name")] string name,
-        [Description("Search query (case-insensitive text search)")] string query,
-        [Description("Language code (default: en)")] string language = "en")
+        AgentFactory agentFactory,
+        string owner,
+        string repo,
+        string query,
+        IReadOnlyList<DocSearchMatch> matches,
+        IReadOnlyList<AITool> tools,
+        CancellationToken cancellationToken)
     {
-        var user = await ResolveUserOrThrow(httpContextAccessor, userResolver);
+        if (matches.Count == 0)
+            return "未找到匹配的文档内容。";
 
-        if (!await userResolver.CanAccessRepositoryAsync(user.UserId, owner, name))
-            return JsonSerializer.Serialize(new { error = true, message = $"Access denied to {owner}/{name}" });
+        var modelConfig = await ResolveMcpModelConfigAsync(context, cancellationToken);
+        if (modelConfig == null)
+            return null;
 
-        if (string.IsNullOrWhiteSpace(query))
-            return JsonSerializer.Serialize(new { error = true, message = "Search query is required" });
+        var requestOptions = new AiRequestOptions
+        {
+            ApiKey = modelConfig.ApiKey,
+            Endpoint = modelConfig.Endpoint,
+            RequestType = ParseRequestType(modelConfig.Provider)
+        };
 
-        var repository = await context.Repositories
-            .FirstOrDefaultAsync(r => r.OrgName == owner && r.RepoName == name && !r.IsDeleted);
+        var agentOptions = new ChatClientAgentOptions
+        {
+            ChatOptions = new ChatOptions
+            {
+                MaxOutputTokens = 12000,
+                ToolMode = ChatToolMode.Auto
+            }
+        };
+
+        var (agent, _) = agentFactory.CreateChatClientWithTools(
+            modelConfig.ModelId,
+            tools.Count == 0 ? Array.Empty<AITool>() : tools.ToArray(),
+            agentOptions,
+            requestOptions);
+
+        var promptBuilder = new StringBuilder();
+        promptBuilder.AppendLine($"Repository: {owner}/{repo}");
+        promptBuilder.AppendLine($"User Question: {query}");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("Search Results:");
+        foreach (var match in matches)
+        {
+            promptBuilder.AppendLine($"- {match.Title} ({match.Path})");
+            if (!string.IsNullOrWhiteSpace(match.Snippet))
+            {
+                promptBuilder.AppendLine($"  Snippet: {match.Snippet}");
+            }
+        }
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("=== INSTRUCTIONS ===");
+        promptBuilder.AppendLine("You are an expert repository documentation assistant. Analyze the search results above and provide a comprehensive, well-structured answer to the user's question.");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("Your response MUST include the following sections:");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("1. **Executive Summary** (2-3 sentences)");
+        promptBuilder.AppendLine("   - Provide a concise, direct answer to the user's question.");
+        promptBuilder.AppendLine("   - Highlight the most critical information or conclusion.");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("2. **Detailed Explanation**");
+        promptBuilder.AppendLine("   - Break down key concepts, configurations, or implementation steps in separate paragraphs.");
+        promptBuilder.AppendLine("   - Reference specific document paths from the search results when applicable (e.g., 'As documented in [path]...').");
+        promptBuilder.AppendLine("   - Include code examples, configuration snippets, or command-line instructions if relevant.");
+        promptBuilder.AppendLine("   - Explain the rationale behind recommendations or design decisions.");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("3. **Recommended Next Steps**");
+        promptBuilder.AppendLine("   - Suggest specific documents or sections the user should read for deeper understanding.");
+        promptBuilder.AppendLine("   - Provide actionable follow-up tasks or verification steps.");
+        promptBuilder.AppendLine("   - If information is incomplete, clearly state what's missing and suggest troubleshooting approaches.");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("=== GUIDELINES ===");
+        promptBuilder.AppendLine("- Use clear, professional technical writing suitable for developers.");
+        promptBuilder.AppendLine("- Maintain consistency with the project's terminology and conventions.");
+        promptBuilder.AppendLine("- Be thorough but avoid unnecessary verbosity.");
+        promptBuilder.AppendLine("- If the search results don't fully answer the question, acknowledge the gaps and explain what additional context would help.");
+        promptBuilder.AppendLine("- When referencing code or configuration, use proper formatting (code blocks for multi-line, inline for single-line).");
+        promptBuilder.AppendLine("- If multiple search results are relevant, synthesize information from all of them rather than treating them separately.");
+        promptBuilder.AppendLine("- Consider the language context: if documentation is in a specific language, maintain consistency in terminology.");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("=== RESPONSE FORMAT ===");
+        promptBuilder.AppendLine("Provide your answer in the following format:");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("## Executive Summary");
+        promptBuilder.AppendLine("[Your concise summary here]");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("## Detailed Explanation");
+        promptBuilder.AppendLine("[Your detailed explanation here]");
+        promptBuilder.AppendLine();
+        promptBuilder.AppendLine("## Recommended Next Steps");
+        promptBuilder.AppendLine("[Your recommendations here]");
+
+        var messages = new List<ChatMessage>
+        {
+            new(ChatRole.System, "You are an expert repository documentation assistant for developers. Your role is to provide clear, comprehensive, and actionable answers based on repository documentation search results. Prioritize accuracy, completeness, and practical guidance. Structure your responses professionally and cite relevant documentation paths when applicable."),
+            new(ChatRole.User, promptBuilder.ToString())
+        };
 
-        if (repository == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"Repository {owner}/{name} not found" });
+        var thread = await agent.CreateSessionAsync(cancellationToken);
+        var summaryBuilder = new StringBuilder();
+        await foreach (var update in agent.RunStreamingAsync(messages, thread, cancellationToken: cancellationToken))
+        {
+            if (!string.IsNullOrEmpty(update.Text))
+            {
+                summaryBuilder.Append(update.Text);
+            }
+        }
 
-        var branch = await context.RepositoryBranches
-            .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && !b.IsDeleted);
+        return summaryBuilder.ToString().Trim();
+    }
 
-        if (branch == null)
-            return JsonSerializer.Serialize(new { error = true, message = "No branch found" });
+    private static async Task<ModelConfig?> ResolveMcpModelConfigAsync(
+        IContext context,
+        CancellationToken cancellationToken)
+    {
+        var providerModelId = await context.McpProviders
+            .Where(p => p.IsActive && !p.IsDeleted && !string.IsNullOrEmpty(p.ModelConfigId))
+            .OrderBy(p => p.SortOrder)
+            .Select(p => p.ModelConfigId)
+            .FirstOrDefaultAsync(cancellationToken);
 
-        var branchLanguage = await context.BranchLanguages
-            .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branch.Id &&
-                                       bl.LanguageCode == language && !bl.IsDeleted);
+        if (!string.IsNullOrEmpty(providerModelId))
+        {
+            var providerModel = await context.ModelConfigs
+                .FirstOrDefaultAsync(m => m.Id == providerModelId && m.IsActive && !m.IsDeleted, cancellationToken);
+            if (providerModel != null) return providerModel;
+        }
 
-        if (branchLanguage == null)
-            return JsonSerializer.Serialize(new { error = true, message = $"No documentation in language '{language}'" });
+        return await context.ModelConfigs
+            .Where(m => m.IsActive && !m.IsDeleted)
+            .OrderByDescending(m => m.IsDefault)
+            .ThenByDescending(m => m.CreatedAt)
+            .FirstOrDefaultAsync(cancellationToken);
+    }
 
-        // Search across all doc files for this branch/language
-        var matchingDocs = await context.DocCatalogs
-            .Where(c => c.BranchLanguageId == branchLanguage.Id &&
-                        !c.IsDeleted && !string.IsNullOrEmpty(c.DocFileId))
-            .Join(context.DocFiles.Where(d => !d.IsDeleted),
-                  c => c.DocFileId, d => d.Id,
-                  (c, d) => new { Catalog = c, DocFile = d })
-            .Where(x => x.DocFile.Content.Contains(query, StringComparison.OrdinalIgnoreCase)
-                     || x.Catalog.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
-            .Select(x => new
-            {
-                x.Catalog.Title,
-                x.Catalog.Path,
-                x.DocFile.Content
-            })
-            .Take(10)
-            .ToListAsync();
+    private static AiRequestType ParseRequestType(string? provider)
+    {
+        return provider?.ToLowerInvariant() switch
+        {
+            "openai" => AiRequestType.OpenAI,
+            "openairesponses" => AiRequestType.OpenAIResponses,
+            "anthropic" => AiRequestType.Anthropic,
+            "azureopenai" => AiRequestType.AzureOpenAI,
+            _ => AiRequestType.OpenAI
+        };
+    }
+
+    private static string BuildRepositoryPath(RepositoryAnalyzerOptions options, string owner, string repo)
+    {
+        var safeOwner = SanitizePathComponent(owner);
+        var safeRepo = SanitizePathComponent(repo);
+        return Path.Combine(options.RepositoriesDirectory, safeOwner, safeRepo, "tree");
+    }
+
+    private static string SanitizePathComponent(string component)
+    {
+        var sanitized = component
+            .Replace('/', '_')
+            .Replace('\\', '_')
+            .Replace("..", "_")
+            .Trim();
+
+        return string.IsNullOrWhiteSpace(sanitized) ? "_" : sanitized;
+    }
+
+    private static string NormalizeRelativePath(string? path)
+    {
+        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
+        var normalized = path.Replace('\\', '/').Trim('/');
+        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries)
+            .Where(p => p != "." && p != "..");
+        return string.Join('/', parts);
+    }
+
+    private static List<string> BuildDirectoryTree(string rootPath, int maxDepth, int maxEntries)
+    {
+        var entries = new List<string>();
+        TraverseDirectory(rootPath, 0, maxDepth, maxEntries, entries, string.Empty);
+        return entries;
+    }
 
-        var results = matchingDocs.Select(doc =>
+    private static void TraverseDirectory(
+        string currentPath,
+        int depth,
+        int maxDepth,
+        int maxEntries,
+        List<string> entries,
+        string indent)
+    {
+        if (entries.Count >= maxEntries) return;
+
+        var directories = Directory.EnumerateDirectories(currentPath)
+            .Where(d => !IsHiddenEntry(d))
+            .OrderBy(d => d, StringComparer.OrdinalIgnoreCase)
+            .ToList();
+
+        foreach (var dir in directories)
         {
-            // Find first matching line for snippet
-            var lines = doc.Content.Split('\n');
-            var matchLine = -1;
-            for (var i = 0; i < lines.Length; i++)
+            if (entries.Count >= maxEntries) return;
+            var name = Path.GetFileName(dir) + "/";
+            entries.Add($"{indent}{name}");
+            if (depth + 1 < maxDepth)
             {
-                if (lines[i].Contains(query, StringComparison.OrdinalIgnoreCase))
-                {
-                    matchLine = i + 1;
-                    break;
-                }
+                TraverseDirectory(dir, depth + 1, maxDepth, maxEntries, entries, indent + "  ");
             }
+        }
 
-            var snippetStart = Math.Max(0, (matchLine > 0 ? matchLine - 1 : 0) - 2);
-            var snippet = string.Join("\n", lines.Skip(snippetStart).Take(5));
-
-            return new
-            {
-                title = doc.Title,
-                path = doc.Path,
-                matchLine,
-                snippet = snippet.Length > 500 ? snippet[..500] + "..." : snippet
-            };
-        });
+        var files = Directory.EnumerateFiles(currentPath)
+            .Where(f => !IsHiddenEntry(f))
+            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
+            .ToList();
 
-        return JsonSerializer.Serialize(new
+        foreach (var file in files)
         {
-            repository = $"{owner}/{name}",
-            query,
-            matchCount = matchingDocs.Count,
-            results
-        });
+            if (entries.Count >= maxEntries) return;
+            var name = Path.GetFileName(file);
+            entries.Add($"{indent}{name}");
+        }
     }
 
-    private static async Task<McpUserInfo> ResolveUserOrThrow(
-        IHttpContextAccessor httpContextAccessor, IMcpUserResolver userResolver)
+    private static bool IsHiddenEntry(string path)
     {
-        var principal = httpContextAccessor.HttpContext?.User;
-        if (principal == null)
-            throw new UnauthorizedAccessException("No authentication context available");
-
-        var user = await userResolver.ResolveUserAsync(principal);
-        if (user == null)
-            throw new UnauthorizedAccessException("User not found in DeepWiki. Please ensure your Google account is registered.");
+        var name = Path.GetFileName(path);
+        return name.StartsWith(".", StringComparison.Ordinal) ||
+               string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase);
+    }
 
-        return user;
+    private sealed class DocSearchMatch
+    {
+        public string Title { get; init; } = string.Empty;
+        public string Path { get; init; } = string.Empty;
+        public int MatchLine { get; init; }
+        public string Snippet { get; init; } = string.Empty;
     }
 }
diff --git a/src/OpenDeepWiki/MCP/McpStatisticsAggregationService.cs b/src/OpenDeepWiki/MCP/McpStatisticsAggregationService.cs
new file mode 100644
index 0000000..2477219
--- /dev/null
+++ b/src/OpenDeepWiki/MCP/McpStatisticsAggregationService.cs
@@ -0,0 +1,50 @@
+using OpenDeepWiki.Services.Mcp;
+
+namespace OpenDeepWiki.MCP;
+
+/// <summary>
+/// MCP 统计聚合后台服务
+/// 每小时聚合前一天的使用日志到 McpDailyStatistics
+/// </summary>
+public class McpStatisticsAggregationService : BackgroundService
+{
+    private readonly IServiceScopeFactory _scopeFactory;
+    private readonly ILogger<McpStatisticsAggregationService> _logger;
+    private static readonly TimeSpan AggregationInterval = TimeSpan.FromHours(1);
+
+    public McpStatisticsAggregationService(
+        IServiceScopeFactory scopeFactory,
+        ILogger<McpStatisticsAggregationService> logger)
+    {
+        _scopeFactory = scopeFactory;
+        _logger = logger;
+    }
+
+    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
+    {
+        // Wait a bit before first run to let the app start up
+        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
+
+        while (!stoppingToken.IsCancellationRequested)
+        {
+            try
+            {
+                using var scope = _scopeFactory.CreateScope();
+                var logService = scope.ServiceProvider.GetRequiredService<IMcpUsageLogService>();
+
+                // Aggregate today and yesterday
+                var today = DateTime.UtcNow.Date;
+                await logService.AggregateDailyStatisticsAsync(today);
+                await logService.AggregateDailyStatisticsAsync(today.AddDays(-1));
+
+                _logger.LogDebug("MCP 统计聚合完成");
+            }
+            catch (Exception ex)
+            {
+                _logger.LogError(ex, "MCP 统计聚合服务异常");
+            }
+
+            await Task.Delay(AggregationInterval, stoppingToken);
+        }
+    }
+}
diff --git a/src/OpenDeepWiki/MCP/McpUsageLoggingMiddleware.cs b/src/OpenDeepWiki/MCP/McpUsageLoggingMiddleware.cs
new file mode 100644
index 0000000..7fe2ce3
--- /dev/null
+++ b/src/OpenDeepWiki/MCP/McpUsageLoggingMiddleware.cs
@@ -0,0 +1,104 @@
+using System.Diagnostics;
+using System.Security.Claims;
+using System.Text.Json;
+using OpenDeepWiki.Entities;
+using OpenDeepWiki.Services.Mcp;
+
+namespace OpenDeepWiki.MCP;
+
+/// <summary>
+/// MCP 请求使用日志中间件
+/// 拦截 /api/mcp 路径的请求，记录工具调用、耗时、状态码
+/// </summary>
+public class McpUsageLoggingMiddleware
+{
+    private readonly RequestDelegate _next;
+    private readonly string _path;
+
+    public McpUsageLoggingMiddleware(RequestDelegate next, string path)
+    {
+        _next = next;
+        _path = path;
+    }
+
+    public async Task InvokeAsync(HttpContext context)
+    {
+        if (!context.Request.Path.StartsWithSegments(_path))
+        {
+            await _next(context);
+            return;
+        }
+
+        var stopwatch = Stopwatch.StartNew();
+        var originalStatusCode = context.Response.StatusCode;
+
+        try
+        {
+            await _next(context);
+        }
+        finally
+        {
+            stopwatch.Stop();
+
+            // Fire-and-forget logging
+            var logService = context.RequestServices.GetService<IMcpUsageLogService>();
+            if (logService != null)
+            {
+                var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
+                             ?? context.User?.FindFirstValue("sub");
+                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
+                var userAgent = context.Request.Headers.UserAgent.FirstOrDefault();
+
+                // Try to extract tool name from request body (MCP JSON-RPC)
+                var toolName = ExtractToolName(context);
+
+                var log = new McpUsageLog
+                {
+                    UserId = userId,
+                    ToolName = toolName ?? "unknown",
+                    ResponseStatus = context.Response.StatusCode,
+                    DurationMs = stopwatch.ElapsedMilliseconds,
+                    IpAddress = ipAddress,
+                    UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent
+                };
+
+                // Don't await - fire and forget
+                _ = Task.Run(async () =>
+                {
+                    try
+                    {
+                        await logService.LogUsageAsync(log);
+                    }
+                    catch
+                    {
+                        // Silently ignore logging failures
+                    }
+                });
+            }
+        }
+    }
+
+    /// <summary>
+    /// 尝试从 MCP JSON-RPC 请求中提取工具名称
+    /// </summary>
+    private static string? ExtractToolName(HttpContext context)
+    {
+        // MCP uses JSON-RPC, tool calls have method "tools/call" with params.name
+        // We store the tool name in HttpContext.Items during request processing if available
+        if (context.Items.TryGetValue("McpToolName", out var toolNameObj) && toolNameObj is string toolName)
+        {
+            return toolName;
+        }
+
+        // Fallback: use the request method + path segment
+        return context.Request.Method + " " + context.Request.Path.Value;
+    }
+}
+
+public static class McpUsageLoggingExtensions
+{
+    public static IApplicationBuilder UseMcpUsageLogging(this IApplicationBuilder app, string path)
+    {
+        return app.UseMiddleware<McpUsageLoggingMiddleware>(path);
+    }
+}
diff --git a/src/OpenDeepWiki/Models/Admin/McpProviderModels.cs b/src/OpenDeepWiki/Models/Admin/McpProviderModels.cs
new file mode 100644
index 0000000..82faa48
--- /dev/null
+++ b/src/OpenDeepWiki/Models/Admin/McpProviderModels.cs
@@ -0,0 +1,112 @@
+namespace OpenDeepWiki.Models.Admin;
+
+/// <summary>
+/// MCP 提供商创建/更新请求
+/// </summary>
+public class McpProviderRequest
+{
+    public string Name { get; set; } = string.Empty;
+    public string? Description { get; set; }
+    public string ServerUrl { get; set; } = string.Empty;
+    public string TransportType { get; set; } = "streamable_http";
+    public bool RequiresApiKey { get; set; } = true;
+    public string? ApiKeyObtainUrl { get; set; }
+    public string? SystemApiKey { get; set; }
+    public string? ModelConfigId { get; set; }
+    public bool IsActive { get; set; } = true;
+    public int SortOrder { get; set; }
+    public string? IconUrl { get; set; }
+    public int MaxRequestsPerDay { get; set; }
+}
+
+/// <summary>
+/// MCP 提供商 DTO
+/// </summary>
+public class McpProviderDto
+{
+    public string Id { get; set; } = string.Empty;
+    public string Name { get; set; } = string.Empty;
+    public string? Description { get; set; }
+    public string ServerUrl { get; set; } = string.Empty;
+    public string TransportType { get; set; } = "streamable_http";
+    public bool RequiresApiKey { get; set; }
+    public string? ApiKeyObtainUrl { get; set; }
+    public bool HasSystemApiKey { get; set; }
+    public string? ModelConfigId { get; set; }
+    public string? ModelConfigName { get; set; }
+    public bool IsActive { get; set; }
+    public int SortOrder { get; set; }
+    public string? IconUrl { get; set; }
+    public int MaxRequestsPerDay { get; set; }
+    public DateTime CreatedAt { get; set; }
+}
+
+/// <summary>
+/// MCP 使用日志 DTO
+/// </summary>
+public class McpUsageLogDto
+{
+    public string Id { get; set; } = string.Empty;
+    public string? UserId { get; set; }
+    public string? UserName { get; set; }
+    public string? McpProviderId { get; set; }
+    public string? McpProviderName { get; set; }
+    public string ToolName { get; set; } = string.Empty;
+    public string? RequestSummary { get; set; }
+    public int ResponseStatus { get; set; }
+    public long DurationMs { get; set; }
+    public int InputTokens { get; set; }
+    public int OutputTokens { get; set; }
+    public string? IpAddress { get; set; }
+    public string? ErrorMessage { get; set; }
+    public DateTime CreatedAt { get; set; }
+}
+
+/// <summary>
+/// MCP 使用日志查询过滤器
+/// </summary>
+public class McpUsageLogFilter
+{
+    public string? McpProviderId { get; set; }
+    public string? UserId { get; set; }
+    public string? ToolName { get; set; }
+    public int Page { get; set; } = 1;
+    public int PageSize { get; set; } = 20;
+}
+
+/// <summary>
+/// 分页结果
+/// </summary>
+public class PagedResult<T>
+{
+    public List<T> Items { get; set; } = new();
+    public int Total { get; set; }
+    public int Page { get; set; }
+    public int PageSize { get; set; }
+}
+
+/// <summary>
+/// MCP 使用统计响应
+/// </summary>
+public class McpUsageStatisticsResponse
+{
+    public List<McpDailyUsage> DailyUsages { get; set; } = new();
+    public long TotalRequests { get; set; }
+    public long TotalSuccessful { get; set; }
+    public long TotalErrors { get; set; }
+    public long TotalInputTokens { get; set; }
+    public long TotalOutputTokens { get; set; }
+}
+
+/// <summary>
+/// MCP 每日使用量
+/// </summary>
+public class McpDailyUsage
+{
+    public DateTime Date { get; set; }
+    public long RequestCount { get; set; }
+    public long SuccessCount { get; set; }
+    public long ErrorCount { get; set; }
+    public long InputTokens { get; set; }
+    public long OutputTokens { get; set; }
+}
diff --git a/src/OpenDeepWiki/OpenDeepWiki.csproj b/src/OpenDeepWiki/OpenDeepWiki.csproj
index 9502772..6798cc2 100644
--- a/src/OpenDeepWiki/OpenDeepWiki.csproj
+++ b/src/OpenDeepWiki/OpenDeepWiki.csproj
@@ -55,6 +55,7 @@
       <Content Include="..\..\.dockerignore">
         <Link>.dockerignore</Link>
       </Content>
+      <Content Include="prompts\**\*" CopyToOutputDirectory="PreserveNewest" />
       <Content Include="skills\**\*" CopyToOutputDirectory="PreserveNewest" />
     </ItemGroup>
 
diff --git a/src/OpenDeepWiki/Program.cs b/src/OpenDeepWiki/Program.cs
index f22e89c..d0a87b0 100644
--- a/src/OpenDeepWiki/Program.cs
+++ b/src/OpenDeepWiki/Program.cs
@@ -20,6 +20,7 @@ using OpenDeepWiki.Services.Prompts;
 using OpenDeepWiki.Services.Recommendation;
 using OpenDeepWiki.Services.Repositories;
 using OpenDeepWiki.Services.Translation;
+using OpenDeepWiki.Services.Mcp;
 using OpenDeepWiki.Services.UserProfile;
 using OpenDeepWiki.Services.Wiki;
 using Scalar.AspNetCore;
@@ -92,7 +93,7 @@ try
             ?? "OpenDeepWiki-Default-Secret-Key-Please-Change-In-Production-Environment-2024";
     }
 
-    var authBuilder = builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
+    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
         .AddJwtBearer(options =>
         {
             options.TokenValidationParameters = new TokenValidationParameters
@@ -108,24 +109,9 @@ try
             };
         });
 
-    // MCP Google auth scheme (only when GOOGLE_CLIENT_ID is configured)
-    var hasMcpAuth = !string.IsNullOrEmpty(builder.Configuration["GOOGLE_CLIENT_ID"]);
-    if (hasMcpAuth)
-    {
-        authBuilder.AddMcpGoogleAuth(builder.Configuration);
-    }
-
     builder.Services.AddAuthorization(options =>
     {
         options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
-        if (hasMcpAuth)
-        {
-            options.AddPolicy(McpAuthConfiguration.McpPolicyName, policy =>
-                policy.AddAuthenticationSchemes(
-                        JwtBearerDefaults.AuthenticationScheme,
-                        McpAuthConfiguration.McpGoogleScheme)
-                    .RequireAuthenticatedUser());
-        }
     });
 
     // 注册认证服务
@@ -296,6 +282,9 @@ try
     // 注册 Wiki Generator
     builder.Services.AddScoped<IWikiGenerator, WikiGenerator>();
 
+    // 注册 Wiki Export Service
+    builder.Services.AddScoped<WikiExportService>();
+
     // 注册缓存框架（默认内存实现）
     builder.Services.AddOpenDeepWikiCache();
 
@@ -373,39 +362,55 @@ try
     // Requirements: 13.5, 13.6, 14.2, 14.7, 17.1, 17.2, 17.4 - 嵌入脚本验证和对话
     builder.Services.AddScoped<IEmbedService, EmbedService>();
 
-    // MCP server registration (requires GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET)
-    var mcpEnabled = !string.IsNullOrEmpty(builder.Configuration["GOOGLE_CLIENT_ID"])
-                     && !string.IsNullOrEmpty(builder.Configuration["GOOGLE_CLIENT_SECRET"]);
+    // 注册 MCP 提供商管理服务
+    builder.Services.AddScoped<IAdminMcpProviderService, AdminMcpProviderService>();
+    builder.Services.AddScoped<IMcpUsageLogService, McpUsageLogService>();
+    builder.Services.AddHostedService<McpStatisticsAggregationService>();
+
+    // MCP server registration (official MCP server + scope via ConfigureSessionOptions)
+    var mcpEnabled = builder.Configuration.GetValue("MCP_ENABLED", true);
     if (mcpEnabled)
     {
-        builder.Services.AddScoped<IMcpUserResolver, McpUserResolver>();
-        builder.Services.AddSingleton<McpOAuthServer>();
-        builder.Services.AddHostedService<McpOAuthCleanupService>();
         builder.Services.AddMcpServer()
-            .WithHttpTransport()
+            .WithHttpTransport(options =>
+            {
+                options.ConfigureSessionOptions = (httpContext, mcpServer, _) =>
+                {
+                    var segments = httpContext.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries)
+                                   ?? Array.Empty<string>();
+                    string? owner = null;
+                    string? repo = null;
+                    if (segments.Length >= 4
+                        && string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase)
+                        && string.Equals(segments[1], "mcp", StringComparison.OrdinalIgnoreCase))
+                    {
+                        owner = Uri.UnescapeDataString(segments[2]);
+                        repo = Uri.UnescapeDataString(segments[3]);
+                    }
+
+                    McpRepositoryScopeAccessor.SetScope(mcpServer, owner, repo);
+                    return Task.CompletedTask;
+                };
+            })
             .WithTools<McpRepositoryTools>();
     }
 
     var app = builder.Build();
 
-    // 初始化数据库
-    await DbInitializer.InitializeAsync(app.Services);
-
-    // 应用数据库中的系统设置到配置（覆盖环境变量和appsettings.json的值）
-    using (var scope = app.Services.CreateScope())
-    {
-        var settingsService = scope.ServiceProvider.GetRequiredService<IAdminSettingsService>();
-        var wikiOptions = scope.ServiceProvider.GetRequiredService<IOptions<WikiGeneratorOptions>>();
-        await SystemSettingDefaults.ApplyToWikiGeneratorOptions(wikiOptions.Value, settingsService);
-    }
-
-    // 启用 CORS
+    // Configure the HTTP request pipeline.
     app.UseCors("AllowAll");
 
-    // Add Serilog request logging
-    app.UseSerilogLogging();
+    // 添加静态文件支持（用于 Skill assets 等）
+    app.UseStaticFiles();
+
+    // Configure forwarded headers for reverse proxy scenarios
+    app.UseForwardedHeaders(new ForwardedHeadersOptions
+    {
+        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
+                           | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
+                           | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
+    });
 
-    // Configure the HTTP request pipeline.
     if (app.Environment.IsDevelopment())
     {
         app.MapOpenApi();
@@ -415,13 +420,13 @@ try
     app.UseAuthentication();
     app.UseAuthorization();
 
-    // MCP server endpoints (only when fully configured with OAuth)
+    // MCP server endpoints (official MCP server + scope via ConfigureSessionOptions)
     if (mcpEnabled)
     {
-        app.MapMcpOAuthEndpoints();
+        app.UseMcpUsageLogging("/api/mcp");
         app.UseSseKeepAlive("/api/mcp");
-        app.MapProtectedResourceMetadata();
-        app.MapMcp("/api/mcp").RequireAuthorization(McpAuthConfiguration.McpPolicyName);
+        app.MapMcp("/api/mcp");
+        app.MapMcp("/api/mcp/{owner}/{repo}");
     }
 
     app.MapMiniApis();
@@ -434,7 +439,16 @@ try
     app.MapEmbedEndpoints();
 
     app.MapSystemEndpoints();
+    app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).ExcludeFromDescription();
     app.MapIncrementalUpdateEndpoints();
+    app.MapMcpProviderEndpoints();
+    app.MapWikiExportEndpoints();
+
+    // 初始化数据库（创建默认数据）
+    using (var scope = app.Services.CreateScope())
+    {
+        await DbInitializer.InitializeAsync(scope.ServiceProvider);
+    }
 
     app.Run();
 }
diff --git a/src/OpenDeepWiki/Services/Admin/AdminMcpProviderService.cs b/src/OpenDeepWiki/Services/Admin/AdminMcpProviderService.cs
new file mode 100644
index 0000000..c664d17
--- /dev/null
+++ b/src/OpenDeepWiki/Services/Admin/AdminMcpProviderService.cs
@@ -0,0 +1,256 @@
+using Microsoft.EntityFrameworkCore;
+using OpenDeepWiki.EFCore;
+using OpenDeepWiki.Entities;
+using OpenDeepWiki.Models.Admin;
+
+namespace OpenDeepWiki.Services.Admin;
+
+/// <summary>
+/// 管理员 MCP 提供商服务实现
+/// </summary>
+public class AdminMcpProviderService : IAdminMcpProviderService
+{
+    private const string RepositoryScopedMcpPathTemplate = "/api/mcp/{owner}/{repo}";
+
+    private readonly IContext _context;
+    private readonly ILogger<AdminMcpProviderService> _logger;
+
+    public AdminMcpProviderService(IContext context, ILogger<AdminMcpProviderService> logger)
+    {
+        _context = context;
+        _logger = logger;
+    }
+
+    public async Task<List<McpProviderDto>> GetProvidersAsync()
+    {
+        var providers = await _context.McpProviders
+            .Where(p => !p.IsDeleted)
+            .OrderBy(p => p.SortOrder)
+            .ThenBy(p => p.Name)
+            .ToListAsync();
+
+        var modelIds = providers
+            .Where(p => !string.IsNullOrEmpty(p.ModelConfigId))
+            .Select(p => p.ModelConfigId!)
+            .Distinct()
+            .ToList();
+
+        var modelNames = modelIds.Count > 0
+            ? await _context.ModelConfigs
+                .Where(m => modelIds.Contains(m.Id) && !m.IsDeleted)
+                .ToDictionaryAsync(m => m.Id, m => m.Name)
+            : new Dictionary<string, string>();
+
+        return providers.Select(p => new McpProviderDto
+        {
+            Id = p.Id,
+            Name = p.Name,
+            Description = p.Description,
+            ServerUrl = RepositoryScopedMcpPathTemplate,
+            TransportType = p.TransportType,
+            RequiresApiKey = p.RequiresApiKey,
+            ApiKeyObtainUrl = p.ApiKeyObtainUrl,
+            HasSystemApiKey = !string.IsNullOrEmpty(p.SystemApiKey),
+            ModelConfigId = p.ModelConfigId,
+            ModelConfigName = p.ModelConfigId != null && modelNames.TryGetValue(p.ModelConfigId, out var name) ? name : null,
+            IsActive = p.IsActive,
+            MaxRequestsPerDay = p.MaxRequestsPerDay,
+            CreatedAt = p.CreatedAt
+        }).ToList();
+    }
+
+    public async Task<McpProviderDto> CreateProviderAsync(McpProviderRequest request)
+    {
+        var provider = new McpProvider
+        {
+            Id = Guid.NewGuid().ToString(),
+            Name = request.Name,
+            Description = request.Description,
+            ServerUrl = RepositoryScopedMcpPathTemplate,
+            TransportType = request.TransportType,
+            RequiresApiKey = request.RequiresApiKey,
+            ApiKeyObtainUrl = request.ApiKeyObtainUrl,
+            SystemApiKey = request.SystemApiKey,
+            ModelConfigId = request.ModelConfigId,
+            IsActive = request.IsActive,
+            MaxRequestsPerDay = request.MaxRequestsPerDay,
+            CreatedAt = DateTime.UtcNow
+        };
+
+        _context.McpProviders.Add(provider);
+        await _context.SaveChangesAsync();
+
+        _logger.LogInformation("MCP 提供商已创建: {Name} ({Id})", provider.Name, provider.Id);
+
+        string? modelName = null;
+        if (!string.IsNullOrEmpty(provider.ModelConfigId))
+        {
+            modelName = await _context.ModelConfigs
+                .Where(m => m.Id == provider.ModelConfigId && !m.IsDeleted)
+                .Select(m => m.Name)
+                .FirstOrDefaultAsync();
+        }
+
+        return new McpProviderDto
+        {
+            Id = provider.Id,
+            Name = provider.Name,
+            Description = provider.Description,
+            ServerUrl = RepositoryScopedMcpPathTemplate,
+            TransportType = provider.TransportType,
+            RequiresApiKey = provider.RequiresApiKey,
+            ApiKeyObtainUrl = provider.ApiKeyObtainUrl,
+            HasSystemApiKey = !string.IsNullOrEmpty(provider.SystemApiKey),
+            ModelConfigId = provider.ModelConfigId,
+            ModelConfigName = modelName,
+            IsActive = provider.IsActive,
+            MaxRequestsPerDay = provider.MaxRequestsPerDay,
+            CreatedAt = provider.CreatedAt
+        };
+    }
+
+    public async Task<bool> UpdateProviderAsync(string id, McpProviderRequest request)
+    {
+        var provider = await _context.McpProviders
+            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
+
+        if (provider == null) return false;
+
+        provider.Name = request.Name;
+        provider.Description = request.Description;
+        provider.ServerUrl = RepositoryScopedMcpPathTemplate;
+        provider.TransportType = request.TransportType;
+        provider.RequiresApiKey = request.RequiresApiKey;
+        provider.ApiKeyObtainUrl = request.ApiKeyObtainUrl;
+        if (request.SystemApiKey != null) provider.SystemApiKey = request.SystemApiKey;
+        provider.ModelConfigId = request.ModelConfigId;
+        provider.IsActive = request.IsActive;
+        provider.MaxRequestsPerDay = request.MaxRequestsPerDay;
+        provider.UpdatedAt = DateTime.UtcNow;
+
+        await _context.SaveChangesAsync();
+
+        _logger.LogInformation("MCP 提供商已更新: {Name} ({Id})", provider.Name, provider.Id);
+        return true;
+    }
+
+    public async Task<bool> DeleteProviderAsync(string id)
+    {
+        var provider = await _context.McpProviders
+            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
+
+        if (provider == null) return false;
+
+        provider.IsDeleted = true;
+        provider.UpdatedAt = DateTime.UtcNow;
+        await _context.SaveChangesAsync();
+
+        _logger.LogInformation("MCP 提供商已删除: {Name} ({Id})", provider.Name, provider.Id);
+        return true;
+    }
+
+    public async Task<Models.Admin.PagedResult<McpUsageLogDto>> GetUsageLogsAsync(McpUsageLogFilter filter)
+    {
+        var query = _context.McpUsageLogs
+            .Where(l => !l.IsDeleted)
+            .AsQueryable();
+
+        if (!string.IsNullOrEmpty(filter.McpProviderId))
+            query = query.Where(l => l.McpProviderId == filter.McpProviderId);
+        if (!string.IsNullOrEmpty(filter.UserId))
+            query = query.Where(l => l.UserId == filter.UserId);
+        if (!string.IsNullOrEmpty(filter.ToolName))
+            query = query.Where(l => l.ToolName.Contains(filter.ToolName));
+
+        var total = await query.CountAsync();
+
+        var items = await query
+            .OrderByDescending(l => l.CreatedAt)
+            .Skip((filter.Page - 1) * filter.PageSize)
+            .Take(filter.PageSize)
+            .ToListAsync();
+
+        // Batch resolve user names and provider names
+        var userIds = items.Where(l => l.UserId != null).Select(l => l.UserId!).Distinct().ToList();
+        var providerIds = items.Where(l => l.McpProviderId != null).Select(l => l.McpProviderId!).Distinct().ToList();
+
+        var userNames = userIds.Count > 0
+            ? await _context.Users
+                .Where(u => userIds.Contains(u.Id))
+                .ToDictionaryAsync(u => u.Id, u => u.Name)
+            : new Dictionary<string, string>();
+
+        var providerNames = providerIds.Count > 0
+            ? await _context.McpProviders
+                .Where(p => providerIds.Contains(p.Id))
+                .ToDictionaryAsync(p => p.Id, p => p.Name)
+            : new Dictionary<string, string>();
+
+        return new Models.Admin.PagedResult<McpUsageLogDto>
+        {
+            Items = items.Select(l => new McpUsageLogDto
+            {
+                Id = l.Id,
+                UserId = l.UserId,
+                UserName = l.UserId != null && userNames.TryGetValue(l.UserId, out var uName) ? uName : null,
+                McpProviderId = l.McpProviderId,
+                McpProviderName = l.McpProviderId != null && providerNames.TryGetValue(l.McpProviderId, out var pName) ? pName : null,
+                ToolName = l.ToolName,
+                RequestSummary = l.RequestSummary,
+                ResponseStatus = l.ResponseStatus,
+                DurationMs = l.DurationMs,
+                InputTokens = l.InputTokens,
+                OutputTokens = l.OutputTokens,
+                IpAddress = l.IpAddress,
+                ErrorMessage = l.ErrorMessage,
+                CreatedAt = l.CreatedAt
+            }).ToList(),
+            Total = total,
+            Page = filter.Page,
+            PageSize = filter.PageSize
+        };
+    }
+
+    public async Task<McpUsageStatisticsResponse> GetMcpUsageStatisticsAsync(int days)
+    {
+        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
+        var response = new McpUsageStatisticsResponse();
+
+        // Try from daily statistics first
+        var dailyStats = await _context.McpDailyStatistics
+            .Where(s => !s.IsDeleted && s.Date >= startDate)
+            .GroupBy(s => s.Date)
+            .Select(g => new
+            {
+                Date = g.Key,
+                RequestCount = g.Sum(s => s.RequestCount),
+                SuccessCount = g.Sum(s => s.SuccessCount),
+                ErrorCount = g.Sum(s => s.ErrorCount),
+                InputTokens = g.Sum(s => s.InputTokens),
+                OutputTokens = g.Sum(s => s.OutputTokens)
+            })
+            .ToListAsync();
+
+        for (var date = startDate; date <= DateTime.UtcNow.Date; date = date.AddDays(1))
+        {
+            var stat = dailyStats.FirstOrDefault(s => s.Date == date);
+            var usage = new McpDailyUsage
+            {
+                Date = date,
+                RequestCount = stat?.RequestCount ?? 0,
+                SuccessCount = stat?.SuccessCount ?? 0,
+                ErrorCount = stat?.ErrorCount ?? 0,
+                InputTokens = stat?.InputTokens ?? 0,
+                OutputTokens = stat?.OutputTokens ?? 0
+            };
+            response.DailyUsages.Add(usage);
+            response.TotalRequests += usage.RequestCount;
+            response.TotalSuccessful += usage.SuccessCount;
+            response.TotalErrors += usage.ErrorCount;
+            response.TotalInputTokens += usage.InputTokens;
+            response.TotalOutputTokens += usage.OutputTokens;
+        }
+
+        return response;
+    }
+}
diff --git a/src/OpenDeepWiki/Services/Admin/IAdminMcpProviderService.cs b/src/OpenDeepWiki/Services/Admin/IAdminMcpProviderService.cs
new file mode 100644
index 0000000..ca8ed25
--- /dev/null
+++ b/src/OpenDeepWiki/Services/Admin/IAdminMcpProviderService.cs
@@ -0,0 +1,16 @@
+using OpenDeepWiki.Models.Admin;
+
+namespace OpenDeepWiki.Services.Admin;
+
+/// <summary>
+/// 管理员 MCP 提供商服务接口
+/// </summary>
+public interface IAdminMcpProviderService
+{
+    Task<List<McpProviderDto>> GetProvidersAsync();
+    Task<McpProviderDto> CreateProviderAsync(McpProviderRequest request);
+    Task<bool> UpdateProviderAsync(string id, McpProviderRequest request);
+    Task<bool> DeleteProviderAsync(string id);
+    Task<Models.Admin.PagedResult<McpUsageLogDto>> GetUsageLogsAsync(McpUsageLogFilter filter);
+    Task<McpUsageStatisticsResponse> GetMcpUsageStatisticsAsync(int days);
+}
diff --git a/src/OpenDeepWiki/Services/Admin/SystemSettingDefaults.cs b/src/OpenDeepWiki/Services/Admin/SystemSettingDefaults.cs
index e227291..bd7928d 100644
--- a/src/OpenDeepWiki/Services/Admin/SystemSettingDefaults.cs
+++ b/src/OpenDeepWiki/Services/Admin/SystemSettingDefaults.cs
@@ -18,29 +18,29 @@ public static class SystemSettingDefaults
     /// </summary>
     public static readonly (string Key, string Category, string Description)[] WikiGeneratorDefaults = 
     [
-        ("WIKI_CATALOG_MODEL", "ai", "目录生成使用的AI模型"),
-        ("WIKI_CATALOG_ENDPOINT", "ai", "目录生成API端点"),
-        ("WIKI_CATALOG_API_KEY", "ai", "目录生成API密钥"),
-        ("WIKI_CATALOG_REQUEST_TYPE", "ai", "目录生成请求类型"),
-        ("WIKI_CONTENT_MODEL", "ai", "内容生成使用的AI模型"),
-        ("WIKI_CONTENT_ENDPOINT", "ai", "内容生成API端点"),
-        ("WIKI_CONTENT_API_KEY", "ai", "内容生成API密钥"),
-        ("WIKI_CONTENT_REQUEST_TYPE", "ai", "内容生成请求类型"),
-        ("WIKI_TRANSLATION_MODEL", "ai", "翻译使用的AI模型"),
-        ("WIKI_TRANSLATION_ENDPOINT", "ai", "翻译API端点"),
-        ("WIKI_TRANSLATION_API_KEY", "ai", "翻译API密钥"),
-        ("WIKI_TRANSLATION_REQUEST_TYPE", "ai", "翻译请求类型"),
-        ("WIKI_LANGUAGES", "ai", "支持的语言列表（逗号分隔）"),
-        ("WIKI_PARALLEL_COUNT", "ai", "并行生成文档数量"),
-        ("WIKI_MAX_OUTPUT_TOKENS", "ai", "最大输出Token数量"),
-        ("WIKI_DOCUMENT_GENERATION_TIMEOUT_MINUTES", "ai", "文档生成超时时间（分钟）"),
-        ("WIKI_TRANSLATION_TIMEOUT_MINUTES", "ai", "翻译超时时间（分钟）"),
-        ("WIKI_TITLE_TRANSLATION_TIMEOUT_MINUTES", "ai", "标题翻译超时时间（分钟）"),
-        ("WIKI_README_MAX_LENGTH", "ai", "README内容最大长度"),
-        ("WIKI_DIRECTORY_TREE_MAX_DEPTH", "ai", "目录树最大深度"),
-        ("WIKI_MAX_RETRY_ATTEMPTS", "ai", "最大重试次数"),
-        ("WIKI_RETRY_DELAY_MS", "ai", "重试延迟时间（毫秒）"),
-        ("WIKI_PROMPTS_DIRECTORY", "ai", "提示模板目录")
+        ("WIKI_CATALOG_MODEL", "ai", "AI model used for catalog generation"),
+        ("WIKI_CATALOG_ENDPOINT", "ai", "API endpoint for catalog generation"),
+        ("WIKI_CATALOG_API_KEY", "ai", "API key for catalog generation"),
+        ("WIKI_CATALOG_REQUEST_TYPE", "ai", "Request type for catalog generation"),
+        ("WIKI_CONTENT_MODEL", "ai", "AI model used for content generation"),
+        ("WIKI_CONTENT_ENDPOINT", "ai", "API endpoint for content generation"),
+        ("WIKI_CONTENT_API_KEY", "ai", "API key for content generation"),
+        ("WIKI_CONTENT_REQUEST_TYPE", "ai", "Request type for content generation"),
+        ("WIKI_TRANSLATION_MODEL", "ai", "AI model used for translation"),
+        ("WIKI_TRANSLATION_ENDPOINT", "ai", "API endpoint for translation"),
+        ("WIKI_TRANSLATION_API_KEY", "ai", "API key for translation"),
+        ("WIKI_TRANSLATION_REQUEST_TYPE", "ai", "Request type for translation"),
+        ("WIKI_LANGUAGES", "ai", "Supported languages list (comma-separated)"),
+        ("WIKI_PARALLEL_COUNT", "ai", "Number of parallel document generations"),
+        ("WIKI_MAX_OUTPUT_TOKENS", "ai", "Maximum output tokens"),
+        ("WIKI_DOCUMENT_GENERATION_TIMEOUT_MINUTES", "ai", "Document generation timeout (minutes)"),
+        ("WIKI_TRANSLATION_TIMEOUT_MINUTES", "ai", "Translation timeout (minutes)"),
+        ("WIKI_TITLE_TRANSLATION_TIMEOUT_MINUTES", "ai", "Title translation timeout (minutes)"),
+        ("WIKI_README_MAX_LENGTH", "ai", "Maximum README length"),
+        ("WIKI_DIRECTORY_TREE_MAX_DEPTH", "ai", "Maximum directory tree depth"),
+        ("WIKI_MAX_RETRY_ATTEMPTS", "ai", "Maximum retry attempts"),
+        ("WIKI_RETRY_DELAY_MS", "ai", "Retry delay (milliseconds)"),
+        ("WIKI_PROMPTS_DIRECTORY", "ai", "Prompt templates directory")
     ];
 
     /// <summary>
diff --git a/src/OpenDeepWiki/Services/Auth/AuthService.cs b/src/OpenDeepWiki/Services/Auth/AuthService.cs
index 3bb360a..e9b4a34 100644
--- a/src/OpenDeepWiki/Services/Auth/AuthService.cs
+++ b/src/OpenDeepWiki/Services/Auth/AuthService.cs
@@ -29,18 +29,18 @@ public class AuthService : IAuthService
 
         if (user == null)
         {
-            throw new UnauthorizedAccessException("邮箱或密码错误");
+            throw new Exception("邮箱或密码错误");
         }
 
         // 验证密码
         if (!VerifyPassword(request.Password, user.Password))
         {
-            throw new UnauthorizedAccessException("邮箱或密码错误");
+            throw new Exception("邮箱或密码错误");
         }
 
         if (user.Status != 1)
         {
-            throw new UnauthorizedAccessException("账号已被禁用或待验证");
+            throw new Exception("账号已被禁用或待验证");
         }
 
         // 更新最后登录时间
diff --git a/src/OpenDeepWiki/Services/Mcp/IMcpUsageLogService.cs b/src/OpenDeepWiki/Services/Mcp/IMcpUsageLogService.cs
new file mode 100644
index 0000000..326ab12
--- /dev/null
+++ b/src/OpenDeepWiki/Services/Mcp/IMcpUsageLogService.cs
@@ -0,0 +1,19 @@
+using OpenDeepWiki.Entities;
+
+namespace OpenDeepWiki.Services.Mcp;
+
+/// <summary>
+/// MCP 使用日志服务接口
+/// </summary>
+public interface IMcpUsageLogService
+{
+    /// <summary>
+    /// 异步记录 MCP 使用日志（不阻塞请求）
+    /// </summary>
+    Task LogUsageAsync(McpUsageLog log);
+
+    /// <summary>
+    /// 聚合指定日期的日志到每日统计
+    /// </summary>
+    Task AggregateDailyStatisticsAsync(DateTime date);
+}
diff --git a/src/OpenDeepWiki/Services/Mcp/McpUsageLogService.cs b/src/OpenDeepWiki/Services/Mcp/McpUsageLogService.cs
new file mode 100644
index 0000000..3847e95
--- /dev/null
+++ b/src/OpenDeepWiki/Services/Mcp/McpUsageLogService.cs
@@ -0,0 +1,116 @@
+using Microsoft.EntityFrameworkCore;
+using OpenDeepWiki.EFCore;
+using OpenDeepWiki.Entities;
+
+namespace OpenDeepWiki.Services.Mcp;
+
+/// <summary>
+/// MCP 使用日志服务实现
+/// </summary>
+public class McpUsageLogService : IMcpUsageLogService
+{
+    private readonly IContextFactory _contextFactory;
+    private readonly ILogger<McpUsageLogService> _logger;
+
+    public McpUsageLogService(IContextFactory contextFactory, ILogger<McpUsageLogService> logger)
+    {
+        _contextFactory = contextFactory;
+        _logger = logger;
+    }
+
+    public async Task LogUsageAsync(McpUsageLog log)
+    {
+        try
+        {
+            using var context = _contextFactory.CreateContext();
+            if (string.IsNullOrWhiteSpace(log.UserId))
+            {
+                log.UserId = "anonymous";
+            }
+
+            if (string.IsNullOrWhiteSpace(log.McpProviderId))
+            {
+                log.McpProviderId = await context.McpProviders
+                    .Where(p => p.IsActive && !p.IsDeleted)
+                    .OrderBy(p => p.SortOrder)
+                    .Select(p => p.Id)
+                    .FirstOrDefaultAsync()
+                    ?? "unknown";
+            }
+
+            log.Id = Guid.NewGuid().ToString();
+            log.CreatedAt = DateTime.UtcNow;
+            context.McpUsageLogs.Add(log);
+            await context.SaveChangesAsync();
+        }
+        catch (Exception ex)
+        {
+            _logger.LogError(ex, "写入 MCP 使用日志失败: {ToolName}", log.ToolName);
+        }
+    }
+
+    public async Task AggregateDailyStatisticsAsync(DateTime date)
+    {
+        try
+        {
+            using var context = _contextFactory.CreateContext();
+            var dateStart = date.Date;
+            var dateEnd = dateStart.AddDays(1);
+
+            var logs = await context.McpUsageLogs
+                .Where(l => !l.IsDeleted && l.CreatedAt >= dateStart && l.CreatedAt < dateEnd)
+                .GroupBy(l => l.McpProviderId)
+                .Select(g => new
+                {
+                    McpProviderId = g.Key,
+                    RequestCount = g.LongCount(),
+                    SuccessCount = g.LongCount(l => l.ResponseStatus >= 200 && l.ResponseStatus < 300),
+                    ErrorCount = g.LongCount(l => l.ResponseStatus >= 400),
+                    TotalDurationMs = g.Sum(l => l.DurationMs),
+                    InputTokens = g.Sum(l => (long)l.InputTokens),
+                    OutputTokens = g.Sum(l => (long)l.OutputTokens)
+                })
+                .ToListAsync();
+
+            foreach (var log in logs)
+            {
+                var existing = await context.McpDailyStatistics
+                    .FirstOrDefaultAsync(s => s.McpProviderId == log.McpProviderId && s.Date == dateStart && !s.IsDeleted);
+
+                if (existing != null)
+                {
+                    existing.RequestCount = log.RequestCount;
+                    existing.SuccessCount = log.SuccessCount;
+                    existing.ErrorCount = log.ErrorCount;
+                    existing.TotalDurationMs = log.TotalDurationMs;
+                    existing.InputTokens = log.InputTokens;
+                    existing.OutputTokens = log.OutputTokens;
+                    existing.UpdatedAt = DateTime.UtcNow;
+                }
+                else
+                {
+                    context.McpDailyStatistics.Add(new McpDailyStatistics
+                    {
+                        Id = Guid.NewGuid().ToString(),
+                        McpProviderId = log.McpProviderId,
+                        Date = dateStart,
+                        RequestCount = log.RequestCount,
+                        SuccessCount = log.SuccessCount,
+                        ErrorCount = log.ErrorCount,
+                        TotalDurationMs = log.TotalDurationMs,
+                        InputTokens = log.InputTokens,
+                        OutputTokens = log.OutputTokens,
+                        CreatedAt = DateTime.UtcNow
+                    });
+                }
+            }
+
+            await context.SaveChangesAsync();
+            _logger.LogInformation("MCP 每日统计聚合完成: {Date}, {Count} 条提供商记录", dateStart.ToString("yyyy-MM-dd"), logs.Count);
+        }
+        catch (Exception ex)
+        {
+            _logger.LogError(ex, "MCP 每日统计聚合失败: {Date}", date.ToString("yyyy-MM-dd"));
+        }
+    }
+}
diff --git a/src/OpenDeepWiki/Services/Wiki/WikiExportService.cs b/src/OpenDeepWiki/Services/Wiki/WikiExportService.cs
new file mode 100644
index 0000000..e26419f
--- /dev/null
+++ b/src/OpenDeepWiki/Services/Wiki/WikiExportService.cs
@@ -0,0 +1,129 @@
+using System.IO.Compression;
+using System.Text;
+using Microsoft.EntityFrameworkCore;
+using OpenDeepWiki.EFCore;
+using OpenDeepWiki.Entities;
+
+namespace OpenDeepWiki.Services.Wiki;
+
+/// <summary>
+/// Service for exporting repository documentation as a ZIP archive of Markdown files.
+/// </summary>
+public class WikiExportService(IContext context)
+{
+    private const string MarkdownExtension = ".md";
+
+    /// <summary>
+    /// Generates a ZIP archive of all documentation for a specific branch and language.
+    /// </summary>
+    /// <param name="org">Organization/Owner name</param>
+    /// <param name="repo">Repository name</param>
+    /// <returns>A byte array containing the ZIP archive content.</returns>
+    public async Task<byte[]> ExportWikiAsync(string org, string repo)
+    {
+        var repository = await GetRepositoryAsync(org, repo);
+        var branch = await GetDefaultBranchAsync(repository.Id);
+        var language = await GetDefaultLanguageAsync(branch.Id);
+
+        var catalogs = await context.DocCatalogs
+            .AsNoTracking()
+            .Where(c => c.BranchLanguageId == language.Id && !c.IsDeleted)
+            .OrderBy(c => c.Order)
+            .ToListAsync();
+
+        if (catalogs.Count == 0)
+        {
+            throw new InvalidOperationException("No documentation found to export.");
+        }
+
+        using var memoryStream = new MemoryStream();
+        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
+        {
+            await ExportCatalogsToArchiveAsync(archive, catalogs, null, "");
+        }
+
+        return memoryStream.ToArray();
+    }
+
+    private async Task ExportCatalogsToArchiveAsync(ZipArchive archive, List<DocCatalog> allCatalogs, string? parentId, string currentPath)
+    {
+        var children = allCatalogs.Where(c => c.ParentId == parentId).OrderBy(c => c.Order).ToList();
+
+        foreach (var catalog in children)
+        {
+            var fileName = SanitizeFileName(catalog.Title) + MarkdownExtension;
+            var entryPath = string.IsNullOrEmpty(currentPath) ? fileName : Path.Combine(currentPath, fileName);
+
+            if (!string.IsNullOrEmpty(catalog.DocFileId))
+            {
+                var docFile = await context.DocFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == catalog.DocFileId);
+                if (docFile != null)
+                {
+                    var entry = archive.CreateEntry(entryPath);
+                    using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
+                    await writer.WriteAsync(docFile.Content);
+                }
+            }
+
+            // Recursively process children
+            // For directories, we might want to create a subfolder if it has children
+            var subPath = string.IsNullOrEmpty(currentPath) ? SanitizeFileName(catalog.Title) : Path.Combine(currentPath, SanitizeFileName(catalog.Title));
+            await ExportCatalogsToArchiveAsync(archive, allCatalogs, catalog.Id, subPath);
+        }
+    }
+
+    private static string SanitizeFileName(string name)
+    {
+        var invalidChars = Path.GetInvalidFileNameChars();
+        return new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
+    }
+
+    private async Task<Repository> GetRepositoryAsync(string org, string repo)
+    {
+        var repository = await context.Repositories
+            .AsNoTracking()
+            .FirstOrDefaultAsync(r => r.OrgName == org && r.RepoName == repo);
+
+        if (repository is null)
+        {
+            throw new KeyNotFoundException($"Repository '{org}/{repo}' not found.");
+        }
+
+        return repository;
+    }
+
+    private async Task<RepositoryBranch> GetDefaultBranchAsync(string repositoryId)
+    {
+        var branches = await context.RepositoryBranches
+            .AsNoTracking()
+            .Where(b => b.RepositoryId == repositoryId)
+            .ToListAsync();
+
+        if (branches.Count == 0)
+        {
+            throw new KeyNotFoundException("No branches found for repository.");
+        }
+
+        return branches.FirstOrDefault(b => string.Equals(b.BranchName, "main", StringComparison.OrdinalIgnoreCase))
+               ?? branches.FirstOrDefault(b => string.Equals(b.BranchName, "master", StringComparison.OrdinalIgnoreCase))
+               ?? branches.OrderBy(b => b.CreatedAt).First();
+    }
+
+    private async Task<BranchLanguage> GetDefaultLanguageAsync(string branchId)
+    {
+        var languages = await context.BranchLanguages
+            .AsNoTracking()
+            .Where(l => l.RepositoryBranchId == branchId)
+            .ToListAsync();
+
+        if (languages.Count == 0)
+        {
+            throw new KeyNotFoundException("No languages found for branch.");
+        }
+
+        // Try 'en' first, then 'zh', then whatever is available
+        return languages.FirstOrDefault(l => string.Equals(l.LanguageCode, "en", StringComparison.OrdinalIgnoreCase))
+               ?? languages.FirstOrDefault(l => string.Equals(l.LanguageCode, "zh", StringComparison.OrdinalIgnoreCase))
+               ?? languages.OrderBy(l => l.CreatedAt).First();
+    }
+}
diff --git a/src/OpenDeepWiki/Services/Wiki/WikiGeneratorOptions.cs b/src/OpenDeepWiki/Services/Wiki/WikiGeneratorOptions.cs
index 21a205a..66fe1db 100644
--- a/src/OpenDeepWiki/Services/Wiki/WikiGeneratorOptions.cs
+++ b/src/OpenDeepWiki/Services/Wiki/WikiGeneratorOptions.cs
@@ -29,13 +29,13 @@ public class WikiGeneratorOptions
     /// Optional custom endpoint for catalog generation.
     /// If not set, falls back to the default AI endpoint.
     /// </summary>
-    public string? CatalogEndpoint { get; set; } = "https://api.routin.ai/";
+    public string? CatalogEndpoint { get; set; }
 
     /// <summary>
     /// Optional custom endpoint for content generation.
     /// If not set, falls back to the default AI endpoint.
     /// </summary>
-    public string? ContentEndpoint { get; set; } = "https://api.routin.ai/";
+    public string? ContentEndpoint { get; set; }
 
     /// <summary>
     /// Optional API key for catalog generation.
@@ -120,13 +120,13 @@ public class WikiGeneratorOptions
     /// The request type for catalog generation (e.g., OpenAI, Azure, Claude).
     /// If not set, uses the default request type.
     /// </summary>
-    public AiRequestType? CatalogRequestType { get; set; } = AiRequestType.Anthropic;
+    public AiRequestType? CatalogRequestType { get; set; }
 
     /// <summary>
     /// The request type for content generation (e.g., OpenAI, Azure, Claude).
     /// If not set, uses the default request type.
     /// </summary>
-    public AiRequestType? ContentRequestType { get; set; } = AiRequestType.Anthropic;
+    public AiRequestType? ContentRequestType { get; set; }
 
     /// <summary>
     /// The AI model to use for translation.
diff --git a/src/OpenDeepWiki/prompts/catalog-generator.md b/src/OpenDeepWiki/prompts/catalog-generator.md
index 87cc8fd..e987b0c 100644
--- a/src/OpenDeepWiki/prompts/catalog-generator.md
+++ b/src/OpenDeepWiki/prompts/catalog-generator.md
@@ -78,6 +78,11 @@ Read the entry point files listed above to understand:
 Use tools to find all significant modules:
 
 ```
+# For Data Pipelines (Airflow + BigQuery)
+ListFiles("dags/**/*.py")
+ListFiles("sql/**/*.sql")
+Grep("BigQueryOperator|PythonOperator", "dags/**/*.py")
+
 # For backend projects
 Grep("class\\s+\\w+(Service|Controller|Repository|Handler)", "**/*.cs")
 
@@ -92,15 +97,16 @@ ListFiles("src/**/*", maxResults=100)
 ### Step 3: Design Catalog Structure
 
 Based on discovered modules, identify **core business features**:
-- What are the main capabilities of this project?
+- What are the main capabilities of this project? (e.g., ETL Pipelines, Data Analysis, User Management)
 - What would a user/developer want to learn about?
 - Group implementation details under meaningful feature names
+- For data pipelines, organize by **Data Flow** or **Pipeline Stage** (e.g., Ingestion, Transformation, Loading)
 - Avoid creating entries for individual files or classes
 
 ### Step 4: Validate & Output
 
 Before calling WriteCatalog, verify:
-- [ ] All major features covered
+- [ ] All major features covered (especially Airflow DAGs and BigQuery logic)
 - [ ] Structure is logical and navigable
 - [ ] No important modules missing
 - [ ] Titles are clear and descriptive
@@ -109,11 +115,10 @@ Before calling WriteCatalog, verify:
 
 ## Output Format
 
-```json
 {
   "items": [
     {
-      "title": "标题 (in {{language}})",
+      "title": "Title (in {{language}})",
       "path": "lowercase-hyphen-path",
       "order": 0,
       "children": []
@@ -122,7 +127,15 @@ Before calling WriteCatalog, verify:
 }
 ```
 
-**Path rules**: lowercase, hyphens, no spaces. Children use dot notation: `parent.child`
+**Path rules**: lowercase, hyphens, no spaces. 
+- Root items: `1-project-overview`, `2-configuration`
+- Children use slash notation: `1-project-overview/1-introduction`, `1-project-overview/2-architecture`
+- **NEVER use dots (`.`) in paths.** Always use slashes (`/`) for nesting.
+
+**Language Enforcement**:
+- ALL output (including titles and descriptions) MUST be in {{language}}.
+- ALL internal reasoning and tool call commentary MUST be in {{language}}.
+
 
 ---
 
diff --git a/src/OpenDeepWiki/prompts/content-generator.md b/src/OpenDeepWiki/prompts/content-generator.md
index 9198082..4388506 100644
--- a/src/OpenDeepWiki/prompts/content-generator.md
+++ b/src/OpenDeepWiki/prompts/content-generator.md
@@ -84,6 +84,11 @@ You are a professional technical documentation writer and code analyst. Your res
 - When `{{language}}` is `en`, generate documentation content in English
 - For other language codes, follow the technical documentation conventions of that language
 
+**Language Enforcement (CRITICAL)**:
+- ALL generated content (prose, explanations, titles) MUST be in the target language ({{language}}).
+- ALL internal reasoning, phase summaries, and tool call commentary MUST be in the target language ({{language}}).
+- Never switch to another language for "thinking" or "reasoning" blocks.
+
 ---
 
 ## 3. Available Tools
@@ -272,6 +277,7 @@ flowchart TD
 - Use ListFiles with targeted glob patterns to find relevant source files
 - Priority order for file discovery:
   P0: Main implementation files (services, controllers, core logic)
+  P0_Data: Airflow DAGs (dags/**/*.py) and BigQuery SQL scripts (sql/**/*.sql)
   P1: Interface/type definitions (contracts, DTOs, models)
   P2: Configuration files (appsettings, env configs, constants)
   P3: Test files (unit tests reveal usage patterns)
@@ -566,8 +572,9 @@ flowchart TD
 |------------|---------------------------|--------------------------------|----------------------------|
 | Service/Component | `flowchart TD` — Architecture & dependencies | `sequenceDiagram` — Key interaction flow | `classDiagram` — Type hierarchy |
 | API/Endpoint | `sequenceDiagram` — Request lifecycle | `flowchart TD` — Error handling paths | `flowchart LR` — Middleware pipeline |
-| Data Model | `erDiagram` — Entity relationships | `flowchart TD` — Data lifecycle | `classDiagram` — Inheritance |
+| Data Model | `erDiagram` — Entity relationships / BigQuery Schemas | `flowchart TD` — Data lifecycle | `classDiagram` — Inheritance |
 | Workflow/Process | `flowchart TD` — Process steps & decisions | `sequenceDiagram` — Actor interactions | `stateDiagram-v2` — State changes |
+| Data Pipeline (Airflow) | `flowchart TD` — DAG task dependencies | `flowchart LR` — Data Lineage (Source -> Sink) | `erDiagram` — Schema transformations |
 | Configuration | `flowchart TD` — Config loading pipeline | `flowchart LR` — Override precedence | — |
 | Infrastructure | `flowchart TD` — Deployment topology | `sequenceDiagram` — Startup sequence | — |
 
@@ -930,8 +937,9 @@ profile management, and account settings.
 
 **Chinese (zh):**
 ```markdown
-## 概述
-UserService 负责处理所有用户相关的操作，包括注册、个人资料管理和账户设置。
+## Overview
+
+UserService handles all user-related operations, including registration, profile management, and account settings.
 ```
 
 ---
diff --git a/src/OpenDeepWiki/prompts/incremental-updater.md b/src/OpenDeepWiki/prompts/incremental-updater.md
index a88abef..4ca71fe 100644
--- a/src/OpenDeepWiki/prompts/incremental-updater.md
+++ b/src/OpenDeepWiki/prompts/incremental-updater.md
@@ -78,6 +78,10 @@ You are a professional documentation maintenance specialist and code change anal
 - For other language codes, follow the technical documentation conventions of that language
 - Maintain language consistency with existing documentation
 
+**Language Enforcement (CRITICAL)**:
+- ALL documentation updates and messages MUST be in the target language ({{language}}).
+- ALL internal reasoning, analysis logs, and tool call summaries MUST be in the target language ({{language}}).
+
 ---
 
 ## 3. Available Tools
@@ -819,7 +823,7 @@ The `GetUserAsync` method now supports cancellation tokens for better async oper
 
 **Chinese Update:**
 ```markdown
-// 更新方法描述
+// Update method description
 `GetUserAsync` 方法现在支持取消令牌，以便更好地控制异步操作。
 ```
 
diff --git a/src/OpenDeepWiki/prompts/mindmap-generator.md b/src/OpenDeepWiki/prompts/mindmap-generator.md
index 22da907..a6bcde2 100644
--- a/src/OpenDeepWiki/prompts/mindmap-generator.md
+++ b/src/OpenDeepWiki/prompts/mindmap-generator.md
@@ -70,32 +70,46 @@ The mind map uses a simple markdown-like format with `#` for hierarchy levels:
 <design_principles>
 **For Backend Projects (dotnet, java, go, python):**
 ```
-# 核心架构
-## API层:src/Controllers
-## 服务层:src/Services
-## 数据层:src/Repositories
-# 领域模型
-## 实体定义:src/Entities
-## 数据传输对象:src/DTOs
-# 基础设施
-## 数据库配置:src/Data
-## 中间件:src/Middleware
+# Core Architecture
+## API Layer:src/Controllers
+## Service Layer:src/Services
+## Data Layer:src/Repositories
+# Domain Models
+## Entity Definitions:src/Entities
+## DTOs:src/DTOs
+# Infrastructure
+## DB Configuration:src/Data
+## Middleware:src/Middleware
 ```
 
 **For Frontend Projects (react, vue, angular):**
 ```
-# 应用入口
-## 路由配置:src/app
-## 布局组件:src/components/layout
-# 功能模块
-## 页面组件:src/pages
-## 业务组件:src/components
-# 状态管理
-## 全局状态:src/store
-## 自定义Hooks:src/hooks
-# 工具层
-## API客户端:src/lib/api
-## 工具函数:src/utils
+# Application Entry
+## Route Config:src/app
+## Layout Components:src/components/layout
+# Features
+## Page Components:src/pages
+## Business Components:src/components
+# State Management
+## Global State:src/store
+## Custom Hooks:src/hooks
+# Utilities
+## API Client:src/lib/api
+## Utility Functions:src/utils
+```
+
+**For Data Pipelines (Airflow, BigQuery, DBT, Spark):**
+```
+# Data Flow
+## Data Sources:src/sources
+## Transformations (SQL/DBT):sql
+## Orchestration (DAGs):dags
+# Metadata & Schema
+## Table Definitions:schema
+## Monitoring & Logs:logs
+# Infrastructure
+## Environment Config:configs
+## Utility Scripts:utils
 ```
 
 **For Full-Stack Projects:**
@@ -171,21 +185,21 @@ Create a hierarchical representation that:
 ## Example Output
 
 ```
-# 系统架构
-## 前端应用:web
-### 页面路由:web/app
-### UI组件:web/components
-### 状态管理:web/hooks
-## 后端服务:src/OpenDeepWiki
-### API端点:src/OpenDeepWiki/Endpoints
-### 业务服务:src/OpenDeepWiki/Services
-### AI代理:src/OpenDeepWiki/Agents
-# 数据层
-## 实体模型:src/OpenDeepWiki.Entities
-## 数据库上下文:src/OpenDeepWiki.EFCore
-# 基础设施
-## 配置文件:compose.yaml
-## 构建脚本:Makefile
+# System Architecture
+## Frontend:web
+### App Routing:web/app
+### UI Components:web/components
+### State Management:web/hooks
+## Backend:src/OpenDeepWiki
+### API Endpoints:src/OpenDeepWiki/Endpoints
+### Business Services:src/OpenDeepWiki/Services
+### AI Agents:src/OpenDeepWiki/Agents
+# Data Layer
+## Entity Models:src/OpenDeepWiki.Entities
+## DB Context:src/OpenDeepWiki.EFCore
+# Infrastructure
+## Config Files:compose.yaml
+## Build Scripts:Makefile
 ```
 
 ---
diff --git a/tests/OpenDeepWiki.Tests/Chat/Config/TestConfigDbContext.cs b/tests/OpenDeepWiki.Tests/Chat/Config/TestConfigDbContext.cs
index 3d5c94f..8817cf5 100644
--- a/tests/OpenDeepWiki.Tests/Chat/Config/TestConfigDbContext.cs
+++ b/tests/OpenDeepWiki.Tests/Chat/Config/TestConfigDbContext.cs
@@ -49,6 +49,9 @@ public class TestConfigDbContext : DbContext, IContext
     public DbSet<ChatLog> ChatLogs { get; set; } = null!;
     public DbSet<TranslationTask> TranslationTasks { get; set; } = null!;
     public DbSet<IncrementalUpdateTask> IncrementalUpdateTasks { get; set; } = null!;
+    public DbSet<McpProvider> McpProviders { get; set; }
+    public DbSet<McpUsageLog> McpUsageLogs { get; set; }
+    public DbSet<McpDailyStatistics> McpDailyStatistics { get; set; }
     public DbSet<ChatShareSnapshot> ChatShareSnapshots { get; set; } = default!;
 
     protected override void OnModelCreating(ModelBuilder modelBuilder)
diff --git a/tests/OpenDeepWiki.Tests/Chat/Sessions/TestDbContext.cs b/tests/OpenDeepWiki.Tests/Chat/Sessions/TestDbContext.cs
index 40b8bb0..382be1a 100644
--- a/tests/OpenDeepWiki.Tests/Chat/Sessions/TestDbContext.cs
+++ b/tests/OpenDeepWiki.Tests/Chat/Sessions/TestDbContext.cs
@@ -49,6 +49,9 @@ public class TestDbContext : DbContext, IContext
     public DbSet<ChatLog> ChatLogs { get; set; } = null!;
     public DbSet<TranslationTask> TranslationTasks { get; set; } = null!;
     public DbSet<IncrementalUpdateTask> IncrementalUpdateTasks { get; set; } = null!;
+    public DbSet<McpProvider> McpProviders { get; set; }
+    public DbSet<McpUsageLog> McpUsageLogs { get; set; }
+    public DbSet<McpDailyStatistics> McpDailyStatistics { get; set; }
     public DbSet<ChatShareSnapshot> ChatShareSnapshots { get; set; } = default!;
     
 
diff --git a/web/app/(main)/mcp/page.tsx b/web/app/(main)/mcp/page.tsx
new file mode 100644
index 0000000..392501a
--- /dev/null
+++ b/web/app/(main)/mcp/page.tsx
@@ -0,0 +1,397 @@
+"use client";
+
+import { useEffect, useState } from "react";
+import { AppLayout } from "@/components/app-layout";
+import { Badge } from "@/components/ui/badge";
+import {
+  Card,
+  CardContent,
+  CardDescription,
+  CardHeader,
+  CardTitle,
+} from "@/components/ui/card";
+import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
+import {
+  Globe,
+  Key,
+  Copy,
+  Check,
+  ExternalLink,
+  Loader2,
+  Zap,
+  Shield,
+  Code2,
+} from "lucide-react";
+import { useTranslations } from "@/hooks/use-translations";
+import { api } from "@/lib/api-client";
+
+interface McpProviderPublic {
+  id: string;
+  name: string;
+  description?: string;
+  serverUrl: string;
+  transportType: string;
+  requiresApiKey: boolean;
+  apiKeyObtainUrl?: string;
+  iconUrl?: string;
+  maxRequestsPerDay: number;
+  allowedTools?: string;
+}
+
+function CopyButton({ text }: { text: string }) {
+  const [copied, setCopied] = useState(false);
+
+  async function handleCopy() {
+    await navigator.clipboard.writeText(text);
+    setCopied(true);
+    setTimeout(() => setCopied(false), 2000);
+  }
+
+  return (
+    <button
+      onClick={handleCopy}
+      className="ml-2 inline-flex items-center text-muted-foreground hover:text-foreground transition-colors"
+    >
+      {copied ? <Check className="h-3.5 w-3.5 text-green-500" /> : <Copy className="h-3.5 w-3.5" />}
+    </button>
+  );
+}
+
+function CodeBlock({ code, language = "json" }: { code: string; language?: string }) {
+  const [copied, setCopied] = useState(false);
+
+  async function handleCopy() {
+    await navigator.clipboard.writeText(code);
+    setCopied(true);
+    setTimeout(() => setCopied(false), 2000);
+  }
+
+  return (
+    <div className="relative rounded-lg bg-muted/60 border">
+      <div className="flex items-center justify-between px-4 py-2 border-b">
+        <span className="text-xs text-muted-foreground font-mono">{language}</span>
+        <button
+          onClick={handleCopy}
+          className="flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
+        >
+          {copied ? (
+            <><Check className="h-3.5 w-3.5 text-green-500" /> Copied</>
+          ) : (
+            <><Copy className="h-3.5 w-3.5" /> Copy</>
+          )}
+        </button>
+      </div>
+      <pre className="p-4 text-sm overflow-x-auto font-mono leading-relaxed">
+        <code>{code}</code>
+      </pre>
+    </div>
+  );
+}
+
+export default function McpPage() {
+  const t = useTranslations();
+  const [providers, setProviders] = useState<McpProviderPublic[]>([]);
+  const [loading, setLoading] = useState(true);
+  const [origin, setOrigin] = useState("");
+
+  function buildRepositoryScopedServerUrl(serverUrl: string) {
+    const template = (serverUrl || "/api/mcp/{owner}/{repo}")
+      .replace("{owner}", "<owner>")
+      .replace("{repo}", "<repo>");
+
+    if (!origin || /^https?:\/\//.test(template)) {
+      return template;
+    }
+
+    return `${origin}${template.startsWith("/") ? "" : "/"}${template}`;
+  }
+
+  useEffect(() => {
+    setOrigin(window.location.origin);
+
+    api
+      .get<{ success: boolean; data: McpProviderPublic[] }>("/api/mcp-providers", {
+        skipAuth: true,
+      })
+      .then((res) => {
+        if (res.success) setProviders(res.data);
+      })
+      .catch(() => {})
+      .finally(() => setLoading(false));
+  }, []);
+
+  return (
+    <AppLayout>
+      <div className="max-w-5xl mx-auto px-4 py-10 space-y-10">
+        {/* Hero */}
+        <div className="text-center space-y-4">
+          <div className="inline-flex items-center gap-2 rounded-full bg-primary/10 px-4 py-1.5 text-sm font-medium text-primary">
+            <Zap className="h-4 w-4" />
+            Model Context Protocol
+          </div>
+          <h1 className="text-4xl font-bold tracking-tight">{t("common.mcp.title")}</h1>
+          <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
+            {t("common.mcp.description")}
+          </p>
+        </div>
+
+        {/* Feature cards */}
+        <div className="grid gap-4 md:grid-cols-3">
+          <Card className="border-dashed">
+            <CardHeader className="pb-3">
+              <div className="rounded-full bg-blue-100 dark:bg-blue-900 w-10 h-10 flex items-center justify-center mb-2">
+                <Code2 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
+              </div>
+              <CardTitle className="text-base">{t("common.mcp.featureStandardTitle")}</CardTitle>
+            </CardHeader>
+            <CardContent>
+              <p className="text-sm text-muted-foreground">{t("common.mcp.featureStandardDesc")}</p>
+            </CardContent>
+          </Card>
+          <Card className="border-dashed">
+            <CardHeader className="pb-3">
+              <div className="rounded-full bg-green-100 dark:bg-green-900 w-10 h-10 flex items-center justify-center mb-2">
+                <Shield className="h-5 w-5 text-green-600 dark:text-green-400" />
+              </div>
+              <CardTitle className="text-base">{t("common.mcp.featureAuthTitle")}</CardTitle>
+            </CardHeader>
+            <CardContent>
+              <p className="text-sm text-muted-foreground">{t("common.mcp.featureAuthDesc")}</p>
+            </CardContent>
+          </Card>
+          <Card className="border-dashed">
+            <CardHeader className="pb-3">
+              <div className="rounded-full bg-purple-100 dark:bg-purple-900 w-10 h-10 flex items-center justify-center mb-2">
+                <Zap className="h-5 w-5 text-purple-600 dark:text-purple-400" />
+              </div>
+              <CardTitle className="text-base">{t("common.mcp.featureToolsTitle")}</CardTitle>
+            </CardHeader>
+            <CardContent>
+              <p className="text-sm text-muted-foreground">{t("common.mcp.featureToolsDesc")}</p>
+            </CardContent>
+          </Card>
+        </div>
+
+        {/* Provider list */}
+        <div className="space-y-4">
+          <h2 className="text-2xl font-semibold">{t("common.mcp.availableProviders")}</h2>
+          {loading ? (
+            <div className="flex items-center justify-center py-16">
+              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
+            </div>
+          ) : providers.length === 0 ? (
+            <Card>
+              <CardContent className="flex flex-col items-center justify-center py-16 text-center">
+                <Globe className="h-12 w-12 text-muted-foreground mb-4" />
+                <p className="text-lg font-medium">{t("common.mcp.noProviders")}</p>
+                <p className="text-sm text-muted-foreground mt-1">{t("common.mcp.noProvidersHint")}</p>
+              </CardContent>
+            </Card>
+          ) : (
+            <div className="space-y-4">
+              {providers.map((provider) => {
+                const repositoryScopedServerUrl = buildRepositoryScopedServerUrl(provider.serverUrl);
+
+                return (
+                <Card key={provider.id}>
+                  <CardHeader>
+                    <div className="flex items-start justify-between gap-4">
+                      <div className="flex items-center gap-3">
+                        {provider.iconUrl ? (
+                          <img src={provider.iconUrl} alt="" className="h-8 w-8 rounded-lg" />
+                        ) : (
+                          <div className="h-8 w-8 rounded-lg bg-muted flex items-center justify-center">
+                            <Globe className="h-4 w-4 text-muted-foreground" />
+                          </div>
+                        )}
+                        <div>
+                          <CardTitle className="text-lg">{provider.name}</CardTitle>
+                          {provider.description && (
+                            <CardDescription className="mt-0.5">{provider.description}</CardDescription>
+                          )}
+                        </div>
+                      </div>
+                      <div className="flex flex-wrap gap-1.5 shrink-0">
+                        <Badge variant="secondary" className="text-xs">{provider.transportType}</Badge>
+                        {provider.requiresApiKey && (
+                          <Badge variant="outline" className="text-xs gap-1">
+                            <Key className="h-3 w-3" />
+                            {t("common.mcp.requiresApiKey")}
+                          </Badge>
+                        )}
+                        {provider.maxRequestsPerDay > 0 && (
+                          <Badge variant="outline" className="text-xs">
+                            {provider.maxRequestsPerDay} {t("common.mcp.reqPerDay")}
+                          </Badge>
+                        )}
+                      </div>
+                    </div>
+                  </CardHeader>
+                  <CardContent>
+                    <Tabs defaultValue="config">
+                      <TabsList className="mb-4">
+                        <TabsTrigger value="config">{t("common.mcp.tabConfig")}</TabsTrigger>
+                        <TabsTrigger value="claude">{t("common.mcp.tabClaude")}</TabsTrigger>
+                        <TabsTrigger value="cursor">{t("common.mcp.tabCursor")}</TabsTrigger>
+                        {provider.allowedTools && (
+                          <TabsTrigger value="tools">{t("common.mcp.tabTools")}</TabsTrigger>
+                        )}
+                      </TabsList>
+
+                      <TabsContent value="config" className="space-y-3">
+                        <div className="flex items-center gap-2 text-sm">
+                          <span className="text-muted-foreground w-28 shrink-0">{t("common.mcp.serverUrl")}:</span>
+                          <code className="font-mono text-xs bg-muted px-2 py-1 rounded flex-1 break-all">
+                            {repositoryScopedServerUrl}
+                          </code>
+                          <CopyButton text={repositoryScopedServerUrl} />
+                        </div>
+                        <div className="flex items-center gap-2 text-sm">
+                          <span className="text-muted-foreground w-28 shrink-0">{t("common.mcp.transport")}:</span>
+                          <code className="font-mono text-xs bg-muted px-2 py-1 rounded">{provider.transportType}</code>
+                        </div>
+                        {provider.requiresApiKey && (
+                          <div className="rounded-lg border border-amber-200 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/30 p-3 text-sm">
+                            <p className="font-medium text-amber-800 dark:text-amber-300 flex items-center gap-1.5">
+                              <Key className="h-3.5 w-3.5" />
+                              {t("common.mcp.apiKeyRequired")}
+                            </p>
+                            <p className="text-amber-700 dark:text-amber-400 mt-1 text-xs">
+                              {t("common.mcp.apiKeyHint")}
+                            </p>
+                            {provider.apiKeyObtainUrl && (
+                              <a
+                                href={provider.apiKeyObtainUrl}
+                                target="_blank"
+                                rel="noopener noreferrer"
+                                className="inline-flex items-center gap-1 mt-2 text-xs text-amber-700 dark:text-amber-400 hover:underline font-medium"
+                              >
+                                <ExternalLink className="h-3 w-3" />
+                                {t("common.mcp.getApiKey")}
+                              </a>
+                            )}
+                          </div>
+                        )}
+                      </TabsContent>
+
+                      <TabsContent value="claude">
+                        <CodeBlock
+                          language="json (claude_desktop_config.json)"
+                          code={JSON.stringify(
+                            {
+                              mcpServers: {
+                                [provider.name.toLowerCase().replace(/\s+/g, "-")]: {
+                                  command: "npx",
+                                  args: [
+                                    "-y",
+                                    "@modelcontextprotocol/client-http",
+                                    repositoryScopedServerUrl,
+                                  ],
+                                  ...(provider.requiresApiKey
+                                    ? {
+                                        env: {
+                                          MCP_API_KEY: "<your-api-key>",
+                                        },
+                                      }
+                                    : {}),
+                                },
+                              },
+                            },
+                            null,
+                            2
+                          )}
+                        />
+                        <p className="text-xs text-muted-foreground mt-2">
+                          {t("common.mcp.claudeHint")}
+                        </p>
+                      </TabsContent>
+
+                      <TabsContent value="cursor">
+                        <CodeBlock
+                          language="json (.cursor/mcp.json)"
+                          code={JSON.stringify(
+                            {
+                              mcpServers: {
+                                [provider.name.toLowerCase().replace(/\s+/g, "-")]: {
+                                  url: repositoryScopedServerUrl,
+                                  transport: provider.transportType,
+                                  ...(provider.requiresApiKey
+                                    ? {
+                                        headers: {
+                                          Authorization: "Bearer <your-api-key>",
+                                        },
+                                      }
+                                    : {}),
+                                },
+                              },
+                            },
+                            null,
+                            2
+                          )}
+                        />
+                        <p className="text-xs text-muted-foreground mt-2">
+                          {t("common.mcp.cursorHint")}
+                        </p>
+                      </TabsContent>
+
+                      {provider.allowedTools && (
+                        <TabsContent value="tools">
+                          <div className="flex flex-wrap gap-2">
+                            {(() => {
+                              try {
+                                const tools: string[] = JSON.parse(provider.allowedTools);
+                                return tools.map((tool) => (
+                                  <Badge key={tool} variant="secondary" className="font-mono text-xs">
+                                    {tool}
+                                  </Badge>
+                                ));
+                              } catch {
+                                return (
+                                  <code className="text-xs text-muted-foreground">{provider.allowedTools}</code>
+                                );
+                              }
+                            })()}
+                          </div>
+                        </TabsContent>
+                      )}
+                    </Tabs>
+                  </CardContent>
+                </Card>
+                );
+              })}
+            </div>
+          )}
+        </div>
+
+        {/* General usage guide */}
+        <div className="space-y-4">
+          <h2 className="text-2xl font-semibold">{t("common.mcp.generalGuide")}</h2>
+          <Card>
+            <CardContent className="pt-6 space-y-4">
+              <p className="text-sm text-muted-foreground">{t("common.mcp.generalGuideDesc")}</p>
+              <CodeBlock
+                language="http"
+                code={`GET /api/mcp-providers HTTP/1.1
+Host: <your-server>
+
+# Response
+{
+  "success": true,
+  "data": [
+    {
+      "id": "...",
+      "name": "My MCP Provider",
+      "serverUrl": "/api/mcp/{owner}/{repo}",
+      "transportType": "streamable_http",
+      "requiresApiKey": true
+    }
+  ]
+}`}
+              />
+            </CardContent>
+          </Card>
+        </div>
+      </div>
+    </AppLayout>
+  );
+}
diff --git a/web/app/admin/mcp-providers/page.tsx b/web/app/admin/mcp-providers/page.tsx
new file mode 100644
index 0000000..2cfdbd3
--- /dev/null
+++ b/web/app/admin/mcp-providers/page.tsx
@@ -0,0 +1,558 @@
+"use client";
+
+import { useState, useEffect } from "react";
+import {
+  getMcpProviders,
+  createMcpProvider,
+  updateMcpProvider,
+  deleteMcpProvider,
+  getMcpUsageLogs,
+  getModelConfigs,
+  type McpProvider,
+  type McpProviderRequest,
+  type McpUsageLog,
+  type PagedResult,
+  type ModelConfig,
+} from "@/lib/admin-api";
+import { Button } from "@/components/ui/button";
+import { Input } from "@/components/ui/input";
+import { Label } from "@/components/ui/label";
+import { Switch } from "@/components/ui/switch";
+import { Textarea } from "@/components/ui/textarea";
+import { Badge } from "@/components/ui/badge";
+import {
+  Card,
+  CardContent,
+  CardDescription,
+  CardHeader,
+  CardTitle,
+} from "@/components/ui/card";
+import {
+  Dialog,
+  DialogContent,
+  DialogDescription,
+  DialogFooter,
+  DialogHeader,
+  DialogTitle,
+} from "@/components/ui/dialog";
+import {
+  AlertDialog,
+  AlertDialogAction,
+  AlertDialogCancel,
+  AlertDialogContent,
+  AlertDialogDescription,
+  AlertDialogFooter,
+  AlertDialogHeader,
+  AlertDialogTitle,
+} from "@/components/ui/alert-dialog";
+import {
+  Select,
+  SelectContent,
+  SelectItem,
+  SelectTrigger,
+  SelectValue,
+} from "@/components/ui/select";
+import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
+import {
+  Plus,
+  Pencil,
+  Trash2,
+  Loader2,
+  Globe,
+  Key,
+  Cpu,
+  ExternalLink,
+} from "lucide-react";
+import { useTranslations } from "@/hooks/use-translations";
+
+const REPOSITORY_SCOPED_MCP_PATH_TEMPLATE = "/api/mcp/{owner}/{repo}";
+
+const defaultFormData: McpProviderRequest = {
+  name: "",
+  description: "",
+  serverUrl: REPOSITORY_SCOPED_MCP_PATH_TEMPLATE,
+  transportType: "streamable_http",
+  requiresApiKey: true,
+  apiKeyObtainUrl: "",
+  systemApiKey: "",
+  modelConfigId: "",
+  isActive: true,
+  sortOrder: 0,
+  iconUrl: "",
+  maxRequestsPerDay: 0,
+};
+
+export default function AdminMcpProvidersPage() {
+  const t = useTranslations();
+  const [providers, setProviders] = useState<McpProvider[]>([]);
+  const [models, setModels] = useState<ModelConfig[]>([]);
+  const [loading, setLoading] = useState(true);
+  const [showDialog, setShowDialog] = useState(false);
+  const [editingProvider, setEditingProvider] = useState<McpProvider | null>(null);
+  const [deleteId, setDeleteId] = useState<string | null>(null);
+  const [formData, setFormData] = useState<McpProviderRequest>(defaultFormData);
+
+  // Usage logs state
+  const [usageLogs, setUsageLogs] = useState<PagedResult<McpUsageLog> | null>(null);
+  const [logsPage, setLogsPage] = useState(1);
+  const [logsLoading, setLogsLoading] = useState(false);
+
+  useEffect(() => {
+    loadData();
+  }, []);
+
+  async function loadData() {
+    setLoading(true);
+    try {
+      const [providerData, modelData] = await Promise.all([
+        getMcpProviders(),
+        getModelConfigs(),
+      ]);
+      setProviders(providerData);
+      setModels(modelData);
+    } catch (error) {
+      console.error("Failed to load data:", error);
+    } finally {
+      setLoading(false);
+    }
+  }
+
+  async function loadUsageLogs(page: number = 1) {
+    setLogsLoading(true);
+    try {
+      const result = await getMcpUsageLogs({ page, pageSize: 20 });
+      setUsageLogs(result);
+      setLogsPage(page);
+    } catch (error) {
+      console.error("Failed to load usage logs:", error);
+    } finally {
+      setLogsLoading(false);
+    }
+  }
+
+  function openCreateDialog() {
+    setEditingProvider(null);
+    setFormData(defaultFormData);
+    setShowDialog(true);
+  }
+
+  function openEditDialog(provider: McpProvider) {
+    setEditingProvider(provider);
+    setFormData({
+      name: provider.name,
+      description: provider.description || "",
+      serverUrl: REPOSITORY_SCOPED_MCP_PATH_TEMPLATE,
+      transportType: provider.transportType,
+      requiresApiKey: provider.requiresApiKey,
+      apiKeyObtainUrl: provider.apiKeyObtainUrl || "",
+      systemApiKey: "",
+      modelConfigId: provider.modelConfigId || "",
+      isActive: provider.isActive,
+      sortOrder: provider.sortOrder,
+      iconUrl: provider.iconUrl || "",
+      maxRequestsPerDay: provider.maxRequestsPerDay,
+    });
+    setShowDialog(true);
+  }
+
+  async function handleSave() {
+    try {
+      if (editingProvider) {
+        await updateMcpProvider(editingProvider.id, formData);
+      } else {
+        await createMcpProvider(formData);
+      }
+      setShowDialog(false);
+      await loadData();
+    } catch (error) {
+      console.error("Failed to save:", error);
+    }
+  }
+
+  async function handleDelete() {
+    if (!deleteId) return;
+    try {
+      await deleteMcpProvider(deleteId);
+      setDeleteId(null);
+      await loadData();
+    } catch (error) {
+      console.error("Failed to delete:", error);
+    }
+  }
+
+  if (loading) {
+    return (
+      <div className="flex items-center justify-center h-64">
+        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
+      </div>
+    );
+  }
+
+  return (
+    <div className="space-y-6">
+      <div className="flex items-center justify-between">
+        <div>
+          <h1 className="text-2xl font-bold">{t("admin.mcpProviders.title")}</h1>
+          <p className="text-muted-foreground">{t("admin.mcpProviders.description")}</p>
+        </div>
+        <Button onClick={openCreateDialog}>
+          <Plus className="h-4 w-4 mr-2" />
+          {t("admin.mcpProviders.create")}
+        </Button>
+      </div>
+
+      <Tabs defaultValue="providers" onValueChange={(v) => v === "logs" && loadUsageLogs()}>
+        <TabsList>
+          <TabsTrigger value="providers">{t("admin.mcpProviders.providersTab")}</TabsTrigger>
+          <TabsTrigger value="logs">{t("admin.mcpProviders.logsTab")}</TabsTrigger>
+        </TabsList>
+
+        <TabsContent value="providers" className="space-y-4">
+          {providers.length === 0 ? (
+            <Card>
+              <CardContent className="flex flex-col items-center justify-center py-12">
+                <Globe className="h-12 w-12 text-muted-foreground mb-4" />
+                <p className="text-muted-foreground">{t("admin.mcpProviders.empty")}</p>
+              </CardContent>
+            </Card>
+          ) : (
+            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
+              {providers.map((provider) => (
+                <Card key={provider.id} className={!provider.isActive ? "opacity-60" : ""}>
+                  <CardHeader className="pb-3">
+                    <div className="flex items-start justify-between">
+                      <div className="flex items-center gap-2">
+                        {provider.iconUrl ? (
+                          <img src={provider.iconUrl} alt="" className="h-6 w-6 rounded" />
+                        ) : (
+                          <Globe className="h-5 w-5 text-muted-foreground" />
+                        )}
+                        <CardTitle className="text-lg">{provider.name}</CardTitle>
+                      </div>
+                      <div className="flex gap-1">
+                        <Button variant="ghost" size="icon" onClick={() => openEditDialog(provider)}>
+                          <Pencil className="h-4 w-4" />
+                        </Button>
+                        <Button variant="ghost" size="icon" onClick={() => setDeleteId(provider.id)}>
+                          <Trash2 className="h-4 w-4 text-destructive" />
+                        </Button>
+                      </div>
+                    </div>
+                    {provider.description && (
+                      <CardDescription>{provider.description}</CardDescription>
+                    )}
+                  </CardHeader>
+                  <CardContent className="space-y-2 text-sm">
+                    <div className="flex items-center gap-2">
+                      <Globe className="h-3.5 w-3.5 text-muted-foreground" />
+                      <span className="text-muted-foreground truncate">{provider.serverUrl}</span>
+                    </div>
+                    <div className="flex flex-wrap gap-1.5">
+                      <Badge variant="secondary">{provider.transportType}</Badge>
+                      {provider.requiresApiKey && (
+                        <Badge variant="outline" className="gap-1">
+                          <Key className="h-3 w-3" />
+                          API Key
+                        </Badge>
+                      )}
+                      {provider.modelConfigName && (
+                        <Badge variant="outline" className="gap-1">
+                          <Cpu className="h-3 w-3" />
+                          {provider.modelConfigName}
+                        </Badge>
+                      )}
+                      {!provider.isActive && <Badge variant="destructive">Disabled</Badge>}
+                    </div>
+                    {provider.apiKeyObtainUrl && (
+                      <a
+                        href={provider.apiKeyObtainUrl}
+                        target="_blank"
+                        rel="noopener noreferrer"
+                        className="flex items-center gap-1 text-xs text-primary hover:underline"
+                      >
+                        <ExternalLink className="h-3 w-3" />
+                        {t("admin.mcpProviders.getApiKey")}
+                      </a>
+                    )}
+                    {provider.maxRequestsPerDay > 0 && (
+                      <p className="text-xs text-muted-foreground">
+                        {t("admin.mcpProviders.dailyLimit")}: {provider.maxRequestsPerDay}
+                      </p>
+                    )}
+                  </CardContent>
+                </Card>
+              ))}
+            </div>
+          )}
+        </TabsContent>
+
+        <TabsContent value="logs">
+          {logsLoading ? (
+            <div className="flex items-center justify-center h-32">
+              <Loader2 className="h-6 w-6 animate-spin" />
+            </div>
+          ) : usageLogs && usageLogs.items.length > 0 ? (
+            <div className="space-y-4">
+              <div className="rounded-md border overflow-x-auto">
+                <table className="w-full text-sm">
+                  <thead className="border-b bg-muted/50">
+                    <tr>
+                      <th className="px-4 py-3 text-left font-medium">{t("admin.mcpProviders.logTime")}</th>
+                      <th className="px-4 py-3 text-left font-medium">{t("admin.mcpProviders.logUser")}</th>
+                      <th className="px-4 py-3 text-left font-medium">{t("admin.mcpProviders.logTool")}</th>
+                      <th className="px-4 py-3 text-left font-medium">{t("admin.mcpProviders.logStatus")}</th>
+                      <th className="px-4 py-3 text-left font-medium">{t("admin.mcpProviders.logDuration")}</th>
+                      <th className="px-4 py-3 text-left font-medium">IP</th>
+                    </tr>
+                  </thead>
+                  <tbody>
+                    {usageLogs.items.map((log) => (
+                      <tr key={log.id} className="border-b last:border-0">
+                        <td className="px-4 py-3 text-xs">
+                          {new Date(log.createdAt).toLocaleString()}
+                        </td>
+                        <td className="px-4 py-3">{log.userName || log.userId || "-"}</td>
+                        <td className="px-4 py-3 font-mono text-xs">{log.toolName}</td>
+                        <td className="px-4 py-3">
+                          <Badge variant={log.responseStatus < 400 ? "secondary" : "destructive"}>
+                            {log.responseStatus}
+                          </Badge>
+                        </td>
+                        <td className="px-4 py-3">{log.durationMs}ms</td>
+                        <td className="px-4 py-3 text-xs text-muted-foreground">
+                          {log.ipAddress || "-"}
+                        </td>
+                      </tr>
+                    ))}
+                  </tbody>
+                </table>
+              </div>
+              <div className="flex justify-between items-center">
+                <p className="text-sm text-muted-foreground">
+                  {t("admin.mcpProviders.logTotal")}: {usageLogs.total}
+                </p>
+                <div className="flex gap-2">
+                  <Button
+                    variant="outline"
+                    size="sm"
+                    disabled={logsPage <= 1}
+                    onClick={() => loadUsageLogs(logsPage - 1)}
+                  >
+                    {t("common.previous")}
+                  </Button>
+                  <Button
+                    variant="outline"
+                    size="sm"
+                    disabled={logsPage * 20 >= usageLogs.total}
+                    onClick={() => loadUsageLogs(logsPage + 1)}
+                  >
+                    {t("common.next")}
+                  </Button>
+                </div>
+              </div>
+            </div>
+          ) : (
+            <Card>
+              <CardContent className="flex flex-col items-center justify-center py-12">
+                <p className="text-muted-foreground">{t("admin.mcpProviders.noLogs")}</p>
+              </CardContent>
+            </Card>
+          )}
+        </TabsContent>
+      </Tabs>
+
+      {/* Create/Edit Dialog */}
+      <Dialog open={showDialog} onOpenChange={setShowDialog}>
+        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
+          <DialogHeader>
+            <DialogTitle>
+              {editingProvider
+                ? t("admin.mcpProviders.edit")
+                : t("admin.mcpProviders.create")}
+            </DialogTitle>
+            <DialogDescription>
+              {t("admin.mcpProviders.dialogDescription")}
+            </DialogDescription>
+          </DialogHeader>
+          <div className="grid gap-4 py-4">
+            <div className="grid grid-cols-2 gap-4">
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldName")}</Label>
+                <Input
+                  value={formData.name}
+                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
+                  placeholder=" MCP"
+                />
+              </div>
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldServerUrl")}</Label>
+                <Input
+                  value={REPOSITORY_SCOPED_MCP_PATH_TEMPLATE}
+                  readOnly
+                />
+                <p className="text-xs text-muted-foreground">
+                  固定路径模板，后端会按仓库解析 owner/repo。
+                </p>
+              </div>
+            </div>
+
+            <div className="space-y-2">
+              <Label>{t("admin.mcpProviders.fieldDescription")}</Label>
+              <Textarea
+                value={formData.description || ""}
+                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
+                rows={2}
+              />
+            </div>
+
+            <div className="grid grid-cols-2 gap-4">
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldTransportType")}</Label>
+                <Select
+                  value={formData.transportType}
+                  onValueChange={(v) => setFormData({ ...formData, transportType: v })}
+                >
+                  <SelectTrigger>
+                    <SelectValue />
+                  </SelectTrigger>
+                  <SelectContent>
+                    <SelectItem value="streamable_http">Streamable HTTP</SelectItem>
+                    <SelectItem value="sse">SSE</SelectItem>
+                  </SelectContent>
+                </Select>
+              </div>
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldModel")}</Label>
+                <Select
+                  value={formData.modelConfigId || "none"}
+                  onValueChange={(v) =>
+                    setFormData({ ...formData, modelConfigId: v === "none" ? "" : v })
+                  }
+                >
+                  <SelectTrigger>
+                    <SelectValue placeholder={t("admin.mcpProviders.noModel")} />
+                  </SelectTrigger>
+                  <SelectContent>
+                    <SelectItem value="none">{t("admin.mcpProviders.noModel")}</SelectItem>
+                    {models
+                      .filter((m) => m.isActive)
+                      .map((m) => (
+                        <SelectItem key={m.id} value={m.id}>
+                          {m.name}
+                        </SelectItem>
+                      ))}
+                  </SelectContent>
+                </Select>
+              </div>
+            </div>
+
+            <div className="space-y-4 rounded-lg border p-4">
+              <div className="flex items-center justify-between">
+                <div>
+                  <Label>{t("admin.mcpProviders.fieldRequiresApiKey")}</Label>
+                  <p className="text-xs text-muted-foreground">
+                    {t("admin.mcpProviders.fieldRequiresApiKeyHint")}
+                  </p>
+                </div>
+                <Switch
+                  checked={formData.requiresApiKey}
+                  onCheckedChange={(v) => setFormData({ ...formData, requiresApiKey: v })}
+                />
+              </div>
+              {formData.requiresApiKey && (
+                <div className="space-y-2">
+                  <Label>{t("admin.mcpProviders.fieldApiKeyObtainUrl")}</Label>
+                  <Input
+                    value={formData.apiKeyObtainUrl || ""}
+                    onChange={(e) => setFormData({ ...formData, apiKeyObtainUrl: e.target.value })}
+                    placeholder="https://platform.example.com/api-keys"
+                  />
+                </div>
+              )}
+              {!formData.requiresApiKey && (
+                <div className="space-y-2">
+                  <Label>{t("admin.mcpProviders.fieldSystemApiKey")}</Label>
+                  <Input
+                    type="password"
+                    value={formData.systemApiKey || ""}
+                    onChange={(e) => setFormData({ ...formData, systemApiKey: e.target.value })}
+                    placeholder={editingProvider?.hasSystemApiKey ? "••••••••" : ""}
+                  />
+                </div>
+              )}
+            </div>
+
+            <div className="grid grid-cols-3 gap-4">
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldSortOrder")}</Label>
+                <Input
+                  type="number"
+                  value={formData.sortOrder}
+                  onChange={(e) =>
+                    setFormData({ ...formData, sortOrder: parseInt(e.target.value) || 0 })
+                  }
+                />
+              </div>
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldDailyLimit")}</Label>
+                <Input
+                  type="number"
+                  value={formData.maxRequestsPerDay}
+                  onChange={(e) =>
+                    setFormData({
+                      ...formData,
+                      maxRequestsPerDay: parseInt(e.target.value) || 0,
+                    })
+                  }
+                />
+                <p className="text-xs text-muted-foreground">0 = {t("admin.mcpProviders.unlimited")}</p>
+              </div>
+              <div className="space-y-2">
+                <Label>{t("admin.mcpProviders.fieldIconUrl")}</Label>
+                <Input
+                  value={formData.iconUrl || ""}
+                  onChange={(e) => setFormData({ ...formData, iconUrl: e.target.value })}
+                  placeholder="https://..."
+                />
+              </div>
+            </div>
+
+            <div className="flex items-center gap-2">
+              <Switch
+                checked={formData.isActive}
+                onCheckedChange={(v) => setFormData({ ...formData, isActive: v })}
+              />
+              <Label>{t("admin.mcpProviders.fieldIsActive")}</Label>
+            </div>
+          </div>
+          <DialogFooter>
+            <Button variant="outline" onClick={() => setShowDialog(false)}>
+              {t("common.cancel")}
+            </Button>
+            <Button onClick={handleSave} disabled={!formData.name}>
+              {t("common.save")}
+            </Button>
+          </DialogFooter>
+        </DialogContent>
+      </Dialog>
+
+      {/* Delete Confirmation */}
+      <AlertDialog open={!!deleteId} onOpenChange={() => setDeleteId(null)}>
+        <AlertDialogContent>
+          <AlertDialogHeader>
+            <AlertDialogTitle>{t("admin.mcpProviders.deleteTitle")}</AlertDialogTitle>
+            <AlertDialogDescription>
+              {t("admin.mcpProviders.deleteDescription")}
+            </AlertDialogDescription>
+          </AlertDialogHeader>
+          <AlertDialogFooter>
+            <AlertDialogCancel>{t("common.cancel")}</AlertDialogCancel>
+            <AlertDialogAction onClick={handleDelete} className="bg-destructive text-destructive-foreground">
+              {t("common.delete")}
+            </AlertDialogAction>
+          </AlertDialogFooter>
+        </AlertDialogContent>
+      </AlertDialog>
+    </div>
+  );
+}
diff --git a/web/app/admin/page.tsx b/web/app/admin/page.tsx
index cddef13..7b76288 100644
--- a/web/app/admin/page.tsx
+++ b/web/app/admin/page.tsx
@@ -6,8 +6,10 @@ import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
 import {
   getDashboardStatistics,
   getTokenUsageStatistics,
+  getMcpUsageStatistics,
   DashboardStatistics,
   TokenUsageStatistics,
+  McpUsageStatistics,
 } from "@/lib/admin-api";
 import {
   BarChart,
@@ -18,7 +20,7 @@ import {
   Legend,
   ResponsiveContainer,
 } from "recharts";
-import { Loader2, GitBranch, Users, Coins, TrendingUp } from "lucide-react";
+import { Loader2, GitBranch, Users, Coins, TrendingUp, Globe } from "lucide-react";
 import { useTranslations } from "@/hooks/use-translations";
 import { useLocale } from "next-intl";
 
@@ -97,6 +99,7 @@ function CustomTooltip({
 export default function AdminDashboardPage() {
   const [dashboardStats, setDashboardStats] = useState<DashboardStatistics | null>(null);
   const [tokenStats, setTokenStats] = useState<TokenUsageStatistics | null>(null);
+  const [mcpStats, setMcpStats] = useState<McpUsageStatistics | null>(null);
   const [loading, setLoading] = useState(true);
   const [days, setDays] = useState(7);
   const t = useTranslations();
@@ -112,6 +115,12 @@ export default function AdminDashboardPage() {
         ]);
         setDashboardStats(dashboard);
         setTokenStats(token);
+        try {
+          const mcp = await getMcpUsageStatistics(days);
+          setMcpStats(mcp);
+        } catch {
+          // MCP stats may not be available if MCP is not configured
+        }
       } catch (error) {
         console.error("Failed to fetch statistics:", error);
       } finally {
@@ -150,6 +159,12 @@ export default function AdminDashboardPage() {
   const totalRepoProcessed = dashboardStats?.repositoryStats.reduce((sum, s) => sum + s.processedCount, 0) || 0;
   const totalNewUsers = dashboardStats?.userStats.reduce((sum, s) => sum + s.newUserCount, 0) || 0;
 
+  const mcpChartData = mcpStats?.dailyUsages.map((stat) => ({
+    date: new Date(stat.date).toLocaleDateString(locale === 'zh' ? 'zh-CN' : locale, { month: "short", day: "numeric" }),
+    [t('admin.dashboard.mcpRequests')]: stat.requestCount,
+    [t('admin.dashboard.mcpErrors')]: stat.errorCount,
+  })) || [];
+
   return (
     <div className="space-y-6">
       <div className="flex items-center justify-between">
@@ -164,7 +179,7 @@ export default function AdminDashboardPage() {
       </div>
 
       {/* 统计卡片 */}
-      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
+      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
         <Card className="p-6">
           <div className="flex items-center gap-4">
             <div className="rounded-full bg-blue-100 p-3 dark:bg-blue-900">
@@ -209,6 +224,17 @@ export default function AdminDashboardPage() {
             </div>
           </div>
         </Card>
+        <Card className="p-6">
+          <div className="flex items-center gap-4">
+            <div className="rounded-full bg-cyan-100 p-3 dark:bg-cyan-900">
+              <Globe className="h-6 w-6 text-cyan-600 dark:text-cyan-400" />
+            </div>
+            <div>
+              <p className="text-sm text-muted-foreground">{t('admin.dashboard.mcpTotal')}</p>
+              <p className="text-2xl font-bold">{(mcpStats?.totalRequests || 0).toLocaleString()}</p>
+            </div>
+          </div>
+        </Card>
       </div>
 
       {/* 图表区域 */}
@@ -266,6 +292,33 @@ export default function AdminDashboardPage() {
           </ResponsiveContainer>
         </Card>
 
+        {/* MCP 使用图表 */}
+        <Card className="p-6">
+          <h3 className="mb-4 text-lg font-semibold">{t('admin.dashboard.mcpTrend')}</h3>
+          <ResponsiveContainer width="100%" height={300}>
+            <BarChart data={mcpChartData} barCategoryGap="20%">
+              <XAxis
+                dataKey="date"
+                axisLine={false}
+                tickLine={false}
+                tick={{ fill: '#888', fontSize: 12 }}
+              />
+              <YAxis
+                axisLine={false}
+                tickLine={false}
+                tick={{ fill: '#888', fontSize: 12 }}
+              />
+              <Tooltip
+                cursor={{ fill: 'rgba(0,0,0,0.04)' }}
+                content={<CustomTooltip totalLabel={t('admin.dashboard.total')} />}
+              />
+              <Legend wrapperStyle={{ paddingTop: 16 }} />
+              <Bar dataKey={t('admin.dashboard.mcpRequests')} fill="#06b6d4" radius={[4, 4, 0, 0]} />
+              <Bar dataKey={t('admin.dashboard.mcpErrors')} fill="#ef4444" radius={[4, 4, 0, 0]} />
+            </BarChart>
+          </ResponsiveContainer>
+        </Card>
+
         {/* Token 消耗图表 */}
         <Card className="p-6 lg:col-span-2">
           <h3 className="mb-4 text-lg font-semibold">{t('admin.dashboard.tokenTrend')}</h3>
diff --git a/web/app/admin/repositories/[id]/_components/utils.ts b/web/app/admin/repositories/[id]/_components/utils.ts
new file mode 100644
index 0000000..260732b
--- /dev/null
+++ b/web/app/admin/repositories/[id]/_components/utils.ts
@@ -0,0 +1,79 @@
+import type { RepoTreeNode } from "@/types/repository";
+import type { AdminIncrementalTask } from "@/lib/admin-api";
+import { getIncrementalUpdateTask } from "@/lib/admin-api";
+
+export interface DocOption {
+  title: string;
+  slug: string;
+}
+
+export function flattenDocNodes(nodes: RepoTreeNode[]): DocOption[] {
+  const docs: DocOption[] = [];
+  const walk = (list: RepoTreeNode[]) => {
+    list.forEach((node) => {
+      if (node.children && node.children.length > 0) {
+        walk(node.children);
+        return;
+      }
+      docs.push({ title: node.title, slug: node.slug });
+    });
+  };
+  walk(nodes);
+  return docs;
+}
+
+export function findNodeTrail(nodes: RepoTreeNode[], targetSlug: string, trail: string[] = []): string[] | null {
+  for (const node of nodes) {
+    const nextTrail = [...trail, node.slug];
+    if (node.slug === targetSlug) {
+      return nextTrail;
+    }
+    if (node.children?.length) {
+      const result = findNodeTrail(node.children, targetSlug, nextTrail);
+      if (result) {
+        return result;
+      }
+    }
+  }
+  return null;
+}
+
+export function statusBadgeClass(status: string) {
+  const value = status.toLowerCase();
+  if (value === "completed" || value === "已完成") return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200";
+  if (value === "processing" || value === "处理中") return "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200";
+  if (value === "pending" || value === "待处理") return "bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200";
+  if (value === "failed" || value === "失败") return "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200";
+  if (value === "cancelled" || value === "已取消") return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200";
+  return "bg-muted text-muted-foreground";
+}
+
+export function mapTaskStatusToAdminTask(
+  source: Awaited<ReturnType<typeof getIncrementalUpdateTask>>
+): AdminIncrementalTask {
+  return {
+    taskId: source.taskId,
+    branchId: source.branchId,
+    branchName: source.branchName,
+    status: source.status,
+    priority: source.priority,
+    isManualTrigger: source.isManualTrigger,
+    retryCount: source.retryCount,
+    previousCommitId: source.previousCommitId,
+    targetCommitId: source.targetCommitId,
+    errorMessage: source.errorMessage,
+    createdAt: source.createdAt,
+    startedAt: source.startedAt,
+    completedAt: source.completedAt,
+  };
+}
+
+export function normalizeTaskStatus(status: string) {
+  const value = status.toLowerCase();
+  if (value.includes("completed") || value.includes("完成")) return "completed";
+  if (value.includes("processing") || value.includes("处理")) return "processing";
+  if (value.includes("pending") || value.includes("待")) return "pending";
+  if (value.includes("failed") || value.includes("失败")) return "failed";
+  if (value.includes("cancel") || value.includes("取消")) return "cancelled";
+  return "other";
+}
diff --git a/web/app/sidebar.tsx b/web/app/sidebar.tsx
index 08af4ca..bbcee84 100644
--- a/web/app/sidebar.tsx
+++ b/web/app/sidebar.tsx
@@ -8,6 +8,7 @@ import {
     Bookmark,
     Building2,
     AppWindow,
+    Zap,
 } from "lucide-react";
 import {
     Sidebar,
@@ -56,6 +57,7 @@ const itemKeys = [
     { key: "bookmarks", url: "/bookmarks", icon: Bookmark, requireAuth: true },
     { key: "organizations", url: "/organizations", icon: Building2, requireAuth: false },
     { key: "apps", url: "/apps", icon: AppWindow, requireAuth: true },
+    { key: "mcp", url: "/mcp", icon: Zap, requireAuth: false },
 ];
 
 interface AppSidebarProps extends React.ComponentProps<typeof Sidebar> {
diff --git a/web/components/admin/admin-sidebar.tsx b/web/components/admin/admin-sidebar.tsx
index c4e3e3a..042b607 100644
--- a/web/components/admin/admin-sidebar.tsx
+++ b/web/components/admin/admin-sidebar.tsx
@@ -60,6 +60,7 @@ const getNavItems = (t: (key: string) => string): NavItem[] => [
       { href: "/admin/tools/mcps", label: t("common.admin.mcps") },
       { href: "/admin/tools/skills", label: t("common.admin.skills") },
       { href: "/admin/tools/models", label: t("common.admin.models") },
+      { href: "/admin/mcp-providers", label: t("admin.mcpProviders.title") },
     ],
   },
   {
diff --git a/web/components/repo/repository-submit-form.tsx b/web/components/repo/repository-submit-form.tsx
index 74f82bf..2c8e825 100644
--- a/web/components/repo/repository-submit-form.tsx
+++ b/web/components/repo/repository-submit-form.tsx
@@ -21,7 +21,7 @@ interface RepositorySubmitFormProps {
   onSuccess?: () => void;
 }
 
-const GIT_URL_REGEX = /^(https?:\/\/|git@)[\w.-]+[/:].+?(\.git)?$/i;
+const GIT_URL_REGEX = /^((https?:\/\/|git@)[\w.-]+[/:].+?(\.git)?|file:\/\/.+|\/.+)$/i;
 
 const SUPPORTED_LANGUAGES = [
   { code: "en", label: "languages.en" },
@@ -41,6 +41,14 @@ function parseGitUrl(url: string): { orgName: string; repoName: string } | null
     return { orgName: sshMatch[1], repoName: sshMatch[2] };
   }
   
+  const localMatch = url.match(/^(?:file:\/\/|\/)(.+)$/i);
+  if (localMatch) {
+    const parts = localMatch[1].split(/[\/\\]/).filter(Boolean);
+    const repoName = parts[parts.length - 1]?.replace(/\.git$/i, '') || 'local-repo';
+    const orgName = parts.length > 1 ? parts[parts.length - 2] : 'local';
+    return { orgName, repoName };
+  }
+  
   return null;
 }
 
diff --git a/web/i18n/messages/en/admin.json b/web/i18n/messages/en/admin.json
index 2a37b7e..14c594f 100644
--- a/web/i18n/messages/en/admin.json
+++ b/web/i18n/messages/en/admin.json
@@ -14,7 +14,11 @@
     "submitCount": "Submissions",
     "inputToken": "Input Tokens",
     "outputToken": "Output Tokens",
-    "total": "Total"
+    "total": "Total",
+    "mcpTotal": "MCP Requests",
+    "mcpTrend": "MCP Usage Trend",
+    "mcpRequests": "Requests",
+    "mcpErrors": "Errors"
   },
   "departments": {
     "title": "Department Management",
@@ -230,6 +234,44 @@
     "fieldMaxReconnectAttempts": "Max Reconnect Attempts",
     "fieldEncryptMode": "Encryption Mode"
   },
+  "mcpProviders": {
+    "title": "MCP Providers",
+    "description": "Manage MCP provider configurations, models, and access settings",
+    "create": "Create Provider",
+    "edit": "Edit Provider",
+    "empty": "No MCP providers configured",
+    "providersTab": "Providers",
+    "logsTab": "Usage Logs",
+    "getApiKey": "Get API Key",
+    "dailyLimit": "Daily limit",
+    "fieldName": "Provider Name",
+    "fieldServerUrl": "Server URL",
+    "fieldDescription": "Description",
+    "fieldTransportType": "Transport Type",
+    "fieldModel": "AI Model",
+    "noModel": "No model",
+    "fieldRequiresApiKey": "Requires User API Key",
+    "fieldRequiresApiKeyHint": "When enabled, users must provide their own API Key",
+    "fieldApiKeyObtainUrl": "API Key Obtain URL",
+    "fieldSystemApiKey": "System API Key",
+    "fieldRequestTypes": "Request Types (JSON)",
+    "fieldAllowedTools": "Allowed Tools (JSON)",
+    "fieldSortOrder": "Sort Order",
+    "fieldDailyLimit": "Daily Request Limit",
+    "unlimited": "Unlimited",
+    "fieldIconUrl": "Icon URL",
+    "fieldIsActive": "Enable this provider",
+    "dialogDescription": "Configure MCP provider settings, model, and access control",
+    "deleteTitle": "Delete Provider",
+    "deleteDescription": "Are you sure you want to delete this MCP provider? This action cannot be undone.",
+    "logTime": "Time",
+    "logUser": "User",
+    "logTool": "Tool",
+    "logStatus": "Status",
+    "logDuration": "Duration",
+    "logTotal": "Total records",
+    "noLogs": "No usage logs yet"
+  },
   "mcps": {
     "title": "MCPs Management",
     "createMcp": "Create MCP",
diff --git a/web/i18n/messages/en/common.json b/web/i18n/messages/en/common.json
index eb9470e..37635e0 100644
--- a/web/i18n/messages/en/common.json
+++ b/web/i18n/messages/en/common.json
@@ -11,6 +11,8 @@
   "delete": "Delete",
   "edit": "Edit",
   "search": "Search",
+  "previous": "Previous",
+  "next": "Next",
   "loading": "Loading...",
   "error": "Error",
   "success": "Success",
@@ -117,6 +119,34 @@
   "language": {
     "selectLanguage": "Select language"
   },
+  "mcp": {
+    "title": "MCP Providers",
+    "description": "Connect your AI tools to powerful MCP services. Browse available providers and follow the integration guides to get started.",
+    "featureStandardTitle": "MCP Standard",
+    "featureStandardDesc": "Built on the open Model Context Protocol standard, compatible with Claude, Cursor, and other AI clients.",
+    "featureAuthTitle": "Flexible Auth",
+    "featureAuthDesc": "Supports both system API keys and user-provided keys. You control access.",
+    "featureToolsTitle": "Rich Tools",
+    "featureToolsDesc": "Each provider exposes a set of tools your AI can call to perform real-world tasks.",
+    "availableProviders": "Available Providers",
+    "noProviders": "No providers available",
+    "noProvidersHint": "Administrators can add MCP providers in the admin panel.",
+    "requiresApiKey": "API Key Required",
+    "reqPerDay": "req/day",
+    "tabConfig": "Configuration",
+    "tabClaude": "Claude Desktop",
+    "tabCursor": "Cursor",
+    "tabTools": "Available Tools",
+    "serverUrl": "Server URL",
+    "transport": "Transport",
+    "apiKeyRequired": "API Key Required",
+    "apiKeyHint": "You need to provide your own API key in the Authorization header when connecting.",
+    "getApiKey": "Get API Key",
+    "claudeHint": "Add this to your Claude Desktop config file, then restart Claude.",
+    "cursorHint": "Add this to your Cursor MCP config file (.cursor/mcp.json).",
+    "generalGuide": "API Reference",
+    "generalGuideDesc": "You can also fetch the provider list programmatically via the public API endpoint."
+  },
   "mermaid": {
     "zoomOut": "Zoom out",
     "zoomIn": "Zoom in",
diff --git a/web/i18n/messages/en/sidebar.json b/web/i18n/messages/en/sidebar.json
index 38fd0d4..427dace 100644
--- a/web/i18n/messages/en/sidebar.json
+++ b/web/i18n/messages/en/sidebar.json
@@ -6,6 +6,7 @@
   "bookmarks": "Bookmarks",
   "organizations": "Organizations",
   "apps": "My Apps",
+  "mcp": "MCP Services",
   "github": "GitHub",
   "feishu": "Feishu",
   "scanQrCode": "Scan to follow Feishu"
diff --git a/web/i18n/messages/ja/admin.json b/web/i18n/messages/ja/admin.json
index 9fdad8c..79d603c 100644
--- a/web/i18n/messages/ja/admin.json
+++ b/web/i18n/messages/ja/admin.json
@@ -14,7 +14,11 @@
     "submitCount": "提出数",
     "inputToken": "入力トークン",
     "outputToken": "出力トークン",
-    "total": "合計"
+    "total": "合計",
+    "mcpTotal": "MCPリクエスト",
+    "mcpTrend": "MCP使用トレンド",
+    "mcpRequests": "リクエスト",
+    "mcpErrors": "エラー"
   },
   "departments": {
     "title": "部門管理",
@@ -230,6 +234,44 @@
     "fieldMaxReconnectAttempts": "最大再接続回数",
     "fieldEncryptMode": "暗号化モード"
   },
+  "mcpProviders": {
+    "title": "MCPプロバイダー",
+    "description": "MCPプロバイダーの設定、モデル、アクセス設定を管理",
+    "create": "プロバイダー作成",
+    "edit": "プロバイダー編集",
+    "empty": "MCPプロバイダーが設定されていません",
+    "providersTab": "プロバイダー",
+    "logsTab": "使用ログ",
+    "getApiKey": "APIキーを取得",
+    "dailyLimit": "日次制限",
+    "fieldName": "プロバイダー名",
+    "fieldServerUrl": "サーバーURL",
+    "fieldDescription": "説明",
+    "fieldTransportType": "トランスポート",
+    "fieldModel": "AIモデル",
+    "noModel": "モデルなし",
+    "fieldRequiresApiKey": "ユーザーAPIキーが必要",
+    "fieldRequiresApiKeyHint": "有効にすると、ユーザーは自分のAPIキーを提供する必要があります",
+    "fieldApiKeyObtainUrl": "APIキー取得URL",
+    "fieldSystemApiKey": "システムAPIキー",
+    "fieldRequestTypes": "リクエストタイプ (JSON)",
+    "fieldAllowedTools": "許可ツール (JSON)",
+    "fieldSortOrder": "並び順",
+    "fieldDailyLimit": "日次リクエスト制限",
+    "unlimited": "無制限",
+    "fieldIconUrl": "アイコンURL",
+    "fieldIsActive": "このプロバイダーを有効にする",
+    "dialogDescription": "MCPプロバイダーの設定、モデル、アクセス制御を構成",
+    "deleteTitle": "プロバイダー削除",
+    "deleteDescription": "このMCPプロバイダーを削除しますか？この操作は取り消せません。",
+    "logTime": "時刻",
+    "logUser": "ユーザー",
+    "logTool": "ツール",
+    "logStatus": "ステータス",
+    "logDuration": "所要時間",
+    "logTotal": "総レコード数",
+    "noLogs": "使用ログがありません"
+  },
   "mcps": {
     "title": "MCP管理",
     "createMcp": "MCP作成",
diff --git a/web/i18n/messages/ja/common.json b/web/i18n/messages/ja/common.json
index 209669c..bd98623 100644
--- a/web/i18n/messages/ja/common.json
+++ b/web/i18n/messages/ja/common.json
@@ -117,6 +117,34 @@
   "language": {
     "selectLanguage": "言語を選択"
   },
+  "mcp": {
+    "title": "MCP プロバイダー",
+    "description": "AI ツールを強力な MCP サービスに接続しましょう。利用可能なプロバイダーを確認し、統合ガイドに従ってすぐに始められます。",
+    "featureStandardTitle": "MCP 標準",
+    "featureStandardDesc": "Claude や Cursor などの主要 AI クライアントと互換性のある Model Context Protocol 標準に基づいています。",
+    "featureAuthTitle": "柔軟な認証",
+    "featureAuthDesc": "システム API Key とユーザー提供 Key の両方をサポートし、アクセス権限を自分で制御できます。",
+    "featureToolsTitle": "豊富なツール",
+    "featureToolsDesc": "各プロバイダーは実際の業務を実行できるツールセットを公開します。",
+    "availableProviders": "利用可能なプロバイダー",
+    "noProviders": "利用可能なプロバイダーがありません",
+    "noProvidersHint": "管理者は管理パネルで MCP プロバイダーを追加できます。",
+    "requiresApiKey": "API Key 必須",
+    "reqPerDay": "回/日",
+    "tabConfig": "設定",
+    "tabClaude": "Claude Desktop",
+    "tabCursor": "Cursor",
+    "tabTools": "利用可能ツール",
+    "serverUrl": "サーバー URL",
+    "transport": "トランスポート",
+    "apiKeyRequired": "API Key 必須",
+    "apiKeyHint": "接続時は Authorization ヘッダーに自分の API Key を設定してください。",
+    "getApiKey": "API Key を取得",
+    "claudeHint": "上記設定を Claude Desktop の設定ファイルに追加し、再起動してください。",
+    "cursorHint": "上記設定を Cursor MCP 設定ファイル (.cursor/mcp.json) に追加してください。",
+    "generalGuide": "API リファレンス",
+    "generalGuideDesc": "公開 API エンドポイントを通じてプログラムでプロバイダー一覧を取得できます。"
+  },
   "mermaid": {
     "zoomOut": "縮小",
     "zoomIn": "拡大",
diff --git a/web/i18n/messages/ja/sidebar.json b/web/i18n/messages/ja/sidebar.json
index af2d8f5..ffb2fa8 100644
--- a/web/i18n/messages/ja/sidebar.json
+++ b/web/i18n/messages/ja/sidebar.json
@@ -6,6 +6,7 @@
   "bookmarks": "ブックマーク",
   "organizations": "組織",
   "apps": "マイアプリ",
+  "mcp": "MCPサービス",
   "github": "GitHub",
   "feishu": "Feishu",
   "scanQrCode": "QRコードをスキャンしてフォロー"
diff --git a/web/i18n/messages/ko/admin.json b/web/i18n/messages/ko/admin.json
index 0ec2f86..9d5f480 100644
--- a/web/i18n/messages/ko/admin.json
+++ b/web/i18n/messages/ko/admin.json
@@ -14,7 +14,11 @@
     "submitCount": "제출 수",
     "inputToken": "입력 토큰",
     "outputToken": "출력 토큰",
-    "total": "합계"
+    "total": "합계",
+    "mcpTotal": "MCP 요청",
+    "mcpTrend": "MCP 사용 추세",
+    "mcpRequests": "요청",
+    "mcpErrors": "오류"
   },
   "departments": {
     "title": "부서 관리",
@@ -230,6 +234,44 @@
     "fieldMaxReconnectAttempts": "최대 재연결 횟수",
     "fieldEncryptMode": "암호화 모드"
   },
+  "mcpProviders": {
+    "title": "MCP 제공자",
+    "description": "MCP 제공자 설정, 모델 및 접근 설정 관리",
+    "create": "제공자 생성",
+    "edit": "제공자 편집",
+    "empty": "구성된 MCP 제공자가 없습니다",
+    "providersTab": "제공자",
+    "logsTab": "사용 로그",
+    "getApiKey": "API 키 가져오기",
+    "dailyLimit": "일일 제한",
+    "fieldName": "제공자 이름",
+    "fieldServerUrl": "서버 URL",
+    "fieldDescription": "설명",
+    "fieldTransportType": "전송 유형",
+    "fieldModel": "AI 모델",
+    "noModel": "모델 없음",
+    "fieldRequiresApiKey": "사용자 API 키 필요",
+    "fieldRequiresApiKeyHint": "활성화하면 사용자가 자체 API 키를 제공해야 합니다",
+    "fieldApiKeyObtainUrl": "API 키 획득 URL",
+    "fieldSystemApiKey": "시스템 API 키",
+    "fieldRequestTypes": "요청 유형 (JSON)",
+    "fieldAllowedTools": "허용 도구 (JSON)",
+    "fieldSortOrder": "정렬 순서",
+    "fieldDailyLimit": "일일 요청 제한",
+    "unlimited": "무제한",
+    "fieldIconUrl": "아이콘 URL",
+    "fieldIsActive": "이 제공자 활성화",
+    "dialogDescription": "MCP 제공자 설정, 모델 및 접근 제어 구성",
+    "deleteTitle": "제공자 삭제",
+    "deleteDescription": "이 MCP 제공자를 삭제하시겠습니까? 이 작업은 취소할 수 없습니다.",
+    "logTime": "시간",
+    "logUser": "사용자",
+    "logTool": "도구",
+    "logStatus": "상태",
+    "logDuration": "소요 시간",
+    "logTotal": "총 레코드",
+    "noLogs": "사용 로그가 없습니다"
+  },
   "mcps": {
     "title": "MCP 관리",
     "createMcp": "MCP 생성",
diff --git a/web/i18n/messages/ko/common.json b/web/i18n/messages/ko/common.json
index 89551db..5514829 100644
--- a/web/i18n/messages/ko/common.json
+++ b/web/i18n/messages/ko/common.json
@@ -117,6 +117,34 @@
   "language": {
     "selectLanguage": "언어 선택"
   },
+  "mcp": {
+    "title": "MCP 제공자",
+    "description": "AI 도구를 강력한 MCP 서비스에 연결하세요. 제공자를 살펴보고 통합 가이드를 따라 바로 시작하세요.",
+    "featureStandardTitle": "MCP 표준",
+    "featureStandardDesc": "Claude, Cursor 등 주요 AI 클라이언트와 호환되는 Model Context Protocol 표준 기반입니다.",
+    "featureAuthTitle": "유연한 인증",
+    "featureAuthDesc": "시스템 API Key와 사용자 제공 Key 두 가지 방식을 지원하며 접근 권한을 직접 제어할 수 있습니다.",
+    "featureToolsTitle": "풍부한 도구",
+    "featureToolsDesc": "각 제공자는 실제 업무를 수행할 수 있는 도구 세트를 노출합니다.",
+    "availableProviders": "사용 가능한 제공자",
+    "noProviders": "사용 가능한 제공자가 없습니다",
+    "noProvidersHint": "관리자는 관리자 패널에서 MCP 제공자를 추가할 수 있습니다.",
+    "requiresApiKey": "API Key 필요",
+    "reqPerDay": "회/일",
+    "tabConfig": "구성",
+    "tabClaude": "Claude Desktop",
+    "tabCursor": "Cursor",
+    "tabTools": "사용 가능 도구",
+    "serverUrl": "서버 URL",
+    "transport": "전송 방식",
+    "apiKeyRequired": "API Key 필요",
+    "apiKeyHint": "연결 시 Authorization 헤더에 본인의 API Key를 넣어야 합니다.",
+    "getApiKey": "API Key 받기",
+    "claudeHint": "위 구성을 Claude Desktop 설정 파일에 추가한 뒤 Claude를 재시작하세요.",
+    "cursorHint": "위 구성을 Cursor MCP 설정 파일(.cursor/mcp.json)에 추가하세요.",
+    "generalGuide": "API 참조",
+    "generalGuideDesc": "공개 API 엔드포인트를 통해 프로그램으로 제공자 목록을 조회할 수 있습니다."
+  },
   "mermaid": {
     "zoomOut": "축소",
     "zoomIn": "확대",
diff --git a/web/i18n/messages/ko/sidebar.json b/web/i18n/messages/ko/sidebar.json
index c9360a4..8b722e4 100644
--- a/web/i18n/messages/ko/sidebar.json
+++ b/web/i18n/messages/ko/sidebar.json
@@ -6,6 +6,7 @@
   "bookmarks": "북마크",
   "organizations": "조직",
   "apps": "내 앱",
+  "mcp": "MCP 서비스",
   "github": "GitHub",
   "feishu": "Feishu",
   "scanQrCode": "QR 코드를 스캔하여 팔로우"
diff --git a/web/i18n/messages/zh/admin.json b/web/i18n/messages/zh/admin.json
index 45aa496..fa53db9 100644
--- a/web/i18n/messages/zh/admin.json
+++ b/web/i18n/messages/zh/admin.json
@@ -14,7 +14,11 @@
     "submitCount": "提交数",
     "inputToken": "输入Token",
     "outputToken": "输出Token",
-    "total": "合计"
+    "total": "合计",
+    "mcpTotal": "MCP 请求数",
+    "mcpTrend": "MCP 使用趋势",
+    "mcpRequests": "请求数",
+    "mcpErrors": "错误数"
   },
   "departments": {
     "title": "部门管理",
@@ -230,6 +234,44 @@
     "fieldMaxReconnectAttempts": "最大重连次数",
     "fieldEncryptMode": "加密模式"
   },
+  "mcpProviders": {
+    "title": "MCP 提供商",
+    "description": "管理 MCP 提供商配置、模型和访问设置",
+    "create": "新增提供商",
+    "edit": "编辑提供商",
+    "empty": "暂无 MCP 提供商配置",
+    "providersTab": "提供商",
+    "logsTab": "使用日志",
+    "getApiKey": "获取 API Key",
+    "dailyLimit": "每日限额",
+    "fieldName": "提供商名称",
+    "fieldServerUrl": "服务端点地址",
+    "fieldDescription": "描述",
+    "fieldTransportType": "传输方式",
+    "fieldModel": "AI 模型",
+    "noModel": "不关联模型",
+    "fieldRequiresApiKey": "需要用户提供 API Key",
+    "fieldRequiresApiKeyHint": "启用后，用户必须自行提供 API Key 才能使用",
+    "fieldApiKeyObtainUrl": "获取 API Key 的地址",
+    "fieldSystemApiKey": "系统 API Key",
+    "fieldRequestTypes": "请求类型 (JSON)",
+    "fieldAllowedTools": "允许的工具 (JSON)",
+    "fieldSortOrder": "排序",
+    "fieldDailyLimit": "每日请求限额",
+    "unlimited": "无限制",
+    "fieldIconUrl": "图标 URL",
+    "fieldIsActive": "启用此提供商",
+    "dialogDescription": "配置 MCP 提供商的设置、模型和访问控制",
+    "deleteTitle": "删除提供商",
+    "deleteDescription": "确定要删除此 MCP 提供商吗？此操作无法撤销。",
+    "logTime": "时间",
+    "logUser": "用户",
+    "logTool": "工具",
+    "logStatus": "状态",
+    "logDuration": "耗时",
+    "logTotal": "总记录数",
+    "noLogs": "暂无使用日志"
+  },
   "mcps": {
     "title": "MCPs 管理",
     "createMcp": "新增 MCP",
diff --git a/web/i18n/messages/zh/common.json b/web/i18n/messages/zh/common.json
index 1e9b005..7d126d2 100644
--- a/web/i18n/messages/zh/common.json
+++ b/web/i18n/messages/zh/common.json
@@ -9,6 +9,8 @@
   "delete": "删除",
   "edit": "编辑",
   "search": "搜索",
+  "previous": "上一页",
+  "next": "下一页",
   "loading": "加载中...",
   "error": "错误",
   "success": "成功",
@@ -115,6 +117,34 @@
   "language": {
     "selectLanguage": "选择语言"
   },
+  "mcp": {
+    "title": "MCP 提供商",
+    "description": "将你的 AI 工具连接到强大的 MCP 服务。浏览可用提供商，按照接入指南快速上手。",
+    "featureStandardTitle": "MCP 标准协议",
+    "featureStandardDesc": "基于开放的 Model Context Protocol 标准，兼容 Claude、Cursor 等主流 AI 客户端。",
+    "featureAuthTitle": "灵活授权",
+    "featureAuthDesc": "支持系统 API Key 和用户自带 Key 两种模式，访问权限由管理员统一管控。",
+    "featureToolsTitle": "丰富工具集",
+    "featureToolsDesc": "每个提供商暴露一组工具，AI 可直接调用来完成真实世界的任务。",
+    "availableProviders": "可用提供商",
+    "noProviders": "暂无可用提供商",
+    "noProvidersHint": "管理员可在管理后台添加 MCP 提供商。",
+    "requiresApiKey": "需要 API Key",
+    "reqPerDay": "次/天",
+    "tabConfig": "配置信息",
+    "tabClaude": "Claude Desktop",
+    "tabCursor": "Cursor",
+    "tabTools": "可用工具",
+    "serverUrl": "服务端点",
+    "transport": "传输方式",
+    "apiKeyRequired": "需要 API Key",
+    "apiKeyHint": "连接时需要在 Authorization 请求头中提供你自己的 API Key。",
+    "getApiKey": "获取 API Key",
+    "claudeHint": "将以上配置添加到 Claude Desktop 配置文件，然后重启 Claude。",
+    "cursorHint": "将以上配置添加到 Cursor 的 MCP 配置文件（.cursor/mcp.json）。",
+    "generalGuide": "API 参考",
+    "generalGuideDesc": "你也可以通过公开 API 端点以编程方式获取提供商列表。"
+  },
   "mermaid": {
     "zoomOut": "缩小",
     "zoomIn": "放大",
diff --git a/web/i18n/messages/zh/sidebar.json b/web/i18n/messages/zh/sidebar.json
index e6c85b0..acfcb4a 100644
--- a/web/i18n/messages/zh/sidebar.json
+++ b/web/i18n/messages/zh/sidebar.json
@@ -6,6 +6,7 @@
   "bookmarks": "收藏夹",
   "organizations": "机构目录",
   "apps": "我的应用",
+  "mcp": "MCP 服务",
   "github": "GitHub",
   "feishu": "飞书",
   "scanQrCode": "扫码关注飞书"
diff --git a/web/i18n/request.ts b/web/i18n/request.ts
index a86f15d..06b337d 100644
--- a/web/i18n/request.ts
+++ b/web/i18n/request.ts
@@ -50,7 +50,7 @@ export default getRequestConfig(async ({ requestLocale }) => {
   let locale = await requestLocale;
   
   if (!locale || !locales.includes(locale as Locale)) {
-    locale = 'zh';
+    locale = 'en';
   }
 
   return {
diff --git a/web/lib/admin-api.ts b/web/lib/admin-api.ts
index 5243c23..f6964cc 100644
--- a/web/lib/admin-api.ts
+++ b/web/lib/admin-api.ts
@@ -1009,3 +1009,143 @@ export async function updateChatAssistantConfig(
   });
   return result.data;
 }
+
+// ==================== MCP Provider API ====================
+
+export interface McpProvider {
+  id: string;
+  name: string;
+  description?: string;
+  serverUrl: string;
+  transportType: string;
+  requiresApiKey: boolean;
+  apiKeyObtainUrl?: string;
+  hasSystemApiKey: boolean;
+  modelConfigId?: string;
+  modelConfigName?: string;
+  isActive: boolean;
+  sortOrder: number;
+  iconUrl?: string;
+  maxRequestsPerDay: number;
+  createdAt: string;
+}
+
+export interface McpProviderRequest {
+  name: string;
+  description?: string;
+  serverUrl: string;
+  transportType: string;
+  requiresApiKey: boolean;
+  apiKeyObtainUrl?: string;
+  systemApiKey?: string;
+  modelConfigId?: string;
+  isActive: boolean;
+  sortOrder: number;
+  iconUrl?: string;
+  maxRequestsPerDay: number;
+}
+
+export interface McpUsageLog {
+  id: string;
+  userId?: string;
+  userName?: string;
+  mcpProviderId?: string;
+  mcpProviderName?: string;
+  toolName: string;
+  requestSummary?: string;
+  responseStatus: number;
+  durationMs: number;
+  inputTokens: number;
+  outputTokens: number;
+  ipAddress?: string;
+  errorMessage?: string;
+  createdAt: string;
+}
+
+export interface McpUsageLogFilter {
+  mcpProviderId?: string;
+  userId?: string;
+  toolName?: string;
+  page: number;
+  pageSize: number;
+}
+
+export interface PagedResult<T> {
+  items: T[];
+  total: number;
+  page: number;
+  pageSize: number;
+}
+
+export interface McpDailyUsage {
+  date: string;
+  requestCount: number;
+  successCount: number;
+  errorCount: number;
+  inputTokens: number;
+  outputTokens: number;
+}
+
+export interface McpUsageStatistics {
+  dailyUsages: McpDailyUsage[];
+  totalRequests: number;
+  totalSuccessful: number;
+  totalErrors: number;
+  totalInputTokens: number;
+  totalOutputTokens: number;
+}
+
+export async function getMcpProviders(): Promise<McpProvider[]> {
+  const url = buildApiUrl("/api/admin/mcp-providers");
+  const result = await fetchWithAuth(url);
+  return result.data;
+}
+
+export async function createMcpProvider(
+  data: McpProviderRequest
+): Promise<McpProvider> {
+  const url = buildApiUrl("/api/admin/mcp-providers");
+  const result = await fetchWithAuth(url, {
+    method: "POST",
+    body: JSON.stringify(data),
+  });
+  return result.data;
+}
+
+export async function updateMcpProvider(
+  id: string,
+  data: McpProviderRequest
+): Promise<void> {
+  const url = buildApiUrl(`/api/admin/mcp-providers/${id}`);
+  await fetchWithAuth(url, {
+    method: "PUT",
+    body: JSON.stringify(data),
+  });
+}
+
+export async function deleteMcpProvider(id: string): Promise<void> {
+  const url = buildApiUrl(`/api/admin/mcp-providers/${id}`);
+  await fetchWithAuth(url, { method: "DELETE" });
+}
+
+export async function getMcpUsageLogs(
+  filter: McpUsageLogFilter
+): Promise<PagedResult<McpUsageLog>> {
+  const params = new URLSearchParams();
+  if (filter.mcpProviderId) params.set("mcpProviderId", filter.mcpProviderId);
+  if (filter.userId) params.set("userId", filter.userId);
+  if (filter.toolName) params.set("toolName", filter.toolName);
+  params.set("page", String(filter.page));
+  params.set("pageSize", String(filter.pageSize));
+  const url = buildApiUrl(`/api/admin/mcp-providers/usage-logs?${params}`);
+  const result = await fetchWithAuth(url);
+  return result.data;
+}
+
+export async function getMcpUsageStatistics(
+  days: number
+): Promise<McpUsageStatistics> {
+  const url = buildApiUrl(`/api/admin/statistics/mcp-usage?days=${days}`);
+  const result = await fetchWithAuth(url);
+  return result.data;
+}
diff --git a/web/middleware.ts b/web/middleware.ts
index 9cb9d9f..04e7fc8 100644
--- a/web/middleware.ts
+++ b/web/middleware.ts
@@ -9,8 +9,8 @@ export function middleware(request: NextRequest) {
   // 从 cookie 中获取语言设置
   const cookieLocale = request.cookies.get('NEXT_LOCALE')?.value;
   
-  // 优先级：URL lang 参数 > cookie > 默认 zh
-  let locale = 'zh';
+  // 优先级：URL lang 参数 > cookie > 默认 en
+  let locale = 'en';
   if (urlLang && supportedLocales.includes(urlLang)) {
     locale = urlLang;
   } else if (cookieLocale && supportedLocales.includes(cookieLocale)) {

```