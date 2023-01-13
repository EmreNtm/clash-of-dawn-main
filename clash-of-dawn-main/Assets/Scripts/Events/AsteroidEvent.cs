using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class AsteroidEvent : NetworkBehaviour
{

    private EventSettings.AsteroidEventSetting asteroidEventSetting;

    [HideInInspector]
    public float startTime;

    [HideInInspector]
    public List<GameObject> asteroids;

    // Values server keeps values;
    [HideInInspector]
    public List<PlayerData> involvedPlayers = new();

    private float sqrEventBorder;

    private void Awake() {
        startTime = Time.time;
        asteroids = new();
        asteroidEventSetting = EventManager.Instance.eventSettings.asteroidEventSetting;
        sqrEventBorder = asteroidEventSetting.borderRadius + asteroidEventSetting.borderThickness;
        sqrEventBorder *= sqrEventBorder;
    }

    private void Start() {
        if (!IsServer)
            return;

        for (int i = 0; i < asteroidEventSetting.asteroidAmount; i++) {
            SpawnAsteroid();
        }
    }

    private void Update() {
        if (!IsServer)
            return;

        // Time condition to end event
        if (false && Time.time > startTime + asteroidEventSetting.duration) {
            EndAsteroidEvent();
            return;
        }

        GameObject asteroid;
        for (int i = asteroids.Count - 1; i >= 0; i--) {
            asteroid = asteroids[i];
            if (Vector3.SqrMagnitude(asteroid.transform.position - transform.position) > sqrEventBorder) {
                asteroid.GetComponent<NetworkObject>().Despawn();
                asteroids.RemoveAt(i);
                SpawnAsteroid();
                Debug.Log("Destroyed!");
            }
        }
    }

    private void SpawnAsteroid() {
        GameObject asteroid = Instantiate(asteroidEventSetting.asteroidPrefab);
        Spawn(asteroid);

        int shapeSettingsSeed = MapManager.serverPrng.Next(-100000, 100000);

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnAsteroid(pd.Owner, asteroid, shapeSettingsSeed);
        }
    }

    [TargetRpc]
    private void TargetSpawnAsteroid(NetworkConnection conn, GameObject asteroid, int seed) {
        asteroid.transform.parent = transform.GetChild(0);

        PlanetObject po = asteroid.GetComponent<PlanetObject>();
        po.autoUpdate = false;
        ShapeSettings ss = new ShapeSettings(po.shapeSettings, seed);
        ss.planetRadius = asteroidEventSetting.minMaxAsteroidSize.x + (float) MapManager.prng.NextDouble() * (asteroidEventSetting.minMaxAsteroidSize.y - asteroidEventSetting.minMaxAsteroidSize.x);
        po.shapeSettings = ss;
        po.CreatePlanet();

        float offset =((float) MapManager.prng.NextDouble() * 2 * asteroidEventSetting.borderThickness - asteroidEventSetting.borderThickness);
        asteroid.transform.position = transform.position + MapManager.Instance.PointOnUnitSphere() * (asteroidEventSetting.borderRadius + offset);
        
        Vector3 velocity = (transform.position - asteroid.transform.position).normalized;
        asteroid.transform.rotation = Quaternion.LookRotation(velocity, asteroid.transform.up);

        velocity = Quaternion.AngleAxis(((float) MapManager.prng.NextDouble() * 10f - 5f), asteroid.transform.up) * velocity;
        velocity = Quaternion.AngleAxis(((float) MapManager.prng.NextDouble() * 10f - 5f), asteroid.transform.right) * velocity;
        Rigidbody rb = asteroid.GetComponent<Rigidbody>();
        rb.velocity = velocity * ((float) MapManager.prng.NextDouble() * 240f - 120f);
        rb.angularVelocity = new Vector3(((float) MapManager.prng.NextDouble() * 5 - 2.5f), ((float) MapManager.prng.NextDouble() * 5 - 2.5f), 
                ((float) MapManager.prng.NextDouble() * 5f - 2.5f));

        asteroids.Add(asteroid);
    }

    public void EndAsteroidEvent() {
        foreach (PlayerData pd in involvedPlayers) {
            pd.eventInfos.asteroidEventReadyTime = Time.time + 30f;
            pd.eventInfos.isHavingAsteroidEvent = false;
        }

        foreach (GameObject asteroid in asteroids) {
            asteroid.GetComponent<NetworkObject>().Despawn();
        }
        Despawn();
        Debug.Log("Asteroid Event is Over!");
    }

}
