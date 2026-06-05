using UnityEngine;

public class GarbageScheduleManager : MonoBehaviour
{
	[Header("Schedule Settings")]
	public float collectionHour = 6.0f; // 6 AM

	[Header("References")]
	public GarbageCollectionZone collectionZone;

	// THE FIX: We reference your central GameManager instead of making a new clock!
	public GameManager gameManager;

	private bool hasCollectedToday = false;

	void Update()
	{
		// Safety check to ensure GameManager is linked
		if (gameManager == null) return;

		// Check if your master game clock is currently in the 6 AM hour
		if (gameManager.currentHour >= collectionHour && gameManager.currentHour < collectionHour + 1f)
		{
			if (!hasCollectedToday)
			{
				if (collectionZone != null) collectionZone.CollectAllBags();
				hasCollectedToday = true;
			}
		}
		else
		{
			// Reset the flag once 7 AM hits
			hasCollectedToday = false;
		}
	}
}