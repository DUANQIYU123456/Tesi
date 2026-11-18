#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Xiaoduan.Room3;

public static class Room3ArtPass
{
    const string F="Assets/Room3/ArtDirection/";
    static Transform root;
    static Room3EmbodiedExpansion x;
    static Material steel,black,brass,pcb,phosphor,ivory,red;
    static Mesh bevel;
    static Vector3 V(float a,float b,float c)=>new Vector3(a,b,c);
    static Transform Group(string name,Transform parent=null){var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
    static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles,List<Vector2> uv)
    {var m=new Mesh{name=name};m.SetVertices(vertices);m.SetTriangles(triangles,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,F+"Meshes/"+name+".asset");return m;}
    static GameObject MeshObject(string name,Mesh m,Material mat,Transform parent)
    {var t=Group(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=m;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;return t.gameObject;}
    static GameObject Box(string name,Vector3 p,Vector3 size,Material m,Transform parent)
    {var g=MeshObject(name,bevel,m,parent);g.transform.localPosition=p;g.transform.localScale=size;return g;}
    static GameObject Cylinder(string name,Vector3 p,float radius,float depth,Material mat,Transform parent)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localRotation=Quaternion.Euler(90,0,0);g.transform.localScale=V(radius*2,depth*.5f,radius*2);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;}
    static void Rod(string name,Vector3 a,Vector3 b,float radius,Material mat,Transform parent)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=(a+b)*.5f;g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);g.transform.localScale=V(radius*2,Vector3.Distance(a,b)*.5f,radius*2);g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    static Material Mat(string name,Color color,float metallic,float smooth,float emission=0)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smooth);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}AssetDatabase.CreateAsset(m,F+"Materials/"+name+".mat");return m;
    }
    static Mesh Torus(string name,float radius,float tube,int sections=72,int sides=10)
    {
        var vs=new List<Vector3>();var ts=new List<int>();var uv=new List<Vector2>();
        for(int i=0;i<=sections;i++)for(int j=0;j<=sides;j++)
        {float a=i*Mathf.PI*2/sections,b=j*Mathf.PI*2/sides;float r=radius+tube*Mathf.Cos(b);vs.Add(V(r*Mathf.Cos(a),r*Mathf.Sin(a),tube*Mathf.Sin(b)));uv.Add(new Vector2(i/(float)sections,j/(float)sides));if(i<sections&&j<sides){int k=i*(sides+1)+j;ts.AddRange(new[]{k,k+sides+1,k+1,k+1,k+sides+1,k+sides+2});}}
        return SaveMesh(name,vs,ts,uv);
    }
    static void Ring(string name,Mesh mesh,Vector3 p,Material mat,Transform parent)
    {var g=MeshObject(name,mesh,mat,parent);g.transform.localPosition=p;}
    static TextMeshPro Label(string name,string text,Vector3 p,float size,Transform parent,Color color)
    {var t=Group(name,parent);t.localPosition=p;var label=t.gameObject.AddComponent<TextMeshPro>();label.font=x.flow.ui.archiveBody.font;label.text=text;label.fontSize=size;label.alignment=TextAlignmentOptions.Center;label.color=color;label.rectTransform.sizeDelta=new Vector2(2,.25f);return label;}
    static void HideChildren(Transform t){foreach(Transform c in t)c.gameObject.SetActive(false);}
    static void Materials()
    {
        steel=Mat("Brushed graphite alloy",new Color(.2f,.23f,.24f),.64f,.35f);
        black=Mat("Worn enamel and fan shroud",new Color(.025f,.032f,.033f),.2f,.22f);
        brass=Mat("Aged brass contacts",new Color(.32f,.235f,.11f),.72f,.38f);
        pcb=Mat("Exposed circuit substrate",new Color(.025f,.075f,.055f),.1f,.2f);
        phosphor=Mat("Aged radium paint",new Color(.27f,.55f,.3f),.04f,.22f,.7f);
        ivory=Mat("Printed enamel markings",new Color(.67f,.68f,.59f),.08f,.2f);
        red=Mat("Worn pressure wheel paint",new Color(.28f,.037f,.019f),.24f,.24f);
        var tex="Assets/LeartesStudios/HDRP/Art/Textures/";
        AssetDatabase.CopyAsset(tex+"T_Wall_Concrete_N.png",F+"Textures/Concrete normal.png");
        AssetDatabase.CopyAsset(tex+"T_Wall_Concrete_ORM.png",F+"Textures/Concrete ORM.png");
        var normal=(TextureImporter)AssetImporter.GetAtPath(F+"Textures/Concrete normal.png");normal.textureType=TextureImporterType.NormalMap;normal.sRGBTexture=false;normal.SaveAndReimport();
        var orm=(TextureImporter)AssetImporter.GetAtPath(F+"Textures/Concrete ORM.png");orm.sRGBTexture=false;orm.SaveAndReimport();
        foreach(var id in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Room3"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
            if(m.shader.name=="Xiaoduan/Room3/World Concrete")
            {m.SetTexture("_NormalMap",AssetDatabase.LoadAssetAtPath<Texture2D>(F+"Textures/Concrete normal.png"));m.SetTexture("_ORMMap",AssetDatabase.LoadAssetAtPath<Texture2D>(F+"Textures/Concrete ORM.png"));m.SetFloat("_NormalStrength",.72f);m.SetFloat("_TextureScale",.32f);EditorUtility.SetDirty(m);}
        }
        var line=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Embodied/Materials/Sonar revealed residue.mat");line.SetColor("_Tint",new Color(.08f,.78f,.27f));line.SetFloat("_Opacity",.75f);EditorUtility.SetDirty(line);
        var ghost=new Material(line){name="Sonar silhouette rim"};ghost.SetFloat("_SurfaceMode",1);AssetDatabase.CreateAsset(ghost,F+"Materials/Sonar silhouette rim.mat");
        foreach(var r in x.GetComponentsInChildren<Renderer>(true))if(r.transform.name=="Work dress"||r.transform.name=="Bowed head"||r.transform.parent.name.StartsWith("Suspended labor residue"))r.sharedMaterial=ghost;
        foreach(var ps in x.steam.Concat(new[]{x.impactDust,x.oathBurst}))
        {
            var renderer=ps.GetComponent<Renderer>();var m=new Material(renderer.sharedMaterial){name=ps.name+" - soft particles"};m.SetFloat("_SurfaceMode",2);AssetDatabase.CreateAsset(m,F+"Materials/"+m.name+".mat");renderer.sharedMaterial=m;
        }
        var route=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Phosphor inlay.mat");route.SetColor("_EmissionColor",new Color(.12f,.73f,.27f));EditorUtility.SetDirty(route);
    }
    static int Composition()
    {
        var scene=x.gameObject.scene;var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var debris=all.Where(t=>t.name=="Drifting concrete fragment").OrderBy(t=>t.GetSiblingIndex()).ToArray();int visible=0;
        for(int i=0;i<debris.Length;i++)
        {
            bool keep=i%10<3;debris[i].gameObject.SetActive(keep);if(!keep)continue;
            int cluster=visible/3;int side=cluster%2==0?-1:1;debris[i].position=V(side*(13+cluster%3*4)+(visible%3-1)*1.2f,-6+cluster%3*8+(visible%3)*1.3f,10+cluster*12+(visible%3)*1.8f);
            debris[i].localScale*=.65f+visible%3*.12f;visible++;
        }
        var galleries=all.Where(t=>t.name.StartsWith("Suspended gallery ")).ToArray();for(int i=0;i<galleries.Length;i++)galleries[i].gameObject.SetActive(i%3!=1);
        var stairs=all.Where(t=>t.name=="Suspended stair mass").ToArray();for(int i=0;i<stairs.Length;i++)if(i==1||i==3)stairs[i].gameObject.SetActive(false);
        foreach(var t in all)
        {
            if(t.name=="Memory period"||t.name=="Sonar instruction")t.gameObject.SetActive(false);
            if(t.name=="Binding loop")t.gameObject.SetActive(false);
        }
        return visible;
    }
    static void PressureWheel(Room3ArtDirection art)
    {
        HideChildren(x.valve);var t=Group("Cast iron pressure wheel",x.valve);
        Ring("Rounded rim",Torus("Pressure rim",.405f,.032f),Vector3.zero,red,t);
        Ring("Inner cast rim",Torus("Pressure inner rim",.36f,.012f),V(0,0,.012f),steel,t);
        for(int i=0;i<5;i++){float a=(i*72+18)*Mathf.Deg2Rad;Rod("Rounded spoke",V(0,0,.022f),V(Mathf.Cos(a)*.392f,Mathf.Sin(a)*.392f,0),.016f,red,t);}
        Cylinder("Hub flange",V(0,0,.018f),.085f,.06f,steel,t);Cylinder("Hex lock nut",V(0,0,-.025f),.035f,.028f,brass,t);
        Cylinder("Threaded spindle",V(0,0,.12f),.029f,.25f,steel,t);
        for(int i=0;i<10;i++){var a=i*Mathf.PI*2/10;Box("Paint chip",V(Mathf.Cos(a)*.408f,Mathf.Sin(a)*.408f,-.027f),V(.015f,.008f,.008f),steel,t);}
        var mount=x.valve.parent;var gauge=Group("Physical pressure gauge",mount);gauge.localPosition=V(-.45f,2.1f,-20.28f);
        Cylinder("Gauge housing",Vector3.zero,.16f,.09f,steel,gauge);Cylinder("Gauge enamel",V(0,0,-.05f),.14f,.008f,ivory,gauge);
        Ring("Gauge bezel",Torus("Gauge ring",.151f,.009f),V(0,0,-.058f),brass,gauge);
        for(int i=0;i<13;i++){float a=(-135+i*22.5f)*Mathf.Deg2Rad;var b=Box("Pressure tick",V(Mathf.Sin(a)*.113f,Mathf.Cos(a)*.113f,-.06f),V(.004f,.018f,.003f),black,gauge);b.transform.localEulerAngles=V(0,0,-a*Mathf.Rad2Deg);}
        var needle=Group("Pressure needle",gauge);needle.localPosition=V(0,0,-.07f);Box("Needle",V(0,.044f,0),V(.005f,.094f,.004f),red,needle);art.pressureNeedle=needle;
        x.pressureReadout.fontSize=.48f;x.pressureReadout.transform.localPosition=V(-1.4f,.57f,-20.3f);
        var sign=mount.Find("Sealed sector sign").GetComponent<TMP_Text>();sign.text="SECTOR 03\nPRESSURE LOCK";sign.fontSize=1.15f;
        var d=x.door;for(int i=0;i<10;i++)
        {
            var stripe=Box("Weathered hazard inlay",V(-2.8f+i*.55f,-2.12f,-.225f),V(.24f,.11f,.006f),i%2==0?brass:black,d);stripe.transform.localEulerAngles=V(0,0,-25);
        }
    }
    static void Dial()
    {
        var f=x.fragments[0];HideChildren(f.visual);var t=Group("Enamel dial and brass movement",f.visual);
        Cylinder("Stepped brass case",V(0,0,.04f),.285f,.075f,brass,t);Cylinder("Black enamel dial",V(0,0,-.005f),.259f,.013f,black,t);
        Ring("Outer machined bezel",Torus("Dial outer bezel",.275f,.014f),V(0,0,-.015f),brass,t);Ring("Inner dial chapter",Torus("Dial chapter rim",.247f,.003f),V(0,0,-.018f),ivory,t);
        for(int i=0;i<60;i++)
        {float a=i*Mathf.PI/30;var b=Box("Minute division",V(Mathf.Sin(a)*.234f,Mathf.Cos(a)*.234f,-.017f),V(i%5==0?.004f:.002f,i%5==0?.019f:.009f,.002f),i%5==0?phosphor:ivory,t);b.transform.localEulerAngles=V(0,0,-i*6);}
        for(int i=1;i<=12;i++){float a=i*Mathf.PI/6;var label=Label("Hand-painted hour "+i,i.ToString(),V(Mathf.Sin(a)*.199f,Mathf.Cos(a)*.199f,-.024f),.29f,t,new Color(.46f,.65f,.43f));label.fontStyle=FontStyles.Bold;}
        var minute=Box("Painted minute hand",V(.065f,.078f,-.03f),V(.009f,.21f,.003f),phosphor,t);minute.transform.localEulerAngles=V(0,0,-40);
        var hour=Box("Painted hour hand",V(-.055f,.025f,-.034f),V(.013f,.135f,.003f),ivory,t);hour.transform.localEulerAngles=V(0,0,65);
        Cylinder("Central pivot",V(0,0,-.036f),.013f,.006f,brass,t);
        var crown=Cylinder("Knurled winding crown",V(0,.322f,.028f),.028f,.065f,brass,t);crown.transform.localRotation=Quaternion.identity;
        Ring("Winding bow",Torus("Winding bow mesh",.049f,.007f),V(0,.362f,.027f),brass,t);
        for(int i=0;i<3;i++)Rod("Enamel hairline crack",V(-.12f+i*.014f,-.13f,-.019f),V(-.19f+i*.01f,-.03f,-.019f),.0007f,steel,t);
        f.title="1920s / The painted dial";f.retainedText="1920s / A repeated hand movement behind the luminous surface.";
    }
    static Mesh FanBlade()
    {
        var vs=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        for(int i=0;i<=8;i++)
        {float t=i/8f,r=.035f+t*.099f,a=t*.85f;for(int s=0;s<2;s++){float angle=a+(s==0?-.15f:.15f)*(.5f+t*.5f);vs.Add(V(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r,Mathf.Sin(t*Mathf.PI)*.01f));uv.Add(new Vector2(s,t));}if(i<8){int j=i*2;tri.AddRange(new[]{j,j+2,j+1,j+1,j+2,j+3,j+1,j+2,j,j+3,j+2,j+1});}}
        var m=SaveMesh("Curved cooling impeller blade",vs,tri,uv);m.normals=Enumerable.Repeat(Vector3.back,vs.Count).ToArray();EditorUtility.SetDirty(m);return m;
    }
    static void ComputeCard(Room3ArtDirection art)
    {
        var f=x.fragments[1];HideChildren(f.visual);var t=Group("Compute card with exposed material layers",f.visual);
        Box("Circuit board",V(0,0,.025f),V(.77f,.36f,.025f),pcb,t);Box("Machined backplate",V(0,.015f,.06f),V(.74f,.34f,.026f),steel,t);
        Box("Cooling shroud",V(0,.02f,-.017f),V(.735f,.32f,.065f),black,t);
        Box("I-O bracket",V(-.403f,-.025f,.02f),V(.025f,.4f,.12f),steel,t);
        var blade=FanBlade();var ring=Torus("Compute fan outer rim",.147f,.008f);var rotors=new List<Transform>();
        foreach(float xPos in new[]{-.19f,.19f})
        {
            Cylinder("Fan recess",V(xPos,.02f,-.053f),.139f,.012f,black,t);Ring("Fan machined ring",ring,V(xPos,.02f,-.062f),steel,t);
            var rotor=Group("Cooling impeller",t);rotor.localPosition=V(xPos,.02f,-.064f);rotors.Add(rotor);
            for(int k=0;k<11;k++){var b=MeshObject("Curved fan blade",blade,steel,rotor);b.transform.localEulerAngles=V(0,0,k*360f/11);}
            Cylinder("Fan bearing",Vector3.zero,.033f,.015f,black,rotor);Cylinder("Bearing screw",V(0,0,-.01f),.009f,.004f,brass,rotor);
        }
        for(int k=0;k<15;k++)Box("Gold edge contact",V(-.32f+k*.044f,-.194f,.024f),V(.025f,.055f,.006f),brass,t);
        for(int k=0;k<9;k++){Box("Heat sink fin",V(-.32f+k*.08f,.19f,.02f),V(.006f,.047f,.085f),steel,t);Box("Board component",V(-.30f+k*.075f,-.15f,-.002f),V(.025f,.012f,.009f),ivory,t);}
        for(int k=0;k<4;k++)Cylinder("Enclosure fastener",V(k%2==0?-.35f:.35f,k<2?-.11f:.15f,-.056f),.008f,.004f,brass,t);
        Box("Quiet activity slit",V(.32f,.006f,-.054f),V(.017f,.09f,.002f),phosphor,t);
        art.coolingRotors=rotors.ToArray();f.title="2026 / The work behind computation";f.retainedText="2026 / People label, check and filter behind the output.";
    }
    static Mesh Cable(string name,Vector3 a,Vector3 b,float radius,int seed)
    {
        var vs=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        for(int i=0;i<=28;i++)
        {
            float t=i/28f;var p=Vector3.Lerp(a,b,t)+V(Mathf.Sin(t*Mathf.PI)*(.7f+seed%3*.25f),0,Mathf.Sin(t*Mathf.PI*2+seed)*.19f*Mathf.Sin(t*Mathf.PI));
            for(int j=0;j<=5;j++){float z=j*Mathf.PI*2/5;vs.Add(p+V(Mathf.Cos(z)*radius,0,Mathf.Sin(z)*radius));uv.Add(new Vector2(j/5f,t));if(i<28&&j<5){int k=i*6+j;tri.AddRange(new[]{k,k+6,k+1,k+1,k+6,k+7});}}
        }
        return SaveMesh(name,vs,tri,uv);
    }
    static void Filaments()
    {
        var lines=x.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Extraction filament ")||t.name.StartsWith("Paired filament ")||t.name=="Suspended worker extraction tether").ToArray();
        int k=0;foreach(var line in lines)
        {
            var renderer=line.GetComponent<Renderer>();if(renderer==null)continue;
            var a=line.position-line.forward*line.lossyScale.z*.5f;var b=line.position+line.forward*line.lossyScale.z*.5f;
            var holder=Group("Curved extraction line "+k,root);var mesh=Cable("Extraction curve "+k,a,b,line.name.StartsWith("Paired")?.006f:.013f,k);MeshObject("Phosphor filament",mesh,renderer.sharedMaterial,holder);line.gameObject.SetActive(false);k++;
        }
    }
    static void Witnesses()
    {
        for(int i=0;i<x.flow.boneLights.Length;i++)
        {
            var witness=x.flow.boneLights[i].transform.parent;var head=witness.Find("Bowed head");head.localEulerAngles=V(8+i*3,(i-2)*7,(i%2==0?1:-1)*9);
            var old=witness.Cast<Transform>().Where(t=>t.name=="Structural link").ToArray();foreach(var arm in old)arm.gameObject.SetActive(false);
            var body=witness.Find("Work dress");body.localScale=V(1+(i-2)*.025f,1-(i%2)*.025f,1);
            foreach(int s in new[]{-1,1})
            {
                Vector3 shoulder=V(s*.27f,1.62f,0),elbow=V(s*.33f,1.07f,-.03f),hand=V(s*.28f,.7f,-.06f);
                if(i==0&&s==1){elbow=V(.31f,1.25f,-.12f);hand=V(.12f,1.83f,-.2f);}
                if(i==1&&s==-1){elbow=V(-.29f,1.14f,-.12f);hand=V(.13f,1.07f,-.19f);}
                if(i==3&&s==1){elbow=V(.32f,1.23f,-.14f);hand=V(.05f,1.39f,-.22f);}
                var mat=body.GetComponent<Renderer>().sharedMaterial;Rod("Upper sleeve",shoulder,elbow,.064f,mat,witness);Rod("Lower sleeve",elbow,hand,.043f,mat,witness);Cylinder("Lowered hand",hand,.043f,.08f,mat,witness);
            }
        }
    }
    static void LightAndReadability()
    {
        var profile=x.flow.volume.sharedProfile;profile.TryGet(out Bloom bloom);bloom.intensity.Override(.28f);bloom.threshold.Override(1.25f);bloom.scatter.Override(.58f);
        profile.TryGet(out ColorAdjustments color);color.postExposure.Override(.25f);color.contrast.Override(16);color.saturation.Override(-18);
        var fog=profile.components.FirstOrDefault(c=>c is VolumetricFogVolumeComponent) as VolumetricFogVolumeComponent;
        if(fog!=null){fog.density.value=.055f;fog.attenuationDistance.value=95;fog.tint.value=new Color(.57f,.67f,.68f);EditorUtility.SetDirty(fog);}
        RenderSettings.fogColor=new Color(.035f,.052f,.057f);RenderSettings.fogDensity=.0035f;
        var shaftMat=new Material(Shader.Find("Xiaoduan/Room3/Archive light shaft"));shaftMat.SetColor("_Tint",new Color(.68f,.8f,.73f));shaftMat.SetFloat("_Density",.13f);AssetDatabase.CreateAsset(shaftMat,F+"Materials/Distant pale shaft.mat");
        var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shaft.name="Pale archive axis";shaft.transform.SetParent(root,false);shaft.transform.position=V(0,25,112);shaft.transform.localScale=V(2.7f,32,2.7f);shaft.GetComponent<Renderer>().sharedMaterial=shaftMat;shaft.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());
        var lamp=Group("Testimony rim from above").gameObject.AddComponent<Light>();lamp.type=LightType.Spot;lamp.transform.position=V(-3,22,44);lamp.transform.rotation=Quaternion.Euler(76,-12,0);lamp.color=new Color(.74f,.81f,.73f);lamp.intensity=180;lamp.range=35;lamp.spotAngle=44;lamp.innerSpotAngle=25;lamp.shadows=LightShadows.Soft;
        var probe=Group("Local metal reflection").gameObject.AddComponent<ReflectionProbe>();probe.transform.position=V(0,4,18);probe.size=V(45,35,90);probe.boxProjection=true;probe.resolution=128;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
        x.flow.ui.noticeText.enableAutoSizing=true;x.flow.ui.noticeText.fontSizeMax=24;x.flow.ui.noticeText.fontSizeMin=18;x.flow.ui.noticeText.textWrappingMode=TextWrappingModes.Normal;x.flow.ui.noticeText.rectTransform.sizeDelta=new Vector2(900,58);
        x.flow.ui.promptCaption.fontSize=18;
        EditorUtility.SetDirty(profile);
    }
    public static string RefineAirborneLayers()
    {
        var e=UnityEngine.Object.FindFirstObjectByType<Room3EmbodiedExpansion>();
        var positions=new[]{V(-6,3.8f,11),V(6,5.2f,17),V(-5,4.1f,23)};
        for(int i=0;i<e.flow.bridgeModules.Length;i++)e.flow.bridgeModules[i].localPosition=positions[i];
        var material=AssetDatabase.LoadAssetAtPath<Material>(F+"Materials/Left pressure steam - soft particles.mat");
        foreach(var ps in UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
        {
            if(ps.name!="Suspended paper archive")continue;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var m=ps.main;m.startLifetime=6;m.startSize=new ParticleSystem.MinMaxCurve(.035f,.1f);m.maxParticles=24;m.startColor=new Color(.6f,.68f,.61f,.2f);
            var emission=ps.emission;emission.rateOverTime=.7f;ps.GetComponent<Renderer>().sharedMaterial=material;
        }
        EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);return "Kept bridge origins out of the central sightline and replaced paper confetti with sparse soft dust";
    }
    public static string FinalizeWitnessLighting()
    {
        var e=UnityEngine.Object.FindFirstObjectByType<Room3EmbodiedExpansion>();var art=GameObject.Find("ROOM3 - Art direction refinement").transform;
        var enamel=AssetDatabase.LoadAssetAtPath<Material>(F+"Materials/Worn enamel and fan shroud.mat");enamel.SetColor("_BaseColor",new Color(.06f,.07f,.075f));EditorUtility.SetDirty(enamel);
        for(int i=0;i<2;i++)
        {
            string name="Artifact grazing light "+i;var t=art.Find(name);if(t==null){t=new GameObject(name,typeof(Light)).transform;t.SetParent(art,false);}
            var target=e.fragments[i].focus.position;t.position=target+V(i==0?.5f:-.1f,1.05f,-2.4f);t.rotation=Quaternion.LookRotation(target-t.position);var l=t.GetComponent<Light>();l.type=LightType.Spot;l.intensity=i==0?9:15;l.range=6;l.spotAngle=38;l.innerSpotAngle=24;l.color=new Color(.8f,.86f,.78f);l.shadows=LightShadows.None;
        }
        foreach(var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))if(r.name=="Testimony backlight")r.enabled=false;
        if(art.Find("Soft testimony light volume")==null)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name="Soft testimony light volume";g.transform.SetParent(art,false);g.transform.position=V(0,13,49);g.transform.localScale=V(8.5f,9.5f,8.5f);g.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(F+"Materials/Distant pale shaft.mat");g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        }
        var shaft=AssetDatabase.LoadAssetAtPath<Mesh>(F+"Meshes/Open atmospheric shaft.asset");
        if(shaft==null)
        {
            var vs=new List<Vector3>();var ts=new List<int>();var uv=new List<Vector2>();
            for(int i=0;i<=72;i++){float a=i*Mathf.PI*2/72;for(int j=0;j<2;j++){vs.Add(V(Mathf.Cos(a)*.5f,j==0?-1:1,Mathf.Sin(a)*.5f));uv.Add(new Vector2(i/72f,j));}if(i<72){int k=i*2;ts.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
            shaft=SaveMesh("Open atmospheric shaft",vs,ts,uv);
        }
        art.Find("Soft testimony light volume").position=V(0,13,49);
        foreach(string name in new[]{"Soft testimony light volume","Pale archive axis"})art.Find(name).GetComponent<MeshFilter>().sharedMesh=shaft;
        EditorSceneManager.MarkSceneDirty(e.gameObject.scene);EditorSceneManager.SaveScene(e.gameObject.scene);AssetDatabase.SaveAssets();return "Added local artifact light and replaced the testimony display board with a soft light volume";
    }
    public static string Install()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ROOM3"||Application.isPlaying)throw new Exception("Open edit-mode ROOM3");if(GameObject.Find("ROOM3 - Art direction refinement")!=null)throw new Exception("Already installed");
        foreach(var folder in new[]{"Materials","Meshes","Textures"})Directory.CreateDirectory(F+folder);AssetDatabase.Refresh();
        root=new GameObject("ROOM3 - Art direction refinement").transform;x=UnityEngine.Object.FindFirstObjectByType<Room3EmbodiedExpansion>();var art=root.gameObject.AddComponent<Room3ArtDirection>();art.expansion=x;
        bevel=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Room3/Meshes/ChamferedBlock.asset");Materials();int debris=Composition();PressureWheel(art);Dial();ComputeCard(art);Filaments();Witnesses();LightAndReadability();
        RefineAirborneLayers();FinalizeWitnessLighting();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return "Art direction refined. Drifting fragments retained: "+debris+" / 60. Original route and collision retained.";
    }
}
#endif
