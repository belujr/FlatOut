using UnityEngine;
using System.Collections.Generic;

// Notice there is no [RequireComponent] here anymore! 
// You can add as many Box, Sphere, or Mesh Colliders as you need to fit the room perfectly.
public class TemperatureZone : MonoBehaviour
{
	[Header("Temperature Settings (°C)")]
	public float currentTemp = 24f;
	public float maxTemp = 35f;
	public float minTemp = 16f;
	public float heatIncreasePerHour = 1.5f;
	public float acCoolingPerHour = 3.0f;

	[Header("Visual Feedback (The Hot Pan Glow)")]
	[Tooltip("Drag the physical 3D objects (Floor, Walls) for THIS room here")]
	public Renderer[] roomSurfaces;

	[Tooltip("Use a deep, dark orange/red (e.g., #FF3300)")]
	[ColorUsage(true, true)]
	public Color hotGlowColor = new Color(1f, 0.2f, 0f);

	[Tooltip("How bright the glow gets at maximum heat (Keep this above your Bloom Threshold!)")]
	public float maxGlowIntensity = 2.5f;
	public float maximumGlowTemp = 35f;

	[Tooltip("How smoothly the color fades when the AC turns on/off")]
	public float visualFadeSpeed = 2f;

	[HideInInspector] public bool isACOn = false;

	private List<PlayerTemperatureEffects> playersInZone = new List<PlayerTemperatureEffects>();
	private Material[] surfaceMaterials;

	// The Ghost Variable for smooth fading
	private float displayedTemp;

	void Start()
	{
		displayedTemp = currentTemp;

		// Cache the materials and tell Unity they are allowed to emit light
		surfaceMaterials = new Material[roomSurfaces.Length];
		for (int i = 0; i < roomSurfaces.Length; i++)
		{
			if (roomSurfaces[i] != null)
			{
				surfaceMaterials[i] = roomSurfaces[i].material;
				surfaceMaterials[i].EnableKeyword("_EMISSION");
			}
		}
	}

	void OnEnable() { EventBus.OnTimeTick += HandleHourPassed; }
	void OnDisable() { EventBus.OnTimeTick -= HandleHourPassed; }

	// This handles the hard math every time the master clock ticks
	private void HandleHourPassed(int currentHour)
	{
		if (isACOn) currentTemp -= acCoolingPerHour;
		else currentTemp += heatIncreasePerHour;

		currentTemp = Mathf.Clamp(currentTemp, minTemp, maxTemp);

		// Update the local temperature for anyone standing in this specific room
		foreach (var player in playersInZone)
		{
			if (player != null) player.SetLocalTemperature(currentTemp);
		}
	}

	// This handles the smooth graphics every single frame
	void Update()
	{
		if (surfaceMaterials == null) return;

		// 1. Smoothly slide the displayed visual temperature towards the actual math temperature
		displayedTemp = Mathf.Lerp(displayedTemp, currentTemp, Time.deltaTime * visualFadeSpeed);

		// 2. Get the raw percentage (0.0 to 1.0)
		float rawPercentage = Mathf.InverseLerp(24f, maximumGlowTemp, displayedTemp);

		// 3. The Exponential Curve: Stays near 0 for a long time, then spikes aggressively at the end
		float heatCurve = Mathf.Pow(rawPercentage, 3f);

		// 4. Apply the curve to the color and intensity
		Color currentEmission = hotGlowColor * (heatCurve * maxGlowIntensity);

		foreach (Material mat in surfaceMaterials)
		{
			if (mat != null)
			{
				mat.SetColor("_EmissionColor", currentEmission);
			}
		}
	}

	// --- SPATIAL DETECTION: Who is in the AC range? ---
	void OnTriggerEnter(Collider other)
	{
		PlayerTemperatureEffects player = other.GetComponentInParent<PlayerTemperatureEffects>();
		if (player != null && !playersInZone.Contains(player))
		{
			playersInZone.Add(player);
			player.SetLocalTemperature(currentTemp); // Instantly update them when they walk in
		}
	}

	void OnTriggerExit(Collider other)
	{
		PlayerTemperatureEffects player = other.GetComponentInParent<PlayerTemperatureEffects>();
		if (player != null)
		{
			playersInZone.Remove(player);
		}
	}
}