# Antigravity-DeepWiki: Implementation Feasibility & Plan

## Context

I want to create **antigravity-deepwiki** — a lightweight, local-first extension that generates living documentation for any codebase opened in Antigravity (or VS Code). It is derived from the [OpenDeepWiki](https://github.com/AIDotNet/OpenDeepWiki) project, which uses LLM prompts to scan a git repo and produce structured wiki pages.

I have already:
- Cloned and modified OpenDeepWiki to run locally
- Configured it to use `gemini-2.0-pro` as the LLM backend
- Successfully generated wiki pages for local repositories

I am now starting fresh from a **clean clone** (not a fork) of OpenDeepWiki to build a stripped-down, standalone tool with no upstream dependency.

---

## Goal

Strip OpenDeepWiki down to its **bare essentials** and rebuild it as a lightweight, modular documentation framework with these properties:

1. **Local-first**: All generated content lives inside the repo itself (e.g., `deepwiki/` folder containing markdown files and diagrams), not in an external database or cloned copy
2. **Persona-driven**: Different team roles see different default views of the same codebase documentation
3. **IDE-native**: Accessible entirely within Antigravity or VS Code — no separate web app required
4. **Publishable**: Markdown output is directly compatible with Confluence, GitHub Pages, or any docs-as-code pipeline
5. **Secure**: No repo cloning to external locations, no telemetry, no network calls except to the configured LLM API

---

## Persona System

Each user is mapped to a persona (via git username, config file, or dropdown selector). The persona determines which wiki sections are shown by default and which prompt templates generate the content.

| Persona | Default View | Key Interests |
|---------|-------------|---------------|
| **Data Engineer** | System architecture, data pipelines, CI/CD, infrastructure diagrams | Schema mappings, Airflow DAGs, deployment configs, performance |
| **Data Scientist** | Feature engineering, model specs, statistical methods, test/eval results | Model accuracy, feature importance, validation approaches, notebooks |
| **Project Analyst / PM** | Changelog, sprint progress, timeline, compliance, JIRA integration | Recent changes, remaining work, deployment status, budget/compliance |
| **Business User** | Plain-language overviews, decision rationale, output descriptions | What does this produce? Why was it built this way? How do I request changes? |

### How Persona Prompts Work

OpenDeepWiki already uses a prompt pipeline to scan repos and generate wiki content. The modification is:
- Extract and organize all generation prompts into a `prompts/` directory as individual markdown templates
- Create persona-specific variants of each prompt (e.g., `prompts/architecture/data-engineer.md`, `prompts/architecture/business-user.md`)
- The persona selection determines which prompt set runs, producing documentation tailored to that audience
- Users can switch personas via a simple dropdown to see the same codebase documented differently

---

## Architecture Requirements

### What to Keep from OpenDeepWiki
- The core prompt pipeline that scans repos and generates structured documentation
- The chunking/context strategy for feeding code to the LLM
- The document structure templates (table of contents, page hierarchy)
- Git integration for detecting changes and triggering regeneration

### What to Remove
- The web application (Next.js frontend, ASP.NET backend)
- The database layer (PostgreSQL/SQLite for storing wiki content)
- The repo cloning mechanism (we already have the repo open locally)
- Docker/container orchestration (unnecessary for IDE extension)
- Any telemetry, analytics, or external service calls
- User authentication system (handled by IDE/git identity)

### What to Build New
- **IDE Extension**: Panel/sidebar in Antigravity/VS Code that renders the generated markdown
- **Persona Selector**: Dropdown or config-based persona switching
- **Local Storage**: All output goes to `deepwiki/` folder in the repo root:
  ```
  deepwiki/
  ├── prompts/           # Prompt templates per persona
  ├── docs/              # Generated markdown pages
  ├── diagrams/          # Generated Mermaid/architecture diagrams
  ├── config.yaml        # Persona mappings, LLM settings, generation options
  └── .deepwiki-cache/   # Cached generation state to avoid re-running unchanged files
  ```
- **Incremental Regeneration**: On commit or manual trigger, only regenerate pages for changed files/modules
- **Confluence Export**: CLI command or button to push markdown to Confluence via API
- **JIRA Panel** (Phase 2): Embedded view of priority tickets, with ability to create feature branches from issues

---

## Key Questions — Please Assess Feasibility

1. **Is this achievable as an Antigravity/VS Code extension?** Can an extension run the prompt pipeline (calling an LLM API, scanning the file tree, writing markdown to disk) without requiring a separate backend server?

2. **What is the minimal viable extraction from OpenDeepWiki?** Identify the specific files/modules from the OpenDeepWiki codebase that contain the core prompt logic, repo scanning, and document generation — everything else can be discarded.

3. **Can Docker be eliminated entirely?** The original uses Docker for the web server and database. If we're storing everything as local markdown files and the LLM is accessed via API, is there any remaining need for containerization? If a container is helpful for the LLM proxy or anything else, can we use Podman instead of Docker for better rootless security?

4. **What is the incremental regeneration strategy?** How should we detect which documentation pages need updating after a code change, without re-scanning the entire repo?

---

## Implementation Plan Request

Please provide a **phased implementation plan** covering:

### Phase 1: Core Extraction (Days 1-3)
- Extract and catalog all prompt templates from OpenDeepWiki into `prompts/` folder
- Identify and isolate the minimal code needed for: repo scanning, LLM calls, document generation
- Remove web app, database, Docker, and authentication layers
- Produce a standalone CLI tool: `deepwiki generate --persona data-engineer --repo .`

### Phase 2: Persona System (Days 4-6)
- Create persona-specific prompt variants for each documentation section
- Implement persona selection via config file and CLI flag
- Build the `deepwiki/` folder structure with proper caching

### Phase 3: IDE Integration (Days 7-10)
- Build Antigravity/VS Code extension that:
  - Renders `deepwiki/docs/` markdown in a sidebar panel
  - Provides persona dropdown selector
  - Offers "Regenerate" button that triggers the CLI
  - Supports search across generated documentation

### Phase 4: Advanced Features (Days 11+)
- Incremental regeneration on file save or commit
- Confluence export command
- JIRA integration panel
- Git hook for auto-regeneration on PR merge

---

## Security Requirements

This tool will be used in **sensitive healthcare analytics environments**. The following are non-negotiable:

- **No repo cloning**: The tool operates on the already-open local repository only
- **No external data transmission**: Only outbound call is to the configured LLM API endpoint
- **No telemetry or analytics**: Strip all tracking code from the OpenDeepWiki base
- **Local storage only**: All generated content stays in the repo's `deepwiki/` folder
- **VPN-compatible**: Must work within corporate network restrictions
- **Auditable prompts**: All prompt templates are version-controlled markdown files visible in the repo

---

## Tech Stack Preferences

- **LLM**: Gemini 2.0 Pro (already configured and working)
- **Language**: Python preferred for CLI tool (familiar to data team), TypeScript for extension
- **No Docker**: Should we eliminate if possible; Podman if containerization is truly needed - what benefits do we get from docker? I dont mind using it - especially if we get the benefit of our own local hosting web server, databases, we could add vector or noSQL semantic knowledge graphs as well.
- **No Database**: File-based storage only (markdown + YAML config + JSON cache)
- **IDE**: Antigravity primary, VS Code secondary compatibility

---

## Deliverable

After assessing feasibility, provide:
1. A clear **yes/no** on whether this is achievable within the constraints above
2. The **specific files/modules** from OpenDeepWiki that form the essential core
3. A **day-by-day implementation plan** with concrete tasks
4. Any **risks or blockers** you foresee
5. Recommended **project structure** for the new antigravity-deepwiki repo
