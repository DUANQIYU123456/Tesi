#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using Xiaoduan.Room3;

namespace Xiaoduan.Survey.Editor
{
 public static class SurveyArtPass
 {
  static Color Ink=new Color(.83f,.89f,.86f),Muted=new Color(.48f,.59f,.54f),Green=new Color(.49f,.88f,.65f);
  static SurveyTypography fonts;
  static RectTransform R(Transform parent,string name)=>parent.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(x=>x.name==name);
  static void Place(RectTransform r,float x,float y,float w,float h){if(r==null)return;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
  static TMP_Text Text(Transform parent,string name,Vector2 position,Vector2 size,float points,string value,TextAlignmentOptions align=TextAlignmentOptions.Left)
  {
   var r=R(parent,name);if(r==null){r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);}
   r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;
   var t=r.GetComponent<TextMeshProUGUI>();if(t==null)t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=fonts.instrumentFont;t.fontSharedMaterial=t.font.material;t.fontSize=points;t.color=Muted;t.text=value;t.alignment=align;t.enableAutoSizing=false;t.raycastTarget=false;return t;
  }
  static UnityEngine.UI.Image Line(Transform parent,string name,Vector2 position,Vector2 size,Color color)
  {
   var r=R(parent,name);if(r==null){r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);}
   r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;
   var image=r.GetComponent<UnityEngine.UI.Image>();if(image==null)image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;
  }
  static void QuietFrames(Transform root)
  {
   foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
    if(image.name=="Bloodlines perimeter")image.color=new Color(.24f,.43f,.34f,.36f);
   foreach(var button in root.GetComponentsInChildren<SurveyButton>(true))
   {
    button.idle=new Color(.43f,.53f,.47f);button.hover=new Color(.43f,.95f,.63f);button.image.color=button.idle;
    var text=button.GetComponentInChildren<TMP_Text>();if(text!=null)text.color=Ink;
   }
  }
  public static string ApplyInterface()
  {
   const string path="Assets/UI/Survey/Resources/SurveyInterface.prefab";
   var root=PrefabUtility.LoadPrefabContents(path);var hud=root.GetComponent<SurveyHUD>();fonts=root.GetComponent<SurveyTypography>();QuietFrames(root.transform);
   // An asymmetrical archive cover; a single fracture occupies the unused field.
   hud.menu.GetComponent<UnityEngine.UI.Image>().color=new Color(.006f,.010f,.009f,1);
   var menuFrame=R(hud.menu.transform,"Bloodlines main frame");if(menuFrame!=null)menuFrame.gameObject.SetActive(false);
   var title=R(hud.menu.transform,"Project title").GetComponent<TMP_Text>();Place(title.rectTransform,-260,190,920,210);title.fontSize=76;title.alignment=TextAlignmentOptions.Left;title.characterSpacing=1.5f;title.color=Ink;
   var sub=R(hud.menu.transform,"Menu subtitle").GetComponent<TMP_Text>();Place(sub.rectTransform,-310,27,820,46);sub.text="A FIELD STUDY OF LABOR, LIGHT AND ERASURE";sub.fontSize=18;sub.color=Muted;sub.alignment=TextAlignmentOptions.Left;
   Text(hud.menu.transform,"Index cover kicker",new Vector2(-260,341),new Vector2(920,32),17,"INTERACTIVE ARCHIVE   /   FIELD RECORD 01");
   Line(hud.menu.transform,"Cover rule",new Vector2(-445,-22),new Vector2(550,1),new Color(.30f,.41f,.35f,.65f));
   Place(hud.startButton.GetComponent<RectTransform>(),-465,-130,510,74);var begin=hud.startButton.GetComponentInChildren<TMP_Text>();begin.text="BEGIN INSPECTION";begin.fontSize=24;
   Place(hud.menuMotionButton.GetComponent<RectTransform>(),-510,-237,420,44);hud.menuMotionButton.GetComponentInChildren<TMP_Text>().fontSize=17;
   var controls=R(hud.menu.transform,"Controls").GetComponent<TMP_Text>();Place(controls.rectTransform,-285,-337,870,78);controls.text="WASD / MOVE      E / EXAMINE\nRMB / ISOTOPE VIEW      F / REVEAL      ESC / PAUSE";controls.fontSize=17;controls.color=Muted;controls.alignment=TextAlignmentOptions.Left;
   var author=R(hud.menu.transform,"Authorship");author.anchorMin=author.anchorMax=new Vector2(0,0);author.pivot=new Vector2(0,.5f);Place(author,68,42,1050,30);author.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Left;author.GetComponent<TMP_Text>().fontSize=14;
   Place(hud.menuSlit,510,75,2,610);hud.menuSlit.GetComponent<UnityEngine.UI.Image>().color=new Color(.38f,.78f,.52f,.72f);
   var halo=R(hud.menuSlit,"Soft fracture halo");Place(halo,0,0,36,610);halo.GetComponent<UnityEngine.UI.Image>().color=new Color(.16f,.65f,.33f,.018f);
   for(int i=0;i<5;i++)Line(hud.menuSlit,"Residual fracture "+i,new Vector2((i%2==0?1:-1)*(4+i),-240+i*115),new Vector2(1,56+i*7),new Color(.38f,.7f,.49f,.13f));
   Text(hud.menu.transform,"Index isotope",new Vector2(510,-294),new Vector2(400,64),15,"Ra-226\nRESIDUAL TRACE / 1600 YEARS",TextAlignmentOptions.Center);
   // Retain the four gameplay widgets with substantially less framing.
   var status=hud.roomLabel.transform.parent.GetComponent<UnityEngine.UI.Image>();status.color=Color.clear;
   foreach(var frame in status.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(frame.name=="Bloodlines perimeter")frame.gameObject.SetActive(false);
   hud.roomLabel.fontSize=25;hud.roleLabel.fontSize=17;hud.integrity.fontSize=16;hud.objective.fontSize=23;
   Place(hud.fracture,102,210,330,455);hud.fracture.GetComponent<UnityEngine.UI.Image>().color=new Color(.13f,.5f,.29f,.12f);
   hud.reducedButton.gameObject.SetActive(false);
   var radar=hud.needle.parent;var ring=R(radar,"Bloodlines dial");Place(ring,0,0,214,214);ring.GetComponent<UnityEngine.UI.Image>().color=new Color(.3f,.46f,.36f,.67f);
   Place(R(radar,"Dial shadow"),0,0,184,184);
   var label=R(radar,"Instrument identity");if(label!=null)label.gameObject.SetActive(false);
   hud.cpm.fontSize=17;Place(hud.cpm.rectTransform,0,-38,106,43);hud.cpm.color=Green;
   foreach(var t in hud.fragments)t.fontSize=20;Place(hud.traceCount.rectTransform,0,-132,235,30);hud.traceCount.fontSize=16;
   foreach(var p in new[]{new Vector2(0,96),new Vector2(96,0),new Vector2(0,-96),new Vector2(-96,0)})Line(radar,"Index tick "+p,new Vector2(p.x,p.y),p.x==0?new Vector2(1,6):new Vector2(6,1),new Color(.40f,.55f,.47f,.5f));
   hud.truthText.color=new Color(.64f,.82f,.72f);hud.officialText.color=Ink;hud.sourceText.color=new Color(.51f,.61f,.56f);
   hud.archive.GetComponent<UnityEngine.UI.Image>().color=new Color(.003f,.006f,.005f,.9f);
   var notice=hud.banner.GetComponent<UnityEngine.UI.Image>();notice.sprite=null;notice.material=null;notice.color=new Color(.012f,.024f,.019f,.94f);
   Line(notice.transform,"Notice leading edge",new Vector2(-389,0),new Vector2(2,45),Green);hud.bannerText.fontSize=21;hud.bannerText.rectTransform.sizeDelta=new Vector2(746,58);
   var pause=root.GetComponent<SurveyPause>();var plate=R(pause.panel.transform,"Pause plate");
   foreach(var t in new[]{R(plate,"Pause title").GetComponent<TMP_Text>(),R(plate,"Pause note").GetComponent<TMP_Text>(),pause.volumeLabel,pause.sensitivityLabel})t.alignment=TextAlignmentOptions.Left;
   pause.volumeLabel.rectTransform.sizeDelta=pause.sensitivityLabel.rectTransform.sizeDelta=new Vector2(560,35);
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
   var theme=AssetDatabase.LoadAssetAtPath<Xiaoduan.Narrative.NarrativeTheme>("Assets/UI/Narrative/NarrativeTheme.asset");theme.introLabel="";theme.text=Ink;theme.accent=Green;theme.muted=Muted;EditorUtility.SetDirty(theme);
   AssetDatabase.SaveAssets();return "Archive cover, quiet HUD, muted frames and consistent typography applied";
  }
  public static string ApplyRoom3Lighting()
  {
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="ROOM3")throw new Exception("Open ROOM3 before lighting");
   var volume=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).First(v=>v.name=="ROOM3 Global Volume");var profile=volume.sharedProfile;
   if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(.35f);color.contrast.Override(18);}
   if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.48f);bloom.scatter.Override(.62f);}
   var fog=profile.components.First(c=>c.GetType().Name=="VolumetricFogVolumeComponent");
   ((ClampedFloatParameter)fog.GetType().GetField("density").GetValue(fog)).Override(.075f);
   ((ClampedFloatParameter)fog.GetType().GetField("scattering").GetValue(fog)).Override(.085f);
   ((ColorParameter)fog.GetType().GetField("tint").GetValue(fog)).Override(new Color(.67f,.77f,.76f));
   RenderSettings.ambientSkyColor=new Color(.20f,.27f,.29f);RenderSettings.ambientEquatorColor=new Color(.105f,.14f,.145f);RenderSettings.ambientGroundColor=new Color(.045f,.06f,.06f);
   foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
   {
    if(light.name=="Cold backlight"){light.intensity=1.8f;light.color=new Color(.72f,.82f,.88f);}
    if(light.name=="Soft frontal fill"){light.intensity=.28f;light.color=new Color(.36f,.47f,.49f);}
    if(light.name=="Cleanroom reflected light")light.intensity=9;
   }
   var root=GameObject.Find("ROOM3 - Motivated light");if(root==null)root=new GameObject("ROOM3 - Motivated light");
   Point(root.transform,"Bridge task light",new Vector3(0,4.2f,3),new Color(.65f,.81f,.8f),7,12);
   Point(root.transform,"Core left rim",new Vector3(-13,18,104),new Color(.47f,.67f,.72f),13,28);
   Point(root.transform,"Core right rim",new Vector3(13,26,113),new Color(.43f,.68f,.62f),11,28);
   Point(root.transform,"Testimony reflected light",new Vector3(0,5,40),new Color(.53f,.77f,.69f),4,13);
   EditorUtility.SetDirty(profile);foreach(var c in profile.components)EditorUtility.SetDirty(c);
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   return "ROOM3 exposure, fog scattering, ambient separation and four task/rim lights adjusted";
  }
  public static string AddGazeUnits()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.name!="ROOM2"&&scene.name!="ROOM3")throw new Exception("Open ROOM2 or ROOM3");
   string basePath="Assets/Room3/Story/Materials/";
   Material Surface(string name,Color color,bool emission=false)
   {
    string path=basePath+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
    if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
    material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.4f);
    if(emission){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.16f,1.15f,.44f));}return material;
   }
   var graphite=Surface("Surveillance graphite",new Color(.045f,.058f,.052f));var lens=Surface("Surveillance phosphor",new Color(.22f,.56f,.34f),true);
   var root=GameObject.Find(scene.name+" - Algorithmic gaze");if(root==null)root=new GameObject(scene.name+" - Algorithmic gaze");
   Vector3[] positions=scene.name=="ROOM2"?new[]{new Vector3(-16.5f,43.2f,-150),new Vector3(5.5f,43.6f,-128)}:new[]{new Vector3(12.8f,7.2f,-12)};
   for(int i=0;i<positions.Length;i++)
   {
    if(root.transform.Find("Gaze unit "+i)!=null)continue;
    var unit=new GameObject("Gaze unit "+i);unit.transform.SetParent(root.transform,false);unit.transform.position=positions[i];unit.transform.rotation=Quaternion.LookRotation(scene.name=="ROOM2"?(i==0?Vector3.right:Vector3.left):Vector3.left);
    GameObject Mesh(string name,PrimitiveType shape,Transform parent,Vector3 local,Vector3 scale,Material material)
    {
     var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    Mesh("Wall mount",PrimitiveType.Cube,unit.transform,new Vector3(0,0,-.16f),new Vector3(.23f,.27f,.055f),graphite);
    Mesh("Support",PrimitiveType.Cube,unit.transform,new Vector3(0,-.045f,0),new Vector3(.055f,.055f,.3f),graphite);
    var pivot=new GameObject("Tracking head").transform;pivot.SetParent(unit.transform,false);pivot.localPosition=new Vector3(0,.08f,.16f);
    Mesh("Housing",PrimitiveType.Cube,pivot,Vector3.zero,new Vector3(.31f,.18f,.25f),graphite);
    var glass=Mesh("Small green lens",PrimitiveType.Cylinder,pivot,new Vector3(0,0,.137f),new Vector3(.067f,.008f,.067f),lens);glass.transform.localRotation=Quaternion.Euler(90,0,0);
    var gaze=unit.AddComponent<AlgorithmicGaze>();gaze.head=pivot;gaze.lens=glass.GetComponent<Renderer>();
   }
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return positions.Length+" silent gaze units placed in "+scene.name;
  }
  static void Point(Transform parent,string name,Vector3 position,Color color,float intensity,float range)
  {
   var t=parent.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(parent,false);}t.position=position;
   var l=t.GetComponent<Light>();if(l==null)l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;
  }
 }
}
#endif

