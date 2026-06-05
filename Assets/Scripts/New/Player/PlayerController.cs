using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	[Header("Core References")]
	public Rigidbody rigidbody3D;
	public ConfigurableJoint mainJoint;
	public Collider sphereCollider;
	public Animator animator;
	public int playerID = 1;
	public Sprite playerAvatar;

	[Header("Preserved Settings")]
	public float maxSpeed = 4f;
	public float pushForce = 60f;
	public float stoppingFriction = 30f;
	public float heavyGravity = 50f;
	public float jumpForce = 50f;
	public float wallJumpForce = 40f;
	public float rotationSpeed = 1200f;

	// Input Data
	[HideInInspector] public Vector2 moveInput;
	[HideInInspector] public Vector2 rightStickArmInput;
	[HideInInspector] public bool jumpTriggered;
	[HideInInspector] public bool wallJumpTriggered;
	[HideInInspector] public bool isPlayingMinigame = false;

	// State Machine
	public IPlayerState currentState { get; private set; }
	public State_Locomotion stateLocomotion;
	public State_Ragdoll stateRagdoll;
	public State_Parry stateParry;

	[HideInInspector] public ConfigurableJoint[] allBodyJoints;
	[HideInInspector] public JointDrive[] originalJointDrives;
	[HideInInspector] public SyncPhysicsObjects[] syncPhysicsObjects;
	[HideInInspector] public bool isIncapacitated = false; // Locks manual state changes



	void Awake()
	{
		rigidbody3D.maxAngularVelocity = 50f;
		allBodyJoints = GetComponentsInChildren<ConfigurableJoint>();
		originalJointDrives = new JointDrive[allBodyJoints.Length];
		syncPhysicsObjects = GetComponentsInChildren<SyncPhysicsObjects>();

		for (int i = 0; i < allBodyJoints.Length; i++)
		{
			originalJointDrives[i] = allBodyJoints[i].slerpDrive;
		}

		// Initialize States
		stateLocomotion = new State_Locomotion(this);
		stateRagdoll = new State_Ragdoll(this);
		stateParry = new State_Parry(this);
	}

	void Start()
	{
		PlayerInput inputComponent = GetComponent<PlayerInput>();
		if (inputComponent != null) playerID = inputComponent.playerIndex + 1;

		ChangeState(stateLocomotion);
	}

	void OnEnable()
	{
		EventBus.OnStartMinigame += LockControls;
		EventBus.OnMinigameEnded += UnlockControls;
	}

	void OnDisable()
	{
		EventBus.OnStartMinigame -= LockControls;
		EventBus.OnMinigameEnded -= UnlockControls;
	}

	// THE FIX: Check if I am the one triggering it
	private void LockControls(object task, GameObject item, GigPropRepair prop, PlayerController triggeringPlayer)
	{
		if (triggeringPlayer != this) return;

		isPlayingMinigame = true;
		moveInput = Vector2.zero;          // Stop walking
		rightStickArmInput = Vector2.zero; // Stop moving arms
		jumpTriggered = false;             // Cancel any jumps
	}

	// THE FIX: Check if I am the one triggering it
	private void UnlockControls(PlayerController triggeringPlayer)
	{
		if (triggeringPlayer != this) return;

		isPlayingMinigame = false;
	}

	public void ChangeState(IPlayerState newState)
	{
		currentState?.ExitState();
		currentState = newState;
		currentState.EnterState();
	}

	void FixedUpdate()
	{
		// 1. Run the physics and inputs for the current state
		currentState?.FixedUpdateState();

		// 2. Sync the limbs to the animator AFTER physics resolve
		for (int i = 0; i < syncPhysicsObjects.Length; i++)
		{
			syncPhysicsObjects[i].UpdateJointFromAnimation();
		}
	}

	// --- INPUT ROUTING ---
	public void OnMove(InputValue value) { if (!isPlayingMinigame) moveInput = value.Get<Vector2>(); }

	public void OnRightStick(InputValue value) { if (!isPlayingMinigame) rightStickArmInput = value.Get<Vector2>(); }

	public void OnJump(InputValue value)
	{
		if (!isPlayingMinigame && value.isPressed) jumpTriggered = true;
	}

	public void OnRagdoll(InputValue value)
	{
		// THE MINIGAME LOCK FIX: Ignore X button if playing a minigame!
		if (isPlayingMinigame) return;

		if (value.isPressed && !isIncapacitated)
		{
			ChangeState(currentState == stateRagdoll ? stateLocomotion : stateRagdoll);
		}
	}

	public void OnParry(InputValue value)
	{
		// THE MINIGAME LOCK FIX: Ignore Parry button if playing a minigame!
		if (isPlayingMinigame) return;

		if (value.isPressed && currentState != stateRagdoll && currentState != stateParry)
			ChangeState(stateParry);
	}

	public void PerformWallJump() { wallJumpTriggered = true; }

	// --- HELPER METHODS FOR UNITY EVENTS (AND DEATH) ---

	// Called by Temperature or Health systems
	public void ForceRagdoll()
	{
		isIncapacitated = true; // Lock the player's controls!
		if (currentState != stateRagdoll)
		{
			ChangeState(stateRagdoll);
			Debug.Log($"{gameObject.name} was forced into Ragdoll state and controls are locked!");
		}
	}

	// Called by your future Medkit system
	public void ReviveFromRagdoll()
	{
		isIncapacitated = false; // Unlock the controls
		if (currentState == stateRagdoll)
		{
			ChangeState(stateLocomotion);
			Debug.Log($"{gameObject.name} was revived and stood back up!");
		}
	}
}