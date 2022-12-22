using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public sealed class Player : NetworkBehaviour
{

    public static Player Instance { get; private set; }

    [field: SerializeField]
    [field: SyncVar]
    public int score {
        get;

        [ServerRpc]
        private set;
    }

    public override void OnStartClient() {
        base.OnStartClient();

        if (!IsOwner)
            return;

        Instance = this;

        ViewManager.Instance.Initialize();
    }

    private void Update() {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.R)) {
            score = Random.Range(0, 1024);
        }
    }

}
