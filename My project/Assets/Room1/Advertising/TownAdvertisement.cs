using System.Collections;
using UnityEngine;
using Xiaoduan.Narrative;
namespace Xiaoduan.Survey
{
 public sealed class TownAdvertisement:MonoBehaviour
 {
  public const string Copy="Feeling tired? Pop a bottle! Got diseases? Radithor’s got your back! Imagine the hype: people wandering around as they’d just won the lottery, clutching their Radithor bottles, convinced they were about to unlock the secrets of eternal vitality.";
  public Texture advertisement;
  public Renderer[] posters;
  public Material[] dissolveMaterials;
  public GameObject footprintTrail;
  public bool Dissolving{get;private set;}
  public bool Dissolved{get;private set;}
  public float DissolveProgress{get;private set;}
  MaterialPropertyBlock block;
  void Awake(){block=new MaterialPropertyBlock();
   #if UNITY_EDITOR
   // Warm only this imported effect, avoiding Unity's cyan async-compilation placeholder on first use.
   foreach(var material in dissolveMaterials)if(material!=null)UnityEditor.ShaderUtil.CompilePass(material,0,true);
   #endif
  }
  public void ReaderClosed(){if(Dissolving||Dissolved)return;GetComponent<TownBlackout>()?.Begin();StartCoroutine(Dissolve());}
  IEnumerator Dissolve()
  {
   Dissolving=true;GetComponent<NarrativeInspectable>().enabled=false;
   if(footprintTrail!=null)footprintTrail.SetActive(false);
   if(GetComponent<TownBlackout>()==null)SurveyHUD.Instance?.Notify("COMMERCIAL PROMISE / SIGNAL DECAY",3);
   // The original paper materials remain visible until the reader is closed.
   for(int i=0;i<posters.Length;i++)if(posters[i]!=null){posters[i].sharedMaterial=dissolveMaterials[i];Set(posters[i],0);}
   float duration=SurveyProgress.ReducedMotion?1.6f:3.8f;
   for(float t=0;t<duration;t+=Time.deltaTime){DissolveProgress=t/duration;for(int i=0;i<posters.Length;i++)if(posters[i]!=null)Set(posters[i],Mathf.Clamp01(DissolveProgress*1.12f-i*.035f));yield return null;}
   foreach(var poster in posters)if(poster!=null){Set(poster,1);poster.enabled=false;}
   DissolveProgress=1;Dissolving=false;Dissolved=true;
  }
  void Set(Renderer target,float value){target.GetPropertyBlock(block);block.SetFloat("_Dissolve",value);block.SetFloat("_EdgeColorIntensity",SurveyProgress.ReducedMotion?.25f:1.1f);target.SetPropertyBlock(block);}
 }
}
