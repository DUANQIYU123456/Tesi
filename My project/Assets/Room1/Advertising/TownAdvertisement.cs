using System.Collections;
using UnityEngine;
using Xiaoduan.Narrative;
namespace Xiaoduan.Survey
{
 public sealed class TownAdvertisement:MonoBehaviour
 {
  public const string Copy="THE BOTTLED PROMISE\n\nRadium enters the language of everyday wellbeing: a bottle, a printed assurance, a promise of vitality.\n\nThe advertisement presents exposure as a benefit. The workplace, the body and the cost of that promise remain outside its frame.\n\nWhat does a promise of health leave out?";
  public Texture advertisement;
  public Renderer[] posters;
  public Material[] dissolveMaterials;
  public GameObject footprintTrail;
  public bool Dissolving{get;private set;}
  public bool Dissolved{get;private set;}
  public float DissolveProgress{get;private set;}
  MaterialPropertyBlock block;
  bool queued;
  // The noticeboard's readable face opens onto the town (+Z in ROOM1).
  public bool CanReadFrom(Vector3 eye)
  {
   Vector3 center=Vector3.zero;int count=0;
   foreach(var poster in posters)if(poster!=null){center+=poster.bounds.center;count++;}
   return count>0&&Vector3.Dot(eye-center/count,Vector3.forward)>.08f;
  }
  void Awake(){block=new MaterialPropertyBlock();
   #if UNITY_EDITOR
   // Warm only this imported effect, avoiding Unity's cyan async-compilation placeholder on first use.
   foreach(var material in dissolveMaterials)if(material!=null)UnityEditor.ShaderUtil.CompilePass(material,0,true);
   #endif
  }
  public void ReaderClosed(){if(Dissolving||Dissolved||queued)return;queued=true;GetComponent<NarrativeInspectable>().enabled=false;StartCoroutine(CloseSequence());}
  IEnumerator CloseSequence()
  {
   SurveyHUD.Instance?.Notify("COMMERCIAL CLAIM INDEXED / SIGNAL UNSTABLE",1);
   yield return new WaitForSeconds(.45f);
   GetComponent<TownBlackout>()?.Begin();yield return Dissolve();
  }
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
