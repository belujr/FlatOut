using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BrokerController — Brain of the broker system.
///
/// Owns: State Machine · Calendar scheduling · Visit flow
/// Delegates movement to: BrokerAI.MoveTo()
/// Delegates task execution to: BrokerTaskRunner.RunRandomTask()
/// All scene references live on BrokerAI (the hub).
/// </summary>
public class BrokerController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  STATE
    // ─────────────────────────────────────────────────────────────────────────

    public enum BrokerState
    {
        Idle,
        Travelling,
        Waiting,
        Entering,
        Leaving
    }

    public BrokerState CurrentState { get; private set; } = BrokerState.Idle;

    // ─────────────────────────────────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Broker References")]
    [Tooltip("The Broker scene GameObject — must have BrokerAI + NavMeshAgent")]
    public BrokerAI brokerAI;

    [Tooltip("BrokerTaskRunner component — attach to same GameObject as BrokerAI")]
    public BrokerTaskRunner taskRunner;

    [Tooltip("BrokerDialogueUI component — attach to broker or its UI child")]
    public BrokerDialogueUI dialogueUI;

    [Header("Visit Scheduling")]
    [Tooltip("Minimum broker visits per month")]
    public int minVisitsPerMonth = 2;

    [Tooltip("Maximum broker visits per month")]
    public int maxVisitsPerMonth = 3;

    [Header("Door Wait")]
    [Tooltip("Seconds broker waits at door before giving up")]
    public float waitDuration = 10f;

    [Header("Debug — Read Only")]
    [SerializeField] private int _visitsThisMonthDebug;
    [SerializeField] private List<int> _visitDaysDebugDisplay = new List<int>();

    // ─────────────────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (brokerAI == null) Debug.LogError("[BrokerController] brokerAI not assigned!");
        if (taskRunner == null) Debug.LogError("[BrokerController] taskRunner not assigned!");
        if (dialogueUI == null) Debug.LogError("[BrokerController] dialogueUI not assigned!");

        // Give TaskRunner its references
        if (taskRunner != null && brokerAI != null && dialogueUI != null)
            taskRunner.Init(brokerAI, dialogueUI);
    }

    void OnEnable() { EventBus.OnDayChanged += HandleDayChanged; }
    void OnDisable() { EventBus.OnDayChanged -= HandleDayChanged; }

    // ─────────────────────────────────────────────────────────────────────────
    //  CALENDAR
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleDayChanged(int newDay, int currentMonth)
    {
        if (brokerAI == null) return;

        if (currentMonth != brokerAI.TrackedMonth)
            GenerateVisitSchedule(currentMonth);

        if (brokerAI.VisitDaysThisMonth.Contains(newDay) && !brokerAI.VisitInProgress)
        {
            Debug.Log($"<color=green>[BrokerController] Day {newDay} is a visit day — broker heading out.</color>");
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

        int visitCount = Random.Range(minVisitsPerMonth, maxVisitsPerMonth + 1);
        visitCount = Mathf.Clamp(visitCount, 0, daysPerMonth);

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

    // ─────────────────────────────────────────────────────────────────────────
    //  VISIT STATE MACHINE
    // ─────────────────────────────────────────────────────────────────────────

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

                if (brokerAI.IsDoorOpen())
                {
                    openedMidWait = true;
                    break;
                }
            }

            if (openedMidWait)
                yield return StartCoroutine(HandleDoorOpen());
            else
                Debug.Log("<color=green>[BrokerController] Not open — door never opened. Broker went back.</color>");
        }

        // ── LEAVING ──────────────────────────────────────────────────────────
        SetState(BrokerState.Leaving);
        yield return StartCoroutine(brokerAI.MoveTo(brokerAI.exitPoint.position));

        brokerAI.transform.rotation = brokerAI.exitPoint.rotation;
        brokerAI.VisitInProgress = false;
        SetState(BrokerState.Idle);

        Debug.Log("<color=green>[BrokerController] Broker back at exit. Idle.</color>");
    }

    /// <summary>Door is open — hand off to TaskRunner, then broker leaves.</summary>
    private IEnumerator HandleDoorOpen()
    {
        SetState(BrokerState.Entering);
        Debug.Log("<color=green>[BrokerController] Open game — door is open! Broker entering.</color>");

        // TaskRunner picks and runs one of the 4 tasks, then returns control here
        yield return StartCoroutine(taskRunner.RunRandomTask());

        // After task is done the leaving state is set back in VisitRoutine
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private void SetState(BrokerState newState)
    {
        CurrentState = newState;
        Debug.Log($"<color=green>[BrokerController] ── State → {newState} ──</color>");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

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