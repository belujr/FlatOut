using UnityEngine;

public class InteractableItem : MonoBehaviour
{
	[Tooltip("Drag the empty Socket child objects here!")]
	public Transform[] grabSockets;

	// The player's hand will call this to find the best place to hold on
	public Transform GetClosestSocket(Vector3 handPosition)
	{
		if (grabSockets == null || grabSockets.Length == 0)
		{
			return transform; // Fallback to the root center if you forgot to add sockets
		}

		Transform bestSocket = grabSockets[0];
		float closestDistance = float.MaxValue;

		// Loop through all sockets to find which one is closest to the grabbing hand
		foreach (Transform socket in grabSockets)
		{
			float dist = Vector3.Distance(handPosition, socket.position);
			if (dist < closestDistance)
			{
				closestDistance = dist;
				bestSocket = socket;
			}
		}

		return bestSocket;
	}
}