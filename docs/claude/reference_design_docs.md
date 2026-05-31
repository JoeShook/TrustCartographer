---
name: reference-design-docs
description: "TrustCartographer docs/ folder with design notes for Sigil/TrustCartographer relationship, PKI-vs-SSI mapping, phased DID/VC roadmap, and captured Q&A"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

The `docs/` folder in TrustCartographer (at `C:\Source\GitHub\JoeShook\TrustCartographer\docs\`) holds design notes captured from our conversations — the *thinking* behind the project, distinct from specs (which live in `../specs/`) and from work-in-progress phase tracking (which lives in `../PLAN.md`).

**Index:** `docs/README.md` — start here for layout and reading order.

**Contents:**
- `sigil-and-trustcartographer.md` — Two-project setup (TrustCartographer = visualizer, Sigil = trust-material backend). Proposed seams: parallel hierarchy under `TrustDomain` with new `Sigil.Did`, `Sigil.Vc`, `Sigil.TrustRegistry` projects. `ISigningProvider` is the load-bearing abstraction reused across paradigms.
- `pki-vs-ssi.md` — Concept mapping table (X.509 ↔ DID ↔ VC), lifecycle parallels, four points where SSI genuinely diverges (no DID issuer, multi-key, holder-mediated, opt-in status), VC format pluralism, what UDAP does better than SSI.
- `phased-roadmap.md` — Phases A–F for adding DID/VC to Sigil: A) did:key + software provider, B) did:web + publishing, C) Vault/GCP KMS, D) JWT-VC issuance, E) Bitstring Status List, F) Trust Registry.
- `qa/openid-in-did-space.md` — Captured Q&A on OID4VCI/OID4VP and how OpenID bridges DID/VC with mainstream IAM.

**When to use:** Before designing or implementing any DID/VC feature in [[project-sigil-relationship|Sigil]], or when answering "why did we structure it this way?" These docs ground every architectural decision and reflect explicit conversation with the user.

**When to extend:** Add new topical design files in `docs/` directly. Drop captured Q&A into `docs/qa/<slug>.md`. Cross-link to [[reference-specs|specs]] for normative requirements.
