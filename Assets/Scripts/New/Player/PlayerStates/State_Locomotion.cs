using UnityEngine;

public class State_Locomotion : IPlayerState
{
    private PlayerController ctx;
    private float currentYRotation = 0f;
    private RaycastHit[] raycastHits = new RaycastHit[10];

    public State_Locomotion(PlayerController context) { ctx = context; }

    public void EnterState() { }
    public void ExitState() { }

    public void FixedUpdateState()
    {
        bool isGrounded = CheckGrounded();

        if (!isGrounded) 
            ctx.rigidbody3D.AddForce(Vector3.down * ctx.heavyGravity, ForceMode.Acceleration);

        float inputMagnitude = ctx.moveInput.magnitude;
        Transform cam = Camera.main.transform;
        
        Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
        Vector3 camRight = cam.right; camRight.y = 0f; camRight.Normalize();
        
        Vector3 desiredMoveDirection = (camForward * ctx.moveInput.y + camRight * ctx.moveInput.x).normalized;

        if (inputMagnitude > 0.05f)
        {
            float targetAngle = Mathf.Atan2(-desiredMoveDirection.x, desiredMoveDirection.z) * Mathf.Rad2Deg;
            currentYRotation = Mathf.MoveTowardsAngle(currentYRotation, targetAngle, Time.fixedDeltaTime * ctx.rotationSpeed);
            ctx.mainJoint.targetRotation = Quaternion.Euler(0, currentYRotation, 0);

            Vector3 flatVelocity = new Vector3(ctx.rigidbody3D.linearVelocity.x, 0f, ctx.rigidbody3D.linearVelocity.z);
            Vector3 velocityInDesiredDir = desiredMoveDirection * Vector3.Dot(desiredMoveDirection, flatVelocity);
            Vector3 driftVelocity = flatVelocity - velocityInDesiredDir;

            ctx.rigidbody3D.AddForce(-driftVelocity * ctx.stoppingFriction, ForceMode.Acceleration);

            float speedInDesiredDirection = Vector3.Dot(desiredMoveDirection, ctx.rigidbody3D.linearVelocity);
            if (speedInDesiredDirection < ctx.maxSpeed)
            {
                ctx.rigidbody3D.AddForce(desiredMoveDirection * inputMagnitude * ctx.pushForce, ForceMode.Acceleration);
            }
        }
        else if (isGrounded)
        {
            Vector3 flatVelocity = new Vector3(ctx.rigidbody3D.linearVelocity.x, 0f, ctx.rigidbody3D.linearVelocity.z);
            ctx.rigidbody3D.AddForce(-flatVelocity * ctx.stoppingFriction, ForceMode.Acceleration);
        }

        HandleJumps(isGrounded);

        Vector3 flatVelForAnim = new Vector3(ctx.rigidbody3D.linearVelocity.x, 0f, ctx.rigidbody3D.linearVelocity.z);
        ctx.animator.SetFloat("movementSpeed", flatVelForAnim.magnitude * 0.4f);
    }

    private void HandleJumps(bool isGrounded)
    {
        if (isGrounded && ctx.jumpTriggered)
        {
            ctx.rigidbody3D.linearVelocity = new Vector3(ctx.rigidbody3D.linearVelocity.x, 0f, ctx.rigidbody3D.linearVelocity.z);
            ctx.rigidbody3D.AddForce(Vector3.up * ctx.jumpForce, ForceMode.Impulse);
        }
        ctx.jumpTriggered = false;

        if (ctx.wallJumpTriggered)
        {
            ctx.rigidbody3D.linearVelocity = new Vector3(ctx.rigidbody3D.linearVelocity.x, 0f, ctx.rigidbody3D.linearVelocity.z);
            ctx.rigidbody3D.AddForce(Vector3.up * ctx.wallJumpForce, ForceMode.Impulse);
            ctx.wallJumpTriggered = false;
        }
    }

    private bool CheckGrounded()
    {
        int numberOfHits = Physics.SphereCastNonAlloc(ctx.rigidbody3D.position, 0.1f, Vector3.down, raycastHits, 0.5f);
        for (int i = 0; i < numberOfHits; i++)
        {
            if (raycastHits[i].transform.root != ctx.transform) return true;
        }
        return false;
    }
}