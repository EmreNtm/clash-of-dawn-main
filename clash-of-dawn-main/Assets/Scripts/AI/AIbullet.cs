using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(ParticleSystem))]
public class AIbullet : MonoBehaviour
{
    [SerializeField] private Explosion explosion;
    private Rigidbody attachedShipRigidBody;
    public float damage = 50f;
    public bool firing = false;
    public ParticleSystem muzzleFlashLeft;
    public Vector3 bulletVel;
    public ParticleSystem bullet;

    private ObjectPool<Explosion> _explosions;

    public WeaponAgent agent;

    private void Start() {
        GameObject explosionHolder = new GameObject("Explosion Holder");
        _explosions = new ObjectPool<Explosion>(() =>
        {
            Explosion go = Instantiate(explosion);
            go.transform.parent = explosionHolder.transform;
            return go;
        }, explosion => 
        {
            explosion.gameObject.SetActive(true);
        }, explosion =>
        {
            explosion.gameObject.SetActive(false);
        }, explosion =>
        {
            Destroy(explosion);
        }, false , 10 ,20  );
    }

    private void Update()
    {
        // // Apply the particle changes to the Particle System
        // if (Input.GetButton("Fire1") && !firing )
        // {
        //     Debug.Log("Firing");
        //     firing = true;
        //     StartCoroutine(FiringBullets());
        // }
    }

    public void AIFire() {
        if (!firing)
        {
            firing = true;
            StartCoroutine(FiringBullets());
        }
    }

    List<ParticleCollisionEvent> colEvents = new List<ParticleCollisionEvent>();
    private void OnParticleCollision(GameObject other) {
        int events = bullet.GetCollisionEvents(other, colEvents);

         for(int i = 0; i < events; i++)
         {
            var explode = _explosions.Get();
            explode.transform.SetPositionAndRotation(colEvents[i].intersection, Quaternion.LookRotation(colEvents[i].normal));
            explode.Init(KillShape);
         }

         //if(IsServer && other.layer == LayerMask.NameToLayer("EnemyShip"))
         {
            agent.AddReward(6f);
            agent.bulletCount--;
         }
    } 
     
    private void KillShape(Explosion explosion) {
        _explosions.Release(explosion);
    }

    IEnumerator FiringBullets() {
        muzzleFlashLeft.Play();
        bulletVel = transform.forward * 500f;
        var emitParams = new ParticleSystem.EmitParams
        {
            //velocity = bulletVel + attachedShipRigidBody.velocity
            velocity = bulletVel
        };
        bullet.Emit(emitParams, 1);
        agent.bulletCount++;
        agent.AddReward(-0.1f);

        yield return new WaitForSeconds(0.166666667f);
        
        yield return new WaitForSeconds(0.166666667f);
        firing = false;
    }
}
