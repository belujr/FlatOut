using UnityEngine;

public class State_Ragdoll : IPlayerState
{
	private PlayerController ctx;
	public State_Ragdoll(PlayerController context) { ctx = context; }

	public void EnterState()
	{
		if (ctx.sphereCollider != null) ctx.sphereCollider.enabled = false;
		SetJointLimps(20f);
	}

	public void FixedUpdateState() { }

	public void ExitState()
	{
		if (ctx.sphereCollider != null) ctx.sphereCollider.enabled = true;
		RestoreJoints();
	}

	private void SetJointLimps(float springStiffness)
	{
		for (int i = 0; i < ctx.allBodyJoints.Length; i++)
		{
			JointDrive limpDrive = new JointDrive
			{
				positionSpring = springStiffness,
				positionDamper = ctx.originalJointDrives[i].positionDamper,
				maximumForce = ctx.originalJointDrives[i].maximumForce
			};
			ctx.allBodyJoints[i].slerpDrive = limpDrive;
		}
	}

	private void RestoreJoints()
	{
		for (int i = 0; i < ctx.allBodyJoints.Length; i++)
		{
			ctx.allBodyJoints[i].slerpDrive = ctx.originalJointDrives[i];
		}
	}
}