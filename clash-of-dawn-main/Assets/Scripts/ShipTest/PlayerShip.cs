using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using Cinemachine;

public class PlayerShip : NetworkBehaviour
{

    public override void OnStartClient() {
        base.OnStartClient();
        
        if (!IsOwner) {
            GetComponentInChildren<Camera>().gameObject.SetActive(false);
            GetComponentInChildren<CinemachineVirtualCamera>().gameObject.SetActive(false);
        }
    }

}
