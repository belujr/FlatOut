using UnityEngine;
using System.Collections.Generic;

public class GigManager : MonoBehaviour
{
	[Header("The Gig Pool")]
	public List<GigData> availableGigs;

	[Header("Dynamic Gig Timers")]
	public int activeStartHour = 8;
	public int activeEndHour = 22;
	[Range(0f, 100f)] public float spawnChancePerHour = 25f;
	public int cooldownHours = 3;

	private int hoursSinceLastGig = 99;

	// --- NEW: Track what the player currently has on their phone ---
	private List<GigData> activeGigsOnPhone = new List<GigData>();

	void OnEnable()
	{
		EventBus.OnTimeTick += CheckForNewGigs;

		// Listen to the phone! When a gig is done, remove it from our tracking list.
		EventBus.OnGigCompleted += ClearGigFromMemory;
	}

	void OnDisable()
	{
		EventBus.OnTimeTick -= CheckForNewGigs;
		EventBus.OnGigCompleted -= ClearGigFromMemory;
	}

	private void CheckForNewGigs(int currentHour)
	{
		hoursSinceLastGig++;

		if (currentHour >= activeStartHour && currentHour <= activeEndHour)
		{
			if (hoursSinceLastGig >= cooldownHours)
			{
				float diceRoll = Random.Range(0f, 100f);
				if (diceRoll <= spawnChancePerHour)
				{
					SendRandomGigToPhone();
				}
			}
		}
	}

	public void SendRandomGigToPhone()
	{
		if (availableGigs == null || availableGigs.Count == 0) return;

		// 1. Create a temporary pool of gigs that are NOT currently on the phone
		List<GigData> validGigs = new List<GigData>();
		foreach (GigData gig in availableGigs)
		{
			if (!activeGigsOnPhone.Contains(gig))
			{
				validGigs.Add(gig);
			}
		}

		// 2. If the player already has every gig in the game active, abort!
		if (validGigs.Count == 0) return;

		// 3. Pick a random gig from the VALID pool
		int randomIndex = Random.Range(0, validGigs.Count);
		GigData newGig = validGigs[randomIndex];

		// 4. Mark it as active so we don't double-text them
		activeGigsOnPhone.Add(newGig);
		hoursSinceLastGig = 0;

		EventBus.OnNewGigAvailable?.Invoke(newGig);
	}

	private void ClearGigFromMemory(GigData completedGig)
	{
		// When the player finishes the job, remove it from the active list
		if (activeGigsOnPhone.Contains(completedGig))
		{
			activeGigsOnPhone.Remove(completedGig);

			// --- THE FIX ---
			// Reset the cooldown timer to 0 right now so they are guaranteed 
			// a break before the cell tower is allowed to text them again!
			hoursSinceLastGig = 0;
		}
	}
}