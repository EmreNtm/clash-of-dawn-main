using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class ShipWeaponRotation : NetworkBehaviour
{
    [SerializeField] private LayerMask PlayerShip;
    public Camera mainCamera;

    public Transform staticGunTransform;

    private Vector3 turretMainTargetDirection;
    private Vector3 gunTargetDirection;
    public Transform turretMain;
    public Transform gunTransform;
    public float turnSpeed;
    public float turretAngle;
    public float currentTurretAngle;
    public float gunAngle;
    public float currentGunAngle;
    public float maxTurretAngle = 90f;
    public float maxGunAngle = 45f;

    public bool isFireLocked = false;

    private void Update() {
        if (!IsOwner)
            return;

        // The step size is equal to speed times frame time.
        float singleStep = turnSpeed * Time.deltaTime;

        var (success, position) = GetMousePosition();
        turretMainTargetDirection = (position - turretMain.position).normalized;
        gunTargetDirection = (position - gunTransform.position).normalized;

        Vector3 turretProjectedDir = Vector3.ProjectOnPlane(turretMainTargetDirection, transform.up);
        Vector3 gunProjectedDir = Vector3.ProjectOnPlane(gunTargetDirection, turretMain.right);
        // Debug.DrawRay(turretMain.position, turretMainTargetDirection * 50f, Color.cyan, 0);
        // Debug.DrawRay(gunTransform.position, gunTargetDirection * 50f, Color.cyan, 0);
        // Debug.DrawRay(turretMain.position, turretProjectedDir * 50f, Color.green, 0);
        // Debug.DrawRay(gunTransform.position, gunProjectedDir * 50f, Color.red, 0);

        currentTurretAngle = Vector3.Angle(transform.forward, turretProjectedDir);
        currentGunAngle = Vector3.SignedAngle(staticGunTransform.forward, gunProjectedDir, staticGunTransform.right);
        if (currentTurretAngle > maxTurretAngle) {
            turretProjectedDir = transform.forward;
            maxGunAngle = 0f;
            isFireLocked = true;
        }else
        {
            maxGunAngle = 45f;
            isFireLocked = false;
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

}
