using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// DoorController — Press Y (Gamepad) to open the door.
///
/// FIX: The reopen coroutine now runs on a separate persistent GameObject
/// that never gets deactivated, so SetActive(false) on the door can't kill it.
///
/// Y pressed  → door deactivates (broker sees open)
/// 10 seconds → door reactivates (broker sees closed)
///
/// BrokerAI.IsDoorOpen() must check:
///     doorObject == null || !doorObject.activeInHierarchy
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("Seconds until the door reappears after being opened")]
    public float reopenDelay = 10f;

    private bool _isOpen = false;

    // A separate always-active GameObject owns the coroutine
    // so deactivating the door never kills the timer
    private CoroutineRunner _runner;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        // Create a hidden persistent GameObject to host the reopen coroutine
        GameObject runnerGO = new GameObject("DoorCoroutineRunner");
        DontDestroyOnLoad(runnerGO);          // survives scene loads if needed
        _runner = runnerGO.AddComponent<CoroutineRunner>();
    }

    void Update()
    {
        if (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            if (!_isOpen)
                OpenDoor();
        }
    }

    void OnDestroy()
    {
        // Clean up the runner when this door is destroyed
        if (_runner != null)
            Destroy(_runner.gameObject);
    }

    // ── Door Logic ────────────────────────────────────────────────────────────

    private void OpenDoor()
    {
        _isOpen = true;
        gameObject.SetActive(false);    // hide door — this script's Update stops here

        Debug.Log($"<color=cyan>[DoorController] Door opened — reappears in {reopenDelay}s.</color>");

        // Runner stays active, so this coroutine is safe
        _runner.Run(ReopenAfterDelay());
    }

    private IEnumerator ReopenAfterDelay()
    {
        yield return new WaitForSeconds(reopenDelay);

        gameObject.SetActive(true);     // show door again
        _isOpen = false;

        Debug.Log("<color=cyan>[DoorController] Door closed again.</color>");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ForceOpen()  { if (!_isOpen) OpenDoor(); }

    public void ForceClose()
    {
        _runner.StopAll();
        gameObject.SetActive(true);
        _isOpen = false;
    }

    public bool IsOpen => _isOpen;
}


/// <summary>
/// Minimal MonoBehaviour that lives on its own always-active GameObject.
/// Exists only to host coroutines that must survive their owner being deactivated.
/// </summary>
public class CoroutineRunner : MonoBehaviour
{
    public void Run(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public void StopAll()
    {
        StopAllCoroutines();
    }
}
