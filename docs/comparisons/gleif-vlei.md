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

## The VITAL Initiative — vLEI for healthcare org identity

<https://vitalframework.org/> — the **VITAL Initiative** applies GLEIF's **vLEI to (US) healthcare
organizational identity**. This makes it the most direct healthcare-domain foil to UDAP: it's the
*organizational-trust* half in the same domain where the PIA program models *subject* identity.

> Captured from a saved PDF of the site (`docs/vitalframework.org.pdf`). That capture was a single
> clipped viewport (right margin cut off, likely more below the fold), so the summary below is the
> legible substance, not the full text — treat exact figures/phrasing as approximate until confirmed.

- **Framing / pitch:** *healthcare isn't the first industry to face this.* After the **2008 financial
  crisis** the G20 stood up **GLEIF** and the **LEI** to identify legal organizations; today **2.6M+
  entities** across many countries (and many US healthcare organizations) hold LEIs. The **vLEI** now
  brings that organizational identity into a **verifiable**, cryptographic form.
- **What the vLEI proves** (VITAL's own framing — "instantly and automatically," replacing manual
  human effort):
  - **WHO** an organization is,
  - **WHO** within that organization is authorized,
  - **WHAT** purposes of use they are authorized for.
- **Why this matters here:** that triplet maps almost 1:1 onto **UDAP** — *who* (org identity) +
  *who-is-authorized* (roles ≈ vLEI OOR/ECR) + **purpose-of-use** (≈ UDAP's **Authorization Extension
  Object**). So VITAL is a concrete, healthcare-specific instance of the "organizational trust behind
  the transaction" that TC's Phase 5 contrasts — vLEI/KERI-ACDC vs UDAP/X.509 answering the same
  WHO/role/purpose question. Pairs naturally with the PIA subject-assurance program.
- **TODO (when capturing more):** governance/sponsors of the VITAL Initiative itself, its relationship
  to GLEIF vs an independent healthcare body, and any pilot/spec details below the captured fold.

## Sources

- GLEIF — LEI & Digital Assets: <https://www.gleif.org/en/lei-solutions/featuring-the-lei/digital-assets>
- GLEIF — vLEI / Ecosystem Governance Framework: <https://www.gleif.org/en/vlei/introducing-the-vlei-ecosystem-governance-framework>
- VITAL Initiative — <https://vitalframework.org/> (saved capture: `docs/vitalframework.org.pdf`).
- ISO 17442 (LEI); KERI / ACDC specs (Trust over IP / WebOfTrust).
