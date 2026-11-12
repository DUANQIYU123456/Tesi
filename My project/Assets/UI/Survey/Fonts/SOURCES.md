# IBM Plex fonts

Upstream: https://github.com/IBM/plex

Pinned commit: `48e8676114c04f29033b4b53da4ec579f8846297`

Downloaded 2026-11-12 from IBM's repository:

- `packages/plex-sans/fonts/complete/ttf/IBMPlexSans-Regular.ttf`
- `packages/plex-sans/fonts/complete/ttf/IBMPlexSans-SemiBold.ttf`
- `packages/plex-mono/fonts/complete/ttf/IBMPlexMono-Medium.ttf`
- `LICENSE.txt`, retained here as `OFL.txt`.

The original TTF files are unmodified. `font-source-lock.json` records byte counts, SHA-256 and Git blob hashes. The blob hashes match the upstream directory metadata.

The game uses separate Unity TextMeshPro static SDF assets generated from these files at sampling size 90, padding 9, 2048×2048 Alpha8, SDF16. The supported characters include English, Italian accented letters and the punctuation used in the interface. The Sans assets use the static Mono asset for redaction block glyphs.

Copyright © 2017 IBM Corp. with Reserved Font Name "Plex". See the complete SIL Open Font License 1.1 in `OFL.txt`.
