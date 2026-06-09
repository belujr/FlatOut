using UnityEngine;
using System.Collections.Generic;

public enum GigRiskLevel
{
	Safe,       // Least broker anger
	Noisy,      // Minor broker anger
	Risky,      // Moderate broker anger
	Illegal     // High broker anger
}

public enum MinigameType
{
	HoldAndRotateJoystick,
	QTE_TimingBar
}

[System.Serializable]
public class MechanicConfig
{
	public MinigameType mechanicType;

	[Range(1, 5)]
	[Tooltip("QTE: How many consecutive hits. Rotate: How many total spins.")]
	public int difficultyLevel = 1;

	[Header("QTE Starting Difficulty")]
	public float arrowSpeed = 250f;
	[Range(0.05f, 1f)] public float safeZoneFill = 0.2f;

	[Header("Rotate Starting Difficulty")]
	public float drainRate = 180f;
}

[System.Serializable]
public class GigTaskInfo
{
	public string taskDescription;
	public string requiredItemName;

	[Tooltip("Add mechanics here. Each one has its own unique difficulty, speed, and progression!")]
	public List<MechanicConfig> mechanicsSequence;

	[Header("Consequences")]
	public float taskFailurePenalty = 5f;
}

[CreateAssetMenu(fileName = "New Gig", menuName = "Gigs/Gig Data")]
public class GigData : ScriptableObject
{
	[Header("Gig Info")]
	public string gigTitle;
	public string shortDescription;
	[TextArea(3, 5)] public string fullDescription;
	public float startingPayoutAmount;

	[Header("Physical World")]
	public GameObject brokenPropPrefab;

	[Header("Repair Steps")]
	public List<GigTaskInfo> requiredTasks;

	[Header("World Impact")]
	public GigRiskLevel gigRiskLevel = GigRiskLevel.Safe;
}