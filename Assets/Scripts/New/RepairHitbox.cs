using UnityEngine;

public class RepairHitbox : MonoBehaviour
{
	public GigPropRepair mainRepairScript;
	public int taskIndexToTrigger = 0;

	[Tooltip("How fast the arm/item must be swinging relative to the body.")]
	public float minimumStrikeSpeed = 2.5f;


	void OnTriggerEnter(Collider other) { ProcessHit(other.gameObject); }
	void OnCollisionEnter(Collision collision) { ProcessHit(collision.gameObject); }

	[Header("Visual Feedback")]
	public GameObject targetIndicator; // Drag a glowing light or arrow here!

	void Update()
	{
		if (targetIndicator != null && mainRepairScript != null)
		{
			// Only show the glowing light if THIS is the current task, and a minigame isn't actively playing
			bool isCurrentTarget = (mainRepairScript.GetCurrentTaskIndex() == taskIndexToTrigger) && !mainRepairScript.isMinigameActive;
			targetIndicator.SetActive(isCurrentTarget);
		}
	}

	private void ProcessHit(GameObject hitObject)
	{
		GameObject rootHit = hitObject.transform.root.gameObject;

		// Get the specific body part or item that touched the box
		Rigidbody rb = hitObject.GetComponentInParent<Rigidbody>();
		if (rb == null) return;

		PlayerController player = rootHit.GetComponentInChildren<PlayerController>();
		float strikeSpeed = 0f;

		if (player != null && player.rigidbody3D != null)
		{
			// --- THE TRUE STRIKE MATH ---
			// Subtract the body's walking speed from the hand's speed. 
			// This proves the arm is actively swinging!
			strikeSpeed = (rb.linearVelocity - player.rigidbody3D.linearVelocity).magnitude;
		}
		else
		{
			// It's a thrown item! Just use its raw speed.
			strikeSpeed = rb.linearVelocity.magnitude;
		}

		if (strikeSpeed < minimumStrikeSpeed) return; // Too gentle! Ignore it.

		PlayerGrab playerHand = rootHit.GetComponentInChildren<PlayerGrab>();

		if (playerHand != null)
		{
			// THE FIX: Pass the player forward to the next script!
			mainRepairScript.TryInteractWithPart(taskIndexToTrigger, playerHand.heldItem, player);
		}
		else
		{
			mainRepairScript.TryInteractWithPart(taskIndexToTrigger, rootHit, player);
		}
	}
}