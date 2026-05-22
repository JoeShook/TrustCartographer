# did:web Method Specification

- **Status:** W3C-CCG Draft (widely deployed)
- **Canonical URL:** https://w3c-ccg.github.io/did-method-web/
- **Fetched:** 2026-05-21
- **Local file:** `did-web.html`

## Why we care
DIDs hosted at a regular HTTPS URL. **This is Phase B for Sigil** — Sigil becomes a real DID controller infrastructure by publishing `did.json` per TrustDomain. Closest analog to UDAP's `.well-known/udap` pattern.

## Key concepts
- Format: `did:web:example.com` → resolves to `https://example.com/.well-known/did.json`
- With path: `did:web:example.com:users:alice` → `https://example.com/users/alice/did.json`
- **No blockchain, no DLT** — just web PKI (TLS cert is the trust root for publication integrity)
- **Rotation** = update the published `did.json` (with a new `verificationMethod`)
- **Deactivation** = publish a `did.json` with `deactivated: true`

## Sigil Phase B flow
1. TrustDomain has a configured base domain
2. New DID web doc generated, signing keys via `ISigningProvider`
3. Sigil exposes `/.well-known/did.json` and `/dids/{id}/did.json` endpoints
4. Rotation updates the document and re-publishes
5. Optional: snapshot every published version for audit (analog to CRL history)
