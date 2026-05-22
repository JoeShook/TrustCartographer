# Verifiable Credentials Data Model v2.0

- **Status:** W3C Recommendation (15 May 2025)
- **Canonical URL:** https://www.w3.org/TR/vc-data-model-2.0/
- **Fetched:** 2026-05-21
- **Local file:** `vc-data-model-2.0.html`

## Why we care
The current data model for Verifiable Credentials. v2.0 split the "what's in a credential" (this spec) from "how it's signed" (see `vc-data-integrity` and `vc-jose-cose`). Anything Sigil issues as a VC must conform to this data model.

## Key concepts to map into Sigil
- **Issuer / Holder / Verifier** — The trust triangle (Sigil acts as Issuer)
- **VerifiableCredential** — JSON-LD document with `@context`, `type`, `issuer`, `validFrom`/`validUntil`, `credentialSubject`, optional `credentialStatus`
- **credentialSubject** — Claims about one or more subjects (usually identified by a DID)
- **credentialStatus** — Pointer to a revocation/suspension mechanism (e.g., Bitstring Status List)
- **credentialSchema** — Pointer to a schema describing required claims (Sigil's "template" analog)
- **VerifiablePresentation** — Wrapper a Holder sends to a Verifier, may contain multiple VCs
- **Securing format** — VC 2.0 is format-agnostic; pick Data Integrity (JSON-LD proofs) or JOSE/COSE (JWT/CWT)
