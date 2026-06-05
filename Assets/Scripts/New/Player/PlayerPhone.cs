using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI; // <--- ADD THIS LINE!
public class PlayerPhone : MonoBehaviour
{
	[Header("Core References")]
	public PlayerController playerController;
	public Canvas phoneCanvas;
	public GraphicRaycaster phoneRaycaster;
	public App_Shopping appShopping;
	public App_Gigs appGigs;

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
	private bool hasPassedOutFromHunger = false;

	void Awake()
	{
		// 1. Grab our true identity
		PlayerController me = GetComponent<PlayerController>();

		if (phoneCanvas != null)
		{
			// 2. Find the apps hidden inside the phone
			App_Shopping shop = phoneCanvas.GetComponentInChildren<App_Shopping>(true);
			App_Gigs gigs = phoneCanvas.GetComponentInChildren<App_Gigs>(true);
			PhoneNotificationCenter notif = phoneCanvas.GetComponentInChildren<PhoneNotificationCenter>(true);

			// 3. Aggressively inject the identity into them!
			if (shop != null) shop.myPlayer = me;
			if (gigs != null) gigs.myPlayer = me;
			if (notif != null) notif.myPlayer = me;

			// 4. Now it is completely safe to detach the canvas!
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
			if (appShopping != null && appShopping.panelShopItems.activeSelf) appShopping.CloseCategory();
			else if (appShopping != null && appShopping.panelShopCategories.activeSelf) appShopping.CloseShoppingApp();
			else if (appGigs != null && appGigs.panelGigDetails.activeSelf) appGigs.GoBack();
			else if (appGigs != null && appGigs.panelGigsList.activeSelf) appGigs.CloseGigsApp();
			else ChangePhoneState(false);
		}
	}

	private void ChangePhoneState(bool open)
	{
		if (playerController.isIncapacitated) return;

		isPhoneOpen = open;
		SetPhoneVisibility(isPhoneOpen);

		// Grab THIS specific player's UI brain, not the global one!
		MultiplayerEventSystem myEventSystem = GetComponent<MultiplayerEventSystem>();

		if (isPhoneOpen)
		{
			if (appShopping != null) appShopping.ForceCloseToHome();
			if (appGigs != null) appGigs.ForceCloseToHome();

			// 1. Wipe memory using the Multiplayer Event System
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

			// 2. Turn OFF jumping, Turn ON UI clicking
			if (playerInput != null)
			{
				playerInput.actions.FindAction("Jump")?.Disable();
				playerInput.actions.FindAction("UI/Submit")?.Enable();
			}
		}
		else
		{
			// 1. Wipe memory for THIS specific player when the phone closes!
			if (myEventSystem != null) myEventSystem.SetSelectedGameObject(null);

			// 2. Turn ON jumping, Turn OFF UI clicking!
			if (playerInput != null)
			{
				playerInput.actions.FindAction("Jump")?.Enable();
				playerInput.actions.FindAction("UI/Submit")?.Disable(); // THE GHOST KILLER
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