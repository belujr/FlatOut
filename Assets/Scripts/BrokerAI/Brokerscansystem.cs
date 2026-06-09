using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrokerScanSystem — Always-active garbage detector on the Broker.
///
/// • Runs a Physics.OverlapSphere every scanInterval seconds.
/// • Detects any GameObject with SmallGarbage component OR tag "garbage".
/// • Checks GarbageDecay.IsStinking to decide fresh vs decayed anger.
/// • Each unique piece of garbage only triggers anger ONCE per visit
///   (tracked in _reportedThisVisit — cleared when broker leaves).
/// • Scan only fires anger while broker is inside (IsInsideHome = true).
///   BrokerController sets IsInsideHome on entry/exit.
/// • Gizmos draw the scan circle in the Scene view for easy debugging.
///
/// Setup:
///   Attach to the Broker GameObject alongside BrokerAI and BrokerAngerSystem.
///   Assign angerSystem in Inspector (or it auto-finds on Awake).
/// </summary>
public class BrokerScanSystem : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Scan Settings")]
    [Tooltip("Radius of the detection circle around the broker")]
    public float scanRadius = 5f;

    [Tooltip("Seconds between each scan check — lower = more responsive, higher = cheaper")]
    public float scanInterval = 1.5f;

    [Header("References")]
    [Tooltip("Auto-found if left empty")]
    public BrokerAngerSystem angerSystem;

    [Header("Gizmos")]
    [Tooltip("Colour of the scan circle in Scene view")]
    public Color gizmoColour = new Color(1f, 0.3f, 0f, 0.25f);
    public Color gizmoOutline = new Color(1f, 0.3f, 0f, 0.9f);

    // ── Runtime ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Set true by BrokerController when broker enters through the door.
    /// Set false when broker leaves. Scan still runs but anger is only
    /// added while this is true.
    /// </summary>
    [HideInInspector] public bool IsInsideHome = false;

    // Tracks which garbage objects already triggered anger this visit
    private HashSet<int> _reportedThisVisit = new HashSet<int>();

    private float _scanTimer = 0f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (angerSystem == null)
            angerSystem = GetComponent<BrokerAngerSystem>();

        if (angerSystem == null)
            angerSystem = FindFirstObjectByType<BrokerAngerSystem>();

        if (angerSystem == null)
            Debug.LogWarning("[BrokerScanSystem] BrokerAngerSystem not found — anger won't be reported.");
    }

    void Update()
    {
        _scanTimer += Time.deltaTime;
        if (_scanTimer >= scanInterval)
        {
            _scanTimer = 0f;
            Scan();
        }
    }

    // ── Public API — called by BrokerController ───────────────────────────────

    /// <summary>Call when broker enters the home — resets the reported set.</summary>
    public void OnBrokerEntered()
    {
        IsInsideHome = true;
        _reportedThisVisit.Clear();
        Debug.Log("<color=orange>[BrokerScanSystem] Broker entered — scan active, reported list cleared.</color>");
    }

    /// <summary>Call when broker leaves the home.</summary>
    public void OnBrokerLeft()
    {
        IsInsideHome = false;
        Debug.Log("<color=orange>[BrokerScanSystem] Broker left — scan passive.</color>");
    }

    // ── Scan Logic ────────────────────────────────────────────────────────────

    private void Scan()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, scanRadius);

        foreach (Collider col in hits)
        {
            // Accept if it has SmallGarbage component OR is tagged "garbage"
            bool hasScript = col.GetComponent<SmallGarbage>() != null;
            bool hasTag = col.CompareTag("garbage");

            if (!hasScript && !hasTag) continue;

            int id = col.gameObject.GetInstanceID();

            // Already reported this piece of garbage this visit — skip
            if (_reportedThisVisit.Contains(id)) continue;

            // Check decay state
            GarbageDecay decay = col.GetComponent<GarbageDecay>();
            bool stinking = decay != null && decay.IsStinking;

            Debug.Log($"<color=orange>[BrokerScanSystem] Garbage spotted: {col.name} " +
                      $"| Decayed: {stinking} | Inside: {IsInsideHome}</color>");

            // Only add anger if broker is currently inside
            if (IsInsideHome && angerSystem != null)
            {
                _reportedThisVisit.Add(id);
                angerSystem.ReportGarbage(stinking);
            }
        }
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        // Filled sphere (transparent)
        Gizmos.color = gizmoColour;
        Gizmos.DrawSphere(transform.position, scanRadius);

        // Outline ring
        Gizmos.color = gizmoOutline;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }

    void OnDrawGizmosSelected()
    {
        // Brighter when the broker is selected
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, scanRadius);
    }
}