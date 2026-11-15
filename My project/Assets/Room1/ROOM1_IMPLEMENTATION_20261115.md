# ROOM1 — Radium Frenzy prologue

Updated 15 November 2026. Unity project: `D:/NABA/Tesi/My project`.

## Implemented route

1. Begin Inspection starts in the town with the portal hidden. Its collider and radar eligibility are disabled too.
2. Hold RMB to see the fluorescent footprint trail and powder deposits. Release RMB to hide them. The existing Bloodlines-derived survey frame gets a restrained green tint; Geiger clicks respond while surveying.
3. Follow the trail to the bench beside the red telephone booths. An open brass compact, camel-hair brush, luminous alarm clock and wooden toy occupy the bench. A luminous keyhole and switch add small domestic traces nearby.
4. Face the compact and press E. The initial commercial layer conceals the reconstructed diary. Hold F for two seconds to focus/reveal it with the existing lip-focus sound. A partial hold does not award XII. Reading locks world input and restores it on close.
5. Retaining XII changes identity/integrity and lights XII on the survey dial. The portal remains absent until the decoded diary is closed.
6. Closing the retained diary materializes a cyan-green vortex, gentle camera movement and a new footprint route around the bench. Reduced-motion mode removes the shake/swirl. Pausing freezes materialization.
7. Approach and press E at FORMATTA ANOMALIA. A green contamination warning precedes the transition to ROOM2. XII survives; the prison restores player input. Restart clears progress and hides the town portal again.

## Environment details

- Three added aged advertisements: Undark in the pub/window area, radium water on the upper brick band, Radior beauty on an end brick bay. The user's earlier torn Undark poster remains.
- Paper damage concentrates on edges so the advertising remains readable. Ads use depth offset and lie close to the facade. Final review relocated two initially misplaced posters that crossed the door/window surfaces.
- RMB replaces the Radior face with a fractured, green skeletal overlay. It does not replace the historical ad source image.
- A small exposed PCB aperture sits at the base of the wall with etched tracks, chip pins, fractures and a chipped stone perimeter. Its tracks glow more than the surrounding domestic paint.
- The brush/compact uses the shared inspection UI and typography; no second UI system was installed.

## Editable scene locations

Root: `ROOM1 - Radium Frenzy Prologue` in `Assets/Scenes/ROOM1.unity`.

| Object | World position / note |
| --- | --- |
| Telephone corner / slatted bench | (52.5, 0, -18.9) |
| Undark - window advertisement | (44.70, 2.8, 1.5) |
| Radior beauty - two layer advertisement | (57.55, 2.8, -27.748); 1.1 × 1.42 m |
| Radium water - side wall advertisement | (54.25, 4.55, -27.748); 1.4 × 1.08 m |
| Digital decay / exposed circuit beneath brick | x55.7, y0.48; z seated against the actual facade mesh |
| Potal / Isotope rift | (55.1, 1.35, -24.3); visual 2.8 × 2.65 m |

Core scripts: `Assets/Room1/Runtime/TownPrologue.cs`, shared `SurveyHUD.cs`, `SurveyNode.cs`, `NarrativeInteractor.cs`. Shaders: `Assets/Room1/Shaders/TownPoster.shader`, `TownRift.shader`. `TownPrologueBuilder` is the guarded initial construction tool; the saved scene includes subsequent art refinements. Do not rebuild over the finished scene.

## Verification and limits

Automated Editor play checks use real Input System E/F/RMB events, the existing first-person controller and physical walking. No jumping or teleporting is used to bypass the town route. Results and gameplay captures are stored beside this document:

- `Final/town-validation.json`: normal route, 38 assertions.
- `FinalAccessible/town-validation.json`: large text plus reduced motion, 40 assertions.
- Checked hidden colliders, both poster/footprint visibility states, interaction facing/obstruction, incomplete F hold, diary/source text overflow, delayed portal activation, pause, route clearance, ROOM2 transfer, one player/listener, input restoration and restart.
- The ordinary route was rerun after the final poster/circuit art revision. Accessibility checks preceded that visual-only relocation; accessibility behavior was unchanged.
- During review an unfocused Game view initially prevented simulated RMB input; focusing the view resolved it. A separate genuine portal framing issue was corrected by lowering the vortex.
- Visual review also corrected the mirror hinge, paper damage that removed too much advertising, and floating/misplaced wall decorations.
- These are Editor checks at a 1280×720 review size, not a claim of measured performance on every machine. The older standalone build has not been rebuilt for this revision.

## Historical material and reconstruction

The English first-person diary and commercial UI text are **fictional reconstructions**, identified as such in the reader. They are not archival quotations.

- Library of Congress, “Radium Girls: Living Dead Women” (2019): https://blogs.loc.gov/headlinesandheroes/2019/03/radium-girls-living-dead-women/
- Library of Congress teaching source on radium girls: https://blogs.loc.gov/teachers/2019/10/primary-sources-in-science-classrooms-asking-science-and-social-studies-questions-about-radium-girls/
- HAER NJ-121 is the contextual historical record cited in the reader. Existing workshop imagery is drawn from the project's research collection.
- Radior beauty: historical New York Tribune advertisement, 10 November 1918, graphic section p.12. The imported reproduction was extracted from the appendix of this university-hosted student research PDF: https://clas.ucdenver.edu/nhdc/sites/default/files/attached-files/entry_458.pdf . That appendix is a reproduction route, not independent historical authority. A 1918 ad was chosen rather than presenting the later Tho-Radia campaign as a 1920s object.
- Undark and radium-water images already supplied in the Unity project were preserved.
- The thesis Fig.2.7 is a negative/green face treatment, not a jaw radiograph. It is used as a subtle ghost layer; the skeletal reveal is explicitly an artistic effect.

## GitHub references reviewed

These informed implementation choices. No third-party source code or packages were copied into this revision.

- Fewes / MeshDecal — mesh/depth-offset decal approach: https://github.com/Fewes/MeshDecal
- DuckBoss / InspectSystem-Unity — inspection and post-inspection events: https://github.com/DuckBoss/InspectSystem-Unity
- llamacademy / urp-decal-projector — URP decal reference: https://github.com/llamacademy/urp-decal-projector

The current shared inspection/HUD architecture was extended directly to preserve the existing three-scene flow.

## Generated image disclosure

Tool: built-in `image_gen`, transparent background; no fallback CLI. This is a fictional visual effect, not a clinical image or historical document.

Final project asset: `D:/NABA/Tesi/My project/Assets/Room1/Images/SkeletalReveal_Reconstruction.png`.

Original generated output: `C:/Users/一只小段/.codex/generated_images/01a135de-07d1-7691-8c34-1b85e11c2079/exec-d0bc2471-d4ea-49be-8de3-c1b3100869f3.png`.

Final prompt:

> Game texture asset, isolated frontal human skull X-ray silhouette for a Unity ghostly radiation overlay. Genuine transparent background. Full cranium and upper jaw, lower mandible fractured into three subtly displaced separate fragments, no flesh, no blood, no medical photograph frame. Thin intricate semi-transparent ivory-green trabecular bone structure, deep transparent eye sockets and nose aperture, spectral emerald edge light and delicate green phosphorescence. Fine photographic radiograph detail, elegant ominous restraint, no cartoon contours or flat solid neon shapes. Face aligned perfectly front-facing, centered, vertical composition, entire skull within image with transparent padding around all sides. No text, labels, radiation symbol, watermark or background.

## Try it

Open ROOM1, Play → Begin Inspection → hold RMB and follow prints → E at compact beside telephone booths → hold F → close reader → follow the new trail around the bench → E at FORMATTA ANOMALIA → ROOM2.
