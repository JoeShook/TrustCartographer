---
name: reference-running-the-app
description: How to run the TrustCartographer Aspire app and the dashboard-token / port-collision gotchas
metadata: 
  node_type: memory
  type: reference
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

Run the whole thing via the Aspire AppHost:
`dotnet run --project src/TrustCartographer.AppHost`

Fixed endpoints (from AppHost `launchSettings.json`):
- **Web + proxy:** `https://localhost:7257` — entry point. UIs: `/` (TC.Web), `/issuer/`, `/holder/`, `/verifier/`, plus `/live-flow` and `/practitioner-license`.
- **Aspire dashboard:** `https://localhost:17051` — login URL with a `?t=<token>` printed in the console **on every launch**.

Gotchas (these caused repeated login failures):
- **Only ONE AppHost at a time.** Two AppHosts collide on the fixed ports 7257/17051; whichever binds first wins, the other can't bind.
- **The dashboard login token rotates every launch** and only the token from the process that actually bound 17051 works. A bookmarked/old-tab token will fail — always copy the `…/login?t=…` from the current run's console.
- The Aspire dashboard runs as a **detached child**; `taskkill /T` on the AppHost can miss it, leaving an orphan dashboard. If logins act weird, kill stray `dotnet.exe` whose command line matches `Aspire.Dashboard` or `TrustCartographer.(Issuer|Holder|Verifier|Web|AppHost)`, confirm 7257/17051 are free, then start one fresh.
- Building requires the AppHost stopped first (Windows DLL file locks). See [[feedback-no-background-apphost]].
- Optional: set `ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true` in AppHost launch settings to drop the login token entirely (local-only).
