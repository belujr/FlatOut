using UnityEngine;

/// <summary>
/// FurnitureValue — Attach to any furniture GameObject to make it stealable.
///
/// SETUP:
///   1. Add this component to every furniture GameObject in the scene.
///   2. Set the "value" field to how much the item is worth (e.g. 500, 2000, 10000).
///   3. Optionally set "itemName" — if left blank it uses the GameObject name.
///
/// HOW STEALING WORKS:
///   BrokerTask_Steal reads all FurnitureValue objects in the scene.
///   Angry tier   → picks randomly from the cheapest 50% of items.
///   Furious tier → picks the single most expensive item.
///   When stolen:
///     • The GameObject is destroyed (item disappears from the apartment).
///     • EventBus.OnMoneySpent fires so the bank balance drops.
///     • A dialogue line plays.
/// </summary>
public class FurnitureValue : MonoBehaviour
{
    [Header("Item Info")]
    [Tooltip("Display name shown in dialogue. Leave blank to use the GameObject name.")]
    public string itemName = "";

    [Tooltip("How much this item is worth in ₹. Higher = more expensive = broker prefers it when Furious.")]
    public float value = 1000f;

    [Header("Steal State — Read Only")]
    [SerializeField] private bool _stolen = false;

    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>True once this item has been stolen this session.</summary>
    public bool IsStolen => _stolen;

    /// <summary>The name to show in dialogue — falls back to GameObject name.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(itemName) ? gameObject.name : itemName;

    /// <summary>
    /// Mark as stolen and destroy the GameObject after a short delay
    /// so the broker's walk-up animation can finish first.
    /// </summary>
    public void Steal(float destroyDelay = 1.2f)
    {
        if (_stolen) return;
        _stolen = true;
        Destroy(gameObject, destroyDelay);

        Debug.Log($"<color=red>[FurnitureValue] \"{DisplayName}\" (₹{value}) was stolen!</color>");
    }
}
