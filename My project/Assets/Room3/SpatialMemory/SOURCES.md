# Spatial memory source record

Reviewed 2026-11-12 for Unity 6000.2.12f1 and URP 17.2.0.

## Adapted source

Zbajnek, **URP Portals**, MIT license.
Repository: https://github.com/Zbajnek/urp-portals
Pinned revision: `13dcdb21d972d5f1f6df4a9506d198524bf044c2`.

- `Packages/URPPortals/Shaders/PortalMask.shader`: projected screen sampling, adapted as `Shaders/MemoryPortalMask.shader`, with a project-specific shader name and render queue.
- `Packages/URPPortals/Runtime/Scripts/Portals/Portal.cs`: portal-space mapping, oblique near-plane and screen-thickness techniques informed `ResidualPortal` and the portal-view methods in `Room3RecursiveArchive`. The project uses an explicit 180-degree mapping between opposing thresholds.
- The upstream project credits Sebastian Lague's Portals. This implementation was adapted from the MIT-licensed Zbajnek revision above, not by importing the original Sebastian Lague project.
- License text is retained at `ThirdParty/LICENSE.Zbajnek.txt`.

Changes for this project: a Starter Assets / Cinemachine traversal adapter; current URP SingleCameraRequest rendering instead of Camera.Render; a two-level view limit, four-view frame budget and 30 Hz texture refresh; owned texture/material cleanup; pause and ending handling; and the labor-record loop progression. No upstream project settings, package manifest or demo scene were imported.

Unity reference for render requests:
https://docs.unity3d.com/6000.0/Documentation/Manual/urp/User-Render-Requests.html

## Evaluated references

- sebheron / Infinite-Room-Unity, revision `79dab68291da85b670e49cef05146c380eb24847`, MIT: https://github.com/sebheron/Infinite-Room-Unity . Considered for spatial repetition; no files copied.
- daniel-ilett / dissolve-urp, MIT: https://github.com/daniel-ilett/dissolve-urp . The README targets Unity 2019.3 / URP 7.1.8; no old Shader Graph or project settings imported. The mineral skin shader in this project is a new implementation.
- barnabasbartha / Unity-URP-Portal: inspected; not imported.

## Narrative research

The new labor ledger is a research summary and a fictional system interface, not a historical document or worker quotation.

- Gray, Mary L., and Siddharth Suri. *Ghost Work*. 2019. Author/institutional summary: https://www.microsoft.com/en-us/research/articles/ghost-work-how-to-stop-silicon-valley-from-building-a-new-global-underclass/
- Crawford, Kate. *Atlas of AI*. Yale University Press, 2021. Publisher: https://yalebooks.yale.edu/book/9780300264630/atlas-of-ai/
- Historic American Engineering Record, U.S. Radium Corporation, NJ-121; see the existing `Assets/Room3/STORY_SOURCES.md` for the historical sources.

The comparison concerns visibility, repetition, time and labor. It does not identify digital labor with radium exposure or claim an identical illness. The repeated corridor, eroding skin and green circuits are artistic constructions.
