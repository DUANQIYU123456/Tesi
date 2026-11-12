#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Xiaoduan.Room3;
using Xiaoduan.Survey;

public static class MemoryArchiveBuilder
{
    const string Folder="Assets/Room3/SpatialMemory";
    static Transform root;
    static Material concrete,dark,metal,phosphor,white,strata,mask;
    static TMP_FontAsset font;
    static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
    static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,bool solid=false,Transform parent=null)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent??root,false);
        go.transform.localPosition=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
        if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return go;
    }
    static TextMeshPro Sign(string name,string content,Vector3 position,float size,float width=3)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
        var t=go.AddComponent<TextMeshPro>();t.font=font;t.fontSize=size;t.text=content;t.color=new Color(.72f,.82f,.77f);
        t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,2);t.enableAutoSizing=false;return t;
    }
    static Material MaterialAt(string name,string shader,Color color)
    {
        var path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}
        if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);return m;
    }
    static ResidualPortal Portal(string name,Vector3 position,float yaw,ResidualPortal.Purpose purpose)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        var p=go.AddComponent<ResidualPortal>();p.purpose=purpose;
        p.screen=Box("Projected opening",Vector3.zero,P(3.2f,3.8f,.04f),mask,false,go.transform).GetComponent<MeshRenderer>();
        for(int side=-1;side<=1;side+=2)
        {
            Box("Threshold upright",P(side*1.72f,0,0),P(.22f,4.05f,.35f),metal,true,go.transform);
            Box("White edge inset",P(side*1.59f,0,.19f),P(.018f,3.75f,.018f),white,false,go.transform);
        }
        Box("Threshold lintel",P(0,1.99f,0),P(3.65f,.18f,.35f),metal,true,go.transform);
        return p;
    }
    static AudioSource Sound(string name,AudioClip clip)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.position=P(80,1.6f,-9);
        var s=go.AddComponent<AudioSource>();s.clip=clip;s.loop=true;s.playOnAwake=true;s.volume=0;s.spatialBlend=1;s.minDistance=3;s.maxDistance=23;s.rolloffMode=AudioRolloffMode.Linear;s.priority=180;return s;
    }
    public static string Apply()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(EditorApplication.isPlaying||scene.name!="ROOM3")throw new Exception("Open ROOM3 in Edit mode.");
        if(GameObject.Find("ROOM3 - Repeated labor archive")!=null)throw new Exception("Archive already exists; inspect it before applying again.");
        foreach(var sub in new[]{Folder,Folder+"/Materials"})if(!AssetDatabase.IsValidFolder(sub)){var at=sub.LastIndexOf('/');AssetDatabase.CreateFolder(sub.Substring(0,at),sub.Substring(at+1));}
        var renderers=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        concrete=renderers.First(r=>r.name=="Calibration landing").sharedMaterial;
        metal=renderers.First(r=>r.name=="Narrow core beam").sharedMaterial;
        dark=MaterialAt("Archive cavity","Universal Render Pipeline/Lit",new Color(.045f,.061f,.058f));
        phosphor=MaterialAt("Retained circuit","Universal Render Pipeline/Lit",new Color(.15f,.28f,.19f));
        phosphor.EnableKeyword("_EMISSION");phosphor.SetColor("_EmissionColor",new Color(.10f,.78f,.27f));
        white=MaterialAt("Cold threshold","Universal Render Pipeline/Lit",new Color(.62f,.77f,.72f));white.EnableKeyword("_EMISSION");white.SetColor("_EmissionColor",new Color(.85f,1.15f,1.06f));
        strata=MaterialAt("Exposed mineral skin","Xiaoduan/Memory/Exposed Strata",new Color(.45f,.50f,.48f));strata.SetTexture("_BaseMap",concrete.GetTexture("_BaseMap"));
        mask=MaterialAt("Projected threshold","Xiaoduan/Memory/PortalMask",Color.white);mask.SetColor("_InactiveColor",new Color(.015f,.025f,.02f));
        font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Survey/Fonts/IBM Plex Mono Medium SDF.asset");
        if(font==null)font=UnityEngine.Object.FindFirstObjectByType<Room3StoryUI>().archiveTitle.font;
        root=new GameObject("ROOM3 - Repeated labor archive").transform;Undo.RegisterCreatedObjectUndo(root.gameObject,"Build recursive labor archive");
        var memory=root.gameObject.AddComponent<Room3RecursiveArchive>();var flow=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();memory.flow=flow;flow.recursiveArchive=memory;
        var entry=Portal("01 Entry threshold",P(0,1.9f,0),180,ResidualPortal.Purpose.Entrance);
        var arrival=Portal("02 Repeated arrival",P(80,1.9f,-20),0,ResidualPortal.Purpose.Arrival);
        var repeat=Portal("03 Repeated exit",P(80,1.9f,0),180,ResidualPortal.Purpose.Repetition);
        var release=Portal("04 Released arrival",P(0,1.9f,2),0,ResidualPortal.Purpose.Release);
        entry.linked=arrival;arrival.linked=entry;repeat.linked=arrival;release.linked=repeat;
        memory.portals=new[]{entry,arrival,repeat,release};memory.repetition=repeat;memory.arrival=arrival;memory.release=release;
        for(int side=-1;side<=1;side+=2)Box("Route threshold surround",P(side*2.65f,1.9f,.15f),P(1.8f,3.8f,.55f),concrete,true);
        Sign("Route plaque","ARCHIVE / REPEATED LABOR",P(0,4.25f,-.3f),.19f,5);
        Box("Repeated floor",P(80,-.3f,-10),P(5,.6f,22),concrete,true);
        Box("Repeated ceiling",P(80,4.5f,-10),P(5,.4f,22),concrete,true);
        foreach(float z in new[]{-20f,0f})
        {
            for(int side=-1;side<=1;side+=2)Box("End-wall return",P(80+side*2.1f,2.2f,z),P(.7f,4.4f,.5f),concrete,true);
            Box("End-wall header",P(80,4.1f,z),P(4.8f,.6f,.5f),concrete,true);
        }
        for(int side=-1;side<=1;side+=2)
        {
            Box("Structural backing",P(80+side*2.55f,2.2f,-10),P(.45f,4.5f,22),dark,true);
            for(int i=0;i<12;i++)Box("Embedded vertical circuit",P(80+side*2.28f,2.2f,-20+i*1.75f),P(.024f,3.8f,.016f),phosphor);
            for(int i=0;i<5;i++)Box("Embedded cross connection",P(80+side*2.28f,.45f+i*.78f,-10),P(.024f,.015f,21),phosphor);
        }
        var skins=new System.Collections.Generic.List<Renderer>();
        for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++)
        {
            var panel=Box("Mineral skin "+side+" "+i,P(80+side*2.16f,2.15f,-18+i*4),P(.12f,4.2f,3.93f),strata);
            skins.Add(panel.GetComponent<Renderer>());
        }
        memory.skins=skins.ToArray();memory.skinTransforms=skins.Select(r=>r.transform).ToArray();
        for(int i=0;i<10;i++)
        {
            float z=-19+i*2;Box("Floor movement joint",P(80,.008f,z),P(4.3f,.012f,.015f),metal);
            Box("Ceiling inspection strip",P(80,4.27f,z),P(1.25f,.025f,.045f),white);
        }
        for(int i=0;i<3;i++)
        {
            var light=new GameObject("Work light "+i);light.transform.SetParent(root,false);light.transform.position=P(80,3.7f,-17+i*7);
            var l=light.AddComponent<Light>();l.type=LightType.Point;l.color=new Color(.65f,.78f,.73f);l.intensity=4;l.range=9;l.shadows=LightShadows.None;
        }
        // A tangible workstation anchors the comparison in the route, not only in HUD text.
        var station=new GameObject("Labor comparison workstation").transform;station.SetParent(root,false);station.localPosition=P(81.5f,0,-8);
        Box("Worktop",P(0,1,0),P(1.3f,.12f,.7f),metal,true,station);
        for(int side=-1;side<=1;side+=2)Box("Worktop support",P(side*.65f,.5f,0),P(.09f,1,.55f),metal,true,station);
        Box("Data monitor",P(0,1.45f,.14f),P(.95f,.62f,.08f),dark,false,station);
        Box("Retained display",P(0,1.45f,.092f),P(.83f,.5f,.018f),phosphor,false,station);
        // A small watch-dial plate and chip pin rows share one physical bench.
        var dial=GameObject.CreatePrimitive(PrimitiveType.Cylinder);dial.name="Unfinished dial";dial.transform.SetParent(station,false);dial.transform.localPosition=P(-.5f,1.08f,-.15f);dial.transform.localScale=P(.25f,.012f,.25f);dial.GetComponent<Renderer>().sharedMaterial=white;UnityEngine.Object.DestroyImmediate(dial.GetComponent<Collider>());
        for(int i=0;i<5;i++)Box("Microchip contact",P(.28f+i*.065f,1.077f,-.18f),P(.025f,.02f,.22f),phosphor,false,station);
        var focus=new GameObject("Ledger focus").transform;focus.SetParent(station,false);focus.localPosition=P(0,1.45f,-.1f);
        var node=station.gameObject.AddComponent<Room3StoryNode>();node.kind=Room3StoryNode.Kind.LaborLedger;node.label="Labor / the work behind the output";node.focus=focus;memory.ledger=node;
        flow.nodes=flow.nodes.Concat(new[]{node}).ToArray();
        memory.workLabel=Sign("Workstation label","",P(81.5f,2.3f,-8.12f),.18f,2.1f);
        memory.passLabel=Sign("Iteration index","REPETITION 00\nOUTPUT UNCHANGED",P(80,3.2f,-.3f),.25f,3);
        memory.watches=Sound("Mechanical time / local",flow.clockSource.clip);memory.clicks=Sound("Digital time / local",flow.clickSource.clip);
        var ui=flow.ui;var diagramObject=new GameObject("Labor comparison diagram",typeof(RectTransform));var rect=diagramObject.GetComponent<RectTransform>();rect.SetParent(ui.archivePanel.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(435,50);rect.sizeDelta=new Vector2(275,350);
        var diagram=diagramObject.AddComponent<TextMeshProUGUI>();diagram.font=ui.archiveSource.font;diagram.fontSize=24;diagram.color=new Color(.55f,.80f,.66f);diagram.alignment=TextAlignmentOptions.Center;diagram.raycastTarget=false;
        diagram.text="DIAL\nBRUSH / HAND / TIME\n\nDATA\nLABEL / CHECK / TIME\n\nWHO IS MISSING\nFROM THE OUTPUT?";diagramObject.SetActive(false);ui.ledgerDiagram=diagram;
        EditorUtility.SetDirty(flow);EditorUtility.SetDirty(ui);foreach(var m in new[]{dark,white,phosphor,strata,mask})EditorUtility.SetDirty(m);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        return "Added four mapped thresholds, a two-pass corridor, exposed strata and a labor ledger; existing scene hierarchy preserved.";
    }
}
#endif
