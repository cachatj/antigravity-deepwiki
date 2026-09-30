# vht-ds-fabric — How the Architecture Works (Current State)

> **Status: as-built, 2026-06-02.** This describes the system *after* the
> repo reorganization (4-wheel split), the fan-out deployer going live, the
> India → East US 2 capacity migration, and the q75 model cut-over.
>
> It is the companion to [`deployment_architecture.md`](deployment_architecture.md),
> which captures the original *design rationale* (written 2026-05-22, when
> deploy_fanout was still "Phase 2" and there were 3 wheels). Where the two
> differ, **this document wins** — it reflects what is actually running.

---

## 1. The 60-second version

We run **one Data Science model (`days_to_payment`) across N per-client Microsoft
Fabric workspaces** (Option C topology — one workspace per PPM client). The git
monorepo is the single source of truth for all *model-agnostic* code; per-client
state (lakehouses, MLflow models) lives only in the workspaces, never in git.

Four things changed recently and together define the current architecture:

1. **Repo reorganized into 4 wheels.** The bronze/silver lakehouse code was
   extracted out of the model into its own shared wheel (`vht_data_lakehouse`),
   so the data-engineering layer is reusable across future models.
2. **Fan-out deployer is live.** `deploy_fanout.py` provisions a new client
   workspace end-to-end and pushes code updates to existing ones — all via REST,
   never via destructive Git sync.
3. **Capacity migrated India → East US 2 (F16).** All workspaces were rebuilt on
   a US East 2 paid capacity; OneLake data residency now follows that region.
4. **Model is now q75 + per-payer conformal**, replacing the old point estimate.

---

## 2. Repository layout — four wheels + one model

```
vht-ds-fabric/
├── shared/
│   ├── vht-fabric-platform/      wheel  vht_fabric_platform   0.1.0
│   │     safety · spark_setup · io_utils · config · paths · run_log
│   │     scripts/deploy_fanout.py        ← THE deployer
│   │     clients/registry.yaml           ← central client list (bulk onboard)
│   │     powerbi/                         ← observability semantic models + reports
│   │
│   ├── vht-claims-domain/        wheel  vht_claims_domain     0.1.0
│   │     revamp_api · schemas · synthetic_data
│   │
│   ├── vht_data_lakehouse/       wheel  vht_data_lakehouse    0.1.0   ← NEW (the extraction)
│   │     src/  bronze · silver_line · silver_claim
│   │     tests/ test_bronze · test_silver_line · test_silver_claim
│   │     *.Notebook/  00, 00a, 01, 02      ← bronze/silver notebooks LIVE HERE NOW
│   │
│   └── infra/revamp-proxy/       Azure Function source (RevAMP NAT proxy)
│
└── models/
    └── days_to_payment/          wheel  vht_ds_days_to_payment 0.5.0
          src/  features · training · inference · delivery (+ bounds)
          config/ client_config.{idg,pmg,aim,example}.yaml
          *.Notebook/  03, 04, 05, 06 + utilities (_watchdog, _q75_backtest, …)
          *.DataPipeline/  run_client_daily · run_client_training
```

All four wheels are installed **`--no-deps`** in the notebooks (AutoGluon is
installed separately, also `--no-deps`, to avoid a sklearn collision that breaks
predictor pickling). Build order is enforced by the Makefile
(`platform → claims → data-lakehouse → model`) and the wheel set is kept in lock-step
with `deploy_fanout.step5_upload_wheels_and_config` — **all four wheels are
required at deploy time.**

### ⚠️ The one thing that surprises everyone: notebooks span two folders

The extraction moved the data-engineering stages out of the model. As a result,
the medallion notebooks now live in **two different git folders**:

| Notebook | Folder | Wheel that owns the logic |
|---|---|---|
| `00_landing_to_bronze` | `shared/vht_data_lakehouse/` | `vht_data_lakehouse` |
| `00a_generate_synthetic_to_landing` | `shared/vht_data_lakehouse/` | `vht_claims_domain` |
| `01_bronze_to_silver_line` | `shared/vht_data_lakehouse/` | `vht_data_lakehouse` |
| `02_silver_line_to_silver_claim` | `shared/vht_data_lakehouse/` | `vht_data_lakehouse` |
| `03_silver_to_gold_features` | `models/days_to_payment/` | `vht_ds_days_to_payment` |
| `04_train_days_to_payment` | `models/days_to_payment/` | `vht_ds_days_to_payment` |
| `05_batch_predict_new_837s` | `models/days_to_payment/` | `vht_ds_days_to_payment` |
| `06_deliver_predictions_to_revamp` | `models/days_to_payment/` | `vht_claims_domain` (RevAMP API) |

A single Fabric workspace, however, holds **one flat set of notebooks** and the
`run_client_daily` pipeline orchestrates all of 00→06. This split is the root
cause of the git-sync rule in §5.

---

## 3. The medallion pipeline (00 → 06)

```
S3 (RevAMP 837/835 drops)
        │  Stage 00  landing → bronze        [data_lakehouse]
        ▼
  bronze.*  (raw 837/835 lines, MERGE-upserted, schema-evolving)
        │  Stage 01  bronze → silver_line    [data_lakehouse]
        ▼
  silver.line  (one row per service line; payer_id backfilled from payer_name)
        │  Stage 02  silver_line → silver_claim   [data_lakehouse]
        ▼
  silver.claim  (one row per claim; days_to_payment + payment_ratio targets)
        │  Stage 03  silver → gold features  [days_to_payment]
        ▼
  gold.features  (model-ready feature rows)
        │  Stage 04  train  ───────────────► MLflow model registry (champion)
        │  Stage 05  batch predict           [days_to_payment]
        ▼
  gold.predictions  (q75 date + conformal bounds per claim)
        │  Stage 06  deliver → RevAMP        [claims_domain, dry_run-gated]
        ▼
  RevAMP API  (currently dry_run=true everywhere — see §9)
```

- **`run_client_daily`** runs 00→06 on a schedule (staggered 5 min apart per
  client so we don't slam the shared capacity with concurrent Spark sessions).
- **`run_client_training`** runs Stage 04 only (reads gold, trains, registers).
- Stages are byte-neutral across the extraction — validated by golden-file diff
  (0 mismatch across 131k+ PMG+AIM claims) before the cut-over merged.

---

## 4. Workspace topology — Option C

One workspace **per client**, plus a shared dev workspace and a central ops
workspace. All on **East US 2** capacity now.

```
        ┌──────────────────────────────────────────────┐
        │  vht-ds-fabric  (git monorepo — source of truth)│
        └───────────────────────┬────────────────────────┘
                                 │  make build (4 wheels)
                                 │  deploy_fanout.py  (REST push, per-workspace overlay)
                                 ▼
   ┌───────────────┬───────────────────┬───────────────┬───── … ─────┐
   │ vht-ds-dev-…  │ vht-ds-ops-eus2   │ vht-ppm-idg   │  vht-ppm-pmg / -aim …
   │ developer     │ observability +   │ PROD client   │  PROD client
   │ sandbox       │ PHI audit + dash. │               │
   │ NOT git-conn* │ NOT git-conn      │ NOT git-conn  │  NOT git-conn
   └───────────────┴───────────────────┴───────────────┴─────────────┘
```

| Workspace | Purpose | Git-connected? |
|---|---|---|
| `vht-ds-dev-eus2` | Developer sandbox; ad-hoc notebooks | **No (as of 2026-06-02)** — see §5 |
| `vht-ds-ops-eus2` | Central run-log Delta, observability semantic models + dashboards, PHI audit | No (it's a sink) |
| `vht-ppm-<client>` (idg, pmg, aim, … → 30 at scale) | Per-tenant data + scheduled runs | **No** — REST-pushed deploys only |

\* **dev used to be git-connected** and is the one place the two-folder split bit
us — see the next section.

---

## 5. How code ships from git to the workspaces

**Principle: git is the source of truth; deployment is a one-way REST push with a
per-workspace overlay. Production workspaces never sync *back* to git.**

```
developer commits to `develop`
        │
        ▼
make build           → 4 wheels in shared/*/dist + models/*/dist
        │
        ▼
deploy_fanout.py     → for each target workspace, via REST only:
   step5   upload 4 wheels + client_config.<ns>.yaml to Files/  (OneLake DFS)
   step5b  push every notebook's SOURCE from git (updateDefinition) — non-destructive
   step6   rewrite each notebook's metadata.dependencies.lakehouse → this ws's lakehouse
   step7   rewrite each pipeline's root param defaults (client, lakehouse_id, workspace_id)
```

`step5b` is the important one for day-to-day work: it **pushes notebook content
from git to an already-onboarded workspace without touching the lakehouse, MLflow
models, or anything else.** It scans **both** notebook folders
(`models/days_to_payment` **and** `shared/vht_data_lakehouse`) and matches each
workspace notebook by its `.platform` displayName — so the GUID-named 00/00a
notebooks resolve correctly.

### The two git-sync rules (learned the hard way)

1. **Never run a full-workspace `updateFromGit` (PreferRemote + allowOverrideItems)
   on a data-bearing workspace.** It deletes every workspace item not in git —
   including the lakehouse and registered models. (This wiped a client lakehouse
   on 2026-05-26.) Code updates go through `step5b`, never through Git sync.

2. **Pipeline-bearing workspaces must be deploy_fanout-managed, NOT git-connected.**
   Because notebooks now live in two folders, a workspace git-connected to
   `/models/days_to_payment` sees 00/00a/01/02 as **deletes** (they moved to the
   other folder) and tries to remove them on every "Update." Since
   `run_client_daily` depends on those notebooks, Fabric blocks the update with
   *"this action will break dependency links."* This is exactly why
   **`vht-ds-dev-eus2` was disconnected from git on 2026-06-02** and is now
   pushed to via the same `step5b` path as the clients.

**Fabric Git is used exactly twice in a workspace's life:** a one-shot
`connect → updateFromGit → disconnect` at creation (step 2, to materialize the
initial items), and never again.

---

## 6. Per-workspace overlay — what gets rewritten

Notebooks committed to git carry the *originating* workspace's lakehouse identity
in their metadata. Without rewriting, `/lakehouse/default/` won't mount in the new
workspace and the wheel bootstrap fails. So every deploy rewrites:

- **Notebook `metadata.dependencies.lakehouse`** → this workspace's
  `default_lakehouse`, `default_lakehouse_name`, `default_lakehouse_workspace_id`,
  `known_lakehouses`. (The metadata-level binding wins over `%%configure`
  parameterName overrides, so this rewrite is mandatory, not optional.)
- **Notebook `%%configure -f` cell `defaultValue` strings** → so standalone
  `fab job run` works without `-P` overrides.
- **Pipeline root parameter defaults** → `client`, `lakehouse_id`, `workspace_id`.

---

## 7. The model — q75 + per-payer conformal

`days_to_payment` is no longer a point estimate. It now predicts a **75th-percentile
"safe by" date** so RevAMP can age claims confidently.

- **AutoGluon `problem_type=quantile`**, `quantile_levels=[0.75]`,
  `eval_metric=pinball_loss`.
- **Trained on Tier-1 payers only** (`train_on_tier1_only`, payers with
  ≥ `tier1_threshold` claims). Thin/low-volume payers are **not modeled**.
- **Mondrian (per-payer) conformal calibration** — per-payer offsets persisted in
  the model's `conformal.json` artifact and applied inside the pyfunc.
- **The 30-day tail is an OPS RULE, not a prediction.** Tier-2 payers get a flat
  `tier2_default_days = 30` business rule. We deliberately do not model them.
- **Registration gate is coverage-based** (first-train always registers — the old
  MAE gate would falsely reject a deliberately-high quantile model).

**"Good" / coverage** is the headline metric: the fraction of claims whose **actual
paid date ≤ predicted q75 date**. Target ≈ 0.75, and crucially *without* inflating
predictions toward the 30-day cap. On the PMG out-of-time backtest this lands ≈ 0.84
with avg predicted ~15 days. See the dashboard in §8.

> **Data-quality caveat baked into the pipeline:** PMG/AIM 837 feeds stopped
> sending `payer_id` ~Jan 2026 (IDG unaffected). `silver_line.backfill_payer_id`
> reconstructs it in-batch from `payer_name` via a most-common-id crosswalk
> (recovers ~94%). The real fix is upstream (escalated to Vijay). Per-payer
> features/conformal depend on this, so it matters.

---

## 8. Observability

`vht-ds-ops-eus2` is the central sink. Built code-first (TMDL + PBIR) and deployed
via REST — no manual Power BI Desktop authoring in the loop:

- **`dtp_monitoring`** — DirectLake semantic model over the ops lakehouse
  (`fact_run_log`, `fact_payer_stats`, `fact_q75_backtest`). Deployed with
  `scripts/deploy_powerbi_model.py`.
- **`dtp_q75_accuracy`** — the q75 validation report: a **claim-level table**
  (Client · Claim ID · DOS · Predicted q75 date · Actual Paid Date · Good ·
  AR Aging Reduction days) plus Good% / Avg-AR / Total-AR / Claims cards.
  *AR Aging Reduction = 30 − predicted days* (the static tickle is 30 days).

> Note: the backtest dashboard validates the q75 *method* on backfilled scratch
> data via a throwaway model. The production champions were trained **before** the
> payer_id backfill, so live predictions won't match the dashboard until
> production silver/gold is re-grained and the champions retrained (§9).

---

## 9. Capacity & data residency

- All workspaces now run on an **East US 2 F16** paid capacity (migrated off the
  former India capacity). **The capacity's region determines OneLake data
  residency.**
- Cross-region capacity *reassignment is hard-blocked* by Fabric — the migration
  was a **build-new-and-copy**, not a reassign. Lakehouses/notebooks/ML items are
  not movable cross-region; data-bearing workspaces had to be recreated in-region
  and data migrated.
- F16 comfortably runs two small clients (PMG + AIM) concurrently on the staggered
  daily schedule.

---

## 10. PHI safety (defense in depth)

A client's identity is ~5 handles, each cross-checked against an independent signal
before any IO; any mismatch raises a fail-closed PHI-safety error.

| Gate | Checks | Where |
|---|---|---|
| `assert_client_consistency` | pipeline `client_namespace` == config namespace == bound lakehouse id/name | top of every medallion notebook |
| `assert_landing_zone_client` | first file under `Files/<ns>/837/` has filename prefix == config `revamp_api.client_id` | Stage 00 |
| `assert_model_client` | model version's `client_namespace` tag == expected namespace | Stage 05 (inference) |
| `assert_egress_client` | RevAMP `client_id` being delivered to == config namespace's id | Stage 06 (delivery) |

Workspace-level RBAC (one lakehouse per tenant) is the Layer-0 boundary under all
of these. `assert_model_client` is dormant until all model versions carry the
`client_namespace` tag (new trains tag it; backfill the existing champions).

---

## 11. Onboarding a new client (the short version)

`deploy_fanout.py` automates this end-to-end; the canonical step-by-step with every
gotcha is the **client onboarding runbook** (memory) and
`models/days_to_payment/docs/onboarding_new_client.md`.

```bash
# single client
python shared/vht-fabric-platform/scripts/deploy_fanout.py --client <ns> \
    --capacity <eus2-capacity-id> --first-train

# bulk (reads the central registry; already-onboarded clients no-op idempotently)
python shared/vht-fabric-platform/scripts/deploy_fanout.py \
    --bulk shared/vht-fabric-platform/clients/registry.yaml --first-train
```

**Order matters** (encoded in the step sequence): create the workspace → one-shot
Git materialize → **create the lakehouse with `enableSchemas: true` AFTER the Git
sync, never before** → upload wheels/config → push notebook source → rewrite
notebook metadata + pipeline params → smoke test → first-train → schedule. The S3
shortcut creation (step 4) is the one genuinely-manual UI step.

---

## 12. Known gaps & parked items (as of 2026-06-02)

| Item | State |
|---|---|
| **`dry_run=false` flip** | **Parked.** All clients deliver in dry-run. Will not flip until RevAMP confirms each client_id (PMG `0007` / AIM `0006` are *inferred from S3 prefixes, not confirmed*), creds are in Key Vault, and the IP allowlist is in place. Wrong client_id = PHI to the wrong tenant. |
| **Production re-grain on payer_id backfill** | Pending. Backfill is merged and validated on scratch only; prod silver/gold not yet re-grained and champions not retrained, so live predictions ≠ dashboard. |
| **Upstream payer_id fix** | Escalated to Vijay; the backfill is a mitigation, not the cure. |
| **AIM q75 dashboard** | Not built (PMG done). |
| **`assert_model_client` activation** | Dormant until existing champion versions are tagged with `client_namespace`. |
| **Pipeline-failure alerting** | All native Fabric options are UI-only (no REST/CLI); watchdog deployed but schedules disabled pending a runtime fix. |

---

## See also

- [`deployment_architecture.md`](deployment_architecture.md) — original design
  rationale (3-wheel era; deploy_fanout as "Phase 2").
- `models/days_to_payment/docs/onboarding_new_client.md` — onboarding runbook.
- `models/days_to_payment/docs/MIGRATION_OPEN_ITEMS.md` — running defect/decision log.
- `shared/vht-fabric-platform/scripts/deploy_fanout.py` — the deployer (read the
  module docstring + the guardrails before running `main()`/steps 1–3 on any
  data-bearing workspace).
