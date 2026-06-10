using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class App_Casino : MonoBehaviour
{
	[Header("Panel Hookups")]
	public GameObject entirePhoneUI;
	public GameObject panelHome;
	public GameObject panelCasino;

	[Header("UI Hookups")]
	public TextMeshProUGUI currentWagerAmtText;
	public Button addButton;
	public Button subtractButton;
	public Button gambleButton;

	[Header("Betting Rules")]
	public float minWager = 400f;
	public float maxWager = 5000f;
	public float wagerStep = 100f;

	private float currentWager = 400f;
	private PlayerController localPlayer;

	// ---> THIS IS THE NEW METHOD FOR YOUR BUTTON <---
	public void OpenCasinoApp()
	{
		if (panelHome != null) panelHome.SetActive(false);
		if (panelCasino != null) panelCasino.SetActive(true);

		// Reset the wager when they open the app
		currentWager = minWager;
		UpdateWagerDisplay();

		if (addButton != null) addButton.Select();
	}

	void OnEnable()
	{
		if (addButton != null) addButton.onClick.AddListener(IncreaseWager);
		if (subtractButton != null) subtractButton.onClick.AddListener(DecreaseWager);
		if (gambleButton != null) gambleButton.onClick.AddListener(ConfirmBet);
	}

	void OnDisable()
	{
		if (addButton != null) addButton.onClick.RemoveListener(IncreaseWager);
		if (subtractButton != null) subtractButton.onClick.RemoveListener(DecreaseWager);
		if (gambleButton != null) gambleButton.onClick.RemoveListener(ConfirmBet);
	}

	private void IncreaseWager()
	{
		currentWager += wagerStep;
		if (currentWager > maxWager) currentWager = maxWager;
		UpdateWagerDisplay();
	}

	private void DecreaseWager()
	{
		currentWager -= wagerStep;
		if (currentWager < minWager) currentWager = minWager;
		UpdateWagerDisplay();
	}

	private void UpdateWagerDisplay()
	{
		if (currentWagerAmtText != null) currentWagerAmtText.text = currentWager.ToString();
	}

	private void ConfirmBet()
	{
		// Turn off the ENTIRE phone so the player can see the game world
		if (entirePhoneUI != null) entirePhoneUI.SetActive(false);
		if (panelCasino != null) panelCasino.SetActive(false);

		// Fire the event to start the spinning arrow minigame!
		EventBus.OnStartGamblingMinigame?.Invoke(currentWager, localPlayer);
	}
}