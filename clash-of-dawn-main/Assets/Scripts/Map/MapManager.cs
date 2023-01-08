using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using FishNet.Object;
using FishNet.Connection;

//Orbit çakışmamasını kodla
//Random generatoru game manager'a taşı
public class MapManager : NetworkBehaviour
{

    [HideInInspector]
    public StarManager starManager;

    public GameObject planetPrefab;
    public GameObject stationPrefab;

    public static MapManager Instance { get; private set; }

    public SystemSettings systemSettings;

    public enum PlanetType {
        Habitable,
        GasGiant,
        SpaceCiv,
        Desert,
        Volcanic
    }

    [HideInInspector]
    public List<GameObject> planets;
    [HideInInspector]
    public List<SystemSettings.PlanetSetting> planetsToCreate;

    [HideInInspector]
    public List<GameObject> stations;

    [HideInInspector]
    public static System.Random prng;
    [HideInInspector]
    public static System.Random serverPrng;

    private float systemScale;

    void Awake() {
        starManager = GetComponentInChildren<StarManager>();
        Instance = this;

        systemScale = MapManager.Instance.systemSettings.scale;
    }

    public void CreateSystem(int seed = 0) {
        //return;
        systemSettings.seed = seed != 0 ? seed : Random.Range(-100000, 100000);
        prng = new System.Random(systemSettings.seed);
        serverPrng = new System.Random(systemSettings.seed);
        foreach (GameObject gameObject in planets) {
            SafeDestroy(gameObject);
        }
        planets.Clear();
        planets = new();

        foreach (GameObject gameObject in stations) {
            SafeDestroy(gameObject);
        }
        stations.Clear();
        stations = new();
        
        planetsToCreate = new();
        foreach (SystemSettings.PlanetSetting planetSetting in systemSettings.planetSettings) {
            for (int i = 0; i < prng.Next((int) planetSetting.minMaxAmount.x, (int) planetSetting.minMaxAmount.y); i++) {
                planetsToCreate.Add(planetSetting);
            }
        }
        planetsToCreate = Shuffle(planetsToCreate);

        Debug.Log(systemSettings.seed);
        if (IsServer) {
            float distance = 0;
            for (int i = planetsToCreate.Count - 1; i >= 0; i--) {
                SpawnPlanet(planetsToCreate[i], i, ref distance);
                //TryToSpawnSpaceStation(planetsToCreate[i], planets[planets.Count - 1]);
                //TryToSpawnMoon(planets[planets.Count - 1], planetsToCreate[i], i, planetsToCreate[i].maxMoonAmount, planets[planets.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius);
            }

            foreach (PlayerData pd in GameManager.Instance.players) {
                TargetCreateStars(pd.Owner, distance);
            }
        }
    }

    public struct PlanetParameters {
        public int planetSettingIndex;
        public int shapeSettingsSeed;
        public float planetRadiusRandom;
        public float orbitDistanceRandom;
        public float lastOrbitDistance;
        public Vector3 rotationSpeed;
    }

    private void SpawnPlanet(SystemSettings.PlanetSetting planetSetting, int settingIndex, ref float lastOrbitDistance) {
        //GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        GameObject go = Instantiate(MapManager.Instance.planetPrefab);
        planets.Add(go);
        GameManager.Instance.Spawn(go);
        
        PlanetParameters parameters = new PlanetParameters();
        parameters.planetSettingIndex = settingIndex;
        parameters.shapeSettingsSeed = serverPrng.Next(-100000, 100000);
        parameters.planetRadiusRandom = (float) serverPrng.NextDouble();
        parameters.orbitDistanceRandom = (float) serverPrng.NextDouble();
        parameters.lastOrbitDistance = lastOrbitDistance;
        parameters.rotationSpeed = new Vector3(serverPrng.Next(-5, 5), serverPrng.Next(-5, 5), serverPrng.Next(-5, 5));

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnPlanet(pd.Owner, go, parameters);
        }
    }

    [TargetRpc]
    private void TargetSpawnPlanet(NetworkConnection conn, GameObject go, PlanetParameters parameters) {
        //Move Planet game object
        go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);

        //Create Planet body
        SystemSettings.PlanetSetting planetSetting = MapManager.Instance.planetsToCreate[parameters.planetSettingIndex];
        PlanetObject po = go.GetComponent<PlanetObject>();
        po.planetSetting = planetSetting;
        po.autoUpdate = false;
        po.resolution = planetSetting.resolution;
        po.colorSettings = planetSetting.colorSettings;
        ShapeSettings ss = new ShapeSettings(planetSetting.shapeSettings, parameters.shapeSettingsSeed);
        ss.planetRadius = planetSetting.minMaxSize.x + parameters.planetRadiusRandom * (planetSetting.minMaxSize.y - planetSetting.minMaxSize.x);
        float offset = ss.planetRadius + planetSetting.minMaxSize.y + parameters.orbitDistanceRandom * MapManager.Instance.systemSettings.increasePlanetOrbitDistance;
        float orbitDistance = parameters.lastOrbitDistance + offset;
        po.shapeSettings = ss;
        po.CreatePlanet();

        //Add rotate speed
        RotatePlanet rp = go.GetComponent<RotatePlanet>();
        rp.rotationSpeed = parameters.rotationSpeed;

        //Place Planet to orbit of center star
        Vector3 orbitPos = PointOnUnitCircle() * orbitDistance;
        orbitPos.y = (float) prng.NextDouble() * ss.planetRadius * 8f - ss.planetRadius * 4f;
        go.transform.localPosition = orbitPos;
    }

    private void TryToSpawnSpaceStation(SystemSettings.PlanetSetting planetSetting, GameObject planet) {
        if (serverPrng.NextDouble() > planetSetting.stationCreateChance)
            return;

        GameObject go = Instantiate(MapManager.Instance.stationPrefab);
        stations.Add(go);
        GameManager.Instance.Spawn(go);

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnStation(pd.Owner, planet, go, planetSetting.stationDistance);
        }
    }

    [TargetRpc]
    private void TargetSpawnStation(NetworkConnection conn, GameObject planet, GameObject go, float stationDistance) {
        go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(1);

        float planetRadius = planet.GetComponent<PlanetObject>().shapeSettings.planetRadius;
        Vector3 orbitPos = planet.transform.localPosition + PointOnUnitSphere() * (planetRadius + (float) prng.NextDouble() * planetRadius / 4f + stationDistance);
        go.transform.localPosition = orbitPos;
        stations.Add(go);
    }

    private void TryToSpawnMoon(GameObject go, SystemSettings.PlanetSetting planetSetting, int settingIndex, int availableMoonAmount, float distance) {
        if (availableMoonAmount == 0 || serverPrng.NextDouble() > planetSetting.moonCreateChance)
            return;

        //GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        GameObject moon = Instantiate(MapManager.Instance.planetPrefab);
        planets.Add(moon);
        GameManager.Instance.Spawn(moon);

        PlanetParameters parameters = new PlanetParameters();
        parameters.planetSettingIndex = settingIndex;
        parameters.shapeSettingsSeed = serverPrng.Next(-100000, 100000);
        parameters.planetRadiusRandom = (float) serverPrng.NextDouble();
        parameters.orbitDistanceRandom = (float) serverPrng.NextDouble();
        parameters.lastOrbitDistance = distance;
        parameters.rotationSpeed = new Vector3(serverPrng.Next(-5, 5), serverPrng.Next(-5, 5), serverPrng.Next(-5, 5));

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnMoon(pd.Owner, moon, go, parameters);
        }

        //Try to add another Moon to Planet
        TryToSpawnMoon(go, planetSetting, settingIndex, availableMoonAmount - 1, distance);
    }

    [TargetRpc]
    private void TargetSpawnMoon(NetworkConnection conn, GameObject moon, GameObject parentPlanet, PlanetParameters parameters) {
        //Move Planet game object
        moon.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        moon.name = "Moon";

        //Create Planet body
        SystemSettings.PlanetSetting planetSetting = MapManager.Instance.planetsToCreate[parameters.planetSettingIndex];
        PlanetObject po = moon.GetComponent<PlanetObject>();
        po.planetSetting = planetSetting;
        po.autoUpdate = false;
        po.resolution = planetSetting.moonResolution;
        po.colorSettings = planetSetting.moonColorSettings;
        ShapeSettings ss = new ShapeSettings(planetSetting.moonShapeSettings, parameters.shapeSettingsSeed);
        ss.planetRadius = planetSetting.moonMinMaxSize.x + parameters.planetRadiusRandom * (planetSetting.moonMinMaxSize.y - planetSetting.moonMinMaxSize.x);
        float offset = ss.planetRadius + planetSetting.moonMinMaxSize.y + parameters.orbitDistanceRandom * MapManager.Instance.systemSettings.increaseMoonOrbitDistance;
        float orbitDistance = parameters.lastOrbitDistance + offset;
        po.shapeSettings = ss;
        po.CreatePlanet();

        //Add rotate speed
        RotatePlanet rp = moon.GetComponent<RotatePlanet>();
        rp.rotationSpeed = parameters.rotationSpeed;

        //Place Planet to orbit of center star
        Vector3 orbitPos = parentPlanet.transform.localPosition + PointOnUnitSphere() * orbitDistance;
        moon.transform.localPosition = orbitPos;
    }

    [TargetRpc]
    private void TargetCreateStars(NetworkConnection conn, float distance) {
        starManager.distance = (distance + 50000) * 2f;
        distance = Mathf.Max(100000, distance);
        starManager.CreateStars();
    }

    //For offline config test
    public void CreateOfflineSystem(int seed = 0) {
        //return;
        systemScale = systemSettings.scale;
        systemSettings.seed = seed != 0 ? seed : Random.Range(-100000, 100000);
        prng = new System.Random(systemSettings.seed);
        serverPrng = new System.Random(systemSettings.seed);
        foreach (GameObject gameObject in planets) {
            SafeDestroy(gameObject);
        }
        planets.Clear();
        planets = new();

        foreach (GameObject gameObject in stations) {
            SafeDestroy(gameObject);
        }
        stations.Clear();
        stations = new();
        
        planetsToCreate = new();
        foreach (SystemSettings.PlanetSetting planetSetting in systemSettings.planetSettings) {
            for (int i = 0; i < prng.Next((int) planetSetting.minMaxAmount.x, (int) planetSetting.minMaxAmount.y); i++) {
                planetsToCreate.Add(planetSetting);
            }
        }
        planetsToCreate = Shuffle(planetsToCreate);

        if (Application.isEditor) {
            float distance = 0;
            for (int i = planetsToCreate.Count - 1; i >= 0; i--) {
                AddPlanet(planetsToCreate[i], ref distance);
                TryToAddSpaceStation(planetsToCreate[i], planets[planets.Count - 1], distance);
                TryToAddMoon(planets[planets.Count - 1], planetsToCreate[i], planetsToCreate[i].maxMoonAmount, 0);
            }
        }
    }

    //For offline config test
    public void AddPlanet(SystemSettings.PlanetSetting planetSetting, ref float lastOrbitDistance) {
        //Create Planet game object
        GameObject go = new GameObject("Planet");
        go.transform.parent = this.transform.GetChild(0).GetChild(0);

        //Create Planet body
        PlanetObject po = go.AddComponent<PlanetObject>();
        po.planetSetting = planetSetting;
        po.autoUpdate = false;
        ShapeSettings ss = null;
        po.resolution = planetSetting.resolution;
        po.colorSettings = planetSetting.colorSettings;
        //ss = ScriptableObject.CreateInstance<ShapeSettings>();
        ss = new ShapeSettings(planetSetting.shapeSettings, serverPrng.Next(-100000, 100000));
        ss.planetRadius = planetSetting.minMaxSize.x + (float) serverPrng.NextDouble() * (planetSetting.minMaxSize.y - planetSetting.minMaxSize.x);
        ss.planetRadius *= systemScale;
        //lastOrbitDistance += ss.planetRadius + planetSetting.minMaxSize.y + (float) serverPrng.NextDouble() * systemSettings.increasePlanetOrbitDistance;
        lastOrbitDistance += ss.planetRadius;
        //Constant distance between aligned planets.
        lastOrbitDistance += systemSettings.increasePlanetOrbitDistance * systemScale;
        if (planets.Count != 0)
            lastOrbitDistance += planets[planets.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius;
        po.shapeSettings = ss;
        po.CreatePlanet();

        //Add rotate script
        RotatePlanet rp = go.AddComponent<RotatePlanet>();
        rp.rotationSpeed = new Vector3(serverPrng.Next(-5, 5), serverPrng.Next(-5, 5), serverPrng.Next(-5, 5));

        //Place Planet to orbit of center star
        Vector3 orbitPos = PointOnUnitCircle() * lastOrbitDistance;
        //Vector3 orbitPos = Vector3.forward * lastOrbitDistance;
        //orbitPos.y = (float) prng.NextDouble() * ss.planetRadius * 8f - ss.planetRadius * 4f;
        go.transform.localPosition = orbitPos;
        planets.Add(go);
    }

    //For offline config test
    private void TryToAddSpaceStation(SystemSettings.PlanetSetting planetSetting, GameObject planet, float lastOrbitDistance) {
        if (serverPrng.NextDouble() > planetSetting.stationCreateChance)
            return;

        //Create Planet game object
        GameObject go = Instantiate(stationPrefab);
        go.transform.parent = this.transform.GetChild(0).GetChild(1);
        go.transform.localScale *= systemScale; 
        go.GetComponent<CTFManager>().parentPlanet = planet;

        //Place Planet to orbit of center star
        float planetRadius = planet.GetComponent<PlanetObject>().shapeSettings.planetRadius;
        //Vector3 orbitPos = planet.transform.localPosition + PointOnUnitSphere() * (planetRadius + (float) prng.NextDouble() * planetRadius / 4f + planetSetting.stationDistance);
        float distance = go.transform.localScale.x / 2f;
        distance += planetSetting.stationDistance * systemScale;
        distance += planetRadius;
        
        Vector3 orbitPos = planet.transform.localPosition + PointOnUnitSphere() * distance;
        //Vector3 orbitPos = planet.transform.localPosition + Vector3.forward * distance;
        go.transform.localPosition = orbitPos;
        go.transform.rotation = Quaternion.LookRotation(go.transform.position - planet.transform.position, go.transform.up);
        stations.Add(go);
    }

    //For offline config test
    private void TryToAddMoon(GameObject go, SystemSettings.PlanetSetting planetSetting, int availableMoonAmount, float distance) {
        if (availableMoonAmount == 0 || serverPrng.NextDouble() > planetSetting.moonCreateChance)
            return;

        //Create Moon game object
        GameObject moon = new GameObject("Moon");
        moon.transform.parent = this.transform.GetChild(0).GetChild(0);

        //Create Moon body
        PlanetObject po = moon.AddComponent<PlanetObject>();
        po.planetSetting = planetSetting;
        ShapeSettings ss = new ShapeSettings(planetSetting.moonShapeSettings, serverPrng.Next(-100000, 100000));
        ss.planetRadius = planetSetting.moonMinMaxSize.x + (float) serverPrng.NextDouble() * (planetSetting.moonMinMaxSize.y - planetSetting.moonMinMaxSize.x);
        ss.planetRadius *= systemScale;
        po.shapeSettings = ss;
        po.colorSettings = planetSetting.moonColorSettings;
        po.autoUpdate = false;
        po.resolution = planetSetting.moonResolution;
        po.isMoon = true;
        po.parentPlanet = go;
        po.CreatePlanet();

        //distance += ss.planetRadius + planetSetting.moonMinMaxSize.y + (float) serverPrng.NextDouble() * systemSettings.increaseMoonOrbitDistance;
        distance += ss.planetRadius;
        distance += systemSettings.increaseMoonOrbitDistance * systemScale;
        if (planets.Count != 0)
            distance += planets[planets.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius;

        //Add rotate script
        RotatePlanet rp = moon.AddComponent<RotatePlanet>();
        rp.rotationSpeed = new Vector3((float) serverPrng.NextDouble() * 4 - 2, (float) serverPrng.NextDouble() * 10 - 5, (float) serverPrng.NextDouble() * 4 - 2);

        //Place Moon to orbit of Planet
        Vector3 orbitPos = go.transform.localPosition + PointOnUnitSphere() * distance;
        //orbitPos = go.transform.localPosition + Vector3.forward * distance;
        moon.transform.localPosition = orbitPos;
        planets.Add(moon);

        //Try to add another Moon to Planet
        TryToAddMoon(go, planetSetting, availableMoonAmount - 1, distance);
    }

    private void OnDrawGizmos() {
        Color c;
        int points = 20;
        PlanetObject po;
        Plane plane = new Plane();
        foreach (GameObject planet in planets) {
            po = planet.GetComponent<PlanetObject>();
            if (po.isMoon) {
                c = Color.red;
                plane.Set3Points(po.parentPlanet.transform.position, po.parentPlanet.transform.position + po.parentPlanet.transform.right, planet.transform.position);
                DrawGizmosCircle(po.parentPlanet.transform.position, (planet.transform.position - po.parentPlanet.transform.position).magnitude, points, c, plane.normal);
            } else {
                c = points % 40 == 0 ? Color.green : Color.yellow;
                DrawGizmosCircle(transform.position, (planet.transform.position - transform.position).magnitude, points, c, Vector3.up);
                points += 20;
            }
        }
        c = Color.cyan;
        points = 20;
        CTFManager cm;
        foreach (GameObject station in stations) {
            cm = station.GetComponent<CTFManager>();
            plane.Set3Points(cm.parentPlanet.transform.position, cm.parentPlanet.transform.position + cm.parentPlanet.transform.right, station.transform.position);
            DrawGizmosCircle(cm.parentPlanet.transform.position, (station.transform.position - cm.parentPlanet.transform.position).magnitude, points, c, plane.normal);
            points += 20;
        }
    }

    private void DrawGizmosCircle(Vector3 position, float radius, int numberOfPoints, Color color, Vector3 planeNormal) {
        Gizmos.color = color;
        Vector3 center = position;
        Vector3 right = Vector3.Cross(planeNormal, Vector3.right).normalized;

        Vector3 lastPoint = center + right * radius;
        for (int i = 0; i <= numberOfPoints; i++)
        {
            float angle = (float)i / numberOfPoints * 360f;
            Vector3 newPoint = center + Quaternion.AngleAxis(angle, planeNormal) * right * radius;
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }

    public Vector3 PointOnUnitCircle() {
        float angle = (float) prng.NextDouble() * Mathf.PI * 2;
        return Vector3.forward * Mathf.Sin(angle) + Vector3.right * Mathf.Cos(angle);
    }

    public Vector3 PointOnUnitSphere() {
        float theta = (float) prng.NextDouble() * Mathf.PI * 2f;
        float phi = (float) prng.NextDouble() * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(theta) * Mathf.Sin(theta), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(phi)).normalized;
    }

    public void OnSystemSettingsUpdate() {

    }

    public static T SafeDestroyGameObject<T>(T component) where T : Component {
        if (component != null)
            SafeDestroy(component.gameObject);
        return null;
    }

    public static T SafeDestroy<T>(T obj) where T : Object {
        if (Application.isEditor)
            Object.DestroyImmediate(obj);
        else
            Object.Destroy(obj);
        
        return null;
    }

    public static List<T> Shuffle<T>(List<T> _list) {
        for (int i = 0; i < _list.Count; i++)
        {
            T temp = _list[i];
            int randomIndex = prng.Next(i, _list.Count);
            _list[i] = _list[randomIndex];
            _list[randomIndex] = temp;
        }

        return _list;
    }

}
