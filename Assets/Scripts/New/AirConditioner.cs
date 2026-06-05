using UnityEngine;

public class AirConditioner : MonoBehaviour
{
	[Header("Dependencies")]
	[Tooltip("Drag the TemperatureZone (Room) that this AC unit cools down")]
	public TemperatureZone linkedZone;

	[Header("Visual Feedback")]
	[Tooltip("Drag the Particle System attached to this AC unit here")]
	public ParticleSystem coldAirParticles;

	[Tooltip("Drag the Sphere Renderer that acts as the indicator light here")]
	public Renderer indicatorLightRenderer;

	[Tooltip("The glowing color when ON (Intensity must be high enough to Bloom!)")]
	[ColorUsage(true, true)]
	public Color lightOnColor = new Color(0f, 1f, 0.2f) * 2.5f; // Default glowing green

	[Tooltip("The color when OFF")]
	[ColorUsage(true, true)]
	public Color lightOffColor = Color.black;

	[Header("Economy Settings")]
	public float costPerHour = 50f;

	// Unique material instance for this specific AC's light
	private Material indicatorMaterial;

	void OnEnable() { EventBus.OnTimeTick += ChargeElectricity; }
	void OnDisable() { EventBus.OnTimeTick -= ChargeElectricity; }

	void Start()
	{
		// 1. Initialize the indicator light material
		if (indicatorLightRenderer != null)
		{
			indicatorMaterial = indicatorLightRenderer.material; // Unique copy
			indicatorMaterial.EnableKeyword("_EMISSION");
		}

		// 2. Sync visuals immediately so it starts in the correct state
		UpdateVisualFeedback();
	}

	public void ToggleAC()
	{
		if (linkedZone == null) return;

		// Flip the state in the zone
		linkedZone.isACOn = !linkedZone.isACOn;
		Debug.Log($"{gameObject.name} is now {(linkedZone.isACOn ? "ON" : "OFF")}");

		// Update the particles and the light
		UpdateVisualFeedback();
	}

	private void UpdateVisualFeedback()
	{
		bool isOn = linkedZone != null && linkedZone.isACOn;

		// Handle the Steam Particles
		if (coldAirParticles != null)
		{
			if (isOn) coldAirParticles.Play();
			else coldAirParticles.Stop();
		}

		// Handle the Glowing Sphere
		if (indicatorMaterial != null)
		{
			indicatorMaterial.SetColor("_EmissionColor", isOn ? lightOnColor : lightOffColor);
		}
	}

	private void ChargeElectricity(int currentHour)
	{
		if (linkedZone != null && linkedZone.isACOn)
		{
			EventBus.OnMoneySpent?.Invoke(costPerHour);
		}
	}
}