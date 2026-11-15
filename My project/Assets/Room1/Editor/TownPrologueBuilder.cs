#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Xiaoduan.Survey;
using Xiaoduan.Narrative;
public static class TownPrologueBuilder
{
 const string Root="Assets/Room1";
 static Transform root;static Material wood,iron,brass,glass,ivory,green,black;static TMP_FontAsset font;static List<Renderer> glowing;
 static Material Mat(string name,Color color,float metal=0,float smooth=.3f,bool emission=false)
 {
  string p=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,p);}m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.025f,1,.16f)*.12f);}return m;
 }
 static GameObject Shape(string name,PrimitiveType type,Transform parent,Vector3 p,Vector3 scale,Material m,bool collision=false)
 {
  var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;if(!collision)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());if(m==green)glowing.Add(g.GetComponent<Renderer>());return g;
 }
 static GameObject Box(string name,Transform p,Vector3 pos,Vector3 scale,Material m,bool collision=false)=>Shape(name,PrimitiveType.Cube,p,pos,scale,m,collision);
 static GameObject Disc(string name,Transform p,Vector3 pos,float radius,float thickness,Material m)=>Shape(name,PrimitiveType.Cylinder,p,pos,new Vector3(radius*2,thickness*.5f,radius*2),m);
 static void Rod(string name,Transform p,Vector3 a,Vector3 b,float radius,Material m){var g=Disc(name,p,(a+b)*.5f,radius,(b-a).magnitude,m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
 static TMP_Text Text(string name,Transform p,string value,Vector3 pos,float size,Color color,Vector3 euler)
 {
  var g=new GameObject(name,typeof(TextMeshPro));g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localEulerAngles=euler;var t=g.GetComponent<TextMeshPro>();t.font=font;t.fontSize=size;t.text=value;t.alignment=TextAlignmentOptions.Center;t.color=color;t.rectTransform.sizeDelta=new Vector2(2,.6f);return t;
 }
 static Transform Group(string name,Transform parent,Vector3 p){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=p;return g.transform;}
 static void Bench(TownPrologue flow)
 {
  var bench=Group("Telephone corner / slatted bench",root,new Vector3(52.5f,0,-18.9f));
  for(int i=0;i<6;i++)Box("Oak seat slat "+i,bench,new Vector3(0,.56f,-.25f+i*.1f),new Vector3(2.5f,.055f,.085f),wood);
  for(int i=0;i<4;i++)Box("Oak back slat "+i,bench,new Vector3(0,.83f+i*.135f,-.29f),new Vector3(2.5f,.095f,.045f),wood);
  for(int side=-1;side<=1;side+=2){float x=side*1.04f;Rod("Cast iron back stay",bench,new Vector3(x,.1f,-.25f),new Vector3(x,1.3f,-.29f),.023f,iron);Rod("Front leg",bench,new Vector3(x,.04f,.22f),new Vector3(x,.56f,.18f),.025f,iron);Rod("Seat bracket",bench,new Vector3(x,.52f,-.32f),new Vector3(x,.52f,.32f),.024f,iron);Rod("Arm rest",bench,new Vector3(x,.82f,-.26f),new Vector3(x,.82f,.31f),.025f,iron);Rod("Arm support",bench,new Vector3(x,.56f,.27f),new Vector3(x,.82f,.27f),.02f,iron);for(int i=0;i<4;i++)Shape("Brass slat fixing",PrimitiveType.Sphere,bench,new Vector3(x,.83f+i*.135f,-.257f),Vector3.one*.021f,brass);}
  var bounds=bench.gameObject.AddComponent<BoxCollider>();bounds.center=new Vector3(0,.31f,0);bounds.size=new Vector3(2.5f,.6f,.62f);
  var vanity=Group("Vanity / camel-hair brush and compact",bench,new Vector3(.47f,.61f,.04f));
  Disc("Brass compact case",vanity,Vector3.zero,.105f,.026f,brass);Disc("Luminous face powder",vanity,new Vector3(0,.017f,0),.093f,.006f,green);
  var lid=Group("Open mirror lid",vanity,new Vector3(0,.012f,-.09f));lid.localEulerAngles=new Vector3(67,0,0);Disc("Compact lid rim",lid,new Vector3(0,0,-.095f),.106f,.012f,brass);var mirror=Disc("Silver mirror",lid,new Vector3(0,.009f,-.095f),.094f,.004f,glass);
  Rod("Brush wooden handle",vanity,new Vector3(.13f,.016f,-.09f),new Vector3(.33f,.016f,.03f),.007f,wood);Rod("Brush ferrule",vanity,new Vector3(.33f,.016f,.03f),new Vector3(.36f,.016f,.048f),.008f,brass);
  for(int i=0;i<7;i++)Rod("Camel hair bristle "+i,vanity,new Vector3(.36f,.017f+i*.0008f,.048f),new Vector3(.405f,.017f,.071f+(i-3)*.001f),.0015f,i<4?green:wood);
  var entry=AssetDatabase.LoadAssetAtPath<NarrativeEntry>(Root+"/VanityRecord.asset");if(entry==null){entry=ScriptableObject.CreateInstance<NarrativeEntry>();AssetDatabase.CreateAsset(entry,Root+"/VanityRecord.asset");}entry.title="Paintbrush & compact";entry.category="XII / A MODERN PROMISE";entry.body="Hold F to retain the hidden diary.";EditorUtility.SetDirty(entry);
  var inspect=vanity.gameObject.AddComponent<NarrativeInspectable>();inspect.entry=entry;inspect.focusRenderer=mirror.GetComponent<Renderer>();inspect.interactionDistance=3.5f;
  var node=vanity.gameObject.AddComponent<SurveyNode>();node.kind=SurveyNodeKind.TownVanity;node.focus=Group("Look here / mirror and brush",vanity,new Vector3(.08f,.06f,0));node.reach=4;flow.benchFocus=node.focus;
  Clock(bench,new Vector3(-.86f,.78f,-.025f));
  var toy=Group("Child's wooden spinning top",bench,new Vector3(-.25f,.68f,.05f));Shape("Painted toy body",PrimitiveType.Sphere,toy,Vector3.zero,new Vector3(.15f,.12f,.15f),ivory);Disc("Faded green band",toy,Vector3.zero,.078f,.016f,green);Rod("Wooden spindle",toy,new Vector3(0,-.09f,0),new Vector3(0,.11f,0),.012f,wood);
  Text("Bench narrative plaque",bench,"PUBLIC SEATING  /  RETURN TO WORK",new Vector3(0,1.39f,-.27f),.6f,new Color(.58f,.52f,.36f),new Vector3(0,180,0));
 }
 static void Clock(Transform parent,Vector3 position)
 {
  var clock=Group("Domestic luminous alarm clock",parent,position);var face=Group("Clock face",clock,Vector3.zero);face.localEulerAngles=new Vector3(90,0,0);Disc("Enamel clock housing",face,Vector3.zero,.16f,.09f,iron);Disc("Ivory dial",face,new Vector3(0,.048f,0),.141f,.008f,ivory);
  for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Box("Luminous numeral marker",face,new Vector3(Mathf.Sin(a)*.117f,.055f,Mathf.Cos(a)*.117f),new Vector3(.012f,.003f,.018f),green);}
  Rod("Luminous minute hand",face,new Vector3(0,.059f,0),new Vector3(.045f,.059f,.092f),.004f,green);Rod("Luminous hour hand",face,new Vector3(0,.061f,0),new Vector3(-.055f,.061f,.025f),.005f,green);
  for(int side=-1;side<=1;side+=2){Shape("Bell",PrimitiveType.Sphere,clock,new Vector3(side*.105f,.15f,0),new Vector3(.11f,.065f,.08f),brass);Rod("Clock foot",clock,new Vector3(side*.09f,-.18f,.01f),new Vector3(side*.07f,-.12f,0),.013f,brass);}
 }
 static void WallObjects()
 {
  var plate=Group("Luminous domestic switch",root,new Vector3(59.6f,1.3f,-23.61f));Box("Bakelite plate",plate,Vector3.zero,new Vector3(.12f,.18f,.02f),ivory);Box("Luminous switch edge",plate,new Vector3(0,0,.012f),new Vector3(.045f,.07f,.012f),green);Box("Switch lever",plate,new Vector3(0,.005f,.025f),new Vector3(.03f,.05f,.024f),black);
  var lockPlate=Group("Luminous keyhole / telephone booth",root,new Vector3(55.12f,1.18f,-18.42f));Box("Lock escutcheon",lockPlate,Vector3.zero,new Vector3(.065f,.12f,.012f),brass);var ring=Disc("Green lock surround",lockPlate,new Vector3(0,.018f,.01f),.018f,.006f,green);ring.transform.localEulerAngles=new Vector3(90,0,0);Box("Keyhole",lockPlate,new Vector3(0,.002f,.016f),new Vector3(.012f,.034f,.004f),black);
 }
 static void Poster(string name,Texture source,Vector3 position,Vector3 normal,float height,float ratio,bool face,int wear)
 {
  var materialPath=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(m==null){m=new Material(Shader.Find("Xiaoduan/Environment/Town Two Layer Poster"));AssetDatabase.CreateAsset(m,materialPath);}
  m.SetTexture("_PosterMap",source);m.SetTexture("_WearMap",AssetDatabase.LoadAssetAtPath<Texture>("Assets/小资产们/StylizedTown/Textures/Advertisement_basecolor.png"));m.SetTexture("_GhostMap",AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Images/Thesis_Fig2_7.jpg"));m.SetTexture("_Radiograph",AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Images/SkeletalReveal_Reconstruction.png"));m.SetColor("_PaperTint",new Color(.85f,.78f,.59f));m.SetVector("_WearRect",new Vector4(.084f,.019f,.386f,.452f));m.SetVector("_FaceRect",face?new Vector4(.71f,.79f,.205f,.22f):Vector4.zero);EditorUtility.SetDirty(m);
  var g=Shape(name,PrimitiveType.Quad,root,position,new Vector3(height*ratio,height,1),m);g.transform.rotation=Quaternion.LookRotation(-normal,Vector3.up);
 }
 static void Circuit()
 {
  var g=Group("Digital decay / exposed circuit beneath brick",root,new Vector3(55.7f,.48f,-27.1f));
  var vs=new[]{new Vector3(-.5f,-.19f,0),new Vector3(-.42f,.17f,0),new Vector3(-.27f,.27f,0),new Vector3(-.13f,.20f,0),new Vector3(.04f,.26f,0),new Vector3(.23f,.17f,0),new Vector3(.49f,.13f,0),new Vector3(.42f,-.12f,0),new Vector3(.21f,-.23f,0),new Vector3(-.07f,-.19f,0)};
  var mesh=new Mesh{name="Broken brick circuit aperture"};var tris=new List<int>();for(int i=1;i<vs.Length-1;i++){tris.Add(0);tris.Add(i+1);tris.Add(i);}mesh.vertices=vs;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();string p=Root+"/CircuitAperture.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(old==null)AssetDatabase.CreateAsset(mesh,p);else{EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}
  var board=new GameObject("Torn PCB aperture",typeof(MeshFilter),typeof(MeshRenderer));board.transform.SetParent(g,false);board.GetComponent<MeshFilter>().sharedMesh=mesh;board.GetComponent<MeshRenderer>().sharedMaterial=Mat("Oxidized PCB",new Color(.008f,.028f,.014f),.3f);
  for(int i=0;i<6;i++){float x=-.32f+i*.12f;Box("Etched vertical circuit",g,new Vector3(x,0,.006f),new Vector3(.006f,.20f,.002f),green);Box("Etched right-angle circuit",g,new Vector3(x+.04f,.10f-i*.032f,.008f),new Vector3(.085f,.006f,.002f),green);}
  Box("Silicon beneath the town",g,new Vector3(0,0,.012f),new Vector3(.14f,.14f,.018f),black);for(int i=0;i<6;i++){Box("Gold chip pin",g,new Vector3(-.087f,-.055f+i*.022f,.012f),new Vector3(.03f,.008f,.008f),brass);Box("Gold chip pin",g,new Vector3(.087f,-.055f+i*.022f,.012f),new Vector3(.03f,.008f,.008f),brass);}
  Rod("Crack into brick",g,new Vector3(-.35f,.18f,.005f),new Vector3(-.62f,.30f,.005f),.006f,black);Rod("Branching brick fracture",g,new Vector3(.37f,-.13f,.005f),new Vector3(.69f,-.22f,.005f),.005f,black);
 }
 static GameObject Trail(string name,Vector3[] points)
 {
  var trail=Group(name,root,Vector3.zero);trail.gameObject.AddComponent<SurveyTrace>();int index=0;
  for(int segment=0;segment<points.Length-1;segment++){
   Vector3 dir=points[segment+1]-points[segment];dir.y=0;int steps=Mathf.CeilToInt(dir.magnitude/.68f);dir.Normalize();
   for(int j=0;j<steps;j++){
    Vector3 p=Vector3.Lerp(points[segment],points[segment+1],(j+.5f)/steps)+Vector3.Cross(dir,Vector3.up)*(index%2==0?.13f:-.13f);
    var hit=Physics.RaycastAll(p+Vector3.up*3,Vector3.down,8,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.85f&&!h.collider.CompareTag("Player")&&!h.collider.transform.root.CompareTag("Player")).OrderBy(h=>Mathf.Abs(h.point.y)).FirstOrDefault();if(hit.collider==null)continue;
    string asset="Assets/Art/Footprints/Prefabs/Footprint_"+(index%2==0?"05":"08")+"_Ground.prefab";var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(asset);var foot=(GameObject)PrefabUtility.InstantiatePrefab(prefab,trail);foot.name="Ghost shoe print "+index;foot.transform.position=hit.point+Vector3.up*.012f;foot.transform.rotation=Quaternion.LookRotation(dir,Vector3.up)*Quaternion.Euler(0,45,0)*Quaternion.Euler(90,0,0);foot.GetComponent<Renderer>().enabled=false;
    if(index%3==0)for(int k=0;k<3;k++){var dot=Disc("Spilled luminous powder",trail,hit.point+new Vector3(.035f*k,.015f,-.045f*k),.008f+.002f*k,.001f,green);dot.GetComponent<Renderer>().enabled=false;}
    index++;
   }
  }
  return trail.gameObject;
 }
 public static string Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Stop Play first");if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="ROOM1")throw new Exception("Open ROOM1");
  if(GameObject.Find("ROOM1 - Radium Frenzy Prologue")!=null)throw new Exception("Prologue already exists; edit it instead of duplicating it");
  root=new GameObject("ROOM1 - Radium Frenzy Prologue").transform;glowing=new List<Renderer>();font=Resources.Load<GameObject>("SurveyInterface").GetComponent<SurveyTypography>().bodyFont;
  wood=Mat("Worn honey oak",new Color(.24f,.115f,.043f));iron=Mat("Painted cast iron",new Color(.024f,.036f,.029f),.65f);brass=Mat("Tarnished brass",new Color(.48f,.31f,.12f),.75f,.5f);glass=Mat("Compact silver mirror",new Color(.65f,.73f,.69f),.95f,.93f);ivory=Mat("Old ivory enamel",new Color(.71f,.66f,.48f));green=Mat("Faint luminous paint",new Color(.08f,.18f,.09f),0,.25f,true);black=Mat("Bakelite",new Color(.009f,.014f,.011f));
  var flow=root.gameObject.AddComponent<TownPrologue>();Bench(flow);WallObjects();Circuit();
  Poster("Undark - window advertisement",AssetDatabase.LoadAssetAtPath<Texture>("Assets/Scenes/960px-Undark_(Radium_Girls)_advertisement,_1921,_retouched.png"),new Vector3(44.70f,2.8f,1.5f),Vector3.left,2.2f,960f/1400,false,1);
  Poster("Radium water - side wall advertisement",AssetDatabase.LoadAssetAtPath<Texture>("Assets/Scenes/aef600aa6ba69aa0a33e388f607d4c44.png"),new Vector3(48.4f,2.55f,-27.1f),Vector3.forward,1.7f,1.30f,false,1);
  var beauty=AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Images/Radior_1918.jpg");Poster("Radior beauty - two layer advertisement",beauty,new Vector3(52.2f,3.1f,-27.1f),Vector3.forward,2.25f,1.75f/2.25f,true,1);
  var oldTrace=GameObject.Find("Isotope footprints / RMB");if(oldTrace!=null)oldTrace.SetActive(false);
  flow.approachTrail=Trail("RMB - ghost shoe route to vanity",new[]{new Vector3(31,0,0),new Vector3(36,0,-2),new Vector3(41,0,-5),new Vector3(43,0,-9),new Vector3(48,0,-14),new Vector3(52.7f,0,-17)});
  flow.exitTrail=Trail("RMB - diary to anomaly route",new[]{new Vector3(52.7f,0,-17),new Vector3(50.3f,0,-17),new Vector3(50.3f,0,-21.5f),new Vector3(52.1f,0,-23),new Vector3(54.2f,0,-23.1f)});
  flow.portal=UnityEngine.Object.FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).First(p=>p.sceneName=="ROOM2");var portal=flow.portal.gameObject;
  var destinations=portal.GetComponentsInChildren<TransitionDestination>().Select(d=>new {d,p=d.transform.position}).ToArray();portal.transform.position=new Vector3(55.1f,1.35f,-24.3f);portal.transform.rotation=Quaternion.identity;portal.transform.localScale=Vector3.one;foreach(var d in destinations)d.d.transform.position=d.p;
  portal.GetComponent<Renderer>().enabled=false;foreach(var collider in portal.GetComponents<Collider>())collider.enabled=false;flow.portal.enabled=false;
  var box=portal.GetComponent<BoxCollider>();box.size=new Vector3(3.1f,3.3f,2.1f);box.center=Vector3.zero;box.isTrigger=true;
  var rift=Group("Isotope rift / revealed after diary",portal.transform,Vector3.zero);var riftMat=new Material(Shader.Find("Xiaoduan/Environment/Town Isotope Rift"));AssetDatabase.CreateAsset(riftMat,Root+"/Materials/Isotope rift.mat");var iris=Shape("Cyan isotope vortex",PrimitiveType.Quad,rift,Vector3.zero,new Vector3(2.8f,2.65f,1),riftMat);iris.transform.rotation=Quaternion.LookRotation((portal.transform.position-new Vector3(52.7f,1.4f,-16.9f)).normalized,Vector3.up);flow.riftSurface=iris.GetComponent<Renderer>();
  var light=new GameObject("Rift spill light",typeof(Light));light.transform.SetParent(rift,false);flow.riftLight=light.GetComponent<Light>();flow.riftLight.type=LightType.Point;flow.riftLight.color=new Color(.12f,1,.57f);flow.riftLight.range=4;flow.riftLight.intensity=.8f;flow.riftLight.shadows=LightShadows.None;flow.riftLight.enabled=false;flow.rift=rift.gameObject;flow.rift.SetActive(false);flow.exitTrail.SetActive(false);flow.domesticGlow=glowing.ToArray();
  EditorSceneManager.MarkSceneDirty(root.gameObject.scene);EditorSceneManager.SaveScene(root.gameObject.scene);AssetDatabase.SaveAssets();Selection.activeGameObject=root.gameObject;
  return "Saved ROOM1 prologue: 3 advertisements, bench diary, 4 domestic props, PCB crack, two shoe trails and delayed vortex";
 }
}
#endif

