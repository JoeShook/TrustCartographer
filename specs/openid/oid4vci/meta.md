# OpenID for Verifiable Credential Issuance 1.0

- **Status:** OpenID Foundation Final / late draft
- **Canonical URL:** https://openid.net/specs/openid-4-verifiable-credential-issuance-1_0.html
- **Fetched:** 2026-05-21
- **Local file:** `oid4vci.html`

## Why we care
The **de facto protocol** for issuing VCs to wallets. Extends OAuth 2.0 — so it looks very familiar to anyone who knows UDAP's OAuth-based flows. **The strongest direct comparison surface between SSI and UDAP** lives here.

## Key concepts
- **Credential Issuer Metadata** — `.well-known/openid-credential-issuer` (UDAP fans will see the parallel immediately)
- **Credential Offer** — Issuer-initiated link/QR code starting the flow
- **Authorization Code / Pre-authorized Code flow** — OAuth grant types adapted for credential delivery
- **Credential Endpoint** — Where the wallet POSTs an access token + proof-of-possession to receive a VC
- **Notification Endpoint** — Wallet acknowledges receipt (for issuer logging)
- **Format negotiation** — `vc+jwt`, `vc+sd-jwt`, `ldp_vc`, `mso_mdoc` — wallet picks what it understands

## Why this matters for the UDAP comparison
OID4VCI essentially answers the question: "what does UDAP-style dynamic registration + JWT client auth look like for issuing credentials instead of access tokens?" Side-by-side, you can show OID4VCI metadata vs UDAP server metadata, OID4VCI Credential Endpoint vs UDAP token endpoint.
