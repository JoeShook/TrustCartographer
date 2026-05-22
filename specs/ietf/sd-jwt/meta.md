# Selective Disclosure for JWTs (SD-JWT)

- **Status:** IETF (verify in fetched HTML — recent draft or RFC)
- **Canonical URL:** https://datatracker.ietf.org/doc/draft-ietf-oauth-selective-disclosure-jwt/
- **Fetched:** 2026-05-21
- **Local file:** `sd-jwt.html`

## Why we care
The mechanism that lets a Holder reveal *some* claims from a credential while hiding others. **This is one of SSI's largest qualitative differences from X.509** — you cannot selectively disclose fields of a certificate, but you can with an SD-JWT.

## Key concepts
- **Disclosures** — Per-claim salted hash commitments; the JWT contains only hashes
- **Issuer-signed JWT** + **Disclosure list** + **optional Key Binding JWT** = the SD-JWT envelope
- **Holder Binding** — Optional proof-of-possession to bind the disclosure to a holder key
- **Decoy digests** — Pad the digest list so the Verifier can't infer how many claims exist
- Used as a building block in SD-JWT-VC (see neighbor folder) and OID4VCI

## Relevance to TrustCartographer
This is the spec to point at when explaining why SSI's privacy properties exceed PKI's. Direct comparison: an X.509 cert's subject DN is fixed; an SD-JWT's revealed claims are negotiated at presentation time.
