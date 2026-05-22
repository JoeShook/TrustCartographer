# OpenID for Verifiable Presentations 1.0

- **Status:** OpenID Foundation Final / late draft
- **Canonical URL:** https://openid.net/specs/openid-4-verifiable-presentations-1_0.html
- **Fetched:** 2026-05-21
- **Local file:** `oid4vp.html`

## Why we care
Companion to OID4VCI. Defines how a **Verifier** asks a Holder for credentials, and how the Holder responds. **This is where the trust-triangle Verifier role gets a wire protocol.** Sigil doesn't host the Verifier (the relying app does), but understanding this is essential to build a meaningful demo in TrustCartographer.

## Key concepts
- **Authorization Request** with `presentation_definition` (a DIF PE doc) — see neighbor `dif/presentation-exchange`
- **Same-device / cross-device flows** — Wallet on same phone vs QR code from desktop
- **Vp Token Response** — Wallet returns one or more VPs containing the requested VCs
- **Client metadata** — Verifier identifies itself with metadata similar to OAuth clients
- **DCQL (Digital Credentials Query Language)** — Newer simpler query language; presentation_definition is the older path

## Relevance to the comparison
OID4VP fills the role of "what UDAP would look like if the Verifier asked for *selectively disclosed credentials* instead of an access token bound to scopes." Useful to demonstrate the SSI privacy story.
