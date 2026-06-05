using UnityEngine;
using TMPro; // TextMeshPro
using System.Collections;

public class UIBankDisplay : MonoBehaviour
{
	[Header("UI References")]
	public TextMeshProUGUI balanceText;

	[Header("Visual Juice")]
	public Color normalColor = Color.white;
	public Color deductColor = new Color(1f, 0.3f, 0.3f); // Bright red
	public float flashDuration = 0.5f;

	void OnEnable()
	{
		EventBus.OnBalanceChanged += UpdateDisplay;
	}

	void OnDisable()
	{
		EventBus.OnBalanceChanged -= UpdateDisplay;
	}

	private void UpdateDisplay(float newBalance)
	{
		if (balanceText == null) return;

		// Update the text. The "N0" format adds commas (e.g. 1,500)
		balanceText.text = $"Rs. {newBalance:N0}";

		// Flash the text red to draw the player's eye
		StopAllCoroutines();
		StartCoroutine(FlashTextRoutine());
	}

	private IEnumerator FlashTextRoutine()
	{
		balanceText.color = deductColor;

		float timer = 0f;
		while (timer < flashDuration)
		{
			timer += Time.deltaTime;
			// Smoothly fade back to the normal color
			balanceText.color = Color.Lerp(deductColor, normalColor, timer / flashDuration);
			yield return null;
		}

		balanceText.color = normalColor;
	}
}