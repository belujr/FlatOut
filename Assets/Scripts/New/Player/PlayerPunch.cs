using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerPunch : MonoBehaviour
{
	[Header("References")]
	public PlayerController playerController;

	[Header("Left Arm")]
	public Rigidbody leftArmRb;
	public Transform leftHand;
	public PlayerGrab leftGrab;

	[Header("Right Arm")]
	public Rigidbody rightArmRb;
	public Transform rightHand;
	public PlayerGrab rightGrab;

	[Header("Heavy Punch Settings (Preserved)")]
	public float punchThrustForce = 30f;
	public float punchDamage = 10f;
	public float punchKnockbackStrength = 15f;
	public float punchVerticalLift = 0.1f;
	public float punchActiveDuration = 0.3f;
	public float hitboxRadius = 0.4f;
	public float punchStunDuration = 1.0f;

	private bool isPunchingLeft = false;
	private bool isPunchingRight = false;

	void Awake()
	{
		if (playerController == null) playerController = GetComponent<PlayerController>();
		if (leftGrab == null && leftArmRb != null) leftGrab = leftArmRb.GetComponent<PlayerGrab>();
		if (rightGrab == null && rightArmRb != null) rightGrab = rightArmRb.GetComponent<PlayerGrab>();
	}

	public void OnPunchLeft(InputValue value)
	{
		if (playerController.isPlayingMinigame) return;
		if (value.isPressed && playerController.currentState != playerController.stateParry)
		{
			// Intercept immediately if holding food item to consume it
			if (TryUseSpecialItem(leftGrab)) return;

			if (!isPunchingLeft) StartCoroutine(PunchRoutine(leftArmRb, leftHand, true, leftGrab));
		}
	}

	public void OnPunchRight(InputValue value)
	{
		if (playerController.isPlayingMinigame) return;
		if (value.isPressed && playerController.currentState != playerController.stateParry)
		{
			// Intercept immediately if holding food item to consume it
			if (TryUseSpecialItem(rightGrab)) return;

			if (!isPunchingRight) StartCoroutine(PunchRoutine(rightArmRb, rightHand, false, rightGrab));
		}
	}

	private bool TryUseSpecialItem(PlayerGrab handGrab)
	{
		if (handGrab != null && handGrab.heldItem != null)
		{
			// Is it food? (Eat it instantly without launching a physical swing)
			ConsumableItem food = handGrab.heldItem.GetComponent<ConsumableItem>();
			if (food != null)
			{
				food.Consume(handGrab, playerController.playerID);
				return true;
			}
		}

		return false;
	}

	IEnumerator PunchRoutine(Rigidbody armRb, Transform handTransform, bool isLeft, PlayerGrab grabScript)
	{
		if (isLeft) isPunchingLeft = true;
		else isPunchingRight = true;

		if (armRb != null)
		{
			armRb.linearVelocity = Vector3.zero;
			armRb.AddForce(playerController.transform.forward * punchThrustForce, ForceMode.VelocityChange);
		}

		float timer = 0f;
		List<int> hitPlayerIDs = new List<int>();

		while (timer < punchActiveDuration)
		{
			bool isHoldingWeapon = grabScript != null && grabScript.heldItem != null;
			bool isHoldingMedkit = false;
			MedkitItem medkit = null;

			// Dynamically evaluate if we are currently holding a Medkit
			if (isHoldingWeapon)
			{
				medkit = grabScript.heldItem.GetComponentInChildren<MedkitItem>();
				if (medkit == null) medkit = grabScript.heldItem.GetComponentInParent<MedkitItem>();
				if (medkit != null) isHoldingMedkit = true;
			}

			// Run hit detection if hands are empty OR holding a medical tool
			if (!isHoldingWeapon || isHoldingMedkit)
			{
				// FORGIVING HITBOX: Triple the sphere radius during a medkit swing so it hits the floor perfectly!
				float currentRadius = isHoldingMedkit ? hitboxRadius * 3f : hitboxRadius;
				Collider[] hits = Physics.OverlapSphere(handTransform.position, currentRadius);

				foreach (Collider col in hits)
				{
					PlayerController victim = col.transform.root.GetComponent<PlayerController>();

					if (victim != null && victim.playerID != playerController.playerID && !hitPlayerIDs.Contains(victim.playerID))
					{
						hitPlayerIDs.Add(victim.playerID);

						// Handle tactical medical revival tracking
						if (isHoldingMedkit && medkit.TryHeal(victim, grabScript))
						{
							// Clear the punch state flag before dropping out of the thread loop
							if (isLeft) isPunchingLeft = false;
							else isPunchingRight = false;
							yield break;
						}

						// Execute standard physical damage/knockback on healthy characters
						ApplyPunchEffects(victim);
					}
				}
			}

			timer += Time.deltaTime;
			yield return null;
		}

		if (isLeft) isPunchingLeft = false;
		else isPunchingRight = false;
	}

	void ApplyPunchEffects(PlayerController victimControl)
	{
		if (victimControl.currentState == victimControl.stateParry)
		{
			Rigidbody myRb = playerController.GetComponent<Rigidbody>();
			if (myRb != null)
			{
				Vector3 bounceDir = (playerController.transform.position - victimControl.transform.position).normalized;
				bounceDir += Vector3.up * 0.2f;
				myRb.linearVelocity = new Vector3(0, myRb.linearVelocity.y, 0);
				myRb.AddForce(bounceDir * 6f, ForceMode.VelocityChange);
			}
			return;
		}

		victimControl.StopAllCoroutines();
		victimControl.StartCoroutine(HandlePunchStun(victimControl, punchStunDuration));

		Rigidbody victimRb = victimControl.GetComponent<Rigidbody>();
		if (victimRb != null)
		{
			Vector3 punchDir = playerController.transform.forward;
			Vector3 forceDirection = (punchDir + Vector3.up * punchVerticalLift).normalized;

			victimRb.linearVelocity = Vector3.zero;
			victimRb.AddForce(forceDirection * punchKnockbackStrength, ForceMode.VelocityChange);

			Vector3 spinAxis = Vector3.Cross(punchDir, Vector3.up);
			victimRb.AddTorque(spinAxis * (punchKnockbackStrength * 1.5f), ForceMode.VelocityChange);
		}
	}

	private IEnumerator HandlePunchStun(PlayerController victim, float duration)
	{
		victim.ChangeState(victim.stateRagdoll);
		yield return new WaitForSeconds(duration);

		if (!victim.isIncapacitated && victim.currentState == victim.stateRagdoll)
		{
			victim.ChangeState(victim.stateLocomotion);
		}
	}
}