# PKI vs SSI — Mental Model and Mapping

A reference for someone coming from X.509 / UDAP into the DID / VC world. Every concept on one side has a near-analog on the other; the *differences* are where the comparative story lives.

## One-line mental models

- **PKI / X.509:** Identifiers are CA-issued. Trust comes from a signed chain back to a root.
- **DIDs:** Identifiers are self-asserted strings that resolve to a DID Document. Trust comes from membership in a trust registry.
- **VCs:** Certificates with arbitrary JSON claims, signed by an Issuer (referenced by DID), held by a Holder, presented to a Verifier.

## Concept mapping

| Concept | X.509 | DID | VC |
|---|---|---|---|
| **Identifier** | Subject DN / SKI | The DID string itself (`did:method:abc...`) | Subject DID inside the VC |
| **"Document"** | The certificate | DID Document (JSON / JSON-LD) | The VC (JSON or JWT) |
| **Public keys** | `SubjectPublicKeyInfo` | `verificationMethod[]` array | Referenced via Issuer's DID |
| **Key purpose** | `KeyUsage` / `EKU` extensions | `authentication`, `assertionMethod`, `keyAgreement`, `capabilityInvocation`, `capabilityDelegation` | Issuer uses `assertionMethod` |
| **Resolution** | AIA + chain build | DID method (`did:key` = arithmetic; `did:web` = HTTPS GET; `did:ion` = blockchain) | Resolve issuer DID → get key → verify signature |
| **Revocation** | CRL / OCSP | Deactivate in DID Document | Bitstring Status List |
| **Publication** | CRL / AIA endpoint | `did:web` URL or blockchain anchor | Status list endpoint |
| **"Template"** | `CertificateTemplate` | (n/a — DID Documents are flat per controller) | `CredentialSchema` |
| **Trust anchor** | Root CA cert | A DID on a trusted issuers list | Verifier's "trusted issuers" list |
| **Discovery / metadata** | `.well-known/udap` (UDAP) | `did:web` document or `.well-known/openid-credential-issuer` | (no equivalent — discovery is at issuer level) |
| **Authorization context** | UDAP Authorization Extension Object | (no direct equivalent — UDAP's strength) | (no direct equivalent) |
| **Third-party assertions** | UDAP Certifications & Endorsements | (none at DID layer) | VCs about an Issuer |
| **Governance domain** | UDAP Community / TrustDomain | Trust Registry per ecosystem | Trust Registry per ecosystem |

## Lifecycle parallels

The operational shapes line up almost perfectly:

| Operation | X.509 | DID | VC |
|---|---|---|---|
| **Create** | Generate cert | Generate DID Document | Issue VC |
| **Verify identity** | Chain validation + CRL/OCSP | Resolve DID Document | Verify signature + check status |
| **Rotate** | Re-key renewal | Update verification method | Re-issue |
| **Revoke** | CRL entry | Deactivate DID Document | Status list bit flip |
| **Publish** | CRL/AIA endpoint | `did:web` URL endpoint | Status list endpoint |

**Implication:** Sigil's existing lifecycle machinery (template → issue → publish → revoke) generalizes naturally. The differences are in *format* and *trust establishment*, not in *lifecycle*.

## Where SSI genuinely diverges from PKI

These are the four points to be careful not to flatten into the X.509 model when designing Sigil.

### 1. No issuer for the DID itself

A DID is *self-asserted*. The controller mints it by generating a key pair. There is no CA signing the DID into existence. Trust in a DID comes from seeing it on a trusted-issuers list (a trust registry), not from a signing chain.

**Sigil implication:** A `DidDocument` row does not have an "issuer" foreign key. It has a `controller` (the entity managing the keys) and lives within a TrustDomain, but it is not chained to any other entity in the database.

**UDAP analog:** This is what UDAP's "Community" concept *already does* for X.509 — a community defines which roots are trusted. Generalize that model for DIDs.

### 2. Multiple keys per identifier, by purpose

A single DID Document typically carries separate keys for:
- **`authentication`** — proves "I am this DID" at login time
- **`assertionMethod`** — signs VCs and other assertions
- **`keyAgreement`** — used for encryption (X25519 typically)
- **`capabilityInvocation`** — proves authority to update the DID Document
- **`capabilityDelegation`** — delegates capability to another DID

This is a **first-class split**, not an extension. In X.509 you'd shoehorn this with KeyUsage flags on a single key; SSI gives you N keys with explicit purposes.

**Sigil implication:** `VerificationMethod` is a separate table with a `purpose` field. A DID Document has many. Each verification method points to a key managed by an `ISigningProvider`.

### 3. Holder-mediated presentation

In SSI, the **Verifier never talks to the Issuer**. The Holder receives the VC from the Issuer, stores it, and presents it to the Verifier on demand. The Issuer doesn't see who you show your credentials to.

This is privacy-by-design and has no PKI analog. In X.509, the certificate is presented during the TLS handshake — there's no "holder" with an envelope to mediate.

**Sigil implication:** Sigil is primarily Issuer-side. Holder functionality lives in wallets (out of scope for Sigil). Verifier functionality could be a separate library or sit in a relying-party app.

### 4. Status is opt-in and pluggable

A VC may have **no `credentialStatus`** at all (if short-lived), or point to a status list, or point to some other registry. PKI assumes CRL/OCSP exist; SSI doesn't.

**Sigil implication:** `IssuedCredential` has an *optional* `credentialStatus` reference. Some credential schemas opt out entirely. Status lists are per-issuer-per-purpose — an Issuer might have a `revocation` list and a `suspension` list separately.

## Format pluralism

VCs have multiple securing formats. Sigil will need to pick which to support and in what order:

| Format | Where | Notes |
|---|---|---|
| **JOSE/COSE** (JWT/CWT-wrapped) | W3C `vc-jose-cose` | Simplest. Reuses JWT infrastructure. **Start here.** |
| **Data Integrity** (JSON-LD with embedded proofs) | W3C `vc-data-integrity` | Mainstream W3C-CCG. Requires JSON-LD canonicalization. Supports BBS+ selective disclosure. |
| **SD-JWT-VC** | IETF `sd-jwt-vc` | EU/eIDAS favored. Selective disclosure built in. |
| **mDoc / mDL** | ISO 18013-5 | Mobile driver's license / passport. Out of scope unless you need it. |

Recommendation: VC + JOSE first, SD-JWT-VC for interop, Data Integrity only if a use case demands BBS+.

## What X.509 / UDAP does better than SSI

The comparison is genuinely two-sided. UDAP has things SSI doesn't:

- **Purpose-of-use propagation** via the Authorization Extension Object. SSI has no standard for carrying *why* a credential is being presented through to downstream systems.
- **Certifications & Endorsements** are a first-class concept attached to the trust chain. SSI can express this with VCs *about* an Issuer, but the ergonomics are not as clean and the discovery story is weaker.
- **Operational maturity** — CAs, CRLs, OCSP, and HSM key custody are decades-old, well-tooled, and battle-tested. SSI infrastructure is newer.
- **Single transport** — UDAP rides on TLS + OAuth, which every IAM team already knows. SSI's transport story is fragmented (OID4VC vs DIDComm).

This is why TrustCartographer's eventual comparative view should show *both* directions — the SSI privacy wins and the UDAP purpose-of-use wins. The argument for UDAP in agent-to-agent auth is that the structured authorization context matters more than the privacy story when you're authorizing an autonomous agent to act on someone's behalf.

## Related docs

- `sigil-and-trustcartographer.md` — how this mapping lands in the Sigil design
- `phased-roadmap.md` — the actual build order
- `qa/openid-in-did-space.md` — protocol-layer details for OID4VC
- `../specs/README.md` — the underlying specs
