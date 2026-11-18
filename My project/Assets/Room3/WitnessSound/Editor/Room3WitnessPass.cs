#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Xiaoduan.Room3;
public static class Room3WitnessPass
{
    const string F="Assets/Room3/WitnessSound/";
    struct Joint {public string name;public int parent;public Vector3 p,s;public Quaternion q;}
    static Joint[] data;
    static Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static Vector3 V(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    static string S(BinaryReader r)=>System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
    static void Load()
    {
        meshes.Clear();using var r=new BinaryReader(File.OpenRead(F+"Data/WitnessMeshes.bytes"));r.ReadBytes(4);if(r.ReadInt32()!=1)throw new Exception("Witness data version");
        data=new Joint[r.ReadInt32()];for(int i=0;i<data.Length;i++)data[i]=new Joint{name=S(r),parent=r.ReadInt32(),p=V(r),q=new Quaternion(r.ReadSingle(),r.ReadSingle(),r.ReadSingle(),r.ReadSingle()),s=V(r)};
        int count=r.ReadInt32();for(int k=0;k<count;k++)
        {
            string name=S(r);int nv=r.ReadInt32(),ni=r.ReadInt32();var vs=new Vector3[nv];var ns=new Vector3[nv];var uv=new Vector2[nv];var bw=new BoneWeight[nv];
            for(int i=0;i<nv;i++)vs[i]=V(r);for(int i=0;i<nv;i++)ns[i]=V(r);for(int i=0;i<nv;i++)uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
            for(int i=0;i<nv;i++)bw[i]=new BoneWeight{boneIndex0=r.ReadInt32(),boneIndex1=r.ReadInt32(),boneIndex2=r.ReadInt32(),boneIndex3=r.ReadInt32()};
            for(int i=0;i<nv;i++){var w=bw[i];w.weight0=r.ReadSingle();w.weight1=r.ReadSingle();w.weight2=r.ReadSingle();w.weight3=r.ReadSingle();bw[i]=w;}
            var triangles=new List<int>();for(int i=0;i<ni;i+=3){int a=r.ReadInt32(),b=r.ReadInt32(),c=r.ReadInt32();var center=(vs[a]+vs[b]+vs[c])/3;
                bool exposed=true;if(exposed)triangles.AddRange(new[]{a,b,c});}
            var m=new Mesh{name=name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=vs,normals=ns,uv=uv,boneWeights=bw};m.SetTriangles(triangles,0);m.RecalculateBounds();
            var path=F+"Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old!=null){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);m=old;}else AssetDatabase.CreateAsset(m,path);meshes[name]=m;
        }
    }
    static Material Material(string name,Color color,float smooth)
    {
        var path=F+"Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",0);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(m);return m;
    }
    static Transform T(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Transform[] Rig(Transform parent)
    {
        var ts=data.Select(j=>T(j.name,parent)).ToArray();for(int i=0;i<ts.Length;i++){var j=data[i];ts[i].SetParent(j.parent<0?parent:ts[j.parent],false);ts[i].localPosition=j.p;ts[i].localRotation=j.q;ts[i].localScale=j.s;}return ts;
    }
    static SkinnedMeshRenderer Skin(string key,Transform parent,Transform[] rig,Material mat)
    {
        var m=meshes[key];if(m.bindposes.Length!=rig.Length){m.bindposes=rig.Select(t=>t.worldToLocalMatrix*parent.localToWorldMatrix).ToArray();EditorUtility.SetDirty(m);}
        var t=T(key,parent);var r=t.gameObject.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=m;r.sharedMaterial=mat;r.bones=rig;r.rootBone=rig[0];r.updateWhenOffscreen=false;r.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(1.6f,2.3f,1.1f));r.quality=SkinQuality.Bone4;return r;
    }
    static Mesh Apron()
    {
        var source=meshes["clothing_dress"];var vs=source.vertices;var ns=source.normals;var triangles=new List<int>();var indices=source.triangles;
        for(int i=0;i<indices.Length;i+=3){var c=(vs[indices[i]]+vs[indices[i+1]]+vs[indices[i+2]])/3;if(c.z<-.055f)triangles.AddRange(new[]{indices[i],indices[i+1],indices[i+2]});}
        for(int i=0;i<vs.Length;i++)vs[i]+=ns[i].normalized*.009f;
        var apronUV=vs.Select(v=>new Vector2(v.x+.5f,v.y/1.7f)).ToArray();var m=new Mesh{name="Cotton work apron surface",indexFormat=source.indexFormat,vertices=vs,normals=ns,uv=apronUV,boneWeights=source.boneWeights,bindposes=source.bindposes};m.SetTriangles(triangles,0);m.RecalculateBounds();string path=F+"Meshes/work_apron_masked.asset";var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(prior!=null){EditorUtility.CopySerialized(m,prior);UnityEngine.Object.DestroyImmediate(m);m=prior;}else AssetDatabase.CreateAsset(m,path);meshes["work_apron"]=m;return m;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before applying");
        foreach(var n in new[]{"Meshes","Materials","Textures"})Directory.CreateDirectory(F+n);Load();
        var flow=UnityEngine.Object.FindFirstObjectByType<Room3StoryFlow>();var x=flow.embodied;var prior=GameObject.Find("Anatomical witnesses");if(prior!=null)UnityEngine.Object.DestroyImmediate(prior);
        var all=flow.gameObject.scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach(var glow in flow.boneLights)if(glow!=null)glow.transform.parent.gameObject.SetActive(false);
        foreach(var t in all)if(t.name.StartsWith("Suspended labor residue"))t.gameObject.SetActive(false);
        var root=T("Anatomical witnesses",null);var cloth=Material("Carbon cotton",new Color(.052f,.061f,.056f),.12f);var skin=Material("Unidentified witness - graphite",new Color(.15f,.17f,.155f),.2f);var shoes=Material("Worn black leather",new Color(.027f,.032f,.029f),.28f);
        var noise=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Room3/ArtDirection/Textures/Concrete normal.png");cloth.SetTexture("_BumpMap",noise);cloth.SetTextureScale("_BumpMap",new Vector2(18,18));cloth.SetFloat("_BumpScale",.075f);cloth.EnableKeyword("_NORMALMAP");
        var apron=Material("Faded cotton apron",new Color(.17f,.18f,.157f),.09f);apron.SetTexture("_BumpMap",noise);apron.SetTextureScale("_BumpMap",new Vector2(15,15));apron.SetFloat("_BumpScale",.08f);apron.EnableKeyword("_NORMALMAP");
        string maskPath=F+"Textures/Apron edge mask.asset";var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);if(mask==null)
        {
            mask=new Texture2D(256,512,TextureFormat.RGBA32,false){name="Smooth cotton apron boundary",wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};var colors=new Color[256*512];
            for(int maskRow=0;maskRow<512;maskRow++)for(int maskColumn=0;maskColumn<256;maskColumn++){float maskX=(maskColumn+.5f)/256-.5f,maskY=(maskRow+.5f)/512*1.7f;float width=maskY>1.02f?Mathf.Lerp(.17f,.10f,Mathf.InverseLerp(1.02f,1.255f,maskY)):Mathf.Lerp(.265f,.17f,Mathf.InverseLerp(.39f,1.02f,maskY));float edge=Mathf.Min(width-Mathf.Abs(maskX),Mathf.Min(maskY-.39f,1.255f-maskY));colors[maskRow*256+maskColumn]=new Color(1,1,1,Mathf.Clamp01(edge/.006f));}mask.SetPixels(colors);mask.Apply();AssetDatabase.CreateAsset(mask,maskPath);
        }
        apron.SetTexture("_BaseMap",mask);apron.SetFloat("_AlphaClip",1);apron.SetFloat("_Cutoff",.5f);apron.EnableKeyword("_ALPHATEST_ON");apron.SetOverrideTag("RenderType","TransparentCutout");apron.renderQueue=2450;

        var lights=new List<Light>();var glows=new List<GameObject>();
        float[] px={-3.15f,-1.64f,0,1.7f,3.1f};float[] pz={40.4f,41.05f,41.5f,40.82f,40.15f};float[] scale={1.14f,1.19f,1.1f,1.17f,1.12f};
        for(int i=0;i<5;i++)
        {
            var t=T("Witness "+(i+1)+" - symbolic reconstruction",root);var rig=Rig(t);var renderers=new List<Renderer>{Skin("base_body",t,rig,skin),Skin("clothing_dress",t,rig,cloth),Skin("clothing_shoes",t,rig,shoes)};
            if(i==0){Apron();}if(i==0||i==3)renderers.Add(Skin("work_apron",t,rig,apron));
            string hairKey=i%3==0?"hair_bob01":i%3==1?"hair_bob02":"hair_short02";var hair=Material(hairKey,new Color(.028f,.036f,.029f),.18f);hair.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(F+"Data/"+hairKey+"0.png"));hair.SetFloat("_AlphaClip",1);hair.SetFloat("_Cutoff",.35f);hair.EnableKeyword("_ALPHATEST_ON");hair.SetFloat("_Cull",0);renderers.Add(Skin(hairKey,t,rig,hair));
            var chest=T("Testimony glow",rig.First(b=>b.name=="spine_03"));chest.position=t.TransformPoint(new Vector3(0,1.17f,-.17f));chest.rotation=Quaternion.LookRotation(Vector3.back,Vector3.up);var light=chest.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.spotAngle=65;light.innerSpotAngle=28;light.color=new Color(.28f,1,.48f);light.range=.8f;light.intensity=0;light.shadows=LightShadows.None;lights.Add(light);glows.Add(chest.gameObject);
            var tracePath=F+"Materials/Testimony phosphor trace.mat";var phosphor=AssetDatabase.LoadAssetAtPath<Material>(tracePath);if(phosphor==null){phosphor=new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Testimony phosphor trace"};AssetDatabase.CreateAsset(phosphor,tracePath);}phosphor.SetColor("_BaseColor",new Color(.18f,3.4f,.64f));EditorUtility.SetDirty(phosphor);
            for(int branch=0;branch<8;branch++)
            {
                var g=new GameObject("Phosphor testimony trace");g.transform.SetParent(chest,false);var line=g.AddComponent<LineRenderer>();line.useWorldSpace=false;line.sharedMaterial=phosphor;line.positionCount=7;line.startWidth=.0038f;line.endWidth=.0009f;line.numCornerVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                for(int k=0;k<7;k++){float u=k/6f;float angle=(branch*137.5f+i*11)*Mathf.Deg2Rad,length=.045f+(branch%3)*.018f;var p=new Vector3(.027f+Mathf.Cos(angle)*length*u,1.19f+Mathf.Sin(angle)*length*u+.006f*Mathf.Sin(u*Mathf.PI*1.3f+branch),-.21f);var vertices=meshes["clothing_dress"].vertices;float best=float.MaxValue;foreach(var v in vertices){if(v.z>-.08f)continue;float d=Mathf.Pow(v.x-p.x,2)+Mathf.Pow(v.y-p.y,2);if(d<best){best=d;p.z=v.z-.011f;}}line.SetPosition(k,chest.InverseTransformPoint(t.TransformPoint(p)));}
            }
            var plume=UnityEngine.Object.Instantiate(x.oathBurst,chest,false);plume.name="Isotope release from sternum";plume.transform.localPosition=chest.InverseTransformPoint(t.TransformPoint(new Vector3(.027f,1.19f,-.225f)));plume.transform.localRotation=Quaternion.identity;plume.transform.localScale=Vector3.one;plume.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var pm=plume.main;pm.playOnAwake=true;pm.loop=false;pm.duration=1.6f;pm.startLifetime=new ParticleSystem.MinMaxCurve(.7f,1.5f);pm.startSpeed=new ParticleSystem.MinMaxCurve(.12f,.35f);pm.startSize=new ParticleSystem.MinMaxCurve(.012f,.03f);pm.maxParticles=64;pm.simulationSpace=ParticleSystemSimulationSpace.World;pm.startColor=new Color(.28f,1,.46f,.8f);
            var emission=plume.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,36)});var shape=plume.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.055f;shape.scale=Vector3.one;var velocity=plume.velocityOverLifetime;velocity.enabled=false;var force=plume.forceOverLifetime;force.enabled=false;pm.gravityModifier=0;
            t.position=new Vector3(px[i],.012f,pz[i]);t.localScale=Vector3.one*scale[i];t.eulerAngles=new Vector3(0,new[]{-9,6,-3,-7,12}[i],0);
            var witness=t.gameObject.AddComponent<Room3Witness>();witness.flow=flow;witness.joints=rig;witness.variation=i;witness.chestLight=light;witness.surfaces=renderers.ToArray();witness.bindRotations=data.Select(j=>j.q).ToArray();witness.PreviewPose();
        }
        flow.boneLights=glows.ToArray();x.chestLights=lights.ToArray();x.oathBurst.transform.position=new Vector3(0,1.3f,40.9f);var oldBurst=x.oathBurst.emission;oldBurst.rateOverTime=0;oldBurst.SetBursts(Array.Empty<ParticleSystem.Burst>());
        foreach(var g in flow.workerFigures)if(g!=null)g.SetActive(false);
        var oldWorkers=GameObject.Find("Anatomical archive workers");if(oldWorkers!=null)UnityEngine.Object.DestroyImmediate(oldWorkers);var workersRoot=T("Anatomical archive workers",null);var workerList=new List<GameObject>();
        for(int i=0;i<3;i++)
        {
            var t=T("Worker echo "+i+" - articulated",workersRoot);var rig=Rig(t);var renderers=new List<Renderer>{Skin("base_body",t,rig,skin),Skin("clothing_dress",t,rig,cloth),Skin("clothing_shoes",t,rig,shoes)};
            var hair=AssetDatabase.LoadAssetAtPath<Material>(F+"Materials/"+(i==0?"hair_bob01":i==1?"hair_bob02":"hair_short02")+".mat");renderers.Add(Skin(i==0?"hair_bob01":i==1?"hair_bob02":"hair_short02",t,rig,hair));if(i!=1)renderers.Add(Skin("work_apron",t,rig,apron));
            t.position=new Vector3(10,.02f,-22.05f+i*6);t.localScale=Vector3.one*(1.05f+i*.035f);var witness=t.gameObject.AddComponent<Room3Witness>();witness.flow=flow;witness.joints=rig;witness.variation=i;witness.workStation=true;witness.surfaces=renderers.ToArray();witness.bindRotations=data.Select(j=>j.q).ToArray();witness.PreviewPose();workerList.Add(t.gameObject);
        }
        flow.workerFigures=workerList.ToArray();
        // Remove the oversize suspended copies and their artificial vertical supports.
        foreach(var t in all)if(t!=null&&(t.name=="Suspended worker extraction tether"||t.name.StartsWith("Witness halo")))t.gameObject.SetActive(false);
        EditorUtility.SetDirty(flow);EditorUtility.SetDirty(x);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);EditorSceneManager.SaveScene(flow.gameObject.scene);
        return "5 anatomical clothed witnesses; 3 short hair meshes; grounded asymmetric composition";
    }
}
#endif
