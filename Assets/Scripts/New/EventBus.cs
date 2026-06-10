using System;
using UnityEngine;

public static class EventBus
{
    // --- COMBAT & HEALTH EVENTS ---
    // Payload: (int targetPlayerID, float damageAmount)
    public static Action<int, float> OnPlayerTakeDamage;

    // Payload: (int deadPlayerID)
    public static Action<int> OnPlayerDied;

    // --- GAMEPLAY EVENTS (For later!) ---
    // --- APARTMENT EVENTS ---
    // Payload: (float currentTemperature)
    public static Action<float> OnTemperatureChanged;

    // Payload: (float moneyAmount)
    public static Action<float> OnMoneySpent;

    public static System.Action<float> OnMoneyEarned;

    // Payload: (float noiseLevel)
    public static Action<float> OnNoiseGenerated;

    // --- TIME EVENTS ---
    // Payload: (int currentHour)
    public static Action<int> OnTimeTick;

    public static Action OnDayStarted;
    public static Action OnNightStarted;

    // Payload: (float exactHour) - e.g., 19.5f means 7:30 PM
    public static Action<float> OnTimeProgress;

    // --- CALENDAR EVENTS ---
    // Payload: (int newDay, int currentMonth)
    public static Action<int, int> OnDayChanged;

    // Payload: (int newMonth)
    public static Action<int> OnMonthChanged;

    // Payload: (float newBalance)
    public static Action<float> OnBalanceChanged;

    public static System.Action<GigData> OnNewGigAvailable;

    public static System.Action<GigData> OnGigCompleted;

    public static System.Action<int, float> OnPlayerRestoreHunger;

    public static System.Action<string, string> OnGenericTextNotification;

	// --- CASINO & GAMBLING EVENTS ---

	// The Casino App calls this to start the game
	public static System.Action<float, PlayerController> OnStartGamblingMinigame;

	// Triggers when the player successfully cashes out
	public static System.Action<float> OnGamblingCashOut;

	// Triggers the brutal amputation if they fail
	public static System.Action<PlayerController> OnLimbSevered;

	// Add this with your other Actions in EventBus.cs
	public static Action<int, string, string> OnPersonalTextNotification;

    // Triggered when you hit "Accept"
    public static System.Action<GigData> OnGigAccepted;

    // Triggered when you successfully finish a minigame step
    public static System.Action<int> OnTaskCompleted;

    // Triggered when you mess up a minigame, deducting money!
    public static System.Action<float> OnGigMistakeMade;

    // Payload: (GigData completedGig, float finalPayoutAmount)
    public static System.Action<GigData, float> OnGigReadyForPickup;

    // THE FIX: Added PlayerController to the payload
    // Payload: (GigTaskInfo task, GameObject heldItem, GigPropRepair propScript, PlayerController triggeringPlayer)
    public static System.Action<object, GameObject, GigPropRepair, PlayerController> OnStartMinigame;

    // THE FIX: Added PlayerController so only the specific player is unlocked
    // Triggered when a minigame UI closes (win or lose)
    public static System.Action<PlayerController> OnMinigameEnded;

    // --- BROKER EVENTS ---
    // Payload: (int angerValue 0-100)
    // Fired by BrokerAngerSystem every time anger changes.
    // BrokerAngerUI listens to this to update the percentage display.
    public static Action<int> OnBrokerAngerChanged;
}