# Verifiable Credential Data Integrity 1.0

- **Status:** W3C Recommendation
- **Canonical URL:** https://www.w3.org/TR/vc-data-integrity/
- **Fetched:** 2026-05-21
- **Local file:** `vc-data-integrity.html`

## Why we care
One of two securing-format families for VC 2.0. Defines **Linked Data Proofs** — embedded `proof` blocks inside the JSON-LD VC. The JSON-LD/cryptosuite-based path. Important for understanding the SSI mainstream (most W3C-CCG implementations use this).

## Key concepts to map into Sigil
- **Proof** — Embedded object inside a VC: `type`, `cryptosuite`, `verificationMethod`, `proofValue`, `proofPurpose`, `created`
- **Cryptosuite** — Named algorithm set (e.g., `eddsa-rdfc-2022`, `ecdsa-rdfc-2019`, `bbs-2023`)
- **proofPurpose** — Why this proof exists (`assertionMethod`, `authentication`, etc.) — must match a key purpose in the issuer's DID Document
- **Canonicalization (RDFC)** — JSON-LD docs are normalized to RDF Dataset Canonical Form before hashing (this is the part that makes JSON-LD VCs operationally heavy)
- **BBS+** — A cryptosuite that enables selective disclosure without exposing all claims

## Contrast with vc-jose-cose
This path requires JSON-LD processing and canonicalization. The JOSE/COSE path doesn't — it wraps the VC as a JWT/CWT payload. Sigil should probably implement JOSE/COSE first (simpler) and add Data Integrity later.
