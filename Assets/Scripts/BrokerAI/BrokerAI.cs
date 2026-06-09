using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BrokerAI — Hub, storage, and reference point for the Broker.
/// Makes zero decisions. All logic lives in BrokerController / task scripts.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BrokerAI : MonoBehaviour
{
    // ── Scene References ──────────────────────────────────────────────────────

    [Header("Core Waypoints")]
    public Transform exitPoint;
    public Transform doorWaypoint;

    [Header("Task Waypoints")]
    public Transform kitchenWaypoint;
    public Transform hallWaypoint;
    public Transform[] roomWaypoints;

    [Header("Door Reference")]
    [Tooltip("The door instance. SetActive(false) = open. Null = also open.")]
    public GameObject doorObject;

    [Header("Movement")]
    public float arrivalRadius = 1.5f;

    // ── Runtime Storage (written by BrokerController) ─────────────────────────

    [HideInInspector] public HashSet<int> VisitDaysThisMonth = new HashSet<int>();
    [HideInInspector] public int TrackedMonth = -1;
    [HideInInspector] public bool VisitInProgress = false;

    // ── Component References (cached for other broker scripts) ────────────────

    public NavMeshAgent Agent { get; private set; }
    public BrokerAngerSystem AngerSystem { get; private set; }
    public BrokerScanSystem ScanSystem { get; private set; }

    // ── Door Query ────────────────────────────────────────────────────────────

    public bool IsDoorOpen() => doorObject == null || !doorObject.activeInHierarchy;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        AngerSystem = GetComponent<BrokerAngerSystem>();
        ScanSystem = GetComponent<BrokerScanSystem>();
    }

    void Start()
    {
        if (exitPoint != null)
            Agent.Warp(exitPoint.position);
    }

    // ── NavMesh Movement API ──────────────────────────────────────────────────

    public IEnumerator MoveTo(Vector3 destination)
    {
        if (!Agent.isOnNavMesh)
        {
            Debug.LogWarning("[BrokerAI] Agent not on NavMesh — movement skipped.");
            yield break;
        }

        Agent.isStopped = false;
        Agent.SetDestination(destination);

        yield return null;

        while (true)
        {
            if (Agent.pathPending) { yield return null; continue; }
            if (Agent.remainingDistance <= arrivalRadius) break;
            if (Agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                Debug.LogWarning("[BrokerAI] Path invalid — check waypoint NavMesh placement.");
                break;
            }
            yield return null;
        }

        Agent.isStopped = true;
    }

    public void StopMovement()
    {
        if (Agent != null && Agent.isOnNavMesh)
            Agent.isStopped = true;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public List<int> GetSortedVisitDays()
    {
        List<int> sorted = new List<int>(VisitDaysThisMonth);
        sorted.Sort();
        return sorted;
    }
}