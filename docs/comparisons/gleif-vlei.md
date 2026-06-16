# GLEIF, the LEI, and the vLEI — global organizational identity

A deep-dive on **organizational identity** as a real-world trust framework, captured for Phase 5
(the SSI vs UDAP comparative views). This is the org-identity foil that sits alongside UDAP; the
broad side-by-side lives in [org-trust-models.md](org-trust-models.md). Analysis/framing, not a
reproduction of the sources.

> Why it matters here: TrustCartographer's deliberate Tier-4 hook is that the verifier proves *who*
> signed, not that they're *authoritative*. GLEIF/vLEI is the cleanest real-world answer to "who is
> authoritative for organizational identity," and a different SSI stack from the one TC implements —
> so it's a strong comparison point.

## GLEIF & the LEI

- **GLEIF** (Global Legal Entity Identifier Foundation) — not-for-profit, established at the
  direction of the **G20 / Financial Stability Board** after the 2008 crisis, to steward a single
  global identifier for legal entities.
- **LEI** — a 20-character **ISO 17442** identifier for a legal entity, plus its "business card"
  reference data (legal name, address, and parent/child "who-owns-whom" relationships). Widely
  **mandated in financial regulation** (MiFID II, EMIR, Dodd-Frank reporting, etc.).
- **Issuance** — GLEIF accredits **LOUs** (Local Operating Units) that issue and maintain LEIs;
  the full registry is **free and public**.
- Limitation for our purposes: the LEI by itself is a *registry identifier* — it answers "who" but
  is **not a cryptographically verifiable presentation**. That gap is what the vLEI fills.

## vLEI — the verifiable form (the interesting part)

The **vLEI** (verifiable LEI) is GLEIF's credential-native layer, and it runs on a **different SSI
stack** from TC's current W3C-VC / `did:key` / SD-JWT / OID4VC:

- **KERI** (Key Event Receipt Infrastructure) — autonomic identifiers (**AIDs**) whose control is
  proven by a **Key Event Log (KEL)** witnessed by **witnesses**, independent of any blockchain or
  DNS/PKI. Key rotation and recovery are first-class (pre-rotation).
- **ACDC** (Authentic Chained Data Containers) — the credential format: chained, graph-structured
  credentials linked by cryptographic **edges**, content-addressed by **SAIDs**, serialized with
  **CESR**. Not JWTs.
- **Chain of trust by credential edges**, governed by the **vLEI Ecosystem Governance Framework (EGF)**:
  1. **GLEIF** — root of trust.
  2. **QVI** (Qualified vLEI Issuer) — organizations GLEIF accredits to issue vLEIs (the RA/CA-equivalent).
  3. **Legal Entity vLEI** — issued by a QVI to the entity.
  4. **Role credentials**:
     - **OOR** (Official Organizational Role) — ties a person to a role recorded in an official
       public source (e.g. "CEO").
     - **ECR** (Engagement Context Role) — a contextual role asserted by the entity (e.g. "authorized
       signer for purchasing").

## How it maps onto TrustCartographer / vs UDAP

| | UDAP | vLEI (GLEIF) | generic SSI (what TC builds) |
|---|---|---|---|
| Identifier | X.509 SAN / cert | **LEI** (ISO 17442) | `did:key` |
| Credential | cert + metadata + Authorization Extension Object | **ACDC** role creds (OOR/ECR) | JWT-VC / SD-JWT |
| Trust anchor | community trust anchors + endorsements | **GLEIF root → QVI** | *(none yet — the hook)* |
| Key model | X.509 / CA | **KERI** AIDs + KELs + witnesses | Ed25519 `did:key` |
| Revocation | CRL / OCSP | KEL / witness receipts | status lists (planned) |
| Governance | Trust Communities | **vLEI EGF** | none |
| Domain | B2B FHIR / healthcare authZ | global finance → digital assets | teaching substrate |

Read across: the **QVI chain ≈ UDAP's CA / community-anchor chain**, and **OOR/ECR role credentials
≈ UDAP's Certifications & Endorsements + purpose-of-use**. vLEI is the *globally-governed, SSI-native*
answer to org trust; UDAP is the *federated, PKI-native* answer (no central registrar). Same problem,
different topology and governance.

## The digital-assets push (the linked page)

GLEIF is now positioning the LEI/vLEI as a **neutral, multi-chain trust anchor for organizational
identity in tokenization / on-chain finance** — closing the "who is issuing, holding, or transacting
this digital asset?" gap across ledgers and permissioned environments. Threads it points to:

- **EU MiCA** — Commission Delegated Regulation (EU) 2025/305.
- **Chainlink × GLEIF** — digital identity + automated compliance in financial services.
- **vLEI on-chain** — "Verifiable Smart Contracts" (Cardano Foundation / Key State Capital).
- **SODA** (Standards Organization for Digital Assets) — interoperability standards.

This is org-identity-as-trust-anchor extended from finance into digital assets — adjacent to, but
broader than, the healthcare subject-identity TC models in the PIA program.

## Where this could go in the build (later)

- **Tier-4 trust list** — model the verifier's "authoritative issuer" entry keyed by an LEI / a
  QVI-style chain, making the abstract hook concrete with a real framework.
- **Comparative view** — render UDAP vs vLEI vs generic-SSI org trust side by side (topology,
  artifact, chain, governance), using the table above.
- KERI/ACDC is a *different* stack from TC's Core — a vLEI demo would be a separate spike, not a
  reuse of the SD-JWT/OID4VC kits; worth noting before committing effort.

## The VITAL Initiative — vLEI for healthcare organizational identity

<https://vitalframework.org/> — **VITAL = Verifiable Identity and Trust Automation Layer**: "a trusted,
automated framework for **healthcare organizational ID** via open-source, open-standard protocols." It
is an **open call for real-world vLEI pilots**, championed by the **CARIN Alliance, DirectTrust, and
GLEIF** plus industry partners. This is the single most relevant external foil for TC's Phase 5: it
applies the vLEI to the **same B2B-FHIR cross-org trust problem UDAP targets**, in the same domain where
the PIA program models *subject* identity.

> Source: saved capture of the site (`docs/VITAL Initiative Website.html` + `docs/vitalframework.org.pdf`).

- **Problem it names:** individual ID has progressed, but **organizational ID hasn't kept pace.** Orgs
  still onboard each new partner manually — "weeks," trust via "spreadsheets, contracts, and manual human
  review" — which doesn't scale as FHIR APIs multiply. Existing identifiers (**NPIs, TINs, and standard
  digital certificates**) are called out as having "serious limitations" and "not designed to serve as
  universal organizational identifiers." *(Note for us: UDAP's trust rests on exactly those X.509 certs +
  trust communities — so VITAL implicitly positions vLEI as succeeding where certs fall short. That's the
  sharp comparison to draw.)*
- **Regulatory driver:** **CMS-0057-F** — by **Jan 1, 2027**, mandatory API readiness for **payer-to-payer**
  exchange (among others); plus duplicated/inaccurate **healthcare provider directories**.
- **Solution — import a proven global standard:** post-2008 the G20 created **GLEIF / the LEI**; today
  **2.6M+ entities in 200+ countries** hold LEIs (many US healthcare orgs already do — a "head start").
  The **open-source, ISO-standardized vLEI** brings that identity into verifiable digital form. The vLEI
  is a tamper-proof, cryptographically verifiable credential proving instantly/automatically:
  - **WHO** an organization is,
  - **WHO** within it is authorized to act on its behalf,
  - **WHAT** purposes of use they are authorized to assert.
- **Two pilots:**
  - **Payer-to-Payer** — fast, trustworthy, automated FHIR connections to meet CMS-0057-F.
  - **Healthcare Directories** — the vLEI as a global standard org identifier across existing directories.
- **Claimed benefits:** **Speed** (connections that take weeks → a day), **Scale** (hundreds of thousands
  of connections without added cost/staff), **Trust** ("cryptographically verified, not just contractually
  attested").
- **Who should join:** Payers (subject to CMS-0057-F), Providers (FHIR APIs at scale), Partners (interop
  vendors, HISPs, industry orgs).
- **Pilot steps:** Obtain/Confirm LEI → Obtain vLEI → Issue vLEI **Role Credentials** → Provision Secure
  Deployment → Establish Trusted Connections.
- **Resources referenced:** CARIN Alliance + DirectTrust at ViVE; VITAL Workflow & Processes; an
  Introductory Slide Deck; the **Original VITAL White Paper** (presented via CARIN Alliance P2P Touchpoint
  Meetings) — worth capturing next for the technical/governance depth.

**Why it matters for the comparison:** VITAL's **WHO / who-is-authorized / purpose-of-use** triplet maps
almost 1:1 onto **UDAP** (org identity + roles ≈ vLEI **OOR/ECR** + **purpose-of-use ≈ UDAP's Authorization
Extension Object**) — but answers it with **vLEI / KERI-ACDC** instead of **X.509 + trust communities**,
for the *same* payer-to-payer FHIR use case. It's the cleanest head-to-head for "SSI vs UDAP" in
healthcare org trust, and the organizational complement to the PIA subject-assurance program.

## Sources

- GLEIF — LEI & Digital Assets: <https://www.gleif.org/en/lei-solutions/featuring-the-lei/digital-assets>
- GLEIF — vLEI / Ecosystem Governance Framework: <https://www.gleif.org/en/vlei/introducing-the-vlei-ecosystem-governance-framework>
- VITAL Initiative — <https://vitalframework.org/> (CARIN Alliance · DirectTrust · GLEIF; saved
  captures: `docs/VITAL Initiative Website.html`, `docs/vitalframework.org.pdf`). Next: the VITAL
  White Paper + slide deck for governance/technical depth.
- ISO 17442 (LEI); KERI / ACDC specs (Trust over IP / WebOfTrust).
