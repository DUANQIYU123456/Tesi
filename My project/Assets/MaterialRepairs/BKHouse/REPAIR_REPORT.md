# BK Alchemist House — scoped URP material repair

15 November 2026. Project: `D:/NABA/Tesi/My project`.

The project was already using URP. The newly imported BK_AlchemistHouse pack had 142 Standard materials, three Built-in GrabPass refraction materials, two legacy particle materials, and one already compatible URP material. No pipeline, renderer, Quality, scene lighting, post-processing, or baking settings were migrated or changed in this repair.

## Completed material repair

- 142 Standard materials converted with Unity's explicit 3D `StandardUpgrader`, targeting `Universal Render Pipeline/Lit`.
- Source base maps/colors, UV tiling/offsets, normals, metallic/smoothness maps, emission maps and transparency/cutout intent were snapshotted before conversion. Saved material reload checks confirmed the 142 base maps/colors and corresponding map references; 17 cutout and nine transparent modes were retained.
- Two legacy particle materials converted to URP Particles/Unlit with additive flame versus alpha-blended smoke behavior, texture/tint preservation and soft-particle parameters.
- Three Custom/Refraction materials mapped to `Assets/MaterialRepairs/BKHouse/BKRefractionURP.shader`. The original package shader is preserved. The replacement uses the project's already-enabled URP opaque texture with the original albedo, normal, opacity, smoothness, metalness, refraction and chromatic parameters. URP opaque-color refraction does not capture other transparent objects exactly as the old GrabPass did; exact Built-in visual parity is not claimed.
- The already-URP material was left alone.
- Three bottle prefab slots referenced Unity's uneditable Built-in Default-Material. Those slots now reference a separate URP default material, without changing Unity's global built-in material.
- Three empty optional particle trail material slots were confirmed unused (`trails.enabled == false`), rather than treated as missing active materials.

## Validation

`validation.json` passed: 157 prefabs scanned, zero missing active material slots, zero unsupported/Built-in active shader slots, no shader compile messages. Representative isolated URP renders were inspected for the door, interior framing, wooden floor, chair, cabinet and glass specimen jar. These are preview renders, not a lighting rebake or a full playtest of the pack's demo scene.

Final Console ground truth reported zero current errors and no C# compilation failure. The user's active ROOM1 scene already had unsaved edits; this repair did not save or discard those edits.

## Rollback

Before conversion, all 148 original material files and metadata were copied to `D:/NABA/Tesi/My project/Library/SurveyUIBackups/BKHouse_URP_20261115_085246`, preserving relative paths. That folder also contains the pre-conversion snapshot and the three original bottle prefabs/metadata. The pack's original shaders were not edited.

`conversion.json` records all material mappings. `validation.json` records saved-state checks. PNG files alongside this report show representative restored assets.
