using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrokerTaskRunner — Picks and executes a broker task based on current anger tier.
///
/// SETUP:
///   1. Attach to the Broker GameObject.
///   2. In Inspector, populate the "Tasks" list with your BrokerTaskSO assets.
///   3. Make sure each asset has its angerTier set correctly.
///   4. BrokerController calls Init() once in Awake, then RunRandomTask() per visit.
///
/// TASK SELECTION LOGIC:
///   • Reads the current anger tier from BrokerAngerSystem.
///   • Filters the task list to only tasks matching that tier.
///   • Picks one at random from the filtered pool.
///   • If the filtered pool is empty, falls back to Calm-tier tasks.
///   • If that is also empty, logs a warning and skips.
///
/// ADDING NEW TASKS:
///   • Create a new class inheriting BrokerTaskSO.
///   • Override Execute().
///   • Create the asset, set its tier, add it to this list.
///   • Done — zero code changes here.
/// </summary>
public class BrokerTaskRunner : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Task Library")]
    [Tooltip("Drag ALL your BrokerTaskSO assets here. " +
             "The runner filters by anger tier at runtime.")]
    public List<BrokerTaskSO> tasks = new List<BrokerTaskSO>();

    // ── Public References (read by task SOs via runner parameter) ─────────────

    /// <summary>Access to movement, waypoints, etc.</summary>
    public BrokerAI BrokerAI { get; private set; }

    /// <summary>Access to the speech bubble.</summary>
    public BrokerDialogueUI Dialogue { get; private set; }

    // ── Init (called once by BrokerController in Awake) ───────────────────────

    public void Init(BrokerAI brokerAI, BrokerDialogueUI dialogue)
    {
        BrokerAI = brokerAI;
        Dialogue = dialogue;
    }

    // ── Public Entry Point ────────────────────────────────────────────────────

    /// <summary>
    /// Pick a task matching the broker's current anger tier and run it.
    /// Yield this coroutine from BrokerController — it blocks until done.
    /// </summary>
    public IEnumerator RunRandomTask()
    {
        BrokerTaskSO chosen = PickTask();

        if (chosen == null)
        {
            Debug.LogWarning("[BrokerTaskRunner] No task found — skipping task phase.");
            yield break;
        }

        Debug.Log($"<color=green>[BrokerTaskRunner] Running task: \"{chosen.taskName}\" " +
                  $"(Tier: {chosen.angerTier})</color>");

        yield return StartCoroutine(chosen.Execute(this));

        Debug.Log($"<color=green>[BrokerTaskRunner] Task complete: \"{chosen.taskName}\"</color>");
    }

    // ── Task Picker ───────────────────────────────────────────────────────────

    private BrokerTaskSO PickTask()
    {
        BrokerAngerSystem.Tier currentTier = GetCurrentTier();

        // 1 — Try to find tasks matching the current tier
        List<BrokerTaskSO> pool = GetTasksForTier(currentTier);

        // 2 — Fallback: if no tasks for this tier, use Calm tasks
        if (pool.Count == 0 && currentTier != BrokerAngerSystem.Tier.Calm)
        {
            Debug.LogWarning($"[BrokerTaskRunner] No tasks for tier {currentTier} — " +
                             "falling back to Calm tasks.");
            pool = GetTasksForTier(BrokerAngerSystem.Tier.Calm);
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("[BrokerTaskRunner] Task list is empty. " +
                             "Add BrokerTaskSO assets to BrokerTaskRunner.tasks in the Inspector.");
            return null;
        }

        return pool[Random.Range(0, pool.Count)];
    }

    private List<BrokerTaskSO> GetTasksForTier(BrokerAngerSystem.Tier tier)
    {
        List<BrokerTaskSO> result = new List<BrokerTaskSO>();
        foreach (BrokerTaskSO t in tasks)
        {
            if (t != null && t.angerTier == tier)
                result.Add(t);
        }
        return result;
    }

    private BrokerAngerSystem.Tier GetCurrentTier()
    {
        if (BrokerAI != null && BrokerAI.AngerSystem != null)
            return BrokerAI.AngerSystem.GetTier();

        // Fallback: no anger system found, default to Calm
        Debug.LogWarning("[BrokerTaskRunner] AngerSystem not found — defaulting to Calm tier.");
        return BrokerAngerSystem.Tier.Calm;
    }
}