# Decisions

Decisions made during implementation that add to or depart from [PLAN.md](PLAN.md). PLAN.md stays as the original design; each entry here says what was decided and why.

## 1. The webhook API key is required configuration
*Phase 2, item 1 (2026-10-31)*

The shared secret for `X-Api-Key` is configured as `Telemetry:ApiKey` (environment variable `Telemetry__ApiKey`). The API validates its settings at startup and refuses to start if the key is missing, so a misconfigured deployment fails loudly instead of rejecting every ping with 401. In Development the key is `local-dev-key`, set in `appsettings.Development.json`.
