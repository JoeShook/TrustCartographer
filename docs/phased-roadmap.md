# Phased Roadmap — Adding DID/VC to Sigil

Six phases. The first three are mostly infrastructure (clean to land independently). The last three are where the SSI vs UDAP comparison gets concrete.

## Phase A — `did:key` with software signing provider

**Goal:** Smallest possible DID surface. Prove the model end-to-end with zero infrastructure.

**Scope:**
- New `Sigil.Did` project under `Sigil.Common`
- `IDidMethodProvider` interface
- `DidKeyMethodProvider` — generates Ed25519 key pair, encodes as multicodec + multibase → DID string
- DID Document synthesis on demand (no storage needed for the document itself)
- `DidDocument` and `VerificationMethod` tables (FK to TrustDomain)
- Software-only `ISigningProvider` for prototyping (reuses existing Sigil infra)
- Minimal UI: "Generate DID" button on TrustDomain detail page, shows the DID and its synthesized document

**Done when:** You can mint a `did:key` in a TrustDomain, view its DID Document, and sign a simple payload with its key. Round-trip verify works.

**Why first:** Validates the `IDidMethodProvider` abstraction and the shared use of `ISigningProvider`. No network, no publishing, no protocol.

**Spec references:**
- `specs/did-methods/did-key/`
- `specs/w3c/did-core-1.0/`

## Phase B — `did:web` with publishing endpoint

**Goal:** Sigil becomes a real DID controller. Hosts DID Documents at HTTPS endpoints per TrustDomain.

**Scope:**
- `DidWebMethodProvider` — generates `did:web:domain:path` identifiers
- `DidWebPublisher` — exposes `/.well-known/did.json` and `/dids/{id}/did.json`
- Domain configuration per TrustDomain (which hostname does this domain publish under?)
- Rotation flow — update verification methods, re-publish
- Deactivation flow — publish `deactivated: true`
- Optional: snapshot every published version for audit (analog to CRL history)

**Done when:** A `did:web` minted in Sigil resolves correctly via any third-party DID resolver. Rotation and deactivation are visible to resolvers.

**Why second:** Closest analog to UDAP's `.well-known/udap` pattern. Forces Sigil to handle the publishing concern that DID/VC infrastructure depends on.

**Spec references:**
- `specs/did-methods/did-web/`

## Phase C — Vault Transit and GCP KMS for DID signing

**Goal:** Prove the `ISigningProvider` abstraction generalizes across paradigms. The same Vault key that signs CRLs can sign DID Documents and VCs.

**Scope:**
- Wire existing `VaultTransitSigningProvider` to DID Document signing operations
- Add `GcpKmsSigningProvider` (new — works for X.509 too, get it for free)
- Add `Sigil.Gcp` project integration (already exists in Sigil's structure)
- UI: signing provider selector when minting a DID, parallel to existing CA flow

**Done when:** You can mint a `did:web` whose private key lives in Vault Transit or GCP KMS, rotate it, and verify a signed document.

**Why third:** This is the architectural payoff of using `ISigningProvider` as the load-bearing abstraction. Validates the design before VC issuance adds complexity.

**Spec references:**
- (no new specs — provider infrastructure)

## Phase D — VC issuance, JWT-VC format

**Goal:** Demonstrate the trust triangle. Sigil issues a signed VC; an external Holder/Verifier can verify it.

**Scope:**
- New `Sigil.Vc` project
- `CredentialSchema` entity — defines required claims for a credential type (e.g., `MemberCredential`: subject DID, name, role, organization)
- `CredentialIssuer` service — takes (schema, claims, subject DID, issuer DID) → signs JWT-VC
- Use `vc+jwt` securing format (W3C `vc-jose-cose`) — reuses existing JWT infrastructure
- Issuer DID is a `did:web` Sigil controls (from Phase B)
- `IssuedCredential` table (FK to TrustDomain, FK to CredentialSchema, JSON column for claims, signed VC blob)
- UI: "Issue Credential" flow — pick schema, fill claims, pick subject DID, mint

**Done when:** You can issue a JWT-VC whose signature verifies against the issuer's `did:web` DID Document. Standard wallets / verifiers can read it.

**Why fourth:** This is the full SSI loop in minimum viable form. Foundation for everything that follows.

**Spec references:**
- `specs/w3c/vc-data-model-2.0/`
- `specs/w3c/vc-jose-cose/`

## Phase E — Bitstring Status List

**Goal:** VC revocation. Sigil publishes a status list; flipping a bit revokes a credential.

**Scope:**
- `StatusList` entity — per (TrustDomain, Issuer DID, purpose) — owns a bitstring
- `StatusListEntry` — assigns a position to each issued credential
- At issuance time, assign next available index, embed `credentialStatus` in the VC pointing to list URL + index
- Status list itself is a published VC (issued by the same issuer DID)
- Publishing endpoint for status list VCs
- UI: "Revoke" button on issued credential, status list viewer, history of revocations

**Done when:** Verifiers can fetch the status list, check the bit at the credential's index, and correctly accept/reject based on current status.

**Why fifth:** Closes the revocation story. Direct analog to CRLs, gives Sigil feature parity for VCs that it already has for X.509.

**Spec references:**
- `specs/w3c/vc-bitstring-status-list/`

## Phase F — Trust Registry

**Goal:** The cross-paradigm dashboard view. Per TrustDomain, list all trusted issuers — X.509 anchors AND DID issuers — in one place. Publish this as a registry document.

**Scope:**
- New `Sigil.TrustRegistry` project
- `TrustedIssuer` entity — polymorphic, points to either a `CaCertificate` or a `DidDocument`
- Per-issuer metadata: display name, governance doc URL, supported credential types
- Trust registry publication endpoint per TrustDomain
- UI: TrustDomain "Trust Registry" tab, add/remove trusted issuers, export as a published document

**Done when:** A relying party can fetch the trust registry for a TrustDomain and get a structured answer to "which entities does this domain trust, and what kind of material do they issue?"

**Why last:** This is the conceptual high point. It's where Sigil's "TrustDomain" concept fully realizes its multi-paradigm potential, and where TrustCartographer can pull a real cross-paradigm view to teach from.

**Spec references:**
- No single canonical spec. Trust registries are an ecosystem-level concern; ToIP and various OpenID drafts are circling the space. For now, define a Sigil-internal format and document it.

## Optional follow-ons

After F lands, natural extensions include:

- **OID4VCI issuer endpoints** — wrap Sigil's VC issuance behind the OpenID for VC Issuance protocol (see `specs/openid/oid4vci/`). This is what real wallets actually talk to.
- **SD-JWT-VC support** — second securing format, important for EU interop (see `specs/ietf/sd-jwt-vc/`).
- **Verifier helper library** — a `Sigil.Vc.Verifier` package that relying-party apps can consume.
- **HSM / PKCS#11 provider** — production-grade key custody.
- **`did:ion` or `did:ebsi` method** — blockchain-anchored DIDs if needed for specific ecosystems.

## TrustCartographer's track

TrustCartographer's own phases (per `PLAN.md`) can proceed in parallel:

- Phase 1 (Crypto Fundamentals) — already underway
- Phase 2 (DIDs) — build *in-memory toys first*, switch to Sigil API after Phase A lands
- Phase 3 (Verifiable Credentials) — same pattern, switch to Sigil API after Phase D lands
- Phase 4 (Trust Triangle animation) — uses Sigil-issued VCs after Phase D
- Phase 5 (Blockchain Anchoring & Comparative Views) — pulls trust-registry data from Sigil after Phase F

The two repos stay loosely coupled. TrustCartographer can always run on toy data for teaching; Sigil-backed mode is for showing the production-shaped flow.

## Related docs

- `sigil-and-trustcartographer.md` — roles and seams
- `pki-vs-ssi.md` — concept mapping
- `qa/openid-in-did-space.md` — where OID4VC fits
- `../specs/README.md` — specs grounding each phase
