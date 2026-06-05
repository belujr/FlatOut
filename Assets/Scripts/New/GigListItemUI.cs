using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GigListItemUI : MonoBehaviour
{
	[Header("UI Hookups")]
	public TextMeshProUGUI titleText;
	public TextMeshProUGUI payoutText;

	private GigData myGig;
	private App_Gigs myParentApp; // <-- THE PRIVATE LINK

	// THE FIX: This method now accepts BOTH the GigData and the App_Gigs!
	public void SetupDisplay(GigData data, App_Gigs parentApp)
	{
		myGig = data;
		myParentApp = parentApp;

		if (titleText != null) titleText.text = data.gigTitle;
		if (payoutText != null) payoutText.text = "$" + data.startingPayoutAmount.ToString("F2");
	}

	public void OnGigClicked()
	{
		// Uses the private link instead of searching the whole game!
		if (myParentApp != null) myParentApp.OpenGigDetails(myGig);
	}
}