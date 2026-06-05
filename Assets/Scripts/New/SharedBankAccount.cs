using UnityEngine;

public class SharedBankAccount : MonoBehaviour
{
	[Header("Economy Settings")]
	public float startingBalance = 2000f;

	// Read-only in inspector for debugging, logic changes it
	[SerializeField] private float currentBalance;

	void Awake()
	{
		currentBalance = startingBalance;
	}

	void OnEnable()
	{
		EventBus.OnMoneySpent += DeductMoney;
		EventBus.OnMoneyEarned += AddMoney; // <-- Add this!
	}

	void OnDisable()
	{
		EventBus.OnMoneySpent -= DeductMoney;
		EventBus.OnMoneyEarned -= AddMoney; // <-- Add this!
	}

	// --- NEW METHOD ---
	private void AddMoney(float amount)
	{
		currentBalance += amount;
		EventBus.OnBalanceChanged?.Invoke(currentBalance);
		Debug.Log($"<color=green>BANK: Received ${amount}! New Balance: ${currentBalance}</color>");
	}

	void Start()
	{
		// Broadcast the initial starting money to the UI the moment the game starts
		EventBus.OnBalanceChanged?.Invoke(currentBalance);
	}

	private void DeductMoney(float amount)
	{
		currentBalance -= amount;

		// Broadcast the new total to any UI screens that care
		EventBus.OnBalanceChanged?.Invoke(currentBalance);

		// Check for catastrophic survival failure
		if (currentBalance <= 0)
		{
			Debug.LogWarning("🚨 The players are BROKE! Prepare for power shutoff!");
		}
	}

	// --- NEW: Allow late-spawning players to check the balance directly ---
	public float GetCurrentBalance()
	{
		return currentBalance;
	}
}