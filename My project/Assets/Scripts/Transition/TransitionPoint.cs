using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TransitionPoint : MonoBehaviour
{
    // Keep the existing field names and enum order so Inspector settings survive.
    public enum TransitionType { SameScene, DifferentScene }

    [Header("Transition Info")]
    public string sceneName;
    public TransitionType transitionType;
    public TransitionDestination.DestinationTag destinationTag;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    public bool CanInteract
    {
        get
        {
            playerColliders.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
            return isActiveAndEnabled && playerColliders.Count > 0;
        }
    }

    private void Update()
    {
        // The shared HUD chooses exactly one target and consumes E once.
        if (Xiaoduan.Narrative.NarrativeInteractor.HasActiveInterface) return;
        playerColliders.RemoveWhere(collider => collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy);
        if (playerColliders.Count == 0) return;

        bool pressed = false;
#if ENABLE_INPUT_SYSTEM
        pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        pressed = Input.GetKeyDown(KeyCode.E);
#endif
        if (pressed) SceneController.GetOrCreate().TransitionToDestination(this);
    }

    private static bool IsPlayer(Collider other)
    {
        // Also accepts colliders on children of the tagged player.
        for (Transform item = other.transform; item != null; item = item.parent)
            if (item.CompareTag("Player")) return true;
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) playerColliders.Add(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsPlayer(other)) playerColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        playerColliders.Remove(other);
    }

    private void OnDisable()
    {
        playerColliders.Clear();
    }
}
