using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using FishNet.Object;
using FishNet.Connection;

[RequireComponent(typeof(ParticleSystem))]
public class BulletParticle2 : NetworkBehaviour
{
    [SerializeField] private Explosion explosion;
    public Rigidbody attachedShipRigidBody;
    public float damage = 50f;
    public bool firing = false;
    public ParticleSystem muzzleFlashLeft;
    public Vector3 bulletVel;
    public ParticleSystem bullet;

    private ObjectPool<Explosion> _explosions;

    private void Start()
    {
        
        _explosions = new ObjectPool<Explosion>(() =>
        {
            return Instantiate(explosion);
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
        // Apply the particle changes to the Particle System
        
        if (!IsOwner)
            return;

        if (Input.GetButton("Fire1") && !firing )
        {
            Debug.Log("Firing");
            ServerFireFlak();
        }
    }


    [ServerRpc]
    public void ServerFireFlak() {
        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetFireFlak(pd.Owner);
        }
    }

    [TargetRpc]
    public void TargetFireFlak(NetworkConnection conn) {
        firing = true;
        StartCoroutine(FiringBullets());
    }


     List<ParticleCollisionEvent> colEvents = new List<ParticleCollisionEvent>();
     private void OnParticleCollision(GameObject other)
     {
        int events = bullet.GetCollisionEvents(other, colEvents);

         for(int i = 0; i < events; i++)
         {
            // Instantiate(explosion, colEvents[i].intersection , Quaternion.LookRotation(colEvents[i].normal) );
            var explode = _explosions.Get();
            explode.transform.SetPositionAndRotation(colEvents[i].intersection, Quaternion.LookRotation(colEvents[i].normal));
            explode.Init(KillShape);
         }

        //  if(other.transform.root.TryGetComponent(out EnemyData en))
        //  {
        //     en.DealDamage(damage);
        //  }
        if (IsServer && other.layer == LayerMask.NameToLayer("EnemyShip")) {
            other.GetComponent<EnemyData>().DealDamage(GameManager.Instance.gameSettings.playerBulletDamage);
        }
     } 
     
     private void KillShape(Explosion explosion)
    {
        _explosions.Release(explosion);
    }



    IEnumerator FiringBullets()
    {

        muzzleFlashLeft.Play();
        //OLD RIGID BODY BULLET OUT OF USE
        //GameObject cloneBullet = Instantiate(bullet, transform.position, transform.rotation);
        //cloneBullet.SetActive(true);
        bulletVel = transform.forward * 500f;
        var emitParams = new ParticleSystem.EmitParams
        {
            velocity = bulletVel + attachedShipRigidBody.velocity
        };
        bullet.Emit(emitParams, 1);
        
        //OLD RIGID BODY BULLET OUT OF USE
        // bulletClone.GetComponent<Rigidbody>().AddForce(bulletVelocity + attachedShip.shipVelocity, ForceMode.VelocityChange);

        // bulletClone.velocity = (bulletSpeed * Time.deltaTime * transform.forward) + attachedShip.shipVelocity ;

        yield return new WaitForSeconds(0.166666667f);
        
        yield return new WaitForSeconds(0.166666667f);
        firing = false;



    }
}
