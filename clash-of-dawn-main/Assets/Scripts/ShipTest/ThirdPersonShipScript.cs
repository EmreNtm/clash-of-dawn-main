using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ThirdPersonShipScript : MonoBehaviour
{

    [SerializeField] private LayerMask PlayerShip;
    // Private Fields of this ship
    private Camera mainCamera;  //attached camera
    private readonly float enginePower = 1000f;    //enginePower of ship
    private readonly float rollEnginePower = 100f;   //ships engines for rolling
    private bool waitLog = false;   //just for logs
    public Rigidbody shipsRigidBody; //ships rigidbody component
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

    //Public fields of this ship so player and we can see it on inspector
    [Range(-0.4f, 1f)] public float thrustPercentage;
    [Range(-400f, 1000f)] public float activeEnginePower = 0f; 
    [Range(-1f, 1f)] public float rollThrustPercentage;
    [Range(-100f, 100f)] public float rollEngineActivePower = 0f;
    
    

    public float turnSpeed;     //turn speed of ships nose with mouse


    void Start()
    {
        
        mainCamera = Camera.main;
        thrustPercentage = 0f;
        rollEngineActivePower = 0f;
        turnSpeed = 0.001f;
        //shipsRigidBody = GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        
        var (success, position) = GetMousePosition();
        targetDirection = (position - shipsRigidBody.transform.position).normalized;

        Debug.DrawRay(shipsRigidBody.transform.position, targetDirection * 50f, Color.red, 0);
        //player holds w for accelaration
        //Bu iki If state IF-ELSE ile birle�tirilebilir fakat i�eride bir if gerekir.
        if (Input.GetKey(KeyCode.W) && !throttleing && !enginePowerLockToggle && !interStellarEngineWorking)
        {
                
               StartCoroutine(OnThrottle());

        }
        //E�er s ve w ya ayn� anda bas�l� tutulursa engine sabit kalacakt�r
       //player does not hold w, if speed is greater than 0, we close the engine
        if (!Input.GetKey(KeyCode.W) && !offThrotling && ( thrustPercentage > 0 ) && !enginePowerLockToggle && !interStellarEngineWorking)
        {
            StartCoroutine(OffThrottle());
         }
        //holding s will open reverse thrust
        //Bu iki If state IF-ELSE ile birle�tirilebilir fakat i�eride bir if gerekir.
        if (Input.GetKey(KeyCode.S) && !reverseThrust && !enginePowerLockToggle && !interStellarEngineWorking)
        {

            StartCoroutine(ReverseThrust());
        }

        if (!Input.GetKey(KeyCode.S) && !offReverseThrust && (thrustPercentage < 0) && !enginePowerLockToggle && !interStellarEngineWorking)
        {
            StartCoroutine(OffReverseThrust());
        }

        if (Input.GetKeyDown(KeyCode.T) && !interStellarEngineWorking)
        {
            enginePowerLockToggle = !enginePowerLockToggle;
        }

        if (Input.GetKey(KeyCode.A) && !isRollingLeft)
        {
            StartCoroutine(RollingLeft());
        }

        if (!Input.GetKey(KeyCode.A) && !isOffRollingLeft && (rollEngineActivePower > 0))
        {
            StartCoroutine(OffRollingLeft());
        }


        if (Input.GetKey(KeyCode.D) && !isRollingRight)
        {
            StartCoroutine(RollingRight());
        }

        if (!Input.GetKey(KeyCode.D) && !isOffRollingRight && (rollEngineActivePower < 0))
        {
            StartCoroutine(OffRollingRight());
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            interStellarToggle = !interStellarToggle;
        }

        if (interStellarToggle && !inActivateInterStellarMode && !interStellarEngineWorking)
        {
            StartCoroutine(ActivateInterStellarMode());
        }

        if (!interStellarToggle && interStellarEngineWorking)
        {
            activeEnginePower = 0;
            engineHeatingCounter = 5;
            interStellarEngineWorking = false;
        }


        if (!waitLog)
        {
            StartCoroutine(Loggss());
        }



    }

    private void FixedUpdate()
    {
        
        AimShip(targetDirection);
    }

    private (bool success, Vector3 position) GetMousePosition()
    {
        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hitInfo, 1000000f , PlayerShip))
        {

            return (success: true, position: hitInfo.point);
        }
        else
        {
            Vector3 vec = ray.direction * 500f;
            return (success: false, position: mainCamera.transform.position + vec);
        }
    }
    
    private void AimShip(Vector3 direction)
    {
        

        Vector3 steeringVector = (direction - shipsRigidBody.transform.forward);
        
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
            targetDirection = shipsRigidBody.transform.forward + steeringVector; 
            
        }
        else 
        {
            targetDirection = shipsRigidBody.transform.forward;
        }

        shipsRigidBody.AddTorque(targetDirection * rollEngineActivePower);

        transform.rotation = Quaternion.LookRotation(targetDirection, shipsRigidBody.transform.up);

        // shipsRigidBody.MoveRotation(Quaternion.LookRotation(targetDirection, shipsRigidBody.transform.up));

        

        shipsRigidBody.AddForce(targetDirection * activeEnginePower, ForceMode.Force);


        // -----------------------OLD TRIAL SCR�PTS-------------------------
        // shipsRigidBody.velocity = targetDirection * speed;
        // AddRelativeForce kullan�labilir, ayr�ca her update te eklenmemeli.
        // transform.rotation = Quaternion.LookRotation(targetDirection, shipsRigidBody.transform.up);
        // controller.Move(speed * Time.deltaTime * transform.forward);
        // shipsRigidBody.AddForce(targetDirection * speed , ForceMode.Force);
        // -----------------------OLD TRIAL SCR�PTS-------------------------

        //DEGUB RAYS (geride kalma s�k�nt�s� fixed update sebebiyle)
        Debug.DrawRay(shipsRigidBody.transform.position, targetDirection * 50f, Color.green, 0);
        Debug.DrawRay(shipsRigidBody.transform.position, shipsRigidBody.transform.forward * 35f, Color.blue, 0);
        Debug.DrawRay(shipsRigidBody.transform.position, direction * 20f, Color.red, 0);
    }
      //increasing engine power
      IEnumerator OnThrottle ()
      {
          throttleing = true;
        // shipAccelaration += 0.05f;
          if (thrustPercentage + 0.1f < 1)
          {
            thrustPercentage += 0.1f;
          }  else
        {
            thrustPercentage = 1;
        }
          activeEnginePower =  enginePower * thrustPercentage;
          yield return new WaitForSeconds(0.1f);
          Debug.Log("W �al���yor.");
          throttleing= false;

      }
      //w b�rak�ld���nda otomatik �ekilde engine i kapat�r
      IEnumerator OffThrottle ()
      {
        offThrotling = true;
        // shipAccelaration += 0.05f;
        
        if(thrustPercentage - 0.05f > 0)
        {
            thrustPercentage -= 0.05f;
        }else
        {
            thrustPercentage = 0;
        }
        
        
        activeEnginePower = enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log("OTO BRAKE WORKS");
        offThrotling = false;

      }
      //geri vites, S ye bas�l� tutuldu�unda �al���r
      IEnumerator ReverseThrust()
      {
        reverseThrust = true;
        if (thrustPercentage -0.1f > -0.4f)
        {
            thrustPercentage -= 0.1f;
        }else
        {
            thrustPercentage = -0.4f;
        }

        activeEnginePower = enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log("REVERSE THRUST WORKS");
        reverseThrust = false;
      }
    //otomatik olarak engine i kapat�r geri vitesten eli �ekince
    IEnumerator OffReverseThrust()
    {
        offReverseThrust = true;
        

        if (thrustPercentage + 0.05f < 0f)
        {
            thrustPercentage += 0.05f;
        }
        else
        {
            thrustPercentage = 0f;
        }


        activeEnginePower = enginePower * thrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log("OTO BRAKE WORKS");
        offReverseThrust = false;

    }
    IEnumerator RollingLeft()
    {
        isRollingLeft = true;
        if (rollThrustPercentage + 0.1f < 1f)
        {
            rollThrustPercentage += 0.1f;
        }
        else
        {
            rollThrustPercentage = 1f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log("ROLLING Left");
        isRollingLeft = false;
    }

    IEnumerator RollingRight()
    {
        isRollingRight = true;
        if (rollThrustPercentage - 0.1f > -1f)
        {
            rollThrustPercentage -= 0.1f;
        }
        else
        {
            rollThrustPercentage = -1f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log("ROLLING RIGHT");
        isRollingRight = false;
    }

    IEnumerator OffRollingLeft()
    {
        isOffRollingLeft = true;
        if (rollThrustPercentage - 0.2f > 0)
        {
            rollThrustPercentage -= 0.2f;
        }
        else
        {
            rollThrustPercentage = 0f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log("OFF ROLLING Left");
        isOffRollingLeft = false;
    }

    IEnumerator OffRollingRight()
    {
        isOffRollingRight = true;
        if (rollThrustPercentage + 0.2f < 0f)
        {
            rollThrustPercentage += 0.2f;
        }
        else
        {
            rollThrustPercentage = 0f;
        }
        rollEngineActivePower = rollEnginePower * rollThrustPercentage;
        yield return new WaitForSeconds(0.1f);
        Debug.Log(" OFF ROLLING RIGHT");
        isOffRollingRight = false;
    }

    IEnumerator ActivateInterStellarMode() 
    {
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

        if (interStellarToggle)
        {
            activeEnginePower = interStellarEnginePower;
            interStellarEngineWorking = true;
        }

        inActivateInterStellarMode = false;


    }

    IEnumerator Loggss()
    {
        waitLog = true;
        Debug.Log("enginepower " + activeEnginePower);
        Debug.Log("rigidbody speed  " + shipsRigidBody.velocity.magnitude);
        yield return new WaitForSeconds(2);
        waitLog= false;
    }

}
