using UnityEngine;

public class Phase_Day : IGamePhaseState
{
	private GameManager ctx;
	public Phase_Day(GameManager context) { ctx = context; }

	public void EnterPhase()
	{
		Debug.Log("🌞 Day has started. Dust accumulates, broker might visit.");
		EventBus.OnDayStarted?.Invoke();
	}

	public void UpdatePhase() { /* Day specific continuous logic if any */ }
	public void ExitPhase() { }
}