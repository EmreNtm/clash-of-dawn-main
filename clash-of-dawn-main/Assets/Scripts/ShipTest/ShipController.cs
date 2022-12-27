using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class ShipController : NetworkBehaviour
{
    
    [SerializeField]
    private Transform myCamera;
    [SerializeField]
    private Transform thirdPersonCamera;

    [SerializeField] private LayerMask playerShipMask;
    private Camera mainCamera;
    public Rigidbody shipRigidBody;
    private readonly float enginePower = 1000f;    //enginePower of ship
    private readonly float rollEnginePower = 2f;   //ships engines for rolling
    private bool waitLog = false;   //just for logs
    private Vector3 targetDirection; //direction of ship from camera raycast
    //InterStellar Issies
    public float interStellarEnginePower = 20000f;
    public int engineHeatingCounter = 5;
    //Space Ship Controls if the functions and coroutines working right!
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

    public override void OnStartClient()
    {
        base.OnStartClient();

        thirdPersonCamera.gameObject.SetActive(IsOwner);
        myCamera.gameObject.SetActive(IsOwner);
    }

    void Start() {
        mainCamera = myCamera.GetComponent<Camera>();
        thrustPercentage = 0f;
        rollEngineActivePower = 0f;
        turnSpeed = 0.001f;
        //shipsRigidBody = GetComponent<Rigidbody>();

        ServerSetShipSpawnPosition();
    }

    [ServerRpc]
    public void ServerSetShipSpawnPosition() {
        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSetShipSpawnPoint(pd.Owner, Random.Range(200, 1000));
        }
    }

    [TargetRpc]
    public void TargetSetShipSpawnPoint(NetworkConnection conn, float random) {
        shipRigidBody.transform.position = new Vector3(0, 0, random);
    }

    void Update() {
        if (!IsOwner)
            return;

        var (success, position) = GetMousePosition();
        targetDirection = (position - shipRigidBody.transform.position).normalized;

        //player holds w for accelaration
        if (Input.GetKey(KeyCode.W) && !throttleing && !enginePowerLockToggle && !interStellarEngineWorking) {
            StartCoroutine(OnThrottle());
        }

        //player does not hold w, if speed is greater than 0, we close the engine
        if (!Input.GetKey(KeyCode.W) && !offThrotling && ( thrustPercentage > 0 ) && !enginePowerLockToggle && !interStellarEngineWorking) {
            StartCoroutine(OffThrottle());
        }

        //holding s will open reverse thrust
        if (Input.GetKey(KeyCode.S) && !reverseThrust && !enginePowerLockToggle && !interStellarEngineWorking) {
            StartCoroutine(ReverseThrust());
        }

        if (!Input.GetKey(KeyCode.S) && !offReverseThrust && (thrustPercentage < 0) && !enginePowerLockToggle && !interStellarEngineWorking) {
            StartCoroutine(OffReverseThrust());
        }

        if (Input.GetKeyDown(KeyCode.T) && !interStellarEngineWorking) {
            enginePowerLockToggle = !enginePowerLockToggle;
        }

        if (Input.GetKey(KeyCode.A) && !isRollingLeft) {
            StartCoroutine(RollingLeft());
        }

        if (!Input.GetKey(KeyCode.A) && !isOffRollingLeft && (rollEngineActivePower > 0)) {
            StartCoroutine(OffRollingLeft());
        }

        if (Input.GetKey(KeyCode.D) && !isRollingRight) {
            StartCoroutine(RollingRight());
        }

        if (!Input.GetKey(KeyCode.D) && !isOffRollingRight && (rollEngineActivePower < 0)) {
            StartCoroutine(OffRollingRight());
        }

        if (Input.GetKeyDown(KeyCode.V)) {
            interStellarToggle = !interStellarToggle;
        }

        if (interStellarToggle && !inActivateInterStellarMode && !interStellarEngineWorking) {
            StartCoroutine(ActivateInterStellarMode());
        }

        if (!interStellarToggle && interStellarEngineWorking) {
            activeEnginePower = 0;
            engineHeatingCounter = 5;
            interStellarEngineWorking = false;
        }

        if (Input.GetButtonDown("Fire2")) {
            lockingShipAim = !lockingShipAim;
        }

        if (!waitLog) {
            StartCoroutine(Loggss());
        }
    }

    void FixedUpdate() {
        if (!IsOwner)
            return;

        AimShip(targetDirection);
    }

    private void AimShip(Vector3 direction) {
        Vector3 steeringVector = (direction - shipRigidBody.transform.forward);
        
        Vector3 targetDirection;
        turnSpeed = steeringVector.magnitude;
        if (turnSpeed > 1) {
            turnSpeed = 1;
        }
        if (turnSpeed > 0.05f) {
            turnSpeed *= 0.01f;
            if (lockingShipAim) {
                steeringVector = steeringVector.normalized * 0;
            } else {
                steeringVector = steeringVector.normalized * turnSpeed;
            }
            targetDirection = shipRigidBody.transform.forward + steeringVector; 
        } else {
            targetDirection = shipRigidBody.transform.forward;
        }

        shipRigidBody.AddTorque(targetDirection * rollEngineActivePower);
        transform.rotation = Quaternion.LookRotation(targetDirection, shipRigidBody.transform.up);
        shipRigidBody.AddForce(targetDirection * activeEnginePower, ForceMode.Force);
   }

    private (bool success, Vector3 position) GetMousePosition() {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 1000000f, playerShipMask)) {
            return (success: true, position: hitInfo.point);
        } else {
            Vector3 vec = ray.direction * 1000000f;
            return (success: false, position: mainCamera.transform.position + vec);
        }
    }

    //increasing engine power
    IEnumerator OnThrottle () {
        throttleing = true;
        //shipAccelaration += 0.05f;
        if (thrustPercentage + 0.1f < 1) {
            thrustPercentage += 0.1f;
        } else {
            thrustPercentage = 1;
        }
        activeEnginePower =  enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        throttleing= false;
    }

    //w b�rak�ld���nda otomatik �ekilde engine i kapat�r
    IEnumerator OffThrottle () {
        offThrotling = true;
        //shipAccelaration += 0.05f;
        if (thrustPercentage - 0.05f > 0) {
            thrustPercentage -= 0.05f;
        } else {
            thrustPercentage = 0;
        }
        activeEnginePower = enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        offThrotling = false;
    }

    IEnumerator ReverseThrust() {
        reverseThrust = true;
        if (thrustPercentage -0.1f > -0.4f) {
            thrustPercentage -= 0.1f;
        } else {
            thrustPercentage = -0.4f;
        }
        activeEnginePower = enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        reverseThrust = false;
    }

    IEnumerator OffReverseThrust()
    {
        offReverseThrust = true;
        if (thrustPercentage + 0.05f < 0f) {
            thrustPercentage += 0.05f;
        } else {
            thrustPercentage = 0f;
        }
        activeEnginePower = enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        offReverseThrust = false;
    }

    IEnumerator RollingLeft() {
        isRollingLeft = true;
        if (rollThrustPercentage + 0.1f < 1f) {
            rollThrustPercentage += 0.1f;
        }
        else {
            rollThrustPercentage = 1f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        isRollingLeft = false;
    }

    IEnumerator RollingRight() {
        isRollingRight = true;
        if (rollThrustPercentage - 0.1f > -1f) {
            rollThrustPercentage -= 0.1f;
        }
        else {
            rollThrustPercentage = -1f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        isRollingRight = false;
    }

    IEnumerator OffRollingLeft() {
        isOffRollingLeft = true;
        if (rollThrustPercentage - 0.2f > 0) {
            rollThrustPercentage -= 0.2f;
        } else {
            rollThrustPercentage = 0f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        isOffRollingLeft = false;
    }

    IEnumerator OffRollingRight() {
        isOffRollingRight = true;
        if (rollThrustPercentage + 0.2f < 0f) {
            rollThrustPercentage += 0.2f;
        } else {
            rollThrustPercentage = 0f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        isOffRollingRight = false;
    }

    IEnumerator ActivateInterStellarMode()  {
        inActivateInterStellarMode = true;
        yield return new WaitForSeconds(1f);
        engineHeatingCounter--;
        yield return new WaitForSeconds(1f);
        engineHeatingCounter--;
        yield return new WaitForSeconds(1f);
        engineHeatingCounter--;
        yield return new WaitForSeconds(1f);
        engineHeatingCounter--;
        yield return new WaitForSeconds(1f);
        engineHeatingCounter--;

        if (interStellarToggle) {
            activeEnginePower = interStellarEnginePower;
            interStellarEngineWorking = true;
        }

        inActivateInterStellarMode = false;
    }

    IEnumerator Loggss() {
        waitLog = true;
        Debug.Log("enginepower " + activeEnginePower);
        Debug.Log("rigidbody speed  " + shipRigidBody.velocity.magnitude);
        yield return new WaitForSeconds(2);
        waitLog= false;
    }

}
