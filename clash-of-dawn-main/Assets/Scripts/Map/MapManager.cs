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
    public GameObject sunBody;

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
    public List<GameObject> planets = new();
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
        planets = new();
    }

    private void Update(){
        //Debug.Log($"server: {IsServer}, count: {planets.Count}" );
    }

    public void CreateSystem(int seed = 0) {
        //return;
        systemSettings.seed = seed; // != 0 ? seed : Random.Range(-100000, 100000);
        prng = new System.Random(systemSettings.seed);
        serverPrng = new System.Random(systemSettings.seed);
        foreach (GameObject planet in planets) {
            PlanetObject po = planet.GetComponent<PlanetObject>();
            foreach (GameObject moon in po.moons) {
                SafeDestroy(moon);
            }
            po.moons.Clear();
            po.moons = new();
            SafeDestroy(planet);
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

        sunBody.GetComponent<PlanetObject>().shapeSettings.planetRadius = systemSettings.sunRadius * systemSettings.scale;
        sunBody.GetComponent<PlanetObject>().CreatePlanet();
        GameObject go = Instantiate(MapManager.Instance.stationPrefab);
        stations.Add(go);
        GameManager.Instance.Spawn(go);

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnStation(pd.Owner, sunBody, go, 5000f);
        }

        Debug.Log(systemSettings.seed);
        if (IsServer) {
            for (int i = planetsToCreate.Count - 1; i >= 0; i--) {
                SpawnPlanet(planetsToCreate[i], i);
                TryToSpawnSpaceStation(planetsToCreate[i], planets[planets.Count - 1]);
                TryToSpawnMoon(planetsToCreate[i], i, planetsToCreate[i].maxMoonAmount);
            }

            if (planets.Count == 0)
                return;

            foreach (PlayerData pd in GameManager.Instance.players) {
                TargetCreateStars(pd.Owner, planets[planets.Count - 1].GetComponent<PlanetObject>().orbitDistance);
            }

            if (IsHost)
                TargetClearPlanetCopies(PlayerData.Instance.Owner);
        }
    }

    [TargetRpc]
    private void TargetClearPlanetCopies(NetworkConnection conn) {
        for (int i = planets.Count / 2 - 1; i >= 0; i--) {
            planets.RemoveAt(i);
        }
    }

    public struct PlanetParameters {
        public int planetSettingIndex;
        public int shapeSettingsSeed;
        public float planetRadiusRandom;
        public float orbitDistanceRandom;
        public Vector3 rotationSpeed;
    }

    private void SpawnPlanet(SystemSettings.PlanetSetting planetSetting, int settingIndex) {
        //GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        GameObject go = Instantiate(MapManager.Instance.planetPrefab);
        planets.Add(go);
        GameManager.Instance.Spawn(go);
        
        PlanetParameters parameters = new PlanetParameters();
        parameters.planetSettingIndex = settingIndex;
        parameters.shapeSettingsSeed = serverPrng.Next(-100000, 100000);
        parameters.planetRadiusRandom = (float) serverPrng.NextDouble();
        parameters.orbitDistanceRandom = (float) serverPrng.NextDouble();
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
        ss.planetRadius *= systemScale;

        float lastOrbitDistance;
        if (IsHost) {
            lastOrbitDistance = planets.Count != planetsToCreate.Count ? planets[planets.Count - 1].GetComponent<PlanetObject>().orbitDistance : systemSettings.sunRadius * systemSettings.scale;
        }
        else
            lastOrbitDistance = planets.Count != 0 ? planets[planets.Count - 1].GetComponent<PlanetObject>().orbitDistance : systemSettings.sunRadius * systemSettings.scale;
        lastOrbitDistance += ss.planetRadius;
        //Constant distance between aligned planets.
        lastOrbitDistance += systemSettings.increasePlanetOrbitDistance * systemScale;
        if (planets.Count != 0)
            lastOrbitDistance += planets[planets.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius;
        po.orbitDistance = lastOrbitDistance;

        po.shapeSettings = ss;
        po.CreatePlanet();

        //Add rotate speed
        RotatePlanet rp = go.GetComponent<RotatePlanet>();
        rp.rotationSpeed = parameters.rotationSpeed;

        //Place Planet to orbit of center star
        Vector3 distanceVector = lastOrbitDistance * PointOnUnitCircle();
        Vector3 turnAroundVector = Quaternion.Euler(0, 90, 0) * distanceVector;
        float angle = systemSettings.maxPlaneAngle;
        distanceVector = Quaternion.AngleAxis((float) prng.NextDouble() * 2 * angle - angle, turnAroundVector) * distanceVector;
        Vector3 orbitPos = distanceVector;

        go.transform.localPosition = orbitPos;
        planets.Add(go);
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
        go.transform.localScale *= systemScale;
        go.GetComponent<CTFManager>().parentPlanet = planet;

        float planetRadius = planet.GetComponent<PlanetObject>().shapeSettings.planetRadius;
        float distance = go.transform.localScale.x / 2f;
        distance += stationDistance * systemScale;
        distance += planetRadius;

        Vector3 orbitPos = planet.transform.localPosition + PointOnUnitSphere() * distance;
        go.transform.localPosition = orbitPos;
        go.transform.rotation = Quaternion.LookRotation(go.transform.position - planet.transform.position, go.transform.up);
        stations.Add(go);
    }

    private void TryToSpawnMoon(SystemSettings.PlanetSetting planetSetting, int settingIndex, int availableMoonAmount) {
        if (availableMoonAmount == 0 || serverPrng.NextDouble() > planetSetting.moonCreateChance)
            return;

        GameObject moon = Instantiate(MapManager.Instance.planetPrefab);
        GameObject parentPlanet = planets[planets.Count - 1];
        parentPlanet.GetComponent<PlanetObject>().moons.Add(moon);
        parentPlanet.GetComponent<PlanetObject>().serverMoonListFlag = true;
        GameManager.Instance.Spawn(moon);

        PlanetParameters parameters = new PlanetParameters();
        parameters.planetSettingIndex = settingIndex;
        parameters.shapeSettingsSeed = serverPrng.Next(-100000, 100000);
        parameters.planetRadiusRandom = (float) serverPrng.NextDouble();
        parameters.orbitDistanceRandom = (float) serverPrng.NextDouble();
        parameters.rotationSpeed = new Vector3(serverPrng.Next(-5, 5), serverPrng.Next(-5, 5), serverPrng.Next(-5, 5));

        foreach (PlayerData pd in GameManager.Instance.players) {
            TargetSpawnMoon(pd.Owner, moon, parameters);
        }

        //Try to add another Moon to Planet
        TryToSpawnMoon(planetSetting, settingIndex, availableMoonAmount - 1);
    }

    [TargetRpc]
    private void TargetSpawnMoon(NetworkConnection conn, GameObject moon, PlanetParameters parameters) {
        GameObject parentPlanet = planets[planets.Count - 1];

        //Move Planet game object
        moon.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        moon.name = "Moon";

        //Create Planet body
        SystemSettings.PlanetSetting planetSetting = MapManager.Instance.planetsToCreate[parameters.planetSettingIndex];
        PlanetObject po = moon.GetComponent<PlanetObject>();
        po.planetSetting = planetSetting;
        ShapeSettings ss = new ShapeSettings(planetSetting.moonShapeSettings, parameters.shapeSettingsSeed);

        ss.planetRadius = planetSetting.moonMinMaxSize.x + parameters.planetRadiusRandom * (planetSetting.moonMinMaxSize.y - planetSetting.moonMinMaxSize.x);
        ss.planetRadius *= systemScale;

        po.shapeSettings = ss;
        po.colorSettings = planetSetting.moonColorSettings;
        po.autoUpdate = false;
        po.resolution = planetSetting.moonResolution;
        po.isMoon = true;
        po.parentPlanet = parentPlanet;
        po.CreatePlanet();
        
        bool isFirstMoon = false;
        if (!IsServer)
            isFirstMoon = po.parentPlanet.GetComponent<PlanetObject>().moons.Count == 0;
        else if (po.parentPlanet.GetComponent<PlanetObject>().serverMoonListFlag) {
            isFirstMoon = true;
            po.parentPlanet.GetComponent<PlanetObject>().serverMoonListFlag = false;
        }
            
        PlanetObject parentPo = parentPlanet.GetComponent<PlanetObject>();

        float lastOrbitDistance = !isFirstMoon ? parentPo.moons[parentPo.moons.Count - 1].GetComponent<PlanetObject>().orbitDistance : parentPo.shapeSettings.planetRadius;
        lastOrbitDistance += ss.planetRadius;
        lastOrbitDistance += systemSettings.increaseMoonOrbitDistance * systemScale;
        if (!isFirstMoon)
            lastOrbitDistance += parentPo.moons[parentPo.moons.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius;
        po.orbitDistance = lastOrbitDistance;

        //Add rotate speed
        RotatePlanet rp = moon.GetComponent<RotatePlanet>();
        rp.rotationSpeed = parameters.rotationSpeed;

        //Place Planet to orbit of center star
        Vector3 orbitPos = parentPlanet.transform.localPosition + PointOnUnitSphere() * lastOrbitDistance;
        moon.transform.localPosition = orbitPos;
        po.parentPlanet.GetComponent<PlanetObject>().moons.Add(moon);
    }

    [TargetRpc]
    private void TargetCreateStars(NetworkConnection conn, float distance) {
        starManager.distance = (distance + 50000) * 2f;
        distance = Mathf.Max(100000, distance);
        starManager.CreateStars();
    }

    //For offline config test
    public void CreateOfflineSystem(int seed = 0) {
        systemScale = systemSettings.scale;
        systemSettings.seed = seed != 0 ? seed : Random.Range(-100000, 100000);
        prng = new System.Random(systemSettings.seed);
        serverPrng = new System.Random(systemSettings.seed);
        foreach (GameObject planet in planets) {
            PlanetObject po = planet.GetComponent<PlanetObject>();
            foreach (GameObject moon in po.moons) {
                SafeDestroy(moon);
            }
            po.moons.Clear();
            po.moons = new();
            SafeDestroy(planet);
        }
        planets.Clear();
        planets = new();

        foreach (GameObject station in stations) {
            SafeDestroy(station);
        }
        stations.Clear();
        stations = new();

        sunBody.GetComponent<PlanetObject>().shapeSettings.planetRadius = systemSettings.sunRadius * systemSettings.scale;
        sunBody.GetComponent<PlanetObject>().CreatePlanet();
        GameObject go = Instantiate(stationPrefab);
        go.transform.parent = this.transform.GetChild(0).GetChild(1);
        go.transform.localScale *= systemScale; 
        go.GetComponent<CTFManager>().parentPlanet = sunBody;
        float planetRadius = sunBody.GetComponent<PlanetObject>().shapeSettings.planetRadius;
        float distance = go.transform.localScale.x / 2f;
        distance += 5000f * systemScale;
        distance += planetRadius;
        Vector3 orbitPos = sunBody.transform.localPosition + Vector3.forward * distance;
        go.transform.localPosition = orbitPos;
        go.transform.rotation = Quaternion.LookRotation(go.transform.position - sunBody.transform.position, go.transform.up);
        stations.Add(go);
        
        planetsToCreate = new();
        foreach (SystemSettings.PlanetSetting planetSetting in systemSettings.planetSettings) {
            for (int i = 0; i < prng.Next((int) planetSetting.minMaxAmount.x, (int) planetSetting.minMaxAmount.y); i++) {
                planetsToCreate.Add(planetSetting);
            }
        }
        planetsToCreate = Shuffle(planetsToCreate);

        if (Application.isEditor) {
            for (int i = planetsToCreate.Count - 1; i >= 0; i--) {
                AddPlanet(planetsToCreate[i]);
                TryToAddSpaceStation(planetsToCreate[i], planets[planets.Count - 1]);
                TryToAddMoon(planets[planets.Count - 1], planetsToCreate[i], planetsToCreate[i].maxMoonAmount);
            }
        }
    }

    //For offline config test
    public void AddPlanet(SystemSettings.PlanetSetting planetSetting) {
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

        float lastOrbitDistance = planets.Count != 0 ? planets[planets.Count - 1].GetComponent<PlanetObject>().orbitDistance : systemSettings.sunRadius * systemSettings.scale;
        lastOrbitDistance += ss.planetRadius;
        //Constant distance between aligned planets.
        lastOrbitDistance += systemSettings.increasePlanetOrbitDistance * systemScale;
        if (planets.Count != 0)
            lastOrbitDistance += planets[planets.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius;
        po.orbitDistance = lastOrbitDistance;

        po.shapeSettings = ss;
        po.CreatePlanet();

        //Add rotate script
        RotatePlanet rp = go.AddComponent<RotatePlanet>();
        rp.rotationSpeed = new Vector3(serverPrng.Next(-5, 5), serverPrng.Next(-5, 5), serverPrng.Next(-5, 5));

        //Place Planet to orbit of center star
        Vector3 distanceVector = lastOrbitDistance * Vector3.forward;
        Vector3 turnAroundVector = Quaternion.Euler(0, 90, 0) * distanceVector;
        float angle = systemSettings.maxPlaneAngle;
        distanceVector = Quaternion.AngleAxis((float) prng.NextDouble() * 2 * angle - angle, turnAroundVector) * distanceVector;
        Vector3 orbitPos = distanceVector;

        go.transform.localPosition = orbitPos;
        planets.Add(go);
    }

    //For offline config test
    private void TryToAddSpaceStation(SystemSettings.PlanetSetting planetSetting, GameObject planet) {
        if (serverPrng.NextDouble() > planetSetting.stationCreateChance)
            return;

        //Create Planet game object
        GameObject go = Instantiate(stationPrefab);
        go.transform.parent = this.transform.GetChild(0).GetChild(1);
        go.transform.localScale *= systemScale; 
        go.GetComponent<CTFManager>().parentPlanet = planet;

        //Place Planet to orbit of center star
        float planetRadius = planet.GetComponent<PlanetObject>().shapeSettings.planetRadius;
        float distance = go.transform.localScale.x / 2f;
        distance += planetSetting.stationDistance * systemScale;
        distance += planetRadius;
        
        Vector3 orbitPos = planet.transform.localPosition + Vector3.forward * distance;
        go.transform.localPosition = orbitPos;
        go.transform.rotation = Quaternion.LookRotation(go.transform.position - planet.transform.position, go.transform.up);
        stations.Add(go);
    }

    //For offline config test
    private void TryToAddMoon(GameObject go, SystemSettings.PlanetSetting planetSetting, int availableMoonAmount) {
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

        bool isFirstMoon = planets[planets.Count - 1].GetComponent<PlanetObject>().moons.Count == 0;
        PlanetObject parentPo = po.parentPlanet.GetComponent<PlanetObject>();

        float lastOrbitDistance = !isFirstMoon ? parentPo.moons[parentPo.moons.Count - 1].GetComponent<PlanetObject>().orbitDistance : parentPo.shapeSettings.planetRadius;
        lastOrbitDistance += ss.planetRadius;
        lastOrbitDistance += systemSettings.increaseMoonOrbitDistance * systemScale;
        if (!isFirstMoon)
            lastOrbitDistance += parentPo.moons[parentPo.moons.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius;
        po.orbitDistance = lastOrbitDistance;

        //Add rotate script
        RotatePlanet rp = moon.AddComponent<RotatePlanet>();
        rp.rotationSpeed = new Vector3((float) serverPrng.NextDouble() * 4 - 2, (float) serverPrng.NextDouble() * 10 - 5, (float) serverPrng.NextDouble() * 4 - 2);

        //Place Moon to orbit of Planet
        Vector3 orbitPos = go.transform.localPosition + Vector3.forward * lastOrbitDistance;
        moon.transform.localPosition = orbitPos;
        po.parentPlanet.GetComponent<PlanetObject>().moons.Add(moon);

        //Try to add another Moon to Planet
        TryToAddMoon(go, planetSetting, availableMoonAmount - 1);
    }

    private void OnDrawGizmos() {
        return;
        Color c;
        int points = 20;
        PlanetObject po;
        Plane plane = new Plane();
        foreach (GameObject planet in planets) {
            po = planet.GetComponent<PlanetObject>();
            c = points % 40 == 0 ? Color.green : Color.yellow;
            plane.Set3Points(transform.position, transform.position + Vector3.right, planet.transform.position);
            DrawGizmosCircle(transform.position, (planet.transform.position - transform.position).magnitude, points, c, plane.normal);
            points += 20;
            foreach (GameObject moon in po.moons) {
                c = Color.red;
                plane.Set3Points(po.transform.position, po.transform.position + Vector3.right, moon.transform.position);
                DrawGizmosCircle(po.transform.position, (moon.transform.position - po.transform.position).magnitude, points, c, plane.normal);
            }
        }
        c = Color.cyan;
        points = 20;
        CTFManager cm;
        foreach (GameObject station in stations) {
            cm = station.GetComponent<CTFManager>();
            if (cm.parentPlanet == null) continue;
            plane.Set3Points(cm.parentPlanet.transform.position, cm.parentPlanet.transform.position + Vector3.right, station.transform.position);
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

    public void ResetLists() {
        planets.Clear();
        planets = new();
        stations.Clear();
        stations = new();
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
