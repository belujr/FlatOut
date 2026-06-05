using UnityEngine;
using System.Collections.Generic;

public class GarbageCollectionZone : MonoBehaviour
{
	[Header("Zone Memory")]
	public List<GameObject> bagsInZone = new List<GameObject>();

	void OnTriggerEnter(Collider other)
	{
		// If a garbage bag enters the zone, remember it!
		GarbageDecay decayScript = other.GetComponentInParent<GarbageDecay>();

		// We only want to collect BAGS, not small wrappers the player kicked outside
		if (decayScript != null && other.name.Contains("Bag"))
		{
			if (!bagsInZone.Contains(other.gameObject))
			{
				bagsInZone.Add(other.gameObject);
			}
		}
	}

	void OnTriggerExit(Collider other)
	{
		// If the player drags the bag back inside, forget it!
		if (bagsInZone.Contains(other.gameObject))
		{
			bagsInZone.Remove(other.gameObject);
		}
	}

	// The Garbage Collector will call this method!
	public void CollectAllBags()
	{
		int collectedCount = bagsInZone.Count;

		foreach (GameObject bag in bagsInZone)
		{
			if (bag != null)
			{
				// Use gameObject.SetActive(false) here if you are using the Object Pool!
				// Otherwise, use Destroy(bag);
				bag.SetActive(false);
			}
		}

		bagsInZone.Clear();
		Debug.Log($"<color=yellow>GARBAGE MAN: Collected {collectedCount} bags!</color>");
	}
}