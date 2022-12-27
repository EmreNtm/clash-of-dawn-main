using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunRotationHandler : MonoBehaviour
{

    [SerializeField] private LayerMask playerShipMask;
    public Transform gunDeckTf;
    public Transform gunRotaterTf;
    public Vector3 targetDirection;
    private Camera mainCamera;
    public float degree;
    public float turnSpeed;

    public Vector3 gunDeckStartingForward;
    
    void Awake() {
        gunDeckStartingForward = gunDeckTf.forward;
    }

    void Start()
    {
        mainCamera = Camera.main;
        turnSpeed = 0.02f;
    }


    void Update()
    {
        var (success, position) = GetMousePosition();
        targetDirection = (position - transform.forward).normalized;
    }

    private void FixedUpdate()
    {
        
        Debug.DrawRay(transform.position, targetDirection * 50f, Color.cyan, 0);
        degree = Vector3.Angle(transform.forward, gunDeckTf.forward);
        //if (degree < 61)
        {
            AimGunDeck(targetDirection);
        }
        AimGunRotater(targetDirection);
    }


    private void AimGunDeck(Vector3 direction)
    {
        Vector3 forwardVec = new Vector3(direction.x, 0, direction.z);

        Vector3 steeringVector = (forwardVec - gunDeckTf.forward);
        if (steeringVector.magnitude > turnSpeed) {
            steeringVector = steeringVector.normalized * turnSpeed;
            Vector3 desiredDirection = gunDeckTf.forward + new Vector3(steeringVector.x, 0, steeringVector.z);
            degree = Vector3.Angle(transform.forward, desiredDirection);
            if (degree < 30) {
                gunDeckTf.rotation = Quaternion.LookRotation(desiredDirection, gunDeckTf.up);
            }
        } else {
            degree = Vector3.Angle(transform.forward, forwardVec);
            if (degree < 30) {
                gunDeckTf.rotation = Quaternion.LookRotation(forwardVec, gunDeckTf.up);
            }
        }

        Debug.DrawRay(gunDeckTf.position, gunDeckTf.forward * 10f, Color.blue, 0);
        Debug.DrawRay(gunDeckTf.position, forwardVec * 20f, Color.red, 0);
    }

    private void AimGunRotater(Vector3 direction) {
        Vector3 steeringVector = new Vector3(0, direction.y - gunRotaterTf.forward.y, 0);
        if (steeringVector.magnitude > turnSpeed) {
            steeringVector = steeringVector.normalized * turnSpeed;
        }
        Vector3 desiredDirection = gunRotaterTf.forward + steeringVector;
        if (Vector3.Angle(gunDeckTf.forward, desiredDirection) < 30) {
            gunRotaterTf.rotation = Quaternion.LookRotation(desiredDirection, gunRotaterTf.up);
        }

        Debug.DrawRay(gunRotaterTf.position, gunRotaterTf.forward * 10f, Color.blue, 0);
    }

    private (bool success, Vector3 position) GetMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 1000000f, playerShipMask))
        {
            
            return (success: true, position: hitInfo.point);
        }
        else
        {
            Vector3 vec = ray.direction * 1000000f;
            return (success: false, position: mainCamera.transform.position + vec);
        }
    }
}
