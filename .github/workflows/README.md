## Pipeline Overview

```
Feature / Bugfix / Hotfix branches
───────────────────────────────────
  feature/* │ bugfix/* │ hotfix/*
        │
        │  push
        ▼
  ┌─────────────────────────┐
  │  ci.yml                 │
  │  validate-and-build     │  ──► artifact: ci-publish-{sha7}  (retained 3 days)
  └─────────────────────────┘
        │
        │  open PR
        ▼
  ┌─────────────────────────┐
  │  Code Review / Approval │
  └─────────────────────────┘
        │
        │  merge to master
        ▼
  ┌─────────────────────────┐
  │  release.yml            │  ──► artifact: 1.0.{run}-pr.{branch}+{sha7}
  │  build & GitHub Release │      GitHub Release: v1.0.{run_number}
  └─────────────────────────┘
        │
        │  MANUAL workflow_dispatch
        ▼
  ┌─────────────────────────┐
  │  deploy-release.yml     │  ──► vm-dev
  └─────────────────────────┘


Supporting workflows (all manual / event-driven)
─────────────────────────────────────────────────

  deploy-branch.yml ──► reuses CI artifact if available ──► vm-dev  (pre-merge testing)

  rollback.yml      ──► reads deployment history ──► calls deploy-release.yml

  reset-env.yml     ──► picks latest release    ──► calls deploy-release.yml

  deployment-status.yml ──► reads all environments ──► step summary report

  cleanup-stale-branches.yml ──► branch hygiene report
```

---

## Setup Checklist

### GitHub Repo Settings

- **Actions > General > Actions permissions**: Allow all actions and reusable workflows
- **Actions > General > Workflow permissions**: Read and write; allow PR creation
- **Secrets**: `BULLFROG_REPO_TOKEN` — fine-grained PAT with `Contents: Read` on `qumbarraza/Bullfrog`

### GitHub Environments

- Create environment: `vm-dev` (no required reviewers for pilot)
- Variables required in each environment:

  | Variable | Purpose |
  |---|---|
  | `APP_POOL` | IIS application pool name to recycle |
  | `SITE_PATH` | Absolute path to IIS site root on the VM |
  | `PUBLIC_URL` | Base URL of the deployed site |

- Optional variables (enable extra behaviour when set):

  | Variable | Purpose |
  |---|---|
  | `HEALTH_CHECK_URL` | Enables post-deploy health check GET request |
  | `BACKUP_ROOT` | Root directory for pre-deploy backups (stage/prod) |
  | `BACKUP_RETENTION` | Number of backup copies to keep (stage/prod) |

### Branch Protection on master

- Require a pull request before merging
- Required status check: `validate-and-build`
- Require branches to be up to date before merging
- Auto-delete head branches after merge

---

## VM Setup Checklist

- Install **.NET 8 Hosting Bundle** (not just the shared framework).
  Verify with: `Get-WebGlobalModule | Where-Object { $_.Name -like "AspNetCore*" }`
- Create an IIS site and application pool with `managedRuntimeVersion=""` (No Managed Code — ASP.NET Core runs out-of-process via the ANCM module)
- Create the directory `C:\RMSApplicationFiles\`
- Register a **self-hosted runner** on the VM with labels: `self-hosted`, `build`, `vm-dev`
- Install and authenticate the **gh CLI** as the runner service account
- **Seed the initial `appsettings.json`** into `SITE_PATH` before the first deploy — deploys never overwrite it
- **Place `ThirdParty.Dummy.dll`** into `SITE_PATH` manually — it is environment-managed and is stripped from all CI/CD artifacts

---

## Workflow Reference

| Filename | Purpose | Trigger |
|---|---|---|
| `ci.yml` | Build, restore, test, publish; upload artifact `ci-publish-{sha7}` | Push to `feature/*`, `bugfix/*`, `hotfix/*`; PR targeting `master` |
| `release.yml` | Build release artifact, create GitHub Release `v1.0.{run_number}` | Push to `master` |
| `deploy-release.yml` | Deploy a specific GitHub Release to a chosen environment | Manual `workflow_dispatch` |
| `deploy-branch.yml` | Deploy a branch artifact (reuses CI artifact if available) to `vm-dev` | Manual `workflow_dispatch` |
| `rollback.yml` | Select a previous release from deployment history and redeploy it | Manual `workflow_dispatch` |
| `reset-env.yml` | Redeploy the latest release to an environment | Manual `workflow_dispatch` |
| `deployment-status.yml` | Report current deployed version and health for all environments | Manual `workflow_dispatch`; scheduled |
| `cleanup-stale-branches.yml` | Report and optionally delete branches with no recent activity | Manual `workflow_dispatch`; scheduled |
| `.github/actions/deploy-iis` | Composite: stop pool, robocopy files, restart pool, optional health check | Called by deploy jobs |
| `.github/actions/link-shared-deps` | Composite: check out `bullfrog/` repo, capture SHA | Called by build jobs |
| `.github/actions/strip-env-managed` | Composite: remove `ThirdParty.Dummy.dll` (and optionally `appsettings.json`) from publish output before upload | Called by build jobs |

---

## Adding More Environments

1. **`deploy-release.yml`** — add the new environment name to the `environment` input `options` list.
2. **`deploy-branch.yml`** — add the new environment name to the `environment` input `options` list.
3. **`rollback.yml`** — add the new environment name to the `environment` input `options` list.
4. **`reset-env.yml`** — add the new environment name to the `environment` input `options` list.
5. **`deployment-status.yml`** — add a new entry to the environment matrix so it is included in the status report.
6. **GitHub Repository Settings** — create the new GitHub Environment under Settings > Environments.
7. **Set required variables** for the new environment: `APP_POOL`, `SITE_PATH`, `PUBLIC_URL`.
8. **Set optional variables** as needed: `HEALTH_CHECK_URL`, `BACKUP_ROOT`, `BACKUP_RETENTION`.
9. **Register a self-hosted runner** on the target VM with a label matching the environment name, or confirm an existing runner covers it.

---

## Trunk Note

The trunk branch for this project is already `master` — no graduation step is required. If a future project is scaffolded using a temporary default branch (e.g. `temp-main` or `main`), the files that must be updated when renaming the trunk are:

- `ci.yml` — push trigger branch filter and PR `base` target
- `release.yml` — push trigger branch filter
- The **branch protection rule** in GitHub repository settings

---

## Open Items

- **Tests**: `dotnet test` in `ci.yml` currently runs with `continue-on-error: true`. Remove this flag once tests are added to the project so that a test failure correctly blocks the build.
- **HEALTH_CHECK_URL**: The variable is not yet set on any environment. Configure it in each GitHub Environment to enable automatic post-deploy health checks after each deployment.
