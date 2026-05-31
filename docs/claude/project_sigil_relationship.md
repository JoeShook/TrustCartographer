---
name: project-sigil-relationship
description: Sigil PKI tool and its proposed role as the trust-material backend for TrustCartographer (DIDs/VCs alongside X.509)
metadata: 
  node_type: memory
  type: project
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

Sigil is an existing .NET PKI/CA management tool at `C:\Source\GitHub\JoeShook\udap-tools\udap-dotnet\examples\CA`. Stack: .NET 10, Blazor Server, FluentUI v4, PostgreSQL, BouncyCastle, Serilog, Aspire + HashiCorp Vault Transit. Multi-project solution (Sigil, Sigil.Common, Sigil.UI, Sigil.AppHost, Sigil.Vault, Sigil.Gcp, Sigil.FileSystem, Sigil.Certificate.Server). Currently manages X.509 trust material: Communities (being renamed to TrustDomains) → Root CA → Intermediate CA → End-entity certs + CRLs, with templates, ASN.1 viewer, chain validation, Vault Transit signing, UDAP-focused features.

**Direction (proposed 2026-05-21):** Extend Sigil to also manage DID and Verifiable Credential trust material as a parallel hierarchy under TrustDomain — not as subtypes of the X.509 tables. TrustCartographer stays as the *visualizer/teacher* and consumes Sigil as a real backend instead of toy in-memory data.

**Why:** Sigil already has the right primitives (trust anchor, issuer, issued artifact, revocation, key storage abstraction via Vault/local). The "TrustDomain" rename aligns perfectly with holding both X.509 anchors and DID anchors. UDAP's Certifications & Endorsements concept is essentially VC-shaped already.

**How to apply:**
- For TrustCartographer Phase 2+ (DIDs, VCs), assume Sigil will be the production backend — design the visualizer to consume an API, not own the data
- Keep DID/VC modeling distinct from X.509 in Sigil — shared concepts, separate storage (likely new `Sigil.Did` / `Sigil.Vc` projects under `Sigil.Common` abstractions)
- Don't collapse DIDs into CaCertificate or VCs into IssuedCertificate — that hides exactly the differences TrustCartographer is meant to teach (see [[overview]])
- Sigil's roadmap Phase 8 (UDAP/FHIR) and Phase 11 (multi-algorithm including Ed25519) are natural extension points
