# Organizational trust models — UDAP vs vLEI vs CMS software statements vs generic SSI

Phase 5 of TrustCartographer is the comparative payoff: *who is authoritative, and how is that trust carried?*
The live build deliberately leaves the verifier with **no trust list** (it proves *who* signed, not that they're
authoritative) — the on-ramp to comparing real-world answers. This note captures four design points worth
contrasting, and how each maps onto the project. It is analysis/framing, not a reproduction of the sources.

## The four design points

| Axis | **UDAP** | **vLEI (GLEIF)** | **CMS software statements** | **generic SSI** (what TC builds) |
|---|---|---|---|---|
| Trust topology | Federated **trust communities** (multiple CAs, shared anchors) | Global root → accredited issuers | **Single authority** (CMS is the registrar/vetter) | Ad hoc (no anchor yet — the Tier-4 hook) |
| Subject | Org / endpoint (X.509 SAN) | **Legal entity** (LEI, ISO 17442) | **App / software client** | Person / minor (subject DID) |
| Artifact | X.509 cert + metadata + **Authorization Extension Object** | **ACDC** role credentials (OOR/ECR), KERI | **Signed JWT** (RFC 7591 `software_statement`) | JWT-VC / **SD-JWT VC** |
| Issuer chain | CA → community trust anchor | GLEIF → **QVI** → Legal Entity → role | CMS signs directly | issuer `did:key` (resolvable, untrusted) |
| Revocation | CRL / OCSP | KEL / witnesses | **TTL + stop-reissue** (freshness = revocation) | status lists (planned) |
| Third-party attestation | **Certifications & Endorsements** | OOR/ECR role credentials | (vetting result, not portable as attestation) | not yet |
| Authorization semantics | **Purpose-of-use** (B2B), rich | role context | requestable data (registration-scoped) | claim disclosure (SD-JWT) |
| Scope it actually solves | Cross-org authZ + registration | Org identity, global | **App registration trust** | Subject identity assurance |
| Governance | Trust Communities | vLEI Ecosystem Governance Framework | CMS program rules | none |

## One-line characterizations

- **UDAP** — PKI-native, **federated** org trust with no central registrar; the heavyweight, general answer.
  Strengths to surface: Authorization Extension Object (purpose-of-use), Certifications & Endorsements, Trust
  Communities (governance).
- **vLEI (GLEIF)** — SSI-native, **globally-governed** org identity on a *different* stack (KERI/ACDC, not
  DIDs+JWTs). QVI chain ≈ UDAP CA/community chain; OOR/ECR ≈ Certifications & purpose-of-use. Now being pushed
  as a multi-chain **trust anchor for digital assets** (MiCA, Chainlink, Cardano vLEI-on-chain).
  **Deep-dive:** [gleif-vlei.md](gleif-vlei.md). See
  <https://www.gleif.org/en/lei-solutions/featuring-the-lei/digital-assets>.
- **CMS software statements** (Josh Mandel) — make an *existing* vetting decision **portable** via a CMS-signed
  short-lived JWT (`software_id`, `jwks_uri`, `library_status`), ingestible as an RFC 7591 `software_statement`
  at `/register`. Deliberately **not a CA**; freshness-as-revocation; key custody stays with the app. It's a
  **hub, not a federation** — right-sized because CMS is the single authority that already vets — and it's
  **registration trust, not authorization-context trust**. Notably silent on UDAP: *UDAP for
  federated/no-central-authority; software statements for a single authority that's already the registrar.*
  See <https://www.linkedin.com/pulse/software-statements-medicare-app-library-josh-mandel-md-robze/>.
- **generic SSI** — the substrate TC implements (did:key, JWT-VC, SD-JWT, OID4VC). Powerful at **subject**
  identity + selective disclosure, but the *authoritative-party* question is exactly what it leaves open.

## Two halves of the trust triangle

A useful framing that fell out of comparing these:

- **Subject identity assurance** — *is this the right person, and how strongly proofed?* → the PIA program
  (delegated proofing → FAST IDIAL → SD-JWT VC). See `../use-cases/portable-identity-assurance/`.
- **Client / software trust** — *is this a known, vetted app, and what's its key?* → CMS software statements
  (RFC 7591). UDAP and vLEI carry the **organizational** trust that sits behind both.

A complete trust story needs all of these; TC can demonstrate each and contrast the topologies.

## Build sketch — UDAP JWT vs vLEI/ACDC client auth

The VITAL white paper reframes this from "vLEI vs UDAP" to **vLEI as a UDAP client-auth variant** over the
same OAuth2/FHIR flow. A concrete build sketch for a comparative view / Live Flow (one flow, swappable
client-assertion + trust anchor) is in [clientauth-comparison-sketch.md](clientauth-comparison-sketch.md).

## How this drives the build (Phase 5 + Tier-4)

- **Tier-4 hook → concrete:** the missing verifier trust list. The lowest-effort realization is a
  CMS-software-statement-style **issuer/app trust list** (signed statement = the authoritative-party assertion);
  the richer realizations are a UDAP-style community anchor or a vLEI QVI chain.
- **SDK north star:** an RFC 7591 **`software_statement` issuer + verifier** is a small, real capability that
  pairs with the PIA issuance/wallet kits — modeling *client trust* next to *subject assurance*. See
  `../../CLAUDE.md` (SDK north star) and the PIA deep-dives.

## Sources
- GLEIF — LEI & Digital Assets: <https://www.gleif.org/en/lei-solutions/featuring-the-lei/digital-assets>
- Josh Mandel — "Software Statements for the Medicare App Library":
  <https://www.linkedin.com/pulse/software-statements-medicare-app-library-josh-mandel-md-robze/>
- UDAP: see `../../specs/` (UDAP profiles) and the UDAP.org SDKs/reference implementations.
