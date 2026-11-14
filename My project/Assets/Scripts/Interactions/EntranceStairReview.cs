#if UNITY_EDITOR
using UnityEngine;
using StarterAssets;
using System.Collections;
using System.IO;
public sealed class EntranceStairReview:MonoBehaviour{
 public string reportPath; public int legs; public bool done; public string error=""; Transform player; StarterAssetsInputs input;
 public void Begin(){StartCoroutine(Safe());}
 IEnumerator Safe(){var e=Run();while(true){object current;try{if(!e.MoveNext())break;current=e.Current;}catch(System.Exception x){error=x.ToString();break;}yield return current;}if(input!=null)input.move=Vector2.zero;done=true;File.WriteAllText(reportPath,Newtonsoft.Json.JsonConvert.SerializeObject(new {passed=error=="",legs,error,position=player.position.ToString(),frames=Time.frameCount},Newtonsoft.Json.Formatting.Indented));}
 void Place(float x,float z){var cc=player.GetComponent<CharacterController>();cc.enabled=false;float y=40;foreach(var h in Physics.RaycastAll(new Vector3(x,80,z),Vector3.down,100,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))if(h.collider.name=="Entrance stair walk surface")y=h.point.y+.15f;player.position=new Vector3(x,y,z);cc.enabled=true;Physics.SyncTransforms();}
 IEnumerator Walk(float x,float z){float end=Time.realtimeSinceStartup+24;while(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(x,z))>.24f&&Time.realtimeSinceStartup<end){var d=new Vector3(x-player.position.x,0,z-player.position.z);player.rotation=Quaternion.LookRotation(d);input.move=Vector2.up;input.look=Vector2.zero;input.jump=false;input.sprint=false;yield return null;}input.move=Vector2.zero;if(Vector2.Distance(new Vector2(player.position.x,player.position.z),new Vector2(x,z))>.5f)throw new System.Exception("Blocked before "+z+" at "+player.position);legs++;yield return new WaitForSecondsRealtime(.3f);}
 IEnumerator Run(){yield return new WaitForSecondsRealtime(.5f);player=GameObject.FindGameObjectWithTag("Player").transform;input=player.GetComponent<StarterAssetsInputs>();foreach(float x in new[]{-6.9f,-5.7f}){Place(x,-268);yield return new WaitForSecondsRealtime(.4f);yield return Walk(x,-233.5f);yield return Walk(x,-268);}Place(-6.9f,-254.65f);yield return new WaitForSecondsRealtime(.4f);yield return Walk(-6.9f,-250);}
}
#endif
