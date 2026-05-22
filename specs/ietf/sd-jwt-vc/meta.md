# SD-JWT-based Verifiable Credentials (SD-JWT-VC)

- **Status:** IETF Draft (under active development)
- **Canonical URL:** https://datatracker.ietf.org/doc/draft-ietf-oauth-sd-jwt-vc/
- **Fetched:** 2026-05-21
- **Local file:** `sd-jwt-vc.html`

## Why we care
SD-JWT applied as a VC format. **Heavily adopted in EU/eIDAS digital identity wallets.** If Sigil wants to interoperate with European wallet ecosystems (and UDAP's eventual cross-border story), SD-JWT-VC matters.

## Key concepts
- Builds on SD-JWT — adds VC-specific claim conventions
- Uses media type `application/vc+sd-jwt`
- Carries `iss`, `iat`, `cnf` (holder binding key), plus selectively-disclosable claims
- Distinct from the JSON-LD VC + Data Integrity world — does NOT use JSON-LD
- Distinct from W3C's `vc-jose-cose` securing format — different envelope conventions

## Where it fits in the landscape
- W3C VC 2.0 + Data Integrity: JSON-LD camp (verifiable.dev, Veramo)
- W3C VC 2.0 + JOSE/COSE: JWT camp (W3C-aligned)
- IETF SD-JWT-VC: parallel JWT-based credential format (eIDAS, OID4VCI default)
- ISO mDoc (18013-5): mobile driver's license / passport credentials

Sigil should probably support W3C VC 2.0 + JOSE first, then SD-JWT-VC for interop.
