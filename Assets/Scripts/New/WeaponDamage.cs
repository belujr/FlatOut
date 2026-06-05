using UnityEngine;
using System.Collections;

public class WeaponDamage : MonoBehaviour
{
	[Header("Damage Settings")]
	public float damageMultiplier = 2f;
	public float minimumHitVelocity = 4f;
	public int ownerID = 0;

	[Header("Knockback Settings")]
	[SerializeField] float knockbackStrength = 10f;
	[SerializeField] float maxKnockbackVelocity = 15f;
	[SerializeField] float verticalLift = 0.4f;

	[Header("Stun Settings")]
	[SerializeField] float stunThreshold = 0.6f;

	[Header("Sharp Weapon Settings")]
	public bool isSharp = false;
	public float stickVelocity = 8f;
	public float embedDepth = 0.15f;

	[Header("Breakable Settings")]
	public bool isBreakable = false;
	public float breakVelocity = 10f;
	public GameObject brokenPrefab;
	public float breakDamageMultiplier = 1.5f;

	[Header("VFX Settings")]
	public GameObject bloodPrefab;
	public GameObject glassBloodPrefab;
	[SerializeField] float weaponLifetime = 5f;
	[SerializeField] float bloodLifetime = 3f;

	[HideInInspector] public PlayerGrab currentHolder;
	private bool isStuck = false;
	private Rigidbody myRigidbody;

	void Awake()
	{
		myRigidbody = GetComponent<Rigidbody>();
		if (myRigidbody != null) myRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
	}

	void OnCollisionEnter(Collision collision)
	{
		if (isStuck) return;

		PlayerController victimControl = collision.transform.root.GetComponent<PlayerController>();

		if (victimControl != null && victimControl.playerID == ownerID) return;

		bool isHeld = currentHolder != null;
		bool isThrown = gameObject.CompareTag("ThrownWeapon");
		bool canDealDamage = isHeld || isThrown;

		if (isThrown) gameObject.tag = "Weapon";

		float impactSpeed = collision.relativeVelocity.magnitude;

		if (impactSpeed >= minimumHitVelocity)
		{
			if (victimControl != null)
			{
				// Check the State Machine for parrying
				if (victimControl.currentState == victimControl.stateParry)
				{
					if (isBreakable && impactSpeed >= breakVelocity)
					{
						BreakObject(collision);
						return;
					}

					if (myRigidbody != null)
					{
						Vector3 bounceDir = (transform.position - victimControl.transform.position).normalized;
						bounceDir += Vector3.up * 0.5f;

						if (currentHolder != null) currentHolder.ForceReleaseWeapon();

						myRigidbody.linearVelocity = Vector3.zero;
						myRigidbody.AddForce(bounceDir.normalized * 5f, ForceMode.VelocityChange);
					}
					return;
				}

				if (isBreakable && impactSpeed >= breakVelocity)
				{
					if (glassBloodPrefab != null)
					{
						ContactPoint contact = collision.contacts[0];
						GameObject blood = Instantiate(glassBloodPrefab, contact.point, Quaternion.LookRotation(contact.normal), collision.collider.transform);
						Destroy(blood, bloodLifetime);
					}

					if (canDealDamage)
					{
						
						ApplyStabilizedKnockback(collision, impactSpeed, victimControl);
					}

					BreakObject(collision);
					return;
				}

				if (!canDealDamage) return;

				ApplyStabilizedKnockback(collision, impactSpeed, victimControl);
				

				if (isSharp && impactSpeed >= stickVelocity) StickIntoTarget(collision);
			}
			else if (isBreakable && impactSpeed >= breakVelocity)
			{
				BreakObject(collision);
			}
		}
	}

	void BreakObject(Collision collision)
	{
		if (currentHolder != null) currentHolder.ForceReleaseWeapon();

		if (brokenPrefab != null)
		{
			GameObject shards = Instantiate(brokenPrefab, transform.position, transform.rotation);
			Rigidbody[] shardRbs = shards.GetComponentsInChildren<Rigidbody>();
			Vector3 impactVelocity = myRigidbody.linearVelocity;

			foreach (Rigidbody rb in shardRbs)
			{
				rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
				rb.linearVelocity = impactVelocity * 0.7f;
				rb.AddExplosionForce(300f, collision.contacts[0].point, 1.5f);
			}
		}

		transform.position = new Vector3(0, -500, 0);
		Destroy(gameObject, 0.02f);
	}

	void ApplyStabilizedKnockback(Collision collision, float speed, PlayerController victimControl)
	{
		Rigidbody mainRb = victimControl.GetComponent<Rigidbody>();
		if (mainRb == null) mainRb = collision.rigidbody;
		if (mainRb == null) return;

		float speedRange = maxKnockbackVelocity - minimumHitVelocity;
		float speedFactor = Mathf.Clamp01((speed - minimumHitVelocity) / speedRange);
		if (speedFactor >= stunThreshold)
		{
			// Start the timer on the victim so it doesn't cancel if the weapon shatters!
			victimControl.StartCoroutine(WeaponStunRoutine(victimControl, 1.5f));
		}

		Vector3 moveDirection = myRigidbody.linearVelocity.normalized;
		if (moveDirection.magnitude < 0.1f) moveDirection = -collision.relativeVelocity.normalized;

		Vector3 forceDirection = (moveDirection + Vector3.up * verticalLift).normalized;

		mainRb.AddForce(forceDirection * (speed * knockbackStrength * speedFactor), ForceMode.Impulse);

		if (mainRb.linearVelocity.magnitude > maxKnockbackVelocity)
		{
			mainRb.linearVelocity = mainRb.linearVelocity.normalized * maxKnockbackVelocity;
		}
	}

	void StickIntoTarget(Collision collision)
	{
		if (currentHolder != null)
		{
			currentHolder.ForceReleaseWeapon();
			currentHolder = null;
		}

		isStuck = true;
		if (myRigidbody != null) Destroy(myRigidbody);

		// --- THE FIX: Find ALL colliders in the child objects and turn them off ---
		Collider[] myColliders = GetComponentsInChildren<Collider>();
		foreach (Collider col in myColliders)
		{
			col.enabled = false;
		}
		// --------------------------------------------------------------------------

		ContactPoint contact = collision.contacts[0];
		if (bloodPrefab != null)
		{
			GameObject blood = Instantiate(bloodPrefab, contact.point, Quaternion.LookRotation(contact.normal), collision.collider.transform);
			Destroy(blood, bloodLifetime);
		}

		transform.position -= contact.normal * embedDepth;
		transform.SetParent(collision.collider.transform, true);

		gameObject.tag = "Untagged";
		Destroy(gameObject, weaponLifetime);
	}
	private IEnumerator WeaponStunRoutine(PlayerController victim, float duration)
	{
		victim.ChangeState(victim.stateRagdoll);

		yield return new WaitForSeconds(duration);

		// Only stand them back up if they didn't just pass out from hunger or heatstroke
		if (!victim.isIncapacitated)
		{
			victim.ChangeState(victim.stateLocomotion);
		}
	}
}