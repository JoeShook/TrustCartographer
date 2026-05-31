---
name: reference-specs
description: "Curated specs folder in TrustCartographer with W3C, IETF, OpenID, DIF, UDAP and SSRAA specifications for DIDs, VCs, and trust-model work"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

The `specs/` folder in TrustCartographer (at `C:\Source\GitHub\JoeShook\TrustCartographer\specs\`) contains a curated set of trust-model specifications. Each spec has its own subfolder with the canonical document + a `meta.md` explaining version, source URL, and project relevance.

**Index:** `specs/README.md` — start here for layout, suggested reading order, and the PKI↔SSI cross-spec mapping table.

**Contents (fetched 2026-05-21):**
- `w3c/` — DID Core 1.0, VC Data Model 2.0, VC Data Integrity, VC JOSE/COSE, Bitstring Status List
- `did-methods/` — did:key (Phase A), did:web (Phase B), did:jwk
- `ietf/` — SD-JWT, SD-JWT-VC
- `openid/` — OID4VCI, OID4VP
- `dif/` — DIDComm v2, Presentation Exchange
- `udap/core/` — 7 UDAP.org base specs (copied from `udap-tools/udap-dotnet/docs/specifications/UDAP.org/`)
- `udap/profiles/ssraa-v2.0.0/` — SSRAA healthcare profile (copied from `udap-tools/.../SSRAA/V2.0.0/`)

**When to use:** When implementing DID/VC features in [[project-sigil-relationship|Sigil]], when teaching/explaining trust models in TrustCartographer, or when answering "what does the spec say about X?" Each `meta.md` calls out the most relevant concepts and gives the PKI/UDAP analog where one exists.

**Refresh:** Each `meta.md` has the canonical URL. Re-curl to update; UDAP/SSRAA copies sync from the `udap-tools` repo.
