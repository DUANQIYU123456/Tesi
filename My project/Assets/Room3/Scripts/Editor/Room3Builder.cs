#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Xiaoduan.Narrative;
using Xiaoduan.Room3;

public static class Room3Builder
{
    const string Folder = "Assets/Room3";
    const string ScenePath = "Assets/Scenes/ROOM3.unity";
    static Material concrete, pale, dark, metal, green, white, red, glass;
    static Transform root;
    static Mesh bevel;
    static TMP_FontAsset font;
    static System.Random random;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    static float R(float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
    static Transform Group(string name, Transform parent=null)
    {
        var g=new GameObject(name).transform; g.SetParent(parent??root,false); return g;
    }
    static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,Transform parent=null,bool solid=false,Vector3 euler=default)
    {
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));
        g.transform.SetParent(parent??root,false);g.transform.localPosition=position;g.transform.localScale=scale;g.transform.localEulerAngles=euler;
        g.GetComponent<MeshFilter>().sharedMesh=bevel;
        var rend=g.GetComponent<MeshRenderer>();rend.sharedMaterial=material;
        rend.shadowCastingMode=(material==green||material==white||material==glass) ? ShadowCastingMode.Off : ShadowCastingMode.On;
        if(solid)g.AddComponent<BoxCollider>();
        return g;
    }
    static Mesh BeveledCube()
    {
        string path=Folder+"/Meshes/ChamferedBlock.asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(old!=null)return old;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        void Polygon(Vector3[] p,Vector3 normal)
        {
            normal.Normalize(); if(Vector3.Dot(Vector3.Cross(p[1]-p[0],p[2]-p[0]),normal)<0)Array.Reverse(p);
            int offset=vertices.Count;
            foreach(var v in p){vertices.Add(v);normals.Add(normal);uv.Add(new Vector2(v.x+v.z,v.y+v.z));}
            for(int i=1;i<p.Length-1;i++){triangles.Add(offset);triangles.Add(offset+i);triangles.Add(offset+i+1);}
        }
        Vector3 P(int a,float av,int b,float bv,int c,float cv){var p=Vector3.zero;p[a]=av;p[b]=bv;p[c]=cv;return p;}
        float h=.5f, k=.477f;
        for(int a=0;a<3;a++)for(int s=-1;s<=1;s+=2)
        {int b=(a+1)%3,c=(a+2)%3;var n=Vector3.zero;n[a]=s;
         Polygon(new[]{P(a,s*h,b,-k,c,-k),P(a,s*h,b,k,c,-k),P(a,s*h,b,k,c,k),P(a,s*h,b,-k,c,k)},n);}
        for(int c=0;c<3;c++)for(int sa=-1;sa<=1;sa+=2)for(int sb=-1;sb<=1;sb+=2)
        {int a=(c+1)%3,b=(c+2)%3;var n=Vector3.zero;n[a]=sa;n[b]=sb;
         Polygon(new[]{P(a,sa*h,b,sb*k,c,-k),P(a,sa*h,b,sb*k,c,k),P(a,sa*k,b,sb*h,c,k),P(a,sa*k,b,sb*h,c,-k)},n);}
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            Polygon(new[]{V(x*h,y*k,z*k),V(x*k,y*h,z*k),V(x*k,y*k,z*h)},V(x,y,z));
        var m=new Mesh{name="Chamfered concrete block"};m.SetVertices(vertices);m.SetNormals(normals);m.SetUVs(0,uv);m.SetTriangles(triangles,0);m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material Lit(string name,Color tint,float metallic=0,float smooth=.2f,Color? emission=null)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",tint);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",smooth);
        if(emission.HasValue){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",emission.Value);}
        EditorUtility.SetDirty(mat);return mat;
    }
    static Material Concrete(string name,Color tint)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat==null){mat=new Material(Shader.Find("Xiaoduan/Room3/World Concrete"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",tint);mat.SetFloat("_TextureScale",.24f);
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LeartesStudios/HDRP/Art/Textures/T_Wall_Concrete_BC.png"));
        EditorUtility.SetDirty(mat);return mat;
    }
    static TextMeshPro Label(string text,Vector3 position,float size,Color color,Transform parent=null,float width=8,Vector3 euler=default)
    {
        var g=new GameObject("Sign - "+text.Replace('\n',' '));g.transform.SetParent(parent??root,false);g.transform.localPosition=position;g.transform.localEulerAngles=euler;
        var t=g.AddComponent<TextMeshPro>();t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=TextAlignmentOptions.Center;t.rectTransform.sizeDelta=new Vector2(width,4);t.enableWordWrapping=false;t.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return t;
    }
    static Light Lamp(string name,Vector3 position,Color color,float intensity,float range,Transform parent=null)
    {
        var t=Group(name,parent);t.localPosition=position;var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;
    }
    static void Motion(Transform t,Vector3 rotation,float amplitude=.2f,float rate=.16f)
    {var m=t.gameObject.AddComponent<Room3Motion>();m.angularVelocity=rotation;m.bobAmplitude=amplitude;m.bobFrequency=rate;m.phase=R(0,6.28f);}
    static void Beam(Vector3 a,Vector3 b,float width,Material material,Transform parent=null)
    {var g=Box("Suspension cable",(a+b)*.5f,V(width,width,Vector3.Distance(a,b)),material,parent);g.transform.localRotation=Quaternion.LookRotation(b-a);}
    static void Floor(string name,Vector3 p,Vector3 size,Transform parent=null)
    {Box(name,p+Vector3.down*.3f,size,concrete,parent,true);}
    static void Portal(string name,Vector3 position,string target,TransitionDestination.DestinationTag tag,Transform parent=null)
    {
        var frame=Group(name,parent);frame.localPosition=position;
        Box("Left jamb",V(-1.65f,2,0),V(.4f,4.4f,.6f),metal,frame,true);
        Box("Right jamb",V(1.65f,2,0),V(.4f,4.4f,.6f),metal,frame,true);
        Box("Lintel",V(0,4.1f,0),V(3.7f,.35f,.6f),metal,frame,true);
        for(int s=-1;s<=1;s+=2)Box("Threshold glow",V(s*1.4f,2,-.32f),V(.035f,3.9f,.035f),green,frame);
        var trigger=new GameObject("E - "+target,typeof(BoxCollider),typeof(Rigidbody),typeof(TransitionPoint));trigger.transform.SetParent(frame,false);trigger.transform.localPosition=V(0,1.5f,0);
        var box=trigger.GetComponent<BoxCollider>();box.isTrigger=true;box.size=V(3,3,3.5f);
        var rb=trigger.GetComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var point=trigger.GetComponent<TransitionPoint>();point.sceneName=target;point.destinationTag=tag;point.transitionType=TransitionPoint.TransitionType.DifferentScene;
        Label(target=="ROOM2"?"RETURN / THE PRISON":"RETURN / THE TOWN",V(0,4.7f,-.35f),.65f,new Color(.65f,.8f,.74f),frame);
    }
    static void Terminal(string id,string title,string body,Vector3 position,Transform parent=null)
    {
        var g=Group("Archive terminal - "+id,parent);g.localPosition=position;
        Box("Plinth",V(0,.55f,0),V(1.4f,1.1f,.85f),dark,g,true);
        var panel=Box("Archive display",V(0,1.28f,-.05f),V(1.35f,.5f,.65f),metal,g,false,V(-15,0,0));
        Box("Active stripe",V(0,1.55f,-.35f),V(1.12f,.025f,.025f),green,g);
        Label(id,V(0,1,-.46f),.28f,new Color(.6f,.8f,.7f),g,1.2f);
        var inspect=g.gameObject.AddComponent<NarrativeInspectable>();inspect.focusRenderer=panel.GetComponent<Renderer>();inspect.interactionDistance=3.2f;
        string path=Folder+"/Entries/"+id+".asset";var entry=AssetDatabase.LoadAssetAtPath<NarrativeEntry>(path);
        if(entry==null){entry=ScriptableObject.CreateInstance<NarrativeEntry>();AssetDatabase.CreateAsset(entry,path);}
        entry.title=title;entry.category="RESIDUAL ARCHIVE / "+id;entry.body=body;EditorUtility.SetDirty(entry);inspect.entry=entry;
    }
    static void SetupRenderer(Room3Environment environment)
    {
        string rp=Folder+"/Room3Renderer.asset";
        var data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rp);
        if(data==null)
        {
            var active=UniversalRenderPipeline.asset;
            var so=new SerializedObject(active);var list=so.FindProperty("m_RendererDataList");
            var source=list.GetArrayElementAtIndex(so.FindProperty("m_DefaultRendererIndex").intValue).objectReferenceValue;
            if(!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),rp))throw new Exception("Cannot duplicate URP renderer.");
            data=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rp);
        }
        // This renderer belongs only to ROOM3; the existing scanner renderer stays untouched.
        foreach(var feature in data.rendererFeatures.ToArray())
        {data.rendererFeatures.Remove(feature);if(feature!=null)UnityEngine.Object.DestroyImmediate(feature,true);}
        var fog=ScriptableObject.CreateInstance<VolumetricFogRendererFeature>();fog.name="Room3 - volumetric backlight";AssetDatabase.AddObjectToAsset(fog,data);data.rendererFeatures.Add(fog);fog.Create();
        var map=new SerializedObject(data);var fm=map.FindProperty("m_RendererFeatureMap");fm.arraySize=1;AssetDatabase.TryGetGUIDAndLocalFileIdentifier(fog,out string _,out long localId);fm.GetArrayElementAtIndex(0).longValue=localId;map.ApplyModifiedPropertiesWithoutUndo();data.SetDirty();EditorUtility.SetDirty(data);
        var pipelines=AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.StartsWith("Assets/")).Select(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>).ToArray();
        int slot=pipelines.Max(p=>new SerializedObject(p).FindProperty("m_RendererDataList").arraySize);
        foreach(var pipeline in pipelines)
        {
            var ps=new SerializedObject(pipeline);var pl=ps.FindProperty("m_RendererDataList");pl.arraySize=slot+1;pl.GetArrayElementAtIndex(slot).objectReferenceValue=data;ps.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        }
        environment.rendererIndex=slot;
    }
    static void SetupVolume()
    {
        string path=Folder+"/Room3Atmosphere.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        foreach(var component in profile.components.ToArray())UnityEngine.Object.DestroyImmediate(component,true);profile.components.Clear();
        T Add<T>() where T:VolumeComponent {var c=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(c,profile);return c;}
        var tone=Add<Tonemapping>();tone.mode.value=TonemappingMode.ACES;
        var bloom=Add<Bloom>();bloom.intensity.value=.65f;bloom.threshold.value=1.15f;bloom.scatter.value=.68f;
        var color=Add<ColorAdjustments>();color.postExposure.value=.5f;color.contrast.value=12;color.saturation.value=-22;
        var vignette=Add<Vignette>();vignette.intensity.value=.2f;vignette.smoothness.value=.6f;
        var grain=Add<FilmGrain>();grain.type.value=FilmGrainLookup.Thin1;grain.intensity.value=.12f;
        var fog=Add<VolumetricFogVolumeComponent>();fog.enabled.value=true;fog.distance.value=150;fog.baseHeight.value=-22;fog.maximumHeight.value=75;fog.density.value=.11f;fog.attenuationDistance.value=140;fog.maxSteps.value=40;fog.blurIterations.value=1;fog.enableMainLightContribution.value=true;fog.enableAdditionalLightsContribution.value=false;fog.scattering.value=.09f;fog.anisotropy.value=.3f;fog.tint.value=new Color(.74f,.84f,.82f);
        var go=Group("ROOM3 Global Volume").gameObject;var v=go.AddComponent<Volume>();v.isGlobal=true;v.priority=20;v.sharedProfile=profile;EditorUtility.SetDirty(profile);
    }
    static void Corridor()
    {
        var c=Group("01 - Isolation corridor");
        Floor("Solid entry floor",V(0,0,-15),V(8,.6f,30),c);
        Box("Corridor roof",V(0,5.5f,-15),V(8,.8f,30),dark,c,true);
        Box("Entry bulkhead",V(0,2.5f,-30),V(8,5.5f,.8f),concrete,c,true);
        Box("Right wall",V(4,2.5f,-15),V(.6f,5.5f,30),concrete,c,true);
        Box("Observation glass",V(-3.8f,2.65f,-12),V(.04f,4.7f,23),glass,c,true);
        for(int i=0;i<8;i++)
        {
            float z=-28+i*4;
            Box("Observation mullion",V(-3.75f,2.6f,z),V(.16f,5.2f,.16f),metal,c,true);
            Box("Concrete rib",V(3.65f,2.6f,z),V(.5f,5.2f,.55f),pale,c);
            Box("Ceiling strip",V(.7f,5.04f,z),V(2.6f,.035f,.11f),white,c);
            Box("Floor guidance",V(3.1f,.025f,z),V(.035f,.025f,1.7f),green,c);
            if(i%2==0)Lamp("Corridor indirect",V(0,4.65f,z),new Color(.62f,.77f,.78f),2,7,c);
        }
        Label("RESIDUAL ARCHIVE",V(0,3.8f,-.6f),.95f,new Color(.72f,.83f,.79f),c);
        Label("SECTOR 03   /   ACCESS UNDER REVIEW",V(0,3,-.6f),.28f,new Color(.39f,.6f,.52f),c);
        Box("Threshold left",V(-4,3,0),V(1.2f,7,1.8f),dark,c,true);
        Box("Threshold right",V(4,3,0),V(1.2f,7,1.8f),dark,c,true);
        Box("Threshold top",V(0,6.5f,0),V(9.2f,1,1.8f),dark,c,true);
        Portal("Return threshold",V(0,0,-26),"ROOM2",TransitionDestination.DestinationTag.B,c);
        Terminal("03-01","The protected side","DESIGN RECORD\n\nThis partition separates two positions within the same workplace. From here, the working space appears distant and orderly.\n\nContinue beyond the partition. Compare what the system permits you to see.\n\nThe architecture is a fictional interpretation; this text is not a historical quotation.",V(2.5f,0,-7),c);
    }
    static void Bridge()
    {
        var b=Group("02 - Suspended causeway");
        // Static, continuous collision: visual joints never require jumping.
        Box("Continuous walkable deck",V(0,-.25f,18),V(3.8f,.5f,36),metal,b,true);
        for(int i=0;i<24;i++)
        {
            float z=.75f+i*1.5f;
            Box("Deck joint",V(0,.012f,z),V(3.7f,.025f,.045f),dark,b);
            for(int s=-1;s<=1;s+=2)
            {
                Box("Green edge inlay",V(s*1.76f,.02f,z),V(.026f,.025f,1.1f),green,b);
                if(i%3==0){Box("Railing post",V(s*1.85f,.55f,z),V(.065f,1.1f,.065f),metal,b,true);}
            }
        }
        for(int s=-1;s<=1;s+=2)
        {
            Box("Handrail",V(s*1.85f,1.05f,18),V(.065f,.065f,36),metal,b,true);
            Box("Structural girder",V(s*1.5f,-.75f,18),V(.25f,1.2f,36),dark,b);
            // Transparent collision rails prevent accidental falls without widening the visible beam.
            var guard=new GameObject("Safety boundary",typeof(BoxCollider));guard.transform.SetParent(b,false);guard.transform.localPosition=V(s*2,1.25f,18);guard.GetComponent<BoxCollider>().size=V(.15f,2.5f,36);
            Beam(V(s*1.7f,-.65f,8),V(s*18,20,14),.04f,metal,b);
            Beam(V(s*1.7f,-.65f,26),V(s*22,26,39),.04f,metal,b);
        }
        Floor("Observation landing",V(0,0,16),V(7,.6f,4),b);
        Lamp("Causeway reflected light",V(0,4,18),new Color(.37f,.56f,.52f),4,15,b);
    }
    static void FloatingArchitecture()
    {
        var all=Group("03 - Displaced architecture");
        for(int side=-1;side<=1;side+=2)for(int i=0;i<6;i++)
        {
            var g=Group("Suspended gallery "+side+" / "+i,all);g.localPosition=V(side*R(16,30),R(-12,24),i*11-6);g.localEulerAngles=V(R(-16,16),side*R(-28,28),side*R(-25,25));
            float w=R(8,15),length=R(9,16);
            for(int level=0;level<3;level++)
            {
                float y=level*4.1f;
                Box("Cantilever slab",V(0,y,0),V(w,.8f,length),concrete,g);
                for(int col=0;col<4;col++)Box("Pier",V(-w*.5f+col*w/3,y+1.85f,length*.3f),V(.55f,3.5f,.7f),dark,g);
                Box("Gallery lip",V(0,y+.5f,-length*.5f),V(w,.25f,.3f),pale,g);
                for(int j=0;j<8;j++)Box("Exposed rebar",V(-w*.44f+j*w*.125f,y,-length*.56f),V(.035f,.035f,1.4f),metal,g);
            }
            for(int k=0;k<9;k++)Box("Broken staircase",V(w*.15f,k*.32f-2,-length*.45f+k*.48f),V(2.3f,.28f,.6f),pale,g);
            Motion(g,V(0,side*.16f,side*.08f),.35f,.12f);
        }
        for(int i=0;i<60;i++)
        {
            float side=i%2==0 ? -1:1;
            var g=Box("Drifting concrete fragment",V(side*R(7,43),R(-25,38),R(0,95)),V(R(.4f,2),R(.3f,1.8f),R(.4f,3)),i%3==0?pale:concrete,all,false,V(R(0,80),R(0,120),R(0,70)));
            if(i%3==0)Motion(g.transform,V(R(-1,1),R(-2,2),R(-1,1)),.2f,.22f);
        }
        // A few immense distant walls provide scale and depth behind the moving fragments.
        for(int s=-1;s<=1;s+=2)for(int i=0;i<8;i++)
            Box("Distant shear wall",V(s*(40+i*.8f),i*5-16,40+i*6),V(3.5f,45,12),dark,all,false,V(0,s*12,s*i*3));
    }
    static void DialAndCore()
    {
        var area=Group("04 - The dial and archive core");
        // Dodecagonal floor. Triangles are deliberately generous to walk across.
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name="Twelve-sided core platform";go.transform.SetParent(area,false);go.transform.localPosition=V(0,-.5f,43);go.transform.localScale=V(22,.5f,22);go.GetComponent<Renderer>().sharedMaterial=concrete;
        var old=go.GetComponent<Collider>();UnityEngine.Object.DestroyImmediate(old);var mc=go.AddComponent<MeshCollider>();mc.sharedMesh=go.GetComponent<MeshFilter>().sharedMesh;
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;Vector3 p=V(Mathf.Sin(a)*10.3f,.025f,43+Mathf.Cos(a)*10.3f);
            Box("Platform perimeter marker",p,V(.055f,.03f,1.1f),green,area,false,V(0,i*30,0));
        }
        var dial=Group("The suspended dial - decorative",area);dial.localPosition=V(0,17,60);Motion(dial,V(0,0,.24f),.2f,.09f);
        string[] numerals={"XII","I","II","III","IV","V","VI","VII","VIII","IX","X","XI"};
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;Vector3 p=V(Mathf.Sin(a)*19,Mathf.Cos(a)*19,0);
            var n=Group("Index "+numerals[i],dial);n.localPosition=p;n.localEulerAngles=V(0,0,-i*30);
            Box("Radial block",V(0,0,0),V(5.5f,2.8f,3.5f),pale,n,false,V(0,R(-7,7),R(-4,4)));
            Box("Index light",V(0,0,-1.82f),V(.055f,1.7f,.035f),green,n);
            for(int j=0;j<3;j++)Box("Sheared layers",V(j*.17f,1.65f+j*.35f,j*.2f),V(4.5f-j*.6f,.24f,3),concrete,n);
            // Keep the numeral upright relative to its own architectural marker.
            Label(numerals[i],V(0,-.1f,-1.83f),1.15f,new Color(.59f,.75f,.64f),n,5);
        }
        var hand=Group("Slow clock hand",dial);Motion(hand,V(0,0,-.7f),0);
        Box("Clock hand",V(0,7,-1.2f),V(.48f,15,1),dark,hand);
        Box("Clock hand inset",V(0,7,-1.73f),V(.035f,14,.035f),green,hand);
        Box("Central suspended monolith",V(0,8,59),V(3.5f,12,4),dark,area,false,V(0,18,0));
        Box("Monolith seam",V(0,8,56.8f),V(.06f,10,.04f),green,area);
        for(int i=0;i<9;i++)
            Box("Backlight aperture",V((i-4)*3.3f,14,77),V(.22f,44,.2f),white,area);
        // The bright source is framed by solid slabs rather than filling the entire sky.
        Box("Back wall left",V(-23,18,79),V(11,62,5),dark,area);
        Box("Back wall right",V(23,18,79),V(11,62,5),dark,area);
        Terminal("03-02","A record can be removed","SYSTEM MAINTENANCE / FICTIONAL INTERFACE\n\nThe instruction is to clear an anomaly. The archive contains traces of work, illness and attempts to establish responsibility.\n\nRemoving a record would make this space easier to classify. It would not undo the event it records.\n\nHistorical documents and their sources will be added during the next research pass.",V(-3.5f,0,40),area);
        Terminal("03-03","The witness position","PROJECT NOTE\n\nYou arrived to inspect a fault. You now stand inside the material the system asks you to classify.\n\nThe dial and suspended architecture are artistic constructions. The green light represents a trace that persists; it is not a scientific depiction of glowing human remains.\n\nThis first ROOM3 prototype establishes the space and route. The final archive decision is still in development.",V(3.5f,0,40),area);
        Label("THE RESIDUAL ARCHIVE",V(0,3.2f,47),.6f,new Color(.62f,.8f,.69f),area);
        Label("WHAT REMAINS AFTER THE RECORD IS CLEARED?",V(0,2.65f,47),.23f,new Color(.48f,.62f,.56f),area,12);
        Portal("Town exit threshold",V(0,0,50),"ROOM1",TransitionDestination.DestinationTag.ENTER,area);
        Lamp("Core green reflected light",V(0,4,45),new Color(.52f,.82f,.43f),5,18,area);
    }
    static void Rig(Scene scene)
    {
        // Copy the user's working rig, including their camera/input settings.
        var source=EditorSceneManager.OpenScene("Assets/Scenes/ROOM1.unity",OpenSceneMode.Additive);
        var items=source.GetRootGameObjects();var player=items.FirstOrDefault(g=>g.CompareTag("Player"));
        if(player==null)throw new Exception("ROOM1 player rig not found.");
        var container=new GameObject("ROOM3 Player rig");SceneManager.MoveGameObjectToScene(container,source);
        foreach(var item in items.Where(g=>g==player||g.name=="MainCamera"||g.name=="Main Camera"||g.name=="PlayerFollowCamera"))item.transform.SetParent(container.transform,true);
        var rig=UnityEngine.Object.Instantiate(container);rig.name=container.name;SceneManager.MoveGameObjectToScene(rig,scene);
        var clonedPlayer=rig.GetComponentsInChildren<Transform>().First(t=>t.CompareTag("Player"));clonedPlayer.SetPositionAndRotation(V(0,.15f,-21),Quaternion.identity);
        EditorSceneManager.CloseScene(source,true);SceneManager.SetActiveScene(scene);
        var main=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c=>c.CompareTag("MainCamera"));
        if(main!=null){main.enabled=true;main.allowHDR=true;main.GetUniversalAdditionalCameraData().renderPostProcessing=true;}
        var ui=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Narrative/NarrativeInterface.prefab");if(ui!=null)PrefabUtility.InstantiatePrefab(ui,scene);
        var events=new GameObject("ROOM3 EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));SceneManager.MoveGameObjectToScene(events,scene);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }
    [MenuItem("Tools/Xiaoduan/ROOM3/Build Residual Archive")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before building ROOM3.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Open ROOM3 before building.");
        string backup="Library/Room3Backups/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(scene);File.Copy(ScenePath,backup+"/ROOM3.unity");
        foreach(var path in new[]{"ProjectSettings/EditorBuildSettings.asset","ProjectSettings/QualitySettings.asset","Packages/manifest.json"})if(File.Exists(path))File.Copy(path,backup+"/"+Path.GetFileName(path));
        foreach(var r in scene.GetRootGameObjects())
        {if(r.name!="Main Camera"&&r.name!="Directional Light"&&r.name!="ROOM3 - Residual Archive")throw new Exception("ROOM3 contains user content: "+r.name);}
        foreach(var r in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(r);
        foreach(var path in new[]{"Materials","Meshes","Entries"})Directory.CreateDirectory(Folder+"/"+path);AssetDatabase.Refresh();
        random=new System.Random(1928);root=new GameObject("ROOM3 - Residual Archive").transform;
        bevel=BeveledCube();font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        concrete=Concrete("Board concrete",new Color(.65f,.68f,.66f));pale=Concrete("Pale concrete",new Color(.85f,.87f,.81f));dark=Concrete("Dark concrete",new Color(.25f,.29f,.29f));
        metal=Lit("Oxidised steel",new Color(.11f,.15f,.15f),.7f,.28f);
        green=Lit("Phosphor inlay",new Color(.25f,.4f,.23f),0,.2f,new Color(.65f,2.6f,.68f));
        white=Lit("Cold aperture",Color.white,0,.2f,new Color(3.1f,4.1f,3.9f));red=Lit("Access warning",new Color(.4f,.03f,.02f),0,.2f,new Color(2,.03f,.01f));
        glass=new Material(Shader.Find("Xiaoduan/Room3/Wired Glass"));string gp=Folder+"/Materials/Wired glass.mat";
        var previous=AssetDatabase.LoadAssetAtPath<Material>(gp);if(previous!=null){UnityEngine.Object.DestroyImmediate(glass);glass=previous;}else AssetDatabase.CreateAsset(glass,gp);
        Corridor();Bridge();FloatingArchitecture();DialAndCore();
        var environment=root.gameObject.AddComponent<Room3Environment>();
        var arrival=Group("ROOM3 arrival ENTER");arrival.position=V(0,.15f,-21);arrival.gameObject.AddComponent<TransitionDestination>().destinationTag=TransitionDestination.DestinationTag.ENTER;environment.arrival=arrival;
        var checkpoint=Group("Core checkpoint");checkpoint.position=V(0,.15f,36);environment.checkpoint=checkpoint;
        environment.dial=root.Find("04 - The dial and archive core/The suspended dial - decorative");
        SetupRenderer(environment);SetupVolume();
        RenderSettings.skybox=null;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.23f,.31f,.32f);RenderSettings.ambientEquatorColor=new Color(.13f,.18f,.18f);RenderSettings.ambientGroundColor=new Color(.07f,.09f,.085f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=new Color(.095f,.14f,.15f);RenderSettings.fogDensity=.0055f;
        var sun=Group("Cold backlight").gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.72f,.85f,.84f);sun.intensity=2.1f;sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(27,172,0);RenderSettings.sun=sun;
        var fill=Group("Soft frontal fill").gameObject.AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.42f,.58f,.55f);fill.intensity=.45f;fill.shadows=LightShadows.None;fill.transform.rotation=Quaternion.Euler(38,-25,0);
        Rig(scene);
        var main=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c=>c.CompareTag("MainCamera"));if(main!=null){main.GetUniversalAdditionalCameraData().SetRenderer(environment.rendererIndex);main.farClipPlane=230;main.backgroundColor=new Color(.025f,.039f,.043f);main.clearFlags=CameraClearFlags.SolidColor;}
        var build=EditorBuildSettings.scenes.ToList();if(!build.Any(s=>s.path==ScenePath))build.Add(new EditorBuildSettingsScene(ScenePath,true));else build.First(s=>s.path==ScenePath).enabled=true;EditorBuildSettings.scenes=build.ToArray();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        SceneView.lastActiveSceneView?.LookAt(V(0,7,35),Quaternion.Euler(10,0,0),45);
        File.WriteAllText("Library/Room3BuildReport.json",JsonUtility.ToJson(new BuildReport{scene=ScenePath,backup=backup,renderers=root.GetComponentsInChildren<Renderer>().Length,movingGroups=root.GetComponentsInChildren<Room3Motion>().Length,rendererIndex=environment.rendererIndex,entries=3},true));
        Debug.Log("ROOM3 built: isolation corridor, suspended causeway, floating galleries, monumental dial, archive terminals.");
    }
    [Serializable]class BuildReport{public string scene,backup;public int renderers,movingGroups,rendererIndex,entries;}
    [MenuItem("Tools/Xiaoduan/ROOM3/Preview ROOM3 on next Play")]
    public static void Preview(){EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);}
}
#endif
