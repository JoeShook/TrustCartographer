# Sigil Phase A + D — DID + VC support (pointer)

The detailed plan and implementation for adding DIDs and Verifiable Credentials to Sigil lives in the **Sigil repo** under `udap-tools/udap-dotnet/examples/CA/`. This page is a short summary so TrustCartographer's docs link to it.

## Scope

Covers two phases from [phased-roadmap.md](./phased-roadmap.md) together:

- **Phase A** — `did:key` minting using a software signing provider (Ed25519)
- **Phase D** — JWT-VC issuance signed by the issued DID + verifier service

Together: mint a DID, issue a VC against it, verify the VC. Full SSI loop, minimum infrastructure.

## What lives in Sigil

| Area | Location |
|------|----------|
| Detailed plan | `C:\Users\Joseph.Shook\.claude\plans\structured-baking-stroustrup.md` (session-local) |
| New projects | `examples/CA/Sigil.Did/`, `examples/CA/Sigil.Vc/` |
| Entity POCOs | `Sigil.Common/Data/Entities/{DidTemplate,DidDocument,VerificationMethod,CredentialSchema,IssuedCredential}.cs` |
| Migration | `Sigil.Common/Migrations/20260522223041_AddDidAndVcEntities.cs` |
| Signing extension | `Sigil.Common/Services/Signing/{ISigningProvider,LocalSigningProvider,RawPublicKey}.cs` (Ed25519 via BouncyCastle) |
| DID services | `Sigil.Did/Services/{IDidMethodProvider,DidKeyMethodProvider,DidTemplateService,DidIssuanceService,DidDocumentBuilder}.cs` |
| VC services | `Sigil.Vc/Services/{CredentialSchemaService,CredentialJwtBuilder,CredentialIssuanceService,CredentialVerifier}.cs` |
| UI pages | `Sigil.UI/Components/Pages/{DidTemplates,Dids,CredentialSchemas,Credentials}.razor(.cs)` |
| Tests | `_tests/Sigil.Signing.Tests/{DidKeyMethodProviderTests,DidIssuanceServiceTests,CredentialIssuanceServiceTests,CredentialVerifierTests}.cs` |

## Architectural choices made

- **Separate projects** (`Sigil.Did`, `Sigil.Vc`) reference `Sigil.Common`. Cleanly isolates new NuGet deps (SimpleBase, JsonSchema.Net) from the X.509 code.
- **Entity POCOs stay in `Sigil.Common`** alongside cert entities — keeps `SigilDbContext` single without circular project refs. New NuGet deps don't leak to `Sigil.Common` (entities are dep-free).
- **`ISigningProvider` reused** for DID/VC signing. Additive extension only: new `GetRawPublicKeyAsync` for raw key bytes (Ed25519 has no `AsymmetricAlgorithm` in .NET).
- **Roll our own JWT-VC** — no `Microsoft.IdentityModel.Tokens`. Header/payload/signature built by hand; BouncyCastle handles Ed25519 sign + verify. Matches the "minimal helpers" goal and keeps the mechanics visible.
- **Vault/GCP stub Ed25519** with `NotSupportedException` — wiring deferred to Phase C.

## What this unlocks for TrustCartographer

Once Sigil exposes an API (or TrustCartographer reads its DB directly), TrustCartographer Phase 2 (DIDs) and Phase 3 (VCs) can swap their in-memory toy implementations for real Sigil-minted material. The visualizer keeps owning the *teaching*; Sigil owns the *production-shaped storage and lifecycle*. See [sigil-and-trustcartographer.md](./sigil-and-trustcartographer.md) for the full division.

## What's next (deferred)

- **Phase B** — `did:web` + publishing endpoint
- **Phase C** — Wire Vault Transit + GCP KMS Ed25519
- **Phase E** — Bitstring Status List (real VC revocation; `IssuedCredential.Revoked` bool becomes a status list reference)
- **Phase F** — Trust Registry per TrustDomain (cross-paradigm view)
