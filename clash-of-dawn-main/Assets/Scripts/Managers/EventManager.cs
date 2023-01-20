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
    public List<GameObject> asteroidEvents;

    [HideInInspector]
    public EventSettings.EnemyShipEventSetting enemyShipEventSetting;
    public List<GameObject> enemyShipEvents;

    private float eventCheckCounter;

    private void Awake() {
        Instance = this;
        eventCheckCounter = Time.time;
        asteroidEventSetting = eventSettings.asteroidEventSetting;
        asteroidEvents = new();

        enemyShipEventSetting = eventSettings.enemyShipEventSetting;
        enemyShipEvents = new();
    }

    public bool temp = false;
    private GameObject tempObject;
    private void Update() {
        if (!IsServer)
            return;

        // Event start with R key for testing.
        if (GameManager.Instance.started && !temp && Input.GetKeyDown(KeyCode.R)) {
            tempObject = StartEnemyShipEvent(PlayerData.Instance);
            temp = !temp;
        } else if (GameManager.Instance.started && temp && Input.GetKeyDown(KeyCode.R)) {
            tempObject.GetComponent<EnemyShipEvent>().EndEnemyShipEvent();
            temp = !temp;
        }
        return;

        if (Time.time < eventCheckCounter)
            return;
        eventCheckCounter = Time.time + eventSettings.eventCheckInterval;

        foreach (PlayerData pd in GameManager.Instance.players) {
            if (IsReadyForAsteroidEvent(pd)) {
                StartAsteroidEvent(pd);
                return;
            }
            if (IsReadyForEnemyShipEvent(pd)) {
                StartEnemyShipEvent(pd);
                return;
            }
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
            if (IsInPlanetEventBorders(pd, planet))
                return false;
            
            foreach (GameObject moon in planet.GetComponent<PlanetObject>().moons) {
                if (IsInPlanetEventBorders(pd, planet))
                    return false;
            }
        }

        return true;
    }

    private GameObject StartAsteroidEvent(PlayerData pd) {
        Vector3 eventPosition = pd.playerShip.transform.position;

        GameObject eventObject = Instantiate(asteroidEventSetting.eventPrefab);
        Spawn(eventObject);
        eventObject.transform.position = eventPosition;
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
        // Game start check
        if (!GameManager.Instance.started)
            return false;

        // Null checks
        if (MapManager.Instance == null)
            return false;

        // Check the event chance
        if (Random.Range(0f, 1f) > enemyShipEventSetting.eventChance)
            return false;

        // Check if player is already having an event
        if (pd.eventInfos.isHavingEnemyShipEvent)
            return false;

        // Check if time is ready
        Debug.Log("current time: " + Time.time + ", ready time: " + pd.eventInfos.enemyShipEventReadyTime);
        if (pd.eventInfos.enemyShipEventReadyTime > Time.time)
            return false;

        // Check if position is ready;
        foreach (GameObject planet in MapManager.Instance.planets) {
            if (IsInPlanetEventBorders(pd, planet))
                return false;
            
            foreach (GameObject moon in planet.GetComponent<PlanetObject>().moons) {
                if (IsInPlanetEventBorders(pd, planet))
                    return false;
            }
        }

        return true;
    }

    private GameObject StartEnemyShipEvent(PlayerData pd) {
        Vector3 eventPosition = pd.playerShip.transform.position;

        GameObject eventObject = Instantiate(enemyShipEventSetting.eventPrefab);
        Spawn(eventObject);
        eventObject.transform.position = eventPosition;
        enemyShipEvents.Add(eventObject);
        float sqrRadius = enemyShipEventSetting.eventInvolvedPlayersRadius;
        sqrRadius *= sqrRadius;
        foreach (PlayerData playerData in GameManager.Instance.players) {
            TargetStartEnemyShipEvent(playerData.Owner, eventObject, eventPosition);
            if (Vector3.SqrMagnitude(playerData.playerShip.transform.position - eventPosition) < sqrRadius) {
                playerData.eventInfos.isHavingEnemyShipEvent = true;
                eventObject.GetComponent<EnemyShipEvent>().involvedPlayers.Add(playerData);
            }
        }

        return eventObject;
    }

    [TargetRpc]
    private void TargetStartEnemyShipEvent(NetworkConnection conn, GameObject eventObject, Vector3 eventPosition) {
        eventObject.transform.parent = transform;
        eventObject.transform.position = eventPosition;

        Debug.Log("EnemyShip Event Started!");
    }

    private bool IsInPlanetEventBorders(PlayerData pd, GameObject planet) {
        PlanetObject po = planet.GetComponent<PlanetObject>();
        GameObject ship = pd.playerShip;

        float sqrDistance = po.shapeSettings.planetRadius + po.planetSetting.eventBorderRadius;
        sqrDistance *= sqrDistance;

        if (Vector3.SqrMagnitude(po.transform.position - ship.transform.position) < sqrDistance)
            return true;

        return false;
    }

}
