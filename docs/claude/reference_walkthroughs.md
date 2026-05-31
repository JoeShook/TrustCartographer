---
name: reference-walkthroughs
description: TrustCartographer interactive walkthroughs (live Razor pages + learning guides) that teach DID/VC concepts by demonstrating real flows
metadata: 
  node_type: memory
  type: reference
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

TrustCartographer has interactive walkthroughs at `src/TrustCartographer.UI/Pages/` (Razor Class Library — see [[project-rcl-layout]]) paired with learning guides in `docs/walkthroughs/`. Each walkthrough is a live page (real keys, real signatures, real verification — no mocks) that demonstrates one trust-model flow end to end, with tamper toggles that exercise the failure modes.

**Index:** `docs/walkthroughs/README.md`

**Available:**
- **Practitioner License Credential** (`/practitioner-license`) — Issuer (State Medical Board) → Holder (Dr. Smith) → Verifier (Hospital EHR). Demonstrates did:key, JWT-VC issuance, presentation, and EdDSA verification. Tamper modes: Tampered signature, Unknown issuer key, Expired credential. Guide: `docs/walkthroughs/practitioner-license.md`.

**Component infrastructure (reusable across future walkthroughs):**
- `SsiSequenceDiagram.razor` (in `src/TrustCartographer.UI/Shared/`) — data-driven sequence diagram; pass `Actors` + `Steps` + `CurrentIndex`. New walkthroughs reuse this without touching it.
- `JwtViewer.razor` — JWT decoder/pretty-printer with epoch annotations (ported from `shin-sand-gate`, TC-flavored CSS).
- `DidKeyService`, `VcService`, `CryptoService` in `TrustCartographer.Core/Services/` — self-contained crypto, deliberately visible (mirrors Sigil logic but stands alone for pedagogy).

**When to use:** When the user wants to learn or teach a SSI/PKI concept hands-on, point them at the relevant walkthrough URL + guide. When they want to add a new use case (Patient Consent, OID4VCI, did:web, SD-JWT, UDAP comparison), follow the "Adding a new walkthrough" recipe in `docs/walkthroughs/README.md` — new page composes `SsiActor` + `SsiStep` lists; no diagram-component changes needed.

**Convention:** every walkthrough has a paired guide doc. "A walkthrough without a guide is undocumented; a guide without a walkthrough is theory."
