using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class WeaponAgent : Agent 
{

    [SerializeField]
    private AIweaponRotationHandler rotationHandler;
    [SerializeField]
    private AIbullet bullet1;
    [SerializeField]
    private AIbullet bullet2;

    public List<GameObject> targets;
    private int targetAmount;

    [HideInInspector]
    public int bulletCount;

    private void Awake() {
        targets = new();
        // GameObject target;
        // for (int i = 0; i < maxTargetAmount; i++) {
        //     target = Instantiate(targetPrefab, transform.position, Quaternion.identity);
        //     target.transform.parent = transform.parent.GetChild(0);
        //     target.SetActive(false);
        //     targets.Add(target);
        // }
    }

    private Vector3 size = new Vector3(500, 300, 1800);
    [SerializeField] private LayerMask targetMask;
    private void FixedUpdate() {
        targets.Clear();
        Collider[] colliders = Physics.OverlapBox(transform.position + transform.forward * 1000f + transform.up * 150f, size * 0.5f, transform.rotation, targetMask);
        // Debug.Log(colliders.Length);
        int i = 0;
        while (i < colliders.Length && i < 1) {
            targets.Add(colliders[i].gameObject);
            Debug.DrawLine(transform.position, targets[i].transform.position, Color.red, 0f);
            i++;
        }
        targetAmount = targets.Count;

        if (targets.Count > 0)
            RequestDecision();
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.green;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position + transform.forward * 1000f + transform.up * 150f, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;
    }

    public override void OnEpisodeBegin()
    {
        // targetAmount = Random.Range(minTargetAmount, maxTargetAmount);
        // for (int i = 0; i < targets.Count; i++) {
        //     if (i < targetAmount) {
        //         targets[i].transform.localPosition = new Vector3(Random.Range(-40, 40), Random.Range(-20, 30), Random.Range(15, 300));
        //         targets[i].GetComponent<Rigidbody>().velocity = new Vector3(Random.Range(-30f, 30f), Random.Range(-30f, 30f), Random.Range(-30f, 30f));
        //         targets[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        //         targets[i].transform.rotation = Quaternion.identity;
        //     }
        //     targets[i].SetActive(i < targetAmount);
        // }

        // rotationHandler.ResetGunTransform();
        bullet1.bullet.Clear(true);
        bullet2.bullet.Clear(true);
        bulletCount = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float gunAngle = rotationHandler.currentGunAngle;
        float turretAngle = Vector3.SignedAngle(rotationHandler.transform.forward, 
            Vector3.ProjectOnPlane(rotationHandler.turretMainTargetDirection, transform.up), rotationHandler.transform.right);

        //Normalization
        gunAngle /= 180f;
        turretAngle /= 180f;
        
        sensor.AddObservation(gunAngle);
        sensor.AddObservation(turretAngle);
        sensor.AddObservation(bulletCount / 14f);

        BufferSensorComponent bsc = GetComponent<BufferSensorComponent>();
        for (int i = 0; i < targetAmount; i++) {
            bsc.AppendObservation(GetTargetObservartions(targets[i]));
        }
    }

    private float[] GetTargetObservartions(GameObject target) {
        Transform targetTransform = target.transform;
        Rigidbody targetRb = target.GetComponent<Rigidbody>();

        Vector3 from = rotationHandler.gunTransform.forward;
        Vector3 towardsTargetVec = targetTransform.position - rotationHandler.gunTransform.position;
        Vector3 to = Vector3.ProjectOnPlane(towardsTargetVec, rotationHandler.turretMain.right);
        float rightAngle = Vector3.SignedAngle(from, to, rotationHandler.turretMain.right);

        from = rotationHandler.turretMain.forward;
        towardsTargetVec = targetTransform.position - rotationHandler.turretMain.position;
        to = Vector3.ProjectOnPlane(towardsTargetVec, rotationHandler.transform.up);
        float forwardAngle = -Vector3.SignedAngle(from, to, rotationHandler.transform.up);

        rightAngle /= 180f;
        forwardAngle /= 180f;

        float[] enemyObs = new float[1 * 8];
        enemyObs[0] = (targetTransform.localPosition.x - transform.localPosition.x) / 900f;
        enemyObs[1] = (targetTransform.localPosition.y - transform.localPosition.y) / 900f;
        enemyObs[2] = (targetTransform.localPosition.z - transform.localPosition.z) / 900f;
        enemyObs[3] = rightAngle;
        enemyObs[4] = forwardAngle;
        enemyObs[5] = targetRb.velocity.x / 30f;
        enemyObs[6] = targetRb.velocity.y / 30f;
        enemyObs[7] = targetRb.velocity.z / 30f;

        return enemyObs;
    }

    // Screen method
    public override void OnActionReceived(ActionBuffers actions)
    {
        float mouseX =  (((actions.ContinuousActions[0] + 1) / 2f) * Screen.width);
        float mouseY =  (((actions.ContinuousActions[1] + 1) / 2f) * Screen.height);

        rotationHandler.AILastMousePos = new Vector3(mouseX, mouseY, 0);
        if (actions.DiscreteActions[0] != 0) {
            bullet1.AIFire();
            bullet2.AIFire();
        }

        float rewardAngle, reward;
        //float max = 0;
        float punishAngle = 15f;
        for (int i = 0; i < targetAmount; i++) {
            rewardAngle = Vector3.Angle(rotationHandler.gunTargetDirection, targets[i].transform.position - rotationHandler.gunTransform.position);
            // if (rewardAngle < 90f) {
                reward = rewardAngle > punishAngle ? -0.01f : 0;
                rewardAngle = Mathf.Clamp(rewardAngle, 0, punishAngle);
                rewardAngle = Mathf.Lerp(0.01f, 1f, rewardAngle / punishAngle) * 100;
                reward += 0.5f / (rewardAngle * rewardAngle);
                //max = reward > max ? reward : max;
                AddReward(reward);
            // }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = (Input.mousePosition.x / Screen.width) * 2 - 1;
        continuousActions[1] = (Input.mousePosition.y / Screen.height) * 2 - 1;

        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetButton("Fire1") ? 1 : 0;
    }

    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        actionMask.SetActionEnabled(0, 1, !bullet1.firing);
    }

}
