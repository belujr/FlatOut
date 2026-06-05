using UnityEngine;
using UnityEngine.Events;

public class PlayerTemperatureEffects : MonoBehaviour
{
	[Header("Dependencies")]
	public PlayerController playerController;

	[Header("Phase 1: Weakness Settings")]
	public float dizzyThreshold = 30f;
	public float recoverySpeedMultiplier = 2f;

	[Tooltip("The lowest the spring strength will drop BEFORE they completely pass out (e.g., 0.15 = 15% strength)")]
	[Range(0f, 1f)]
	public float minimumSpringMultiplier = 0.15f;

	[Header("Phase 2: Heatstroke Collapse Settings")]
	public float secondsUntilCollapse = 15f;
	public UnityEvent onHeatstrokeCollapse;
	public UnityEvent onHeatstrokeRecover;

	// Internal State
	private bool isDizzy = false;
	[HideInInspector] public bool hasCollapsed = false;
	private float dizzyTimer = 0f;

	// --- SPATIAL TEMPERATURE ---
	[HideInInspector] public float currentBodyTemp = 24f;

	// The TemperatureZone calls this when the player is standing inside it
	public void SetLocalTemperature(float localRoomTemp)
	{
		currentBodyTemp = localRoomTemp;
	}

	void Update()
	{
		if (hasCollapsed) return;

		// 1. Check their local body temperature every frame
		if (currentBodyTemp >= dizzyThreshold && !isDizzy)
		{
			isDizzy = true;
			Debug.Log($"<color=orange>{gameObject.name} entered a hot zone. Muscles weakening!</color>");
		}
		else if (currentBodyTemp < dizzyThreshold && isDizzy)
		{
			isDizzy = false;

			if (!hasCollapsed)
			{
				Debug.Log($"<color=cyan>{gameObject.name} is cooling down and regaining strength.</color>");
			}
		}

		// 2. Handle the Heatstroke Timers
		if (isDizzy)
		{
			dizzyTimer += Time.deltaTime;

			if (dizzyTimer >= secondsUntilCollapse)
			{
				hasCollapsed = true;
				dizzyTimer = secondsUntilCollapse;

				Debug.Log($"<color=red>CRITICAL: {gameObject.name} passed out from Heatstroke!</color>");
				onHeatstrokeCollapse?.Invoke();
				return;
			}
		}
		else if (dizzyTimer > 0f)
		{
			dizzyTimer -= Time.deltaTime * recoverySpeedMultiplier;
			if (dizzyTimer < 0f) dizzyTimer = 0f;
		}

		// 3. Apply the Physics Degradation
		// Only run this if the player is NOT in the manual 'X' button ragdoll state
		if (playerController != null && playerController.currentState != playerController.stateRagdoll)
		{
			// Calculate the weakness, stopping at the minimum threshold you set in the Inspector
			float strengthMultiplier = Mathf.Lerp(1f, minimumSpringMultiplier, dizzyTimer / secondsUntilCollapse);
			ApplyWeaknessToJoints(strengthMultiplier);
		}
	}

	private void ApplyWeaknessToJoints(float strengthMultiplier)
	{
		if (playerController == null || playerController.allBodyJoints == null) return;

		for (int i = 0; i < playerController.allBodyJoints.Length; i++)
		{
			ConfigurableJoint joint = playerController.allBodyJoints[i];
			JointDrive originalDrive = playerController.originalJointDrives[i];

			JointDrive weakenedDrive = joint.slerpDrive;
			weakenedDrive.positionSpring = originalDrive.positionSpring * strengthMultiplier;
			joint.slerpDrive = weakenedDrive;
		}
	}

	// A future Medkit script can grab this component and call this method
	public void ReviveFromHeatstroke()
	{
		if (hasCollapsed)
		{
			hasCollapsed = false;
			dizzyTimer = 0f;
			ApplyWeaknessToJoints(1f); // Instantly restore full spring strength

			Debug.Log($"<color=green>{gameObject.name} was revived with a Medkit!</color>");
			onHeatstrokeRecover?.Invoke();
		}
	}
}