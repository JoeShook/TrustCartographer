# Q&A: What does OpenID do in the DID space?

> Captured: 2026-05-21

## Question

What does OpenID do in DID space?

## Answer

OpenID in the DID / SSI space primarily shows up via two specs — **OID4VCI** (OpenID for Verifiable Credential Issuance) and **OID4VP** (OpenID for Verifiable Presentations). Together they're often called **OID4VC**.

### What they do

**OID4VCI — Issuance protocol**

Defines how an Issuer hands a Verifiable Credential to a Holder's wallet using familiar OAuth 2.0 flows. The Holder's wallet acts as an OAuth client; the Issuer acts as an OAuth-style server with a new credential endpoint. You get a VC instead of (or in addition to) a normal access token.

**OID4VP — Presentation protocol**

Defines how a Verifier requests one or more VCs from a Holder's wallet, and how the wallet returns a Verifiable Presentation. Built on OAuth 2.0's authorization request/response shape, with DIF Presentation Exchange as the query language for "what VCs do you want?"

### Why it matters

- **Reuses OAuth 2.0 infrastructure.** Wallets, issuers, and verifiers don't need a brand-new transport — they ride on the same HTTP redirects, PKCE, DPoP, JAR, etc. that OAuth deployments already use.
- **Bridges DID/VC world with mainstream IAM.** Instead of needing exotic SSI transports (DIDComm, etc.), an organization with an existing OAuth/OIDC stack can add VC issuance/verification incrementally.
- **De facto standard for the EU and US deployments.** The EU Digital Identity Wallet (eIDAS 2.0 / EUDI Wallet) and many US pilots have settled on OID4VC as the protocol layer. If you're comparing real-world SSI deployments to A2A or UDAP, OID4VC is what they actually use over the wire.

### Relationship to DIDs/VCs

OID4VC is **protocol-layer, not data-model-layer**. It carries VCs (whose subject and issuer are typically DIDs) over the wire — but it doesn't define what a VC or DID is. That's W3C's job.

So the stack looks like:

| Layer | Spec |
|---|---|
| Identifier model | W3C DID Core — what an identifier is |
| Credential data model | W3C VC Data Model 2.0 — what a credential is |
| Issuance / presentation protocol | OID4VCI / OID4VP — how issuers, wallets, and verifiers actually talk to each other |
| Alternative transport | DIDComm v2 — the DIF camp's transport (more SSI-native, less OAuth-aligned) |

### Comparison to the TrustCartographer goal

This is exactly where **UDAP and OID4VC overlap conceptually** — both are OAuth 2.0-based trust frameworks for cross-org credential exchange. UDAP uses X.509 + JWTs; OID4VC uses DIDs + VCs. That side-by-side is one of the strongest comparative arguments TrustCartographer can make.

## Related local material

- `../../specs/openid/oid4vci/` — the issuance protocol spec
- `../../specs/openid/oid4vp/` — the presentation protocol spec
- `../../specs/dif/presentation-exchange/` — the query language OID4VP uses
- `../../specs/udap/core/` — the UDAP specs for the side-by-side comparison
- `../pki-vs-ssi.md` — broader mapping between paradigms
