using UnityEngine;

public class Phase_Night : IGamePhaseState
{
	private GameManager ctx;
	public Phase_Night(GameManager context) { ctx = context; }

	public void EnterPhase()
	{
		Debug.Log("🌙 Night has started. Temperature rises, AC needed.");
		EventBus.OnNightStarted?.Invoke();
	}

	public void UpdatePhase() { /* Night specific continuous logic if any */ }
	public void ExitPhase() { }
}