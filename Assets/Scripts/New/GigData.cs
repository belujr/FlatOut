using UnityEngine;
using System.Collections.Generic;

public enum MinigameType
{
	HoldAndRotateJoystick,
	QTE_TimingBar
}

[System.Serializable]
public class GigTaskInfo
{
	public string taskDescription;
	public string requiredItemName;

	[Tooltip("Add multiple mechanics to make them play back-to-back!")]
	public List<MinigameType> mechanicsSequence; // <-- THE FIX: Now it's a List!

	[Range(1, 5)]
	public int difficultyLevel = 1;
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
}