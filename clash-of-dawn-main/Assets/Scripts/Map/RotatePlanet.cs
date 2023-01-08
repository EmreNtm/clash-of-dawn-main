using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class RotatePlanet : NetworkBehaviour
{
    
    public Vector3 rotationSpeed;
    private Vector3 timedRotation;

    void FixedUpdate() {
        //if (!IsOwner)
        //    return;

        if (!IsServer)
            return;

        timedRotation = rotationSpeed * Time.deltaTime;
        transform.Rotate(timedRotation, Space.Self);
    }

}
