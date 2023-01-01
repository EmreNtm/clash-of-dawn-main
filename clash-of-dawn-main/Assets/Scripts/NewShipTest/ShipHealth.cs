using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    public float health = 3000f;
    public GameObject shipBurning;
    public Rigidbody crackedShip;
    public bool isExploading = false;
    private Rigidbody shipsRigid;

    private void Start()
    {
        shipsRigid = GetComponent<Rigidbody>();
        
    }
    public void TakeDamage(float damage)
    {
        health -= damage;
        

    }

    private void Update()
    {
        //just to give a velocity for trying
        shipsRigid.velocity = shipsRigid.transform.forward * 35f; //þimdilik sadece hýz vermek için
        if (health <= 0 && !isExploading)
        {
            StartCoroutine(TheEnd());
            
        }
    }

    //birden fazla gemi spawnlanmasýný engelliyor

    IEnumerator TheEnd()
    {
        isExploading = true;
        var shipsDemise = Instantiate(shipBurning, transform.position + transform.forward * 31f , transform.rotation);
        shipsDemise.transform.SetParent(transform,true);
        yield return new WaitForSeconds(8f);
        var ship = Instantiate(crackedShip, transform.position, transform.rotation) ;
        ship.velocity = shipsRigid.velocity;
        shipsDemise.transform.SetParent(null);
        Destroy(gameObject);
        isExploading = false;
    }

}
