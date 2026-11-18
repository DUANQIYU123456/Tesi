#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;
using Xiaoduan.Room3;

public static class Room3HybridSetup
{
    const string Folder="Assets/Room3/Embodied";
    static Transform root;
    static Material steel,concrete,green,red,pulse,white,pale;
    static Room3EmbodiedExpansion x;
    static Vector3 V(float a,float b,float c)=>new Vector3(a,b,c);
    static Transform Group(string name,Transform parent=null)
    {var g=new GameObject(name).transform;g.SetParent(parent??root,false);return g;}
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null,bool solid=false)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent??root,false);g.transform.localPosition=p;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=mat;if(!solid)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Link(string name,Vector3 a,Vector3 b,float width,Material mat,Transform parent=null)
    {var g=Box(name,(a+b)*.5f,V(width,width,Vector3.Distance(a,b)),mat,parent);g.transform.localRotation=Quaternion.LookRotation(b-a);}
    static Material Lit(string name,Color color,float emission=0)
    {
        string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",.72f);m.SetFloat("_Smoothness",.31f);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}return m;
    }
    static Mesh RingMesh(string name,float radius,float width,int count=96)
    {
        string path=Folder+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old!=null)return old;
        var m=new Mesh{name=name};var v=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        for(int i=0;i<=count;i++){float a=i*Mathf.PI*2/count;for(int j=0;j<2;j++){float r=radius+(j==0?-width:width)*.5f;v.Add(V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0));uv.Add(new Vector2(j,i/(float)count));}if(i<count){int k=i*2;tri.AddRange(new[]{k,k+1,k+2,k+1,k+3,k+2});}}
        m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);return m;
    }
    static GameObject MeshObject(string name,Mesh mesh,Material mat,Transform parent=null)
    {var g=Group(name,parent).gameObject;g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return g;}
    static void Ring(string name,Vector3 p,float radius,float width,Material mat,Transform parent=null)
    {var g=MeshObject(name,RingMesh(name.Replace(' ','_')+radius.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),radius,width),mat,parent);g.transform.localPosition=p;}
    static TextMeshPro Text(string name,string text,Vector3 p,float size,Transform parent=null,float width=6)
    {var g=Group(name,parent);g.localPosition=p;var t=g.gameObject.AddComponent<TextMeshPro>();t.font=x.flow.ui.archiveBody.font;t.text=text;t.fontSize=size;t.alignment=TextAlignmentOptions.Center;t.color=new Color(.7f,.8f,.74f);t.rectTransform.sizeDelta=new Vector2(width,1);return t;}
    static Light Light(string name,Vector3 p,Color color,float intensity,float range,Transform parent=null)
    {var g=Group(name,parent);g.localPosition=p;var l=g.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;return l;}
    static ParticleSystem Particles(string name,Vector3 p,Material mat,bool steam=false)
    {
        var g=Group(name);g.localPosition=p;var ps=g.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var m=ps.main;m.playOnAwake=false;m.loop=false;m.duration=1.5f;m.startLifetime=steam?.65f:1.2f;m.startSpeed=steam?2:1.1f;m.startSize=steam?.09f:.04f;m.maxParticles=180;m.startColor=new Color(.65f,.85f,.7f,.6f);m.gravityModifier=steam?-.08f:.14f;m.simulationSpace=ParticleSystemSimulationSpace.World;
        var emission=ps.emission;emission.rateOverTime=0;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=steam?.08f:2;
        var color=ps.colorOverLifetime;color.enabled=true;var gr=new Gradient();gr.SetKeys(new[]{new GradientColorKey(new Color(.7f,.9f,.72f),0),new GradientColorKey(new Color(.2f,.4f,.25f),1)},new[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(0,1)});color.color=gr;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;return ps;
    }
    static AudioSource Audio(string name)
    {var s=Group(name).gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.priority=110;s.spatialBlend=0;return s;}
    static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/"+name+".wav");
    static void Entrance()
    {
        var g=Group("01 - Manual pressure threshold");
        Box("Pressure header",V(0,5.2f,-18.7f),V(8.2f,.6f,1),steel,g,true);
        foreach(int s in new[]{-1,1})
        {
            Box("Pressure door rail",V(s*3.7f,2.5f,-18.7f),V(.5f,5,1),steel,g,true);
            Box("Pressure track",V(s*3.36f,2.5f,-19.21f),V(.08f,4.8f,.06f),green,g);
        }
        var door=Group("Heavy pneumatic blast leaf",g);door.localPosition=V(0,2.4f,-18.7f);x.door=door;
        Box("Riveted leaf",Vector3.zero,V(6.8f,4.8f,.4f),steel,door);
        for(int i=0;i<5;i++)Box("Reinforcing rib",V(0,-1.85f+i*.9f,-.26f),V(6.5f,.13f,.13f),steel,door);
        foreach(int side in new[]{-1,1})for(int i=0;i<8;i++)Box("Bolt",V(side*3.1f,-2+i*.55f,-.25f),Vector3.one*.08f,white,door);
        var barrier=Box("Sealed threshold collision",V(0,2.5f,-18.7f),V(7.2f,5,.5f),steel,g,true);barrier.GetComponent<Renderer>().enabled=false;x.doorBarrier=barrier.GetComponent<Collider>();
        Box("Valve mounting console",V(-2.15f,1.25f,-20.05f),V(.95f,1.8f,.32f),steel,g,true);
        var wheel=Group("Hand driven pressure wheel",g);wheel.localPosition=V(-2.15f,1.5f,-20.27f);x.valve=wheel;
        Ring("Pressure wheel",Vector3.zero,.43f,.075f,red,wheel);Ring("Wheel inner ring",Vector3.zero,.34f,.018f,steel,wheel);
        for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;Link("Wheel spoke",V(0,0,-.015f),V(Mathf.Cos(a)*.4f,Mathf.Sin(a)*.4f,-.015f),.035f,red,wheel);}
        Box("Wheel hub",V(0,0,-.02f),V(.13f,.13f,.12f),steel,wheel);
        x.pressureReadout=Text("Pressure readout","MANUAL OVERRIDE\n00% / PRESSURE",V(-2.15f,2.6f,-20.3f),.13f,g,2);
        Text("Sealed sector sign","03 / BLACK BOX\nACCESS DENIED",V(.8f,3.3f,-18.96f),.28f,g,4);
        var lens=Box("Pressure warning lens",V(0,4.6f,-19),V(.42f,.16f,.08f),red,g);x.warningLens=lens.GetComponent<Renderer>();
        x.warningLight=Light("Red pressure beacon",V(0,4.3f,-19.6f),new Color(1,.04f,.015f),2,7,g);x.warningRed=red;x.warningGreen=green;
        x.steam=new[]{Particles("Left pressure steam",V(-3,4.1f,-19.3f),pale,true),Particles("Right pressure steam",V(3,4.1f,-19.3f),pale,true)};
    }
    static void Sonar()
    {
        var g=Group("02 - Hidden extraction network");
        var ring=MeshObject("Player radial isotope wave",RingMesh("Sonar_ground_wave",1,.012f,160),green);ring.transform.localEulerAngles=V(90,0,0);x.sonarRing=ring.transform;
        var endpoint=new List<Vector3>();
        foreach(var w in x.flow.workerFigures)endpoint.Add(w.transform.position+V(0,1,0));
        foreach(var module in x.flow.bridgeModules)endpoint.Add(module.position);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<5;i++)endpoint.Add(V(s*(7+i*1.9f),5+i*2.7f,8+i*6.1f));
        int k=0;
        foreach(var p in endpoint)
        {
            var top=p+V((k%3-1)*2.1f,22+(k%4)*4,-1.5f);
            Link("Extraction filament "+k,top,p+V(.15f,.2f,0),.025f,pulse,g);
            Link("Paired filament "+k,top+V(.11f,0,.1f),p+V(-.16f,.18f,.1f),.009f,pulse,g);
            if(k>=3){Ring("Binding loop",p,.85f,.018f,pulse,g);}
            k++;
        }
        for(int i=0;i<4;i++)
        {
            var source=x.flow.workerFigures[i%3];var ghost=UnityEngine.Object.Instantiate(source,g);ghost.name="Suspended labor residue "+(i+1);ghost.SetActive(true);
            ghost.transform.position=V(i%2==0?-8:9,3+i*.7f,10+i*7);ghost.transform.localEulerAngles=V(0,i%2==0?25:-35,(i%2==0?1:-1)*13);
            foreach(var r in ghost.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=pulse;
            foreach(var t in ghost.GetComponentsInChildren<TMP_Text>(true))UnityEngine.Object.DestroyImmediate(t.gameObject);
            Link("Suspended worker extraction tether",ghost.transform.position+V(0,30,0),ghost.transform.position+V(0,1.8f,0),.028f,pulse,g);
        }
        Text("Sonar instruction","HOLD RMB / ISOTOPE PULSE\nExpose the structures that keep labor suspended.",V(0,2.9f,6.8f),.17f,null,6);
    }
    static void Fragments()
    {
        x.fragments=new Room3MemoryFragment[2];
        for(int i=0;i<2;i++)
        {
            var g=Group(i==0?"03A - Suspended dial memory":"03B - Suspended compute memory");g.localPosition=V(i==0?-1.5f:1.5f,1.45f,12+i*9);
            var fragment=g.gameObject.AddComponent<Room3MemoryFragment>();x.fragments[i]=fragment;fragment.title=i==0?"1920s / THE PAINTED DIAL":"2026 / COMPUTE IS ALSO MATERIAL LABOR";
            fragment.retainedText=i==0?"RETAINED / A REPEATED HAND MOVEMENT BEHIND THE LUMINOUS SURFACE":"RETAINED / LABELING, CHECKING, FILTERING: PEOPLE BEHIND THE OUTPUT";
            var visual=Group("Weightless artifact",g);fragment.visual=visual;fragment.focus=visual;
            if(i==0)
            {
                Ring("Watch rim",Vector3.zero,.31f,.035f,steel,visual);Ring("Phosphor chapter ring",V(0,0,-.01f),.267f,.007f,green,visual);
                var disk=GameObject.CreatePrimitive(PrimitiveType.Cylinder);disk.name="Round painted dial plate";disk.transform.SetParent(visual,false);disk.transform.localPosition=V(0,0,.055f);disk.transform.localEulerAngles=V(90,0,0);disk.transform.localScale=V(.57f,.025f,.57f);disk.GetComponent<Renderer>().sharedMaterial=steel;UnityEngine.Object.DestroyImmediate(disk.GetComponent<Collider>());
                for(int j=0;j<12;j++){float a=j*Mathf.PI/6;var t=Box("Dial index",V(Mathf.Sin(a)*.235f,Mathf.Cos(a)*.235f,-.04f),V(.014f,.045f,.012f),green,visual);t.transform.localEulerAngles=V(0,0,-j*30);}
                Link("Minute hand",V(0,0,-.05f),V(.12f,.16f,-.05f),.012f,white,visual);Link("Hour hand",V(0,0,-.06f),V(-.1f,.04f,-.06f),.019f,green,visual);
            }
            else
            {
                Box("Computing card substrate",Vector3.zero,V(.76f,.4f,.08f),steel,visual);
                foreach(float s in new[]{-.19f,.19f}){Ring("Cooling fan",V(s,0,-.06f),.148f,.018f,steel,visual);for(int n=0;n<7;n++){float a=n*Mathf.PI*2/7;Link("Fan blade",V(s,0,-.065f),V(s+Mathf.Sin(a)*.12f,Mathf.Cos(a)*.12f,-.065f),.022f,white,visual);}}
                for(int n=0;n<12;n++)Box("Compute connector",V(-.31f+n*.05f,-.223f,0),V(.025f,.07f,.03f),green,visual);
            }
            Text("Memory period",i==0?"1920s / HANDWORK":"2026 / HIDDEN WORK",V(0,-.55f,0),.09f,g,2.5f);
        }
    }
    static void Climax()
    {
        x.chestLights=new Light[x.flow.boneLights.Length];
        for(int i=0;i<x.chestLights.Length;i++)x.chestLights[i]=Light("Testimony chest light",V(0,1.25f,-.2f),new Color(.25f,1,.4f),0,4,x.flow.boneLights[i].transform.parent);
        x.finalBridgeVisuals=UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene==x.gameObject.scene&&(r.name=="Narrow core beam"||r.name=="Beam inset")).ToArray();
        x.impactDust=Particles("Concrete impact dust",V(0,0,17),pale);
        x.oathBurst=Particles("Five witnesses phosphor release",V(0,8.9f,41.5f),green);var sh=x.oathBurst.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=V(9,.5f,.5f);var em=x.oathBurst.emission;em.SetBursts(new[]{new ParticleSystem.Burst(0,90)});
        var a=Group("05 - Memorial aurora preview");var ribbons=new List<Renderer>();
        for(int i=0;i<7;i++)
        {
            var r=Box("Aurora veil",V(-9+i*3,16,105+Mathf.Sin(i)*4),V(.08f,26,2.5f),green,a);r.transform.localEulerAngles=V(0,-30+i*9,-7+i*2);ribbons.Add(r.GetComponent<Renderer>());
        }
        x.aurora=ribbons.ToArray();
        var ui=x.flow.ui;var mask=new GameObject("Memorial names window",typeof(RectTransform),typeof(UnityEngine.UI.RectMask2D));mask.transform.SetParent(ui.endingPanel.transform,false);var rect=(RectTransform)mask.transform;rect.sizeDelta=new Vector2(700,510);rect.anchoredPosition=new Vector2(0,60);
        ui.endingBody.transform.SetParent(mask.transform,false);ui.endingBody.rectTransform.sizeDelta=new Vector2(660,750);ui.endingBody.rectTransform.anchoredPosition=Vector2.zero;ui.endingBody.fontSize=27;
    }
    public static string Install()
    {
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.name!="ROOM3"||Application.isPlaying)throw new Exception("Open ROOM3 in edit mode first.");
        if(GameObject.Find("ROOM3 - Embodied archive additions")!=null)throw new Exception("Already installed. Edit the existing expansion instead.");
        Directory.CreateDirectory(Folder+"/Materials");Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();
        root=new GameObject("ROOM3 - Embodied archive additions").transform;
        x=root.gameObject.AddComponent<Room3EmbodiedExpansion>();x.flow=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();x.flow.embodied=x;
        steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Oxidised steel.mat");concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Board concrete.mat");
        red=Lit("Override red",new Color(.65f,.025f,.012f),.8f);white=Lit("Dust and steel highlights",new Color(.48f,.6f,.53f),.25f);
        pulse=new Material(Shader.Find("Xiaoduan/Room3/Isotope Pulse"));pulse.SetColor("_Tint",new Color(.13f,1.5f,.36f));AssetDatabase.CreateAsset(pulse,Folder+"/Materials/Sonar revealed residue.mat");
        green=new Material(pulse);green.SetFloat("_Always",1);green.SetColor("_Tint",new Color(.17f,1.1f,.36f));AssetDatabase.CreateAsset(green,Folder+"/Materials/Phosphor additive.mat");
        pale=new Material(green);pale.SetColor("_Tint",new Color(.42f,.48f,.45f));pale.SetFloat("_Opacity",.33f);AssetDatabase.CreateAsset(pale,Folder+"/Materials/Pale vapor.mat");
        Entrance();Sonar();Fragments();Climax();
        x.mechanical=Audio("Pressure lock sound");x.sonarAudio=Audio("Isotope sonar sound");x.memoryAudio=Audio("Cross-era memory sound");x.endingAudio=Audio("World preview sound");
        x.valveStroke=Clip("ValveStroke");x.pressureRelease=Clip("PressureRelease");x.pulseSound=Clip("IsotopePing");x.memoryMix=Clip("MemoryMix");x.formatHum=Clip("FormatHum");x.preserveChord=Clip("PreserveChord");x.finalTick=Clip("FinalTick");
        EditorUtility.SetDirty(x.flow);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Room3HybridPolish.Apply();return "Installed physical threshold, radial sonar, two memories, oath feedback and ending previews; original route retained.";
    }
}
#endif
