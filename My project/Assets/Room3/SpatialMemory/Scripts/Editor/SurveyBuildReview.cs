#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class SurveyBuildReview
{
    [Serializable]class Result{public string result,output;public ulong bytes;public int errors,warnings;public double seconds;}
    public static string Schedule(string folder)
    {
        Directory.CreateDirectory(folder);
        EditorApplication.delayCall+=()=>
        {
            var result=new Result{output=Path.Combine(folder,"Luce Verde nelle Ossa.exe")};
            try
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=result.output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                result.result=report.summary.result.ToString();result.bytes=report.summary.totalSize;result.errors=(int)report.summary.totalErrors;result.warnings=(int)report.summary.totalWarnings;result.seconds=report.summary.totalTime.TotalSeconds;
            }
            catch(Exception e){result.result=e.GetType().Name+": "+e.Message;result.errors++;}
            File.WriteAllText(Path.Combine(folder,"build-review.json"),JsonUtility.ToJson(result,true));
        };
        return "Windows development review build scheduled";
    }
}
#endif
