using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections.Generic;

public class App_Shopping : MonoBehaviour
{
	public PlayerController myPlayer;

	[Header("UI Panels")]
	public GameObject panelHome;
	public GameObject panelShopCategories;
	public GameObject panelShopItems;

	[Header("Shop Database & Spawning")]
	public GameObject itemUIPrefab;
	public Transform shopItemsGrid;
	public ShopItemData[] availableFurnitureItems;
	public ShopItemData[] availableFoodItems;

	[Header("Navigation Memory")]
	public GameObject firstCategoryButton;
	private GameObject buttonThatOpenedApp;

	[Header("Economy UI")]
	public TextMeshProUGUI accountBalanceText;
	private float currentKnownBalance = 0f;

	private MultiplayerEventSystem localEventSystem;

	void Awake()
	{
		localEventSystem = GetComponentInParent<MultiplayerEventSystem>();
	}

	void Start()
	{
		SharedBankAccount bank = FindFirstObjectByType<SharedBankAccount>();
		if (bank != null) UpdateBankUI(bank.GetCurrentBalance());
	}

	void OnEnable() { EventBus.OnBalanceChanged += UpdateBankUI; }
	void OnDisable() { EventBus.OnBalanceChanged -= UpdateBankUI; }

	private void UpdateBankUI(float newBalance)
	{
		currentKnownBalance = newBalance;
		if (accountBalanceText != null) accountBalanceText.text = "$" + currentKnownBalance.ToString("F2");
	}

	public void OpenShoppingApp()
	{
		if (localEventSystem != null) buttonThatOpenedApp = localEventSystem.currentSelectedGameObject;

		panelHome.SetActive(false);
		panelShopCategories.SetActive(true);
		panelShopItems.SetActive(false);

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			localEventSystem.SetSelectedGameObject(firstCategoryButton);
		}
	}

	public void CloseShoppingApp()
	{
		panelShopCategories.SetActive(false);
		panelShopItems.SetActive(false);
		panelHome.SetActive(true);

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			localEventSystem.SetSelectedGameObject(buttonThatOpenedApp);
		}
	}

	public void ForceCloseToHome()
	{
		panelShopCategories.SetActive(false);
		panelShopItems.SetActive(false);
		panelHome.SetActive(true);
	}

	private void PopulateShopGrid(ShopItemData[] categoryItems)
	{
		panelShopCategories.SetActive(false);
		panelShopItems.SetActive(true);

		foreach (Transform child in shopItemsGrid) Destroy(child.gameObject);

		List<Button> spawnedButtons = new List<Button>();

		foreach (ShopItemData item in categoryItems)
		{
			GameObject newItemBtn = Instantiate(itemUIPrefab, shopItemsGrid);
			newItemBtn.GetComponent<ShopItemUI>().SetupDisplay(item, this);
			spawnedButtons.Add(newItemBtn.GetComponent<Button>());
		}

		for (int i = 0; i < spawnedButtons.Count; i++)
		{
			Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
			if (i - 2 >= 0) nav.selectOnUp = spawnedButtons[i - 2];
			if (i + 2 < spawnedButtons.Count) nav.selectOnDown = spawnedButtons[i + 2];
			if (i % 2 != 0) nav.selectOnLeft = spawnedButtons[i - 1];
			if (i % 2 == 0 && i + 1 < spawnedButtons.Count) nav.selectOnRight = spawnedButtons[i + 1];
			spawnedButtons[i].navigation = nav;
		}

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			if (spawnedButtons.Count > 0) localEventSystem.SetSelectedGameObject(spawnedButtons[0].gameObject);
		}
	}

	public void OpenFurnitureCategory() { PopulateShopGrid(availableFurnitureItems); }
	public void OpenFoodCategory() { PopulateShopGrid(availableFoodItems); }

	public void CloseCategory()
	{
		panelShopItems.SetActive(false);
		panelShopCategories.SetActive(true);

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			localEventSystem.SetSelectedGameObject(firstCategoryButton);
		}
	}

	public void PurchaseItem(ShopItemData itemToBuy)
	{
		// --- THE SUPER SHIELD ---
		// 1. Check if the actual Canvas screen is turned off
		Canvas myCanvas = GetComponentInParent<Canvas>();
		if (myCanvas != null && !myCanvas.enabled)
		{
			return; // The phone is in your pocket. Block the ghost!
		}

		// 2. Check if the UI panel itself is turned off
		if (panelShopItems == null || !panelShopItems.activeInHierarchy)
		{
			return; // We aren't looking at the items grid. Block the ghost!
		}

		string buttonName = "Unknown";
		if (localEventSystem != null && localEventSystem.currentSelectedGameObject != null)
		{
			buttonName = localEventSystem.currentSelectedGameObject.name;
		}

		Debug.Log($"<color=green>PURCHASE VALIDATED! Ordered: {itemToBuy.name}.</color>", this.gameObject);

		if (currentKnownBalance >= itemToBuy.itemCost)
		{
			EventBus.OnMoneySpent?.Invoke(itemToBuy.itemCost);

			if (myPlayer != null)
			{
				EventBus.OnPersonalTextNotification?.Invoke(myPlayer.playerID, "ORDER PLACED", $"Arriving in {itemToBuy.minDeliverySeconds} seconds.");

				if (DeliveryManager.Instance != null)
				{
					DeliveryManager.Instance.PlaceOrder(itemToBuy.prefabToSpawn, itemToBuy.minDeliverySeconds, itemToBuy.maxDeliverySeconds, myPlayer.playerID);
				}
			}
			else
			{
				Debug.LogError("CRITICAL: myPlayer is still null! Check PlayerPhone.cs Awake()");
			}
		}
	}
}