# Bitstring Status List v1.0

- **Status:** W3C Recommendation
- **Canonical URL:** https://www.w3.org/TR/vc-bitstring-status-list/
- **Fetched:** 2026-05-21
- **Local file:** `vc-bitstring-status-list.html`

## Why we care
The current standard for VC revocation/suspension. Replaces the older StatusList2021. **Direct conceptual analog to a CRL** — but as a bitstring inside a published VC, indexed by position. Sigil's `StatusListService` (proposed) implements this.

## Key concepts to map into Sigil
- **Status List Credential** — A VC published by the issuer (or a status authority) containing a compressed bitstring
- **statusListIndex** — Position in the bitstring assigned to each issued credential at issuance time
- **statusPurpose** — `revocation` or `suspension` (multiple lists per issuer for different purposes)
- **statusSize** — Bits per entry (1 for boolean revoke, more for multi-state)
- **Publishing** — Issuer hosts the status list at a stable URL (Sigil already does this pattern for CRLs)

## Issuance flow in Sigil
1. Pick next available `statusListIndex` from the active status list
2. Embed `credentialStatus` in the VC pointing to the list URL + index
3. To revoke: flip the bit, re-sign the status list, re-publish
