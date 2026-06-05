using UnityEngine;

public class GarbageDecay : MonoBehaviour
{
	[Header("Decay Settings")]
	[Tooltip("How many in-game hours before it starts stinking?")]
	public float hoursUntilStink = 48f;
	[Tooltip("How fast does in-game time pass? (1 real second = X in-game hours)")]
	public float timeScaleMultiplier = 1f;

	[Header("Stink Consequences")]
	public float stinkRadius = 3f;
	[Tooltip("How much does this bother the NPC/Player? (Higher = worse)")]
	public int stinkSeverity = 1;
	public GameObject flyParticlesPrefab;

	private float currentAgeInHours = 0f;
	private bool isStinking = false;
	private GameObject activeFlies;

	void Update()
	{
		if (isStinking)
		{
			// Optional: Broadcast a "Stink Aura" to your EventBus or Physics system here
			// so your "Broke" flatmate knows to complain or lose stamina!
			EmitStinkAura();
			return;
		}

		// Age the garbage
		currentAgeInHours += Time.deltaTime * timeScaleMultiplier;

		if (currentAgeInHours >= hoursUntilStink)
		{
			TriggerStink();
		}
	}

	private void TriggerStink()
	{
		isStinking = true;

		// Spawn the flies!
		if (flyParticlesPrefab != null)
		{
			activeFlies = Instantiate(flyParticlesPrefab, transform.position, Quaternion.identity);
			activeFlies.transform.SetParent(this.transform); // Attach flies to the bag
		}
	}

	private void EmitStinkAura()
	{
		// This pushes an invisible sphere that NPCs can detect
		Collider[] hits = Physics.OverlapSphere(transform.position, stinkRadius);
		foreach (Collider col in hits)
		{
			if (col.CompareTag("Player") || col.CompareTag("Flatmate"))
			{
				// Trigger coughing, stamina drain, or complaints here!
				Debug.Log($"<color=green>STINK AURA hit {col.name}! Severity: {stinkSeverity}</color>");
			}
		}
	}

	// If the bag gets cleaned up or collected, reset it for the Object Pool!
	void OnDisable()
	{
		currentAgeInHours = 0f;
		isStinking = false;
		if (activeFlies != null) Destroy(activeFlies);
	}
}