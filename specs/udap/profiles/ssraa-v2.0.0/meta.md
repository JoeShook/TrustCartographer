# SSRAA v2.0.0 (UDAP Security IG)

- **Status:** SSRAA v2.0.0 (Specifying Standards for Robust Application Authentication)
- **Profile of:** UDAP Core (see `../../core/`)
- **Source:** Copied from `udap-tools/udap-dotnet/docs/specifications/SSRAA/V2.0.0` on 2026-05-21

## Contents

| File | Notes |
|------|-------|
| `udap-security-v2.0.0-ai-friendly.md` | Markdown-rendered version of the IG, structured for easy reading / search |
| `ssraa_full-ig_v2.0.0.zip` | Original full IG (HL7 FHIR Implementation Guide bundle, 15MB). Unzip on demand only — too large to leave expanded in the repo |

## Why this matters
SSRAA is the **healthcare-vertical profile of UDAP** that nails down the specific constraints, certificate templates, and metadata requirements for FHIR-based exchange. When demonstrating "UDAP in production," SSRAA is the concrete profile to point at.

## Working with the zip
The full IG is a standard HL7 FHIR IG publication (HTML, JSON resources, profiles). Extract on demand:

```bash
unzip -d ssraa-v2.0.0-extracted ssraa_full-ig_v2.0.0.zip
```

The AI-friendly markdown file is sufficient for spec questions during development; reach for the zip when you need to inspect actual StructureDefinitions / ValueSets / examples.
