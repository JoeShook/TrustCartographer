# TrustCartographer - Project Memory

## Overview
- **Repo**: github.com/joeshook/TrustCartographer
- **Tech**: .NET 10, Blazor Server (Interactive), BouncyCastle, System.IdentityModel.Tokens.Jwt, SimpleBase
- **Purpose**: Interactive visualizer for blockchain, SSI, and trust model concepts
- **End goal**: Comparative understanding of SSI vs Google A2A vs UDAP to argue for UDAP's strengths in agent-to-agent auth

## User Background
- Built UDAP.org SDKs, reference implementations, and visualizers
- Deep experience with trust frameworks, authorization chains, cross-org trust (X.509, FHIR)
- Key UDAP advantages to demonstrate: Authorization Extension Object (purpose-of-use), Certifications & Endorsements (third-party trust), Trust Communities (governance)

## Solution Structure
Now a **.NET Aspire** multi-service solution (orchestrated by `src/TrustCartographer.AppHost/AppHost.cs`):
- `src/TrustCartographer.Core/` — crypto + protocol services & models: CryptoService, DidKeyService, VcService; `Oid4Vci/` (issuance) and `Oid4Vp/` (VpService = presentation/verification) namespaces.
- `src/TrustCartographer.UI/` — Razor Class Library; shared components incl. `SsiSequenceDiagram`, `JwtViewer`, `AppShell`; pages `PractitionerLicense` + `LiveFlow`. MAUI-portable.
- `src/TrustCartographer.Web/` — Blazor Server host **and YARP reverse proxy**: owns `/issuer/* /holder/* /verifier/*` (PathRemovePrefix + X-Forwarded-Prefix), `TrafficLoggingMiddleware` captures every proxied hop into `TrafficLog` for the Live Flow page.
- `src/TrustCartographer.Issuer/` — real OID4VCI 1.0 issuer (endpoints + Blazor UI: Overview/Mint/Activity).
- `src/TrustCartographer.Holder/` — wallet: `WalletClient` (issuance) + `PresentationClient` (presentation), endpoints + Blazor UI (Overview/Wallet/Redeem).
- `src/TrustCartographer.Verifier/` — real OID4VP 1.0 verifier (endpoints + Blazor UI: Overview/Request/Activity).
- `src/TrustCartographer.ServiceDefaults/` — Aspire defaults (OTel, health, discovery).
- See [project-rcl-layout.md](project_rcl_layout.md), [Blazor behind proxy](project_blazor_behind_proxy.md), and [How to run](reference_running_the_app.md).
- `PLAN.md` — full project plan with phases, comparisons, and resources.

## Current Status (2026-05-31)
- **Live trust triangle complete**: real OID4VCI issuance (Issuer→Holder) + real OID4VP verification (Holder→Verifier), all over HTTP through the YARP proxy. All four services have Blazor UIs.
- **Two learning surfaces**: `/practitioner-license` is a *local simulation* of the whole triangle (animated, tamper toggles); `/live-flow` replays the *real* captured HTTP as the same animated sequence diagram (Trigger issuance / Trigger verification / Run full demo).
- **PIA program started** (implements the Cambia paper; cited+linked, not copied): grouped workflows under `/pia`. Phase A1 done — `Core/SdJwt/SdJwtService` (SD-JWT selective disclosure, SDK-grade) + `SdJwtViewer` + live Minor Age Check (`/pia/minor-age-check`). Backlog: delegated proofing+IDIAL, issuance pathways A/B/C, custody transfer, verify→OIDC token, Tier-4 trust list. Docs: `docs/use-cases/portable-identity-assurance/`. See [[reference-cambia-minor-identity-proposal]].
- **Deliberate teaching hook**: the Verifier trusts any did:key issuer (no trust list) — the on-ramp to the comparative end goal. See [Trust triangle complete](project_trust_triangle_complete.md).
- **Next**: Phase 5 — trust frameworks + comparative views (SSI vs Google A2A vs UDAP), e.g. issuer trust lists / UDAP communities.

## Phases Summary
1. Crypto Fundamentals — key pairs, signing, hashing ✅
2. DIDs — did:key + DID Documents ✅
3. Verifiable Credentials — issue/hold/verify (JWT-VC) ✅
4. Trust Triangle — issuance (OID4VCI) + verification (OID4VP), live + animated ✅
5. Comparative Views — SSI vs Google A2A vs UDAP; trust lists/communities (NEXT, end goal)

## Index
- [Sigil relationship](project_sigil_relationship.md) — Sigil PKI tool as proposed trust-material backend for TrustCartographer DIDs/VCs
- [Specs folder](reference_specs.md) — curated W3C/IETF/OpenID/DIF/UDAP/SSRAA specs at `specs/`, indexed by `specs/README.md`
- [Design docs folder](reference_design_docs.md) — `docs/` with Sigil/TrustCartographer relationship, PKI-vs-SSI mapping, phased roadmap, captured Q&A
- [Walkthroughs](reference_walkthroughs.md) — interactive Razor pages + paired learning guides; reusable `SsiSequenceDiagram` component; Practitioner License live at `/practitioner-license`
- [RCL layout](project_rcl_layout.md) — three-project structure (Core / UI RCL / Web host); MAUI-portable; `AddAdditionalAssemblies` gotcha
- [Blazor behind proxy](project_blazor_behind_proxy.md) — making Issuer/Holder Blazor UIs work under YARP path prefix (X-Forwarded-Prefix → PathBase → dynamic base href + relative links)
- [Trust triangle complete](project_trust_triangle_complete.md) — OID4VCI issuance + OID4VP verification live + animated; verifier's missing trust list is the deliberate UDAP hook
- [How to run](reference_running_the_app.md) — Aspire AppHost, ports 7257/17051, dashboard token rotates per launch, one-instance-only gotcha
- [No background AppHost](feedback_no_background_apphost.md) — don't leave verification AppHosts running; they collide with the user's run and break dashboard login
- [Cambia minor-identity proposal](reference_cambia_minor_identity_proposal.md) — external OID4VC/SD-JWT healthcare proposal that maps onto the trust triangle + Phase 5; a comparative foil to UDAP
