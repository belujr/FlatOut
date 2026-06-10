using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class GamblingManager : MonoBehaviour
{
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

	void OnEnable() { EventBus.OnStartGamblingMinigame += StartGambling; }
	void OnDisable() { EventBus.OnStartGamblingMinigame -= StartGambling; }

	private void StartGambling(float wager, PlayerController player)
	{
		activePlayer = player;
		currentWager = wager;
		currentMultiplier = 1;

		gamblingUIContainer.SetActive(true);
		SetupNextRound();
	}

	private void SetupNextRound()
	{
		isPlaying = true;
		isWaitingForChoice = false;

		// Progressive Math: Speed increases, safe zone shrinks every multiplier
		currentArrowSpeed = startingSpeed + (50f * (currentMultiplier - 1));
		float fillAmount = Mathf.Max(0.03f, startingSafeZoneFill - (0.02f * (currentMultiplier - 1)));

		safeZoneWidthAngles = fillAmount * 360f;
		safeZoneIndicator.GetComponent<Image>().fillAmount = fillAmount;

		safeZoneCenterAngle = Random.Range(45f, 315f);
		safeZoneIndicator.localRotation = Quaternion.Euler(0, 0, -safeZoneCenterAngle);
		currentArrowAngle = 0f;

		statusText.text = "PRESS ANY FACE BUTTON!";
		payoutText.text = $"POT: ${currentWager * currentMultiplier}\n<color=yellow>({currentMultiplier}x)</color>";
	}

	void Update()
	{
		if (!isPlaying || activePlayer == null) return;

		Gamepad pad = activePlayer.GetComponent<PlayerInput>().GetDevice<Gamepad>();
		if (pad == null) return;

		if (isWaitingForChoice)
		{
			HandlePlayerChoice(pad);
			return;
		}

		// Spin the arrow
		currentArrowAngle += currentArrowSpeed * Time.deltaTime;
		currentArrowAngle %= 360f;
		rotatingArrow.localRotation = Quaternion.Euler(0, 0, -currentArrowAngle);

		// Check for any hit button
		if (pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
			pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame)
		{
			float difference = Mathf.DeltaAngle(safeZoneCenterAngle + (safeZoneWidthAngles / 2f), currentArrowAngle);
			if (Mathf.Abs(difference) <= safeZoneWidthAngles / 2f)
			{
				// HIT! Pause and ask for choice
				isWaitingForChoice = true;
				statusText.text = "<color=green>HIT!</color>\n[X] CASH OUT | [A] DOUBLE DOWN";
			}
			else
			{
				// MISS! Brutal punishment
				LoseGambling();
			}
		}
	}

	private void HandlePlayerChoice(Gamepad pad)
	{
		if (pad.buttonWest.wasPressedThisFrame) // X Button
		{
			CashOut();
		}
		else if (pad.buttonSouth.wasPressedThisFrame) // A Button
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
		statusText.text = $"<color=green>CASHED OUT: ${payout}</color>";
		Invoke(nameof(CloseUI), 1.5f);
	}

	private void LoseGambling()
	{
		isPlaying = false;
		statusText.text = "<color=red>ARM SEVERED!</color>";
		EventBus.OnLimbSevered?.Invoke(activePlayer);
		Invoke(nameof(CloseUI), 2.0f);
	}

	private void CloseUI()
	{
		gamblingUIContainer.SetActive(false);
	}
}