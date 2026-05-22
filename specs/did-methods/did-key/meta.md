# did:key Method v0.9

- **Status:** W3C-CCG Draft (community spec; widely implemented)
- **Canonical URL:** https://w3c-ccg.github.io/did-key-spec/
- **Fetched:** 2026-05-21
- **Local file:** `did-key.html`

## Why we care
The simplest possible DID method — the DID **is** the public key (multibase + multicodec encoded). No network, no storage, no infrastructure. **This is Phase A for Sigil/TrustCartographer** — proves the model end-to-end with zero moving parts.

## Key concepts
- Format: `did:key:<multibase>` where multibase decodes to `<multicodec-prefix><raw-public-key-bytes>`
- Multicodec prefixes: `ed01` (Ed25519), `e701` (secp256k1), `8024` (P-256), etc.
- Multibase: `z` prefix = base58btc
- **DID Document is fabricated on resolution** — just synthesize it from the parsed key
- No rotation, no update, no deactivation — you mint a new DID instead
- **Tradeoff:** zero infra, but no key rotation = bad for long-lived identities

## Sigil Phase A flow
1. User picks an algorithm (Ed25519 default)
2. `ISigningProvider` generates a key (software, Vault, etc.)
3. Encode public key as multicodec + multibase → DID string
4. Synthesize DID Document on demand
5. Store the (DID, key reference, TrustDomain) tuple in `Sigil.Did.DidDocument` table
