using UnityEngine;

public class ConsumableItem : MonoBehaviour
{
	[Header("Nutrition")]
	public float hungerRestored = 30f;

	[Header("Garbage")]
	[Tooltip("The empty wrapper or box to spawn after eating")]
	public GameObject garbagePrefab;

	// The hand script calls this when you press the punch button!
	public void Consume(PlayerGrab handThatIsHolding, int playerID)
	{
		// 1. Tell the global event system that this player ate!
		EventBus.OnPlayerRestoreHunger?.Invoke(playerID, hungerRestored);

		// 2. Safely let go of the item so the physics joints don't freak out
		handThatIsHolding.ForceReleaseWeapon();

		// 3. Spawn the empty wrapper exactly where the food was
		if (garbagePrefab != null)
		{
			Instantiate(garbagePrefab, transform.position, transform.rotation);
		}

		// 4. Destroy the actual food object
		Destroy(gameObject);
	}
}