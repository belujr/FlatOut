using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class GamblingManager : MonoBehaviour
{
	[Header("App Reference")]
	public App_Casino casinoApp;

	[Header("UI References")]
	public GameObject gamblingUIContainer;
	public RectTransform rotatingArrow;
	public RectTransform safeZoneIndicator;
	public TextMeshProUGUI statusText;
	public TextMeshProUGUI payoutText;

	[Header("Difficulty Tuning")]
	public float startingSpeed = 300f;
	public float startingSafeZoneFill = 0.15f;

	private PlayerController activePlayer;
	private float currentWager;
	private int currentMultiplier = 1;

	private bool isPlaying = false;
	private bool isWaitingForChoice = false;

	private float currentArrowAngle = 0f;
	private float currentArrowSpeed;
	private float safeZoneCenterAngle;
	private float safeZoneWidthAngles;

	// ---> THE FIX: The Grace Period timer
	private float inputCooldown = 0f;

	void OnEnable() { EventBus.OnStartGamblingMinigame += StartGambling; }
	void OnDisable() { EventBus.OnStartGamblingMinigame -= StartGambling; }

	private void StartGambling(float wager, PlayerController player)
	{
		activePlayer = player != null ? player : FindFirstObjectByType<PlayerController>();
		currentWager = wager;
		currentMultiplier = 1;

		if (gamblingUIContainer != null) gamblingUIContainer.SetActive(true);
		SetupNextRound();
	}

	private void SetupNextRound()
	{
		isPlaying = true;
		isWaitingForChoice = false;

		// ---> THE FIX: Ignore all buttons for 0.5 seconds so "Place Bet" doesn't instantly kill them!
		inputCooldown = 0.5f;

		currentArrowSpeed = startingSpeed + (50f * (currentMultiplier - 1));
		float fillAmount = Mathf.Max(0.03f, startingSafeZoneFill - (0.02f * (currentMultiplier - 1)));

		safeZoneWidthAngles = fillAmount * 360f;
		if (safeZoneIndicator != null) safeZoneIndicator.GetComponent<Image>().fillAmount = fillAmount;

		safeZoneCenterAngle = Random.Range(45f, 315f);
		if (safeZoneIndicator != null) safeZoneIndicator.localRotation = Quaternion.Euler(0, 0, -safeZoneCenterAngle);
		currentArrowAngle = 0f;

		if (statusText != null) statusText.text = "PRESS ANY FACE BUTTON!";
		if (payoutText != null) payoutText.text = $"POT: ${currentWager * currentMultiplier}\n<color=yellow>({currentMultiplier}x)</color>";
	}

	void Update()
	{
		if (!isPlaying) return;

		// ---> THE FIX: Reverted to standard Time.deltaTime!
		if (!isWaitingForChoice && rotatingArrow != null)
		{
			currentArrowAngle += currentArrowSpeed * Time.deltaTime;
			currentArrowAngle %= 360f;
			rotatingArrow.localRotation = Quaternion.Euler(0, 0, -currentArrowAngle);
		}

		// ---> THE FIX: Countdown the grace period. Do NOT read inputs if the cooldown is active!
		if (inputCooldown > 0f)
		{
			inputCooldown -= Time.deltaTime;
			return;
		}

		Gamepad pad = Gamepad.current;

		if (isWaitingForChoice)
		{
			if (pad != null) HandlePlayerChoice(pad);

			if (Keyboard.current != null)
			{
				if (Keyboard.current.xKey.wasPressedThisFrame) CashOut();
				else if (Keyboard.current.cKey.wasPressedThisFrame) { currentMultiplier++; SetupNextRound(); }
			}
			return;
		}

		bool actionTriggered = false;

		if (pad != null)
		{
			if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
				pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)
			{
				actionTriggered = true;
			}
		}

		if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) actionTriggered = true;

		if (actionTriggered)
		{
			float difference = Mathf.DeltaAngle(safeZoneCenterAngle + (safeZoneWidthAngles / 2f), currentArrowAngle);
			if (Mathf.Abs(difference) <= safeZoneWidthAngles / 2f)
			{
				isWaitingForChoice = true;
				if (statusText != null) statusText.text = "<color=green>HIT!</color>\n[X] CASH OUT | [A] DOUBLE DOWN";
			}
			else LoseGambling();
		}
	}

	private void HandlePlayerChoice(Gamepad pad)
	{
		if (pad.buttonWest.wasPressedThisFrame) CashOut();
		else if (pad.buttonSouth.wasPressedThisFrame)
		{
			currentMultiplier++;
			SetupNextRound();
		}
	}

	private void CashOut()
	{
		isPlaying = false;
		float payout = currentWager * currentMultiplier;
		EventBus.OnGamblingCashOut?.Invoke(payout);
		if (statusText != null) statusText.text = $"<color=green>CASHED OUT: ${payout}</color>";
		Invoke(nameof(CloseUI), 1.5f);
	}

	private void LoseGambling()
	{
		isPlaying = false;
		if (statusText != null) statusText.text = "<color=red>ARM SEVERED!</color>";
		EventBus.OnLimbSevered?.Invoke(activePlayer);
		Invoke(nameof(CloseUI), 2.0f);
	}

	private void CloseUI()
	{
		if (gamblingUIContainer != null) gamblingUIContainer.SetActive(false);
		if (casinoApp != null) casinoApp.OpenGameSelection();
	}
}