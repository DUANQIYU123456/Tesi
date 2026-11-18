#if UNITY_EDITOR
using System;using System.IO;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;
namespace Xiaoduan.Panopticon {
 public static class CentralProjectionSetup {
  public static string Apply(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");var h=UnityEngine.Object.FindFirstObjectByType<PanopticonHall>();var scene=h.gameObject.scene;EditorSceneManager.SaveScene(scene);var backup="Library/CentralProjectionBackup/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);File.Copy(scene.path,backup+"/ROOM2.unity",false);
   var center=h.transform.Find("Central projection point - move this");if(center==null){center=new GameObject("Central projection point - move this").transform;center.SetParent(h.transform,false);}center.position=new Vector3(-5.55f,39,-141);center.rotation=Quaternion.identity;
   for(int i=0;i<h.memories.Length;i++){var m=h.memories[i];m.anchor.SetParent(center,false);m.anchor.localPosition=Vector3.zero;m.anchor.localRotation=Quaternion.identity;m.anchor.localScale=Vector3.one*1.2f;foreach(var r in m.surfaces)r.enabled=i==0;if(m.projection!=null)m.projection.SetActive(false);EditorUtility.SetDirty(m.anchor);}
   var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/小资产们/溶解特效/URP/ShaderGraph/DoubleSide/Dissolve_Unlit_DoubleSide.shadergraph");string path="Assets/Room2/Panopticon/Materials/CentralMemory_Dissolve.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader){name="Central memory - imported dissolution"};AssetDatabase.CreateAsset(mat,path);}mat.SetFloat("_Dissolve",.1f);mat.SetFloat("_EdgeWidth",.045f);mat.SetFloat("_EdgeColorIntensity",1.8f);mat.SetColor("_EdgeColor",new Color(.26f,1,.52f));mat.SetFloat("_NoiseScale",14);h.memoryDissolveTemplate=mat;h.centralProjection=true;h.memoryTransitionSeconds=.5f;h.revealSeconds=5;h.scanner.scanDuration=14;h.scanner.repeatDelay=2;
   EditorUtility.SetDirty(h);EditorUtility.SetDirty(h.scanner);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=center.gameObject;
   return "Saved central projection at (-5.55,39,-141), uniform 1.2 scale, one memory per 16-second scan cycle, imported picture-2 dissolution. Backup: "+backup;
  }
 }
}
#endif
