using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class EnemyData : NetworkBehaviour
{
    
    [field: SerializeField]
    [field: SyncVar]
    public float health {
        get;

        private set;
    }
    private float damageImmuneTime;

    private void Awake() {
        health = 100f;
        damageImmuneTime = Time.time;
    }

    private void FixedUpdate() {
        if (!IsServer)
            return;

        if (health <= 0) {
            ShipGenerator.Instance.DestroyShip(this.gameObject);
            Debug.Log("Ship destroyed!");
        }
    }

    public void DealDamage(float damage) {
        if (!IsServer)
            return;

        if (damageImmuneTime > Time.time)
            return;

        damageImmuneTime = Time.time + 0.1f;

        health = health - damage < 0 ? 0 : health - damage;
        Debug.Log("deal damage health: " + health);
    }

}
