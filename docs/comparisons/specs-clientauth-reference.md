# Spec reference — UDAP client auth & vLEI EGF (for the client-auth comparison)

Captured technical detail behind [clientauth-comparison-sketch.md](clientauth-comparison-sketch.md).
Summarized from the public specs (links at bottom); not a reproduction. Verify against the source before
implementing.

## UDAP JWT-Based Client Authentication (token endpoint, B2B `client_credentials`)

The client authenticates with a **signed Authentication Token JWT** (`token_endpoint_auth_method =
private_key_jwt`); no client secret. JOSE header carries `alg` + **`x5c`** (the X.509 cert chain) — trust
flows from that certificate to a **trust-community anchor**.

Token request (form params): `grant_type=client_credentials`, `scope`, `udap=1`,
`client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer`, `client_assertion=<JWT>`.

Authentication Token JWT claims:

| Claim | Req | Value |
|---|---|---|
| `iss` | ✔ | the app's `client_id` |
| `sub` | ✔ | the app's `client_id` (== `iss`) |
| `aud` | ✔ | the AS **token endpoint URL** |
| `exp` | ✔ | epoch secs; **`exp − iat` ≤ 300** |
| `iat` | ✔ | epoch secs |
| `jti` | ✔ | unique nonce |
| `extensions` | cond | holds `hl7-b2b` (required for `client_credentials`) |

## UDAP B2B Authorization Extension Object (`extensions.hl7-b2b`)

The **purpose-of-use carrier** — the direct analog of the vLEI **ECR** credential.

| Member | Req | Notes |
|---|---|---|
| `version` | ✔ | `"1"` |
| `subject_name` | cond | requestor name (if known) |
| `subject_id` | cond | e.g. NPI as URI |
| `subject_role` | cond | role code |
| `organization_name` | opt | |
| `organization_id` | ✔ | a URI (resolvable if a URL) |
| `purpose_of_use` | ✔ | array of codes |
| `consent_policy` | opt | array of URIs |
| `consent_reference` | cond | array of FHIR Consent/DocRef URLs |

Identifier/code format: a URI, else `namespaceURI#identifier`. Examples: NPI →
`urn:oid:2.16.840.1.113883.4.6#1234567890`; treatment → `urn:oid:2.16.840.1.113883.5.8#TREAT`.

```json
{
  "iss": "https://payer-a.example.com/client_id",
  "sub": "https://payer-a.example.com/client_id",
  "aud": "https://payer-b.example.com/token",
  "exp": 1700000300, "iat": 1700000000, "jti": "nonce-...",
  "extensions": { "hl7-b2b": {
    "version": "1",
    "organization_id": "https://payer-a.example.com",
    "purpose_of_use": ["urn:oid:2.16.840.1.113883.5.8#HPAYMT"]
  }}
}
```

## UDAP Dynamic Client Registration — software statement

Signed JWT (header `alg` + `x5c`; signed by the cert's private key) POSTed to `/register` as
`{ "software_statement": "<JWT>", "certifications": ["<JWT>"], "udap": "1" }`. Claims: `iss`/`sub` = a unique
client URI **matching a SAN in the cert**, `aud` = registration URL, `exp`/`iat`/`jti`, `client_name`,
`grant_types` (`authorization_code` XOR `client_credentials`), `token_endpoint_auth_method="private_key_jwt"`,
`scope`, `contacts`, (+`redirect_uris`/`logo_uri`/`response_types` for `authorization_code`). Server validates
JWT payload+sig, the **X.509 chain**, and the params; returns `201` + `client_id`. Empty `grant_types` = cancel.
(`iss` scheme is unconstrained — `https` **or `did`**, a hook for the vLEI variant.)

## UDAP trust communities

X.509 is the basis of community trust; the client URI in the cert **SAN** uniquely identifies the app+operator
over time. Communities define PKI policies, server URIs, a community-identifier URI, and `scopes_supported`.

## SSRAA

"Security for Scalable Registration, Authentication, and Authorization" — the **TEFCA Facilitated FHIR**
security profile, built on the UDAP Security IG above (the white paper's "UDAP JWT-Based Client Authentication
… used in the SSRAA IG"). Published via the RCE (The Sequoia Project); not separately captured here — the
UDAP IG is its technical basis. *TODO: capture the SSRAA IG itself if a stable URL is found.*

## vLEI Ecosystem Governance Framework (EGF v4.0, March 2026)

Based on the **Trust over IP Governance Metamodel**. Delegated hierarchy of **KERI Autonomic Identifiers (AIDs)**:

- **GLEIF Root** (Root AID + GLEIF Delegated AIDs) → **QVIs** (QVI Delegated AIDs, via GLEIF's qualification
  program) → **Legal Entity** → **OOR** / **ECR** role holders.
- **Five credential types:** QVI vLEI Credential · Legal Entity vLEI Credential · **Authorization** vLEI
  Credential (a **LAR** authorizes a QVI to issue/revoke OOR/ECR) · **OOR** (official roles) · **ECR**
  (functional roles **and exchange purpose-of-use**; issuable by the QVI or the Legal Entity itself).
- **Governance docs:** Primary Document + Controlled Documents; Identifier Governance Frameworks (GLEIF Root,
  QVI); Glossary/Business/Governance Requirements; Trust Assurance Framework (compliance with "MUST"s);
  Information Trust Policies. QVI accreditation via the **vLEI Issuer Qualification Agreement + Program**.
- **Technical Requirements:** Part 1 **KERI Infrastructure** · Part 2 **vLEI Credentials** · Part 3 **Credential
  Schema Registry**. (ACDC/CESR/IPEX are detailed in the white paper §3.3, not this EGF page.)

## Mapping (the comparison crux)

| | UDAP | vLEI |
|---|---|---|
| Client assertion | `private_key_jwt` (x5c) | CESR/IPEX Grant (ACDC chain) |
| Identity anchor | X.509 SAN URI + community CA | AID → GLEIF Root |
| Authorized roles | (registration) | OOR / ECR |
| Purpose-of-use | `hl7-b2b.purpose_of_use` | **ECR** credential |
| Registration trust | software statement + cert chain | QVI-issued credential chain |
| Governance | Trust Community | vLEI EGF |

## Sources

- HL7 FHIR UDAP Security IG: <https://hl7.org/fhir/us/udap-security/> (`b2b.html`, `registration.html`).
- GLEIF vLEI EGF: <https://www.gleif.org/en/vlei/introducing-the-vlei-ecosystem-governance-framework>.
- SSRAA (TEFCA Facilitated FHIR) — via the RCE / The Sequoia Project (URL TBD).
