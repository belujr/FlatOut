using System.Collections;
using UnityEngine;

/// <summary>
/// BrokerTask_RoomTour — Broker inspects every room waypoint in order.
/// Default tier: Annoyed (he's becoming suspicious, wants to check rooms)
///
/// CREATE: Assets > Create > Broker > Tasks > Room Tour
/// </summary>
[CreateAssetMenu(menuName = "Broker/Tasks/Room Tour", fileName = "Task_RoomTour")]
public class BrokerTask_RoomTour : BrokerTaskSO
{
    [Header("Room Tour Settings")]
    [Tooltip("How long the broker pauses at each room")]
    public float pausePerRoom = 1.5f;

    public override IEnumerator Execute(BrokerTaskRunner runner)
    {
        if (runner.BrokerAI.roomWaypoints == null || runner.BrokerAI.roomWaypoints.Length == 0)
        {
            Debug.LogWarning("[BrokerTask_RoomTour] roomWaypoints array is empty on BrokerAI.");
            yield break;
        }

        Debug.Log($"<color=green>[BrokerTask_RoomTour] Inspecting {runner.BrokerAI.roomWaypoints.Length} rooms.</color>");

        foreach (Transform room in runner.BrokerAI.roomWaypoints)
        {
            if (room == null) continue;

            Debug.Log($"<color=green>[BrokerTask_RoomTour] Visiting room: {room.name}</color>");
            yield return runner.StartCoroutine(runner.BrokerAI.MoveTo(room.position));

            yield return new WaitForSeconds(pausePerRoom);
        }

        Debug.Log("<color=green>[BrokerTask_RoomTour] Room tour complete.</color>");
    }
}
