using UnityEngine;

public class State_Parry : IPlayerState
{
	private PlayerController ctx;
	private float parryDuration = 0.5f;
	private float stiffnessMultiplier = 10f;
	private float timeEntered;

	public State_Parry(PlayerController context) { ctx = context; }

	public void EnterState()
	{
		timeEntered = Time.time;
		ctx.animator.SetFloat("movementSpeed", 0f);

		for (int i = 0; i < ctx.allBodyJoints.Length; i++)
		{
			JointDrive stiffDrive = new JointDrive
			{
				positionSpring = ctx.originalJointDrives[i].positionSpring * stiffnessMultiplier,
				positionDamper = ctx.originalJointDrives[i].positionDamper,
				maximumForce = ctx.originalJointDrives[i].maximumForce
			};
			ctx.allBodyJoints[i].slerpDrive = stiffDrive;
		}
	}

	public void FixedUpdateState()
	{
		if (Time.time >= timeEntered + parryDuration)
		{
			ctx.ChangeState(ctx.stateLocomotion);
		}
	}

	public void ExitState()
	{
		for (int i = 0; i < ctx.allBodyJoints.Length; i++)
		{
			ctx.allBodyJoints[i].slerpDrive = ctx.originalJointDrives[i];
		}
	}
}