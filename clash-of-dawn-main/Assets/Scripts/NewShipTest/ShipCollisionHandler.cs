using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipCollisionHandler : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.tag == "Asteroid" && PlayerData.Instance.playerShip == this.gameObject) {
            PlayerData.Instance.DealDamage(5f);
        }
    }

}
