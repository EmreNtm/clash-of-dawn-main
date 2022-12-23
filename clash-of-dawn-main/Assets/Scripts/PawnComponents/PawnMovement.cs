using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public sealed class PawnMovement : NetworkBehaviour
{

    private PawnInput pawnInput;

    [SerializeField]
    private float speed;

    [SerializeField]
    private float jumpSpeed;

    [SerializeField]
    private float gravityScale;

    private CharacterController characterController;
    private Vector3 velocity;

    public override void OnStartNetwork() {
        base.OnStartNetwork();

        pawnInput = GetComponent<PawnInput>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update() {
        if (!IsOwner)
            return;

        Vector3 desiredVelocity = Vector3.ClampMagnitude(((transform.forward * pawnInput.vertical) + (transform.right * pawnInput.horizontal)) * speed , speed);
        velocity.x = desiredVelocity.x;
        velocity.z = desiredVelocity.z;

        if (characterController.isGrounded) {
            velocity.y = 0;

            if (pawnInput.jump) {
                velocity.y = jumpSpeed;
            }
        } else {
            velocity.y += Physics.gravity.y * gravityScale * Time.deltaTime;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

}
