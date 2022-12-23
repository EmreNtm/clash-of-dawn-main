using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public sealed class PawnCameraLook : NetworkBehaviour
{
    
    private PawnInput pawnInput;

    [SerializeField]
    private Transform myCamera;

    [SerializeField]
    private float xMin;

    [SerializeField]
    private float xMax;

    private Vector3 eulerAngles;

    public override void OnStartNetwork() {
        base.OnStartNetwork();

        pawnInput = GetComponent<PawnInput>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        myCamera.GetComponent<Camera>().enabled = IsOwner;
        myCamera.GetComponent<AudioListener>().enabled = IsOwner;
    }

    private void Update() {
        if (!IsOwner)
            return;

        eulerAngles.x -= pawnInput.mouseY;
        eulerAngles.x = Mathf.Clamp(eulerAngles.x, xMin, xMax);
        myCamera.localEulerAngles = eulerAngles;
        transform.Rotate(0f, pawnInput.mouseX, 0f, Space.World);
    }

}
