# Sigil and TrustCartographer

How the two projects relate, why we want a single trust-material backend with multiple paradigms, and where the seams should live.

## Roles

| Project | Role |
|---|---|
| **TrustCartographer** | Interactive *visualizer and teacher*. Shows the mechanics of crypto, DIDs, VCs, trust frameworks side-by-side. Goal: argue for UDAP's strengths in agent-to-agent auth by comparison with SSI and Google A2A. |
| **Sigil** | The *trust-material backend*. Existing .NET PKI/CA tool at `C:\Source\GitHub\JoeShook\udap-tools\udap-dotnet\examples\CA`. Manages X.509 hierarchies, templates, CRLs, signing providers. To be extended to also manage DIDs and VCs. |

TrustCartographer stays focused on teaching the mechanics. Sigil owns the production-quality storage, lifecycle, and key management. TrustCartographer's later phases consume Sigil's API instead of toy in-memory data.

## Why extend Sigil rather than build a separate tool

Sigil already has the right primitives for trust material:

- **TrustDomain** (currently `Community`, rename in flight) — the parent governance unit. Naturally holds X.509 anchors *and* DID anchors *and* trust-registry entries.
- **`ISigningProvider`** — abstracts local PFX / Vault Transit (later: HSM/PKCS#11, GCP KMS, Azure Key Vault, AWS KMS). Verification methods on a DID Document and signing keys on a VC use the *same* primitive. **This is the load-bearing abstraction that makes the merge natural.**
- **Templates → issuance → publication → revocation** lifecycle is already implemented for X.509. DIDs and VCs follow the same lifecycle shape.
- **UDAP-focused roadmap** — Sigil is already trending toward generalized "trust domains" with the planned rename. SSI material is the next generalization.
- **Multi-provider key storage** — the abstraction Sigil built for CAs gives DID/VC issuance HSM/Vault/KMS support for free.

## The paradigm choice

DIDs and VCs are **not** subtypes of X.509. They share lifecycle shape but differ in:
- Trust establishment (no chain — uses a trust registry)
- Identifier model (self-asserted DID vs CA-issued subject)
- Key-purpose model (multi-key per identifier vs single key per cert)
- Revocation mechanism (bitstring status list vs CRL/OCSP)
- Disclosure semantics (selective disclosure possible vs fixed)

**Do not** subclass `CaCertificate` for DIDs or `IssuedCertificate` for VCs. Build a parallel hierarchy with shared abstractions where appropriate. Forcing DIDs into the cert model would hide exactly what makes them different — and TrustCartographer's whole purpose is to show those differences.

See `pki-vs-ssi.md` for the full mapping and the divergence points.

## Proposed seams

```
Sigil.Common                                  (existing — shared kernel)
├── TrustDomain                               (existing, will hold all paradigms)
├── ISigningProvider                          (existing — REUSED as-is for DID/VC signing)
└── IKeyMaterialProvider                      (extract if not already — DIDs need raw public-key bytes for did:key)

Sigil.Did                                     (NEW project)
├── IDidMethodProvider                        (did:key, did:web, did:jwk; later did:ion)
├── DidDocument / VerificationMethod          (own tables, FK to TrustDomain)
├── DidResolver                               (method-dispatched)
└── DidWebPublisher                           (hosts /.well-known/did.json per TrustDomain)

Sigil.Vc                                      (NEW project)
├── CredentialSchema                          (the "template" analog for VCs)
├── CredentialIssuer                          (template + claims + subject DID + issuer DID → signed VC)
├── StatusListService                         (Bitstring Status List — the "CRL of VCs")
└── PresentationVerifier                      (verify VPs from holders)

Sigil.TrustRegistry                           (NEW project — the cross-paradigm payoff)
├── TrustedIssuer                             (FK to TrustDomain; can point to an X.509 anchor OR a DID)
└── Publishes a unified registry document per TrustDomain
```

The **TrustRegistry** project is where the cross-paradigm value really lands. A relying party asks "who do I trust in this TrustDomain?" and gets back a list spanning X.509 roots, DID issuers, and metadata about each. This is UDAP's "Community" concept generalized to cover SSI material.

## UI implications

The TrustDomain detail page becomes the orchestration surface:

```
TrustDomain: "ACME Healthcare"
├── Tab: CAs              (existing — X.509 hierarchy)
├── Tab: DIDs             (new — list of DIDs Sigil controls in this domain)
├── Tab: Credentials      (new — issued VCs, schemas, status lists)
└── Tab: Trust Registry   (new — who this domain trusts, across both paradigms)
```

Unified dashboard widgets: expiring certs *and* expiring VCs, deactivated DIDs, revocation activity across all material.

## Storage

Separate tables. FK to `TrustDomain`. New entities:
- `DidDocument`, `VerificationMethod`, `DidServiceEndpoint`
- `CredentialSchema`, `IssuedCredential`, `StatusList`, `StatusListEntry`
- `TrustRegistryEntry` (polymorphic: points to a `CaCertificate` OR a `DidDocument`)

Do not extend `CaCertificate` or `IssuedCertificate`. Different lifecycles, different identifier formats, different revocation models — separate tables keep the model honest.

## Signing-provider extensions

The same `ISigningProvider` interface handles all three paradigms. Concrete providers:

| Provider | X.509 use | DID use | VC use | Status |
|---|---|---|---|---|
| Software (in-memory / on-disk PFX) | Existing | New: rapid prototyping default | New: rapid prototyping default | Phase A |
| HashiCorp Vault Transit | Existing | New | New | Phase C |
| Google Cloud KMS | Planned | New | New | Phase C+ |
| HSM (PKCS#11) | Planned | Planned | Planned | Later |
| Azure Key Vault | Planned | Planned | Planned | Later |
| AWS KMS | Planned | Planned | Planned | Later |

Software provider is mandatory — it's how prototypes start. Vault and KMS providers give production-grade key isolation for free across all paradigms once the abstraction proves out.

## Related docs

- `pki-vs-ssi.md` — concept mapping and where SSI diverges
- `phased-roadmap.md` — Phases A–F for actually building this
- `qa/openid-in-did-space.md` — where OpenID fits in the DID/VC stack
- `../specs/README.md` — curated specs that ground every design decision here
