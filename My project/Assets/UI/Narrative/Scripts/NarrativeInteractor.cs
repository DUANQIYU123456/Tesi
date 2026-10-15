using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using StarterAssets;

namespace Xiaoduan.Narrative
{
    [DefaultExecutionOrder(-100)]
    public sealed class NarrativeInteractor : MonoBehaviour
    {
        public NarrativeHUD view;
        public static NarrativeInteractor Active {get;private set;}
        public static bool HasActiveInterface => Active!=null && Active.isActiveAndEnabled;
        public bool IsReading {get;private set;}
        public Object CurrentTarget => selectedPortal!=null ? (Object)selectedPortal : selectedObject;
        TransitionPoint[] portals;
        NarrativeInspectable[] inspectables;
        TransitionPoint selectedPortal;
        NarrativeInspectable selectedObject,openedObject;
        float nextRefresh,nextUse;
        Camera playerCamera;
        Transform player;
        StarterAssetsInputs lockedInput;
        PlayerInput lockedPlayerInput;
        bool originalInputActive,originalLook,originalCursorLocked,originalCursorVisible;
        string originalMap;
        CursorLockMode originalCursor;
        readonly RaycastHit[] hits=new RaycastHit[48];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active=null;
        void OnEnable() { Active=this; SceneManager.sceneUnloaded+=SceneUnloaded; }
        void Awake() { view.closeButton.onClick.AddListener(CloseEntry); }
        void SceneUnloaded(Scene _) { if (IsReading) CloseEntry(); }
        void Refresh()
        {
            nextRefresh=Time.unscaledTime+.5f;
            portals=FindObjectsByType<TransitionPoint>(FindObjectsSortMode.None);
            inspectables=FindObjectsByType<NarrativeInspectable>(FindObjectsSortMode.None);
            var found=GameObject.FindGameObjectWithTag("Player");
            player=found!=null ? found.transform : null;
            playerCamera=Camera.main;
        }
        void Update()
        {
            if (Time.unscaledTime>=nextRefresh || playerCamera==null || player==null) Refresh();
            bool use=Keyboard.current!=null && Keyboard.current.eKey.wasPressedThisFrame;
            bool close=Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (IsReading)
            {
                if (openedObject==null || !openedObject.CanInspect || use || close) CloseEntry();
                return;
            }
            if (playerCamera==null || player==null || playerCamera.gameObject.scene!=gameObject.scene ||
                (SceneController.Instance!=null && SceneController.Instance.IsTransitioning))
            { ClearTarget(); return; }
            SelectTarget();
            if (use && Time.unscaledTime>=nextUse) TryInteract();
        }
        void ClearTarget()
        {
            selectedPortal=null; selectedObject=null;
            view.SetPrompt("",""); view.SetFocus(false);
        }
        void SelectTarget()
        {
            selectedPortal=null; selectedObject=null;
            float best=float.PositiveInfinity;
            Vector3 eye=playerCamera.transform.position,forward=playerCamera.transform.forward;
            if (portals!=null) foreach (var portal in portals)
            {
                if (portal==null || portal.gameObject.scene!=gameObject.scene || !portal.CanInteract) continue;
                var collider=portal.GetComponent<Collider>();
                Vector3 center=collider!=null ? collider.bounds.center : portal.transform.position;
                Vector3 delta=center-eye;
                float facing=Vector3.Dot(forward,delta.normalized);
                if (facing<.1f) continue;
                float score=delta.magnitude+(1-facing)*3;
                if (score<best) { best=score; selectedPortal=portal; }
            }
            if (selectedPortal==null && inspectables!=null) foreach (var item in inspectables)
            {
                if (item==null || !item.CanInspect || item.gameObject.scene!=gameObject.scene) continue;
                Bounds bounds=item.FocusBounds;
                float distance=Vector3.Distance(eye,bounds.ClosestPoint(eye));
                if (distance>item.interactionDistance) continue;
                Vector3 delta=bounds.center-eye;
                float dot=Vector3.Dot(forward,delta.normalized);
                if (dot<.94f || Blocked(eye,bounds.center,item.transform)) continue;
                float score=(1-dot)*20+distance*.1f;
                if (score<best) { best=score; selectedObject=item; }
            }
            if (selectedPortal!=null) view.SetPrompt(view.theme.enterLabel,view.theme.portalTitle);
            else if (selectedObject!=null) view.SetPrompt(view.theme.examineLabel,selectedObject.entry.title);
            else view.SetPrompt("","");
            view.SetFocus(selectedPortal!=null || selectedObject!=null);
        }
        bool Blocked(Vector3 start,Vector3 end,Transform target)
        {
            Vector3 delta=end-start;
            int count=Physics.RaycastNonAlloc(start,delta.normalized,hits,delta.magnitude,
                Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            // Saturated queries are treated as blocked instead of allowing through-wall inspection.
            if (count>=hits.Length) return true;
            for (int i=0;i<count;i++)
            {
                Transform hit=hits[i].collider.transform;
                if (hit==player || hit.IsChildOf(player) || hit==target || hit.IsChildOf(target)) continue;
                return true;
            }
            return false;
        }
        public void TryInteract()
        {
            if (IsReading) { CloseEntry(); return; }
            if (Time.unscaledTime<nextUse) return;
            nextUse=Time.unscaledTime+.2f;
            if (selectedPortal!=null && selectedPortal.CanInteract)
            {
                var destination=selectedPortal;
                ClearTarget();
                SceneController.GetOrCreate().TransitionToDestination(destination);
            }
            else if (selectedObject!=null && selectedObject.CanInspect)
            {
                openedObject=selectedObject;
                LockInput();
                IsReading=true;
                view.ShowEntry(openedObject.entry);
                view.SetPrompt("","");
            }
        }
        void LockInput()
        {
            lockedInput=player!=null ? player.GetComponent<StarterAssetsInputs>() : null;
            lockedPlayerInput=player!=null ? player.GetComponent<PlayerInput>() : null;
            originalCursor=Cursor.lockState; originalCursorVisible=Cursor.visible;
            if (lockedInput!=null)
            {
                originalLook=lockedInput.cursorInputForLook;
                originalCursorLocked=lockedInput.cursorLocked;
                lockedInput.cursorInputForLook=false; lockedInput.cursorLocked=false;
            }
            if (lockedPlayerInput!=null)
            {
                originalInputActive=lockedPlayerInput.inputIsActive;
                originalMap=lockedPlayerInput.currentActionMap?.name;
                lockedPlayerInput.DeactivateInput();
            }
            ClearPlayerValues();
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
        void ClearPlayerValues()
        {
            if (lockedInput==null) return;
            lockedInput.move=Vector2.zero; lockedInput.look=Vector2.zero;
            lockedInput.jump=false; lockedInput.sprint=false;
        }
        public void CloseEntry()
        {
            if (!IsReading) return;
            IsReading=false; openedObject=null;
            view.HideEntry();
            ClearPlayerValues();
            if (lockedInput!=null)
            { lockedInput.cursorInputForLook=originalLook; lockedInput.cursorLocked=originalCursorLocked; }
            if (lockedPlayerInput!=null && originalInputActive)
            {
                lockedPlayerInput.ActivateInput();
                if (!string.IsNullOrEmpty(originalMap) && lockedPlayerInput.currentActionMap?.name!=originalMap)
                    lockedPlayerInput.SwitchCurrentActionMap(originalMap);
            }
            Cursor.lockState=originalCursor; Cursor.visible=originalCursorVisible;
            lockedInput=null; lockedPlayerInput=null;
            nextUse=Time.unscaledTime+.2f;
        }
        void OnDisable()
        {
            CloseEntry();
            SceneManager.sceneUnloaded-=SceneUnloaded;
            if (Active==this) Active=null;
        }
    }
}
