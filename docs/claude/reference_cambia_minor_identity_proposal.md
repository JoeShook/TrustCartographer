---
name: reference-cambia-minor-identity-proposal
description: "Cambia \"Portable Identity Assurance in Healthcare\" proposal — a real OID4VC/SD-JWT exemplar that maps onto TrustCartographer + Phase 5"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 6b1ccb6a-277f-4760-92dd-c0f12f5d5930
---

Proposal received 2026-06 (machine-local file: `C:\temp\CMS\Portable Identity Assurance in Healthcare.docx`, Max Templeton / Cambia, DRAFT v0.9). Proposes a **portable health identity credential** (minor-identity pilot that generalizes) on **SD-JWT VC + OID4VC (OID4VCI+OID4VP)**, proofing via **NIST 800-63A rev4 delegated proofing**, assurance via **FAST IDIAL** (HL7 FHIR Identity Matching IG), governed by a **TEFCA-aligned trust framework**; EU EUDI Wallet cited as precedent. Rejects OIDC ID token (not portable) and SMART Health Card/CARIN (membership≠identity, no selective disclosure, bearer/no holder binding).

Why it matters for TrustCartographer:
- **Same stack** as the built trust triangle (OID4VCI issuer → wallet → OID4VP verifier).
- Its **Tier 4 governance** == the deliberate trust-list gap the Verifier surfaces — i.e. it is a worked instance of the [[project-trust-triangle-complete]] hook and feeds Phase 5.
- **New ideas to model**: SD-JWT **selective disclosure** (vs current all-or-nothing JWT-VC), **assurance levels** carried in the credential (FAST IDIAL/IAL, delegation ceiling + discount), delegated-proofing provenance, and a real verifier trust list.
- **Comparative-goal foil**: it's the SSI/OID4VC camp's answer and never mentions UDAP. Contrast: governance (TEFCA/FAST vs UDAP trust communities), third-party assurance (IAL2 CSP/FAST vs UDAP Certifications & Endorsements), minimal disclosure (OID4VP+SD-JWT data layer vs UDAP Authorization Extension Object purpose-of-use). Caveats: it's human identity (not agent-to-agent), and issuer trust is registry/PKI-flavored (not did:key).
