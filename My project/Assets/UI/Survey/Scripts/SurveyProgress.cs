using System;
using UnityEngine;

namespace Xiaoduan.Survey
{
    // Playthrough data only: survives portals, resets when a new game starts.
    public static class SurveyProgress
    {
        public static int Mask { get; private set; }
        public static bool LeftTuned, RightTuned, Started, ReducedMotion;
        public static bool Modal { get; internal set; }
        public static int Count { get { int n=0; for(int i=0;i<4;i++)if(Has(i))n++;return n; } }
        public static bool Has(int index) => (Mask & (1<<index))!=0;
        public static event Action Changed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize(){Mask=0;LeftTuned=RightTuned=Started=ReducedMotion=Modal=false;Changed=null;}
        public static void Collect(int index)
        {
            if(index<0||index>3||Has(index))return;
            Mask|=1<<index;Changed?.Invoke();
        }
        public static void Tune(bool left)
        {
            if(left)LeftTuned=true;else RightTuned=true;
            if(LeftTuned&&RightTuned)Collect(1);
            Changed?.Invoke();
        }
        public static void NewGame(){Mask=0;LeftTuned=RightTuned=false;Started=true;Modal=false;Changed?.Invoke();}
    }
}
