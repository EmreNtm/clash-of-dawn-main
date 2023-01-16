using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class ShipAimMode : NetworkBehaviour
{

    [SerializeField] private LayerMask PlayerShip;
    [SerializeField]
    private Camera mainCamera;  //attached camera
    private Vector3 targetDirection;
    public float turnSpeed;
    public Transform pitchTransform;
    private Vector3 eulerAngles;
    public Vector2 angleDownUp;
    public Transform thirdPersonCam;

    // Start is called before the first frame update
    void Start()
    {
        eulerAngles = pitchTransform.localEulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
            return;

        if (thirdPersonCam.gameObject.activeSelf)
            return;

        var (success, position) = GetMousePosition();
        targetDirection = (position - transform.position).normalized;
        Debug.DrawRay(transform.position, targetDirection * 50f, Color.cyan, 0);
        
        Vector3 aimDirection;
        Vector3 steeringVector = targetDirection - pitchTransform.forward;
        float speed = steeringVector.magnitude;
        if (speed > 1) {
            speed = 1;
        }
        if (speed > 0.0005f) {
            speed *= 0.01f;
            steeringVector = steeringVector.normalized * speed;
            aimDirection = pitchTransform.forward + steeringVector;
        } else {
            aimDirection = targetDirection;
        }

        Debug.DrawRay(transform.position, aimDirection * 50f, Color.magenta, 0);

        Vector3 yawVector = Vector3.ProjectOnPlane(aimDirection, transform.up).normalized;
        Debug.DrawRay(transform.position, yawVector * 50f, Color.green, 0);
        Vector3 pitchVector = Vector3.ProjectOnPlane(aimDirection, transform.right).normalized;
        Debug.DrawRay(transform.position, pitchVector * 50f, Color.red, 0);


        //Debug.DrawRay(transform.position, newYawDirection * 50f, Color.magenta, 0);
        //Debug.DrawRay(pitchTransform.position, newPitchDirection * 50f, Color.yellow, 0);
        
        transform.rotation = Quaternion.LookRotation(yawVector, transform.up);
        eulerAngles.x += Vector3.SignedAngle(pitchTransform.forward, pitchVector, pitchTransform.right);
        eulerAngles.x = Mathf.Clamp(eulerAngles.x, -angleDownUp.y, angleDownUp.x);
        pitchTransform.localEulerAngles = eulerAngles;
        //pitchTransform.rotation = Quaternion.LookRotation(newPitchDirection, transform.up);

        //Aim(targetDirection);
    }

    private (bool success, Vector3 position) GetMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 500f, PlayerShip))
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
