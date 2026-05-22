# Securing Verifiable Credentials using JOSE and COSE

- **Status:** W3C Recommendation
- **Canonical URL:** https://www.w3.org/TR/vc-jose-cose/
- **Fetched:** 2026-05-21
- **Local file:** `vc-jose-cose.html`

## Why we care
The other VC 2.0 securing-format family. Wraps the VC as a JWT (JOSE) or CWT (COSE). Much simpler than Data Integrity — no JSON-LD canonicalization required. **This is the right starting point for Sigil's VC issuance** because it reuses existing JWT infrastructure (System.IdentityModel.Tokens.Jwt is already in TrustCartographer).

## Key concepts to map into Sigil
- **vc+jwt** / **vc+sd-jwt** / **vc+cose** — Media types for the secured envelope
- **JWT claims** — `iss` (issuer DID), `sub` (subject DID), `iat`/`exp`, plus `vc` claim containing the VC body
- **Verification** — Standard JWT verify against the issuer's `assertionMethod` key (resolved from the issuer's DID Document)
- **SD-JWT path** — Combines with the SD-JWT spec to enable selective disclosure (see `ietf/sd-jwt-vc`)

## Contrast with vc-data-integrity
JOSE/COSE: simpler, faster, no JSON-LD required. Data Integrity: embedded proofs, supports BBS+ selective disclosure natively, but heavyweight.
