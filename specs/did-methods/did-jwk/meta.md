# did:jwk Method Specification

- **Status:** Community spec (Quartzjer; informally adopted)
- **Canonical URL:** https://github.com/quartzjer/did-jwk
- **Fetched:** 2026-05-21
- **Local file:** `did-jwk.md`

## Why we care
Even simpler than did:key — the DID is just a base64url-encoded JWK. **No multicodec table to maintain.** Useful as a backup option if did:key encoding feels awkward. Often paired with OID4VCI.

## Key concepts
- Format: `did:jwk:<base64url(JWK)>`
- DID Document is synthesized from the embedded JWK
- Trivially supports any algorithm the JWK format supports (RSA, EC, OKP)
- Same lifecycle tradeoffs as did:key (no rotation)

## Why this could be useful for Sigil
- Reuses Sigil's existing JWK infrastructure (JWT/JOSE already in stack)
- Easier interop with OpenID4VC ecosystem
- Implementation is ~50 LOC once you have a JWK serializer
