# ROOM2 evidence presentation — 2026-11-18

This pass refines the existing ROOM2 rather than replacing its route or interaction framework.

- `Room2Presentation` directs the continuous scan-body audio and the released gate's cold spill. The previous opaque threshold-panel experiment was removed; `thresholdSurface` is intentionally null.
- `Evidence grade.asset` contains four persistent Volume sub-assets: Color Adjustments, Chromatic Aberration, Bloom, Depth of Field. Baseline saturation -8, exposure -.15, chromatic .012. Entry and scan transient volumes have higher priority. Add Volume components as sub-assets when editing this asset; transient in-memory components do not survive reloading.
- `ArchiveProjection.shader` preserves the source photograph with a silver-green tint, regular alpha and fine scan texture. It does not deform faces. Original reference images remain unchanged. Existing imported dissolve handles brief appearances/exits.
- Real image planes now coincide. The final image local scale is .5616 after composition refinement. Labels identify the source kind; the HUD owns the countdown.
- MRI continuous body is a 3-second derived loop from the existing `SpatialScan_MRI_Prototype`, with an overlapping .12-second seam. The controller shapes its envelope over the existing 14-second pass. Record latch and archive pressure release are project-generated synthetic sounds, not historical or clinical recordings.
- Floor, specimen, skull and gate materials are local clones. Source material assets were not edited. Four floor clones serve 195 floor MeshRenderers. Bottle ambient particles are disabled; the dedicated four dust pockets remain.
- The reconstructed tabletop document uses English world-space TMP printing. The clinical reference image is not asserted to depict Mollie Maggia.

Related scripts refined outside this folder:

- `SurveyHUD`: side-cell HUD priority, audible signal locking, optional-task wording, suppressed duplicate scan banners.
- `LaborEchoEmitter`: audible signal strength and partial scan ducking; side-cell focus restores the labor voice.
- `PanopticonHall`: capture hint states, restrained receipt, staggered exit sequence, source labels, dedicated three-record count. Original 14s + 2s cycle and 5s exposure retained.
- `ClinicalDesk`, `ClinicalDeskReader`, `ClinicalCoating`: single specimen lift, paper light, English live text, large text, partial scraping persistence, motion-sensitive scratch feedback.
- `PrisonHandprints`: softer cloned visor and local, distance/gaze-dependent opacity. ROOM1's shared vignette asset is unchanged.

Validation and native screenshots: `D:/CODEX/2026-11-17/new-chat-2/outputs/ROOM2整体精修/`.
Before-state backups: `D:/CODEX/2026-11-17/new-chat-2/work/Room2Polish/Backup/`.

The work scripts are installation/iteration records, not safe rerunnable installers over the final scene. To revert, restore the backed-up scene and modified scripts together. Do not apply older Panopticon/Clinical installers over this pass. ROOM2 intentionally receives its player from the original ROOM1 SceneController route.
