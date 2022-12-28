using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

[RequireComponent(typeof(ParticleSystem))]
public class BulletParticle : NetworkBehaviour
{
    
    
    public bool firing = false;
    public ParticleSystem muzzleFlashLeft;
    
    public ParticleSystem bullet;

    public Rigidbody shipRigidbody;

    private void Update()
    {
        if (!IsOwner)
            return;
        
        bullet.main.emitterVelocity.Set(shipRigidbody.velocity.x, shipRigidbody.velocity.y, shipRigidbody.velocity.z);

        // Apply the particle changes to the Particle System
        if (Input.GetButton("Fire1") && !firing ) {
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
    /* private void OnParticleCollision(GameObject other)
     {
        int events = m_System.GetCollisionEvents(other, colEvents);

         for(int i = 0; i < events; i++)
         {

         }
     } */

    IEnumerator FiringBullets()
    {

        muzzleFlashLeft.Play();
        //OLD RIGID BODY BULLET OUT OF USE
        //GameObject cloneBullet = Instantiate(bullet, transform.position, transform.rotation);
        //cloneBullet.SetActive(true);

        bullet.Emit(1);
        
        //OLD RIGID BODY BULLET OUT OF USE
        // bulletClone.GetComponent<Rigidbody>().AddForce(bulletVelocity + attachedShip.shipVelocity, ForceMode.VelocityChange);

        // bulletClone.velocity = (bulletSpeed * Time.deltaTime * transform.forward) + attachedShip.shipVelocity ;

        yield return new WaitForSeconds(0.166666667f);
        
        yield return new WaitForSeconds(0.166666667f);
        firing = false;



    }
}
