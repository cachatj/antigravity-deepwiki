# OpenDeepWiki Verification Walkthrough

I've completed the cleanup, localization, and feature enhancement for your OpenDeepWiki setup.

## Key Changes Implemented

- **Native Gemini Support**: Added built-in support for Google Gemini via [AgentFactory.cs](file:///Users/jc-vht/code_sandbox/OpenDeepWiki/src/OpenDeepWiki/Agents/AgentFactory.cs).
- **Documentation Export**: Added a new service to export organized documentation as a ZIP archive of Markdown files.
- **English Default**: Fixed the UI and generator to default to English.
- **Path Cleanup**: Purged stale dot-notation paths and Chinese content from the database.
- **Context Awareness**: Enhanced AI prompts with BigQuery and Airflow domain knowledge.

## How to Verify

### 1. English UI & Local Repository
1. Visit `http://localhost:3000`. The UI should now be in English by default.
2. Log in using the local admin credentials (created during DB initialization):
   - **Email**: `admin@opendeepwiki.com`
   - **Password**: `123456`
3. Submit your workspace repository: `/Users/jc-vht/code_sandbox/OpenDeepWiki`.

### 2. Gemini 2.5 Pro Optimization
With the latest rebuild, Gemini is now the active provider. Here is how to get the best results based on the `ai.google.dev` documentation:

- **Thinking/Reasoning**: Gemini 2.5 Pro (and Gemini 3) has built-in thinking tokens. This is exceptionally powerful for the `WikiGenerator` as it allows the model to "reason" about your folder structure and code dependencies before outputting the catalog.
- **Safety Filters**: If you see empty pages or "Service Error" during generation, check the logs (`docker logs opendeepwiki-opendeepwiki-1`). Gemini's safety filters can sometimes be oversensitive with code analysis.
- **System Instructions**: I've ensured that all prompts are passed as `System Instructions`, which is Google's preferred way to ground Gemini's behavior.

### 3. Export Documentation
Once your documentation is generated, you can download a portable snapshot:
1. Use the following API endpoint (or browser URL):
   `http://localhost:5000/api/v1/wiki/local/OpenDeepWiki/export`
2. This will return a ZIP file (`local_OpenDeepWiki_docs.zip`) containing all your generated Markdown files in their respective folders.

### 4. Path Fixes
Verify that new paths use slashes (e.g., `1-project-overview/1-introduction`) instead of dots. The updated prompts now strictly enforce this.

### Troubleshooting
- **404 Errors**: I've removed the hardcoded `api.routin.ai` defaults that were overriding your environment. The latest build (Step 1281) has applied this fix.
- **Safety Blocks**: If a specific file fails, it might be due to Gemini's safety policy.
- **Manual Reset**: If you still see old paths or Chinese content, run:
  ```bash
  sqlite3 ./data/opendeepwiki.db "DELETE FROM DocCatalogs; DELETE FROM DocFiles; DELETE FROM BranchLanguages;"
  ```
  Then restart the app to trigger a full rescan.
