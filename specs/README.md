# Specifications

Curated reference specs for TrustCartographer (visualizer) and Sigil (trust-material backend). Each spec lives in its own folder with the canonical document plus a `meta.md` explaining version, source URL, and why it matters for this project.

All web-sourced specs were fetched 2026-05-21 as canonical HTML. To refresh, re-run the curl commands documented in the relevant `meta.md` files (or see [Refreshing](#refreshing) below).

## Layout

```
specs/
├── w3c/                       # W3C SSI core specs
│   ├── did-core-1.0/          # DID syntax + DID Document model
│   ├── vc-data-model-2.0/     # VC 2.0 data model
│   ├── vc-data-integrity/     # JSON-LD / Linked Data Proof securing format
│   ├── vc-jose-cose/          # JWT/COSE securing format (simpler — start here)
│   └── vc-bitstring-status-list/  # VC revocation (the CRL analog)
├── did-methods/
│   ├── did-key/               # Self-describing — Phase A for Sigil
│   ├── did-web/               # URL-hosted — Phase B for Sigil
│   └── did-jwk/               # JWK-encoded — even simpler than did:key
├── ietf/
│   ├── sd-jwt/                # Selective Disclosure JWT (privacy lever)
│   └── sd-jwt-vc/             # SD-JWT applied as a VC format (eIDAS-favored)
├── openid/
│   ├── oid4vci/               # OpenID for VC Issuance (de facto SSI issuance protocol)
│   └── oid4vp/                # OpenID for VC Presentation
├── dif/
│   ├── didcomm-v2/            # Agent-to-agent messaging — comparison to A2A
│   └── presentation-exchange/ # How Verifiers formally ask for VCs
└── udap/
    ├── core/                  # UDAP.org base specs (7 files)
    └── profiles/
        └── ssraa-v2.0.0/      # SSRAA healthcare profile of UDAP
```

## Suggested reading order

For someone coming from PKI/UDAP into SSI:

1. **`w3c/did-core-1.0/`** — Understand the DID & DID Document concept (the "no-CA identifier")
2. **`did-methods/did-key/`** — See the simplest concrete DID method
3. **`did-methods/did-web/`** — See how DIDs can be hosted (UDAP `.well-known` analog)
4. **`w3c/vc-data-model-2.0/`** — Understand the VC data model (the "open-schema certificate")
5. **`w3c/vc-jose-cose/`** — Pick the JWT-based securing format (closest to existing UDAP JWT auth)
6. **`w3c/vc-bitstring-status-list/`** — Understand VC revocation (CRL analog)
7. **`openid/oid4vci/`** — See how VCs are issued at protocol level (UDAP DCR analog)
8. **`openid/oid4vp/`** + **`dif/presentation-exchange/`** — See the Verifier side
9. **`ietf/sd-jwt/`** + **`ietf/sd-jwt-vc/`** — The selective-disclosure differentiator
10. **`dif/didcomm-v2/`** — The other agent protocol (DIF answer to A2A)

Then compare against your existing UDAP knowledge in **`udap/core/`** and **`udap/profiles/ssraa-v2.0.0/`**.

## Cross-spec mapping (PKI ↔ SSI)

| Concept | X.509 / UDAP | SSI |
|---|---|---|
| Identifier | Subject DN / SKI | DID (`did-core-1.0`) |
| "Document" binding identifier to keys | Certificate | DID Document (`did-core-1.0`) |
| Signing authority | CA | Issuer (referenced by DID) |
| Trust establishment | Cert chain to a trust anchor | Membership in a trust registry |
| Revocation | CRL / OCSP | Bitstring Status List (`vc-bitstring-status-list`) |
| Claims about a subject | Cert fields / extensions | VC (`vc-data-model-2.0`) |
| Selective disclosure | (not supported) | SD-JWT (`sd-jwt`) or BBS+ in Data Integrity |
| Discovery / metadata | `.well-known/udap` | `did:web` document or `.well-known/openid-credential-issuer` |
| Authorization context propagation | UDAP Authorization Extension Object | (no direct equivalent — UDAP's strength) |
| Third-party assertions | UDAP Certifications & Endorsements | VCs *about* an Issuer |
| Governance domain | UDAP Community / TrustDomain | Trust Registry per ecosystem |

## Refreshing

To re-fetch a single spec, use the canonical URL listed in its `meta.md`. For example:

```bash
curl -fsSL -A "Mozilla/5.0 (compatible; TrustCartographer-spec-importer)" \
  "https://www.w3.org/TR/did-1.0/" \
  -o specs/w3c/did-core-1.0/did-core-1.0.html
```

When refreshing, update the `meta.md` `Fetched:` line and (if the spec status has changed) the `Status:` line.

## Provenance of UDAP / SSRAA copies

| Local path | Source |
|---|---|
| `udap/core/*.md` | `C:/Source/GitHub/JoeShook/udap-tools/udap-dotnet/docs/specifications/UDAP.org/` |
| `udap/profiles/ssraa-v2.0.0/*` | `C:/Source/GitHub/JoeShook/udap-tools/udap-dotnet/docs/specifications/SSRAA/V2.0.0/` |

To refresh, copy from those source paths. Treat the `udap-tools` repo as the upstream.
