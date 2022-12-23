using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public sealed class PawnInput : NetworkBehaviour
{
    
    private Pawn pawn;

    public float horizontal;
    public float vertical;

    public float mouseX;
    public float mouseY;

    public float sensitivity;

    public bool jump;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        pawn = GetComponent<Pawn>();
    }

    private void Update() {
        if (!IsOwner)
            return;

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        mouseX = Input.GetAxis("Mouse X") * sensitivity;
        mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        jump = Input.GetButton("Jump");
    }

}
