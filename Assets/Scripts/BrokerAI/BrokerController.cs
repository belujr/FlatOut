using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrokerController — Brain of the broker system.
///
/// Changes from previous version:
///   • Calls brokerAI.ScanSystem.OnBrokerEntered() / OnBrokerLeft()
///     so garbage scanning activates only while broker is inside.
///   • Calls brokerAI.AngerSystem.ReportDoorIgnored() when door
///     wait timer expires without the door opening.
/// </summary>
public class BrokerController : MonoBehaviour
{
    // ── State ─────────────────────────────────────────────────────────────────

    public enum BrokerState { Idle, Travelling, Waiting, Entering, Leaving }
    public BrokerState CurrentState { get; private set; } = BrokerState.Idle;

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Broker References")]
    public BrokerAI brokerAI;
    public BrokerTaskRunner taskRunner;
    public BrokerDialogueUI dialogueUI;

    [Header("Visit Scheduling")]
    public int minVisitsPerMonth = 2;
    public int maxVisitsPerMonth = 3;

    [Header("Door Wait")]
    [Tooltip("Seconds broker waits at door before giving up (and getting angry)")]
    public float waitDuration = 10f;

    [Header("Debug — Read Only")]
    [SerializeField] private int _visitsThisMonthDebug;
    [SerializeField] private List<int> _visitDaysDebugDisplay = new List<int>();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (brokerAI == null) Debug.LogError("[BrokerController] brokerAI not assigned!");
        if (taskRunner == null) Debug.LogError("[BrokerController] taskRunner not assigned!");
        if (dialogueUI == null) Debug.LogError("[BrokerController] dialogueUI not assigned!");

        if (taskRunner != null && brokerAI != null && dialogueUI != null)
            taskRunner.Init(brokerAI, dialogueUI);
    }

    void OnEnable() { EventBus.OnDayChanged += HandleDayChanged; }
    void OnDisable() { EventBus.OnDayChanged -= HandleDayChanged; }

    // ── Calendar ──────────────────────────────────────────────────────────────

    private void HandleDayChanged(int newDay, int currentMonth)
    {
        if (brokerAI == null) return;

        if (currentMonth != brokerAI.TrackedMonth)
            GenerateVisitSchedule(currentMonth);

        if (brokerAI.VisitDaysThisMonth.Contains(newDay) && !brokerAI.VisitInProgress)
        {
            Debug.Log($"<color=green>[BrokerController] Day {newDay} — broker heading out.</color>");
            StartCoroutine(VisitRoutine());
        }
    }

    private void GenerateVisitSchedule(int month)
    {
        brokerAI.TrackedMonth = month;
        brokerAI.VisitDaysThisMonth.Clear();

        int daysPerMonth = 15;
        CalendarManager cal = FindFirstObjectByType<CalendarManager>();
        if (cal != null) daysPerMonth = cal.daysPerMonth;

        int visitCount = Mathf.Clamp(
            Random.Range(minVisitsPerMonth, maxVisitsPerMonth + 1), 0, daysPerMonth);

        List<int> allDays = new List<int>();
        for (int d = 1; d <= daysPerMonth; d++) allDays.Add(d);
        for (int i = allDays.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (allDays[i], allDays[j]) = (allDays[j], allDays[i]);
        }
        for (int i = 0; i < visitCount; i++)
            brokerAI.VisitDaysThisMonth.Add(allDays[i]);

        _visitsThisMonthDebug = visitCount;
        _visitDaysDebugDisplay = brokerAI.GetSortedVisitDays();

        Debug.Log($"<color=green>[BrokerController] Month {month} — {visitCount} visits on days: " +
                  $"{string.Join(", ", _visitDaysDebugDisplay)}</color>");
    }

    // ── Visit State Machine ───────────────────────────────────────────────────

    private IEnumerator VisitRoutine()
    {
        brokerAI.VisitInProgress = true;

        // ── TRAVELLING ───────────────────────────────────────────────────────
        SetState(BrokerState.Travelling);
        yield return StartCoroutine(brokerAI.MoveTo(brokerAI.doorWaypoint.position));

        // ── WAITING — check door immediately ─────────────────────────────────
        SetState(BrokerState.Waiting);

        if (brokerAI.IsDoorOpen())
        {
            yield return StartCoroutine(HandleDoorOpen());
        }
        else
        {
            Debug.Log($"<color=green>[BrokerController] Not open — waiting {waitDuration}s.</color>");

            float elapsed = 0f;
            bool openedMidWait = false;

            while (elapsed < waitDuration)
            {
                yield return new WaitForSeconds(1f);
                elapsed += 1f;
                if (brokerAI.IsDoorOpen()) { openedMidWait = true; break; }
            }

            if (openedMidWait)
            {
                yield return StartCoroutine(HandleDoorOpen());
            }
            else
            {
                Debug.Log("<color=green>[BrokerController] Door never opened — broker going back.</color>");

                // ── ANGER: door ignored ───────────────────────────────────────
                brokerAI.AngerSystem?.ReportDoorIgnored();
            }
        }

        // ── LEAVING ──────────────────────────────────────────────────────────
        SetState(BrokerState.Leaving);

        // Tell scan system broker is leaving
        brokerAI.ScanSystem?.OnBrokerLeft();

        yield return StartCoroutine(brokerAI.MoveTo(brokerAI.exitPoint.position));

        brokerAI.transform.rotation = brokerAI.exitPoint.rotation;
        brokerAI.VisitInProgress = false;
        SetState(BrokerState.Idle);

        Debug.Log("<color=green>[BrokerController] Broker back at exit. Idle.</color>");
    }

    private IEnumerator HandleDoorOpen()
    {
        SetState(BrokerState.Entering);
        Debug.Log("<color=green>[BrokerController] Door open — broker entering.</color>");

        // Tell scan system broker is now inside — garbage scanning activates
        brokerAI.ScanSystem?.OnBrokerEntered();

        // Run one random task (task runner blocks until done)
        yield return StartCoroutine(taskRunner.RunRandomTask());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetState(BrokerState newState)
    {
        CurrentState = newState;
        Debug.Log($"<color=green>[BrokerController] ── State → {newState} ──</color>");
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void ForceVisit()
    {
        if (brokerAI == null || brokerAI.VisitInProgress) return;
        Debug.Log("<color=green>[BrokerController] ForceVisit() triggered.</color>");
        StartCoroutine(VisitRoutine());
    }

    public void ForceLeave()
    {
        if (brokerAI == null || !brokerAI.VisitInProgress) return;
        StopAllCoroutines();
        StartCoroutine(ForceLeaveRoutine());
    }

    private IEnumerator ForceLeaveRoutine()
    {
        brokerAI.StopMovement();
        brokerAI.ScanSystem?.OnBrokerLeft();
        dialogueUI.Hide();
        SetState(BrokerState.Leaving);
        yield return StartCoroutine(brokerAI.MoveTo(brokerAI.exitPoint.position));
        brokerAI.transform.rotation = brokerAI.exitPoint.rotation;
        SetState(BrokerState.Idle);
        brokerAI.VisitInProgress = false;
    }

    public BrokerState GetState() => CurrentState;
    public bool IsVisiting() => brokerAI != null && brokerAI.VisitInProgress;
}