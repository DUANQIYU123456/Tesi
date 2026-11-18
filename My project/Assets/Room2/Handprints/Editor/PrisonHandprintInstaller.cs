#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Xiaoduan.Survey;

namespace Xiaoduan.PrisonTraces
{
 public static class PrisonHandprintInstaller
 {
  const string Root="Assets/Room2/Handprints";
  [MenuItem("Tools/Xiaoduan/ROOM2/Install fluorescent handprints")]
  public static string Install()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="ROOM2")throw new InvalidOperationException("Open ROOM2 in Edit mode.");
   var previous=GameObject.Find("ROOM2 - Isotope handprint traces");
   if(previous!=null&&previous.GetComponent<PrisonHandprints>().marks!=null&&previous.GetComponent<PrisonHandprints>().marks.Length>4)return "Already installed; positions preserved.";
   if(previous!=null){foreach(var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Xiaoduan/Environment/Fluorescent Handprint").ToArray())UnityEngine.Object.DestroyImmediate(r.gameObject);UnityEngine.Object.DestroyImmediate(previous);}
   EditorSceneManager.SaveScene(scene);string backup="Library/HandprintBackups/ROOM2_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
   Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(scene.path,backup,false);
   foreach(var f in new[]{"Materials","Meshes","Prefabs"})Directory.CreateDirectory(Root+"/"+f);
   foreach(var p in new[]{"Handprint_A.jpg","Handprint_B.jpg"})
   {
    var importer=AssetImporter.GetAtPath(Root+"/Textures/"+p) as TextureImporter;
    importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.None;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();
   }
   var shader=Shader.Find("Xiaoduan/Environment/Fluorescent Handprint");if(shader==null)throw new Exception("Handprint shader not imported.");
   var mats=new Material[2];for(int i=0;i<2;i++){string label=i==0?"Handprint A - worn palm":"Handprint B - full palm";string path=Root+"/Materials/"+label+".mat";mats[i]=AssetDatabase.LoadAssetAtPath<Material>(path);if(mats[i]==null){mats[i]=new Material(shader){name=label};AssetDatabase.CreateAsset(mats[i],path);}mats[i].SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/Handprint_"+(i==0?"A":"B")+".jpg"));mats[i].SetColor("_Tint",new Color(.085f,1,.22f));mats[i].SetFloat("_Opacity",.82f);mats[i].SetFloat("_Glow",2.1f);EditorUtility.SetDirty(mats[i]);}
   var root=new GameObject("ROOM2 - Isotope handprint traces");var control=root.AddComponent<PrisonHandprints>();var marks=new List<Renderer>();
   string[] ids={"SM_PrisonDoor15","SM_PrisonDoor14","SM_PrisonDoor16","SM_PrisonDoor13","SM_PrisonDoor20","SM_PrisonDoor4","SM_PrisonDoor3","SM_PrisonDoor23","SM_PrisonDoor18","SM_PrisonDoor24","SM_PrisonDoor17","SM_PrisonDoor26","SM_PrisonDoor8"};
   var walls=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name.Contains("Wall")&&r.GetComponent<MeshFilter>()!=null).ToArray();
   for(int k=0;k<ids.Length;k++)
   {
    var door=GameObject.Find(ids[k]);if(door==null)throw new Exception("Missing gate: "+ids[k]);
    var gate=door.GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith("LOD0"));var b=gate.bounds;
    Vector3 normal=b.center.x<0?Vector3.right:Vector3.left;
    Vector3 barCenter=new Vector3(b.center.x+normal.x*(b.extents.x+.002f),b.center.y+.05f+(k%3)*.16f,b.center.z+(k%2==0?-.12f:.12f));
    marks.Add(Project(ids[k]+" - hand on iron bars",gate,barCenter,normal,.48f+(k%3)*.035f,.64f+(k%2)*.045f,(k%2==0?-13:11),mats[k%2]));
    if(k<7&&k%3!=1)marks.Add(Project(ids[k]+" - lower iron palm",gate,barCenter+new Vector3(0,-.57f,k%2==0?.30f:-.28f),normal,.39f,.53f,k%2==0?24:-20,mats[(k+1)%2]));
    for(int j=0;j<(k<7&&k%2==0?2:1);j++)
    {
     float z=b.center.z+(j==0?-3.13f:3.08f);
     Vector3 approximate=new Vector3(b.center.x,b.center.y+.1f+(j==1?.31f:0)+(k%3)*.10f,z);
     MeshRenderer wall;Vector3 center;
     if(!FindSurface(walls,approximate,normal,out wall,out center))throw new Exception("No physical wall near "+ids[k]);
     marks.Add(Project(ids[k]+" - wall palm "+j,wall,center,normal,.30f+(k%3)*.025f,.43f+(k%3)*.035f,(j==0?-18:16)*(k%2==0?1:-1),mats[(k+j)%2]));
    }
   }
   control.marks=marks.ToArray();
   for(int i=0;i<2;i++)
   {
    var sample=GameObject.CreatePrimitive(PrimitiveType.Quad);sample.name="Handprint "+(i==0?"A":"B")+" - reusable wall stamp";UnityEngine.Object.DestroyImmediate(sample.GetComponent<Collider>());sample.transform.localScale=new Vector3(.32f,.46f,1);var renderer=sample.GetComponent<Renderer>();renderer.sharedMaterial=mats[i];renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;sample.AddComponent<SurveyTrace>();PrefabUtility.SaveAsPrefabAsset(sample,Root+"/Prefabs/Handprint_"+(i==0?"A":"B")+"_Wall.prefab");UnityEngine.Object.DestroyImmediate(sample);
   }
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=root;
   return "Saved "+marks.Count+" scattered handprints on ground and upper floor iron gates and real wall surfaces. Backup: "+backup;
  }
  static Renderer Project(string name,MeshRenderer source,Vector3 center,Vector3 normal,float width,float height,float tilt,Material material)
  {
   Quaternion rotation=Quaternion.LookRotation(normal,Vector3.up)*Quaternion.Euler(0,0,tilt);var matrix=Matrix4x4.TRS(center,rotation,Vector3.one);var inverse=matrix.inverse;
   var src=source.GetComponent<MeshFilter>().sharedMesh;var verts=src.vertices;var triangles=src.triangles;
   var result=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
   for(int n=0;n<triangles.Length;n+=3)
   {
    Vector3 a=inverse.MultiplyPoint3x4(source.transform.TransformPoint(verts[triangles[n]])),b=inverse.MultiplyPoint3x4(source.transform.TransformPoint(verts[triangles[n+1]])),c=inverse.MultiplyPoint3x4(source.transform.TransformPoint(verts[triangles[n+2]]));
    if(Vector3.Cross(b-a,c-a).normalized.z<.25f)continue;
    if(Mathf.Min(a.x,b.x,c.x)>width*.5f||Mathf.Max(a.x,b.x,c.x)<-width*.5f||Mathf.Min(a.y,b.y,c.y)>height*.5f||Mathf.Max(a.y,b.y,c.y)<-height*.5f||Mathf.Min(a.z,b.z,c.z)>.10f||Mathf.Max(a.z,b.z,c.z)<-.10f)continue;
    var poly=new List<Vector3>{a,b,c};poly=Clip(poly,0,width*.5f,true);poly=Clip(poly,0,-width*.5f,false);poly=Clip(poly,1,height*.5f,true);poly=Clip(poly,1,-height*.5f,false);poly=Clip(poly,2,.10f,true);poly=Clip(poly,2,-.10f,false);
    if(poly.Count<3)continue;int start=result.Count;
    foreach(var p in poly){result.Add(p+Vector3.forward*.003f);uv.Add(new Vector2(p.x/width+.5f,p.y/height+.5f));}
    for(int k=1;k<poly.Count-1;k++){indices.Add(start);indices.Add(start+k);indices.Add(start+k+1);}
   }
   if(indices.Count==0)throw new Exception("No surface triangles for "+name+" on "+source.name);
   var mesh=new Mesh{name=name};mesh.SetVertices(result);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();string assetPath=Root+"/Meshes/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,assetPath);
   var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetPositionAndRotation(center,rotation);go.transform.SetParent(source.transform,true);go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return r;
  }
  static bool FindSurface(MeshRenderer[] walls,Vector3 approximate,Vector3 normal,out MeshRenderer found,out Vector3 hit)
  {
   found=null;hit=approximate;
   foreach(float step in new[]{0f,-.22f,.22f,-.45f,.45f,-.7f,.7f})
   {
    var ray=new Ray(approximate+Vector3.forward*step+normal*2,-normal);float best=4;
    foreach(var wall in walls)
    {
     if(Mathf.Abs(wall.bounds.center.x-approximate.x)>.9f||!wall.bounds.IntersectRay(ray))continue;
     var mesh=wall.GetComponent<MeshFilter>().sharedMesh;if(mesh==null)continue;var vs=mesh.vertices;var ts=mesh.triangles;
     for(int n=0;n<ts.Length;n+=3)
     {
      var a=wall.transform.TransformPoint(vs[ts[n]]);var b=wall.transform.TransformPoint(vs[ts[n+1]]);var c=wall.transform.TransformPoint(vs[ts[n+2]]);
      var edge1=b-a;var edge2=c-a;var cross=Vector3.Cross(ray.direction,edge2);float determinant=Vector3.Dot(edge1,cross);if(Mathf.Abs(determinant)<.000001f)continue;
      float inv=1/determinant;var delta=ray.origin-a;float u=Vector3.Dot(delta,cross)*inv;if(u<0||u>1)continue;var q=Vector3.Cross(delta,edge1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0||u+v>1)continue;float t=Vector3.Dot(edge2,q)*inv;
      if(t>0&&t<best){best=t;found=wall;hit=ray.GetPoint(t);}
     }
    }
    if(found!=null)return true;
   }
   return false;
  }
  static List<Vector3> Clip(List<Vector3> input,int axis,float plane,bool below)
  {
   var output=new List<Vector3>();if(input.Count==0)return output;
   Vector3 prior=input[input.Count-1];bool previous=below?prior[axis]<=plane:prior[axis]>=plane;
   foreach(var point in input){bool inside=below?point[axis]<=plane:point[axis]>=plane;if(inside!=previous){float t=(plane-prior[axis])/(point[axis]-prior[axis]);output.Add(Vector3.Lerp(prior,point,t));}if(inside)output.Add(point);prior=point;previous=inside;}return output;
  }
 }
}
#endif
