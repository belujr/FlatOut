using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PhoneNotificationCenter : MonoBehaviour
{
	[Header("Player Tracking")]
	public PlayerController myPlayer; // <-- NEW: Public so we can drag it in!

	[Tooltip("Drag Notification_1, Notification_2, and Notification_3 here in order!")]
	public NotificationSlot[] slots;

	[Header("Navigation Linking")]
	public Button[] appButtons;

	[Header("Empty State UI")]
	public GameObject noNotificationsText;

	// Removed Awake() completely so it doesn't crash from unparenting!
	
	void Start()
	{
		if (slots == null) return;
		foreach (NotificationSlot slot in slots) { if (slot != null) slot.ClearNotification(); }
		UpdateEmptyStateText();
		RewireNavigation();
	}

	void OnEnable()
	{
		EventBus.OnNewGigAvailable += HandleNewGig;
		EventBus.OnGigCompleted += RemoveGigNotification;

		// THE FIX: Instantly delete the notification when ANYONE accepts the gig!
		EventBus.OnGigAccepted += RemoveGigNotification;

		EventBus.OnGenericTextNotification += HandleGenericNotification;
		EventBus.OnPersonalTextNotification += HandlePersonalNotification;
	}

	void OnDisable()
	{
		EventBus.OnNewGigAvailable -= HandleNewGig;
		EventBus.OnGigCompleted -= RemoveGigNotification;
		EventBus.OnGigAccepted -= RemoveGigNotification;
		EventBus.OnGenericTextNotification -= HandleGenericNotification;
		EventBus.OnPersonalTextNotification -= HandlePersonalNotification;
	}

	private void HandlePersonalNotification(int targetPlayerID, string title, string desc)
	{
		if (myPlayer == null || myPlayer.playerID != targetPlayerID) return;
		PushMessageToUI(title, desc);
	}

	private void HandleGenericNotification(string title, string desc)
	{
		PushMessageToUI(title, desc);
	}

	private void PushMessageToUI(string title, string desc)
	{
		if (slots.Length == 0) return;
		for (int i = slots.Length - 1; i > 0; i--) { if (!slots[i - 1].IsEmpty()) slots[i].CopyFrom(slots[i - 1]); }
		slots[0].PushTextNotification(title, desc);
		UpdateEmptyStateText();
		RewireNavigation();
	}

	private void HandleNewGig(GigData newGig)
	{
		if (slots.Length == 0) return;
		for (int i = slots.Length - 1; i > 0; i--) { if (!slots[i - 1].IsEmpty()) slots[i].CopyFrom(slots[i - 1]); }
		slots[0].PushGigNotification(newGig);
		UpdateEmptyStateText();
		RewireNavigation();
	}

	private void RemoveGigNotification(GigData completedGig)
	{
		if (slots == null || slots.Length == 0) return;
		foreach (NotificationSlot slot in slots)
		{
			if (slot.HasGig() && slot.GetCurrentGig() == completedGig) slot.ClearNotification();
		}
		UpdateEmptyStateText();
		RewireNavigation();
	}

	private void UpdateEmptyStateText()
	{
		if (noNotificationsText == null) return;
		bool hasAnyNotifications = false;
		foreach (NotificationSlot slot in slots) { if (!slot.IsEmpty()) hasAnyNotifications = true; }
		noNotificationsText.SetActive(!hasAnyNotifications);
	}

	private void RewireNavigation()
	{
		foreach (NotificationSlot slot in slots)
		{
			Button btn = slot.GetComponent<Button>();
			if (btn != null)
			{
				Navigation clearNav = btn.navigation;
				clearNav.mode = Navigation.Mode.None;
				btn.navigation = clearNav;
			}
		}

		List<Button> visibleButtons = new List<Button>();
		foreach (NotificationSlot slot in slots) { if (!slot.IsEmpty()) visibleButtons.Add(slot.GetComponent<Button>()); }

		for (int i = 0; i < visibleButtons.Count; i++)
		{
			Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
			if (i > 0) nav.selectOnUp = visibleButtons[i - 1];

			if (i < visibleButtons.Count - 1) nav.selectOnDown = visibleButtons[i + 1];
			else if (appButtons != null && appButtons.Length > 0) nav.selectOnDown = appButtons[0];

			visibleButtons[i].navigation = nav;
		}

		if (appButtons != null)
		{
			for (int i = 0; i < appButtons.Length; i++)
			{
				if (appButtons[i] == null) continue;
				Navigation appNav = appButtons[i].navigation;
				appNav.mode = Navigation.Mode.Explicit;

				if (visibleButtons.Count > 0) appNav.selectOnUp = visibleButtons[visibleButtons.Count - 1];
				else appNav.selectOnUp = null;

				if (i > 0) appNav.selectOnLeft = appButtons[i - 1];
				if (i < appButtons.Length - 1) appNav.selectOnRight = appButtons[i + 1];

				appNav.selectOnDown = null;
				appButtons[i].navigation = appNav;
			}
		}
	}
}