#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Xiaoduan.Narrative;
using Xiaoduan.Survey;

namespace Xiaoduan.Clinical
{
    public static class ClinicalDeskInstaller
    {
        const string Root="Assets/Room2/ClinicalDesk";
        [MenuItem("Tools/Xiaoduan/ROOM2/Install candlelit medical archive")]
        public static string Install()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlayingOrWillChangePlaymode||scene.name!="ROOM2")throw new InvalidOperationException("Open ROOM2 in Edit mode.");
            if(GameObject.Find("ROOM2 - Candlelit medical archive")!=null)return "Already installed; preserved current scene.";
            var table=GameObject.Find("Table01");var gate=GameObject.Find("SM_Door_Prison");
            if(table==null||gate==null)throw new InvalidOperationException("Cannot find the user's Table01 / entrance gate.");
            EditorSceneManager.SaveScene(scene);
            string backup="Library/ClinicalDeskBackups/ROOM2_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(scene.path,backup,false);
            Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Root+"/Meshes");
            var group=new GameObject("ROOM2 - Candlelit medical archive");
            var desk=group.AddComponent<ClinicalDesk>();desk.table=table.transform;desk.entrance=gate.transform;
            desk.clinicalImage=AssetDatabase.LoadAssetAtPath<Texture>("Assets/Room3/Story/Images/Clinical_Thesis_Fig1_2.png");
            desk.paperScratch=Resources.Load<AudioClip>("ClinicalDesk/PaperScratch");desk.boneFriction=Resources.Load<AudioClip>("ClinicalDesk/BoneFriction");desk.geigerBurst=Resources.Load<AudioClip>("ClinicalDesk/GeigerBurst");
            var bounds=table.GetComponent<Renderer>().bounds;float top=bounds.max.y+.009f;
            foreach(var name in new[]{"Book09c","ClutterGlass02","ClutterBowl01","Skull01b","Skull02"})
            {var old=table.transform.parent.Find(name);if(old!=null){Undo.RecordObject(old.gameObject,"Preserve original desk props");old.gameObject.SetActive(true);}}
            var liquidRoot=GameObject.Find("药水");
            var liquids=liquidRoot.GetComponentsInChildren<SoftKitty.LiquidContainer.LiquidControl>().OrderBy(x=>x.name).ToArray();
            var positions=new[]{new Vector2(-6.61f,-194.19f),new Vector2(-6.27f,-193.80f),new Vector2(-5.96f,-193.72f),new Vector2(-4.88f,-193.83f),new Vector2(-4.83f,-194.23f)};
            for(int i=0;i<liquids.Length;i++)
            {
                var bottle=liquids[i];Undo.RecordObject(bottle.transform,"Rest bottle on table");bottle.transform.rotation=Quaternion.identity;
                var r=bottle.GetComponent<Renderer>();Vector2 p=positions[i%positions.Length];
                bottle.transform.position+=new Vector3(p.x-r.bounds.center.x,top-r.bounds.min.y,p.y-r.bounds.center.z);
                foreach(var rb in bottle.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;
                foreach(var inspect in bottle.GetComponents<NarrativeInspectable>())inspect.enabled=false;
                foreach(var node in bottle.GetComponents<SurveyNode>())node.enabled=false;
                var glow=bottle.GetComponent<ProximityPotionGlow>();if(glow!=null){glow.enabled=false;var material=glow.liquidRenderer.sharedMaterial;material.SetFloat("_EmissionStrength",1.1f);EditorUtility.SetDirty(material);if(glow.glowLight!=null){glow.glowLight.enabled=true;glow.glowLight.intensity=.12f;glow.glowLight.range=.6f;glow.glowLight.shadows=LightShadows.None;}}
                foreach(var col in bottle.GetComponentsInChildren<Collider>())col.enabled=false;
            }
            desk.bottles=liquids.Select(x=>x.transform).ToArray();
            var candle=GameObject.Find("Candle01b_on (3)");
            if(candle!=null)
            {
                var r=candle.GetComponent<Renderer>();candle.transform.position+=new Vector3(-5.40f-r.bounds.center.x,top-r.bounds.min.y,-193.76f-r.bounds.center.z);
                foreach(var rb in candle.GetComponentsInChildren<Rigidbody>())rb.isKinematic=true;
                desk.candle=candle.GetComponentInChildren<Light>();if(desk.candle!=null){desk.candle.color=new Color(1,.65f,.29f);desk.candle.range=5;desk.candle.shadows=LightShadows.None;}
            }
            var key=new GameObject("Candle beam / tabletop only");key.transform.SetParent(group.transform);key.transform.position=new Vector3(-5.4f,top+1.25f,-193.65f);key.transform.LookAt(new Vector3(-5.7f,top,-194.15f));
            desk.keyLight=key.AddComponent<Light>();desk.keyLight.type=LightType.Spot;desk.keyLight.color=new Color(1,.67f,.32f);desk.keyLight.range=5;desk.keyLight.spotAngle=72;desk.keyLight.innerSpotAngle=35;desk.keyLight.intensity=3;desk.keyLight.shadows=LightShadows.Soft;desk.keyLight.shadowBias=.035f;
            var paperMat=Material("1922 printed certificate",new Color(.87f,.85f,.79f),0);
            paperMat.SetTexture("_BaseMap",Resources.Load<Texture2D>("ClinicalDesk/MedicalRecord_1922"));paperMat.SetFloat("_Cull",0);
            var doc=GameObject.CreatePrimitive(PrimitiveType.Quad);doc.name="1922 - Mollie Maggia / redacted certificate";doc.transform.SetParent(group.transform);doc.transform.position=new Vector3(-5.63f,top+.004f,-194.13f);doc.transform.rotation=Quaternion.Euler(90,0,0);doc.transform.localScale=new Vector3(.57f,.77f,1);doc.GetComponent<Renderer>().sharedMaterial=paperMat;
            UnityEngine.Object.DestroyImmediate(doc.GetComponent<Collider>());
            var entry=ScriptableObject.CreateInstance<NarrativeEntry>();entry.title="1922 / Mollie Maggia — redacted medical record";entry.category="CANDLELIT ARCHIVE / XII";entry.body="Hold the left mouse button and scrape the black ink.";AssetDatabase.CreateAsset(entry,Root+"/MedicalDeskEntry.asset");
            var target=doc.AddComponent<NarrativeInspectable>();target.entry=entry;target.focusRenderer=doc.GetComponent<Renderer>();target.interactionDistance=3.2f;
            doc.AddComponent<ClinicalDeskTarget>().desk=desk;var nodeTarget=doc.AddComponent<SurveyNode>();nodeTarget.kind=SurveyNodeKind.Bottle;nodeTarget.focus=doc.transform;nodeTarget.reach=7;
            var wood=Material("Aged wood brush handle",new Color(.20f,.105f,.038f),0);var brass=Material("Oxidised brass binding",new Color(.32f,.245f,.105f),.6f);var hair=Material("Camel hair",new Color(.13f,.075f,.028f),0);var paint=Material("Undark residue on brush",new Color(.075f,.32f,.065f),0);paint.EnableKeyword("_EMISSION");paint.SetColor("_EmissionColor",new Color(.12f,.65f,.085f));
            var pen=new GameObject("1920s - camel hair wooden brush");pen.transform.SetParent(group.transform);pen.transform.position=new Vector3(-5.1f,top+.018f,-194.27f);pen.transform.rotation=Quaternion.Euler(90,0,-17);
            Lathe("Wooden shaft",pen.transform,new[]{new Vector2(.005f,0),new Vector2(.006f,.04f),new Vector2(.006f,.23f),new Vector2(.005f,.25f)},wood);
            Lathe("Brass ferrule",pen.transform,new[]{new Vector2(.007f,.235f),new Vector2(.008f,.26f)},brass);
            Lathe("Tapered camel bristles",pen.transform,new[]{new Vector2(.006f,.257f),new Vector2(.008f,.273f),new Vector2(.006f,.3f),new Vector2(.001f,.326f)},hair);
            Lathe("Green paint at brush point",pen.transform,new[]{new Vector2(.003f,.313f),new Vector2(.0005f,.332f)},paint);
            for(int i=0;i<9;i++)
            {
                var strand=new GameObject("Fine camel hair "+i);strand.transform.SetParent(pen.transform,false);var line=strand.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=3;float a=i*Mathf.PI*2/9;line.SetPositions(new[]{new Vector3(Mathf.Cos(a)*.006f,.26f,Mathf.Sin(a)*.006f),new Vector3(Mathf.Cos(a)*.004f,.295f,Mathf.Sin(a)*.004f),new Vector3(0,.33f,0)});line.startWidth=.0007f;line.endWidth=.0001f;line.sharedMaterial=hair;
            }
            foreach(var id in AssetDatabase.FindAssets("t:Texture2D",new[]{Root+"/Resources"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(id);var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)continue;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject=group;
            return "Installed ClinicalDesk; "+liquids.Length+" bottles rest on Table01; backup: "+backup;
        }
        static Material Material(string name,Color c,float metallic)
        {
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.SetColor("_BaseColor",c);mat.SetFloat("_Metallic",metallic);mat.SetFloat("_Smoothness",.16f);AssetDatabase.CreateAsset(mat,Root+"/Materials/"+name+".mat");return mat;
        }
        static void Lathe(string name,Transform parent,Vector2[] rings,Material material)
        {
            const int sides=16;var vertices=new Vector3[rings.Length*(sides+1)];var uv=new Vector2[vertices.Length];var triangles=new int[(rings.Length-1)*sides*6];
            for(int y=0;y<rings.Length;y++)for(int x=0;x<=sides;x++){float a=x*Mathf.PI*2/sides;int k=y*(sides+1)+x;vertices[k]=new Vector3(Mathf.Cos(a)*rings[y].x,rings[y].y,Mathf.Sin(a)*rings[y].x);uv[k]=new Vector2((float)x/sides,rings[y].y*10);}
            int n=0;for(int y=0;y<rings.Length-1;y++)for(int x=0;x<sides;x++){int k=y*(sides+1)+x;triangles[n++]=k;triangles[n++]=k+sides+1;triangles[n++]=k+1;triangles[n++]=k+1;triangles[n++]=k+sides+1;triangles[n++]=k+sides+2;}
            var mesh=new Mesh{name=name};mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Root+"/Meshes/"+name+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
    }
}
#endif
