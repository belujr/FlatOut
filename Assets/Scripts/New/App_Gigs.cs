using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Collections.Generic;

public class App_Gigs : MonoBehaviour
{
	[Header("Core OS Panels")]
	public GameObject panelHome;

	[Header("Gigs App Panels")]
	public GameObject panelGigsList;
	public GameObject panelGigDetails;
	public GameObject panelOngoingGig;

	[Header("Gigs Database & Spawning")]
	public GameObject gigListItemPrefab;
	public Transform gigsListContainer;
	private List<GigData> receivedGigs = new List<GigData>();

	[Header("Gig Details Elements")]
	public TextMeshProUGUI detailsTitleText;
	public TextMeshProUGUI detailsDescriptionText;
	public TextMeshProUGUI detailsPayoutText;
	public GameObject acceptButton;

	[Header("Ongoing Gig Elements")]
	public TextMeshProUGUI ongoingTitleText;
	public TextMeshProUGUI ongoingChecklistText;
	public TextMeshProUGUI ongoingCurrentPayoutText;
	public Button finalizeGigButton;

	[Header("Empty State")]
	public GameObject noGigsText;

	[Header("Home Screen Hookup")]
	public GameObject gigsAppButton;

	private GigData currentlyViewedGig;
	private GameObject buttonThatOpenedDetails;
	private GigData activeGig;
	private float currentDynamicPayout = 0f;
	private int completedTasksCount = 0;

	public PlayerController myPlayer;
	private MultiplayerEventSystem localEventSystem;

	void Awake()
	{
		localEventSystem = GetComponentInParent<MultiplayerEventSystem>();
		myPlayer = GetComponentInParent<PlayerController>();
	}

	void OnEnable()
	{
		EventBus.OnNewGigAvailable += DownloadGigToPhone;
		EventBus.OnTaskCompleted += HandleTaskCompleted;
		EventBus.OnGigMistakeMade += DeductPayout;
		EventBus.OnGigAccepted += HandleGigAcceptedGlobally;
	}

	void OnDisable()
	{
		EventBus.OnNewGigAvailable -= DownloadGigToPhone;
		EventBus.OnTaskCompleted -= HandleTaskCompleted;
		EventBus.OnGigMistakeMade -= DeductPayout;
		EventBus.OnGigAccepted -= HandleGigAcceptedGlobally;
	}

	private void DownloadGigToPhone(GigData newGig)
	{
		if (!receivedGigs.Contains(newGig)) receivedGigs.Insert(0, newGig);
	}

	private void HandleGigAcceptedGlobally(GigData acceptedGig)
	{
		// If WE are the ones who accepted it, ignore this!
		if (activeGig == acceptedGig) return;

		// If someone ELSE took it, remove it from our inbox!
		if (receivedGigs.Contains(acceptedGig)) receivedGigs.Remove(acceptedGig);

		// Were we actively looking at the details screen for this stolen gig?
		if (panelGigDetails.activeSelf && currentlyViewedGig == acceptedGig)
		{
			GoBack(); // Kick us out!
		}
		else if (panelGigsList.activeSelf)
		{
			PopulateGigsList(); // Silently refresh the list
		}

		// THE FIX: Send a personal text to THIS player that they missed the job!
		if (myPlayer != null)
		{
			EventBus.OnPersonalTextNotification?.Invoke(myPlayer.playerID, "GIG TAKEN", $"{acceptedGig.gigTitle} was claimed by another player!");
		}
	}

	public void OpenGigsApp()
	{
		panelHome.SetActive(false);
		panelGigDetails.SetActive(false);

		if (activeGig != null) OpenOngoingGigPanel();
		else
		{
			panelOngoingGig.SetActive(false);
			panelGigsList.SetActive(true);
			PopulateGigsList();
		}
	}

	private void PopulateGigsList()
	{
		foreach (Transform child in gigsListContainer)
		{
			if (noGigsText != null && child.gameObject == noGigsText) continue;
			Destroy(child.gameObject);
		}

		if (noGigsText != null) noGigsText.SetActive(receivedGigs.Count == 0);

		List<Button> spawnedButtons = new List<Button>();

		foreach (GigData gig in receivedGigs)
		{
			GameObject newGigBtn = Instantiate(gigListItemPrefab, gigsListContainer);
			newGigBtn.GetComponent<GigListItemUI>().SetupDisplay(gig, this); // Keeps it local
			spawnedButtons.Add(newGigBtn.GetComponent<Button>());
		}

		for (int i = 0; i < spawnedButtons.Count; i++)
		{
			Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
			if (i > 0) nav.selectOnUp = spawnedButtons[i - 1];
			if (i < spawnedButtons.Count - 1) nav.selectOnDown = spawnedButtons[i + 1];
			spawnedButtons[i].navigation = nav;
		}

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			if (spawnedButtons.Count > 0) localEventSystem.SetSelectedGameObject(spawnedButtons[0].gameObject);
		}
	}

	public void OpenGigDetails(GigData gigData)
	{
		// THE FIX: Completely removed EventSystem.current!
		if (localEventSystem != null) buttonThatOpenedDetails = localEventSystem.currentSelectedGameObject;

		currentlyViewedGig = gigData;

		panelHome.SetActive(false);
		panelGigsList.SetActive(false);
		panelGigDetails.SetActive(true);

		detailsTitleText.text = gigData.gigTitle;
		detailsDescriptionText.text = gigData.fullDescription;
		detailsPayoutText.text = "Payout: $" + gigData.startingPayoutAmount.ToString("F2");

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			localEventSystem.SetSelectedGameObject(acceptButton);
		}
	}

	public void GoBack()
	{
		panelGigDetails.SetActive(false);

		if (buttonThatOpenedDetails != null && buttonThatOpenedDetails.transform.IsChildOf(panelHome.transform))
		{
			panelHome.SetActive(true);

			if (localEventSystem != null)
			{
				localEventSystem.SetSelectedGameObject(null);

				bool canSnap = false;
				if (buttonThatOpenedDetails.activeInHierarchy)
				{
					CanvasGroup cg = buttonThatOpenedDetails.GetComponent<CanvasGroup>();
					if (cg == null || cg.interactable == true) canSnap = true;
				}

				if (canSnap) localEventSystem.SetSelectedGameObject(buttonThatOpenedDetails);
				else if (gigsAppButton != null) localEventSystem.SetSelectedGameObject(gigsAppButton);
			}
		}
		else
		{
			panelGigsList.SetActive(true);
			PopulateGigsList();
		}
	}

	public void CloseGigsApp()
	{
		panelGigsList.SetActive(false);
		panelGigDetails.SetActive(false);
		panelOngoingGig.SetActive(false);
		panelHome.SetActive(true);

		if (localEventSystem != null)
		{
			localEventSystem.SetSelectedGameObject(null);
			if (gigsAppButton != null) localEventSystem.SetSelectedGameObject(gigsAppButton);
		}
	}

	public void ForceCloseToHome() { CloseGigsApp(); }

	public void AcceptCurrentGig()
	{
		if (activeGig != null) return;

		activeGig = currentlyViewedGig;
		currentDynamicPayout = activeGig.startingPayoutAmount;
		completedTasksCount = 0;

		if (receivedGigs.Contains(activeGig)) receivedGigs.Remove(activeGig);
		EventBus.OnGigAccepted?.Invoke(activeGig);
		OpenOngoingGigPanel();
	}

	private void OpenOngoingGigPanel()
	{
		panelGigDetails.SetActive(false);
		panelGigsList.SetActive(false);
		panelOngoingGig.SetActive(true);
		UpdateChecklistUI();
	}

	private void UpdateChecklistUI()
	{
		ongoingTitleText.text = activeGig.gigTitle;
		ongoingCurrentPayoutText.text = $"Current Payout: <color=green>${currentDynamicPayout:F2}</color>";

		string checklistString = "";
		for (int i = 0; i < activeGig.requiredTasks.Count; i++)
		{
			if (i < completedTasksCount) checklistString += $"<color=green>[DONE]</color> <s>{activeGig.requiredTasks[i].taskDescription}</s>\n";
			else checklistString += $"[ ] {activeGig.requiredTasks[i].taskDescription}\n";
		}
		ongoingChecklistText.text = checklistString;

		if (completedTasksCount >= activeGig.requiredTasks.Count)
		{
			finalizeGigButton.interactable = true;
			if (localEventSystem != null)
			{
				localEventSystem.SetSelectedGameObject(null);
				localEventSystem.SetSelectedGameObject(finalizeGigButton.gameObject);
			}
		}
		else finalizeGigButton.interactable = false;
	}

	private void HandleTaskCompleted(int taskIndex)
	{
		if (activeGig == null) return;
		completedTasksCount++;
		UpdateChecklistUI();
	}

	private void DeductPayout(float penaltyAmount)
	{
		if (activeGig == null) return;
		currentDynamicPayout -= penaltyAmount;
		if (currentDynamicPayout < 0) currentDynamicPayout = 0;
		UpdateChecklistUI();
	}

	public void FinalizeAndReturnGig()
	{
		EventBus.OnGigReadyForPickup?.Invoke(activeGig, currentDynamicPayout);
		activeGig = null;
		CloseGigsApp();
	}
}