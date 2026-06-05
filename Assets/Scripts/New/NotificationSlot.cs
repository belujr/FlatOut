using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Button))]
public class NotificationSlot : MonoBehaviour
{
	public TextMeshProUGUI notificationText;
	private GigData myGigData;

	private CanvasGroup canvasGroup;
	private Button myButton;

	// --- THE FIX: A private link to this specific phone's Gigs App ---
	private App_Gigs myLocalGigsApp;

	void Awake()
	{
		canvasGroup = GetComponent<CanvasGroup>();
		myButton = GetComponent<Button>();

		// Automatically find the Gigs app that shares this exact phone canvas!
		Canvas myPhoneCanvas = GetComponentInParent<Canvas>();
		if (myPhoneCanvas != null)
		{
			myLocalGigsApp = myPhoneCanvas.GetComponentInChildren<App_Gigs>(true);
		}
	}

	public void PushGigNotification(GigData newGig)
	{
		myGigData = newGig;
		notificationText.text = $"{newGig.gigTitle} - ${newGig.startingPayoutAmount}\n<size=80%>{newGig.shortDescription}</size>";

		canvasGroup.alpha = 1f;
		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;
		if (myButton != null) myButton.interactable = true;
	}

	public void PushTextNotification(string title, string description)
	{
		myGigData = null;
		notificationText.text = $"{title}\n<size=80%>{description}</size>";

		canvasGroup.alpha = 1f;
		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;
		if (myButton != null) myButton.interactable = true;
	}

	public void ClearNotification()
	{
		myGigData = null;
		notificationText.text = "";

		canvasGroup.alpha = 0f;
		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;
		if (myButton != null) myButton.interactable = false;
	}

	public bool IsEmpty()
	{
		return string.IsNullOrEmpty(notificationText.text);
	}

	public void CopyFrom(NotificationSlot otherSlot)
	{
		myGigData = otherSlot.myGigData;
		notificationText.text = otherSlot.notificationText.text;

		canvasGroup.alpha = otherSlot.canvasGroup.alpha;
		canvasGroup.interactable = otherSlot.canvasGroup.interactable;
		canvasGroup.blocksRaycasts = otherSlot.canvasGroup.blocksRaycasts;

		if (myButton != null && otherSlot.myButton != null)
		{
			myButton.interactable = otherSlot.myButton.interactable;
		}
	}

	public bool HasGig() { return myGigData != null; }
	public GigData GetCurrentGig() { return myGigData; }

	public void OnNotificationClicked()
	{
		// --- THE FIX: Use the local app instead of searching the whole game! ---
		if (myGigData != null && myLocalGigsApp != null)
		{
			myLocalGigsApp.OpenGigDetails(myGigData);
		}
	}
}