using System;
using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
	[SerializeField]
	Collider thisCollider;

	[SerializeField]
	Collider[] colliderToIgnore;

	void Start()
	{
		foreach (Collider otherCollider in colliderToIgnore)
		{
			Physics.IgnoreCollision(thisCollider, otherCollider, true);
		}

	}

}
