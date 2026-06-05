using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class MultiplayerCamera : MonoBehaviour
{
	[Header("Targets")]
	public List<Transform> players;
	public float searchInterval = 1f; // How often it scans the arena for new players

	[Header("Camera Positioning")]
	public bool followCenter = true; // If false, the camera stays locked looking at the fixedArenaCenter
	public Vector3 fixedArenaCenter = Vector3.zero;
	public Vector3 viewingOffset = new Vector3(0, 2f, 0); // Adds a little height so it looks at their chests/heads, not their feet

	[Header("Zoom Limits (Distance)")]
	public float minZoomDistance = 8f;  // The closest the camera is allowed to get
	public float maxZoomDistance = 25f; // The furthest the camera will zoom out

	[Tooltip("How far apart players need to be to trigger the max zoom out")]
	public float zoomLimiter = 15f;

	[Header("Smoothness")]
	public float smoothTime = 0.3f; // How "floaty" or snappy the camera is

	private Vector3 velocity;
	private Camera cam;

	void Start()
	{
		cam = GetComponent<Camera>();

		// Auto-scans for players every 1 second so you don't have to manually drag them into the script!
		InvokeRepeating(nameof(FindPlayers), 0f, searchInterval);
	}

	void FindPlayers()
	{
		players.Clear();
		// Finds all objects with the PlayerControl script
		PlayerController[] foundPlayers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

		foreach (PlayerController p in foundPlayers)
		{
			if (p != null)
			{
				players.Add(p.transform);
			}
		}
	}

	void LateUpdate()
	{
		if (players.Count == 0) return;

		// Clean up the list in case a player was destroyed (fell off the map, disconnected, etc.)
		players.RemoveAll(item => item == null);
		if (players.Count == 0) return;

		MoveAndZoom();
	}

	void MoveAndZoom()
	{
		// 1. Create an invisible bounding box around all active players
		Bounds bounds = new Bounds(players[0].position, Vector3.zero);
		for (int i = 0; i < players.Count; i++)
		{
			bounds.Encapsulate(players[i].position);
		}

		// 2. Find the Center Point of the fight
		Vector3 targetCenter = followCenter ? bounds.center : fixedArenaCenter;
		targetCenter += viewingOffset;

		// 3. Calculate how far apart the furthest players are
		float greatestDistance = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

		// 4. Calculate the required Zoom Distance based on the player distance
		float requiredDistance = Mathf.Lerp(minZoomDistance, maxZoomDistance, greatestDistance / zoomLimiter);

		// 5. Position the camera by pulling it BACKWARD from the center, along its current viewing angle
		Vector3 targetPosition = targetCenter - (transform.forward * requiredDistance);

		// 6. Smoothly move the camera to the new position
		transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
	}
}