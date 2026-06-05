using UnityEngine;

// This line lets us right-click in the Project folder to create new items!
[CreateAssetMenu(fileName = "New Shop Item", menuName = "Phone OS/Shop Item")]
public class ShopItemData : ScriptableObject
{
	public string itemName;
	public float itemCost;
	public Sprite itemIcon;

	[Header("Delivery System")]
	[Tooltip("The actual 3D model that will spawn in the apartment")]
	public GameObject prefabToSpawn;

	[Header("Delivery Logistics")]
	[Tooltip("Minimum real-world seconds this takes to arrive")]
	public float minDeliverySeconds = 15f;

	[Tooltip("Maximum real-world seconds this takes to arrive")]
	public float maxDeliverySeconds = 45f;
}