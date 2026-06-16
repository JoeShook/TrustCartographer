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
