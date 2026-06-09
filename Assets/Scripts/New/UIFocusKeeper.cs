using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class UIFocusKeeper : MonoBehaviour
{
	private MultiplayerEventSystem localEventSystem;
	private GameObject lastKnownSelected;

	[Tooltip("Drag your fallback App Button here (e.g., App_Shopping)")]
	public GameObject fallbackButton;

	void Awake()
	{
		localEventSystem = GetComponent<MultiplayerEventSystem>();
	}

	void Update()
	{
		if (localEventSystem == null) return;

		// If something is currently selected, memorize it!
		if (localEventSystem.currentSelectedGameObject != null)
		{
			lastKnownSelected = localEventSystem.currentSelectedGameObject;
		}
		// If the selection dropped to NULL, force it back instantly!
		else
		{
			if (lastKnownSelected != null && lastKnownSelected.activeInHierarchy)
			{
				localEventSystem.SetSelectedGameObject(lastKnownSelected);
			}
			else if (fallbackButton != null && fallbackButton.activeInHierarchy)
			{
				localEventSystem.SetSelectedGameObject(fallbackButton);
			}
		}
	}
}