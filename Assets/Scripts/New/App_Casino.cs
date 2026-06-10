using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem.UI;

public class App_Casino : MonoBehaviour
{
	[Header("Phone OS Panels")]
	public GameObject panelHome;
	public GameObject panelCasino;

	[Header("Casino Sub-Panels")]
	public GameObject panelGameSelection;
	public GameObject panelBetting;

	[Header("Buttons Hookups")]
	public Button fiveFingerFilletBtn;
	public Button addButton;
	public Button subtractButton;
	public Button gambleButton;

	[Header("UI Hookups")]
	public TextMeshProUGUI currentWagerAmtText;

	[Header("Betting Rules")]
	public float minWager = 400f;
	public float maxWager = 5000f;
	public float wagerStep = 100f;

	private float currentWager = 400f;
	public PlayerController localPlayer;
	private MultiplayerEventSystem localEventSystem;

	private void Awake()
	{
		// ---> THE FIX: Guarantee we find the player so controls actually lock!
		if (localPlayer == null) localPlayer = FindFirstObjectByType<PlayerController>();
		localEventSystem = GetComponentInParent<MultiplayerEventSystem>();
	}

	public void OpenCasinoApp()
	{
		if (panelHome != null) panelHome.SetActive(false);
		if (panelCasino != null) panelCasino.SetActive(true);

		OpenGameSelection();
	}

	public void OpenGameSelection()
	{
		if (panelBetting != null) panelBetting.SetActive(false);
		if (panelGameSelection != null) panelGameSelection.SetActive(true);

		if (localEventSystem != null && fiveFingerFilletBtn != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			localEventSystem.SetSelectedGameObject(fiveFingerFilletBtn.gameObject);
		}
	}

	public void OpenBettingScreen()
	{
		if (panelGameSelection != null) panelGameSelection.SetActive(false);
		if (panelBetting != null) panelBetting.SetActive(true);

		currentWager = minWager;
		UpdateWagerDisplay();

		if (localEventSystem != null && addButton != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			localEventSystem.SetSelectedGameObject(addButton.gameObject);
		}
	}

	public bool IsGamblingActive()
	{
		return panelCasino != null && panelCasino.activeSelf && !panelGameSelection.activeSelf && !panelBetting.activeSelf;
	}

	public void GoBack()
	{
		if (panelBetting != null && panelBetting.activeSelf)
		{
			OpenGameSelection();
		}
		else if (panelGameSelection != null && panelGameSelection.activeSelf)
		{
			ForceCloseToHome();
		}
	}

	public void ForceCloseToHome()
	{
		if (panelBetting != null) panelBetting.SetActive(false);
		if (panelGameSelection != null) panelGameSelection.SetActive(true);
		if (panelCasino != null) panelCasino.SetActive(false);
		if (panelHome != null) panelHome.SetActive(true);

		if (localEventSystem != null && panelHome != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			Transform casinoAppIcon = panelHome.transform.Find("BottomSection_Apps/App_Casino");
			if (casinoAppIcon != null) localEventSystem.SetSelectedGameObject(casinoAppIcon.gameObject);
		}
	}

	void OnEnable()
	{
		if (fiveFingerFilletBtn != null) fiveFingerFilletBtn.onClick.AddListener(OpenBettingScreen);
		if (addButton != null) addButton.onClick.AddListener(IncreaseWager);
		if (subtractButton != null) subtractButton.onClick.AddListener(DecreaseWager);
		if (gambleButton != null) gambleButton.onClick.AddListener(ConfirmBet);
	}

	void OnDisable()
	{
		if (fiveFingerFilletBtn != null) fiveFingerFilletBtn.onClick.RemoveListener(OpenBettingScreen);
		if (addButton != null) addButton.onClick.RemoveListener(IncreaseWager);
		if (subtractButton != null) subtractButton.onClick.RemoveListener(DecreaseWager);
		if (gambleButton != null) gambleButton.onClick.RemoveListener(ConfirmBet);

		ForceCloseToHome();
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
		if (panelBetting != null) panelBetting.SetActive(false);
		EventBus.OnStartGamblingMinigame?.Invoke(currentWager, localPlayer);
	}
}