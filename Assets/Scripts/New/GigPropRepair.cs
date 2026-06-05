using UnityEngine;

public class GigPropRepair : MonoBehaviour
{
	public GigData associatedGig;

	[Header("Feedback")]
	public ParticleSystem successParticles;

	// A helper so the hitboxes can check if they are the current target
	public int GetCurrentTaskIndex() { return currentTaskIndex; }
	private int currentTaskIndex = 0;

	[HideInInspector] public bool isMinigameActive = false;

	// THE FIX: Now accepts the triggeringPlayer
	public void TryInteractWithPart(int taskIndex, GameObject itemUsed, PlayerController triggeringPlayer)
	{
		if (isMinigameActive || taskIndex < currentTaskIndex) return;

		// Prevent random physics objects from triggering the minigame if no player threw it
		if (triggeringPlayer == null) return;

		if (taskIndex != currentTaskIndex)
		{
			// Only complain if they smacked it with an actual item!
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
		// THE FIX: Pass the player into the global event!
		EventBus.OnStartMinigame?.Invoke(currentTask, itemUsed, this, triggeringPlayer);
	}

	public void OnMinigameSuccess(GameObject usedItem)
	{
		isMinigameActive = false;
		if (successParticles != null) successParticles.Play();
		if (usedItem != null) Destroy(usedItem);

		EventBus.OnTaskCompleted?.Invoke(currentTaskIndex);
		currentTaskIndex++;

		if (currentTaskIndex >= associatedGig.requiredTasks.Count)
		{
			EventBus.OnGenericTextNotification?.Invoke("REPAIR FINISHED", "Open your phone to Complete & Return.");
		}
	}

	public void OnMinigameFailed()
	{
		isMinigameActive = false;
	}
}