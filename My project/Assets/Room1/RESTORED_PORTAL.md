# ROOM1 original portal restored

At the user's request, the ROOM1 portal visual now uses the original `Assets/Prefabs/Potal.prefab` quad mesh, scale and shared `Assets/Scenes/Materials/Green portal.mat` material (`Shader Graphs/portal shader`). The replacement art-pass rift material is no longer assigned to the active visual.

The visual remains a child of the delayed-activation group, preserving the diary-close reveal and ROOM2 transition logic. The disabled original root renderer stays disabled to avoid drawing two overlapping portals. Shared Green portal material values were not changed, so this edit does not alter other scenes. Reduced-motion mode controls `_speed` through a per-renderer property block.

`town-validation.json` and the screenshots in this folder record the Editor route verification. Earlier art-pass descriptions of a fine broken rift rim are superseded by this restoration.
