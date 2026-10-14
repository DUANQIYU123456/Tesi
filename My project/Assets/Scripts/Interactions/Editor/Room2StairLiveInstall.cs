#if UNITY_EDITOR
using UnityEditor;

// Attach after importing during a running test; the runtime bootstrap handles future runs.
[InitializeOnLoad]
public static class Room2StairLiveInstall
{
    static Room2StairLiveInstall() => EditorApplication.delayCall += Install;
    static void Install()
    {
        if (EditorApplication.isPlaying && !EditorApplication.isCompiling)
            Room2StairBootstrap.Attach();
    }
}
#endif
