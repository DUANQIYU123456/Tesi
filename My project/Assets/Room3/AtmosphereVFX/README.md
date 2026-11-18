# ROOM3 archive refraction — 2026-11-18

Uses the user's existing Vefects Distortion VFX URP. Original vendor source remains untouched. The dedicated ROOM3 renderer includes VefectsDistortionFeature alongside the existing volumetric-light feature. Idle saved state is inactive; Room3ArchiveDistortion enables it only while there is refraction.

Two memory patches track fragments. E retention uses sin(progress*pi)*.012 and a .7-second success echo. Four archive patches use a restrained ERASE preview and .58-second commit pulse, max .026. PRESERVE does not trigger erasure. Reduced motion disables refraction; reading/pause suppress it. A cold afterimage keeps minimal archival contours visible after the gold light recedes. UI stays in the original English overlay.

Important: use the _AT / Transparent material variant; normal Flat_01 lacks the late pass. The saved custom patch supplies UV0.z and UV1.y required by Vefects' particle shader. Ordinary Unity Quad streams are insufficient. The main camera explicitly requests depth/color. OnDisable clears the feature and late global flag.

Validation and scope: D:/CODEX/2026-11-17/new-chat-2/outputs/ROOM2浮尘与ROOM3扭曲/. PC Editor, MSAA=1, not a performance or independent build certification. Backups include both scenes and ROOM3 renderer. Do not blindly rerun installer records over the current scenes.
