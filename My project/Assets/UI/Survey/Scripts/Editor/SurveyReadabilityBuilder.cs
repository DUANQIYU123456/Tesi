#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.TextCore.LowLevel;
namespace Xiaoduan.Survey.Editor
{
 public static class SurveyReadabilityBuilder
 {
  const string Root="Assets/UI/Survey";
  static TMP_FontAsset body,title,mono;
  static Material art;static Sprite button;
  static Color White=new Color(.9f,.94f,.92f),Green=new Color(.4f,.94f,.6f);
  static TMP_FontAsset Bake(string source,string name)
  {
   string path=Root+"/Fonts/"+name+" SDF.asset";var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);if(asset!=null)return asset;
   var font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/"+source+".ttf");
   asset=TMP_FontAsset.CreateFontAsset(font,90,9,GlyphRenderMode.SDF16,2048,2048,AtlasPopulationMode.Dynamic,false);
   asset.name=name+" SDF";
   string chars=new string(Enumerable.Range(32,95).Concat(Enumerable.Range(160,96)).Concat(new[]{0x2013,0x2014,0x2018,0x2019,0x201C,0x201D,0x2026,0x2588}).Select(x=>(char)x).ToArray());
   asset.TryAddCharacters(chars,out string missing);asset.atlasPopulationMode=AtlasPopulationMode.Static;
   asset.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset")};
   AssetDatabase.CreateAsset(asset,path);AssetDatabase.AddObjectToAsset(asset.material,asset);
   foreach(var texture in asset.atlasTextures)if(texture!=null)AssetDatabase.AddObjectToAsset(texture,asset);
   EditorUtility.SetDirty(asset);return asset;
  }
  static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
  {
   var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;
  }
  static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
  {
   var r=Rect(name,parent,pos,size);var i=r.gameObject.AddComponent<UnityEngine.UI.Image>();i.color=color;i.raycastTarget=false;i.sprite=sprite;if(sprite!=null)i.material=art;return i;
  }
  static TMP_Text Label(string name,Transform parent,Vector2 pos,Vector2 size,float points,string value,bool heading=false)
  {
   var r=Rect(name,parent,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=heading?title:body;t.fontSize=points;t.text=value;t.color=White;t.raycastTarget=false;t.alignment=TextAlignmentOptions.Center;return t;
  }
  static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string value)
  {
   var i=Image(name,parent,pos,size,White,button);i.raycastTarget=true;var b=i.gameObject.AddComponent<UnityEngine.UI.Button>();b.transition=Selectable.Transition.None;var feedback=i.gameObject.AddComponent<SurveyButton>();feedback.image=i;
   Label("Label",i.transform,Vector2.zero,size-new Vector2(34,10),23,value,true);return b;
  }
  static UnityEngine.UI.Slider Slider(string name,Transform parent,Vector2 position,float min,float max)
  {
   var r=Rect(name,parent,position,new Vector2(560,34));var slider=r.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.minValue=min;slider.maxValue=max;
   Image("Track",r,Vector2.zero,new Vector2(560,4),new Color(.2f,.3f,.25f));
   var fillArea=Rect("Fill area",r,Vector2.zero,new Vector2(560,4));
   var fill=Image("Fill",fillArea,Vector2.zero,Vector2.zero,Green);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;slider.fillRect=fill.rectTransform;
   var handleArea=Rect("Handle area",r,Vector2.zero,new Vector2(536,0));
   var handle=Image("Handle",handleArea,Vector2.zero,new Vector2(24,24),Green,AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));handle.raycastTarget=true;
   var trackTarget=r.gameObject.AddComponent<UnityEngine.UI.Image>();trackTarget.color=Color.clear;trackTarget.raycastTarget=true;
   slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;slider.value=1;return slider;
  }
  public static string Install()
  {
   body=Bake("IBMPlexSans-Regular","IBM Plex Sans Regular");title=Bake("IBMPlexSans-SemiBold","IBM Plex Sans SemiBold");mono=Bake("IBMPlexMono-Medium","IBM Plex Mono Medium");
   body.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{mono};title.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{mono};mono.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
   EditorUtility.SetDirty(body);EditorUtility.SetDirty(title);EditorUtility.SetDirty(mono);
   const string path=Root+"/Resources/SurveyInterface.prefab";var root=PrefabUtility.LoadPrefabContents(path);var hud=root.GetComponent<SurveyHUD>();art=hud.artwork;button=hud.buttonSprite;
   var type=root.GetComponent<SurveyTypography>();if(type==null)type=root.AddComponent<SurveyTypography>();type.bodyFont=body;type.titleFont=title;type.instrumentFont=mono;
   hud.effects.ignoreListenerPause=true;
   foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))
   {
    var chosen=text==hud.cpm||text==hud.integrity||text==hud.traceCount||System.Array.IndexOf(hud.fragments,text)>=0?mono:text.fontSize>=30||text.GetComponentInParent<UnityEngine.UI.Button>()!=null?title:body;
    text.font=chosen;text.fontSharedMaterial=chosen.material;text.enableAutoSizing=false;
   }
   var pause=root.GetComponent<SurveyPause>();
   if(pause==null)
   {
    pause=root.AddComponent<SurveyPause>();
    var backdrop=Image("Pause and reading settings",hud.safeArea,Vector2.zero,Vector2.zero,new Color(.004f,.012f,.009f,.96f));backdrop.raycastTarget=true;
    var rect=backdrop.rectTransform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;pause.panel=backdrop.gameObject;
    var plate=Image("Pause plate",backdrop.transform,Vector2.zero,new Vector2(1050,850),new Color(.015f,.028f,.024f,.99f));
    var frame=Image("Bloodlines perimeter",plate.transform,Vector2.zero,new Vector2(1034,834),new Color(.3f,.8f,.5f),hud.frameSprite);
    Label("Pause title",plate.transform,new Vector2(0,344),new Vector2(900,60),42,"SURVEY PAUSED",true);
    Label("Pause note",plate.transform,new Vector2(0,287),new Vector2(900,45),22,"Take your time. The archive will wait.");
    pause.volumeLabel=Label("Volume label",plate.transform,new Vector2(0,218),new Vector2(700,35),22,"");
    pause.volumeSlider=Slider("Master volume",plate.transform,new Vector2(0,167),0,1);
    pause.sensitivityLabel=Label("Sensitivity label",plate.transform,new Vector2(0,100),new Vector2(700,35),22,"");
    pause.sensitivitySlider=Slider("Look sensitivity",plate.transform,new Vector2(0,49),.4f,1.8f);
    pause.motionButton=Button("Reduce motion",plate.transform,new Vector2(-228,-37),new Vector2(420,57),"");pause.motionLabel=pause.motionButton.GetComponentInChildren<TMP_Text>();
    pause.readingButton=Button("Reading size",plate.transform,new Vector2(228,-37),new Vector2(420,57),"");pause.readingLabel=pause.readingButton.GetComponentInChildren<TMP_Text>();
    Label("Pause controls",plate.transform,new Vector2(0,-135),new Vector2(920,76),21,"WASD MOVE  /  E EXAMINE  /  F REVEAL\nRMB ISOTOPE VIEW  /  M REDUCED MOTION  /  ESC RETURN");
    pause.resumeButton=Button("Resume survey",plate.transform,new Vector2(0,-247),new Vector2(440,73),"RESUME SURVEY / ESC");
    pause.restartButton=Button("Restart survey",plate.transform,new Vector2(0,-341),new Vector2(380,55),"RESTART FROM THE TOWN");
    pause.panel.SetActive(false);
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
   var theme=AssetDatabase.LoadAssetAtPath<Xiaoduan.Narrative.NarrativeTheme>("Assets/UI/Narrative/NarrativeTheme.asset");if(theme!=null){theme.font=body;EditorUtility.SetDirty(theme);}
   AssetDatabase.SaveAssets();return "Three static IBM Plex fonts and pause/readability controls installed";
  }
 }
}
#endif
