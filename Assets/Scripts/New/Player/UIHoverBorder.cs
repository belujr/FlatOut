using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Outline))]
public class UIHoverBorder : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
	[Header("Border Settings")]
	[Tooltip("Change this color in the Inspector!")]
	public Color hoverColor = new Color(0f, 1f, 0.2f);

	[Tooltip("How thick the border is")]
	public Vector2 borderThickness = new Vector2(3f, -3f);

	private Outline borderOutline;

	void Awake()
	{
		SetupOutline();
	}

	private void SetupOutline()
	{
		if (borderOutline == null)
		{
			borderOutline = GetComponent<Outline>();

			// THE FIX: Use the Inspector variables instead of hardcoded numbers!
			borderOutline.effectColor = hoverColor;
			borderOutline.effectDistance = borderThickness;

			borderOutline.enabled = false;
		}
	}

	public void OnSelect(BaseEventData eventData) { SetupOutline(); borderOutline.enabled = true; }
	public void OnDeselect(BaseEventData eventData) { SetupOutline(); borderOutline.enabled = false; }
	public void OnPointerEnter(PointerEventData eventData) { SetupOutline(); borderOutline.enabled = true; }
	public void OnPointerExit(PointerEventData eventData) { SetupOutline(); borderOutline.enabled = false; }

	void OnDisable()
	{
		// THE FIX: If the UI panel turns off, kill the outline immediately
		// so it isn't stuck on when we come back!
		if (borderOutline != null)
		{
			borderOutline.enabled = false;
		}
	}
}