using UnityEngine;

/// <summary>
/// BrokerAngerSystem — Owns the broker's anger bar (0–100).
///
/// All anger sources funnel through AddAnger():
///   • Garbage found (fresh)   → +5   (called by BrokerScanSystem)
///   • Garbage found (decayed) → +8   (called by BrokerScanSystem)
///   • Door not opened in time → +5   (called by BrokerController)
///   • Rent unpaid at month end→ +10  (self-triggered via EventBus.OnMonthChanged)
///
/// Anger tiers (what the broker does at each level):
///   0–29   Calm     — normal behaviour
///   30–59  Annoyed  — dialogue becomes sharper
///   60–89  Angry    — extra charges, loud complaints
///   90–99  Furious  — eviction warnings
///   100    GAME OVER — Time.timeScale = 0, pauses editor
///
/// Broadcasts EventBus.OnBrokerAngerChanged(int anger) on every change.
/// BrokerAngerUI listens to that event to update the percentage display.
///
/// Setup:
///   Attach to the Broker GameObject alongside BrokerAI.
///   Assign dialogueUI and bankAccount in Inspector.
/// </summary>
public class BrokerAngerSystem : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("References")]
    public BrokerDialogueUI dialogueUI;
    public SharedBankAccount bankAccount;

    [Header("Anger Amounts")]
    public int angerFreshGarbage = 5;
    public int angerDecayedGarbage = 8;
    public int angerDoorIgnored = 5;
    public int angerRentUnpaid = 10;

    [Header("Rent")]
    [Tooltip("Amount the broker demands at end of every month")]
    public float monthlyRent = 50000f;

    [Header("Debug — Read Only")]
    [SerializeField] private int _anger = 0;

    // ── Public read ───────────────────────────────────────────────────────────

    public int Anger => _anger;
    /// <summary>0–100 float, ready to display as percentage.</summary>
    public float AngerPercent => Mathf.Clamp(_anger, 0, 100);

    public enum Tier { Calm, Annoyed, Angry, Furious }

    public Tier GetTier()
    {
        if (_anger >= 90) return Tier.Furious;
        if (_anger >= 60) return Tier.Angry;
        if (_anger >= 30) return Tier.Annoyed;
        return Tier.Calm;
    }

    // ── Dialogue pools ────────────────────────────────────────────────────────

    private static readonly string[] _furiousLines =
    {
        "One more problem and you are OUT of this house!",
        "I have never seen such a mess in my life. FINAL warning!",
        "My patience is finished. Fix everything or face eviction!",
    };

    private static readonly string[] _rentUnpaidLines =
    {
        "WHERE IS MY RENT?! You owe me ₹{0}!",
        "No rent AGAIN?! This is completely unacceptable — ₹{0} NOW!",
        "I am not running a charity. Pay your ₹{0} immediately!",
    };

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find bank if not assigned
        if (bankAccount == null)
            bankAccount = FindFirstObjectByType<SharedBankAccount>();

        if (bankAccount == null)
            Debug.LogWarning("[BrokerAngerSystem] SharedBankAccount not found — rent check disabled.");

        // Broadcast initial state so UI shows 0% on first frame
        Broadcast();
    }

    void OnEnable() { EventBus.OnMonthChanged += HandleMonthEnd; }
    void OnDisable() { EventBus.OnMonthChanged -= HandleMonthEnd; }

    // ── Public API — called by other broker scripts ───────────────────────────

    /// <summary>Call from BrokerScanSystem when garbage is spotted.</summary>
    public void ReportGarbage(bool isDecayed)
    {
        int amount = isDecayed ? angerDecayedGarbage : angerFreshGarbage;
        string label = isDecayed ? "decayed garbage" : "fresh garbage";
        AddAnger(amount, label);
    }

    /// <summary>Call from BrokerController when door wait timer expires.</summary>
    public void ReportDoorIgnored()
    {
        AddAnger(angerDoorIgnored, "door not opened in time");
    }

    // ── Month end — rent demand ───────────────────────────────────────────────

    private void HandleMonthEnd(int newMonth)
    {
        if (bankAccount == null) return;

        float balance = bankAccount.GetCurrentBalance();

        // Push a phone notification so players get warned
        EventBus.OnGenericTextNotification?.Invoke(
            "🏠 Rent Due!",
            $"Broker is demanding ₹{monthlyRent:0}. Balance: ₹{balance:0}");

        if (balance >= monthlyRent)
        {
            // Deduct via EventBus — SharedBankAccount handles it
            EventBus.OnMoneySpent?.Invoke(monthlyRent);
            Debug.Log($"<color=green>[BrokerAngerSystem] Rent ₹{monthlyRent:0} paid from balance ₹{balance:0}.</color>");
        }
        else
        {
            string line = string.Format(
                _rentUnpaidLines[Random.Range(0, _rentUnpaidLines.Length)],
                monthlyRent);
            dialogueUI?.Show(line);

            AddAnger(angerRentUnpaid,
                $"rent unpaid (balance ₹{balance:0} / need ₹{monthlyRent:0})");
        }
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void AddAnger(int amount, string reason)
    {
        _anger = Mathf.Clamp(_anger + amount, 0, 100);

        Debug.Log($"<color=red>[BrokerAngerSystem] +{amount} anger ({reason}) " +
                  $"→ {_anger}/100  [{GetTier()}]</color>");

        Broadcast();
        ReactToTier();

        if (_anger >= 100)
            TriggerGameOver();
    }

    private void Broadcast()
    {
        EventBus.OnBrokerAngerChanged?.Invoke(_anger);
    }

    private void ReactToTier()
    {
        if (GetTier() == Tier.Furious && dialogueUI != null)
        {
            string line = _furiousLines[Random.Range(0, _furiousLines.Length)];
            dialogueUI.Show(line);
        }
    }

    private void TriggerGameOver()
    {
        Debug.LogError(
            "<color=red>══════════════════════════════════════\n" +
            "  GAME OVER — Broker anger reached 100!\n" +
            "  The players have been EVICTED.\n" +
            "══════════════════════════════════════</color>");

        dialogueUI?.ShowPersistent("THAT IS IT! You are ALL EVICTED. GET OUT NOW!");

        Time.timeScale = 0f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = true;
#endif
    }

    // ── Debug helper (call from tests or a debug button) ─────────────────────

    public void DEBUG_ReduceAnger(int amount)
    {
        _anger = Mathf.Clamp(_anger - amount, 0, 100);
        Broadcast();
        Debug.Log($"<color=cyan>[BrokerAngerSystem] DEBUG anger reduced → {_anger}/100</color>");
    }
}