using System.Collections;
using UnityEngine;

/// <summary>
/// BrokerTaskSO — Base ScriptableObject for every broker task.
///
/// HOW TO ADD A NEW TASK:
///   1. Create a new C# class that inherits BrokerTaskSO.
///   2. Override Execute() with your task logic.
///   3. In Unity: Assets > Create > Broker > Tasks > (your task type).
///   4. Set the angerTier field in the Inspector.
///   5. Drag the asset into BrokerTaskRunner.tasks list in the Inspector.
///      That's it — no code changes needed in BrokerTaskRunner.
///
/// TIER MATCHING:
///   BrokerTaskRunner filters tasks by the broker's CURRENT anger tier,
///   then picks one at random from the matching pool.
///   If no tasks exist for the current tier, it falls back to Calm tasks.
/// </summary>
public abstract class BrokerTaskSO : ScriptableObject
{
    [Header("Task Identity")]
    [Tooltip("Human-readable name shown in Inspector and logs")]
    public string taskName = "Unnamed Task";

    [Header("Anger Tier")]
    [Tooltip("The broker will only run this task when his anger is at this tier.\n" +
             "Calm = 0-29 | Annoyed = 30-59 | Angry = 60-89 | Furious = 90-100")]
    public BrokerAngerSystem.Tier angerTier = BrokerAngerSystem.Tier.Calm;

    // ── Execution ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Run the task. Yield until complete so BrokerController can block on it.
    /// Use runner.BrokerAI and runner.Dialogue for movement and speech.
    /// </summary>
    public abstract IEnumerator Execute(BrokerTaskRunner runner);
}
