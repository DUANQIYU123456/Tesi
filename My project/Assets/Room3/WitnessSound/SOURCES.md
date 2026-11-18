# ROOM3 anatomical witnesses and composed soundscape

Added 17 November 2026. These assets improve the existing ROOM3 route; the 3D women are symbolic, unidentified reconstructions. They are not portraits or documentary reenactments of the five named plaintiffs. Existing thesis portraits, newspaper clippings, chronology and provenance remain separate.

## Character data: CC0-1.0

Source: [kblood/CharacterCreator](https://github.com/kblood/CharacterCreator), revision `852e48f316b90a0937234886630e4fb4cdfd1f78`.

Only the CC0 output **data** were used: `base_body.glb`, `clothing_dress.glb`, `clothing_shoes.glb`, `hair_bob01.glb`, `hair_bob02.glb`, `hair_short02.glb`, plus their CC0 embedded textures. Their provenance is documented in the repository's [LICENSE-NOTES.md](https://github.com/kblood/CharacterCreator/blob/852e48f316b90a0937234886630e4fb4cdfd1f78/LICENSE-NOTES.md) and `output/asset_licenses.json`. The generator program is GPL; its code is not copied, linked or imported into this project.

The project's independently authored data converter reads the GLB vertex/bone data, applies the female morphology and modest age/weight variation, converts coordinates to Unity, lengthens the skirt, and writes native Mesh assets. Native Unity SkinnedMeshRenderer components use the existing 53-bone data. Poses, restrained breathing, witness arrangement, work aprons and effort-dependent arm movement are project adaptations. The initial downloaded shirt was evaluated but is not included in the produced meshes.

## Recorded sound sources

- **Metal impact and heavy plate:** Kenney, [Impact Sounds](https://kenney.nl/assets/impact-sounds), CC0-1.0. The public source download was unavailable; two original unmodified OGG assets were obtained from the [commander-in-chief mirror](https://github.com/shoemoney/commander-in-chief/tree/main/assets/cc0_extra/sfx_impact): `impactMetal_medium_000.ogg` and `impactPlate_heavy_000.ogg`. Their author and license are Kenney's, not the mirror's program license. Used in the forced gear, latch and concrete impact layers.
- **Pipe organ:** Versilian Studios / Sam Gossner, [VCSL Pipe Organ](https://github.com/sgossner/VCSL/tree/master/Aerophones/Edge-blown%20Aerophones/Pipe%20Organ), CC0-1.0; `Rode_Man3Open_C2.wav`, `C3.wav`, `A3.wav`. [VCSL license](https://github.com/sgossner/VCSL/blob/master/LICENSE) retained. Sustain splices, transposition and a new layered chord were authored for testimony success.
- **Female cough:** Joseph SARDIN, [Woman cough, sound 0654](https://bigsoundbank.com/woman-cough-s0654.html), CC0. The same author's public `https://lasonotheque.org/UPLOAD/mp3/0654.mp3` was used after the main host could not connect. This is a contemporary studio recording used as interpretive sound design, not archival evidence of a named worker.
- **Airy female choir:** tsaworks, [female choir ambience, Freesound 532292](https://freesound.org/people/tsaworks/sounds/532292/), CC0-1.0. Uses the publicly available HQ MP3 preview. The author explicitly identifies this as a digital creation, not a recorded choir. A 24-second portion was edited for the post-oath layer; it is not a historical recording or a live singer performance.

Source-file SHA-256 hashes are listed in `SOURCES-SHA256.json`. Original legal notes and the VCSL license are retained alongside this document.

## Project-authored audio

The muffled pressure siren, pressure leakage, cold wind, sub drone, submerged pulse, Geiger grains, clock mechanism, cooling fan, irregular mouse clicks, resistance tone, heart/bone pressure, digital wipe, warm major pad and gavel are authored sound-design layers. Impact layers also include the recorded sources above. The resistance tone is a pitch-controlled layered riser; it is not claimed to be an endless Shepard-tone implementation. No claim is made that these sounds are historical recordings.

Twenty-one rendered WAV clips use 32 kHz mono PCM input, are normalized below full scale and have short edge fades. The organ/choir enter only after testimony. A native AudioReverbZone starts from Cave and stores a User preset with 4.2-second decay; 2D score layers use their baked spatial tails, while spatial gear/stone sources use the zone. Reverb does not turn ordinary 2D audio into positional sound.

The original archive route, collision gates, history panels, accessibility input and two ending consequences remain owned by the existing story flow. No new global Audio Manager, audio package, paid dependency or external service is required.
