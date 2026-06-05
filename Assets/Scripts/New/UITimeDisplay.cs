using UnityEngine;
using TMPro; // Make sure you are using TextMeshPro!

public class UITimeDisplay : MonoBehaviour
{
	[Header("UI Reference")]
	public TextMeshProUGUI timeText;

	void OnEnable()
	{
		// Subscribe to the clock tick
		EventBus.OnTimeTick += UpdateTimeDisplay;
	}

	void OnDisable()
	{
		EventBus.OnTimeTick -= UpdateTimeDisplay;
	}

	private void UpdateTimeDisplay(int currentHour)
	{
		if (timeText == null) return;

		// Convert 24-hour format to 12-hour AM/PM format
		string amPm = currentHour >= 12 ? "PM" : "AM";
		int displayHour = currentHour % 12;

		// Handle midnight and noon properly
		if (displayHour == 0) displayHour = 12;

		// Update the placeholder text
		timeText.text = $"{displayHour}:00 {amPm}";
	}
}