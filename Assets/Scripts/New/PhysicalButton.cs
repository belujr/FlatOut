using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PhysicalButton : MonoBehaviour
{
	[Header("Interaction Settings")]
	public UnityEvent onButtonPressed;
	public float cooldown = 1.0f;

	[Header("Visual Feedback Settings")]
	public Transform buttonMesh;

	[Tooltip("If TRUE, the button stays pushed in until punched again. If FALSE, it immediately pops back out.")]
	public bool isToggleButton = true;

	public float pressDepth = 0.05f;
	public float animationSpeed = 15f;

	private float lastPressTime = 0f;
	private Vector3 originalLocalPos;
	private Vector3 pressedLocalPos;

	// Tracks the physical state of the toggle
	private bool isCurrentlyPressedIn = false;

	void Start()
	{
		if (buttonMesh != null)
		{
			originalLocalPos = buttonMesh.localPosition;
			// Pre-calculate the "bottomed out" position (Assumes local Z axis)
			pressedLocalPos = originalLocalPos - new Vector3(0, 0, pressDepth);
		}
	}

	void OnTriggerEnter(Collider other)
	{
		CheckPress(other);
	}

	void OnCollisionEnter(Collision collision)
	{
		CheckPress(collision.collider);
	}

	private void CheckPress(Collider other)
	{
		if (Time.time < lastPressTime + cooldown) return;

		PlayerController player = other.GetComponentInParent<PlayerController>();
		bool isThrownItem = other.CompareTag("ThrownWeapon") || other.transform.root.CompareTag("ThrownWeapon");

		if (player != null || isThrownItem)
		{
			lastPressTime = Time.time;

			if (player != null) Debug.Log($"Button smashed by {player.gameObject.name}");
			else Debug.Log("Button smashed by a thrown object!");

			// Handle the visual animation
			if (buttonMesh != null)
			{
				StopAllCoroutines();

				if (isToggleButton)
				{
					// Flip the state and animate to the new position
					isCurrentlyPressedIn = !isCurrentlyPressedIn;
					Vector3 targetPos = isCurrentlyPressedIn ? pressedLocalPos : originalLocalPos;
					StartCoroutine(AnimateToPosition(targetPos));
				}
				else
				{
					// Act like an arcade button (push in and immediately pop out)
					StartCoroutine(AnimateMomentaryPress());
				}
			}

			onButtonPressed?.Invoke();
		}
	}

	// --- NEW: Smoothly slides to a specific position and stays there ---
	private IEnumerator AnimateToPosition(Vector3 targetPos)
	{
		float t = 0;
		Vector3 startPos = buttonMesh.localPosition;

		while (t < 1f)
		{
			t += Time.deltaTime * animationSpeed;
			buttonMesh.localPosition = Vector3.Lerp(startPos, targetPos, t);
			yield return null;
		}

		buttonMesh.localPosition = targetPos; // Guarantee perfect alignment
	}

	// --- PRESERVED: The old arcade button logic just in case! ---
	private IEnumerator AnimateMomentaryPress()
	{
		float t = 0;
		while (t < 1f)
		{
			t += Time.deltaTime * animationSpeed;
			buttonMesh.localPosition = Vector3.Lerp(originalLocalPos, pressedLocalPos, t);
			yield return null;
		}

		t = 0;
		while (t < 1f)
		{
			t += Time.deltaTime * animationSpeed;
			buttonMesh.localPosition = Vector3.Lerp(pressedLocalPos, originalLocalPos, t);
			yield return null;
		}

		buttonMesh.localPosition = originalLocalPos;
	}
}