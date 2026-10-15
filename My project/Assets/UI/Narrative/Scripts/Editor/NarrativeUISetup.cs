#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Xiaoduan.Narrative;

[InitializeOnLoad]
public static class NarrativeUISetup
{
    const string Version="narrative-ui-english-v2";
    const string Marker="Library/XiaoduanNarrativeUI.completed";
    const string Folder="Assets/UI/Narrative";
    const string Bridge="Assets/Packages/com.unity.ugui";
    const string BridgeMarker="Library/XiaoduanTMPBridge";
    static bool running;
    static double nextCheck;
    [Serializable] sealed class SceneReport
    {
        public string scene,backup;
        public int portals,inspectableBottles,canvases,eventSystems;
    }
    [Serializable] sealed class Report
    {
        public string status,message,prefab,theme,font;
        public SceneReport[] scenes;
    }
    static NarrativeUISetup() => EditorApplication.update+=WhenReady;
    static void WhenReady()
    {
        if (running || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup<nextCheck) return;
        nextCheck=EditorApplication.timeSinceStartup+3;
        if (File.Exists(Marker) && File.ReadAllText(Marker)==Version)
        { EditorApplication.update-=WhenReady; return; }
        if (PrefabStageUtility.GetCurrentPrefabStage()!=null) return;
        try
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")==null)
            { EnsureEssentials(); return; }
            CleanBridge();
            running=true;
            Configure();
            EditorApplication.update-=WhenReady;
        }
        catch (Exception error)
        {
            File.WriteAllText("Library/XiaoduanNarrativeUI.json",JsonUtility.ToJson(new Report {status="error",message=error.ToString()},true));
            Debug.LogException(error);
            EditorApplication.update-=WhenReady;
        }
        finally { running=false; }
    }
    static void EnsureEssentials()
    {
        if (SessionState.GetBool("XiaoduanTMPRequested",false)) return;
        SessionState.SetBool("XiaoduanTMPRequested",true);
        // The bundled TMP importer searches embedded paths. Bridge only its resource file
        // when the package is in PackageCache; remove the owned bridge after import.
        if (!Directory.Exists("Packages/com.unity.ugui") && !Directory.Exists(Bridge))
        {
            string package=UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
            Directory.CreateDirectory(Bridge+"/Editor Resources");
            Directory.CreateDirectory(Bridge+"/Package Resources");
            File.Copy(package+"/Package Resources/TMP Essential Resources.unitypackage",
                Bridge+"/Package Resources/TMP Essential Resources.unitypackage",false);
            File.WriteAllText(BridgeMarker,Bridge);
        }
        TMP_PackageResourceImporter.ImportResources(true,false,false);
        Debug.Log("[NarrativeUI] Importing bundled TMP essentials without an interactive dialog.");
    }
    static void CleanBridge()
    {
        if (!File.Exists(BridgeMarker) || File.ReadAllText(BridgeMarker)!=Bridge) return;
        AssetDatabase.DeleteAsset(Bridge);
        File.Delete(BridgeMarker);
    }
    [MenuItem("Tools/Xiaoduan/Setup Narrative Interaction UI")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font==null) throw new InvalidOperationException("Wait for the bundled font resources.");
        Directory.CreateDirectory(Folder+"/Entries");
        AssetDatabase.Refresh();
        var theme=AssetDatabase.LoadAssetAtPath<NarrativeTheme>(Folder+"/NarrativeTheme.asset");
        if (theme==null)
        {
            theme=ScriptableObject.CreateInstance<NarrativeTheme>(); theme.font=font;
            AssetDatabase.CreateAsset(theme,Folder+"/NarrativeTheme.asset");
        }
        EditorUtility.SetDirty(theme);
        string prefabPath=Folder+"/NarrativeInterface.prefab";
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab==null)
        {
            var root=BuildView(theme);
            prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }
        var contents=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            contents.GetComponent<NarrativeHUD>().ApplyTheme();
            PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        var reports=new System.Collections.Generic.List<SceneReport>();
        foreach (string path in new[]{"Assets/Scenes/ROOM1.unity","Assets/Scenes/ROOM2.unity"})
        {
            Scene scene=SceneManager.GetSceneByPath(path);
            bool opened=!scene.isLoaded;
            if (opened) scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                if (all.Count(t=>t.GetComponent<NarrativeInteractor>()!=null)>1) throw new InvalidOperationException("Duplicate narrative interfaces.");
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save scene before UI setup.");
                string backup="Library/NarrativeUIBackups/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/"+scene.name+".unity";
                Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path,backup,false);
                Undo.IncrementCurrentGroup(); int group=Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Add narrative interface to "+scene.name);
                if (!all.Any(t=>t.GetComponent<NarrativeInteractor>()!=null))
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                    Undo.RegisterCreatedObjectUndo(go,"Add reusable narrative interface");
                }
                var systems=all.Select(t=>t.GetComponent<EventSystem>()).Where(e=>e!=null).ToArray();
                if (systems.Length>1) throw new InvalidOperationException("More than one EventSystem in "+scene.name);
                if (systems.Length==0)
                {
                    var events=new GameObject("Narrative EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                    SceneManager.MoveGameObjectToScene(events,scene);
                    events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                    Undo.RegisterCreatedObjectUndo(events,"Add new-input UI event system");
                }
                int bottles=0;
                foreach (var glow in all.Select(t=>t.GetComponent<ProximityPotionGlow>()).Where(g=>g!=null))
                {
                    string entryPath=Folder+"/Entries/Vessel_"+(++bottles).ToString("00")+".asset";
                    var entry=AssetDatabase.LoadAssetAtPath<NarrativeEntry>(entryPath);
                    if (entry==null)
                    {
                        entry=ScriptableObject.CreateInstance<NarrativeEntry>();
                        entry.category="SEALED VESSEL / "+bottles.ToString("00");
                        AssetDatabase.CreateAsset(entry,entryPath);
                    }
                    var inspectable=glow.GetComponent<NarrativeInspectable>();
                    if (inspectable==null) inspectable=Undo.AddComponent<NarrativeInspectable>(glow.gameObject);
                    Undo.RecordObject(inspectable,"Connect editable bottle observation");
                    inspectable.entry=entry;
                    inspectable.focusRenderer=glow.liquidRenderer;
                    inspectable.interactionDistance=3f;
                    EditorUtility.SetDirty(inspectable);
                    if (PrefabUtility.IsPartOfPrefabInstance(inspectable)) PrefabUtility.RecordPrefabInstancePropertyModifications(inspectable);
                }
                Undo.CollapseUndoOperations(group);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save narrative interface.");
                reports.Add(new SceneReport {scene=path,backup=backup,portals=all.Count(t=>t.GetComponent<TransitionPoint>()!=null),
                    inspectableBottles=bottles,canvases=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Canvas>(true).Length),
                    eventSystems=scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<EventSystem>(true).Length)});
            }
            finally { if (opened && scene.isLoaded) EditorSceneManager.CloseScene(scene,true); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Library/XiaoduanNarrativeUI.json",JsonUtility.ToJson(new Report {status="configured",scenes=reports.ToArray(),
            prefab=prefabPath,theme=AssetDatabase.GetAssetPath(theme),font=AssetDatabase.GetAssetPath(font),
            message="English reusable uGUI HUD, E portal prompts and five bottle observation entries. Existing portal triggers preserved; one target consumes E."},true));
        File.WriteAllText(Marker,Version);
        Debug.Log("[NarrativeUI] English portal and observation interface saved in ROOM1 and ROOM2.");
    }
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));
        var rect=go.GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=anchor; rect.anchorMax=anchor;
        rect.pivot=pivot; rect.sizeDelta=size; rect.anchoredPosition=position;
        return rect;
    }
    static RectTransform Stretch(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var rect=Rect(name,parent,Vector2.zero,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=min; rect.offsetMax=max;
        return rect;
    }
    static UnityEngine.UI.Image Image(RectTransform rect,Color color,bool raycast=false)
    { var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.raycastTarget=raycast; return image; }
    static TextMeshProUGUI Text(RectTransform rect,NarrativeTheme theme,string text,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.Left)
    {
        var label=rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font=theme.font; label.text=text; label.fontSize=size; label.color=color;
        label.alignment=align; label.raycastTarget=false; label.enableAutoSizing=false;
        label.margin=Vector4.zero; return label;
    }
    public static GameObject BuildView(NarrativeTheme theme)
    {
        var root=new GameObject("Narrative Interface",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=100;
        var scaler=root.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
        var hud=root.AddComponent<NarrativeHUD>(); hud.theme=theme;
        var safe=Stretch("Safe Area",root.transform,Vector2.zero,Vector2.zero); hud.safeArea=safe;
        var room=Rect("Location",safe,new Vector2(0,1),new Vector2(0,1),new Vector2(500,24),new Vector2(36,-32));
        hud.roomText=Text(room,theme,"01 / THE TOWN",15,theme.muted);
        Image(Rect("Location Accent",safe,new Vector2(0,1),new Vector2(0,1),new Vector2(32,2),new Vector2(36,-67)),theme.accent);
        hud.dot=Image(Rect("Reticle",safe,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(3,3),Vector2.zero),theme.muted);
        var focus=Rect("Focus Brackets",safe,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(20,20),Vector2.zero);
        hud.focusGroup=focus.gameObject.AddComponent<CanvasGroup>(); hud.focusGroup.alpha=0; hud.focusGroup.blocksRaycasts=false;
        foreach (float x in new[]{-1f,1f}) foreach (float y in new[]{-1f,1f})
        {
            Image(Rect("Focus H",focus,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(5,1),new Vector2(x*8,y*10)),theme.accent);
            Image(Rect("Focus V",focus,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1,5),new Vector2(x*10,y*8)),theme.accent);
        }
        var prompt=Rect("Interaction Prompt",safe,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(620,86),new Vector2(0,110));
        hud.promptRect=prompt; hud.promptGroup=prompt.gameObject.AddComponent<CanvasGroup>(); hud.promptGroup.alpha=0; hud.promptGroup.blocksRaycasts=false;
        Image(Stretch("Background",prompt,Vector2.zero,Vector2.zero),new Color(.025f,.04f,.033f,.9f));
        Image(Rect("Accent",prompt,Vector2.zero,Vector2.zero,new Vector2(2,86),Vector2.zero),theme.accent);
        var key=Rect("Key",prompt,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(42,42),new Vector2(26,0));
        Image(key,new Color(.13f,.2f,.15f,.95f));
        Text(Stretch("Letter",key,Vector2.zero,Vector2.zero),theme,"E",21,theme.text,TextAlignmentOptions.Center);
        hud.promptAction=Text(Rect("Action",prompt,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(510,30),new Vector2(88,12)),theme,"Enter",24,theme.text);
        hud.promptTitle=Text(Rect("Target",prompt,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(510,24),new Vector2(88,-16)),theme,"The light beyond",16,theme.muted);
        var intro=Rect("Controls",safe,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(850,32),new Vector2(0,48));
        hud.introGroup=intro.gameObject.AddComponent<CanvasGroup>(); hud.introGroup.blocksRaycasts=false;
        hud.introText=Text(Stretch("Text",intro,Vector2.zero,Vector2.zero),theme,theme.introLabel,16,theme.text,TextAlignmentOptions.Center);
        var reading=Stretch("Observation Overlay",safe,Vector2.zero,Vector2.zero);
        hud.readingGroup=reading.gameObject.AddComponent<CanvasGroup>(); hud.readingGroup.alpha=0;
        hud.readingGroup.blocksRaycasts=false; hud.readingGroup.interactable=false;
        Image(Stretch("Dimmer",reading,Vector2.zero,Vector2.zero),new Color(.005f,.012f,.008f,.7f),true);
        var card=Rect("Observation Card",reading,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(900,620),Vector2.zero);
        hud.cardRect=card; Image(card,theme.panel,true);
        Image(Rect("Top Accent",card,new Vector2(0,1),new Vector2(0,1),new Vector2(88,3),new Vector2(56,0)),theme.accent);
        hud.cardCategory=Text(Rect("Category",card,new Vector2(0,1),new Vector2(0,1),new Vector2(550,30),new Vector2(56,-42)),theme,"SEALED VESSEL / 01",16,theme.accent);
        hud.cardTitle=Text(Rect("Title",card,new Vector2(0,1),new Vector2(0,1),new Vector2(760,80),new Vector2(56,-92)),theme,"Green liquid",44,theme.text);
        var divider=Rect("Divider",card,new Vector2(0,1),new Vector2(0,1),new Vector2(788,1),new Vector2(56,-191));
        divider.anchorMax=Vector2.one; divider.sizeDelta=new Vector2(-112,1);
        Image(divider,new Color(.4f,.55f,.44f,.35f));
        hud.cardBody=Text(Stretch("Body",card,new Vector2(56,130),new Vector2(-56,-220)),theme,"",27,theme.text,TextAlignmentOptions.TopLeft);
        hud.cardBody.lineSpacing=12;
        var buttonRect=Rect("Close",card,Vector2.one,Vector2.one,new Vector2(152,36),new Vector2(-28,-28));
        Image(buttonRect,new Color(.1f,.15f,.12f,.8f),true);
        hud.closeButton=buttonRect.gameObject.AddComponent<UnityEngine.UI.Button>();
        hud.closeButton.targetGraphic=buttonRect.GetComponent<UnityEngine.UI.Image>();
        var colors=hud.closeButton.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(.6f,.85f,.65f); colors.pressedColor=new Color(.4f,.65f,.4f); hud.closeButton.colors=colors;
        hud.closeText=Text(Stretch("Text",buttonRect,Vector2.zero,Vector2.zero),theme,"Close [ESC]",15,theme.text,TextAlignmentOptions.Center);
        hud.returnText=Text(Rect("Return Hint",card,Vector2.zero,Vector2.zero,new Vector2(760,32),new Vector2(56,48)),theme,theme.returnLabel,16,theme.muted);
        var interactor=root.AddComponent<NarrativeInteractor>(); interactor.view=hud;
        return root;
    }
}
#endif
