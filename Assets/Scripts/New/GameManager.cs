using UnityEngine;

public class GameManager : MonoBehaviour
{
	[Header("Time Settings (Real World Minutes)")]
	[Tooltip("How many real-world minutes the Day phase lasts")]
	public float dayDurationMinutes = 6f;

	[Tooltip("How many real-world minutes the Night phase lasts")]
	public float nightDurationMinutes = 4f;

	[Header("Phase Settings (In-Game Hours)")]
	[Tooltip("The hour (0-23) when Day starts (Sunrise)")]
	public int dayStartHour = 8;

	[Tooltip("The hour (0-23) when Night starts (Sunset)")]
	public int nightStartHour = 20;

	[Header("Game Clock")]
	public int currentHour = 8;
	private float timeTimer = 0f;

	// The engine recalculates this automatically when phases change
	private float currentSecondsPerHour;

	// State Machine
	public IGamePhaseState currentPhase { get; private set; }
	public Phase_Day phaseDay;
	public Phase_Night phaseNight;

	void Awake()
	{
		phaseDay = new Phase_Day(this);
		phaseNight = new Phase_Night(this);
	}

	void Start()
	{
		// Set the starting hour to whatever the Day Start is, so it doesn't break if you change it
		currentHour = dayStartHour;
		ChangePhase(phaseDay);
	}

	void Update()
	{
		HandleClock();
		currentPhase?.UpdatePhase();
	}

	public void ChangePhase(IGamePhaseState newPhase)
	{
		currentPhase?.ExitPhase();
		currentPhase = newPhase;

		// Dynamically calculate how fast the clock ticks based on custom start times
		if (currentPhase == phaseDay)
		{
			// Calculate how many in-game hours the Day phase lasts
			int dayHours = (nightStartHour - dayStartHour + 24) % 24;
			// Prevent divide by zero if you accidentally set both to the same hour
			currentSecondsPerHour = (dayDurationMinutes * 60f) / Mathf.Max(1, dayHours);
		}
		else if (currentPhase == phaseNight)
		{
			// Calculate how many in-game hours the Night phase lasts
			int nightHours = (dayStartHour - nightStartHour + 24) % 24;
			currentSecondsPerHour = (nightDurationMinutes * 60f) / Mathf.Max(1, nightHours);
		}

		currentPhase.EnterPhase();
	}

	private void HandleClock()
	{
		timeTimer += Time.deltaTime;

		// --- NEW: Calculate the exact decimal time and broadcast it every frame ---
		float exactHour = currentHour + (timeTimer / currentSecondsPerHour);
		EventBus.OnTimeProgress?.Invoke(exactHour);
		// ------------------------------------------------------------------------

		if (timeTimer >= currentSecondsPerHour)
		{
			timeTimer = 0f;
			currentHour++;

			if (currentHour >= 24) currentHour = 0;

			EventBus.OnTimeTick?.Invoke(currentHour);

			if (currentHour == nightStartHour && currentPhase != phaseNight)
			{
				ChangePhase(phaseNight);
			}
			else if (currentHour == dayStartHour && currentPhase != phaseDay)
			{
				ChangePhase(phaseDay);
			}
		}
	}
}