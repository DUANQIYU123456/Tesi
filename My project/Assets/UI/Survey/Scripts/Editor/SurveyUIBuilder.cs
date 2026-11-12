#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using Xiaoduan.Narrative;

namespace Xiaoduan.Survey.Editor
{
 public static class SurveyUIBuilder
 {
  const string Root="Assets/UI/Survey";
  const string Pack="Assets/Alebardium/Bloodlines UI/";
  static TMP_FontAsset font;
  static Material art;
  static Color Green=new Color(.38f,.94f,.59f),White=new Color(.88f,.93f,.91f),Muted=new Color(.5f,.6f,.56f);
  static Sprite frame,button;
  static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
  {
   var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;
   r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;
  }
  static void Stretch(RectTransform rect,float inset=0){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(inset,inset);rect.offsetMax=new Vector2(-inset,-inset);}
  static UnityEngine.UI.Image Image(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
  {
   var r=Rect(name,parent,anchor,pos,size);var graphic=r.gameObject.AddComponent<UnityEngine.UI.Image>();graphic.color=color;graphic.raycastTarget=false;
   if(sprite!=null){graphic.sprite=sprite;graphic.material=art;}
   return graphic;
  }
  static TMP_Text Text(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,float points,Color color,string value,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
  {
   var r=Rect(name,parent,anchor,pos,size);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=points;text.color=color;text.text=value;text.alignment=alignment;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
  }
  static UnityEngine.UI.Button Button(string name,Transform parent,Vector2 pos,Vector2 size,string label)
  {
   var image=Image(name,parent,new Vector2(.5f,.5f),pos,size,White,button);image.raycastTarget=true;
   var b=image.gameObject.AddComponent<UnityEngine.UI.Button>();b.transition=Selectable.Transition.None;
   var feedback=image.gameObject.AddComponent<SurveyButton>();feedback.image=image;
   Text("Label",image.transform,new Vector2(.5f,.5f),Vector2.zero,size-new Vector2(38,12),23,White,label,TextAlignmentOptions.Center);
   return b;
  }
  static CanvasGroup Group(RectTransform rect){return rect.gameObject.AddComponent<CanvasGroup>();}
  static Sprite Load(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(Pack+path);
  static void Perimeter(Transform panel)
  {
   if(panel.Find("Bloodlines perimeter")!=null)return;
   var image=Image("Bloodlines perimeter",panel,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.3f,.85f,.5f,.9f),frame);Stretch(image.rectTransform,8);
  }
  public static string Build()
  {
   Directory.CreateDirectory(Root+"/Resources");Directory.CreateDirectory(Root+"/Materials");
   font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
   art=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Bloodlines Green.mat");
   if(art==null){art=new Material(Shader.Find("Xiaoduan/UI/Bloodlines Monochrome"));AssetDatabase.CreateAsset(art,Root+"/Materials/Bloodlines Green.mat");}
   frame=Load("Textures/Frame/Frame_outline_v2.png");button=Load("Textures/Button/Button1/Status_Grey_Hover.png");
   var root=new GameObject("System Survey Interface / Bloodlines",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
   var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=250;
   var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
   var ui=root.AddComponent<SurveyHUD>();ui.artwork=art;ui.buttonSprite=button;ui.frameSprite=frame;
   var safe=Rect("Safe area",root.transform,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(safe);ui.safeArea=safe;
   var hud=Rect("Survey HUD",safe,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(hud);ui.hudRoot=hud;ui.hud=Group(hud);ui.hud.blocksRaycasts=false;
   // Status stays small; the centre of the first-person view remains unobstructed.
   var status=Image("Identity plate",hud,new Vector2(0,1),new Vector2(302,-116),new Vector2(520,160),new Color(.01f,.025f,.02f,.65f));
   Perimeter(status.transform);
   ui.roomLabel=Text("Scene",status.transform,new Vector2(.5f,1),new Vector2(0,-28),new Vector2(476,32),24,White,"01 / THE TOWN");
   ui.roleLabel=Text("Role",status.transform,new Vector2(.5f,1),new Vector2(0,-62),new Vector2(476,38),18,White,"RUOLO: PERITO DI SISTEMA");
   ui.integrity=Text("Integrity label",status.transform,new Vector2(.5f,1),new Vector2(0,-102),new Vector2(476,24),17,Muted,"INTEGRITÀ 100%");
   Image("Integrity track",status.transform,new Vector2(.5f,1),new Vector2(0,-134),new Vector2(476,3),new Color(.18f,.23f,.21f));
   ui.integrityFill=Image("Integrity remaining",status.transform,new Vector2(.5f,1),new Vector2(0,-134),new Vector2(476,3),Green);ui.integrityFill.type=UnityEngine.UI.Image.Type.Filled;ui.integrityFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
   ui.objective=Text("Objective",hud,new Vector2(0,1),new Vector2(302,-245),new Vector2(520,105),22,White,"");
   ui.reducedButton=Button("Motion setting",hud,Vector2.zero,new Vector2(330,45),"");var rr=ui.reducedButton.GetComponent<RectTransform>();rr.anchorMin=rr.anchorMax=new Vector2(0,0);rr.anchoredPosition=new Vector2(213,38);
   ui.reducedText=ui.reducedButton.GetComponentInChildren<TMP_Text>();ui.reducedText.fontSize=16;
   ui.fracture=Image("Growing bone border",hud,new Vector2(0,0),new Vector2(120,220),new Vector2(410,550),new Color(.2f,.8f,.4f,.3f),Load("Textures/Frame/Frame_main_menu.png")).rectTransform;
   // A small four-quarter dial, with the kit's round frame and our own numerals.
   var radar=Rect("Undark pocket watch",hud,new Vector2(1,0),new Vector2(-172,178),new Vector2(235,260));
   Image("Dial shadow",radar,new Vector2(.5f,.5f),Vector2.zero,new Vector2(206,206),new Color(.01f,.02f,.015f,.85f),AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"));
   Image("Bloodlines dial",radar,new Vector2(.5f,.5f),Vector2.zero,new Vector2(238,238),new Color(.4f,.65f,.5f,.9f),Load("Textures/Progress_Bar/Round/Progress_Bar_Round_empty_v1_v4.png"));
   ui.fragments=new TMP_Text[4];Vector2[] locations={new Vector2(0,82),new Vector2(82,0),new Vector2(0,-82),new Vector2(-82,0)};
   for(int i=0;i<4;i++)ui.fragments[i]=Text("Fragment "+i,radar,new Vector2(.5f,.5f),locations[i],new Vector2(44,30),23,Muted,new[]{"XII","III","VI","IX"}[i],TextAlignmentOptions.Center);
   var needleRoot=Rect("Detector needle",radar,new Vector2(.5f,.5f),Vector2.zero,Vector2.one);ui.needle=needleRoot;
   Image("Needle",needleRoot,new Vector2(.5f,.5f),new Vector2(0,24),new Vector2(2,49),Green);
   Image("Pin",radar,new Vector2(.5f,.5f),Vector2.zero,new Vector2(7,7),Green);
   ui.cpm=Text("Simulated detector",radar,new Vector2(.5f,.5f),new Vector2(0,-40),new Vector2(94,39),15,Green,"0015\nSIM. CPM",TextAlignmentOptions.Center);
   ui.traceCount=Text("Collected traces",radar,new Vector2(.5f,.5f),new Vector2(0,-147),new Vector2(235,28),17,White,"TRACES 0 / 4",TextAlignmentOptions.Center);
   Text("Instrument identity",radar,new Vector2(.5f,.5f),new Vector2(0,-173),new Vector2(300,26),13,Muted,"UNDARK / RESIDUAL TRACES",TextAlignmentOptions.Center);
   // Decoder sits above the old [E] prompt rather than replacing target selection.
   var decoder=Rect("Audio decoder",hud,new Vector2(.5f,0),new Vector2(0,230),new Vector2(700,132));ui.decoder=Group(decoder);
   var wr=Rect("Live waveform",decoder,new Vector2(.5f,.5f),new Vector2(0,40),new Vector2(590,38));ui.wave=wr.gameObject.AddComponent<SurveyWave>();ui.wave.color=Green;ui.wave.raycastTarget=false;
   ui.decoderLabel=Text("Decoder result",decoder,new Vector2(.5f,.5f),new Vector2(0,-1),new Vector2(700,32),20,Green,"",TextAlignmentOptions.Center);
   ui.decoderHint=Text("Tuning instruction",decoder,new Vector2(.5f,.5f),new Vector2(0,-37),new Vector2(700,28),18,White,"",TextAlignmentOptions.Center);
   ui.decoderFill=Image("Frequency progress",decoder,new Vector2(.5f,.5f),new Vector2(0,-62),new Vector2(590,3),Green);ui.decoderFill.type=UnityEngine.UI.Image.Type.Filled;ui.decoderFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
   var vision=Rect("Isotope viewfinder",hud,Vector2.zero,Vector2.zero,Vector2.zero);Stretch(vision,32);ui.visionFrame=Group(vision);ui.visionFrame.blocksRaycasts=false;
   Image("Sight perimeter",vision,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.3f,1,.55f,.7f),frame).rectTransform.AnchoredStretch(0);
   Text("Vision label",vision,new Vector2(.5f,0),new Vector2(0,40),new Vector2(600,28),18,Green,"ISOTOPE VIEW / RESIDUAL LAYER",TextAlignmentOptions.Center);
   var warning=Image("Panopticon notice",safe,new Vector2(.5f,1),new Vector2(180,-65),new Vector2(790,75),new Color(.005f,.018f,.011f,.9f),button);ui.banner=warning.gameObject;
   ui.bannerText=Text("Warning text",warning.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,50),23,Green,"",TextAlignmentOptions.Center);
   var oath=Rect("Testimony struggle",hud,new Vector2(1,.5f),new Vector2(-100,0),new Vector2(95,400));ui.oath=oath.gameObject;
   Image("Oath track",oath,new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,320),new Color(.15f,.2f,.17f));
   ui.oathFill=Image("Oath rising",oath,new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,310),Green);ui.oathFill.type=UnityEngine.UI.Image.Type.Filled;ui.oathFill.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;ui.oathFill.fillOrigin=0;
   var ob=Image("Bloodlines oath border",oath,new Vector2(.5f,.5f),Vector2.zero,new Vector2(335,42),Green,Load("Textures/Progress_Bar/Rectangle/Progress_Bar_Rectangle_empty_v4.png"));ob.rectTransform.localRotation=Quaternion.Euler(0,0,90);
   Text("Oath label",oath,new Vector2(.5f,1),new Vector2(0,-10),new Vector2(190,46),18,White,"TESTIMONY",TextAlignmentOptions.Center);
   BuildArchive(ui,safe);BuildMenu(ui,safe);
   ui.flash=Image("Scan pulse",safe,Vector2.zero,Vector2.zero,Vector2.zero,Color.clear);Stretch(ui.flash.rectTransform);
   ui.brushCursor=Image("Isotope brush cursor",safe,new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,28),Green,Load("Textures/Progress_Bar/Round/Progress_Bar_Round_empty_v1_v4.png")).rectTransform;
   Image("Brush nib",ui.brushCursor,new Vector2(.5f,.5f),Vector2.zero,new Vector2(3,10),Green);
   ui.brushCursor.gameObject.SetActive(false);
   ui.workshop=AssetDatabase.LoadAssetAtPath<Texture>(Root+"/Images/Workshop.png");ui.clinical=AssetDatabase.LoadAssetAtPath<Texture>("Assets/Room3/Story/Images/Clinical_Thesis_Fig1_2.png");ui.mollie=AssetDatabase.LoadAssetAtPath<Texture>("Assets/Room3/Story/Images/Mollie_Thesis_Fig1_3.png");
   ui.effects=Source(root,"UI effects");ui.detector=Source(root,"Quiet detector");ui.previewAudio=Source(root,"Ending previews");
   ui.tick=Clip("Detector_Tick");ui.click=AssetDatabase.LoadAssetAtPath<AudioClip>(Pack+"Audio/Click Button SFX.wav");
   ui.boot=Clip("CRT_Start");ui.chime=Clip("Trace_Chime");ui.musicBox=Clip("False_Music_Box");ui.reveal=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Room3/Story/Audio/Archive_Reveal.wav");
   ui.focusSound=Clip("Lip_Focus_Prototype");
   var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"/Resources/SurveyInterface.prefab");UnityEngine.Object.DestroyImmediate(root);
   StyleNarrativePrefab();AssetDatabase.SaveAssets();return AssetDatabase.GetAssetPath(prefab);
  }
  static AudioClip Clip(string name)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+name+".wav");
  static AudioSource Source(GameObject root,string name){var go=new GameObject(name);go.transform.SetParent(root.transform,false);var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;return source;}
  static void BuildArchive(SurveyHUD ui,Transform safe)
  {
   var blocker=Image("Interactive archive",safe,Vector2.zero,Vector2.zero,Vector2.zero,new Color(0,.008f,.006f,.83f));Stretch(blocker.rectTransform);blocker.raycastTarget=true;ui.archive=blocker.gameObject;
   var panel=Image("Archive plate",blocker.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1130,790),new Color(.018f,.028f,.025f,.99f));Perimeter(panel.transform);
   ui.archiveTitle=Text("Title",panel.transform,new Vector2(.5f,.5f),new Vector2(0,324),new Vector2(1024,58),34,White,"");
   Image("Header rule",panel.transform,new Vector2(.5f,.5f),new Vector2(0,280),new Vector2(1024,2),Green);
   var official=Rect("Official layer",panel.transform,new Vector2(.5f,.5f),new Vector2(-176,162),new Vector2(650,180));ui.officialLayer=Group(official);
   ui.officialText=Text("Official text",official,new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,180),23,White,"");
   var truth=Rect("Historical layer",panel.transform,new Vector2(.5f,.5f),new Vector2(-176,-55),new Vector2(650,247));ui.truthLayer=Group(truth);
   ui.truthText=Text("Historical text",truth,new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,247),23,Green,"");
   var mask=Rect("Redaction coating",truth,new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,247));ui.scratch=mask.gameObject.AddComponent<SurveyScratch>();ui.scratch.color=new Color(.005f,.012f,.009f,.99f);ui.scratch.raycastTarget=true;
   var picture=Rect("Historical photograph",panel.transform,new Vector2(.5f,.5f),new Vector2(359,44),new Vector2(305,330));ui.archivePicture=picture.gameObject.AddComponent<RawImage>();ui.archivePicture.raycastTarget=false;
   Text("Photograph label",panel.transform,new Vector2(.5f,.5f),new Vector2(355,-144),new Vector2(300,30),14,Muted,"RESEARCH COLLECTION",TextAlignmentOptions.Center);
   ui.revealLabel=Text("Reveal progress",panel.transform,new Vector2(.5f,.5f),new Vector2(-176,-224),new Vector2(650,32),19,Green,"");
   ui.focusMarker=Image("Unsteady focus ring",panel.transform,new Vector2(.5f,.5f),new Vector2(-176,-192),new Vector2(34,34),Green,Load("Textures/Progress_Bar/Round/Progress_Bar_Round_empty_v1_v4.png")).rectTransform;
   ui.sourceText=Text("Sources",panel.transform,new Vector2(.5f,.5f),new Vector2(0,-290),new Vector2(1024,73),16,Muted,"");
   ui.archiveHint=Text("Interaction",panel.transform,new Vector2(.5f,.5f),new Vector2(-80,-355),new Vector2(860,34),18,White,"");
   ui.closeButton=Button("Close archive",panel.transform,new Vector2(464,-351),new Vector2(125,42),"CLOSE");ui.closeButton.GetComponentInChildren<TMP_Text>().fontSize=17;
  }
  static void BuildMenu(SurveyHUD ui,Transform safe)
  {
   var background=Image("Inspection menu",safe,Vector2.zero,Vector2.zero,Vector2.zero,new Color(.003f,.008f,.006f,1));Stretch(background.rectTransform);background.raycastTarget=true;ui.menu=background.gameObject;
   var slit=Image("Breathing green fracture",background.transform,new Vector2(.5f,.5f),new Vector2(0,65),new Vector2(4,690),new Color(.24f,.8f,.42f,.8f));ui.menuSlit=slit.rectTransform;
   Image("Soft fracture halo",slit.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(60,700),new Color(.1f,.8f,.35f,.04f));
   var perimeter=Image("Bloodlines main frame",background.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1090,990),new Color(.26f,.67f,.39f,.8f),Load("Textures/Frame/Frame_main_menu.png"));
   Text("Project title",background.transform,new Vector2(.5f,.5f),new Vector2(0,215),new Vector2(1040,160),68,White,"LUCE VERDE\nNELLE OSSA",TextAlignmentOptions.Center);
   Text("Menu subtitle",background.transform,new Vector2(.5f,.5f),new Vector2(0,73),new Vector2(700,70),20,Muted,"A RESIDUAL HISTORY / AN INTERACTIVE ARCHIVE",TextAlignmentOptions.Center);
   ui.startButton=Button("Begin inspection",background.transform,new Vector2(0,-120),new Vector2(540,86),"AVVIA ISPEZIONE / BEGIN INSPECTION");
   ui.menuMotionButton=Button("Menu reduced motion",background.transform,new Vector2(0,-230),new Vector2(410,54),"REDUCED MOTION [M]");
   Text("Controls",background.transform,new Vector2(.5f,.5f),new Vector2(0,-325),new Vector2(900,80),18,Muted,"WASD  MOVE   /   MOUSE  LOOK   /   E  EXAMINE\nRMB  ISOTOPE VIEW   /   F  REVEAL   /   ESC  CLOSE",TextAlignmentOptions.Center);
   Text("Authorship",background.transform,new Vector2(.5f,0),new Vector2(0,53),new Vector2(1100,40),15,Muted,"DUAN QIYU  /  NABA  /  FICTIONAL SYSTEM · HISTORICAL RESEARCH",TextAlignmentOptions.Center);
  }
  static void StyleNarrativePrefab()
  {
   const string path="Assets/UI/Narrative/NarrativeInterface.prefab";var root=PrefabUtility.LoadPrefabContents(path);var ui=root.GetComponentInChildren<NarrativeHUD>(true);
   if(ui!=null)
   {
    var panel=ui.cardRect.GetComponent<UnityEngine.UI.Image>();if(panel!=null){panel.color=new Color(.015f,.027f,.021f,.98f);Perimeter(panel.transform);}
    var prompt=ui.promptRect.GetComponent<UnityEngine.UI.Image>();if(prompt!=null){prompt.sprite=button;prompt.material=art;prompt.color=new Color(.35f,.5f,.4f,.9f);}
   }
   PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
  }
  public static string InstallScenes()
  {
   foreach(string name in new[]{"ROOM1","ROOM2","ROOM3"})
   {
    var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity",OpenSceneMode.Single);
    if(name=="ROOM1")InstallTown(scene);else if(name=="ROOM2")InstallPrison(scene);else InstallArchive(scene);
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   return "Saved ROOM1, ROOM2 and ROOM3 bindings";
  }
  static Material Glow(string name,Color color)
  {
   string path=Root+"/Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
   material.SetColor("_BaseColor",color);material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*2.5f);return material;
  }
  static SurveyNode Node(GameObject target,SurveyNodeKind kind,Vector3? focus=null,float reach=5)
  {
   var node=target.GetComponent<SurveyNode>();if(node==null)node=target.AddComponent<SurveyNode>();node.kind=kind;node.reach=reach;
   if(focus.HasValue){var marker=target.transform.Find("Survey focus");if(marker==null){var go=new GameObject("Survey focus");marker=go.transform;marker.SetParent(target.transform,false);}marker.position=focus.Value;node.focus=marker;}
   return node;
  }
  static GameObject Cube(string name,Transform parent,Vector3 pos,Vector3 scale,Material material,bool collider=false)
  {
   var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;
   if(!collider)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
  }
  static void InstallTown(Scene scene)
  {
   var portal=UnityEngine.Object.FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).FirstOrDefault(x=>x.sceneName=="ROOM2");
   if(portal==null)throw new Exception("ROOM1 portal missing");Node(portal.gameObject,SurveyNodeKind.Portal);
   var previous=GameObject.Find("Isotope footprints / RMB");if(previous!=null)UnityEngine.Object.DestroyImmediate(previous);
   var root=new GameObject("Isotope footprints / RMB");root.AddComponent<SurveyTrace>();var material=Glow("Residual paint",new Color(.13f,.65f,.3f));
   var player=GameObject.FindGameObjectWithTag("Player");Vector3 start=player!=null?player.transform.position:new Vector3(28.5f,2,-4.8f);
   Vector3 end=portal.transform.position;Vector3 direction=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;
   float distance=Vector3.Distance(start,end);int steps=Mathf.Clamp(Mathf.CeilToInt(distance/.65f),6,70);
   for(int i=0;i<steps;i++)
   {
    float t=(i+.5f)/steps;Vector3 point=Vector3.Lerp(start,end,t)+Vector3.Cross(direction,Vector3.up)*(i%2==0?.13f:-.13f);
    var hits=Physics.RaycastAll(point+Vector3.up*12,Vector3.down,30,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).OrderBy(x=>Mathf.Abs(x.point.y-start.y));
    var hit=hits.FirstOrDefault(x=>!x.collider.transform.IsChildOf(player!=null?player.transform:root.transform)&&Vector3.Angle(x.normal,Vector3.up)<45);
    if(hit.collider==null)continue;
    var foot=Cube("Residual footprint "+i,root.transform,hit.point+Vector3.up*.014f,new Vector3(.085f,.008f,.23f),material);
    foot.transform.rotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Euler(0,i%2==0?-8:8,0);
    Cube("Heel trace "+i,root.transform,foot.transform.position-direction*.145f,new Vector3(.075f,.008f,.06f),material);
   }
  }
  static void InstallPrison(Scene scene)
  {
   foreach(var item in UnityEngine.Object.FindObjectsByType<NarrativeInspectable>(FindObjectsSortMode.None))Node(item.gameObject,SurveyNodeKind.Bottle,item.FocusBounds.center);
   foreach(var emitter in UnityEngine.Object.FindObjectsByType<LaborEchoEmitter>(FindObjectsSortMode.None))
   {
    if(emitter.name.Contains("Left_Watches"))Node(emitter.gameObject,SurveyNodeKind.Watches,null,8);
    if(emitter.name.Contains("Right_Stamping"))Node(emitter.gameObject,SurveyNodeKind.Stamping,null,8);
   }
   var old=GameObject.Find("Concealed medical file / VI");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
   var root=new GameObject("Concealed medical file / VI");root.transform.position=new Vector3(-13.5f,36.95f,-125);
   var material=Glow("Archive paper",new Color(.11f,.23f,.16f));
   Cube("File stand",root.transform,new Vector3(0,.5f,0),new Vector3(.72f,1,.52f),Glow("Archive plinth",new Color(.055f,.065f,.06f)),true);
   var file=Cube("Redacted medical folio",root.transform,new Vector3(0,1.03f,0),new Vector3(.48f,.055f,.36f),material,true);
   var entry=AssetDatabase.LoadAssetAtPath<NarrativeEntry>(Root+"/Medical Record.asset");if(entry==null){entry=ScriptableObject.CreateInstance<NarrativeEntry>();AssetDatabase.CreateAsset(entry,Root+"/Medical Record.asset");}
   entry.title="A disputed diagnosis";entry.category="ARCHIVE VI";entry.body="Hold F or scratch the concealed layer.";EditorUtility.SetDirty(entry);
   var inspect=file.AddComponent<NarrativeInspectable>();inspect.entry=entry;inspect.focusRenderer=file.GetComponent<Renderer>();inspect.interactionDistance=3.3f;Node(file,SurveyNodeKind.Medical,file.transform.position);
   var scanReveal=file.AddComponent<SurveyScanReveal>();scanReveal.surface=file.GetComponent<Renderer>();scanReveal.inspectable=inspect;
   var portal=UnityEngine.Object.FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None).FirstOrDefault(x=>x.sceneName=="ROOM3");
   if(portal!=null){var gate=portal.GetComponent<SurveyPortalGate>();if(gate==null)gate=portal.gameObject.AddComponent<SurveyPortalGate>();gate.portal=portal;Node(portal.gameObject,SurveyNodeKind.Portal);}
  }
  static void InstallArchive(Scene scene)
  {
   foreach(var node in UnityEngine.Object.FindObjectsByType<Xiaoduan.Room3.Room3StoryNode>(FindObjectsSortMode.None))
   {
    if(node.kind==Xiaoduan.Room3.Room3StoryNode.Kind.MedicalRecord)Node(node.gameObject,SurveyNodeKind.Medical,node.focus.position);
    if(node.kind==Xiaoduan.Room3.Room3StoryNode.Kind.CourtRecord)Node(node.gameObject,SurveyNodeKind.Court,node.focus.position);
   }
  }
  static void AnchoredStretch(this RectTransform rect,float inset){Stretch(rect,inset);}
 }
}
#endif
