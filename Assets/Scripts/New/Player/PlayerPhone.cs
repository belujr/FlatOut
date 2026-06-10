using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class PlayerPhone : MonoBehaviour
{
	[Header("Core References")]
	public PlayerController playerController;
	public Canvas phoneCanvas;
	public GraphicRaycaster phoneRaycaster;
	public App_Shopping appShopping;
	public App_Gigs appGigs;
	public App_Casino appCasino; // ---> NEW: Casino Reference

	[Header("3D Model References")]
	public GameObject physicalPhoneModel;

	[Header("Profile UI Elements")]
	public TextMeshProUGUI playerNameText;
	public Image avatarImage;
	public Image hungerBarFill;

	[Header("UI Navigation")]
	public GameObject topNotificationButton;
	public GameObject firstAppButton;

	[Header("Phone State")]
	public bool isPhoneOpen = false;

	[Header("Survival Stats")]
	public float maxHunger = 100f;
	public float currentHunger = 100f;

	private PlayerInput playerInput;

	[Header("Survival Settings")]
	public float hungerDrainPerSecond = 0.5f;
	public bool hasPassedOutFromHunger = false;

	void Awake()
	{
		PlayerController me = GetComponent<PlayerController>();

		if (phoneCanvas != null)
		{
			App_Shopping shop = phoneCanvas.GetComponentInChildren<App_Shopping>(true);
			App_Gigs gigs = phoneCanvas.GetComponentInChildren<App_Gigs>(true);
			App_Casino casino = phoneCanvas.GetComponentInChildren<App_Casino>(true); // Find Casino
			PhoneNotificationCenter notif = phoneCanvas.GetComponentInChildren<PhoneNotificationCenter>(true);

			if (shop != null) shop.myPlayer = me;
			if (gigs != null) gigs.myPlayer = me;
			if (casino != null) casino.localPlayer = me;
			if (notif != null) notif.myPlayer = me;

			if (casino != null) appCasino = casino; // Assign local reference

			phoneCanvas.transform.SetParent(null);
		}
	}

	void Start()
	{
		if (playerController != null) playerInput = playerController.GetComponent<PlayerInput>();

		SetPhoneVisibility(false);

		if (playerController != null)
		{
			playerNameText.text = $"Player {playerController.playerID}";
			if (playerController.playerAvatar != null) avatarImage.sprite = playerController.playerAvatar;
		}
		UpdateHungerUI();
	}

	void OnEnable() { EventBus.OnPlayerRestoreHunger += HandleHungerRestored; }
	void OnDisable() { EventBus.OnPlayerRestoreHunger -= HandleHungerRestored; }

	void Update()
	{
		if (currentHunger > 0)
		{
			SetHunger(currentHunger - (hungerDrainPerSecond * Time.deltaTime));
		}
		else if (!hasPassedOutFromHunger)
		{
			hasPassedOutFromHunger = true;
			if (isPhoneOpen) ChangePhoneState(false);
			if (playerController != null) playerController.ForceRagdoll();
		}
	}

	private void HandleHungerRestored(int id, float amount)
	{
		if (playerController != null && playerController.playerID == id)
		{
			SetHunger(currentHunger + amount);
			if (hasPassedOutFromHunger && currentHunger > 0)
			{
				hasPassedOutFromHunger = false;
				playerController.ReviveFromRagdoll();
			}
		}
	}

	void OnDestroy()
	{
		if (phoneCanvas != null) Destroy(phoneCanvas.gameObject);
	}

	public void OnTogglePhone(InputValue value)
	{
		if (value.isPressed && !isPhoneOpen) ChangePhoneState(true);
	}

	public void OnClosePhone(InputValue value)
	{
		if (value.isPressed && isPhoneOpen)
		{
			// ---> THE FIX: Block 'B' from closing the phone if the QTE is active!
			if (appCasino != null && appCasino.IsGamblingActive()) return;

			if (appShopping != null && appShopping.panelShopItems.activeSelf) appShopping.CloseCategory();
			else if (appShopping != null && appShopping.panelShopCategories.activeSelf) appShopping.CloseShoppingApp();
			else if (appGigs != null && appGigs.panelGigDetails.activeSelf) appGigs.GoBack();
			else if (appGigs != null && appGigs.panelGigsList.activeSelf) appGigs.CloseGigsApp();
			// ---> THE FIX: Route the 'B' button to cleanly go back through Casino menus!
			else if (appCasino != null && appCasino.panelCasino.activeSelf) appCasino.GoBack();
			else ChangePhoneState(false);
		}
	}

	private void ChangePhoneState(bool open)
	{
		if (playerController.isIncapacitated) return;

		isPhoneOpen = open;
		SetPhoneVisibility(isPhoneOpen);

		MultiplayerEventSystem myEventSystem = GetComponent<MultiplayerEventSystem>();

		if (isPhoneOpen)
		{
			if (appShopping != null) appShopping.ForceCloseToHome();
			if (appGigs != null) appGigs.ForceCloseToHome();
			if (appCasino != null) appCasino.ForceCloseToHome(); // Resets Casino cleanly!

			if (myEventSystem != null) myEventSystem.SetSelectedGameObject(null);

			bool canSelectNotification = false;

			if (topNotificationButton != null && topNotificationButton.activeInHierarchy)
			{
				CanvasGroup cg = topNotificationButton.GetComponent<CanvasGroup>();
				if (cg != null && cg.interactable == true) canSelectNotification = true;
			}

			if (myEventSystem != null)
			{
				if (canSelectNotification) myEventSystem.SetSelectedGameObject(topNotificationButton);
				else myEventSystem.SetSelectedGameObject(firstAppButton);
			}

			if (playerInput != null)
			{
				playerInput.actions.FindAction("Jump")?.Disable();
				playerInput.actions.FindAction("UI/Submit")?.Enable();
			}
		}
		else
		{
			if (myEventSystem != null) myEventSystem.SetSelectedGameObject(null);

			if (playerInput != null)
			{
				playerInput.actions.FindAction("Jump")?.Enable();
				playerInput.actions.FindAction("UI/Submit")?.Disable();
			}
		}
	}

	private void SetPhoneVisibility(bool isVisible)
	{
		if (phoneCanvas != null) phoneCanvas.enabled = isVisible;
		if (phoneRaycaster != null) phoneRaycaster.enabled = isVisible;
		if (physicalPhoneModel != null) physicalPhoneModel.SetActive(isVisible);
	}

	public void SetHunger(float newHungerLevel)
	{
		currentHunger = Mathf.Clamp(newHungerLevel, 0, maxHunger);
		UpdateHungerUI();
	}

	private void UpdateHungerUI()
	{
		if (hungerBarFill != null)
		{
			hungerBarFill.fillAmount = currentHunger / maxHunger;
			if (currentHunger < 20f) hungerBarFill.color = Color.red;
			else hungerBarFill.color = Color.white;
		}
	}
}