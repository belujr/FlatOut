using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
	[Header("Assign your empty GameObjects here!")]
	public Transform[] spawnPoints;

	public void OnPlayerJoined(PlayerInput playerInput)
	{
		int playerIndex = playerInput.playerIndex;

		if (spawnPoints.Length > 0)
		{
			Transform targetSpawn = spawnPoints[playerIndex % spawnPoints.Length];

			// 1. Move the root object to the spawn point
			playerInput.transform.position = targetSpawn.position;
			playerInput.transform.rotation = targetSpawn.rotation;

			// 2. Force Unity's physics engine to instantly update all child objects
			Physics.SyncTransforms();

			// 3. STOP THE RUBBER BAND: Find every single bone/limb and kill its momentum
			Rigidbody[] allBones = playerInput.GetComponentsInChildren<Rigidbody>();
			foreach (Rigidbody bone in allBones)
			{
				// THE FIX: Only touch the velocity if the bone is actually physics-driven
				if (!bone.isKinematic)
				{
					bone.linearVelocity = Vector3.zero;
					bone.angularVelocity = Vector3.zero;
				} // This closes the 'if' statement
			} // This closes the 'foreach' loop!

			Debug.Log($"Player {playerIndex + 1} safely spawned at {targetSpawn.name}");
		}
	}
}