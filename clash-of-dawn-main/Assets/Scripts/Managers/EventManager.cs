using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class EventManager : NetworkBehaviour
{
    
    public static EventManager Instance { get; private set; }

    public float EnemyShipEventChance = 0.1f;
    public float EnemyShipEventStartingCooldown = 60f;

    public EventSettings eventSettings;
    [HideInInspector]
    public EventSettings.AsteroidEventSetting asteroidEventSetting;

    private List<GameObject> asteroidEvents;

    private float eventCheckCounter;

    private void Awake() {
        Instance = this;
        eventCheckCounter = Time.time;
        asteroidEventSetting = eventSettings.asteroidEventSetting;
        asteroidEvents = new();
    }

    private bool temp = false;
    private GameObject tempObject;
    private void Update() {
        if (!IsServer)
            return;

        if (GameManager.Instance.started && !temp && Input.GetKeyDown(KeyCode.R)) {
            tempObject = StartAsteroidEvent(PlayerData.Instance);
            temp = !temp;
        } else if (GameManager.Instance.started && temp && Input.GetKeyDown(KeyCode.R)) {
            tempObject.GetComponent<AsteroidEvent>().EndAsteroidEvent();
            temp = !temp;
        }
        return;

        if (Time.time < eventCheckCounter)
            return;
        
        eventCheckCounter = Time.time + eventSettings.eventCheckInterval;
        foreach (PlayerData pd in GameManager.Instance.players) {
            if (IsReadyForAsteroidEvent(pd))
                StartAsteroidEvent(pd);
            if (IsReadyForEnemyShipEvent(pd))
                StartEnemyShipEvent(pd);
        }
    }

    private bool IsReadyForAsteroidEvent(PlayerData pd) {
        // Game start check
        if (!GameManager.Instance.started)
            return false;

        // Null checks
        if (MapManager.Instance == null)
            return false;

        // Check the event chance
        if (Random.Range(0f, 1f) > asteroidEventSetting.eventChance)
            return false;

        // Check if player is already having an event
        if (pd.eventInfos.isHavingAsteroidEvent)
            return false;

        // Check if time is ready
        Debug.Log("current time: " + Time.time + ", ready time: " + pd.eventInfos.asteroidEventReadyTime);
        if (pd.eventInfos.asteroidEventReadyTime > Time.time)
            return false;

        // Check if position is ready;
        foreach (GameObject planet in MapManager.Instance.planets) {
            if (IsInPlanetBorders(pd, planet))
                return false;
            
            foreach (GameObject moon in planet.GetComponent<PlanetObject>().moons) {
                if (IsInPlanetBorders(pd, planet))
                    return false;
            }
        }

        return true;
    }

    private GameObject StartAsteroidEvent(PlayerData pd) {
        Vector3 eventPosition = pd.playerShip.transform.position;

        GameObject eventObject = Instantiate(asteroidEventSetting.eventPrefab);
        Spawn(eventObject);
        asteroidEvents.Add(eventObject);
        float sqrRadius = asteroidEventSetting.eventInvolvedPlayersRadius;
        sqrRadius *= sqrRadius;
        foreach (PlayerData playerData in GameManager.Instance.players) {
            TargetStartAsteroidEvent(playerData.Owner, eventObject, eventPosition);
            if (Vector3.SqrMagnitude(playerData.playerShip.transform.position - eventPosition) < sqrRadius) {
                playerData.eventInfos.isHavingAsteroidEvent = true;
                eventObject.GetComponent<AsteroidEvent>().involvedPlayers.Add(playerData);
            }
        }

        return eventObject;
    }

    [TargetRpc]
    private void TargetStartAsteroidEvent(NetworkConnection conn, GameObject eventObject, Vector3 eventPosition) {
        eventObject.transform.parent = transform;
        eventObject.transform.position = eventPosition;

        Debug.Log("Asteroid Event Started!");
    }

    private bool IsReadyForEnemyShipEvent(PlayerData pd) {
        return false;
    }

    private void StartEnemyShipEvent(PlayerData pd) {

    }

    private bool IsInPlanetBorders(PlayerData pd, GameObject planet) {
        PlanetObject po = planet.GetComponent<PlanetObject>();
        GameObject ship = pd.playerShip;

        float sqrDistance = po.shapeSettings.planetRadius + po.planetSetting.borderRadius;
        sqrDistance *= sqrDistance;

        if (Vector3.SqrMagnitude(po.transform.position - ship.transform.position) < sqrDistance)
            return true;

        return false;
    }

}
