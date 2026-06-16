# Sketch — "Client trust over the same OAuth2/FHIR flow: UDAP JWT vs vLEI/ACDC"

A build sketch (not yet implemented) for the Phase-5 comparative payoff, anchored on the VITAL white
paper's key reframe: the **UDAP vLEI/ACDC Client Authentication** profile is *semantically equivalent* to
**UDAP JWT-Based Client Authentication** (SSRAA IG, Facilitated FHIR under TEFCA) — same OAuth2/FHIR rails,
different client-assertion format. See [gleif-vlei.md](gleif-vlei.md) and
[vital-initiative-capture.md](vital-initiative-capture.md).

## The one-sentence teaching claim

> *A payer's FHIR client authenticates to another payer's token endpoint and pulls a `Patient` — and the
> **only** thing that changes between "UDAP" and "vLEI" is the **client-assertion** it presents and **how the
> Authorization Server establishes trust** in it. Everything else (OAuth2 flow, access token, FHIR call) is
> identical.*

This is the cleanest "SSI vs UDAP" head-to-head in TC because it isn't two different demos — it's **one flow
with a swappable trust mechanism**, which is exactly what the white paper claims.

## What's the same vs different

| Step | UDAP JWT | vLEI / ACDC | Same? |
|---|---|---|---|
| Discover AS metadata | `/.well-known/...` | same | ✅ |
| Build **client assertion** | `private_key_jwt` (signed JWT, `iss=sub=client_id`) | **CESR-encoded IPEX Grant** carrying the vLEI ACDC chain | ❌ **the divergence** |
| Token request | `POST /token` `grant_type=client_credentials` + `client_assertion` | same endpoint, assertion in IPEX form | ✅ shape |
| **AS validates client** | cert chain → **trust-community anchor** (+ optional software statement); verify JWT sig | verify IPEX/ACDC signatures → **chain to GLEIF root** + KERI key state (witness/watcher); read ECR | ❌ **the divergence** |
| Purpose-of-use | UDAP **Authorization Extension Object** | **ECR** credential (exchange purpose) | ≈ analogous |
| Access token issued | bearer/DPoP | same | ✅ |
| FHIR call | `GET /Patient` with token | same | ✅ |

Two boxes differ; the rest is shared. The whole UX should make those two boxes light up.

## Actors / topology (map onto TC)

Reuse the proxy + `SsiSequenceDiagram` + capture engine. New minimal apps (thin samples over Core, per the
SDK north star):

- **`payer-a-client`** — the requesting payer's FHIR client (initiates the token request).
- **`payer-b-as`** — the responding payer's **Authorization Server** (`/token`, pluggable client-auth
  validation) + a tiny **FHIR resource** endpoint (`/Patient`).
- **Trust anchors** (not full apps at first):
  - UDAP: a **trust-community anchor** (CA) + optional **software-statement** issuer (ties to the CMS
    Medicare App Library / CARIN Code of Conduct angle from the video → reuse the
    [software-statements](org-trust-models.md) idea).
  - vLEI: **GLEIF root → QVI → Legal Entity → ECR** credential chain (+ a stand-in for KERI key-state).

Lifelines: `payer-a-client · payer-b-as · (trust anchor) · payer-b-fhir`.

## The SDK seam (the actual reusable bit)

Define one client-auth abstraction in Core and implement it twice — this is the API surface the apps exist
to discover:

```
IClientAuthenticator                 // builds a client assertion for a token request
  -> UdapJwtClientAuthenticator      // private_key_jwt + X.509/community
  -> VleiAcdcClientAuthenticator     // CESR/IPEX grant + ACDC chain

IClientAuthVerifier (on the AS)      // validates an assertion, returns a typed result
  -> UdapJwtVerifier                 // cert chain to community anchor; software statement
  -> VleiAcdcVerifier                // ACDC chain to GLEIF root + key state; ECR purpose
```

Both return the same typed `ClientAuthResult { ClientOrgId, PurposeOfUse, TrustPath[], Checks... }` (the
"every check reported" style of `SdJwtVerifyResult`). The token endpoint is format-agnostic; only the
verifier differs. That *is* the white paper's "semantically equivalent, different assertion format."

## Delivery surfaces (match TC's two-surface pedagogy)

1. **Simulation walkthrough** (`/compare/client-auth`, like `/practitioner-license`): in-process, animated
   side-by-side of both flows with a **toggle** `Client auth: UDAP JWT | vLEI/ACDC`. Tamper toggles:
   wrong/expired cert, untrusted community anchor, broken ACDC chain, revoked QVI, wrong purpose-of-use.
   Teaches the concepts with zero infra.
2. **Live Flow trigger(s)**: "Run client auth (UDAP JWT)" and "Run client auth (vLEI/ACDC)" against the real
   `payer-a-client → payer-b-as → payer-b-fhir`, captured through the proxy and animated. A side panel shows
   the actual **client_assertion bytes** for each (a UDAP `private_key_jwt` vs a CESR/IPEX grant) — the
   visual proof of "same flow, different assertion."

This also finally **fills the deliberate Tier-4 hook**: today TC's verifier has no trust list; here both
mechanisms ARE the trust anchor (community CA vs GLEIF root), shown concretely.

## Staged build plan (smallest first)

- **Stage 0 — conceptual comparative view (cheap, high value).** Pure `SsiSequenceDiagram` data: both flows
  side by side, the two differing steps highlighted, assertion samples shown as static payloads. No new
  crypto. Mostly content + one page. Ship this first.
- **Stage 1 — real UDAP JWT slice.** Implement `UdapJwtClientAuthenticator` + `UdapJwtVerifier` for real
  (`private_key_jwt`; model the community anchor with TC's key infra; note UDAP uses RS256/ES256 + X.509 SAN
  vs TC's Ed25519 — decide whether to add an X.509 suite or model it). New `payer-b-as` (`/token` + `/Patient`)
  + `payer-a-client`. Captured in Live Flow. Joe's home turf — fast.
- **Stage 2 — vLEI/ACDC slice.** Two fidelity options:
  - **(a) Modeled (C#, stays in the SDK):** represent the GLEIF→QVI→LE→ECR chain as chained credentials
    using TC's existing SD-JWT/JWT machinery, an "IPEX-like" grant, and a GLEIF root key as anchor. Teaches
    the *structure* (root-of-trust, chained ACDC, ECR purpose-of-use, selective disclosure) without real
    KERI witnesses/CESR. Best first pass; consistent with the C# SDK north star.
  - **(b) Full fidelity (spike):** orchestrate the open-source **keripy** (KERI/ACDC/CESR/IPEX) as an
    external Aspire-managed service; TC's AS validates a real IPEX Grant. Cross-language, heavy, but real.
    Align with the **OSS UDAP-vLEI client/server libraries expected April 2026** rather than re-deriving.
- **Stage 3 — comparative view polish + governance layer.** Side-by-side scorecard (topology, assertion,
  anchor, revocation, governance) wired to the live runs; fold in the **software-statement / app-vetting**
  facet (CMS Medicare App Library + CARIN Code of Conduct) as a third client-trust lens.

## Risks / unknowns

- **KERI/ACDC/CESR/IPEX has no mature C# stack.** Full fidelity means keripy interop (Stage 2b) or a faithful
  C# model (2a). Don't pretend the model is the real thing — label it.
- **UDAP uses X.509 + RS256/ES256**, TC is Ed25519/`did:key`. Either add an X.509 + RSA/EC suite to Core (a
  good SDK addition) or clearly model it.
- **Scope:** keep `payer-b-as` tiny (token + one FHIR resource). Resist building a full FHIR server.
- Wait for the **April 2026 OSS reference libs** before investing in 2b — they define the canonical wire form.

## Smallest next step

Stage 0 + Stage 1: a `/compare/client-auth` walkthrough page (data-driven `SsiSequenceDiagram`, toggle UDAP
vs vLEI, sample assertions) **plus** the real UDAP JWT slice (`payer-a-client` + `payer-b-as`) captured in
Live Flow. That delivers the teaching claim immediately and gives the vLEI side a real flow to slot into
once Stage 2 lands. (Not started — this doc is the design.)
