#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Xiaoduan.Room3;
public static class Room3HybridPolish
{
    const string F="Assets/Room3/Embodied/";
    public static string Apply()
    {
        var x=Object.FindFirstObjectByType<Room3EmbodiedExpansion>();if(x==null||Application.isPlaying)return "Open edit-mode ROOM3 first";
        var entry=x.valve.parent;
        var arrival=new Vector3(-.6f,.15f,-24);x.flow.environment.arrival.position=arrival;
        var player=GameObject.FindGameObjectWithTag("Player");if(player!=null)player.transform.SetPositionAndRotation(arrival,Quaternion.identity);
        x.valve.localPosition=new Vector3(-1.4f,1.5f,-20.27f);entry.Find("Valve mounting console").localPosition=new Vector3(-1.4f,1.25f,-20.05f);
        x.pressureReadout.transform.localPosition=new Vector3(-1.4f,.55f,-20.3f);x.pressureReadout.fontSize=.65f;
        var sign=entry.Find("Sealed sector sign");sign.localPosition=new Vector3(.75f,2.05f,-18.96f);sign.GetComponent<TextMeshPro>().fontSize=2.1f;
        x.warningLens.transform.localPosition=new Vector3(0,2.85f,-19);x.warningLight.transform.localPosition=new Vector3(-.4f,2.6f,-22);x.warningLight.range=9;
        x.warningRed.SetColor("_EmissionColor",new Color(1.15f,.04f,.015f));EditorUtility.SetDirty(x.warningRed);
        if(x.door.Find("Pressure status red inset")==null)
        {
            var stripe=GameObject.CreatePrimitive(PrimitiveType.Cube);stripe.name="Pressure status red inset";stripe.transform.SetParent(x.door,false);stripe.transform.localPosition=new Vector3(.7f,-1.35f,-.27f);stripe.transform.localScale=new Vector3(2.8f,.025f,.025f);stripe.GetComponent<Renderer>().sharedMaterial=x.warningRed;Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }
        foreach(var t in x.GetComponentsInChildren<TextMeshPro>()){if(t.name=="Sonar instruction")t.fontSize=1.7f;if(t.name=="Memory period")t.fontSize=.8f;}
        var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Oxidised steel.mat");
        var steel=AssetDatabase.LoadAssetAtPath<Material>(F+"Materials/Threshold worn steel.mat");
        if(steel==null){steel=new Material(original);AssetDatabase.CreateAsset(steel,F+"Materials/Threshold worn steel.mat");}
        steel.SetColor("_BaseColor",new Color(.27f,.29f,.3f));steel.SetFloat("_Metallic",.28f);steel.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(steel);
        foreach(var r in x.GetComponentsInChildren<Renderer>(true))if(r.sharedMaterial==original)r.sharedMaterial=steel;
        foreach(var id in AssetDatabase.FindAssets("t:Mesh",new[]{F+"Meshes"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(id));
            if(m.normals.Length==0||m.normals[0].z<=0)continue;
            var tr=m.triangles;for(int i=0;i<tr.Length;i+=3){int a=tr[i];tr[i]=tr[i+2];tr[i+2]=a;}m.triangles=tr;m.RecalculateNormals();EditorUtility.SetDirty(m);
        }
        var lamp=x.transform.Find("Valve work lamp");if(lamp==null){lamp=new GameObject("Valve work lamp",typeof(Light)).transform;lamp.SetParent(x.transform,false);}
        lamp.localPosition=new Vector3(-1.9f,2.5f,-22.5f);var light=lamp.GetComponent<Light>();light.type=LightType.Point;light.color=new Color(.9f,.7f,.55f);light.intensity=5;light.range=6;light.shadows=LightShadows.None;
        var curtain=AssetDatabase.LoadAssetAtPath<Material>(F+"Materials/Memorial aurora.mat");
        if(curtain==null){curtain=new Material(x.aurora[0].sharedMaterial);AssetDatabase.CreateAsset(curtain,F+"Materials/Memorial aurora.mat");}
        curtain.SetColor("_Tint",new Color(.35f,1.15f,.58f));curtain.SetFloat("_Always",1);curtain.SetFloat("_Aurora",1);curtain.SetFloat("_Opacity",.36f);EditorUtility.SetDirty(curtain);
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(F+"Meshes/Aurora curtain.asset");
        if(mesh==null)
        {
            mesh=new Mesh{name="Soft memorial aurora curtain"};var vs=new Vector3[66];var uv=new Vector2[66];var ts=new int[192];
            for(int i=0;i<=32;i++)for(int j=0;j<2;j++){vs[i*2+j]=new Vector3((j-.5f)*4.5f+Mathf.Sin(i*.2f)*1.6f,-13+i*26f/32,0);uv[i*2+j]=new Vector2(j,i/32f);}
            for(int i=0;i<32;i++){int k=i*2;int b=i*6;ts[b]=k;ts[b+1]=k+2;ts[b+2]=k+1;ts[b+3]=k+1;ts[b+4]=k+2;ts[b+5]=k+3;}
            mesh.vertices=vs;mesh.uv=uv;mesh.triangles=ts;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,F+"Meshes/Aurora curtain.asset");
        }
        foreach(var r in x.aurora){r.GetComponent<MeshFilter>().sharedMesh=mesh;r.sharedMaterial=curtain;r.transform.localScale=Vector3.one;}
        EditorSceneManager.MarkSceneDirty(x.gameObject.scene);EditorSceneManager.SaveScene(x.gameObject.scene);AssetDatabase.SaveAssets();return "Polished threshold materials, readable pressure label and soft aurora curtains";
    }
}
#endif
