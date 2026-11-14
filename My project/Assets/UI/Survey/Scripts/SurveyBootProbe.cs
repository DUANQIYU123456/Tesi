#if DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Xiaoduan.Survey
{
    public sealed class SurveyBootProbe:MonoBehaviour
    {
        string report;
        [Serializable]class Result{public bool passed,menuVisible;public string scene,displayFont,bodyFont;public int frames;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Begin()
        {
            if(Application.isEditor)return;
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--survey-boot-check");
            if(index<0||index+1>=args.Length)return;
            var probe=new GameObject("Development boot check").AddComponent<SurveyBootProbe>();probe.report=args[index+1];DontDestroyOnLoad(probe.gameObject);probe.StartCoroutine(probe.Check());
        }
        IEnumerator Check()
        {
            Application.runInBackground=true;yield return new WaitForSecondsRealtime(3);
            var hud=SurveyHUD.Instance;var typography=SurveyTypography.Instance;
            var result=new Result{scene=SceneManager.GetActiveScene().name,frames=Time.frameCount,menuVisible=hud!=null&&hud.MenuVisible,displayFont=typography!=null&&typography.displayFont!=null?typography.displayFont.name:"",bodyFont=typography!=null?typography.bodyFont.name:""};
            result.passed=result.scene=="ROOM1"&&result.menuVisible&&result.frames>10&&result.displayFont.Contains("ManufacturingConsent");
            File.WriteAllText(report,JsonUtility.ToJson(result,true));Application.Quit(result.passed?0:1);
        }
    }
}
#endif
