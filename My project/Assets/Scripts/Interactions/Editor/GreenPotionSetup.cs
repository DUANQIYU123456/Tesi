#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoftKitty.LiquidContainer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>Configures only the LiquidControl bottles in the currently open ROOM2.</summary>
[InitializeOnLoad]
public static class GreenPotionSetup
{
    const string Version = "green-potions-20261014-v1";
    const string Folder = "Assets/Materials/GreenPotions";
    const string ReportPath = "Library/XiaoduanPotionSetup.json";
    const string MarkerPath = "Library/XiaoduanPotionSetup.completed";
    static double nextAttempt;
    static bool attempting;

    [Serializable] public sealed class BottleReport
    {
        public string name;
        public Vector3 position;
        public float waterLine;
        public string liquidMaterial;
        public string[] shaders;
        public bool liquidShaderSupported;
    }
    [Serializable] public sealed class Report
    {
        public string status;
        public string message;
        public string backup;
        public string scene;
        public BottleReport[] bottles;
        public string[] shaderErrors;
    }

    static GreenPotionSetup()
    {
        EditorApplication.update += TryAutomaticSetup;
    }

    static void TryAutomaticSetup()
    {
        if (attempting || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextAttempt) return;
        nextAttempt = EditorApplication.timeSinceStartup + 3;
        if (File.Exists(MarkerPath) && File.ReadAllText(MarkerPath) == Version)
        {
            EditorApplication.update -= TryAutomaticSetup;
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.name != "ROOM2") return;
        var bottles = GetBottles(scene);
        if (bottles.Length == 0) return; // The user's newly placed objects may not be imported yet.
        attempting = true;
        try { Setup(); }
        catch (Exception exception)
        {
            WriteReport(new Report { status = "error", message = exception.ToString(), scene = scene.path });
            Debug.LogException(exception);
        }
        finally { EditorApplication.update -= TryAutomaticSetup; attempting = false; }
    }

    static LiquidControl[] GetBottles(Scene scene) => scene.GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<LiquidControl>(true))
        .Where(liquid => liquid.LiquidMeshFilter != null).ToArray();

    [MenuItem("Tools/Xiaoduan/Setup Green Potions in ROOM2")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "ROOM2")
            throw new InvalidOperationException("Open ROOM2 in Edit mode before configuring the potions.");
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
            throw new InvalidOperationException("This setup requires the project's existing URP pipeline.");
        Shader liquidShader = Shader.Find("Xiaoduan/Fluorescent Liquid URP");
        Shader glassShader = Shader.Find("Universal Render Pipeline/Lit");
        if (liquidShader == null || glassShader == null)
            throw new InvalidOperationException("Wait for the liquid shader to finish importing.");
        string[] errors = ShaderUtil.GetShaderMessages(liquidShader)
            .Where(message => message.severity.ToString() == "Error").Select(message => message.message).ToArray();
        if (errors.Length > 0) throw new InvalidOperationException(string.Join("\n", errors));
        LiquidControl[] bottles = GetBottles(scene);
        if (bottles.Length == 0) throw new InvalidOperationException("No liquid bottles found in ROOM2.");

        // Preserve the currently open scene, including the user's unsaved bottle placement.
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Unable to save ROOM2.");
        string backup = "Library/PotionBackups/ROOM2_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        File.Copy(scene.path, backup, false);
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure fluorescent green potions");
        var reports = new List<BottleReport>();

        foreach (LiquidControl liquid in bottles)
        {
            Renderer water = liquid.LiquidMeshFilter.GetComponent<Renderer>();
            if (water == null) continue;
            Undo.RecordObject(liquid, "Keep the liquid inside the bottle");
            liquid.Opened = false; // These are display bottles; no unsupported pouring effects.
            liquid.ShowOpenningHelper = false;
            float height = liquid.CalSize();
            Material liquidMaterial = new Material(liquidShader) { name = liquid.name + "_GreenLiquid" };
            liquidMaterial.SetFloat("_Size", height);
            liquidMaterial.SetFloat("_WaterLine", liquid.WaterLine);
            liquidMaterial.SetColor("_TopColor", new Color(0.18f, 0.6f, 0.025f));
            liquidMaterial.SetColor("_BottomColor", new Color(0.015f, 0.15f, 0.004f));
            liquidMaterial.SetColor("_GlowColor", new Color(0.12f, 1f, 0.025f));
            liquidMaterial.SetFloat("_EmissionStrength", 0.08f);
            string waterPath = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + liquid.name + "_Liquid.mat");
            AssetDatabase.CreateAsset(liquidMaterial, waterPath);
            Undo.RecordObject(water, "Assign URP green liquid");
            water.sharedMaterial = liquidMaterial;
            water.shadowCastingMode = ShadowCastingMode.Off;

            if (liquid.LiquidMaskMesh != null)
            {
                Renderer mask = liquid.LiquidMaskMesh.GetComponent<Renderer>();
                if (mask != null)
                {
                    Undo.RecordObject(mask, "Disable legacy stencil mask");
                    mask.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(mask);
                }
            }
            foreach (ParticleSystemRenderer particle in liquid.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Undo.RecordObject(particle, "Disable legacy particle rendering");
                particle.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(particle);
            }
            // New material assets protect the imported vendor materials and other scenes.
            foreach (MeshRenderer renderer in liquid.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == water || !renderer.enabled) continue;
                bool glass = renderer.gameObject == liquid.gameObject ||
                    renderer.name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null) continue;
                    if (!glass && source.shader != null && source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                    Material converted = Convert(source, glassShader, glass);
                    AssetDatabase.CreateAsset(converted, AssetDatabase.GenerateUniqueAssetPath(
                        Folder + "/" + liquid.name + (glass ? "_Glass" : "_Detail") + ".mat"));
                    materials[i] = converted;
                    changed = true;
                }
                if (changed)
                {
                    Undo.RecordObject(renderer, "Assign compatible bottle material");
                    renderer.sharedMaterials = materials;
                    renderer.shadowCastingMode = glass ? ShadowCastingMode.Off : renderer.shadowCastingMode;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
            }
            ProximityPotionGlow glow = liquid.GetComponent<ProximityPotionGlow>();
            if (glow == null) glow = Undo.AddComponent<ProximityPotionGlow>(liquid.gameObject);
            Undo.RecordObject(glow, "Configure proximity glow");
            glow.liquidRenderer = water;
            if (glow.glowLight == null)
            {
                var lightObject = new GameObject("Potion Green Light");
                Undo.RegisterCreatedObjectUndo(lightObject, "Create potion light");
                lightObject.transform.SetParent(liquid.transform, false);
                glow.glowLight = Undo.AddComponent<Light>(lightObject);
                glow.glowLight.transform.position = water.bounds.center;
            }
            glow.glowLight.type = LightType.Point;
            glow.glowLight.color = glow.glowColor;
            glow.glowLight.range = glow.lightRange;
            glow.glowLight.intensity = 0f;
            glow.glowLight.shadows = LightShadows.None;
            glow.glowLight.lightmapBakeType = LightmapBakeType.Realtime;
            glow.glowLight.enabled = false;
            foreach (UnityEngine.Object obj in new UnityEngine.Object[] { liquid, water, glow, glow.glowLight })
            {
                EditorUtility.SetDirty(obj);
                PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
            }
            reports.Add(new BottleReport { name = liquid.name, position = liquid.transform.position,
                waterLine = liquid.WaterLine, liquidMaterial = waterPath,
                liquidShaderSupported = liquidShader.isSupported,
                shaders = liquid.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled)
                    .SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => m.shader.name).ToArray() });
        }
        ConfigureBloom(scene);
        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Potion setup couldn't save ROOM2.");
        errors = ShaderUtil.GetShaderMessages(liquidShader).Where(m => m.severity.ToString() == "Error")
            .Select(m => m.message).ToArray();
        WriteReport(new Report { status = errors.Length == 0 ? "configured" : "shader-errors",
            message = "Saved URP liquid, glass, distance glow, point lights and Bloom.",
            scene = scene.path, backup = backup, bottles = reports.ToArray(), shaderErrors = errors });
        File.WriteAllText(MarkerPath, Version);
        Debug.Log("[GreenPotions] Configured and saved " + reports.Count + " bottles in ROOM2. Backup: " + backup);
        SceneView.RepaintAll();
    }

    static Material Convert(Material source, Shader shader, bool glass)
    {
        Texture map = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : null;
        if (map == null && source.HasProperty("_BaseMap")) map = source.GetTexture("_BaseMap");
        Color color = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        Texture normal = source.HasProperty("_Normal") ? source.GetTexture("_Normal") : null;
        if (normal == null && source.HasProperty("_BumpMap")) normal = source.GetTexture("_BumpMap");
        Texture ao = source.HasProperty("_AO") ? source.GetTexture("_AO") : null;
        Material result = new Material(shader) { name = source.name + "_PotionURP" };
        result.SetColor("_BaseColor", glass ? new Color(0.65f, 0.85f, 0.75f, 0.13f) : color);
        result.SetFloat("_Smoothness", glass ? 0.95f : 0.4f);
        if (!glass && map != null)
        {
            result.SetTexture("_BaseMap", map);
            string prop = source.HasProperty("_MainTex") && source.GetTexture("_MainTex") != null ? "_MainTex" : "_BaseMap";
            result.SetTextureScale("_BaseMap", source.GetTextureScale(prop));
            result.SetTextureOffset("_BaseMap", source.GetTextureOffset(prop));
        }
        if (normal != null) { result.SetTexture("_BumpMap", normal); result.EnableKeyword("_NORMALMAP"); }
        if (ao != null) result.SetTexture("_OcclusionMap", ao);
        if (glass)
        {
            result.SetFloat("_Surface", 1f);
            result.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            result.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            result.SetFloat("_ZWrite", 0f);
            result.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            result.SetOverrideTag("RenderType", "Transparent");
            result.renderQueue = 3000;
            result.SetShaderPassEnabled("ShadowCaster", false);
        }
        return result;
    }

    static void ConfigureBloom(Scene scene)
    {
        var environment = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Potion Glow Environment");
        if (environment == null)
        {
            environment = new GameObject("Potion Glow Environment");
            SceneManager.MoveGameObjectToScene(environment, scene);
            Undo.RegisterCreatedObjectUndo(environment, "Create potion Bloom volume");
        }
        Volume volume = environment.GetComponent<Volume>();
        if (volume == null) volume = Undo.AddComponent<Volume>(environment);
        Undo.RecordObject(volume, "Configure potion Bloom");
        if (volume.sharedProfile == null)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(Folder + "/PotionBloom.asset"));
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.6f);
            AssetDatabase.AddObjectToAsset(bloom, profile);
            EditorUtility.SetDirty(profile);
            volume.sharedProfile = profile;
        }
        volume.isGlobal = true;
        volume.priority = 60f;
        volume.weight = 1f;
        if (environment.GetComponent<PotionGlowEnvironment>() == null)
            Undo.AddComponent<PotionGlowEnvironment>(environment);
        EditorUtility.SetDirty(volume);
        // The runtime helper also enables Bloom on the camera arriving from ROOM1.
    }

    static void WriteReport(Report report) => File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
}
#endif
