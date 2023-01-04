using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using FishNet;

public class OriginShiftPositionHandler : NetworkBehaviour
{

    private Vector3 offset = Vector3.zero;

    private void Awake() {
        InstanceFinder.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }

    private void OnDestroy() {
        if (InstanceFinder.TimeManager != null) {
            InstanceFinder.TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }

    private void TimeManager_OnPostTick() {
        if (!IsOwner)
            transform.position = (transform.position - offset) + GameManager.Instance.totalShift;
    }

    [TargetRpc]
    public void TargetSetOffsetVector(NetworkConnection conn, Vector3 _offset) {
        offset = _offset;
    }
}
