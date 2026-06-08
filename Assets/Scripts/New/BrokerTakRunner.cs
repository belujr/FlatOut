using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrokerTaskRunner — Executes one of 4 tasks after the broker enters.
///
/// Called by BrokerController.HandleDoorOpen().
/// Reads waypoints from BrokerAI (the hub).
/// Fires EventBus.OnMoneySpent for Task 4.
///
/// Task 1 — Sit at random furniture (tag "furniture"), wait 5s, say tired line
/// Task 2 — Go to kitchen waypoint, say hungry/thirsty line
/// Task 3 — Visit all room waypoints in order, then return to exit
/// Task 4 — Go to hall waypoint, deduct ₹100, say complaint line
/// </summary>
public class BrokerTaskRunner : MonoBehaviour
{
    // ── Dialogue Lines ────────────────────────────────────────────────────────

    private static readonly string[] TiredLines = new string[]
    {
        "Wow... I am sitting here, feeling so tired.",
        "Ah, finally a place to rest. My feet are killing me.",
        "These stairs are too much. Let me just sit for a moment.",
    };

    private static readonly string[] FoodLines = new string[]
    {
        "Oh, I am hungry. Seeing if there is something to eat...",
        "Hey, I am thirsty. I need a glass of water.",
        "All this walking made me hungry. Any food around here?",
    };

    private static readonly string[] ChargeReasons = new string[]
    {
        "Your neighbours are complaining about the noise. Deducting ₹100.",
        "Your house looks very messy. Cleanliness fine — ₹100.",
        "Your house is so hot. Ventilation penalty — ₹100.",
        "It is maintenance charge time. ₹100 deducted.",
        "Noise complaints from below again. That will be ₹100.",
    };

    // ── Private References — set by BrokerController before calling RunTask ──

    private BrokerAI _brokerAI;
    private BrokerDialogueUI _dialogue;

    // ── Init (called once by BrokerController in Awake) ───────────────────────

    public void Init(BrokerAI brokerAI, BrokerDialogueUI dialogue)
    {
        _brokerAI = brokerAI;
        _dialogue = dialogue;
    }

    // ── Public Entry Point ────────────────────────────────────────────────────

    /// <summary>
    /// Pick a random task (1-4) and run it.
    /// Yield this from BrokerController — it blocks until the task is done.
    /// </summary>
    public IEnumerator RunRandomTask()
    {
        int task = Random.Range(1, 5); // 1, 2, 3, or 4
        Debug.Log($"<color=green>[BrokerTaskRunner] Running Task {task}.</color>");

        switch (task)
        {
            case 1: yield return StartCoroutine(Task1_SitAtFurniture()); break;
            case 2: yield return StartCoroutine(Task2_GoToKitchen()); break;
            case 3: yield return StartCoroutine(Task3_RoomTour()); break;
            case 4: yield return StartCoroutine(Task4_CollectCharge()); break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TASK 1 — Sit at random furniture
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator Task1_SitAtFurniture()
    {
        // Find all furniture GameObjects in the scene by tag
        GameObject[] furnitureObjects = GameObject.FindGameObjectsWithTag("furniture");

        if (furnitureObjects.Length == 0)
        {
            Debug.LogWarning("[BrokerTaskRunner] Task 1: No GameObjects with tag 'furniture' found. " +
                             "Tag your furniture objects in the Inspector.");
            yield break;
        }

        // Pick one at random
        GameObject target = furnitureObjects[Random.Range(0, furnitureObjects.Length)];

        Debug.Log($"<color=green>[BrokerTaskRunner] Task 1 — walking to furniture: {target.name}</color>");
        yield return StartCoroutine(_brokerAI.MoveTo(target.transform.position));

        // Sit (wait 5 seconds), show tired dialogue
        string line = TiredLines[Random.Range(0, TiredLines.Length)];
        _dialogue.ShowPersistent(line);

        yield return new WaitForSeconds(5f);

        _dialogue.Hide();
        Debug.Log("<color=green>[BrokerTaskRunner] Task 1 complete.</color>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TASK 2 — Go to kitchen
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator Task2_GoToKitchen()
    {
        if (_brokerAI.kitchenWaypoint == null)
        {
            Debug.LogWarning("[BrokerTaskRunner] Task 2: kitchenWaypoint not assigned on BrokerAI.");
            yield break;
        }

        Debug.Log("<color=green>[BrokerTaskRunner] Task 2 — walking to kitchen.</color>");
        yield return StartCoroutine(_brokerAI.MoveTo(_brokerAI.kitchenWaypoint.position));

        string line = FoodLines[Random.Range(0, FoodLines.Length)];
        _dialogue.Show(line);

        // Wait for dialogue to finish showing before returning
        yield return new WaitForSeconds(_dialogue.displayDuration);

        Debug.Log("<color=green>[BrokerTaskRunner] Task 2 complete.</color>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TASK 3 — Visit every room in order
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator Task3_RoomTour()
    {
        if (_brokerAI.roomWaypoints == null || _brokerAI.roomWaypoints.Length == 0)
        {
            Debug.LogWarning("[BrokerTaskRunner] Task 3: roomWaypoints array is empty on BrokerAI. " +
                             "Assign room waypoint Transforms in the Inspector.");
            yield break;
        }

        Debug.Log($"<color=green>[BrokerTaskRunner] Task 3 — inspecting {_brokerAI.roomWaypoints.Length} rooms.</color>");

        foreach (Transform room in _brokerAI.roomWaypoints)
        {
            if (room == null) continue;

            Debug.Log($"<color=green>[BrokerTaskRunner] Task 3 — visiting room: {room.name}</color>");
            yield return StartCoroutine(_brokerAI.MoveTo(room.position));

            // Short pause at each room so it looks like an inspection
            yield return new WaitForSeconds(1.5f);
        }

        Debug.Log("<color=green>[BrokerTaskRunner] Task 3 — room tour complete. Heading to exit.</color>");
        // BrokerController handles the final walk to exitPoint after this coroutine returns
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TASK 4 — Collect charge from hall
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator Task4_CollectCharge()
    {
        if (_brokerAI.hallWaypoint == null)
        {
            Debug.LogWarning("[BrokerTaskRunner] Task 4: hallWaypoint not assigned on BrokerAI.");
            yield break;
        }

        Debug.Log("<color=green>[BrokerTaskRunner] Task 4 — walking to hall to collect charge.</color>");
        yield return StartCoroutine(_brokerAI.MoveTo(_brokerAI.hallWaypoint.position));

        // Pick a random complaint reason
        string reason = ChargeReasons[Random.Range(0, ChargeReasons.Length)];
        _dialogue.Show(reason);

        // Deduct ₹100 via EventBus — SharedBankAccount handles it
        EventBus.OnMoneySpent?.Invoke(100f);
        Debug.Log("<color=green>[BrokerTaskRunner] Task 4 — ₹100 deducted via EventBus.OnMoneySpent.</color>");

        yield return new WaitForSeconds(_dialogue.displayDuration);

        Debug.Log("<color=green>[BrokerTaskRunner] Task 4 complete.</color>");
    }
}