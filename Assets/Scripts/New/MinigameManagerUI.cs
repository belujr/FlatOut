using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum QTEInputType { A_Button, B_Button, X_Button, Y_Button, RStick_Up, RStick_Down, RStick_Left, RStick_Right }

[System.Serializable]
public class QTEPromptConfig
{
	public QTEInputType inputType;
	public Sprite promptIcon;
}

public class MinigameManagerUI : MonoBehaviour
{
	[Header("UI Panels")]
	public GameObject mainMinigameContainer;
	public TextMeshProUGUI instructionsText;

	[Header("Dynamic Tracking & Scaling")]
	[Tooltip("How high above the player's feet the UI should float. Increase this if it overlaps their head!")]
	public float hoverHeight = 2.2f;

	[Tooltip("If true, the UI will shrink when the camera zooms out.")]
	public bool scaleWithDistance = true;

	[Tooltip("The camera distance where the UI is perfectly 1x scale.")]
	public float referenceDistance = 15f;
	public float minScale = 0.5f;
	public float maxScale = 1.5f;

	private Camera mainCamera;

	[Header("Polish & Feedback")]
	public TextMeshProUGUI feedbackText;

	[Header("Timing QTE Hookups")]
	public GameObject panelTimingQTE;
	public Image qteInstructionIcon;
	public RectTransform rotatingArrow;
	public RectTransform safeZoneIndicator;
	public QTEPromptConfig[] availableQTEPrompts;
	public float baseArrowSpeed = 250f;
	public float baseSafeZoneFill = 0.2f;

	[Header("Rotate Minigame Hookups")]
	public GameObject panelJoystickRotate;
	public Image rotateInstructionIcon;
	public Image circularProgressBar;
	public Sprite defaultRotateIcon;

	[Tooltip("How fast it drains back to 0 (Degrees per second)")]
	public float baseDrainRate = 180f;
	[Tooltip("How fast you MUST spin to fill the bar (Degrees per second)")]
	public float requiredSpinSpeed = 360f;

	// --- Player Isolation ---
	private PlayerController activePlayer;

	// --- State Tracking ---
	private GigTaskInfo currentTaskInfo;
	private int currentSequenceIndex = 0;

	// QTE Variables
	private int qteTotalSteps = 1;
	private int qteCurrentStep = 0;
	private float currentArrowAngle = 0f;
	private float currentArrowSpeed = 250f;
	private float safeZoneCenterAngle = 0f;
	private float safeZoneWidthAngles = 50f;
	private QTEPromptConfig currentQTEPrompt;
	private Vector2 lastRightStick;

	// Rotate Variables
	private float accumulatedRotation = 0f;
	private float requiredRotation = 1800f;
	private Vector2 lastStickPos;
	private float lastStickAngle;

	private bool isPlaying = false;
	private bool isEnding = false;
	private bool isTransitioning = false;
	private string activeMechanic = "";
	private GameObject currentItem;
	private GigPropRepair currentProp;
	private Coroutine feedbackCoroutine;

	void Start()
	{
		mainCamera = Camera.main;
	}

	void OnEnable() { EventBus.OnStartMinigame += StartMinigame; }
	void OnDisable() { EventBus.OnStartMinigame -= StartMinigame; }

	private void StartMinigame(object taskObj, GameObject item, GigPropRepair prop, PlayerController triggeringPlayer)
	{
		currentTaskInfo = taskObj as GigTaskInfo;
		if (currentTaskInfo == null || currentTaskInfo.mechanicsSequence.Count == 0) return;

		currentItem = item;
		currentProp = prop;
		activePlayer = triggeringPlayer;

		if (mainMinigameContainer != null) mainMinigameContainer.SetActive(true);
		if (feedbackText != null) feedbackText.gameObject.SetActive(false);

		isPlaying = true;
		isEnding = false;
		isTransitioning = false;

		currentSequenceIndex = 0;
		SetupCurrentMechanic();
	}

	private void SetupCurrentMechanic()
	{
		if (panelJoystickRotate != null) panelJoystickRotate.SetActive(false);
		if (panelTimingQTE != null) panelTimingQTE.SetActive(false);

		activeMechanic = currentTaskInfo.mechanicsSequence[currentSequenceIndex].ToString();

		if (activeMechanic.Contains("Rotate"))
		{
			if (rotateInstructionIcon != null && defaultRotateIcon != null) rotateInstructionIcon.sprite = defaultRotateIcon;
			if (instructionsText != null) instructionsText.text = "Spin Fast!";
			if (panelJoystickRotate != null) panelJoystickRotate.SetActive(true);

			accumulatedRotation = 0f;
			if (circularProgressBar != null) circularProgressBar.fillAmount = 0f;

			int requiredSpins = 1 + (currentTaskInfo.difficultyLevel * 2);
			requiredRotation = requiredSpins * 360f;
		}
		else if (activeMechanic.Contains("QTE"))
		{
			if (instructionsText != null) instructionsText.text = "Hit the Safe Zone!";
			if (panelTimingQTE != null) panelTimingQTE.SetActive(true);

			qteTotalSteps = currentTaskInfo.difficultyLevel;
			qteCurrentStep = 0;

			SetupNextQTEStep();
		}
	}

	private void HandleJoystickRotation()
	{
		if (activePlayer == null) return;

		PlayerInput myInput = activePlayer.GetComponent<PlayerInput>();
		Gamepad activePad = myInput != null ? myInput.GetDevice<Gamepad>() : null;
		if (activePad == null) return;

		Vector2 stick = activePad.rightStick.ReadValue();

		if (stick.sqrMagnitude > 0.5f)
		{
			float currentAngle = Mathf.Atan2(stick.y, stick.x) * Mathf.Rad2Deg;

			if (lastStickPos.sqrMagnitude > 0.5f)
			{
				float delta = Mathf.Abs(Mathf.DeltaAngle(lastStickAngle, currentAngle));
				float currentSpinSpeed = delta / Time.deltaTime;

				if (currentSpinSpeed > requiredSpinSpeed)
				{
					accumulatedRotation += delta;
				}
			}
			lastStickAngle = currentAngle;
		}
		lastStickPos = stick;

		float currentDrain = baseDrainRate + (currentTaskInfo.difficultyLevel * 30f);
		accumulatedRotation -= currentDrain * Time.deltaTime;
		accumulatedRotation = Mathf.Max(0, accumulatedRotation);

		if (circularProgressBar != null) circularProgressBar.fillAmount = accumulatedRotation / requiredRotation;

		if (accumulatedRotation >= requiredRotation) MechanicStepWon();
	}

	private void SetupNextQTEStep()
	{
		if (availableQTEPrompts == null || availableQTEPrompts.Length == 0) return;
		currentQTEPrompt = availableQTEPrompts[Random.Range(0, availableQTEPrompts.Length)];
		if (qteInstructionIcon != null) qteInstructionIcon.sprite = currentQTEPrompt.promptIcon;
		safeZoneCenterAngle = Random.Range(45f, 315f);
		safeZoneIndicator.localRotation = Quaternion.Euler(0, 0, -safeZoneCenterAngle);
		float currentFill = Mathf.Max(0.05f, baseSafeZoneFill - (0.03f * qteCurrentStep));
		safeZoneIndicator.GetComponent<Image>().fillAmount = currentFill;
		safeZoneWidthAngles = currentFill * 360f;
		currentArrowSpeed = baseArrowSpeed + (40f * qteCurrentStep);
		currentArrowAngle = 0f;

		Gamepad activePad = activePlayer != null ? activePlayer.GetComponent<PlayerInput>().GetDevice<Gamepad>() : null;
		lastRightStick = activePad != null ? activePad.rightStick.ReadValue() : Vector2.zero;
	}

	private void HandleTimingQTE()
	{
		currentArrowAngle += currentArrowSpeed * Time.deltaTime;
		currentArrowAngle %= 360f;
		rotatingArrow.localRotation = Quaternion.Euler(0, 0, -currentArrowAngle);

		bool actionTriggered = false;
		bool wrongActionTriggered = false;

		Gamepad activePad = activePlayer != null ? activePlayer.GetComponent<PlayerInput>().GetDevice<Gamepad>() : null;

		if (activePad != null)
		{
			bool a = activePad.buttonSouth.wasPressedThisFrame;
			bool b = activePad.buttonEast.wasPressedThisFrame;
			bool x = activePad.buttonWest.wasPressedThisFrame;
			bool y = activePad.buttonNorth.wasPressedThisFrame;

			Vector2 stick = activePad.rightStick.ReadValue();
			bool rsUp = stick.y > 0.5f && lastRightStick.y <= 0.5f;
			bool rsDown = stick.y < -0.5f && lastRightStick.y >= -0.5f;
			bool rsLeft = stick.x < -0.5f && lastRightStick.x >= -0.5f;
			bool rsRight = stick.x > 0.5f && lastRightStick.x <= 0.5f;
			lastRightStick = stick;

			if (a || b || x || y || rsUp || rsDown || rsLeft || rsRight)
			{
				if (currentQTEPrompt.inputType == QTEInputType.A_Button && a) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.B_Button && b) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.X_Button && x) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.Y_Button && y) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.RStick_Up && rsUp) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.RStick_Down && rsDown) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.RStick_Left && rsLeft) actionTriggered = true;
				else if (currentQTEPrompt.inputType == QTEInputType.RStick_Right && rsRight) actionTriggered = true;
				else wrongActionTriggered = true;
			}
		}

		if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) actionTriggered = true;

		if (actionTriggered)
		{
			float difference = Mathf.DeltaAngle(safeZoneCenterAngle + (safeZoneWidthAngles / 2f), currentArrowAngle);
			if (Mathf.Abs(difference) <= safeZoneWidthAngles / 2f)
			{
				qteCurrentStep++;
				if (qteCurrentStep >= qteTotalSteps) MechanicStepWon();
				else
				{
					ShowFeedback("PERFECT!", Color.yellow, 0.5f);
					SetupNextQTEStep();
				}
			}
			else FailMinigame("Bad Timing!");
		}
		else if (wrongActionTriggered) FailMinigame("Wrong Button!");
	}

	void Update()
	{
		// --- DYNAMIC 3D-TO-2D UI TRACKING & SCALING ---
		if (isPlaying && activePlayer != null && mainMinigameContainer != null)
		{
			if (mainCamera == null) mainCamera = Camera.main;

			// 1. Calculate the 3D position directly above the player
			Vector3 targetWorldPosition = activePlayer.transform.position + (Vector3.up * hoverHeight);

			// 2. Translate that 3D space into 2D screen pixels (Z axis becomes distance!)
			Vector3 screenPosition = mainCamera.WorldToScreenPoint(targetWorldPosition);

			// 3. Prevent UI from flipping out if the player gets behind the camera
			if (screenPosition.z > 0)
			{
				mainMinigameContainer.transform.position = screenPosition;

				// 4. THE NEW SCALING MATH
				if (scaleWithDistance)
				{
					// Divide our baseline reference distance by the actual distance to the player
					float dynamicScale = referenceDistance / screenPosition.z;

					// Clamp it so it doesn't become microscopically small or cover the entire screen
					dynamicScale = Mathf.Clamp(dynamicScale, minScale, maxScale);

					mainMinigameContainer.transform.localScale = Vector3.one * dynamicScale;
				}
			}
		}
		// ----------------------------------------------

		if (!isPlaying || isEnding || isTransitioning) return;

		if (activeMechanic.Contains("Rotate")) HandleJoystickRotation();
		else if (activeMechanic.Contains("QTE")) HandleTimingQTE();
	}

	private void MechanicStepWon()
	{
		currentSequenceIndex++;

		if (currentSequenceIndex >= currentTaskInfo.mechanicsSequence.Count)
		{
			WinMinigame();
		}
		else
		{
			StartCoroutine(TransitionToNextMechanic());
		}
	}

	private IEnumerator TransitionToNextMechanic()
	{
		isTransitioning = true;

		ShowFeedback("NEXT!", Color.cyan, 0.5f);

		if (panelJoystickRotate != null) panelJoystickRotate.SetActive(false);
		if (panelTimingQTE != null) panelTimingQTE.SetActive(false);

		// THE FIX: Increased the buffer from 0.15s to 0.4s.
		// This gives the player exactly enough time to stop spinning their joystick 
		// so it doesn't accidentally trigger the next QTE as a failure!
		yield return new WaitForSeconds(0.4f);

		SetupCurrentMechanic();
		isTransitioning = false;
	}

	private void ShowFeedback(string msg, Color col, float duration = 1.0f)
	{
		if (feedbackText != null)
		{
			feedbackText.text = msg;
			feedbackText.color = col;
			feedbackText.gameObject.SetActive(true);

			if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);

			feedbackCoroutine = StartCoroutine(FlashyTextRoutine(duration));
		}
	}

	private IEnumerator FlashyTextRoutine(float duration)
	{
		feedbackText.transform.localScale = Vector3.one * 1.5f;
		float t = 0;
		while (t < 0.15f)
		{
			t += Time.deltaTime;
			feedbackText.transform.localScale = Vector3.Lerp(Vector3.one * 1.5f, Vector3.one, t / 0.15f);
			yield return null;
		}
		feedbackText.transform.localScale = Vector3.one;
		yield return new WaitForSeconds(duration - 0.15f);
		feedbackText.gameObject.SetActive(false);
	}

	private void WinMinigame()
	{
		isEnding = true;
		ShowFeedback("REPAIR SUCCESS!", Color.green, 1.0f);
		currentProp.OnMinigameSuccess(currentItem);

		// 3. THE FIX: Dropped from 1.0 second to 0.25 seconds.
		// The UI will vanish almost instantly, making the gameplay feel fluid and continuous.
		Invoke(nameof(CloseUI), 0.25f);
	}

	private void FailMinigame(string reason)
	{
		isEnding = true;
		ShowFeedback($"MISTAKE! {reason}", Color.red, 1.0f);
		EventBus.OnGenericTextNotification?.Invoke(reason, "-$5 Penalty");
		EventBus.OnGigMistakeMade?.Invoke(5f);
		currentProp.OnMinigameFailed();

		StartCoroutine(HapticFeedbackRoutine(0.5f, 0.8f, 0.4f));

		// 4. THE FIX: Dropped from 1.5 seconds to 0.6 seconds.
		// Failing should feel punishing, but it shouldn't lock the player out of moving for an eternity.
		Invoke(nameof(CloseUI), 0.6f);
	}

	private IEnumerator HapticFeedbackRoutine(float lowFreq, float highFreq, float duration)
	{
		if (activePlayer == null) yield break;

		Gamepad activePad = activePlayer.GetComponent<PlayerInput>().GetDevice<Gamepad>();
		if (activePad != null)
		{
			activePad.SetMotorSpeeds(lowFreq, highFreq);
			yield return new WaitForSeconds(duration);
			if (activePad != null) activePad.SetMotorSpeeds(0f, 0f);
		}
	}

	private void CloseUI()
	{
		isPlaying = false;
		isEnding = false;
		if (mainMinigameContainer != null) mainMinigameContainer.SetActive(false);
		if (panelTimingQTE != null) panelTimingQTE.SetActive(false);
		if (panelJoystickRotate != null) panelJoystickRotate.SetActive(false);

		if (activePlayer != null)
		{
			Gamepad activePad = activePlayer.GetComponent<PlayerInput>().GetDevice<Gamepad>();
			if (activePad != null) activePad.SetMotorSpeeds(0f, 0f);
		}

		EventBus.OnMinigameEnded?.Invoke(activePlayer);
	}
}