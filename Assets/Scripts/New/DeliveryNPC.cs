using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class DeliveryNPC : MonoBehaviour
{
	[HideInInspector] public Transform doorTarget;
	[HideInInspector] public Transform exitTarget;
	[HideInInspector] public GameObject itemToDropPrefab;

	[Header("Audio")]
	public AudioClip doorbellSound;

	private NavMeshAgent agent;
	private AudioSource audioSource;

	private bool hasDroppedItem = false;
	private bool isReadyToDespawn = false; // <-- NEW: A safety flag for walking away!

	void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		audioSource = GetComponent<AudioSource>();

		audioSource.spatialBlend = 1f;
		audioSource.playOnAwake = false;

		if (doorTarget != null)
		{
			// THE FIX: Calculate exactly where the door is
			Vector3 directionToDoor = doorTarget.position - transform.position;
			directionToDoor.y = 0; // Ignore height so he doesn't tilt up or down

			// Instantly snap his rotation to face the door before taking a single step!
			transform.rotation = Quaternion.LookRotation(directionToDoor);

			// Now tell him to walk
			agent.SetDestination(doorTarget.position);
		}
	}

	void Update()
	{
		// 1. Check if we arrived at the door
		if (!hasDroppedItem && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
		{
			DropItemAndLeave();
		}

		// 2. THE FIX: Only check for the exit IF we have been given permission to despawn
		if (isReadyToDespawn && agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
		{
			Destroy(gameObject);
		}
	}

	private void DropItemAndLeave()
	{
		hasDroppedItem = true;

		if (itemToDropPrefab != null)
		{
			Instantiate(itemToDropPrefab, transform.position, transform.rotation);
		}

		if (doorbellSound != null && audioSource != null)
		{
			audioSource.PlayOneShot(doorbellSound);
		}

		Debug.Log("*DING DONG* Delivery is here!");
		

		if (exitTarget != null)
		{
			agent.SetDestination(exitTarget.position);
		}

		// THE FIX: Wait half a second before allowing the script to check the exit distance.
		// This gives the NavMeshAgent time to calculate the new path and start moving!
		Invoke("EnableDespawnCheck", 0.5f);
	}

	private void EnableDespawnCheck()
	{
		isReadyToDespawn = true;
	}
}