using UnityEngine;

public class GigReturnZone : MonoBehaviour
{
	private bool isWaitingForPickup = false;
	private float payoutWaiting = 0f;
	private GigData gigWaiting;

	void OnEnable() { EventBus.OnGigReadyForPickup += PrepareForPickup; }
	void OnDisable() { EventBus.OnGigReadyForPickup -= PrepareForPickup; }

	private void PrepareForPickup(GigData gig, float payout)
	{
		isWaitingForPickup = true;
		payoutWaiting = payout;
		gigWaiting = gig;
		EventBus.OnGenericTextNotification?.Invoke("GIG UPDATE", "Place the item outside your door.");
	}

	void OnTriggerEnter(Collider other)
	{
		if (isWaitingForPickup)
		{
			// Safety Check: Did they drop a physical item, and does its name match the Gig Prop?
			// (This prevents players from completing the cycle gig by throwing a hamburger in the zone!)
			if (other.GetComponentInParent<Rigidbody>() != null &&
				other.transform.root.name.Contains(gigWaiting.brokenPropPrefab.name))
			{
				Debug.Log("Item placed in return zone! Calling the client...");
				isWaitingForPickup = false;

				GameObject itemToReturn = other.transform.root.gameObject;

				// Tell the Delivery Manager to send the NPC!
				DeliveryManager.Instance.SendPickupBoy(itemToReturn, payoutWaiting, gigWaiting);
			}
		}
	}
}