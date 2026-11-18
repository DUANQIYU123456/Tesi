#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Xiaoduan.Room3;
public static class RocketboxSetup
{
    const string F="Assets/Room3/Rocketbox/";
    static Room3StoryFlow flow;static Transform root;
    static string[] models={"Female_Adult_11","Female_Adult_15","Female_Adult_02"};
    static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
    static Material Mat(string name,Color col,float smooth=.24f)
    {var p=F+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",col);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);return m;}
    static Material Surface(string model,Material source)
    {
        var n=source.name;bool opacity=n.Contains("opacity"),head=n.Contains("head");
        var m=Mat(model+"_"+n,head?new Color(.62f,.64f,.59f):new Color(.44f,.47f,.45f),opacity?.1f:head?.28f:.16f);
        string prefix=n;var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(F+"Models/"+model+"/"+prefix+"_color.png");m.SetTexture("_BaseMap",tex);
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(F+"Models/"+model+"/"+prefix+"_normal.png");if(normal!=null){m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.55f);m.EnableKeyword("_NORMALMAP");}
        if(opacity){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.45f);m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_Cull",0);m.SetOverrideTag("RenderType","TransparentCutout");m.renderQueue=2450;}
        EditorUtility.SetDirty(m);return m;
    }
    static Transform Figure(string name,int model,Vector3 pos,float scale,int variation,bool suspended,bool work,Transform parent)
    {
        string key=models[model];var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(F+"Models/"+key+"/"+key+".fbx");
        var go=UnityEngine.Object.Instantiate(prefab,parent);go.name=name;var t=go.transform;t.localPosition=pos;t.localScale=Vector3.one*scale;t.localRotation=Quaternion.Euler(0,work?0:180+(variation-2)*3,0);
        foreach(var a in go.GetComponentsInChildren<Animator>())a.enabled=false;
        foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {bool high=r.name.Contains("hipoly");r.gameObject.SetActive(high);if(!high)continue;r.sharedMaterials=r.sharedMaterials.Select(m=>Surface(key,m)).ToArray();r.updateWhenOffscreen=true;r.quality=SkinQuality.Bone4;}
        var actor=go.AddComponent<RocketboxWitness>();actor.flow=flow;actor.joints=go.GetComponentsInChildren<Transform>(true).Where(b=>b.name.StartsWith("Bip01")).ToArray();actor.bind=actor.joints.Select(b=>b.localRotation).ToArray();actor.variation=variation;actor.suspended=suspended;actor.workStation=work;actor.anchor=t.localPosition;actor.orientation=t.localRotation;actor.Preview(0);return t;
    }
    static Transform Box(string n,Vector3 pos,Vector3 size,Material mat)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(root,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go.transform;}
    static Light Spot(string n,Vector3 pos,Vector3 target,Color col,float power,float angle,float range)
    {var t=Group(n,root);t.position=pos;t.rotation=Quaternion.LookRotation(target-pos);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=col;l.intensity=power;l.spotAngle=angle;l.innerSpotAngle=angle*.6f;l.range=range;l.shadows=LightShadows.Soft;l.shadowBias=.035f;l.shadowNormalBias=.2f;return l;}
    public static string ImportSettings()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{F+"Models"}))
        {var p=AssetDatabase.GUIDToAssetPath(guid);var ti=(TextureImporter)AssetImporter.GetAtPath(p);bool normal=p.Contains("normal");ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=!normal;ti.alphaSource=TextureImporterAlphaSource.FromInput;ti.alphaIsTransparency=p.Contains("opacity");ti.maxTextureSize=1024;ti.mipmapEnabled=true;ti.SaveAndReimport();}
        foreach(string key in models){var mi=(ModelImporter)AssetImporter.GetAtPath(F+"Models/"+key+"/"+key+".fbx");mi.isReadable=true;mi.importAnimation=false;mi.SaveAndReimport();}
        return "Three models configured; original bones retained; normal maps and alpha cutouts configured";
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play");flow=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();
        if(flow==null)throw new Exception("Open ROOM3");if(GameObject.Find("ROOM3 - Rocketbox suspended testimony")!=null)throw new Exception("Already applied");
        Directory.CreateDirectory(F+"Materials");AssetDatabase.Refresh();root=Group("ROOM3 - Rocketbox suspended testimony",null);
        var old=GameObject.Find("Anatomical witnesses");var oldWorkers=GameObject.Find("Anatomical archive workers");
        var oldGlows=flow.boneLights.ToArray();var glows=new List<GameObject>();var lights=new List<Light>();
        float[] px={-3.1f,-1.55f,0,1.65f,3.18f},pz={40.3f,40.75f,41.1f,40.7f,40.1f},py={.24f,.39f,.52f,.3f,.2f};
        for(int i=0;i<5;i++)
        {
            var actor=Figure("Witness "+(i+1)+" - Rocketbox symbolic testimony",i%3,new Vector3(px[i],py[i],pz[i]),new[]{1.1f,1.06f,1.08f,1.04f,1.1f}[i],i,true,false,root);
            var chest=actor.GetComponentsInChildren<Transform>().First(b=>b.name=="Bip01 Spine2");
            var g=UnityEngine.Object.Instantiate(oldGlows[i],chest,false);g.name="Testimony isotope traces";g.transform.localPosition=Vector3.zero;g.transform.localRotation=Quaternion.identity;g.transform.position=actor.TransformPoint(new Vector3(0,1.23f,.16f));g.transform.localScale=Vector3.one;g.SetActive(true);
            foreach(var line in g.GetComponentsInChildren<LineRenderer>(true))line.enabled=false;
            var light=g.GetComponent<Light>();if(light!=null){light.range=.65f;light.spotAngle=55;light.transform.rotation=Quaternion.LookRotation(actor.forward);lights.Add(light);}glows.Add(g);
        }
        var workRoot=Group("Rocketbox archive workers",null);var workers=new List<GameObject>();
        for(int i=0;i<3;i++)workers.Add(Figure("Archive worker "+i,i,new Vector3(10,.02f,-22.05f+i*6),1.04f,i,false,true,workRoot).gameObject);
        for(int i=0;i<6;i++)
        {
            var t=Figure("Suspended memory "+(i+1),i%3,new Vector3(i%2==0?-5.2f:5.2f,new[]{3.2f,4.7f,6.5f,3.7f,5.8f,7.6f}[i],new[]{37,38,43,44,47,46}[i]),1.01f,5+i,true,false,root);
            t.localRotation=Quaternion.Euler(i%2==0?8:-5,i%2==0?110:245,(i%3-1)*5);var a=t.GetComponent<RocketboxWitness>();a.orientation=t.localRotation;a.Preview(0);
        }
        old.SetActive(false);oldWorkers.SetActive(false);flow.workerFigures=workers.ToArray();flow.boneLights=glows.ToArray();flow.embodied.chestLights=lights.ToArray();
        foreach(var n in new[]{"Testimony soft side light 0","Testimony soft side light 1","Testimony reflected light","Testimony rim from above"}){var g=GameObject.Find(n);if(g!=null)g.GetComponent<Light>().enabled=false;}
        var concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/ArtDirection/Materials/Archive concrete.mat");if(concrete==null)concrete=Mat("Vault charcoal",new Color(.095f,.105f,.11f),.23f);
        var dark=Mat("Blackened steel ribs",new Color(.028f,.034f,.038f),.34f);
        var red=Mat("Muted red ceiling slit",new Color(.18f,.006f,.004f));red.EnableKeyword("_EMISSION");red.SetColor("_EmissionColor",new Color(2.1f,.025f,.012f));
        for(int side=-1;side<=1;side+=2)
        {
            Box("Archive pylon",new Vector3(side*5.9f,5.5f,43),new Vector3(1.2f,11,1.7f),concrete);
            for(int j=0;j<6;j++)Box("Pylon vertical flute",new Vector3(side*5.9f-.48f+j*.19f,7.3f,42.08f),new Vector3(.055f,7.2f,.12f),dark);
            Box("Ceiling perimeter",new Vector3(side*6,11,41.5f),new Vector3(.4f,.5f,12),dark);
            Spot("Crimson upper rim "+side,new Vector3(side*3.5f,9,46),new Vector3(side*2,3,40),new Color(1,.055f,.022f),100,68,17);
        }
        for(int j=0;j<7;j++)Box("Ceiling light slot",new Vector3(0,11.05f,36.5f+j*1.4f),new Vector3(11.6f,.07f,.075f),red);
        Box("Vault rear lintel",new Vector3(0,10.7f,47),new Vector3(13.2f,.85f,1.1f),concrete);
        Spot("Cold portrait key",new Vector3(-3.5f,5.6f,34.8f),new Vector3(0,1.7f,41),new Color(.73f,.82f,.85f),85,65,18);
        Spot("Quiet portrait fill",new Vector3(4.5f,3.6f,36),new Vector3(0,1.6f,41),new Color(.48f,.63f,.59f),33,65,13);
        Spot("Upper silhouettes cool rim",new Vector3(0,10,34),new Vector3(0,5,44),new Color(.45f,.57f,.6f),125,85,23);
        EditorUtility.SetDirty(flow);EditorUtility.SetDirty(flow.embodied);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);EditorSceneManager.SaveScene(flow.gameObject.scene);
        return "Five witnesses, three archive workers, six suspended memories; new local lighting and fluted vault; story connections retained";
    }
}
#endif
