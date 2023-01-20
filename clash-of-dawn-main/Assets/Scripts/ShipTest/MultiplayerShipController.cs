using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet;
using FishNet.Object.Prediction;
using FishNet.Connection;

public class MultiplayerShipController : NetworkBehaviour
{

    [SerializeField]
    private Transform myCamera;
    [SerializeField]
    private Transform thirdPersonCamera;
    [SerializeField]
    private Transform shipAimCam;
    [SerializeField] 
    private LayerMask playerShipMask;
    private Camera shipCamera;
    [SerializeField]
    private GameObject VoyageCam;


    public struct MoveData {
        public Vector3 targetDirection;

        public bool interStellarToggle;
        public bool interStellarEngineWorking;
        public bool lockingShipAim;
        public bool runThrottle;
        public bool runOffThrottle;
        public bool runReverseThrust;
        public bool runOffReverseThrust;
        public bool runIsRollingLeft;
        public bool runOffIsRollingLeft;
        public bool runIsRollingRight;
        public bool runOffIsRollingRight;

        public MoveData(Vector3 _targetDirection, bool _interStellarToggle, bool _interStellarEngineWorking, bool _lockingShipAim,
                bool _runThrottle, bool _runOffThrottle, bool _runReverseThrust, bool _runOffReverseThrust, bool _runIsRollingLeft,
                bool _runOffIsRollingLeft, bool _runIsRollingRight, bool _runOffIsRollingRight) {
            
            targetDirection = _targetDirection;
            interStellarToggle = _interStellarToggle;
            interStellarEngineWorking = _interStellarEngineWorking;
            lockingShipAim = _lockingShipAim;
            runThrottle = _runThrottle;
            runOffThrottle = _runOffThrottle;
            runReverseThrust = _runReverseThrust;
            runOffReverseThrust = _runOffReverseThrust;
            runIsRollingLeft = _runIsRollingLeft;
            runOffIsRollingLeft = _runOffIsRollingLeft;
            runIsRollingRight = _runIsRollingRight;
            runOffIsRollingRight = _runOffIsRollingRight;
        }
    }

    public struct ReconcileData {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;

        public float thrustPercentage;
        public float rollThrustPercentage;

        public ReconcileData(Vector3 _position, Quaternion _rotation, Vector3 _velocity, Vector3 _angularVelocity, float _thrustPercentage, float _rollThrustPercentage) {
            position = _position;
            rotation = _rotation;
            velocity = _velocity;
            angularVelocity = _angularVelocity;
            thrustPercentage = _thrustPercentage;
            rollThrustPercentage = _rollThrustPercentage;
        }
    }

    [SerializeField]
    private float enginePower = 1000f;
    [SerializeField]
    private float rollEnginePower = 2f;
    [SerializeField]
    private float interStellarEnginePower = 20000f;

    private Rigidbody shipRigidbody;
    public bool throttleing = false; //is ship throttling?
    public bool offThrotling = false; //is ship on auto brake
    public bool reverseThrust = false;     //is ship reverseThrusting?
    public bool offReverseThrust = false; //is ship offReverseThrust
    public bool enginePowerLockToggle = false; //engine power toggled
    public bool isRollingRight = false; //is player rolling right in meant time
    public bool isRollingLeft = false;  //is player rolling left in meant time
    public bool isOffRollingRight = false; //is player not rolling right in meant time
    public bool isOffRollingLeft = false;  //is player not rolling left in meant time
    public bool interStellarToggle = false; //is player toggle interStellarEngine
    public bool inActivateInterStellarMode = false; //is interStellar engine heating?
    public bool interStellarEngineWorking = false; //is inter stellar engine working right now
    public bool lockingShipAim = false;

    //Public fields of this ship so player and we can see it on inspector
    [Range(-0.4f, 1f)] public float thrustPercentage;
    [Range(-400f, 1000f)] public float activeEnginePower = 0f; 
    [Range(-1f, 1f)] public float rollThrustPercentage;
    [Range(-100f, 100f)] public float rollEngineActivePower = 0f;
    public float turnSpeed;

    private float nextThrottleTime;
    private float nextOffThrottleTime;
    private float nextReverseThrustTime;
    private float nextOffReverseThrustTime;
    private float nextIsRollingLeftTime;
    private float nextIsOffRollingLeftTime;
    private float nextIsRollingRightTime;
    private float nextIsOffRollingRightTime;
    private float nextInActivateInterStellarModeTime;

    private bool runThrottle = false;
    private bool runOffThrottle = false;
    private bool runReverseThrust = false;
    private bool runOffReverseThrust = false;
    private bool runIsRollingLeft = false;
    private bool runOffIsRollingLeft = false;
    private bool runIsRollingRight = false;
    private bool runOffIsRollingRight = false;

    public override void OnStartClient()
    {
        base.OnStartClient();

        thirdPersonCamera.gameObject.SetActive(IsOwner);
        shipAimCam.gameObject.SetActive(IsOwner);
        myCamera.gameObject.SetActive(IsOwner);
    }

    private void Awake() {
        shipCamera = myCamera.GetComponent<Camera>();

        shipRigidbody = GetComponent<Rigidbody>();
        InstanceFinder.TimeManager.OnTick += TimeManager_OnTick;
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerSetShipSpawnPosition() {
        float offset = MapManager.Instance.systemSettings.sunRadius * MapManager.Instance.systemSettings.scale;
        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSetShipSpawnPoint(pd.Owner,  offset + Random.Range(20000, 20800));
        }
    }

    [TargetRpc]
    public void TargetSetShipSpawnPoint(NetworkConnection conn, float random) {
        shipRigidbody.transform.position = new Vector3(0, random, 0);
        Debug.Log(random);
    }

    private void OnDestroy() {
        if (InstanceFinder.TimeManager != null) {
            InstanceFinder.TimeManager.OnTick -= TimeManager_OnTick;
            InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }

    private void Update() {
        if (!base.IsOwner)
            return;

        //player holds w for accelaration
        if (throttleing == true && Time.time > nextThrottleTime) {
            throttleing = false;
        }
        if (Input.GetKey(KeyCode.W) && !throttleing && !enginePowerLockToggle && !interStellarEngineWorking && Time.time > nextThrottleTime) {
            throttleing = true;
            runThrottle = true;
            nextThrottleTime = Time.time + 0.1f;
        } 

        //player does not hold w, if speed is greater than 0, we close the engine
        if (offThrotling == true && Time.time > nextOffThrottleTime) {
            offThrotling = false;
        }
        if (!Input.GetKey(KeyCode.W) && !offThrotling && ( thrustPercentage > 0 ) && !enginePowerLockToggle && !interStellarEngineWorking && Time.time > nextOffThrottleTime) {
            offThrotling = true;
            runOffThrottle = true;
            nextOffThrottleTime = Time.time + 0.1f;
        }

        //holding s will open reverse thrust
        if (reverseThrust == true && Time.time > nextReverseThrustTime) {
            reverseThrust = false;
        }
        if (Input.GetKey(KeyCode.S) && !reverseThrust && !enginePowerLockToggle && !interStellarEngineWorking && Time.time > nextReverseThrustTime) {
            reverseThrust = true;
            runReverseThrust = true;
            nextReverseThrustTime = Time.time + 0.1f;
        }

        if (offReverseThrust == true && Time.time > nextOffReverseThrustTime) {
            offReverseThrust = false;
        }
        if (!Input.GetKey(KeyCode.S) && !offReverseThrust && (thrustPercentage < 0) && !enginePowerLockToggle && !interStellarEngineWorking && Time.time > nextOffReverseThrustTime) {
            offReverseThrust = true;
            runOffReverseThrust = true;
            nextOffReverseThrustTime = Time.time + 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.T) && !interStellarEngineWorking) {
            enginePowerLockToggle = !enginePowerLockToggle;
        }

        if (isRollingLeft == true && Time.time > nextIsRollingLeftTime) {
            isRollingLeft = false;
        }
        if (Input.GetKey(KeyCode.A) && !isRollingLeft && Time.time > nextIsRollingLeftTime) {
            isRollingLeft = true;
            runIsRollingLeft = true;
            nextIsRollingLeftTime = Time.time + 0.1f;
        }

        if (isOffRollingLeft == true && Time.time > nextIsOffRollingLeftTime) {
            isOffRollingLeft = false;
        }
        if (!Input.GetKey(KeyCode.A) && !isOffRollingLeft && (rollThrustPercentage > 0) && Time.time > nextIsOffRollingLeftTime) {
            isOffRollingLeft = true;
            runOffIsRollingLeft = true;
            nextIsOffRollingLeftTime = Time.time + 0.1f;
        }

        if (isRollingRight == true && Time.time > nextIsRollingRightTime) {
            isRollingRight = false;
        }
        if (Input.GetKey(KeyCode.D) && !isRollingRight && Time.time > nextIsRollingRightTime) {
            isRollingRight = true;
            runIsRollingRight = true;
            nextIsRollingRightTime = Time.time + 0.1f;
        }

        if (isOffRollingRight == true && Time.time > nextIsOffRollingRightTime) {
            isOffRollingRight = false;
        }
        if (!Input.GetKey(KeyCode.D) && !isOffRollingRight && (rollThrustPercentage < 0) && Time.time > nextIsOffRollingRightTime) {
            isOffRollingRight = true;
            runOffIsRollingRight = true;
            nextIsOffRollingRightTime = Time.time + 0.1f;
        }

        if (Input.GetKeyDown(KeyCode.V)) {
            interStellarToggle = !interStellarToggle;
        }

        if (inActivateInterStellarMode == true && Time.time > nextInActivateInterStellarModeTime && nextInActivateInterStellarModeTime != 0f) {
            inActivateInterStellarMode = false;
            if (interStellarToggle) {
                interStellarEngineWorking = true;
            }
        }
        if (interStellarToggle && !inActivateInterStellarMode && !interStellarEngineWorking && Time.time > nextInActivateInterStellarModeTime) {
            inActivateInterStellarMode = true;
            nextInActivateInterStellarModeTime = Time.time + 5f;
        }

        if (Input.GetButtonDown("Fire2")) {
            lockingShipAim = !lockingShipAim;
            VoyageCam.SetActive(true);
        }

        if (Input.GetKeyDown(KeyCode.X)) {
            if (lockingShipAim && VoyageCam.activeSelf) {
                VoyageCam.SetActive(false);
            } else if (lockingShipAim) {
                VoyageCam.SetActive(true);
                lockingShipAim = false;
            } else {
                lockingShipAim = true;
                VoyageCam.SetActive(false);
            }
        } 
    }

    //Before fixed update
    private void TimeManager_OnTick() {
        if (base.IsOwner) {
            Reconciliation(default, false);
            CheckInput(out MoveData md);
            Move(md, false);
        }
        if (base.IsServer) {
            Move(default, true);
        }
    }

    //After fixed update
    private void TimeManager_OnPostTick() {
        if (base.IsServer) {
            //ReconcileData rd = new ReconcileData(GameManager.Instance.GetRealLocation(transform), transform.rotation, shipRigidbody.velocity, shipRigidbody.angularVelocity,
            //    thrustPercentage, rollThrustPercentage);

            ReconcileData rd = new ReconcileData(transform.position, transform.rotation, shipRigidbody.velocity, shipRigidbody.angularVelocity,
                thrustPercentage, rollThrustPercentage);
            Reconciliation(rd, true);
        }
    }

    private void CheckInput(out MoveData md) {
        md = default;

        var (success, position) = GetMousePosition();
        Vector3 targetDirection = (position - shipRigidbody.transform.position).normalized;

        md = new MoveData(targetDirection, interStellarToggle, interStellarEngineWorking, lockingShipAim, runThrottle,
                runOffThrottle, runReverseThrust, runOffReverseThrust, runIsRollingLeft, runOffIsRollingLeft, runIsRollingRight,
                runOffIsRollingRight);
    }


    [Replicate]
    private void Move(MoveData md, bool asServer, bool replaying = false) {
        CalculateEnginePower(md, asServer);
        AimShip(md);
    }

    private void CalculateEnginePower(MoveData md, bool asServer) {
        if (md.runThrottle) {
            if (thrustPercentage + 0.1f < 1) {
                thrustPercentage += 0.1f;
            } else {
                thrustPercentage = 1;
            }
            activeEnginePower =  enginePower * thrustPercentage;
            runThrottle = false;
        }

        if (md.runOffThrottle) {
            if (thrustPercentage - 0.05f > 0) {
                thrustPercentage -= 0.05f;
            } else {
                thrustPercentage = 0;
            }
            activeEnginePower = enginePower * thrustPercentage;
            runOffThrottle = false;
        }

        if (md.runReverseThrust) {
            if (thrustPercentage -0.1f > -0.4f) {
                thrustPercentage -= 0.1f;
            } else {
                thrustPercentage = -0.4f;
            }
            activeEnginePower = enginePower * thrustPercentage;
            runReverseThrust = false;
        }

        if (md.runOffReverseThrust) {
            if (thrustPercentage + 0.05f < 0f) {
                thrustPercentage += 0.05f;
            } else {
                thrustPercentage = 0f;
            }
            activeEnginePower = enginePower * thrustPercentage;
            runOffReverseThrust = false;
        }

        if (md.runIsRollingLeft) {
            if (rollThrustPercentage + 0.1f < 1f) {
                rollThrustPercentage += 0.1f;
            }
            else {
                rollThrustPercentage = 1f;
            }
            rollEngineActivePower = rollEnginePower * rollThrustPercentage;
            runIsRollingLeft = false;
        }

        if (md.runIsRollingRight) {
            if (rollThrustPercentage - 0.1f > -1f) {
                rollThrustPercentage -= 0.1f;
            }
            else {
                rollThrustPercentage = -1f;
            }
            rollEngineActivePower = rollEnginePower * rollThrustPercentage;
            runIsRollingRight = false;
        }

        if (md.runOffIsRollingLeft) {
            if (rollThrustPercentage - 0.2f > 0) {
                rollThrustPercentage -= 0.2f;
            } else {
                rollThrustPercentage = 0f;
            }
            rollEngineActivePower = rollEnginePower * rollThrustPercentage;
            runOffIsRollingLeft = false;
        }

        if (md.runOffIsRollingRight) {
            if (rollThrustPercentage + 0.2f < 0f) {
                rollThrustPercentage += 0.2f;
            } else {
                rollThrustPercentage = 0f;
            }
            rollEngineActivePower = rollEnginePower * rollThrustPercentage;
            runOffIsRollingRight = false;
        }

        if (md.interStellarEngineWorking) {
            activeEnginePower = interStellarEnginePower;
        }

        if (!md.interStellarToggle && md.interStellarEngineWorking) {
            activeEnginePower = 0;
            interStellarEngineWorking = false;
        }
    }

    private void AimShip(MoveData md) {
        Vector3 direction = md.targetDirection;
        Vector3 steeringVector = (direction - shipRigidbody.transform.forward);
        
        Vector3 targetDirection;
        turnSpeed = steeringVector.magnitude;
        if (turnSpeed > 1) {
            turnSpeed = 1;
        }
        if (turnSpeed > 0.05f) {
            turnSpeed *= 0.05f;
            if (md.lockingShipAim) {
                steeringVector = steeringVector.normalized * 0;
            } else {
                steeringVector = steeringVector.normalized * turnSpeed;
            }
            targetDirection = shipRigidbody.transform.forward + steeringVector; 
        } else {
            targetDirection = shipRigidbody.transform.forward;
        }

        shipRigidbody.AddTorque(targetDirection * rollEngineActivePower);
        transform.rotation = Quaternion.LookRotation(targetDirection, shipRigidbody.transform.up);
        shipRigidbody.AddForce(targetDirection * activeEnginePower, ForceMode.Force);
   }

    [Reconcile]
    private void Reconciliation(ReconcileData rd, bool asServer) {
        //Debug.Log(rd.position - GameManager.Instance.totalShift);
        //Sorun burda
        //transform.position = rd.position + GameManager.Instance.totalShift;

        transform.position = rd.position;
        transform.rotation = rd.rotation;
        shipRigidbody.velocity = rd.velocity;
        shipRigidbody.angularVelocity = rd.angularVelocity;
        thrustPercentage = rd.thrustPercentage;
        rollThrustPercentage = rd.rollThrustPercentage;
    }

    private (bool success, Vector3 position) GetMousePosition() {
        var ray = shipCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 1000000f, playerShipMask)) {
            return (success: true, position: hitInfo.point);
        } else {
            Vector3 vec = ray.direction * 500f;
            return (success: false, position: shipCamera.transform.position + vec);
        }
    }
    
}
