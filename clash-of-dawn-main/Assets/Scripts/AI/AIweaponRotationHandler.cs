using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIweaponRotationHandler : MonoBehaviour
{
    [SerializeField] private LayerMask PlayerShip;
    public Camera mainCamera;

    public Transform staticGunTransform;

    public Vector3 turretMainTargetDirection;
    public Vector3 gunTargetDirection;
    public Vector3 turretProjectedDir;
    public Vector3 gunProjectedDir;

    public Transform turretMain;
    public Transform gunTransform;
    public float turnSpeed;
    public float turretAngle;
    public float currentTurretAngle;
    public float gunAngle;
    public float currentGunAngle;
    public float maxTurretAngle = 90f;
    public float maxGunAngle = 45f;

    [HideInInspector]
    public Vector3 AILastMousePos = Vector3.zero;

    [HideInInspector]
    public Vector2 AIangleInput = Vector2.zero;

    private void Update() {
        // The step size is equal to speed times frame time.
        float singleStep = turnSpeed * Time.deltaTime;

        var (success, position) = GetAIMousePosition();
        turretMainTargetDirection = (position - turretMain.position).normalized;
        gunTargetDirection = (position - gunTransform.position).normalized;

        // turretMainTargetDirection = Quaternion.AngleAxis(AIangleInput.x, turretMain.transform.up) * turretMain.transform.forward;
        // gunTargetDirection = Quaternion.AngleAxis(AIangleInput.y, gunTransform.transform.right) * gunTransform.transform.forward;

        turretProjectedDir = Vector3.ProjectOnPlane(turretMainTargetDirection, transform.up);
        gunProjectedDir = Vector3.ProjectOnPlane(gunTargetDirection, turretMain.right);
        // Debug.DrawRay(turretMain.position, turretMainTargetDirection * 50f, Color.cyan, 0);
        Debug.DrawRay(gunTransform.position, gunTargetDirection * 25f, Color.cyan, 0);
        Debug.DrawRay(gunTransform.position, gunTransform.forward * 25, Color.green, 0);
        // Debug.DrawRay(turretMain.position, turretProjectedDir * 50f, Color.green, 0);
        // Debug.DrawRay(gunTransform.position, gunProjectedDir * 50f, Color.red, 0);

        currentTurretAngle = Vector3.Angle(transform.forward, turretProjectedDir);
        currentGunAngle = Vector3.SignedAngle(staticGunTransform.forward, gunProjectedDir, staticGunTransform.right);
        if (currentTurretAngle > maxTurretAngle) {
            turretProjectedDir = transform.forward;
            maxGunAngle = 0f;
        } else
        {
            maxGunAngle = 45f;
        }

        if (currentGunAngle >= maxGunAngle  && currentGunAngle < -maxGunAngle)
        {
            gunProjectedDir = staticGunTransform.forward;
        }

        // Rotate the forward vector towards the target direction by one step
        Vector3 turretNewDirection = Vector3.RotateTowards(turretMain.forward, turretProjectedDir, singleStep, 0.0f);
        Vector3 gunNewDirection = Vector3.RotateTowards(gunTransform.forward, gunProjectedDir, singleStep, 0.0f);

        turretAngle = Vector3.Angle(transform.forward, turretNewDirection);
        gunAngle = Vector3.SignedAngle(staticGunTransform.forward, gunNewDirection , staticGunTransform.right);

        //if (turretAngle <= maxTurretAngle)
        turretMain.rotation = Quaternion.LookRotation(turretNewDirection, transform.up);

        if (gunAngle <= maxGunAngle  && gunAngle >= -maxGunAngle)
            gunTransform.rotation = Quaternion.LookRotation(gunNewDirection, turretMain.up);

        if (gunAngle < 20f && gunAngle >= -maxGunAngle)
        {
            maxTurretAngle = 180f;
        }else
        {
            maxTurretAngle = 90f;
        }        
    }

    public void ResetGunTransform() {
        turretMain.rotation = Quaternion.identity;
        gunTransform.rotation = Quaternion.identity;
    }

    private (bool success, Vector3 position) GetMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 1500f , PlayerShip))
        {
            
            return (success: true, position: hitInfo.point);
        }
        else
        {
            Vector3 vec = ray.direction * 1500f;
            return (success: false, position: mainCamera.transform.position + vec);
        }
    }

    public (bool success, Vector3 position) GetAIMousePosition() {
        var ray = mainCamera.ScreenPointToRay(AILastMousePos);

        if (Physics.Raycast(ray, out var hitInfo, 1500f , PlayerShip))
        {
            
            return (success: true, position: hitInfo.point);
        }
        else
        {
            Vector3 vec = ray.direction * 1500f;
            return (success: false, position: mainCamera.transform.position + vec);
        }
    }

}
