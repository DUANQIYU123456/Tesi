#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections.Generic;
using UnityEngine;using UnityEngine.Rendering;using UnityEditor;using UnityEditor.SceneManagement;
namespace Xiaoduan.PrisonTraces {
 public static class HandprintCompositionPass {
  const string Root="Assets/Room2/Handprints";
  static List<Renderer> marks;static MeshRenderer[] walls;static Material[] mats;
  public static string Apply() {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="ROOM2")throw new Exception("ROOM2 Edit mode required");
   var control=UnityEngine.Object.FindFirstObjectByType<PrisonHandprints>();
   if(control==null)throw new Exception("Existing handprint controller missing");
   EditorSceneManager.SaveScene(scene);var backup="Library/HandprintBackups/ROOM2_before_composition_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
   Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(scene.path,backup,false);
   mats=new Material[8];
   for(int i=0;i<8;i++) {
    int texture=i%2;bool mirror=(i/2)%2==1;bool faint=i>=4;
    string label="Composition "+(texture==0?"A":"B")+(mirror?" mirrored":"")+(faint?" faded":" fresh");
    string path=Root+"/Materials/"+label+".mat";
    var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(mat==null){mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Handprint "+(texture==0?"A - worn palm":"B - full palm")+".mat"));mat.name=label;AssetDatabase.CreateAsset(mat,path);}
    mat.SetFloat("_Mirror",mirror?1:0);mat.SetFloat("_Opacity",faint?.47f:.82f);mat.SetFloat("_Glow",faint?1.65f:2.1f);EditorUtility.SetDirty(mat);mats[i]=mat;
   }
   walls=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name.Contains("Wall")&&r.GetComponent<MeshFilter>()!=null).ToArray();
   marks=new List<Renderer>();
   try {
    // Authored asymmetry: no repeating per-door pattern. Offsets are metres from each gate centre.
    Gate(15,.48f,-.23f,.41f,.57f,-27,1,"hand on iron bars");
    Gate(15,-.37f,.27f,.32f,.46f,34,6,"lower iron palm");
    Gate(14,-.19f,-.28f,.37f,.51f,16,2,"single iron palm");
    Gate(16,.57f,.25f,.35f,.49f,31,3,"high iron palm");
    Gate(16,.03f,-.14f,.43f,.59f,-18,0,"middle iron palm");
    Gate(16,-.60f,.34f,.30f,.43f,-39,5,"low faded iron palm");
    Gate(20,.13f,-.34f,.39f,.54f,8,4,"single iron palm");
    Gate(4,-.44f,-.30f,.35f,.49f,-32,1,"lower iron palm");
    Gate(4,.41f,.19f,.44f,.60f,21,2,"high iron palm");
    Gate(3,.22f,.32f,.36f,.50f,-15,3,"iron palm");
    Gate(3,-.48f,-.18f,.29f,.41f,42,4,"faded iron palm");
    Gate(23,.41f,-.32f,.39f,.54f,-23,0,"iron palm");
    Gate(23,-.31f,.26f,.31f,.45f,36,7,"faded iron palm");
    Gate(18,-.49f,.22f,.34f,.47f,-41,5,"single iron palm");
    Gate(17,.59f,.31f,.33f,.46f,13,1,"high iron palm");
    Gate(17,-.13f,-.28f,.41f,.56f,-29,2,"iron palm");
    Gate(26,.15f,-.27f,.36f,.50f,28,3,"iron palm");
    Gate(26,-.58f,.24f,.29f,.40f,-12,4,"low faded iron palm");
    // Wall groups have unequal density, height and handedness, with intentionally empty opposite posts.
    Wall(15,-3.16f,.66f,.30f,.43f,-32,0,0);
    Wall(15,-2.78f,.10f,.25f,.36f,18,7,1);
    Wall(14,3.12f,1.02f,.28f,.40f,29,3,0);
    Wall(16,3.06f,-.39f,.24f,.35f,-46,6,0);
    Wall(13,-3.26f,.22f,.32f,.45f,-19,1,0);
    Wall(13,-2.86f,.80f,.27f,.38f,38,2,1);
    Wall(13,-3.05f,-.51f,.24f,.34f,53,5,2);
    Wall(20,3.17f,.05f,.28f,.40f,-11,4,0);
    Wall(3,-3.24f,.56f,.33f,.47f,26,2,0);
    Wall(3,-2.79f,-.12f,.25f,.36f,-37,5,1);
    Wall(23,3.08f,-.43f,.26f,.37f,43,6,0);
    Wall(18,-3.23f,.85f,.29f,.42f,-25,1,0);
    Wall(18,-2.81f,.28f,.25f,.35f,19,7,1);
    Wall(17,3.22f,.09f,.31f,.44f,34,0,0);
    Wall(26,-3.18f,-.55f,.24f,.34f,-42,4,0);
    Wall(26,-2.82f,.04f,.30f,.42f,12,3,1);
    Wall(8,3.11f,.56f,.27f,.38f,-17,5,0);
   } catch {foreach(var r in marks)if(r!=null)UnityEngine.Object.DestroyImmediate(r.gameObject);throw;}
   if(marks.Count!=35)throw new Exception("Unexpected count");
   foreach(var r in control.marks)if(r!=null)UnityEngine.Object.DestroyImmediate(r.gameObject);
   control.marks=marks.ToArray();EditorUtility.SetDirty(control);
   AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=control.gameObject;
   return "Saved 35 recomposed palms: 18 gate / 17 wall, 8 varied materials. Backup: "+backup;
  }
  static MeshRenderer GateSource(int id) {return GameObject.Find("SM_PrisonDoor"+id).GetComponentsInChildren<MeshRenderer>().First(r=>r.name.EndsWith("LOD0"));}
  static void Gate(int id,float dy,float dz,float width,float height,float tilt,int material,string label) {
   var gate=GateSource(id);var b=gate.bounds;var normal=b.center.x<0?Vector3.right:Vector3.left;
   var center=new Vector3(b.center.x+normal.x*(b.extents.x+.002f),b.center.y+dy,b.center.z+dz);
   marks.Add(Project("SM_PrisonDoor"+id+" - "+label,gate,center,normal,width,height,tilt,mats[material]));
  }
  static void Wall(int id,float dz,float dy,float width,float height,float tilt,int material,int index) {
   var gate=GateSource(id);var b=gate.bounds;var normal=b.center.x<0?Vector3.right:Vector3.left;
   MeshRenderer wall;Vector3 center;
   if(!FindSurface(walls,new Vector3(b.center.x,b.center.y+dy,b.center.z+dz),normal,out wall,out center))throw new Exception("No wall for group "+id+"/"+index);
   marks.Add(Project("SM_PrisonDoor"+id+" - composed wall palm "+index,wall,center,normal,width,height,tilt,mats[material]));
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
