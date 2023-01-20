using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class CTFManager : NetworkBehaviour
{
    [HideInInspector]
    public GameObject parentPlanet;

    [HideInInspector]
    public EventSettings.CtfEventSetting ctfEventSetting;
    public Transform captureAreaTransform;

    private float sqrBorder;
    private float sqrInvolvedPlayerRadius;

    [HideInInspector]
    public bool isHostile = true;
    [HideInInspector]
    public bool isCtfActive = false;

    private void Awake() {
        ctfEventSetting = EventManager.Instance.eventSettings.ctfEventSetting;

        sqrBorder = ctfEventSetting.borderRadius + ctfEventSetting.borderThickness;
        sqrBorder *= sqrBorder;

        sqrInvolvedPlayerRadius = ctfEventSetting.eventInvolvedPlayersRadius;
        sqrInvolvedPlayerRadius *= sqrInvolvedPlayerRadius;
    }

    private void FixedUpdate() {
        if (!IsServer)
            return;
        
        // Game start check
        if (!GameManager.Instance.started)
            return;

        // Null checks
        if (MapManager.Instance == null)
            return;

        if (!isHostile || isCtfActive)
            return;

        if (PlayerData.Instance.eventInfos.ctfEventReadyTime > Time.time)
            return;

        foreach (PlayerData pd in GameManager.Instance.players) {
            if (Vector3.SqrMagnitude(pd.playerShip.transform.position - transform.position) < sqrBorder && !isCtfActive) {
                isCtfActive = true;
                StartCtfEvent();
                return;
            }
        }
    }

    private void OnDrawGizmos() {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, ctfEventSetting.borderRadius + ctfEventSetting.borderThickness);
    }

    private void StartCtfEvent() {
        Vector3 eventPosition = transform.position;

        GameObject eventObject = Instantiate(ctfEventSetting.eventPrefab);
        Spawn(eventObject);
        eventObject.transform.position = eventPosition;
        eventObject.GetComponent<CtfEvent>().ctfManager = this;
        foreach (PlayerData playerData in GameManager.Instance.players) {
            TargetStartCtfEvent(playerData.Owner, eventObject, eventPosition);
            if (Vector3.SqrMagnitude(playerData.playerShip.transform.position - eventPosition) < sqrInvolvedPlayerRadius) {
                playerData.eventInfos.isHavingCtfEvent = true;
                eventObject.GetComponent<CtfEvent>().involvedPlayers.Add(playerData);
            }
        }
    }

    [TargetRpc]
    private void TargetStartCtfEvent(NetworkConnection conn, GameObject eventObject, Vector3 eventPosition) {
        eventObject.transform.parent = transform;
        eventObject.transform.position = eventPosition;
        eventObject.GetComponent<CtfEvent>().ctfManager = this;
        eventObject.GetComponent<CtfEvent>().SetTargetTransform(this.captureAreaTransform);

        Debug.Log("Ctf Event Started!");
    }

}
