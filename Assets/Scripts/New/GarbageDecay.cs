using UnityEngine;

public class GarbageDecay : MonoBehaviour
{
    [Header("Decay Settings")]
    [Tooltip("How many in-game hours before it starts stinking?")]
    public float hoursUntilStink = 48f;
    [Tooltip("How fast does in-game time pass? (1 real second = X in-game hours)")]
    public float timeScaleMultiplier = 1f;

    [Header("Stink Consequences")]
    public float stinkRadius = 3f;
    [Tooltip("How much does this bother the NPC/Player? (Higher = worse)")]
    public int stinkSeverity = 1;
    public GameObject flyParticlesPrefab;

    private float currentAgeInHours = 0f;
    private bool _isStinking = false;
    private GameObject activeFlies;

    // ── Public read — used by BrokerScanSystem ────────────────────────────────
    /// <summary>True once the garbage has aged past hoursUntilStink.</summary>
    public bool IsStinking => _isStinking;

    void Update()
    {
        if (_isStinking)
        {
            EmitStinkAura();
            return;
        }

        currentAgeInHours += Time.deltaTime * timeScaleMultiplier;

        if (currentAgeInHours >= hoursUntilStink)
            TriggerStink();
    }

    private void TriggerStink()
    {
        _isStinking = true;

        if (flyParticlesPrefab != null)
        {
            activeFlies = Instantiate(flyParticlesPrefab, transform.position, Quaternion.identity);
            activeFlies.transform.SetParent(this.transform);
        }
    }

    private void EmitStinkAura()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, stinkRadius);
        foreach (Collider col in hits)
        {
            if (col.CompareTag("Player") || col.CompareTag("Flatmate"))
            {
                Debug.Log($"<color=green>STINK AURA hit {col.name}! Severity: {stinkSeverity}</color>");
            }
        }
    }

    void OnDisable()
    {
        currentAgeInHours = 0f;
        _isStinking = false;
        if (activeFlies != null) Destroy(activeFlies);
    }
}