# TrustCartographer — project context for Claude

Interactive visualizer for SSI / decentralized-identity and trust-model concepts,
built to teach by *showing real protocol traffic*, not just diagrams.

## End goal
A comparative understanding of **SSI vs Google A2A vs UDAP** that makes the case for
UDAP's strengths in agent-to-agent / cross-org auth. The build is the teaching vehicle:
issue → hold → present → verify a real credential, then contrast trust models.

## Who I'm working with
Joe Shook — built the UDAP.org SDKs, reference implementations, and visualizers. Deep in
trust frameworks, authorization chains, cross-org trust (X.509, FHIR). **Assume expert
fluency**; don't over-explain crypto/PKI basics. UDAP advantages to surface eventually:
Authorization Extension Object (purpose-of-use), Certifications & Endorsements (third-party
trust), Trust Communities (governance).

## Architecture (.NET 10 + Aspire)
Orchestrated by `src/TrustCartographer.AppHost/AppHost.cs`:
- **Core** (`src/TrustCartographer.Core/`) — crypto + protocol: `CryptoService` (Ed25519),
  `DidKeyService` (did:key encode/decode), `VcService` (JWT-VC issue/verify),
  `Oid4Vci/` (issuance), `Oid4Vp/` (`VpService` = presentation/verification).
- **UI** (`src/TrustCartographer.UI/`) — Razor Class Library (MAUI-portable). Shared:
  `SsiSequenceDiagram`, `JwtViewer`, `AppShell`. Pages: `PractitionerLicense`, `LiveFlow`.
- **Web** (`src/TrustCartographer.Web/`) — Blazor host **+ YARP reverse proxy**. Owns
  `/issuer/* /holder/* /verifier/*` (PathRemovePrefix + sets `X-Forwarded-Prefix`).
  `TrafficLoggingMiddleware` captures every proxied hop into `TrafficLog` for Live Flow.
- **Issuer / Holder / Verifier** — three real services, each with its own Blazor UI:
  - Issuer: OID4VCI 1.0 issuer (Overview / Mint / Activity).
  - Holder: wallet — `WalletClient` (issuance) + `PresentationClient` (presentation);
    UI Overview / Wallet / Redeem.
  - Verifier: OID4VP 1.0 verifier (Overview / Request / Activity).
- **ServiceDefaults** — Aspire OTel/health/discovery.

**Blazor-behind-proxy invariant:** because YARP strips the `/issuer|/holder|/verifier`
prefix, each service reinstates it from `X-Forwarded-Prefix` into `Request.PathBase`
(middleware in its `Program.cs`), `App.razor` emits a dynamic `<base href>`, and all
nav/links are relative. Don't reintroduce absolute `/foo` links or a static `<base href="/">`.

## Two learning surfaces (the core pedagogy)
- **`/practitioner-license`** — a *local simulation* of the whole trust triangle (runs the
  crypto in-process, animated, with tamper toggles). Teaches the concepts.
- **`/live-flow`** — replays the **real** captured HTTP (issuance + verification) as the same
  animated `SsiSequenceDiagram`. Buttons: Trigger issuance / Trigger verification / Run full
  demo. Open the 📨 on any step to inspect actual request/response bytes. Actors:
  browser · issuer · holder · verifier.

## Status (2026-05-31) and phases
Phases 1–4 ✅ — crypto, did:key, JWT-VCs, and the **live trust triangle** (real OID4VCI
issuance + real OID4VP verification through the proxy; all four services have UIs).

**Deliberate teaching hook:** the Verifier resolves *any* issuer `did:key` and verifies the
signature, but has **no trust list** — it proves *who* signed, not that they're authoritative.
Surfaced on the Verifier Overview page on purpose. This is the on-ramp to:

**Phase 5 (NEXT, the end goal):** trust frameworks + comparative views — issuer trust lists /
UDAP trust communities, and side-by-side **SSI vs Google A2A vs UDAP**.

**In progress — "Portable Identity Assurance in Healthcare" program** (implements the Templeton/Cambia
proposal; cited+linked, not copied). A grouped suite of workflows under `/pia`, built in phases.
*Phase A1 done:* `Core/SdJwt/SdJwtService` (SD-JWT VC selective disclosure — SDK-grade), `SdJwtViewer`,
and the live **Minor Age Check** walkthrough (`/pia/minor-age-check`, reveal `age_over_18` only + tamper
toggles). Backlog: delegated proofing + FAST IDIAL assurance, the 3 issuance pathways, custody
transfer, verification→OIDC token, and the Tier-4 trust list (the UDAP-comparison pivot). Deep-dives in
`docs/use-cases/portable-identity-assurance/`.

## Running it
`dotnet run --project src/TrustCartographer.AppHost`
- Entry/proxy: `https://localhost:7257` → `/`, `/issuer/`, `/holder/`, `/verifier/`,
  `/live-flow`, `/practitioner-license`, `/pia` (Portable Identity Assurance hub).
- Aspire dashboard: `https://localhost:17051` — login URL with `?t=<token>` printed each launch.

**Gotchas (caused real pain — heed them):**
- Only **one** AppHost at a time. Two collide on the fixed ports 7257/17051; the loser can't bind.
- The dashboard **login token rotates every launch**; only the token from the process that bound
  17051 works. Use the `…/login?t=…` from the *current* run's console, not a bookmarked tab.
- The Aspire dashboard is a **detached child**; `taskkill /T` can leave an orphan. If logins act
  up, kill stray `dotnet.exe` matching `Aspire.Dashboard` / `TrustCartographer.*`, confirm
  7257/17051 free, start one fresh.
- Building needs the AppHost **stopped** first (Windows DLL file locks).

## Working agreements
- **Do NOT leave a background AppHost running** after a turn, and don't start one while Joe may be
  running his — it triggers the port/token collision above. Prefer `dotnet build` to verify, or
  curl against an instance he's already running; if you must start it, kill it (tree + dashboard
  child) in the same turn.
- Commits go on `master` (Joe's established pattern). End commit messages with the
  `Co-Authored-By` trailer. **Don't push unless asked** — Joe pushes manually.
- LF→CRLF git warnings on Windows are cosmetic.
- Security constraint: do not port any Surescripts.* code/packages/namespaces; reimplement fresh.

## More committed docs
- `PLAN.md` — full phased plan.
- `docs/` — Sigil/TrustCartographer relationship, PKI-vs-SSI mapping, roadmap, walkthroughs, Q&A.
- `specs/` — curated W3C/IETF/OpenID/DIF/UDAP specs, indexed by `specs/README.md`.

> Note: there is also a machine-local Claude auto-memory under
> `~/.claude/projects/.../memory/` that mirrors much of this. It does NOT sync across machines —
> this committed `CLAUDE.md` is the portable source of truth. Keep it current when the
> architecture, status, or working agreements change.
