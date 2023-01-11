using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class EventManager : NetworkBehaviour
{
    
    public static EventManager Instance { get; private set; }

    // Carry these to game settings.
    public float AsteroidEventChance = 0.1f;
    public float AsteroidEventCooldown = 300f;
    public float AsteroidEventStartingCooldown = 30;
    public float AsteroidEventInvolvedPlayersRadius = 100f;
    public GameObject asteroidEventPrefab;

    public float EnemyShipEventChance = 0.1f;
    public float EnemyShipEventStartingCooldown = 60f;

    private void Awake() {
        Instance = this;
    }

    private void FixedUpdate() {
        if (!IsServer)
            return;

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
        if (Random.Range(0f, 1f) > AsteroidEventChance)
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

    private void StartAsteroidEvent(PlayerData pd) {
        Vector3 eventPosition = pd.playerShip.transform.position;

        // pd.eventInfos.asteroidEventReadyTime = Time.time + 300f;
        // pd.eventInfos.isHavingAsteroidEvent = true;

        GameObject eventObject = Instantiate(asteroidEventPrefab);
        Spawn(eventObject);
        float sqrRadius = AsteroidEventInvolvedPlayersRadius;
        sqrRadius *= sqrRadius;
        foreach (PlayerData playerData in GameManager.Instance.players) {
            if (Vector3.SqrMagnitude(playerData.playerShip.transform.position - eventPosition) < sqrRadius) {
                TargetStartAsteroidEvent(playerData.Owner, eventObject, eventPosition);
                playerData.eventInfos.isHavingAsteroidEvent = true;
                eventObject.GetComponent<AsteroidEvent>().involvedPlayers.Add(playerData);
            }
        }
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
