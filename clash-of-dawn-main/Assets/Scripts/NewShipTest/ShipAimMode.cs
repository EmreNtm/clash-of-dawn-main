using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipAimMode : MonoBehaviour
{

    [SerializeField] private LayerMask PlayerShip;
    private Camera mainCamera;  //attached camera
    private Vector3 targetDirection;
    public float turnSpeed;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        var (success, position) = GetMousePosition();
        targetDirection = (position - transform.position).normalized;

        Aim(targetDirection);
    }

    private (bool success, Vector3 position) GetMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 1000000f, PlayerShip))
        {

            return (success: true, position: hitInfo.point);
        }
        else
        {
            Vector3 vec = ray.direction * 500f;
            return (success: false, position: mainCamera.transform.position + vec);
        }
    }

    private void Aim(Vector3 direction)
    {
        Vector3 steeringVector = (direction - transform.forward);

        Vector3 targetDirection;
        turnSpeed = steeringVector.magnitude;
        if (turnSpeed > 1)
        {
            turnSpeed = 1;

        }
        //turnSpeed *= Time.fixedDeltaTime;
        if (turnSpeed > 0.05f)
        {
            turnSpeed *= 0.01f;
            steeringVector = steeringVector.normalized * turnSpeed;
            
            targetDirection = transform.forward + steeringVector;
        }
        else
        {
            targetDirection = transform.forward;
        }



        transform.rotation = Quaternion.LookRotation(targetDirection, transform.up);
        


    }


}
