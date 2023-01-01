using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrusterControl : MonoBehaviour
{
    public Rigidbody attachedShip;
    public Vector3 transferredVelocity;
    private ParticleSystem thruster;

    // Start is called before the first frame update
    void Start()
    {
        thruster = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        

        var emitParams = new ParticleSystem.EmitParams
        {
            velocity =  Vector3.ClampMagnitude(attachedShip.velocity , 120f)
        };
        thruster.Emit(emitParams, 1);

    }
}
