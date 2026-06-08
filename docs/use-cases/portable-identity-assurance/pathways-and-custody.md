# Issuance pathways, delegated proofing & custody transfer

The full multi-party realization of the proposal's **Tier 1–2** (delegate proofing → issuance) plus the
**custody lifecycle**. Where [minor-age-sd-jwt.md](minor-age-sd-jwt.md) is a single-process *simulation*
of selective disclosure, this is the **real topology**: each actor is its own running service behind the
YARP proxy, so the [Live Flow](../../../README.md) captures genuine cross-service traffic.

> **Source:** Max Templeton, Cambia Health Solutions — DRAFT v0.9 —
> <https://docs.google.com/document/d/11P-XSaal9cgpXTzp8W5Jc50ppZoeN7PF/edit>. This guide is our own
> analysis and re-expression; we do not reproduce the paper's text.

## Actors (real apps)

| App | Path | Role |
|---|---|---|
| Identity-CSP | `/identity-csp` | Delegated identity proofing (NIST 800-63A rev4) → a **FAST IDIAL** assurance level + a proofing-event record. |
| Payer-Issuer | `/payer-issuer` | **Pathway A** — identity established at payer enrollment (static IDIAL3). |
| Provider-Issuer | `/provider-issuer` | **Pathway B** — in-person photo-ID at the clinic (static IDIAL3). |
| CSP-Issuer | `/csp-issuer` | **Pathway C** — consults the Identity-CSP at offer time; embeds the CSP-derived effective IDIAL + `proofing_event_id`. |
| Guardian Wallet | `/holder` | Holds the minor's SD-JWT VC on the minor's behalf; can transfer custody. |
| Minor's Wallet | `/holder-minor` | Receives custody via re-issuance; thereafter the minor controls the credential. |
| Verifier | `/verifier` | OID4VP / SD-JWT relying party (e.g. urgent care needing only `age_over_18`). |

## Delegated proofing & assurance (Tier 1)

A responsible adult (the **delegate**) is proofed by some method; the Identity-CSP maps it to a FAST
IDIAL, then `DelegationPolicy` derives the minor's effective assurance under two rules:

- **Ceiling** — the minor's effective assurance can never exceed the delegate's.
- **Discount** — delegated proofing is a recognised-but-distinct pathway, marked "via delegation".
- **Floor** — below **IDIAL 2**, no minor credential may be issued (the CSP returns `issuable: false`).

The result is a **proofing event** (`pe-…`) an issuer can later consult. Run one at `/identity-csp/proof`.

Rough mapping: IDIAL 4 ≈ NIST IAL2 (Login.gov); IDIAL 3 is the practical middle ground; IDIAL 2 ≈ IAL1.

## Three issuance pathways (Tier 2)

All three mint the **same** Minor Identity SD-JWT VC, differing only in *how identity was established* —
the **assurance basis travels inside the credential** as selectively-disclosable claims
(`assurance_level`, `assurance_pathway`, `assurance_method`, `assurance_basis`):

- **A — payer enrollment** and **B — provider in-person** assert a *fixed* pathway level (no separate CSP).
- **C — IAL2 CSP** does not assert a level; it **consults the Identity-CSP** for the referenced proofing
  event, honours its issuable/floor decision (refuses with `400 assurance_insufficient` when not
  issuable), and embeds the CSP-derived effective IDIAL plus the `proofing_event_id` for traceability.

The consult is a real HTTP hop **through the proxy**, so Pathway C shows the CSP as its own Live Flow
lifeline — including the issuer's mid-issuance callback to the CSP.

## Custody transfer (the lifecycle)

A credential first held *on behalf of* a minor by the guardian is later **re-issued to the minor's own
wallet** once they can hold it (the proposal's "Phase 2"). On the Guardian wallet (`/holder/wallet`),
**Transfer custody to minor**:

1. Recovers the held SD-JWT's claims (disclosures).
2. Stamps custody provenance: `custody=self`, `prior_custody=guardian`, `custody_transferred_from=<guardian DID>`, `custody_transferred_at`.
3. Asks the **original issuer** to re-issue, with assurance **preserved** (carried forward, *not*
   re-derived) via the `preserveProvidedAssurance` flag.
4. The **minor's wallet** redeems the new offer with **its own key**, so the re-issued credential is
   key-bound (`cnf`) to the minor — not the guardian. (Verified by decoding both `did:key`s: the new
   `cnf.jwk.x` equals the minor's public key.)

In production the guardian's copy would be revoked at this point; the demo keeps it so the flow can be re-run.

## Selective-disclosure presentation (Tier 3)

The holder (guardian or minor) presents to the Verifier revealing **only** the requested claim
(`age_over_18`) plus a Key Binding JWT bound to the verifier's `nonce`/`client_id`. The verifier checks
the holder proof, then resolves the issuer's `did:key` to verify the signature — without ever seeing the
minor's name or date of birth. (The verifier still has **no trust list** — see the deliberate Tier-4 hook.)

## See it animate

`/live-flow` → **Run Portable Identity Assurance (CSP-backed)**: delegated proofing → CSP-backed
issuance (with the issuer's CSP consult) → guardian redeem (OID4VCI) → guardian presents `age_over_18`
(OID4VP). Open the 📨 on any step for the real request/response bytes.

## Code map

| Concept | Where |
|---|---|
| FAST IDIAL + delegation policy | `Core/Proofing/{IdialLevel,DelegationPolicy,ProofingEvent}.cs` |
| CSP proofing service + endpoints | `TrustCartographer.IdentityCsp/` (`ProofingCatalog`, `ProofingEndpoints`) |
| Issuance kit (shared) | `TrustCartographer.Issuing/` — `Oid4VciEndpoints`, `CredentialCatalog`, `IssuerProfile` |
| Assurance seam | `TrustCartographer.Issuing/Assurance.cs` (`IAssuranceSource`, `StaticAssuranceSource`); `CspIssuer/Services/CspAssuranceSource.cs` |
| Re-issuance flag | `CreateOfferRequest.PreserveProvidedAssurance` (skips the assurance source) |
| Custody transfer | `TrustCartographer.Wallet/CustodyClient.cs` + `/wallet/transfer-custody` |
| SD-JWT primitive | `Core/SdJwt/SdJwtService.cs` (`IssueSdJwtVc` / `Present` / `Verify` / `Decode`) |
| Live Flow PIA classifier | `UI/Pages/LiveFlow.razor.cs` (`ClassifyPia`, `PiaActors`) |

## SDK notes

The issuance/wallet **kits are the SDK seed**; the seven apps are samples that exercise the API surface.
The assurance seam (`IAssuranceSource`) and the custody re-issuance flag are deliberately transport-free
extension points: a future `TrustCartographer.Sdk` exposes them so new use cases compose issuance,
assurance, and custody without re-implementing the OID4VCI/SD-JWT mechanics.

## UDAP comparison (where this is heading)

The assurance basis traveling *inside* the credential is the SSI analog of UDAP's certificate-/metadata-
borne trust. The open question this program drives toward (Tier 4): **who is authoritative?** — the
verifier's missing trust list is the on-ramp to the SSI-vs-UDAP comparison.
