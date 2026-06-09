using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrokerTask_Steal — Broker enters, walks to a furniture item, steals it, and leaves.
///
/// TIER BEHAVIOUR:
///   Angry   → picks randomly from the CHEAPEST half of available furniture.
///             (He's annoyed, grabs whatever is easy.)
///   Furious → picks the MOST EXPENSIVE item available.
///             (He's done being patient — takes your best thing.)
///
/// You need TWO assets from this class:
///   • Task_StealCheap   — set angerTier = Angry
///   • Task_StealExpensive — set angerTier = Furious
///   Both use this same script; the tier field controls which stealing logic runs.
///
/// CREATE: Assets > Create > Broker > Tasks > Steal Furniture
///
/// SETUP:
///   • Attach FurnitureValue to every furniture GameObject in the scene.
///   • Add both steal assets to BrokerTaskRunner.tasks list.
///   • Make sure furniture GameObjects are tagged "furniture".
/// </summary>
[CreateAssetMenu(menuName = "Broker/Tasks/Steal Furniture", fileName = "Task_StealFurniture")]
public class BrokerTask_Steal : BrokerTaskSO
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Steal Settings")]
    [Tooltip("How long the broker stands next to the item before it disappears " +
             "(gives time for a pick-up animation if you have one)")]
    public float stealPauseSeconds = 1.5f;

    [Tooltip("0.0 – 1.0  What fraction of the cheapest items count as 'low cost'.\n" +
             "0.5 = cheapest half. Only used when tier is Angry.")]
    [Range(0.01f, 1f)]
    public float cheapFraction = 0.5f;

    // ── Dialogue lines ────────────────────────────────────────────────────────

    private static readonly string[] StealLines =
    {
        "I'm taking this. Consider it compensation.",
        "This is mine now. You brought this on yourself.",
        "Payment in kind. Should have kept the place clean.",
        "I'll be taking that. Don't test me again.",
    };

    private static readonly string[] WarningLines =
    {
        "You better not make me angry next time.",
        "Next time I visit, everything had better be perfect.",
        "Don't push me again or I'll take something even bigger.",
        "Consider this a final warning. I am NOT joking.",
    };

    // ── Execute ───────────────────────────────────────────────────────────────

    public override IEnumerator Execute(BrokerTaskRunner runner)
    {
        // ── 1. Find all stealable furniture ───────────────────────────────────
        FurnitureValue[] allFurniture = Object.FindObjectsByType<FurnitureValue>(FindObjectsSortMode.None);

        // Filter out already-stolen items
        List<FurnitureValue> available = new List<FurnitureValue>();
        foreach (FurnitureValue f in allFurniture)
        {
            if (!f.IsStolen) available.Add(f);
        }

        if (available.Count == 0)
        {
            Debug.LogWarning("[BrokerTask_Steal] No FurnitureValue objects found in scene. " +
                             "Attach FurnitureValue to your furniture GameObjects.");
            yield break;
        }

        // ── 2. Pick target based on tier ──────────────────────────────────────
        FurnitureValue target = angerTier == BrokerAngerSystem.Tier.Furious
            ? PickMostExpensive(available)
            : PickRandomCheap(available);

        if (target == null)
        {
            Debug.LogWarning("[BrokerTask_Steal] Could not select a target furniture item.");
            yield break;
        }

        Debug.Log($"<color=red>[BrokerTask_Steal] Broker targeting \"{target.DisplayName}\" " +
                  $"(₹{target.value}) — Tier: {angerTier}</color>");

        // ── 3. Walk to the furniture ──────────────────────────────────────────
        yield return runner.StartCoroutine(runner.BrokerAI.MoveTo(target.transform.position));

        // ── 4. Show steal dialogue ────────────────────────────────────────────
        string stealLine = StealLines[Random.Range(0, StealLines.Length)];
        runner.Dialogue.ShowPersistent(stealLine);

        // ── 5. Pause next to item (pick-up moment) ────────────────────────────
        yield return new WaitForSeconds(stealPauseSeconds);

        // ── 6. Steal: destroy item + deduct money via EventBus ────────────────
        float stolenValue = target.value;
        string stolenName = target.DisplayName;

        target.Steal(destroyDelay: 0f); // destroy immediately
        EventBus.OnMoneySpent?.Invoke(stolenValue);

        Debug.Log($"<color=red>[BrokerTask_Steal] Stole \"{stolenName}\" — " +
                  $"₹{stolenValue} deducted via EventBus.OnMoneySpent.</color>");

        // ── 7. Switch to warning dialogue ─────────────────────────────────────
        string warningLine = WarningLines[Random.Range(0, WarningLines.Length)];
        runner.Dialogue.Show(warningLine);

        yield return new WaitForSeconds(runner.Dialogue.displayDuration);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Returns the single highest-value item.</summary>
    private FurnitureValue PickMostExpensive(List<FurnitureValue> items)
    {
        FurnitureValue best = null;
        foreach (FurnitureValue f in items)
        {
            if (best == null || f.value > best.value)
                best = f;
        }
        return best;
    }

    /// <summary>
    /// Sorts items cheapest-first, takes the bottom cheapFraction,
    /// then returns a random pick from that cheap pool.
    /// </summary>
    private FurnitureValue PickRandomCheap(List<FurnitureValue> items)
    {
        // Sort ascending by value
        items.Sort((a, b) => a.value.CompareTo(b.value));

        // Take the cheapest fraction (at least 1 item)
        int poolSize = Mathf.Max(1, Mathf.FloorToInt(items.Count * cheapFraction));
        List<FurnitureValue> cheapPool = items.GetRange(0, poolSize);

        return cheapPool[Random.Range(0, cheapPool.Count)];
    }
}
