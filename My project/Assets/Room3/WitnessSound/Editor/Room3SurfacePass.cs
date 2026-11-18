#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Xiaoduan.Room3;
public static class Room3SurfacePass
{
    const string F="Assets/Room3/WitnessSound/";
    static Mesh bevel;
    static Transform root;
    static Material Mat(string name,Color c,float metal,float smooth,Texture2D normal=null)
    {string p=F+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",c);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);if(normal!=null){m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.32f);m.EnableKeyword("_NORMALMAP");}EditorUtility.SetDirty(m);return m;}
    static GameObject Box(string name,Vector3 p,Vector3 s,Material m,Transform parent=null)
    {var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent??root,false);g.transform.localPosition=p;g.transform.localScale=s;g.GetComponent<MeshFilter>().sharedMesh=bevel;g.GetComponent<Renderer>().sharedMaterial=m;return g;}
    public static string Apply()
    {
        var f=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();if(EditorApplication.isPlaying)throw new Exception("Stop Play");
        bevel=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Room3/Meshes/ChamferedBlock.asset");var old=GameObject.Find("ROOM3 contact and construction detail");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
        root=new GameObject("ROOM3 contact and construction detail").transform;
        var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/LeartesStudios/HDRP/Art/Textures/T_Rust_N.png");
        var steel=Mat("Rolled weathered gate steel",new Color(.105f,.125f,.119f),.57f,.22f,normal);var edge=Mat("Exposed steel edges",new Color(.18f,.20f,.185f),.63f,.3f,normal);var dark=Mat("Expansion joints and recesses",new Color(.014f,.018f,.016f),.05f,.08f);
        var deck=Mat("Worn testimony steel deck",new Color(.095f,.112f,.103f),.39f,.18f,normal);deck.SetTextureScale("_BumpMap",new Vector2(30,35));deck.SetFloat("_BumpScale",.065f);
        var concretePath=F+"Materials/Cast testimony concrete.mat";var board=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Board concrete.mat");var concrete=AssetDatabase.LoadAssetAtPath<Material>(concretePath);if(concrete==null){concrete=new Material(board){name="Cast testimony concrete"};AssetDatabase.CreateAsset(concrete,concretePath);}concrete.SetFloat("_NormalStrength",.48f);concrete.SetFloat("_TextureScale",.38f);concrete.SetFloat("_Pathology",0);EditorUtility.SetDirty(concrete);
        var all=f.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach(var t in all)if(t.name=="Testimony platform")t.GetComponent<Renderer>().sharedMaterial=concrete;
        foreach(var leaf in f.gateLeaves)
        {
            leaf.GetComponent<Renderer>().sharedMaterial=steel;var previous=leaf.Find("Engineered moving gate");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var t=new GameObject("Engineered moving gate").transform;t.position=leaf.position;t.SetParent(leaf,true);t.localScale=new Vector3(1/leaf.lossyScale.x,1/leaf.lossyScale.y,1/leaf.lossyScale.z);
            for(int row=0;row<3;row++)
            {
                float y=-1.98f+row*1.98f;Box("Pressed metal cassette",new Vector3(0,y,-.21f),new Vector3(2.25f,1.76f,.095f),steel,t);
                Box("Inset panel seam",new Vector3(0,y+.89f,-.225f),new Vector3(2.32f,.024f,.02f),dark,t);
                for(int side=-1;side<=1;side+=2)for(int top=-1;top<=1;top+=2)
                    Box("Recessed fastener",new Vector3(side*1.03f,y+top*.75f,-.265f),new Vector3(.031f,.031f,.014f),edge,t);
            }
            foreach(float side in new[]{-1.22f,1.22f})Box("Structural stile",new Vector3(side,0,-.22f),new Vector3(.085f,5.9f,.11f),edge,t);
            foreach(float height in new[]{-2.88f,2.88f})Box("Structural cross rail",new Vector3(0,height,-.22f),new Vector3(2.5f,.1f,.12f),edge,t);
        }
        for(int z=31;z<=43;z+=2)
            Box("Deck expansion joint",new Vector3(0,.004f,z),new Vector3(10.6f,.006f,.011f),dark);
        foreach(float x in new[]{-3.5f,0,3.5f})Box("Long deck joint",new Vector3(x,.004f,37.5f),new Vector3(.011f,.006f,13.7f),dark);
        foreach(float side in new[]{-1f,1f})
        {
            Box("Recessed peripheral drain",new Vector3(side*4.8f,.005f,38),new Vector3(.24f,.009f,10),dark);
            for(int k=0;k<46;k++)Box("Drain grate bar",new Vector3(side*4.8f,.012f,33.1f+k*.215f),new Vector3(.25f,.012f,.018f),edge);
        }
        for(int i=0;i<2;i++)
        {
            var t=new GameObject("Testimony soft side light "+i).transform;t.SetParent(root,false);t.position=new Vector3(i==0?-4.2f:4.2f,3.6f,37.9f);t.LookAt(new Vector3(i==0?-.8f:.8f,1,40.9f));var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.68f,.8f,.72f);l.intensity=i==0?48f:31f;l.range=10;l.spotAngle=72;l.innerSpotAngle=35;l.shadows=LightShadows.Soft;l.shadowStrength=.65f;
        }
        EditorSceneManager.MarkSceneDirty(f.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(f.gameObject.scene);return "Oath gate metal cassettes, fasteners, floor joints, drainage and restrained side light";
    }
}
#endif
