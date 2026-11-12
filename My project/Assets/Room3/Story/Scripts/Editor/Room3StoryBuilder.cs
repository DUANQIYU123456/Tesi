#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Unity.Cinemachine;
using Xiaoduan.Narrative;
using Xiaoduan.Room3;

public static class Room3StoryBuilder
{
    const string Folder="Assets/Room3/Story";
    static Transform root;
    static Material concrete,dark,metal,green,white,red,glass,bone,shadow,paper;
    static Mesh block;
    static TMP_FontAsset font;
    static Room3StoryFlow flow;
    static Vector3 P(float x,float y,float z)=>new Vector3(x,y,z);
    static Transform Group(string name,Transform parent=null)
    {var t=new GameObject(name).transform;t.SetParent(parent??root,false);return t;}
    static GameObject Box(string name,Vector3 p,Vector3 size,Material mat,Transform parent=null,bool solid=false,Vector3 rotation=default)
    {
        var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent??root,false);g.transform.localPosition=p;g.transform.localScale=size;g.transform.localEulerAngles=rotation;
        g.GetComponent<MeshFilter>().sharedMesh=block;g.GetComponent<Renderer>().sharedMaterial=mat;
        if(mat==green||mat==white)g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        if(solid)g.AddComponent<BoxCollider>();return g;
    }
    static TextMeshPro Text(string text,Vector3 p,float size,Transform parent=null,float width=10)
    {
        var t=Group("Sign / "+text,parent);t.localPosition=p;var label=t.gameObject.AddComponent<TextMeshPro>();label.font=font;label.text=text;label.fontSize=size;label.color=new Color(.7f,.88f,.77f);label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(width,3);return label;
    }
    static Material Lit(string name,Color color,float emission=0)
    {
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.22f);
        if(emission>0){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*emission);}
        AssetDatabase.CreateAsset(mat,Folder+"/Materials/"+name+".mat");return mat;
    }
    static void Link(Vector3 a,Vector3 b,float width,Material mat,Transform parent)
    {var g=Box("Structural link",(a+b)/2,P(width,width,Vector3.Distance(a,b)),mat,parent);g.transform.localRotation=Quaternion.LookRotation(b-a);}
    static Transform Figure(string name,Vector3 position,Transform parent,bool ribLight,out GameObject glow)
    {
        var figure=Group(name,parent);figure.localPosition=position;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var tri=new List<int>();
        float[] ys={0,.12f,.55f,1.1f,1.36f,1.58f,1.72f,1.8f};
        float[] radii={.33f,.36f,.32f,.2f,.25f,.34f,.27f,.11f};
        for(int k=0;k<ys.Length;k++)for(int i=0;i<=24;i++)
        {float a=i*Mathf.PI*2/24;vertices.Add(P(Mathf.Cos(a)*radii[k],ys[k],Mathf.Sin(a)*radii[k]*.6f));uv.Add(new Vector2(i/24f,k/(ys.Length-1f)));}
        for(int k=0;k<ys.Length-1;k++)for(int i=0;i<24;i++)
        {int a=k*25+i,b=a+25;tri.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        var mesh=new Mesh{name="Clothed memorial silhouette"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tri,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/Meshes/"+name.Replace(' ','_')+".asset";AssetDatabase.CreateAsset(mesh,path);
        var body=new GameObject("Work dress",typeof(MeshFilter),typeof(MeshRenderer));body.transform.SetParent(figure,false);body.GetComponent<MeshFilter>().sharedMesh=mesh;body.GetComponent<Renderer>().sharedMaterial=shadow;
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Bowed head";head.transform.SetParent(figure,false);head.transform.localPosition=P(0,1.96f,-.08f);head.transform.localScale=P(.29f,.38f,.3f);head.GetComponent<Renderer>().sharedMaterial=shadow;UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());
        for(int side=-1;side<=1;side+=2){Link(P(side*.29f,1.63f,0),P(side*.35f,.96f,-.04f),.115f,shadow,figure);Link(P(side*.35f,.96f,-.04f),P(side*.29f,.67f,-.08f),.085f,shadow,figure);}
        var bones=Group("Symbolic phosphor ribs",figure);for(int i=0;i<5;i++)Box("Rib trace",P(0,1.13f+i*.085f,-.155f),P(.22f-i*.018f,.013f,.016f),green,bones);
        Box("Spinal trace",P(0,1.22f,-.17f),P(.016f,.65f,.015f),green,bones);glow=bones.gameObject;glow.SetActive(ribLight);
        var motion=figure.gameObject.AddComponent<Room3Motion>();motion.bobAmplitude=.055f;motion.bobFrequency=.35f;motion.phase=position.x;
        return figure;
    }
    static void Corridor(Transform oldRoot)
    {
        var old=oldRoot.Find("01 - Isolation corridor");
        foreach(string name in new[]{"Observation glass","Right wall","Corridor roof"}){var item=old.Find(name);if(item!=null)item.gameObject.SetActive(false);}
        foreach(Transform t in old)if(t.name=="Observation mullion")t.gameObject.SetActive(false);
        var zone=Group("ZONE 1 - Cleanroom and lead screen");
        Box("Raised office slab",P(8.7f,-.3f,-14),P(10,.6f,29),concrete,zone,true);
        Box("Tall corridor ceiling",P(0,9.6f,-15),P(8,.7f,30),dark,zone);
        Box("Tall left wall",P(-4,6,-15),P(.6f,7,30),concrete,zone);
        // Two breaches force the player to cross to the other side of the partition.
        Box("Observation pane A",P(3.8f,4.7f,-23),P(.045f,9.4f,10),glass,zone,true);
        Box("Observation pane B",P(3.8f,4.7f,-8),P(.045f,9.4f,10),glass,zone,true);
        foreach(float z in new[]{-28f,-23,-18,-13,-8,-3})Box("Curtain wall mullion",P(3.8f,4.7f,z),P(.13f,9.4f,.13f),metal,zone,true);
        Box("Collapsed corridor obstruction",P(-.1f,1.65f,-9),P(7.1f,3.3f,2.5f),concrete,zone,true,P(0,0,2));
        for(int i=0;i<7;i++){Box("Path inlay",P(5.45f,.025f,-15+i*2),P(.035f,.03f,1.2f),green,zone);}
        Box("Return path inlay",P(2.7f,.03f,-2),P(5.5f,.03f,.035f),green,zone);
        Text("RETURN TO CENTRAL ROUTE",P(2.6f,1.2f,-1.45f),.19f,zone,5);
        for(int i=0;i<3;i++)
        {
            float z=-23+i*6;
            Box("Cleanroom desk",P(10,1,z),P(3.6f,.15f,1.4f),metal,zone,true);
            for(int side=-1;side<=1;side+=2)Box("Desk support",P(10+side*1.5f,.5f,z),P(.1f,1,.8f),metal,zone,true);
            Box("Monitor",P(10,1.55f,z+.25f),P(1,.75f,.12f),dark,zone);
            Box("Screen surface",P(10,1.55f,z+.17f),P(.85f,.55f,.015f),white,zone);
            Box("Backlight vertical",P(13.2f,5,z),P(.12f,8,.12f),white,zone);
        }
        Text("CLEANROOM / SCHERMO DI PIOMBO",P(0,7.4f,-17),.5f,zone,13);
        Text("AUTHORIZED OBSERVATION ROUTE",P(0,6.65f,-17),.25f,zone,12);
        Text("BREACH / CONTINUE THROUGH GLASS",P(2.4f,2.65f,-16),.24f,zone,6);
        var hidden=new List<GameObject>();
        for(int i=0;i<3;i++){var f=Figure("Worker echo "+i,P(10,2.7f,-23+i*6),zone,false,out var _);f.localEulerAngles=P(0,-90,0);hidden.Add(f.gameObject);}
        flow.workerFigures=hidden.ToArray();
        // The existing first archive plinth is reused and moved onto the forced route.
        var terminal=old.Find("Archive terminal - 03-01");terminal.SetParent(zone,true);terminal.position=P(5.5f,0,-3.8f);
        var node=terminal.gameObject.AddComponent<Room3StoryNode>();node.kind=Room3StoryNode.Kind.MedicalRecord;node.label="VI / A disputed diagnosis";node.focus=terminal.Find("Archive display");
        terminal.GetComponent<NarrativeInspectable>().enabled=false;
    }
    static ParticleSystem Papers(Transform parent,Vector3 position,Vector3 size,bool greenParticles=false)
    {
        var g=Group(greenParticles?"Memorial traces":"Suspended paper archive",parent);g.localPosition=position;
        var ps=g.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.loop=true;main.duration=16;main.startLifetime=14;main.maxParticles=128;main.startSpeed=.18f;main.startSize=greenParticles? .045f:.28f;main.startColor=greenParticles?new Color(.5f,1,.55f,.65f):new Color(.7f,.74f,.66f,.9f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=!greenParticles;
        var emission=ps.emission;emission.rateOverTime=greenParticles?6:7;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=size;
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;velocity.x=.08f;velocity.y=greenParticles? .22f:.12f;velocity.z=-.05f;
        var rotation=ps.rotationOverLifetime;rotation.enabled=true;rotation.z=.4f;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=greenParticles?green:paper;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
        return ps;
    }
    static void Bridge(Transform oldRoot)
    {
        oldRoot.Find("02 - Suspended causeway").gameObject.SetActive(false);
        var zone=Group("ZONE 2 - Alignment bridge");
        Box("Calibration landing",P(0,-.3f,3.7f),P(7,.6f,8.6f),concrete,zone,true);
        Box("Far landing",P(0,-.3f,30),P(9,.6f,9),concrete,zone,true);
        Box("Stand marker",P(0,.015f,4),P(1.1f,.03f,1.1f),metal,zone);
        for(int s=-1;s<=1;s+=2){Box("Calibration inlay",P(s*.55f,.04f,4),P(.03f,.02f,1.2f),green,zone);}
        var calibration=Group("Calibration position",zone);calibration.localPosition=P(0,.1f,4);flow.calibrationPoint=calibration;
        var target=Group("Alignment target",zone);target.localPosition=P(0,2.1f,17);flow.calibrationTarget=target;
        // A visible reticle in the scene corresponds to the projected HUD marker.
        for(int s=-1;s<=1;s+=2){Box("World calibration cross",P(s*.2f,2.1f,17),P(.18f,.018f,.018f),green,zone);Box("World calibration cross",P(0,2.1f+s*.2f,17),P(.018f,.18f,.018f),green,zone);}
        flow.bridgeModules=new Transform[3];flow.lockedBridgePositions=new Vector3[3];var colliders=new List<Collider>();
        Vector3[] origins={P(-3,3,11),P(3,5,17),P(-2,3.5f,23)};Vector3[] rotations={P(15,12,23),P(-13,-18,-27),P(10,20,13)};
        for(int i=0;i<3;i++)
        {
            var module=Group("Bridge module "+(i+1),zone);module.localPosition=origins[i];module.localEulerAngles=rotations[i];
            var slab=Box("Lockable concrete deck",Vector3.zero,P(4,.6f,6),concrete,module,true);colliders.Add(slab.GetComponent<Collider>());
            for(int s=-1;s<=1;s+=2)
            {
                Box("Circuit alignment rail",P(s*1.7f,.32f,0),P(.035f,.025f,5.9f),green,module);
                var boundary=new GameObject("Invisible fall guard",typeof(BoxCollider));boundary.transform.SetParent(module,false);boundary.transform.localPosition=P(s*2,1.35f,0);boundary.GetComponent<BoxCollider>().size=P(.12f,3,6);colliders.Add(boundary.GetComponent<Collider>());
                Box("Deck edge girder",P(s*1.8f,-.5f,0),P(.2f,.9f,6),metal,module);
            }
            flow.bridgeModules[i]=module;flow.lockedBridgePositions[i]=P(0,-.3f,11+i*6);
        }
        flow.bridgeColliders=colliders.ToArray();
        var blocker=new GameObject("Bridge locked boundary",typeof(BoxCollider));blocker.transform.SetParent(zone,false);blocker.transform.localPosition=P(0,2,7.75f);blocker.GetComponent<BoxCollider>().size=P(8,5,.18f);flow.bridgeBarrier=blocker.GetComponent<Collider>();
        Text("ALIGN / RETAIN / CROSS",P(0,1,7.4f),.24f,zone);
        Papers(zone,P(0,5,16),P(24,13,25));
        for(int s=-1;s<=1;s+=2)for(int i=0;i<3;i++)
        {
            var g=Group("Suspended stair mass",zone);g.localPosition=P(s*(8+i*2),5+i*6,10+i*8);g.localEulerAngles=P(20,s*30,s*(25+i*12));
            Box("Concrete spine",P(0,0,0),P(3.8f,1.5f,10),concrete,g);
            for(int j=0;j<9;j++)Box("Cantilever stair",P(0,j*.25f,-4+j),P(5,.35f,1),concrete,g);
            var motion=g.gameObject.AddComponent<Room3Motion>();motion.angularVelocity=P(0,s*.1f,s*.12f);motion.bobAmplitude=.1f;
        }
    }
    static void Oath(Transform oldRoot)
    {
        var zone=Group("ZONE 3 - Testimony gate");
        Box("Testimony platform",P(0,-.3f,37.5f),P(11,.6f,14),metal,zone,true);
        Box("Oath lintel",P(0,6.2f,42),P(6.5f,.6f,.8f),dark,zone,true);
        for(int s=-1;s<=1;s+=2){Box("Gate pier",P(s*3.1f,3,42),P(.55f,6.5f,.8f),dark,zone,true);Box("Gate edge light",P(s*2.8f,3,41.55f),P(.05f,6,.04f),green,zone);}
        flow.gateLeaves=new Transform[2];
        for(int i=0;i<2;i++){var leaf=Box("Sealed gate leaf "+i,P(i==0?-1.3f:1.3f,3,42),P(2.6f,6,.35f),metal,zone);flow.gateLeaves[i]=leaf.transform;}
        var barrier=new GameObject("Testimony gate boundary",typeof(BoxCollider));barrier.transform.SetParent(zone,false);barrier.transform.localPosition=P(0,2.7f,42);barrier.GetComponent<BoxCollider>().size=P(6,6,.4f);flow.oathBarrier=barrier.GetComponent<Collider>();
        var oath=Group("Oath position",zone);oath.localPosition=P(0,.1f,38);flow.oathPoint=oath;
        var glow=new List<GameObject>();string[] names={"Grace Fryer","Katherine Schaub","Edna Hussman","Albina Larice","Quinta McDonald"};
        for(int i=0;i<5;i++)
        {
            var f=Figure("Witness "+names[i],P((i-2)*1.8f,7.8f,42),zone,false,out var bones);f.localScale=Vector3.one*1.25f;glow.Add(bones);
            Text(names[i],P((i-2)*1.8f,7.1f,41.4f),.17f,zone,2);
        }
        flow.boneLights=glow.ToArray();
        Box("Testimony backlight",P(0,9.5f,45),P(11,6,.15f),white,zone);
        Text("PRESTARE GIURAMENTO",P(0,6.8f,41.5f),.38f,zone);
        var terminal=oldRoot.Find("04 - The dial and archive core/Archive terminal - 03-02");terminal.SetParent(zone,true);terminal.position=P(-3,0,33);
        terminal.GetComponent<NarrativeInspectable>().enabled=false;var n=terminal.gameObject.AddComponent<Room3StoryNode>();n.kind=Room3StoryNode.Kind.CourtRecord;n.label="IX / Five women doomed to die";n.focus=terminal.Find("Archive display");
    }
    static void Core(Transform oldRoot)
    {
        var old=oldRoot.Find("04 - The dial and archive core");old.localPosition=P(0,0,55);
        old.Find("Town exit threshold").gameObject.SetActive(false);
        var zone=Group("ZONE 4 - Silicon bone abyss");
        Box("Narrow core beam",P(0,-.22f,67),P(2.2f,.44f,46),metal,zone,true);
        for(int s=-1;s<=1;s+=2)
        {
            Box("Beam inset",P(s*1.02f,.025f,67),P(.025f,.03f,46),green,zone);
            var g=new GameObject("Beam safety boundary",typeof(BoxCollider));g.transform.SetParent(zone,false);g.transform.localPosition=P(s*1.65f,1.3f,67);g.GetComponent<BoxCollider>().size=P(.12f,3,46);
        }
        var helix=Group("Double helix memorial tower",zone);flow.monument=helix;
        for(int side=0;side<2;side++)for(int i=0;i<22;i++)
        {
            float a=i*.36f+side*Mathf.PI;float y=i*2.6f+2;float radius=14-i*.12f;
            Vector3 p=P(Mathf.Sin(a)*radius,y,112+Mathf.Cos(a)*radius);
            var rib=Group("Porous vertebra "+side+"-"+i,helix);rib.localPosition=p;rib.localEulerAngles=P(0,a*Mathf.Rad2Deg,side==0?-12:12);
            Box("Vertebral body",Vector3.zero,P(3.3f,1.65f,2.4f),bone,rib);
            Box("Rib arch left",P(-2.1f,.15f,.5f),P(2.7f,.6f,1),bone,rib,false,P(0,16,10));
            Box("Rib arch right",P(2.1f,.15f,.5f),P(2.7f,.6f,1),bone,rib,false,P(0,-16,-10));
            Box("Embedded circuit",P(0,-.15f,-1.23f),P(.035f,1.3f,.025f),green,rib);
        }
        var turn=helix.gameObject.AddComponent<Room3Motion>();turn.angularVelocity=P(0,.32f,0);turn.bobAmplitude=0;
        // Rotate around the tower's actual center instead of orbiting the whole level.
        foreach(Transform t in helix)t.localPosition-=P(0,0,112);helix.localPosition=P(0,0,112);
        var chip=Group("Pulsing archive core",zone);chip.localPosition=P(0,45,112);Box("Silicon core",Vector3.zero,P(4,4,3),metal,chip);
        for(int s=-1;s<=1;s+=2)for(int i=0;i<9;i++){Box("Core pin",P(s*2.5f,-1.7f+i*.4f,0),P(1,.09f,.12f),green,chip);Box("Core circuit",P(-1.6f+i*.4f,s*2.05f,0),P(.08f,.06f,2.6f),green,chip);}
        var motion=chip.gameObject.AddComponent<Room3Motion>();motion.angularVelocity=P(2,4,0);motion.bobAmplitude=.3f;
        var terminal=old.Find("Archive terminal - 03-03");terminal.position=P(0,0,96);terminal.GetComponent<NarrativeInspectable>().enabled=false;
        var n=terminal.gameObject.AddComponent<Room3StoryNode>();n.kind=Room3StoryNode.Kind.Decision;n.label="Core console / retain or erase";n.focus=terminal.Find("Archive display");
        flow.memorialParticles=Papers(zone,P(0,7,98),P(19,14,18),true);
        flow.landingCheckpoint=Group("Checkpoint after alignment");flow.landingCheckpoint.position=P(0,.15f,29);
        flow.postGateCheckpoint=Group("Checkpoint after testimony");flow.postGateCheckpoint.position=P(0,.15f,46);
        flow.coreCheckpoint=Group("Checkpoint at archive core");flow.coreCheckpoint.position=P(0,.15f,91);
        flow.environment.checkpoint=flow.landingCheckpoint;
    }
    static AudioSource Sound(string name,string clip,float volume,bool loop=true)
    {
        var g=Group(name,flow.transform);var s=g.gameObject.AddComponent<AudioSource>();s.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/"+clip+".wav");s.loop=loop;s.playOnAwake=loop;s.volume=volume;s.spatialBlend=0;s.priority=160;return s;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 size,Vector2 position)
    {
        var g=new GameObject(name,typeof(RectTransform));var r=g.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static UnityEngine.UI.Image Panel(string name,Transform parent,Vector2 size,Vector2 position,Color color)
    {var r=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),size,position);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=false;return image;}
    static TextMeshProUGUI Label(string name,string value,Transform parent,Vector2 size,Vector2 position,float fontSize,TextAlignmentOptions align=TextAlignmentOptions.TopLeft)
    {
        var r=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),size,position);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=fontSize;t.color=new Color(.89f,.93f,.88f);t.alignment=align;t.raycastTarget=false;return t;
    }
    static UnityEngine.UI.Button Button(string name,string text,Transform parent,Vector2 size,Vector2 position,UnityEngine.Events.UnityAction action=null)
    {
        var panel=Panel(name,parent,size,position,new Color(.09f,.15f,.12f,.98f));panel.raycastTarget=true;
        var button=panel.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=panel;
        var colors=button.colors;colors.highlightedColor=new Color(.55f,.82f,.62f);colors.pressedColor=new Color(.3f,.5f,.35f);button.colors=colors;
        Label("Label",text,panel.transform,size-new Vector2(20,4),Vector2.zero,22,TextAlignmentOptions.Center);
        if(action!=null)button.onClick.AddListener(action);return button;
    }
    static UnityEngine.UI.RawImage Picture(string name,Transform parent,Vector2 size,Vector2 position)
    {var r=Rect(name,parent,new Vector2(.5f,.5f),new Vector2(.5f,.5f),size,position);var im=r.gameObject.AddComponent<UnityEngine.UI.RawImage>();im.raycastTarget=false;return im;}
    static void BuildUI()
    {
        var canvas=new GameObject("Black Box Archive HUD",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvas.transform.SetParent(flow.transform,false);
        var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=200;
        var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var ui=canvas.AddComponent<Room3StoryUI>();flow.ui=ui;ui.flow=flow;
        var top=Rect("Survey status",canvas.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(900,160),new Vector2(30,-25));
        Label("Sector title","03 / THE BLACK BOX ARCHIVE",top,new Vector2(880,40),new Vector2(0,50),25);
        ui.objectiveText=Label("Objective","",top,new Vector2(880,65),new Vector2(0,-3),22);
        ui.recordText=Label("Traces","",top,new Vector2(880,30),new Vector2(0,-60),18);
        var settings=Label("Motion setting","",canvas.transform,new Vector2(550,32),Vector2.zero,17,TextAlignmentOptions.Right);settings.rectTransform.anchorMin=settings.rectTransform.anchorMax=new Vector2(1,1);settings.rectTransform.pivot=new Vector2(1,1);settings.rectTransform.anchoredPosition=new Vector2(-30,-25);ui.settingsText=settings;
        var prompt=Panel("Interaction",canvas.transform,new Vector2(1000,115),new Vector2(0,-390),new Color(.02f,.04f,.035f,.84f));ui.promptGroup=prompt.gameObject.AddComponent<CanvasGroup>();
        ui.promptText=Label("Action","",prompt.transform,new Vector2(950,38),new Vector2(0,25),25,TextAlignmentOptions.Center);
        ui.promptCaption=Label("Hint","",prompt.transform,new Vector2(950,30),new Vector2(0,-12),19,TextAlignmentOptions.Center);
        var track=Panel("Progress track",prompt.transform,new Vector2(700,4),new Vector2(0,-43),new Color(.18f,.25f,.2f));
        ui.progressFill=Panel("Progress",track.transform,new Vector2(700,4),Vector2.zero,new Color(.55f,.95f,.62f));ui.progressFill.type=UnityEngine.UI.Image.Type.Filled;ui.progressFill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;ui.progressFill.fillAmount=0;
        var notice=Panel("System notice",canvas.transform,new Vector2(950,65),new Vector2(0,300),new Color(.02f,.04f,.03f,.9f));ui.noticeGroup=notice.gameObject.AddComponent<CanvasGroup>();ui.noticeText=Label("Notice","",notice.transform,new Vector2(900,40),Vector2.zero,23,TextAlignmentOptions.Center);
        var reticle=Rect("Calibration overlay",canvas.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(1920,1080),Vector2.zero);ui.calibration=reticle.gameObject;
        Panel("Center horizontal",reticle,new Vector2(34,2),Vector2.zero,new Color(.7f,.9f,.8f));Panel("Center vertical",reticle,new Vector2(2,34),Vector2.zero,new Color(.7f,.9f,.8f));
        ui.targetMarker=Rect("Alignment marker",reticle,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(48,48),Vector2.zero);
        for(int s=-1;s<=1;s+=2){Panel("Target top bottom",ui.targetMarker,new Vector2(48,2),new Vector2(0,s*24),new Color(.4f,1,.55f));Panel("Target left right",ui.targetMarker,new Vector2(2,48),new Vector2(s*24,0),new Color(.4f,1,.55f));}
        var archive=Panel("Double layer archive",canvas.transform,new Vector2(1120,770),Vector2.zero,new Color(.016f,.027f,.023f,.99f));ui.archivePanel=archive.gameObject;
        ui.archiveTitle=Label("Archive title","",archive.transform,new Vector2(1040,60),new Vector2(0,325),37);
        ui.archiveBody=Label("Reading instruction","",archive.transform,new Vector2(1040,45),new Vector2(0,265),20);
        var whiteText=Label("Official layer","",archive.transform,new Vector2(660,450),new Vector2(-170,-5),25);ui.whiteText=whiteText;ui.whiteRecord=whiteText.gameObject.AddComponent<CanvasGroup>();
        var greenText=Label("Revealed layer","",archive.transform,new Vector2(660,450),new Vector2(-170,-5),24);greenText.color=new Color(.62f,.94f,.66f);greenText.enableAutoSizing=true;greenText.fontSizeMin=21;greenText.fontSizeMax=24;ui.greenText=greenText;ui.greenRecord=greenText.gameObject.AddComponent<CanvasGroup>();
        ui.archivePicture=Picture("Historical image",archive.transform,new Vector2(275,350),new Vector2(365,20));
        ui.clinicalButton=Button("Clinical image button","Show / hide clinical image",archive.transform,new Vector2(290,42),new Vector2(365,-180));
        ui.archiveSource=Label("Source and reconstruction note","",archive.transform,new Vector2(1040,78),new Vector2(0,-270),16);
        ui.revealLabel=Label("Reveal status","",archive.transform,new Vector2(640,30),new Vector2(-200,-342),19);
        ui.closeButton=Button("Close archive","CLOSE  [E / ESC]",archive.transform,new Vector2(280,43),new Vector2(375,-338));
        ui.clinicalImage=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Images/Clinical_Thesis_Fig1_2.png");ui.newspaperImage=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Images/World_1928_05_10.png");ui.mollieImage=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Images/Mollie_Thesis_Fig1_3.png");ui.graceImage=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Images/Grace_Thesis_Fig1_4.png");
        var decision=Panel("Core decision",canvas.transform,new Vector2(1100,610),Vector2.zero,new Color(.015f,.027f,.02f,.99f));ui.decisionPanel=decision.gameObject;
        Label("Decision title","THE RECORD IS NOW IN YOUR CUSTODY",decision.transform,new Vector2(1000,60),new Vector2(0,225),34,TextAlignmentOptions.Center);
        Label("Decision explanation","Choose what this system retains.\nBoth actions end the current survey.",decision.transform,new Vector2(950,100),new Vector2(0,110),26,TextAlignmentOptions.Center);
        ui.formatButton=Button("Format","RESTORE ORDER\nRIPRISTINA L'ORDINE",decision.transform,new Vector2(455,90),new Vector2(-250,-30));
        ui.preserveButton=Button("Preserve","GUARD THE RUINS\nCUSTODISCI LE ROVINE",decision.transform,new Vector2(455,90),new Vector2(250,-30));
        Label("Format consequence","Erase the local archive.\nReturn to the town; close the portal.",decision.transform,new Vector2(450,100),new Vector2(-250,-145),21,TextAlignmentOptions.Center);
        Label("Preserve consequence","Keep the records.\nStabilize the ruins as a memorial.",decision.transform,new Vector2(450,100),new Vector2(250,-145),21,TextAlignmentOptions.Center);
        ui.fade=Panel("Transition veil",canvas.transform,new Vector2(6000,6000),Vector2.zero,new Color(0,0,0,0));
        var ending=Panel("Ending",canvas.transform,new Vector2(1350,930),Vector2.zero,new Color(.013f,.024f,.018f,.97f));ui.endingPanel=ending.gameObject;
        ui.endingTitle=Label("Ending title","",ending.transform,new Vector2(1200,70),new Vector2(0,380),42,TextAlignmentOptions.Center);
        ui.endingBody=Label("Ending body","",ending.transform,new Vector2(680,510),new Vector2(0,60),27,TextAlignmentOptions.Center);
        ui.portraitMollie=Picture("Mollie portrait",ending.transform,new Vector2(205,270),new Vector2(-500,65));
        ui.portraitGrace=Picture("Grace portrait",ending.transform,new Vector2(205,270),new Vector2(500,65));
        Label("Portrait name left","Mollie Maggia",ending.transform,new Vector2(230,30),new Vector2(-500,-90),19,TextAlignmentOptions.Center);
        Label("Portrait name right","Grace Fryer",ending.transform,new Vector2(230,30),new Vector2(500,-90),19,TextAlignmentOptions.Center);
        ui.endingCredits=Label("Ending references","",ending.transform,new Vector2(1200,140),new Vector2(0,-275),17,TextAlignmentOptions.Center);
        ui.restartButton=Button("Restart","RESTART THE SURVEY  [R]",ending.transform,new Vector2(500,60),new Vector2(0,-390));
    }
    [MenuItem("Tools/Xiaoduan/ROOM3/Install four-zone story expansion")]
    public static void Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first.");
        var scene=SceneManager.GetActiveScene();if(scene.name!="ROOM3")throw new Exception("Open ROOM3 first.");
        if(scene.GetRootGameObjects().Any(g=>g.name=="ROOM3 - Four Zone Story"))throw new Exception("Expansion already exists; update its objects instead of rebuilding.");
        string backup="Library/Room3Backups/Story_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene);File.Copy(scene.path,backup+"/ROOM3.unity");
        foreach(var f in new[]{"Materials","Meshes"})Directory.CreateDirectory(Folder+"/"+f);AssetDatabase.Refresh();
        var oldRoot=scene.GetRootGameObjects().First(g=>g.name=="ROOM3 - Residual Archive").transform;
        root=new GameObject("ROOM3 - Four Zone Story").transform;
        block=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Room3/Meshes/ChamferedBlock.asset");font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        concrete=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Board concrete.mat");dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Dark concrete.mat");metal=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Oxidised steel.mat");green=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Phosphor inlay.mat");white=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Cold aperture.mat");glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Room3/Materials/Wired glass.mat");
        bone=new Material(concrete);bone.name="Pathological concrete";bone.SetFloat("_Pathology",.9f);bone.SetColor("_BaseColor",new Color(.65f,.68f,.58f));AssetDatabase.CreateAsset(bone,Folder+"/Materials/Pathological concrete.mat");
        shadow=Lit("Worker silhouettes",new Color(.008f,.013f,.011f));red=Lit("Authority red",new Color(.7f,.035f,.02f),2);
        paper=Lit("Paper fragments",new Color(.67f,.7f,.6f));
        var story=Group("Room3 Story Controller");flow=story.gameObject.AddComponent<Room3StoryFlow>();flow.environment=oldRoot.GetComponent<Room3Environment>();flow.volume=oldRoot.GetComponentInChildren<Volume>();flow.returnPortal=oldRoot.Find("01 - Isolation corridor/Return threshold").gameObject;
        Corridor(oldRoot);Bridge(oldRoot);Oath(oldRoot);Core(oldRoot);
        flow.nodes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Room3StoryNode>(true)).ToArray();
        var vcam=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CinemachineVirtualCameraBase>(true)).First();flow.cameraInterference=vcam.gameObject.AddComponent<Room3CameraInterference>();
        flow.clockSource=Sound("1920s ticking residue","Layered_Ticking",.14f);flow.clickSource=Sound("Contemporary click residue","Mouse_Residue",0);flow.geigerSource=Sound("Radiation clicks","Geiger_Trace",0);flow.heartbeatSource=Sound("Core pulse","Core_Heartbeat",0);flow.effectsSource=Sound("Story impacts","Bridge_Lock",1,false);
        flow.bridgeSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/Bridge_Lock.wav");flow.oathSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/Testimony_Impact.wav");flow.revealSound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/Audio/Archive_Reveal.wav");
        BuildUI();
        // Reuse selected older surfaces as visibly corroded material, preserving their geometry.
        oldRoot.Find("01 - Isolation corridor/Concrete rib")?.GetComponent<Renderer>()?.SetPropertyBlock(null);
        foreach(var r in oldRoot.Find("03 - Displaced architecture").GetComponentsInChildren<Renderer>().Where((r,i)=>i%9==0))r.sharedMaterial=bone;
        var strip=oldRoot.Find("01 - Isolation corridor").GetComponentsInChildren<Renderer>().Where(r=>r.name=="Concrete rib");foreach(var r in strip)r.sharedMaterial=bone;
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText("Library/Room3StoryBuild.json",JsonUtility.ToJson(new Report{scene=scene.path,backup=backup,storyNodes=flow.nodes.Length,bridgeModules=flow.bridgeModules.Length,figures=flow.boneLights.Length},true));
        Debug.Log("ROOM3 four-zone story expansion installed.");
    }
    [Serializable]class Report{public string scene,backup;public int storyNodes,bridgeModules,figures;}
}
#endif

