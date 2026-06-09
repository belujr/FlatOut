using UnityEngine;

// --- THE NEW MAPPING SYSTEM ---
[System.Serializable]
public class TaskMeshMapping
{
	[Tooltip("Optional: The broken part to HIDE when this task finishes (e.g., flat tire)")]
	public GameObject brokenMesh;

	[Tooltip("Optional: The fixed part to SHOW when this task finishes (e.g., new tire, or missing chain)")]
	public GameObject fixedMesh;
}

public class GigPropRepair : MonoBehaviour
{
	public GigData associatedGig;

	[Header("Modular Part Repair")]
	[Tooltip("Map each task to the specific 3D meshes it affects. Index 0 = Task 1, Index 1 = Task 2, etc.")]
	public TaskMeshMapping[] partMappings;

	[Header("Feedback")]
	public ParticleSystem successParticles;

	private int currentTaskIndex = 0;
	[HideInInspector] public bool isMinigameActive = false;

	public int GetCurrentTaskIndex() { return currentTaskIndex; }

	void Start()
	{
		// Automatically set up the prop's initial broken state the exact millisecond it spawns!
		InitializeBrokenState();
	}

	private void InitializeBrokenState()
	{
		if (partMappings == null) return;

		// Go through every task mapping and ensure the broken mesh is ON and fixed mesh is OFF
		for (int i = 0; i < partMappings.Length; i++)
		{
			if (partMappings[i].brokenMesh != null) partMappings[i].brokenMesh.SetActive(true);
			if (partMappings[i].fixedMesh != null) partMappings[i].fixedMesh.SetActive(false);
		}
	}

	public void TryInteractWithPart(int taskIndex, GameObject itemUsed, PlayerController triggeringPlayer)
	{
		if (isMinigameActive || taskIndex < currentTaskIndex) return;

		if (triggeringPlayer == null) return;

		if (taskIndex != currentTaskIndex)
		{
			if (itemUsed != null)
			{
				Debug.Log($"Wrong part! You need to do task {currentTaskIndex} first.");
			}
			return;
		}

		GigTaskInfo currentTask = associatedGig.requiredTasks[currentTaskIndex];

		if (!string.IsNullOrEmpty(currentTask.requiredItemName))
		{
			if (itemUsed == null || !itemUsed.name.Contains(currentTask.requiredItemName))
			{
				EventBus.OnGenericTextNotification?.Invoke("MISSING ITEM", $"You need: {currentTask.requiredItemName}");
				return;
			}
		}

		isMinigameActive = true;
		EventBus.OnStartMinigame?.Invoke(currentTask, itemUsed, this, triggeringPlayer);
	}

	public void OnMinigameSuccess(GameObject usedItem)
	{
		isMinigameActive = false;
		if (successParticles != null) successParticles.Play();
		if (usedItem != null) Destroy(usedItem);

		EventBus.OnTaskCompleted?.Invoke(currentTaskIndex);

		// --- THE NEW MODULAR SWAP ---
		RepairCurrentPart();

		currentTaskIndex++;

		if (currentTaskIndex >= associatedGig.requiredTasks.Count)
		{
			EventBus.OnGenericTextNotification?.Invoke("REPAIR FINISHED", "Open your phone to Complete & Return.");
		}
	}

	private void RepairCurrentPart()
	{
		if (partMappings == null || currentTaskIndex >= partMappings.Length) return;

		// Turn OFF the broken mesh (if one exists)
		if (partMappings[currentTaskIndex].brokenMesh != null)
		{
			partMappings[currentTaskIndex].brokenMesh.SetActive(false);
		}

		// Turn ON the fixed mesh (if one exists)
		if (partMappings[currentTaskIndex].fixedMesh != null)
		{
			partMappings[currentTaskIndex].fixedMesh.SetActive(true);
		}
	}

	public void OnMinigameFailed()
	{
		isMinigameActive = false;
	}
}