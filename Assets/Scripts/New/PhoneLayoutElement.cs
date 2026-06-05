using UnityEngine;
using System.Collections.Generic;

public class PhoneLayoutElement : MonoBehaviour
{
	// A shared list that remembers every phone currently open on the screen!
	private static List<PhoneLayoutElement> activePhones = new List<PhoneLayoutElement>();

	private RectTransform rectTransform;
	public float edgePadding = 25f; // How far from the edge of the screen it sits

	void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	void OnEnable()
	{
		if (!activePhones.Contains(this)) activePhones.Add(this);
		RecalculateAllPositions();
	}

	void OnDisable()
	{
		if (activePhones.Contains(this)) activePhones.Remove(this);
		RecalculateAllPositions();
	}

	public static void RecalculateAllPositions()
	{
		for (int i = 0; i < activePhones.Count; i++)
		{
			RectTransform rt = activePhones[i].rectTransform;
			float pad = activePhones[i].edgePadding;

			switch (i)
			{
				case 0: // 1st Phone: Top Left
					rt.anchorMin = new Vector2(0, 1);
					rt.anchorMax = new Vector2(0, 1);
					rt.pivot = new Vector2(0, 1);
					rt.anchoredPosition = new Vector2(pad, -pad);
					break;
				case 1: // 2nd Phone: Top Right
					rt.anchorMin = new Vector2(1, 1);
					rt.anchorMax = new Vector2(1, 1);
					rt.pivot = new Vector2(1, 1);
					rt.anchoredPosition = new Vector2(-pad, -pad);
					break;
				case 2: // 3rd Phone: Bottom Left
					rt.anchorMin = new Vector2(0, 0);
					rt.anchorMax = new Vector2(0, 0);
					rt.pivot = new Vector2(0, 0);
					rt.anchoredPosition = new Vector2(pad, pad);
					break;
				case 3: // 4th Phone: Bottom Right
					rt.anchorMin = new Vector2(1, 0);
					rt.anchorMax = new Vector2(1, 0);
					rt.pivot = new Vector2(1, 0);
					rt.anchoredPosition = new Vector2(-pad, pad);
					break;
			}
		}
	}
}