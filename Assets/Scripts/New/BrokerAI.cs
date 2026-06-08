using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// BrokerAI — Hub, storage, and reference point for the Broker.
///
/// Holds ALL scene references and ALL runtime state.
/// BrokerController reads state and issues commands.
/// BrokerTaskRunner reads waypoints and calls MoveTo().
/// This script makes zero decisions.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BrokerAI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  SCENE REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Core Waypoints")]
    [Tooltip("Where the broker stands when not visiting — spawn / return point")]
    public Transform exitPoint;

    [Tooltip("Waypoint just outside the player's door")]
    public Transform doorWaypoint;

    [Header("Task Waypoints")]
    [Tooltip("Task 2 — Waypoint inside or near the kitchen")]
    public Transform kitchenWaypoint;

    [Tooltip("Task 4 — Waypoint inside the hall where the broker collects the charge")]
    public Transform hallWaypoint;

    [Tooltip("Task 3 — Every room waypoint the broker will visit in order")]
    public Transform[] roomWaypoints;

    [Header("Door Reference")]
    [Tooltip("The door instance. SetActive(false) = open. Null = also open.")]
    public GameObject doorObject;

    [Header("Movement")]
    [Tooltip("Metres from target that counts as 'arrived'")]
    public float arrivalRadius = 1.5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  RUNTIME STORAGE  (written by BrokerController)
    // ─────────────────────────────────────────────────────────────────────────

    [HideInInspector] public HashSet<int> VisitDaysThisMonth = new HashSet<int>();
    [HideInInspector] public int TrackedMonth = -1;
    [HideInInspector] public bool VisitInProgress = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  COMPONENT REF
    // ─────────────────────────────────────────────────────────────────────────

    public NavMeshAgent Agent { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  DOOR QUERY  (pure read — no decisions)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Door is open when the GameObject is null (destroyed) OR inactive (hidden by DoorController).
    /// </summary>
    public bool IsDoorOpen() => doorObject == null || !doorObject.activeInHierarchy;

    // ─────────────────────────────────────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        if (exitPoint != null)
            Agent.Warp(exitPoint.position);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  NAVMESH MOVEMENT API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Walk to destination. Yield from BrokerController or BrokerTaskRunner.</summary>
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
                Debug.LogWarning("[BrokerAI] Path invalid — check waypoint positions on NavMesh.");
                break;
            }
            yield return null;
        }

        Agent.isStopped = true;
    }

    /// <summary>Hard stop — used by ForceLeave.</summary>
    public void StopMovement()
    {
        if (Agent != null && Agent.isOnNavMesh)
            Agent.isStopped = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    public List<int> GetSortedVisitDays()
    {
        List<int> sorted = new List<int>(VisitDaysThisMonth);
        sorted.Sort();
        return sorted;
    }
}