---
name: project-trust-triangle-complete
description: "The full SSI trust triangle (issuance + verification) is implemented live; the verifier's missing trust list is a deliberate teaching hook"
metadata: 
  node_type: memory
  type: project
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

As of 2026-05-31 the live trust triangle is complete end-to-end through the YARP proxy:
- **Issuance** — OID4VCI (Issuer → Holder), `src/TrustCartographer.Core/Oid4Vci/`, Issuer endpoints, Holder `WalletClient`.
- **Verification** — OID4VP (Holder → Verifier), `src/TrustCartographer.Core/Oid4Vp/` (`VpService`, `PresentationRequest`, `PresentationRequestUri`), Verifier `PresentationEndpoints` + Blazor UI, Holder `PresentationClient` + `/wallet/present`.
- **Live Flow** (`src/TrustCartographer.UI/Pages/LiveFlow.razor`) animates both halves as a sequence diagram from real captured HTTP; actors browser/issuer/holder/verifier; "Run full demo" plays the whole triangle.

**Deliberate gap (the UDAP hook):** the Verifier resolves *any* issuer `did:key` and verifies the signature, but has **no trust list** — it proves *who* signed, not that they're authoritative. This is surfaced on the Verifier Home page ("No trust list yet") on purpose, as the concrete motivation for trust frameworks (trust lists, certifications, UDAP trust communities). This is the next natural build toward the comparative end goal. See [[project-blazor-behind-proxy]].

Commits on master (not auto-pushed): `57c8118` proxy fix, `82b4f2a` Live Flow animation, `636de9b` OID4VP verifier.
