# docs/

Design notes, conceptual references, and captured Q&A for TrustCartographer and its relationship to Sigil.

These documents capture the *thinking* behind the project — paradigm choices, mappings between PKI and SSI, the phased plan for extending Sigil with DID/VC support. The actual specs that ground every decision here live in `../specs/`.

## Layout

```
docs/
├── README.md                            # this file
├── sigil-and-trustcartographer.md       # roles, paradigm, seams sketch
├── pki-vs-ssi.md                        # concept mapping, where SSI diverges
├── phased-roadmap.md                    # Phases A–F for adding DID/VC to Sigil
└── qa/
    └── openid-in-did-space.md           # what OpenID does in the DID space
```

## Suggested reading order

For a new contributor or future-you returning to the project:

1. **`sigil-and-trustcartographer.md`** — Understand the two-project setup: TrustCartographer is the visualizer, Sigil is the trust-material backend. The proposed seams for adding DID/VC paradigms.
2. **`pki-vs-ssi.md`** — The mental model and concept mapping. Where SSI genuinely diverges from PKI (don't flatten one into the other).
3. **`phased-roadmap.md`** — The actual build order for Sigil's DID/VC support, Phases A–F. Cross-references to `../specs/`.
4. **`qa/openid-in-did-space.md`** — Snippet-level Q&A. The `qa/` folder is for these as they come up.

## Conventions

- **Cross-links** use relative paths. `../specs/...` for spec references, `./other-file.md` (or `qa/file.md`) for sibling docs.
- **Mapping tables** are first-class. When introducing a new SSI concept, show its PKI/UDAP analog if one exists.
- **Distinguish opinion from spec.** When a doc reflects a design *decision we made*, say so. When it states what a spec *requires*, link to the spec.
- **Capture Q&A as it happens** — drop short single-topic snippets into `qa/` rather than letting them dissolve into chat.

## Extending

When adding new design content:

- **Topical design files** go in `docs/` directly. New file when the topic doesn't fit an existing one; otherwise extend an existing file.
- **Q&A snippets** go in `docs/qa/<short-slug>.md`. Date them in the body.
- **Spec material** does *not* go here — that belongs in `../specs/<area>/<spec>/`.
- **Project status / phase tracking** stays in `../PLAN.md`. This folder is for stable references, not work-in-progress state.

Update this README's layout section when adding new files.

## Related

- `../PLAN.md` — TrustCartographer's own phased plan (visualizer phases 1–5)
- `../specs/` — Curated specifications referenced throughout these docs
- Sigil source: `C:\Source\GitHub\JoeShook\udap-tools\udap-dotnet\examples\CA`
