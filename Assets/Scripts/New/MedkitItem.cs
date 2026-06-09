using UnityEngine;

public class MedkitItem : MonoBehaviour
{
	public bool TryHeal(PlayerController victim, PlayerGrab handHoldingMedkit)
	{
		if (victim == null || handHoldingMedkit == null) return false;

		// 1. Instead of trusting 'isIncapacitated', just check if they are actually on the floor!
		if (victim.currentState != victim.stateRagdoll) return false;

		bool successfullyHealed = false;

		// --- CHECK 1: HEATSTROKE ---
		PlayerTemperatureEffects tempEffects = victim.GetComponentInChildren<PlayerTemperatureEffects>();
		// Check if they officially collapsed, OR if they are just stuck in a hot room
		if (tempEffects != null && (tempEffects.hasCollapsed || tempEffects.currentBodyTemp >= tempEffects.dizzyThreshold))
		{
			// THE FIX: Force the internal boolean to true so the Revive method is GUARANTEED to run!
			tempEffects.hasCollapsed = true;
			tempEffects.ReviveFromHeatstroke();
			successfullyHealed = true;
		}

		// --- CHECK 2: STARVATION ---
		PlayerPhone phone = victim.GetComponentInChildren<PlayerPhone>();
		if (phone != null && phone.currentHunger <= 5f)
		{
			phone.SetHunger(phone.maxHunger / 2f);
			phone.hasPassedOutFromHunger = false;
			successfullyHealed = true;
		}

		// --- RESOLUTION ---
		if (successfullyHealed)
		{
			// THE SUPER WAKE-UP: We manually rip them out of the ragdoll state 
			// guaranteeing they stand up even if the other scripts fail!
			victim.isIncapacitated = false;
			victim.ChangeState(victim.stateLocomotion);

			Debug.Log($"<color=green>MEDKIT USED! Revived {victim.gameObject.name}.</color>");

			handHoldingMedkit.ForceReleaseWeapon();
			Destroy(gameObject);
			return true;
		}

		return false;
	}
}