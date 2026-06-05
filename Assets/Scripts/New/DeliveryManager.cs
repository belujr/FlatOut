using UnityEngine;
using System.Collections;

public class DeliveryManager : MonoBehaviour
{
	public static DeliveryManager Instance;

	[Header("Spawn Settings")]
	public GameObject deliveryBoyPrefab;
	public Transform corridorSpawnPoint;
	public Transform playerFrontDoor;

	void Awake() { Instance = this; }

	void OnEnable() { EventBus.OnGigAccepted += DeliverGigDropoff; }
	void OnDisable() { EventBus.OnGigAccepted -= DeliverGigDropoff; }

	// --- 1. SHOP DELIVERIES ---
	public void PlaceOrder(GameObject itemPrefab, float minTime, float maxTime, int buyerID)
	{
		float waitTime = Random.Range(minTime, maxTime);
		StartCoroutine(DeliveryTimerRoutine(itemPrefab, waitTime, buyerID));
	}

	private IEnumerator DeliveryTimerRoutine(GameObject itemPrefab, float waitTime, int buyerID)
	{
		yield return new WaitForSeconds(waitTime);
		GameObject npc = Instantiate(deliveryBoyPrefab, corridorSpawnPoint.position, Quaternion.identity);
		DeliveryNPC aiScript = npc.GetComponent<DeliveryNPC>();
		aiScript.doorTarget = playerFrontDoor;
		aiScript.exitTarget = corridorSpawnPoint;
		aiScript.itemToDropPrefab = itemPrefab;

		// Wait until the NPC physically arrives at the door
		yield return new WaitUntil(() => npc == null || Vector3.Distance(npc.transform.position, playerFrontDoor.position) < 2.5f);

		// THE FIX: Text the specific player's phone right when the item drops!
		EventBus.OnPersonalTextNotification?.Invoke(buyerID, "DELIVERY", "Your item is at the door.");
	}

	// --- 2. GIG DROPOFFS ---
	private void DeliverGigDropoff(GigData gig)
	{
		if (gig.brokenPropPrefab != null)
		{
			StartCoroutine(GigDropoffRoutine(gig.brokenPropPrefab, 5f));
		}
	}

	private IEnumerator GigDropoffRoutine(GameObject itemPrefab, float waitTime)
	{
		yield return new WaitForSeconds(waitTime);
		GameObject npc = Instantiate(deliveryBoyPrefab, corridorSpawnPoint.position, Quaternion.identity);
		DeliveryNPC aiScript = npc.GetComponent<DeliveryNPC>();
		aiScript.doorTarget = playerFrontDoor;
		aiScript.exitTarget = corridorSpawnPoint;
		aiScript.itemToDropPrefab = itemPrefab;

		yield return new WaitUntil(() => npc == null || Vector3.Distance(npc.transform.position, playerFrontDoor.position) < 2.5f);
	}

	// --- 3. GIG PICKUPS (Called by the Return Zone) ---
	public void SendPickupBoy(GameObject itemToDestroy, float payoutAmount, GigData completedGig)
	{
		StartCoroutine(PickupRoutine(itemToDestroy, payoutAmount, completedGig));
	}

	private IEnumerator PickupRoutine(GameObject itemToDestroy, float payoutAmount, GigData completedGig)
	{
		GameObject npc = Instantiate(deliveryBoyPrefab, corridorSpawnPoint.position, Quaternion.identity);
		DeliveryNPC aiScript = npc.GetComponent<DeliveryNPC>();
		aiScript.doorTarget = playerFrontDoor;
		aiScript.exitTarget = corridorSpawnPoint;
		aiScript.itemToDropPrefab = null;

		yield return new WaitUntil(() => npc == null || Vector3.Distance(npc.transform.position, playerFrontDoor.position) < 2.5f);

		if (itemToDestroy != null) Destroy(itemToDestroy);

		EventBus.OnMoneyEarned?.Invoke(payoutAmount);
		EventBus.OnGigCompleted?.Invoke(completedGig);

		EventBus.OnGenericTextNotification?.Invoke("GIG COMPLETE", $"Client picked up the item. Received ${payoutAmount:F2}!");
	}
}