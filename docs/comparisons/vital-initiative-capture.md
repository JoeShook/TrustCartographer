# VITAL Initiative — offline text capture

> Verbatim-ish text extracted from a saved copy of <https://vitalframework.org/> (captured 2026-06-16).
> Kept as an offline record because the live site is a JavaScript single-page app. Analysis/commentary
> lives in [gleif-vlei.md](gleif-vlei.md); this file is just the source text.

---

**VITAL Initiative — Verifiable Identity and Trust Automation Layer**

Piloting a trusted, automated framework for healthcare organizational ID via open source, open standard protocols.

Do we know who we're exchanging with? Can we prove it?

## No Identity, No Interop

Every new API, every new connection, and every new partner requires a trust conversation. The volume and
methods of connection have expanded but how we establish trust with the org on the other end hasn't changed.

The industry has made incredible progress in the realm of individual ID. Organizational ID hasn't kept pace.

Orgs still manually onboard newcomers, building trust from scratch every new connection. And those newcomers
have to run through the same hoops time and time again.

The result?
- Onboarding takes weeks
- Trust decisions rely on spreadsheets, contracts, and manual human review
- As connectivity grows, manual trust across networks just isn't scalable.

Already, healthcare exchanges data billions of times each year. As FHIR-based APIs come online, that rate of
connection will continue to grow. We cannot continue to rely on manual, duplicative onboarding that is slow,
expensive, and fragile.

As an industry we've answered the question, "can we connect?" We can. But we are still asking, "Can we trust
this connection?"

The bottleneck has become urgent:
- **CMS-0057-F:** By January 1, 2027, mandatory API readiness for payer-to-payer exchange goes into effect (among other types).
- **Healthcare Provider Directories:** Directories independently collect organizational data, resulting in duplication and inaccuracies.
- **Inadequate ID:** Existing identifiers (NPIs, TINs, and standard digital certificates) all have serious limitations and were not designed to serve as universal organizational identifiers.

The Deadlines: CMS-0057 & Provider Directories.

## The Solution: Importing a Proven, Global Standard

Healthcare isn't the first industry to face the organizational ID problem. Following the 2008 financial crisis,
the G20 established the Global Legal Entity Identifier Foundation (GLEIF), to create the LEI, a globally unique
20-character code that identifies legal organizations.

Today, over 2.6 million entities in more than 200 countries hold LEIs, including many US healthcare
organizations. Now, the open-source, ISO-standardized Verifiable LEI (vLEI) brings organizational identity
into the digital realm.

### What is the vLEI?

In practice, the vLEI proves who can do what on behalf of which organizations across large-scale networks,
without requiring manual, human effort. The vLEI is a cryptographically verifiable credential that acts as a
tamper-proof digital assertion. It proves instantly and automatically:
- **WHO** an organization is
- **WHO** within that organization is authorized to act on its behalf
- **WHAT** purposes of use they are authorized to assert.

## The VITAL Initiative

The VITAL Initiative is an open call for real-world vLEI pilots, championed by the **CARIN Alliance**,
**DirectTrust**, and **GLEIF** along with industry partners. We are seeking participants for two primary pilots:

- **Payer to Payer** — Enable payers to establish fast, trustworthy, and automated FHIR connections to meet the CMS-0057-F mandate.
- **Healthcare Directories** — Utilize the vLEI as a viable, global standard for organizational identifiers across existing healthcare directories.

### Why Participate?

The future of healthcare data exchange is automated verification of digital connections — with trust at scale.
With trusted connections, we can finally unlock the latent power of healthcare's data.
- **Speed** — Connections that currently take weeks of manual coordination are established in a day.
- **Scale** — Hundreds of thousands of connections can be established without incurring costs or staffing increases.
- **Trust** — Every connection is cryptographically verified, not just contractually attested.

### Who Should Join?

- **Payers** — Payers and health plans that are subject to CMS-0057-F requirements.
- **Providers** — Healthcare providers looking to establish FHIR-based APIs at scale.
- **Partners** — Interoperability vendors, HISPs, industry orgs focused on data exchange.

Many healthcare organizations — including publicly traded health systems, payers engaged in financial hedging,
and large non-profits — already hold LEIs, giving the industry a significant head start.

### Project Steps

Steps from obtaining an LEI to establishing secure, trustworthy FHIR-based API connections between participants:
1. Obtain or Confirm LEI
2. Obtain vLEI
3. Issue vLEI Role Credentials
4. Provision Secure Deployment
5. Establish Trusted Connections

### Resources (presented via the CARIN Alliance P2P Touchpoint Meetings)

- CARIN Alliance and DirectTrust at ViVE
- VITAL Initiative Workflow and Processes
- VITAL Initiative Introductory Slide Deck
- Original VITAL Initiative White Paper

---

# VITAL White Paper — "Jumpstarting Healthcare Exchange with Reliable Organizational Identity"

> Source: `docs/Jumpstarting_Reliable_Org_Identity.pdf` — the **full 14-page** white paper with a text
> layer (supersedes the earlier 2-page screenshot). This section is a faithful condensation of the key
> content; the complete text is extractable from the committed PDF.

**Presented by:** CARIN Alliance & DirectTrust. **In Partnership With:** GLEIF and Industry Partners.
**Cover partners:** CARIN, DirectTrust, GLEIF, onyx, healthKERI. **Date:** March 2026.

## Section 1 — Summary and Pilot Proposals
- **1.1 The Problem:** "Who is on the other end of this connection, and are they authorized to receive
  this data?" Networks each manually onboard the same orgs (weeks/connection); trust gaps have triggered
  lawsuits and weakened exchange frameworks.
- **1.2 The Solution — GLEIF / LEI / vLEI:** the LEI (20-char, ISO, G20/GLEIF, 2.6M+ entities in 200+
  countries) is the proven financial-services answer; the open-source, standardized **vLEI** extends it to
  a cryptographically verifiable credential proving "who can do what on behalf of which organizations."
  Drivers: **CMS-0057-F** (payer-to-payer / prior-auth / patient-access API readiness by **Jan 1 2027**)
  and the **CMS National Provider Directory (NPD)**.
  > Callout: "There is no single, globally recognized, digitally verifiable organizational identifier…
  > Every solution proposed so far — National Provider Identifiers, Tax ID Numbers, digital certificates —
  > has material gaps."
- **Pilot A — Payer-to-Payer:** automated FHIR connections using vLEI credentials + digital certificates
  to prove identity and assert exchange purposes; meets CMS-0057-F. Uses **Dynamic Client Registration**
  enabled by the **SSRAA Implementation Guide** (security standard for Facilitated FHIR under TEFCA).
  Leverages the **Engagement Context Role (ECR)** credential to assert a specific exchange purpose (e.g.
  payer-to-payer under CMS-0057-F) and to identify a specific plan/product — building a "trusted community
  of practice." Drivers also cite **TEFCA expansion** and **ISO 17442-3** (vLEI standard, Oct 2024).
- **Pilot B — Healthcare Directories:** establish the LEI as a standard org identifier across directories
  (incl. CMS NPD) so verified data is entered once and shared. Targets DirectTrust HISPs/CAs/RAs, CSPs,
  clearinghouses, HIEs/HDUs, QHINs. DirectTrust Aggregated Directory is a confirmed participant.
- **Four steps:** confirm/obtain LEI (LOU; <1 hr, <$100, few days) → acquire vLEI + issue delegated
  credentials → provide verified data to a participating directory → establish trusted connections.
- **Contacts:** Ryan Howells (CARIN Alliance); Scott Stuewe (DirectTrust).

## Section 2 — Background: the org-identity problem & GLEIF
- **2.1** Individual identity is solved (CSPs do IAL2/AAL2), but most workers act *on behalf of orgs*;
  org identity is the dimension least reliably captured.
- **2.2 Why existing identifiers fall short:**
  - **Type-2 NPIs** — only Medicare billers; tied to billing locations, not org hierarchy.
  - **TINs/EINs** — align at any hierarchy level, no ownership/line-of-business info, proprietary.
  - **Digital Certificates** — for crypto auth, not org identifiers; the **O-value is not globally unique**
    ("there are 27 St. Mary's Hospitals across 19 states") and certs carry little authorization/relationship
    info. *(This is the explicit critique of cert-based identity — relevant to UDAP, which uses X.509.)*
- **2.3** Cost of the status quo: every network (TEFCA, Carequality, CommonWell, DirectTrust, HIEs)
  re-onboards the same orgs; trust disputes contributed to lawsuits weakening Carequality and TEFCA. FHIR's
  hierarchy problem: servers sit at enterprise/region/hospital/dept level — names are unreliable; LEI-based
  directory entries fix endpoint discovery.
- **2.4 GLEIF & the LEI system:** GLEIF (2014, FSB/G20 ROC) runs the Global LEI Index; ~40 **LOUs** issue
  LEIs; LEI emerged from the 2008 crisis. Six US financial rules (SEC/CFTC/Treasury/Fed) require the LEI.
- **2.5 The vLEI:** announced 2020; Ecosystem Governance Framework + first QVIs 2021; KERI technical
  requirements 2022; **GLEIF vLEI Technical Advisory Board incl. healthKERI (July 2024)**; **ISO 17442-3
  (Oct 2024)**; Partner Program 2025. Proves WHO the org is + WHO the individual is + WHAT they're
  authorized to do. LEI data: **Level 1 "who is who"**, **Level 2 "who owns whom."**
- **2.6** Financial-services parallel (LEI in EMIR/MiFID II/Dodd-Frank/ISO 20022); EU **DORA** cites
  LEI/vLEI; healthcare regulators (ASTP/ONC, CMS, VA) could tie LEI to Medicare participation, Type-2 NPI
  issuance, or TEFCA QHIN onboarding.

## Section 3 — How the vLEI works (credentials + infrastructure)
- **3.1 Trust chain:** GLEIF (root) → **QVIs** → Legal Entities → Role Holders / Authorized Reps; every
  credential cryptographically traceable to GLEIF.
- **3.2 Credential types:** (1) **QVI** credential, (2) **Legal Entity vLEI** credential, (3) **QVI AUTH**
  (OOR-AUTH / ECR-AUTH; LAR authorizes a QVI; revoking cascades), (4) **OOR** (Official Organizational
  Role — legally registered officers; legally binding signatures), (5) **ECR** (Engagement Context Role —
  operational roles AND **exchange-purpose** assertions, e.g. payer-to-payer under CMS-0057-F; can identify
  a plan/product — sub-org identification no current identifier supports).
- **3.3 Tech stack:** **KERI** (AIDs, append-only KELs, key pre-rotation), **ACDC** (SAIDs, composable
  chaining), **CESR** (encoding), **JSON Schema 2020-12**. Trust anchoring via **witnesses** (KAWA, min
  pool 5) + **watchers**. **IPEX** issuance/presentation protocol; **PTEL** revocation registries. Ed25519
  signatures, BLAKE3 SAIDs, ~128-bit strength; TEEs/HSMs; selective disclosure for PII in ECRs. Open-source
  Python KERI/ACDC/CESR; witness/watcher reference impls expected **April 2026**.
- **3.4 Integration with OAuth 2.0 + FHIR (the UDAP bridge):** *"The **UDAP vLEI/ACDC Client Authentication
  specification** defines a client authentication profile for OAuth 2.0 token endpoints… **semantically
  equivalent to the existing UDAP JWT-Based Client Authentication specification used in the SSRAA
  Implementation Guide for Facilitated FHIR under TEFCA**, differing only in the cryptographic assertion
  format and verification mechanism, and adding extended capabilities through KERI identifier delegation."*
  A payer FHIR client presents its vLEI in the OAuth 2.0 request (CESR-encoded IPEX Grant); the AS validates
  the signature + trust chain and grants the token with no manual intervention; each org hosts its own IdP
  ("true zero-trust"). Open-source Client Library, Server Library, and an example vLEI/OAuth2 healthcare
  portal expected **April 2026**.

---

# ViVE 2026 video — "No Identity, No Interop" (DirectTrust, ~21 min)

> Summary extracted by Joe from the video transcript (<https://www.youtube.com/watch?v=9rm4QZ6jv6Y>);
> the video is the same VITAL messaging in talk form. Speakers: **Scott Stuewe** (President & CEO,
> DirectTrust) and **Ryan Howells** (Principal, Leavitt Partners), plus a brief CMS announcement.

**Thesis — "No Identity, No Interop":** B2B exchange under TEFCA / CMS-aligned networks / upcoming FHIR APIs
requires solving **both individual and organizational identity** plus **purpose-of-use** assertions.

**Challenges:** individual identity is largely solved (ID.me, CLEAR, IAL2/AAL2); the org context (who the
person represents) is the hard part. Type-2 NPIs identify care locations not orgs; Tax IDs are opaque;
digital certificates rely on string names that don't disambiguate (multiple "St. Mary's" across states).
The need: prove "who you are, what organization you represent, and why you want the data" before B2B FHIR
(Jan 1, 2027).

**Solution — GLEIF / LEI / vLEI:** ISO standard post-2008; ~50 countries, 26+ US regulations; public LEI
registry. LEI ≈ **$50/year**, easy to obtain; gives ownership hierarchy / ultimate parent (e.g. a "St.
Mary's" → Intermountain Healthcare). **vLEI** = cryptographically verifiable credential tying identity-
proofed individuals (with role/context) to the org, enabling purpose-of-use assertions. Open, standards-
based, low-cost, federates across trust frameworks.

**Trust frameworks / policy:** healthcare pioneered a common **Care and Digital Identity Credentialing
Policy** harmonizing NIST, DirectTrust, CAQH, PKI (X.509/RFC) — interoperable credentials across
frameworks; combines with **UDAP certificates** for FHIR + IAL2 proofing. Other industries (finance) are
interested.

**Next steps / announcements:**
- Testing at scale starting with **payer-to-payer FHIR APIs (14+ major national payers)** before broader
  rollout; providers/others invited (contact DirectTrust / Leavitt).
- **CMS Medicare App Library** launching **end of March 2026**; apps must adhere to the **CARIN Code of
  Conduct** and be **certified by CARIN or DirectTrust** to be listed (ties to software-statement / app
  vetting — see [org-trust-models.md](org-trust-models.md)).

**Framing:** an **enumeration / disambiguation** strategy (not just a directory), built on proven open
standards + healthcare-specific profiling by conveners (DirectTrust). Applies to TEFCA, the National
Provider Directory, etc.
