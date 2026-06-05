using UnityEngine;

public class DayNightLighting : MonoBehaviour
{
	[Header("Light Reference")]
	public Light directionalLight;

	[Header("Lighting Schedule (24h time)")]
	[Tooltip("The hour the sun finishes rising (Match with GameManager DayStart)")]
	public float sunriseEndHour = 8f;

	[Tooltip("The hour the sun finishes setting (Match with GameManager NightStart)")]
	public float sunsetEndHour = 20f;

	[Tooltip("How many in-game hours before the end hour the fade begins")]
	public float transitionHours = 2f;

	[Header("Day Settings")]
	public Color dayColor = new Color(1f, 0.95f, 0.85f);
	public float dayIntensity = 1.2f;

	[Header("Night Settings")]
	public Color nightColor = Color.black; // Completely black as requested
	public float nightIntensity = 0f;      // 0 Light

	void OnEnable()
	{
		EventBus.OnTimeProgress += UpdateLighting;
	}

	void OnDisable()
	{
		EventBus.OnTimeProgress -= UpdateLighting;
	}

	private void UpdateLighting(float exactHour)
	{
		if (directionalLight == null) return;

		// Calculate when the transitions should start
		float sunsetStart = sunsetEndHour - transitionHours;
		float sunriseStart = sunriseEndHour - transitionHours;

		// 1. DAWN (Fading from Night to Day)
		if (exactHour >= sunriseStart && exactHour <= sunriseEndHour)
		{
			float t = (exactHour - sunriseStart) / transitionHours;
			directionalLight.color = Color.Lerp(nightColor, dayColor, t);
			directionalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
		}
		// 2. DUSK (Fading from Day to Night)
		else if (exactHour >= sunsetStart && exactHour <= sunsetEndHour)
		{
			float t = (exactHour - sunsetStart) / transitionHours;
			directionalLight.color = Color.Lerp(dayColor, nightColor, t);
			directionalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
		}
		// 3. FULL DAY
		else if (exactHour > sunriseEndHour && exactHour < sunsetStart)
		{
			directionalLight.color = dayColor;
			directionalLight.intensity = dayIntensity;
		}
		// 4. FULL NIGHT
		else
		{
			directionalLight.color = nightColor;
			directionalLight.intensity = nightIntensity;
		}
	}
}