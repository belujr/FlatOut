using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
	[Header("UI Hookups")]
	public Image iconImage;
	public TextMeshProUGUI nameText;
	public TextMeshProUGUI priceText;

	private ShopItemData myData;
	private App_Shopping myParentApp; // <-- THE PRIVATE LINK

	// THE FIX: This method now accepts BOTH the ShopItemData and the App_Shopping!
	public void SetupDisplay(ShopItemData data, App_Shopping parentApp)
	{
		myData = data;
		myParentApp = parentApp; // Save the link to the specific phone!

		iconImage.sprite = data.itemIcon;
		nameText.text = data.itemName;
		priceText.text = "$" + data.itemCost.ToString();
	}

	public void OnBuyClicked()
	{
		// THE FIX: Uses the private link instead of searching the whole game!
		if (myParentApp != null) myParentApp.PurchaseItem(myData);
	}
}