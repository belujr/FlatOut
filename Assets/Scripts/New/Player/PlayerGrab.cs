using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGrab : MonoBehaviour
{
	[Header("Hand Settings")]
	public bool isLeftHand = false;

	[Header("Rig References")]
	public Rigidbody forearmRigidbody;
	public Collider[] playerColliders;
	public Transform weaponSocket;
	public PlayerController playerController;

	[Header("Gang Beasts Arm Control")]
	public ConfigurableJoint shoulderJoint;
	public float relaxedArmSpring = 5f;
	public float holdingItemSpring = 60f;
	public float armJoystickForce = 400f;

	[Header("Throw Feel Settings")]
	public float throwVelocityMultiplier = 0.8f;
	public float maxThrowVelocity = 12f;
	public float minThrowSpeedThreshold = 2.5f; // <-- NEW: Threshold for dropping vs throwing

	public GameObject heldItem;
	private FixedJoint currentJoint;

	private GameObject hoveredWeapon;
	private GameObject hoveredWall;
	private bool isGrabbingWall = false;

	private float grabCooldown = 0f;
	private bool isSqueezingTrigger = false;
	private bool hasAcknowledgedTrigger = false;
	private bool jumpTriggeredThisFrame = false;

	private GrabSocket currentOccupiedSocket;

	void OnTriggerEnter(Collider other)
	{
		if (other.attachedRigidbody != null)
		{
			if ((other.attachedRigidbody.CompareTag("Weapon") || other.attachedRigidbody.CompareTag("ThrownWeapon")) && heldItem == null)
			{
				hoveredWeapon = other.attachedRigidbody.gameObject;
			}
		}
		else if (other.CompareTag("Wall") && !isGrabbingWall)
		{
			hoveredWall = other.gameObject;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (hoveredWeapon != null && other.attachedRigidbody != null && other.attachedRigidbody.gameObject == hoveredWeapon)
		{
			hoveredWeapon = null;
		}
		else if (hoveredWall != null && other.gameObject == hoveredWall)
		{
			hoveredWall = null;
		}
	}

	public void OnGrabLeft(InputValue value) { if (isLeftHand) HandleTriggerInput(value.Get<float>() > 0.5f); }
	public void OnGrabRight(InputValue value) { if (!isLeftHand) HandleTriggerInput(value.Get<float>() > 0.5f); }

	private void HandleTriggerInput(bool isPressed)
	{
		// THE MINIGAME LOCK FIX: Stop hands from grabbing/dropping during QTEs
		if (playerController != null && playerController.isPlayingMinigame) return;

		if (!isPressed) hasAcknowledgedTrigger = false;
		isSqueezingTrigger = isPressed;
	}

	public void OnJump(InputValue value) { if (value.isPressed) jumpTriggeredThisFrame = true; }

	void Update()
	{
		if (grabCooldown > 0f) grabCooldown -= Time.deltaTime;

		if (isGrabbingWall && jumpTriggeredThisFrame)
		{
			ReleaseWall();
			grabCooldown = 0.4f;
			if (playerController != null) playerController.PerformWallJump();
			jumpTriggeredThisFrame = false;
			return;
		}

		jumpTriggeredThisFrame = false;

		if (isSqueezingTrigger)
		{
			if (heldItem == null && !isGrabbingWall && grabCooldown <= 0f && !hasAcknowledgedTrigger)
			{
				if (hoveredWeapon != null) { GrabItem(hoveredWeapon); hasAcknowledgedTrigger = true; }
				else if (hoveredWall != null) { GrabWall(hoveredWall); hasAcknowledgedTrigger = true; }
			}
		}
		else
		{
			if (heldItem != null) ThrowItem();
			else if (isGrabbingWall) ReleaseWall();
		}
	}

	void FixedUpdate()
	{
		if (shoulderJoint != null)
		{
			bool isActive = (heldItem != null || isSqueezingTrigger || isGrabbingWall);
			bool isMovingStick = (isActive && playerController != null && playerController.rightStickArmInput.sqrMagnitude > 0.05f);

			float targetSpring = relaxedArmSpring;
			if (isActive) targetSpring = isMovingStick ? (holdingItemSpring * 0.4f) : holdingItemSpring;

			SetJointSpring(shoulderJoint, targetSpring);

			if (isMovingStick)
			{
				Vector2 stick = playerController.rightStickArmInput;
				Vector3 verticalPush = playerController.transform.up * stick.y;
				Vector3 horizontalPush = playerController.transform.right * stick.x;

				float forwardAmount = Mathf.Max(0, stick.y) * 1.5f;
				Vector3 forwardPush = playerController.transform.forward * forwardAmount;

				Vector3 pushDirection = (verticalPush + horizontalPush + forwardPush).normalized * stick.magnitude;
				forearmRigidbody.AddForce(pushDirection * armJoystickForce, ForceMode.Force);
			}
		}
	}

	private void SetJointSpring(ConfigurableJoint joint, float springValue)
	{
		JointDrive xDrive = joint.angularXDrive; xDrive.positionSpring = springValue; joint.angularXDrive = xDrive;
		JointDrive yzDrive = joint.angularYZDrive; yzDrive.positionSpring = springValue; joint.angularYZDrive = yzDrive;
		JointDrive slerp = joint.slerpDrive; slerp.positionSpring = springValue; joint.slerpDrive = slerp;
	}

	void GrabItem(GameObject item)
	{
		heldItem = item;

		InteractableItem interactable = item.GetComponent<InteractableItem>();

		if (interactable != null && interactable.grabSockets.Length > 0)
		{
			Transform bestSocket = interactable.GetClosestSocket(weaponSocket.position);
			GrabSocket socketComponent = bestSocket.GetComponent<GrabSocket>();

			if (socketComponent != null)
			{
				if (socketComponent.isOccupied)
				{
					heldItem = null; // Abort the grab!
					return;
				}

				currentOccupiedSocket = socketComponent;
				currentOccupiedSocket.isOccupied = true; // Lock it down!
			}

			// --- THE TWO-HANDED SNAP FIX ---
			bool isAlreadyHeld = false;
			GrabSocket[] allSockets = item.transform.root.GetComponentsInChildren<GrabSocket>();
			foreach (GrabSocket s in allSockets)
			{
				if (s != socketComponent && s.isOccupied) isAlreadyHeld = true;
			}

			if (!isAlreadyHeld)
			{
				Quaternion rotationDifference = weaponSocket.rotation * Quaternion.Inverse(bestSocket.rotation);
				item.transform.rotation = rotationDifference * item.transform.rotation;

				Vector3 positionDifference = weaponSocket.position - bestSocket.position;
				item.transform.position += positionDifference;
			}
		}
		else
		{
			item.transform.position = weaponSocket.position;
			item.transform.rotation = weaponSocket.rotation;
		}

		Collider[] allItemColliders = item.transform.root.GetComponentsInChildren<Collider>();
		foreach (Collider itemCol in allItemColliders)
		{
			foreach (Collider playerCol in playerColliders) Physics.IgnoreCollision(playerCol, itemCol, true);
		}

		if (playerController != null)
		{
			WeaponDamage[] weapons = item.transform.root.GetComponentsInChildren<WeaponDamage>();
			foreach (WeaponDamage wd in weapons)
			{
				wd.ownerID = playerController.playerID;
				wd.currentHolder = this;
			}
		}

		currentJoint = item.AddComponent<FixedJoint>();
		currentJoint.connectedBody = forearmRigidbody;
		hoveredWeapon = null;
	}

	void ThrowItem()
	{
		if (heldItem == null) return;

		Rigidbody itemRb = heldItem.GetComponent<Rigidbody>();
		if (currentJoint != null) Destroy(currentJoint);

		if (currentOccupiedSocket != null)
		{
			currentOccupiedSocket.isOccupied = false;
			currentOccupiedSocket = null;
		}

		// --- THE DROP VS THROW FIX ---
		if (itemRb != null && forearmRigidbody != null)
		{
			Vector3 armVel = forearmRigidbody.linearVelocity;

			if (armVel.magnitude < minThrowSpeedThreshold)
			{
				itemRb.linearVelocity = armVel * 0.1f;
			}
			else
			{
				Vector3 rawThrowVelocity = armVel * throwVelocityMultiplier;

				if (rawThrowVelocity.magnitude > maxThrowVelocity)
				{
					rawThrowVelocity = rawThrowVelocity.normalized * maxThrowVelocity;
				}

				itemRb.linearVelocity = rawThrowVelocity;
			}
		}

		bool isStillHeldByOtherHand = false;
		GrabSocket[] allSockets = heldItem.transform.root.GetComponentsInChildren<GrabSocket>();
		foreach (GrabSocket s in allSockets)
		{
			if (s.isOccupied) isStillHeldByOtherHand = true;
		}

		if (!isStillHeldByOtherHand)
		{
			Collider[] allItemColliders = heldItem.transform.root.GetComponentsInChildren<Collider>();
			foreach (Collider itemCol in allItemColliders)
			{
				foreach (Collider playerCol in playerColliders) Physics.IgnoreCollision(playerCol, itemCol, false);
			}
		}

		WeaponDamage[] weapons = heldItem.transform.root.GetComponentsInChildren<WeaponDamage>();
		foreach (WeaponDamage wd in weapons) wd.currentHolder = null;

		heldItem.tag = "ThrownWeapon";
		heldItem = null;
		grabCooldown = 0.2f;
	}

	public void ForceReleaseWeapon()
	{
		if (heldItem == null) return;

		Rigidbody itemRb = heldItem.GetComponent<Rigidbody>();
		if (currentJoint != null) Destroy(currentJoint);

		// --- THE DROP CLAMP FIX ---
		if (itemRb != null)
		{
			itemRb.linearVelocity *= 0.5f;
		}

		if (currentOccupiedSocket != null)
		{
			currentOccupiedSocket.isOccupied = false;
			currentOccupiedSocket = null;
		}

		bool isStillHeldByOtherHand = false;
		GrabSocket[] allSockets = heldItem.transform.root.GetComponentsInChildren<GrabSocket>();
		foreach (GrabSocket s in allSockets)
		{
			if (s.isOccupied) isStillHeldByOtherHand = true;
		}

		if (!isStillHeldByOtherHand)
		{
			Collider[] allItemColliders = heldItem.transform.root.GetComponentsInChildren<Collider>();
			foreach (Collider itemCol in allItemColliders)
			{
				foreach (Collider playerCol in playerColliders) Physics.IgnoreCollision(playerCol, itemCol, false);
			}
		}

		heldItem.tag = "Weapon";
		heldItem = null;
		hasAcknowledgedTrigger = true;
	}

	void GrabWall(GameObject wall)
	{
		isGrabbingWall = true;
		currentJoint = forearmRigidbody.gameObject.AddComponent<FixedJoint>();
		Rigidbody wallRb = wall.GetComponent<Rigidbody>();
		if (wallRb != null) currentJoint.connectedBody = wallRb;
	}

	void ReleaseWall()
	{
		isGrabbingWall = false;
		if (currentJoint != null) Destroy(currentJoint);
	}

	public void DropEverything()
	{
		if (heldItem != null) ThrowItem();
		if (isGrabbingWall) ReleaseWall();
		this.enabled = false;
	}
}