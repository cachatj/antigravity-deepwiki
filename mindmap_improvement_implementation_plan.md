# Mind Map Improvement — Implementation Plan

Status: **proposed, not started** (drafted 2026-09-30)

Goal: replace DeepWiki's plain markdown-outline mind map with architecture / data-flow diagrams at the visual level of [datadef.io](https://datadef.io): nested containers (e.g. Lakehouse > Bronze/Silver/Gold > tables, or Resource Group > VNet > Subnet > services), table nodes with typed columns and PK/FK badges, colored tier "families", and clean orthogonal edges.

Reference inputs used for this plan:
- datadef.io preview: https://datadef.io/preview/fabric-lakehouse-claims-denial-risk-abd5cd43
- datadef export JSON: `datadef-project-fabric-lakehouse---claims-denial-risk-20260930T191539.json` (format v3.0.0)
- AzureCraft / ArchitectureHelper: https://github.com/MCKRUZ/ArchitectureHelper (see `.claude/agents/azure-architect.md`)

---

## 1. Findings: what datadef.io is built with

Chrome remote debugging was off, so browser-use couldn't attach. Instead, the page and all 22 of its Next.js JS/CSS chunks were downloaded and searched for library signatures.

| Layer | Evidence in their bundles |
|---|---|
| Framework | Next.js 16 (App Router, Turbopack chunks) with Tailwind |
| Diagram engine | **React Flow (`@xyflow/react`)**: `@xyflow` imports and ~30 `react-flow__*` class names (`node-group`, `resize-control`, `edgelabel-renderer`, …), plus `getSmoothStepPath` and `NodeResizer` |
| Edge routing | **libavoid-js** (a WebAssembly build of libavoid): `AvoidLib.load("/libavoid.wasm")` (492 KB, served at datadef.io/libavoid.wasm), called with `OrthogonalRouting`, `shapeBufferDistance: 18`, `idealNudgingDistance: 12`, `nudgeOrthogonalSegmentsConnectedToShapes` and `nudgeSharedPathsWithCommonEndPoint`. This is why their lines are clean right angles that fan apart instead of overlapping. |
| State and icons | zustand and lucide |
| Layout | No ELK or dagre in the bundles. The export JSON has an `aiPrompt` holding an ASCII sketch and absolute coordinates, so an LLM plans the layout and their own code places the nodes. |
| Look | Every node carries a `family` token (bronze/silver/gold/neutral/ingest) that sets its colors. Table names are in JetBrains Mono, body text in Inter, and each column row gets PK/FK badges. |

To inspect a site live next time, enable `chrome://inspect/#remote-debugging` so browser-use can attach.

## 2. Decision: React Flow + ELK, not Konva

AzureCraft uses Konva and argues that React Flow's `parentId` model (child positions relative to the parent) doesn't work for Azure's nested containers. That argument doesn't hold for DeepWiki:

- **React Flow handles deep nesting.** The datadef export nests three levels deep using `parentId`: Lakehouse, then BRONZE/SILVER/GOLD, then tables. That is the same shape as Resource Group > VNet > Subnet.
- **The relative-position cost is mainly an editor problem.** It hurts when a user drags a service from one subnet into another. AzureCraft is a drag-and-drop editor; DeepWiki generates read-only diagrams.
- **ELK's output fits React Flow's model.** ELK lays out nested graphs and returns child coordinates relative to their parent, which is exactly what `parentId` expects.
- **The AzureCraft style is still possible.** React Flow can keep every node absolute and draw containers as background nodes whose bounds are computed from their children.
- **Konva doesn't take us off React.** AzureCraft almost certainly uses it through react-konva, and DeepWiki's frontend is Next 16 / React 19 either way.

What Konva would cost DeepWiki:

- **Everything is pixels.** Text can't be selected or found with Ctrl+F, screen readers can't read it, and there is no real SVG export.
- **We'd rebuild the basics.** Edges, labels, handles, routing, minimap and fit-to-view would all be our code.
- **It repeats the current problem.** `web/components/repo/mind-map-viewer.tsx` is already a hand-drawn `<canvas>` with its own tree layout. Its "SVG export" is just the PNG wrapped in an `<image>` tag. That is a big part of why it looks plain.

To keep the choice reversible, **store a diagram spec that is independent of the renderer**. The LLM describes what exists and how it connects, ELK computes coordinates, and any renderer can draw the result: React Flow, Konva, or static SVG for the offline export.

## 3. Current state in DeepWiki

- **Entity:** `BranchLanguage.MindMapContent` (string) and `MindMapStatus` (Pending/Processing/Completed/Failed) in `src/OpenDeepWiki.Entities/Repositories/BranchLanguage.cs`.
- **Tool:** `src/OpenDeepWiki/Agents/Tools/MindMapTool.cs` stores a `#/##/###` markdown outline. It rejects content that is empty or has no `#`. A trailing `:path` is kept as a file link.
- **Generation:** `MindMapWorker` (a hosted service, registered in `Program.cs`) calls `WikiGenerator.GenerateMindMapAsync` (`src/OpenDeepWiki/Services/Wiki/WikiGenerator.cs`, ~line 113). That loads the `mindmap-generator` prompt with GitTool and MindMapTool. The mind map is translated in `TranslateMindMapAsync`.
- **Unused prompts:** `FilePromptPlugin.cs:44` loads exactly `{promptName}.md`, so only `prompts/mindmap-generator.md` runs. `mindmap-generator azure.md` and `mindmap-generator v2.md` are never used.
- **API:** `MindMapApiService` serves `GET /api/v1/repos/{owner}/{repo}/mindmap`.
- **Frontend:** `web/app/[owner]/[repo]/mindmap/page.tsx` renders `components/repo/mindmap-page-content.tsx`, which renders `mind-map-viewer.tsx` (hand-rolled canvas). The sidebar link is in `repo-shell.tsx`.
- **Export:** `WikiHtmlExportService` does not export mind maps. Its bundled assets are only mermaid and highlight.js.
- **Diagram libraries:** only `mermaid ^11.12.2` and `recharts ^3.7.0`. There is no xyflow, ELK or d3. Next is 16.1.4 and React is 19.2.3.

## 4. Implementation plan

### Step 1: Pick the prompt deliberately
Make the mind map prompt name configurable (appsettings or per repository), or fold the Azure/Fabric prompt in as a project-type branch of `mindmap-generator.md`, so the recent prompt work actually runs.

### Step 2: Define a renderer-independent diagram spec
Base it on the datadef v3 export, minus the coordinates:

```jsonc
{
  "version": 1,
  "title": "Fabric Lakehouse · Claims Denial Risk",
  "direction": "RIGHT",
  "nodes": [
    { "id": "lakehouse", "kind": "group", "label": "Fabric Lakehouse · lh_claims_denial", "family": "neutral", "icon": "cloud" },
    { "id": "bronze", "kind": "group", "parent": "lakehouse", "label": "BRONZE · raw and reference", "family": "bronze" },
    { "id": "raw_837", "kind": "table", "parent": "bronze", "label": "raw_837_{client}", "family": "bronze",
      "description": "Raw EDI 837 claim transactions.",
      "columns": [ { "name": "client", "type": "string" }, { "name": "edi_payload", "type": "variant" } ],
      "source": "notebooks/ingest.py" },
    { "id": "nb0", "kind": "pipeline", "parent": "silver", "label": "Notebook 0 · training preparation", "serviceId": "fabric-notebook" },
    { "id": "note1", "kind": "note", "label": "client is a namespace column, not a table per client" }
  ],
  "edges": [
    { "source": "raw_837", "target": "nb0", "kind": "lineage", "label": "raw 837", "style": "solid" }
  ]
}
```

- Node kinds: `group`, `table`, `service`, `pipeline`, `note`.
- Edge kinds: `lineage`, `transform`, `lookup`, `network`. Styles: `solid`, `dashed`, `dotted`.
- `columns[]` entries can carry `pk` and `fk` flags.
- `source` keeps today's "link to source file" behavior.
- **No coordinates.** The LLM is poor at geometry; the fractional positions and overlaps in the datadef export show that. ELK is good at it.

### Step 3: Backend tool, storage and validation
- Add `WriteArchitectureDiagramAsync`, either alongside `MindMapTool` or as a new `ArchitectureDiagramTool`.
- It validates on the server:
  - The JSON parses and the IDs are unique.
  - Every edge endpoint and every `parent` exists.
  - The parent chain has no cycles.
  - Node and edge counts stay under a cap (e.g. 150 nodes).
- It returns specific `ERROR:` messages so the agent can fix its own output, the same way `MindMapTool` rejects bad input today.
- Storage: add a `DiagramContent` column, which means a migration for both the SQLite and Postgres providers. Alternatively, add a format discriminator to `MindMapContent`.
- Keep the markdown mind map as a fallback when no diagram is produced.
- Extend `MindMapApiService` (or add a sibling endpoint) to return the spec.
- Translation: translate only `label`, `description` and note text, never `id`s.

### Step 4: Frontend renderer
Location: `web/components/repo/architecture-diagram/`.

- Dependencies: `@xyflow/react` and `elkjs`.
- ELK options: `elk.algorithm: layered`, `elk.direction: RIGHT`, `elk.hierarchyHandling: INCLUDE_CHILDREN`, with node and layer spacing tuned so the result looks like datadef.
- Map ELK output to React Flow nodes: set `parentId` from the spec, set `position` from ELK's relative x/y, and list parents before children in the nodes array.
- Custom node types:
  - `GroupNode`: a tier-colored container with a header label.
  - `TableNode`: a header plus column rows, with the type right-aligned and PK/FK badges.
  - `ServiceNode`: shows an Azure icon.
  - `PipelineNode`: a compact pill with its technology subtitle.
  - `NoteNode`
- The `family` values (bronze, silver, gold, neutral, ingest) become CSS variables, with dark-mode variants. Use JetBrains Mono for names and Inter for body text.
- Edges: start with React Flow's built-in smooth-step edges and edge-label renderer. Later polish: libavoid-js orthogonal routing, the technique datadef uses.
- Keep the current viewer's features: zoom and pan, a list view, and PNG/SVG export via `html-to-image` (a real SVG export this time).
- Wire the new renderer into `mindmap-page-content.tsx`: use it when a diagram spec exists, and fall back to `MindMapViewer` otherwise.

### Step 5: Azure icons and the architect prompt
- Map `serviceId` to Microsoft's official Azure Architecture Icons, which Microsoft publishes for use in architecture diagrams.
- Reuse the useful parts of AzureCraft's `azure-architect.md`: the LLM outputs nodes and edges instead of a drawing, and it flags Well-Architected Framework concerns, which can appear as `note` nodes on the diagram.

### Step 6: Offline HTML export
- After ELK runs in the browser, save the computed positions back to the backend (a small POST endpoint).
- `WikiHtmlExportService` then writes a static SVG from the saved coordinates with no JS, consistent with the existing mermaid and highlight.js assets.

### Suggested first milestone
Render the datadef JSON listed above through a React Flow + ELK prototype inside DeepWiki. This validates the visual result before any backend, prompt or migration work.

## 5. Licenses
- React Flow: MIT.
- elkjs: EPL-2.0. Fine to bundle.
- libavoid-js: believed to be LGPL. Workable as a separately loaded WASM file, but confirm before shipping.
- Konva (only if revisited): MIT.
