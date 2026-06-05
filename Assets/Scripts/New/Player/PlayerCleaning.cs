using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCleaning : MonoBehaviour
{
	[Header("Core References")]
	public PlayerController playerController;

	[Header("Tornado Settings")]
	[Tooltip("How fast the player must be spinning (Degrees per second)")]
	public float requiredSpinSpeed = 350f; // 350 is a good baseline for a fast joystick spin
	[Tooltip("How long they must maintain the spin to trigger the clean")]
	public float requiredSpinDuration = 1.0f;
	[Tooltip("How far the vacuum reach extends")]
	public float cleaningRadius = 3.5f;

	[Header("Garbage Conversion")]
	[Tooltip("How many pieces of small trash equal one big garbage bag?")]
	public int piecesPerBag = 3;
	[Tooltip("How long the bags stay frozen in the smoke so you can't kick them!")]
	public float smokeClearDelay = 1.5f; // <--- THE NEW INSPECTOR VARIABLE
	public GameObject smokeParticlesPrefab;
	public GameObject garbageBagPrefab;

	private float currentSpinTimer = 0f;

	void Awake()
	{
		if (playerController == null) playerController = GetComponent<PlayerController>();
	}

	void Update()
	{
		if (playerController.currentState == playerController.stateRagdoll || playerController.isPlayingMinigame)
		{
			currentSpinTimer = 0f;
			return;
		}

		float spinSpeed = Mathf.Abs(playerController.rigidbody3D.angularVelocity.y) * Mathf.Rad2Deg;

		if (spinSpeed >= requiredSpinSpeed)
		{
			currentSpinTimer += Time.deltaTime;

			//Debug.Log($"<color=cyan>TORNADO CHARGING: Speed {spinSpeed:F0} | Time: {currentSpinTimer:F2}</color>");//

			if (currentSpinTimer >= requiredSpinDuration)
			{
				TryCleanGarbage();
				currentSpinTimer = 0f;
			}
		}
		else
		{
			currentSpinTimer = Mathf.Max(0, currentSpinTimer - (Time.deltaTime * 0.5f));
		}
	}

	private void TryCleanGarbage()
	{
		Collider[] hits = Physics.OverlapSphere(transform.position, cleaningRadius);
		List<SmallGarbage> foundGarbage = new List<SmallGarbage>();

		foreach (Collider col in hits)
		{
			SmallGarbage trash = col.GetComponentInParent<SmallGarbage>();
			if (trash != null && !foundGarbage.Contains(trash))
			{
				foundGarbage.Add(trash);
			}
		}

		Debug.Log($"<color=orange>TORNADO FIRED! Found {foundGarbage.Count} pieces of garbage in range.</color>");

		if (foundGarbage.Count > 0)
		{
			StartCoroutine(CleanRoutine(foundGarbage));
		}
	}

	private IEnumerator CleanRoutine(List<SmallGarbage> trashList)
	{
		Vector3 floorCenter = Vector3.zero;
		foreach (var t in trashList) floorCenter += t.transform.position;
		floorCenter /= trashList.Count;
		floorCenter.y = transform.position.y;

		Vector3 directionToCamera = Vector3.zero;
		if (Camera.main != null) directionToCamera = (Camera.main.transform.position - floorCenter).normalized;

		Vector3 smokeSpawnPoint = floorCenter + (Vector3.up * 0.2f) + (directionToCamera * 2f);

		if (smokeParticlesPrefab != null)
		{
			GameObject smoke = Instantiate(smokeParticlesPrefab, smokeSpawnPoint, Quaternion.identity);
			Destroy(smoke, 3f);
		}

		yield return new WaitForSeconds(0.3f);

		foreach (var t in trashList)
		{
			Rigidbody rb = t.GetComponent<Rigidbody>();
			if (rb != null) rb.isKinematic = true;
			t.gameObject.SetActive(false);
		}

		yield return new WaitForSeconds(0.5f);

		int numberOfBags = Mathf.Max(1, trashList.Count / piecesPerBag);

		// THE FIX: Create a temporary list to hold the bags we are about to spawn
		List<Rigidbody> newlySpawnedBags = new List<Rigidbody>();

		if (garbageBagPrefab != null)
		{
			for (int i = 0; i < numberOfBags; i++)
			{
				Vector3 randomOffset = new Vector3(Random.Range(0.4f, 0.5f), 1f, Random.Range(0.4f, 0.5f));
				GameObject newBag = Instantiate(garbageBagPrefab, floorCenter + randomOffset, Quaternion.identity);

				// Freeze the bag's physics the exact millisecond it spawns!
				Rigidbody bagRb = newBag.GetComponent<Rigidbody>();
				if (bagRb != null)
				{
					bagRb.isKinematic = true;
					newlySpawnedBags.Add(bagRb); // Save it to the list
				}
			}
		}

		foreach (var t in trashList) Destroy(t.gameObject);

		// THE FIX: Pause the script until your specific smoke animation is totally finished
		yield return new WaitForSeconds(smokeClearDelay);

		// Now that the smoke is gone, unfreeze all the bags so they drop and can be kicked!
		foreach (Rigidbody rb in newlySpawnedBags)
		{
			if (rb != null) rb.isKinematic = false;
		}
	}
}