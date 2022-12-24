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
    public static System.Random prng;
    [HideInInspector]
    public static System.Random serverPrng;

    void Awake() {
        starManager = GetComponentInChildren<StarManager>();
        Instance = this;
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
                //AddPlanet(planetsToCreate[i], ref distance);
                //TryToAddMoon(planets[planets.Count - 1], planetsToCreate[i], planetsToCreate[i].maxMoonAmount, planets[planets.Count - 1].GetComponent<PlanetObject>().shapeSettings.planetRadius);
            }

            foreach (PlayerData pd in GameManager.Instance.players) {
                TargetCreateStars(pd.Owner, distance);
            }
        }
    }

    private void SpawnPlanet(SystemSettings.PlanetSetting planetSetting, int settingIndex, ref float lastOrbitDistance) {
        GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        GameObject go = Instantiate(prefab);
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
        go.transform.localPosition = PointOnUnitCircle() * orbitDistance;
    }

    [TargetRpc]
    private void TargetCreateStars(NetworkConnection conn, float distance) {
        starManager.distance = distance + 50000;
        distance = Mathf.Max(100000, distance);
        starManager.CreateStars();
    }

    //[SerializeField]
    public struct PlanetParameters {
        public int planetSettingIndex;
        public int shapeSettingsSeed;
        public float planetRadiusRandom;
        public float orbitDistanceRandom;
        public float lastOrbitDistance;
        public Vector3 rotationSpeed;
    }

    public void AddPlanet(SystemSettings.PlanetSetting planetSetting, ref float lastOrbitDistance) {
        // //Create Planet game object
        // GameObject prefab = Addressables.LoadAssetAsync<GameObject>("Planet").WaitForCompletion();
        // GameObject go = Instantiate(prefab);
        // go.transform.parent = MapManager.Instance.transform.GetChild(0).GetChild(0);
        // //Create Planet body
        // PlanetObject po = go.GetComponent<PlanetObject>();
        // po.autoUpdate = false;
        // ShapeSettings ss = null;
        // po.resolution = planetSetting.resolution;
        // po.colorSettings = planetSetting.colorSettings;
        // //ss = ScriptableObject.CreateInstance<ShapeSettings>();
        // ss = new ShapeSettings(planetSetting.shapeSettings, prng.Next(-100000, 100000));
        // ss.planetRadius = planetSetting.minMaxSize.x + (float) prng.NextDouble() * (planetSetting.minMaxSize.y - planetSetting.minMaxSize.x);
        // lastOrbitDistance += ss.planetRadius + planetSetting.minMaxSize.y + (float) prng.NextDouble() * systemSettings.increasePlanetOrbitDistance;
        // po.shapeSettings = ss;
        // po.CreatePlanet();
        // //Add rotate script
        // RotatePlanet rp = go.GetComponent<RotatePlanet>();
        // rp.rotationSpeed = new Vector3(prng.Next(-5, 5), prng.Next(-5, 5), prng.Next(-5, 5));
        // //Place Planet to orbit of center star
        // go.transform.localPosition = PointOnUnitCircle() * lastOrbitDistance;
        // planets.Add(go);
        // GameManager.Instance.Spawn(go);

        //------------------------------------------------------------------------------

        // //Create Planet game object
        // GameObject go = new GameObject("Planet");
        // go.transform.parent = this.transform.GetChild(0).GetChild(0);

        // //Create Planet body
        // PlanetObject po = go.AddComponent<PlanetObject>();
        // po.autoUpdate = false;
        // ShapeSettings ss = null;
        // po.resolution = planetSetting.resolution;
        // po.colorSettings = planetSetting.colorSettings;
        // //ss = ScriptableObject.CreateInstance<ShapeSettings>();
        // ss = new ShapeSettings(planetSetting.shapeSettings, prng.Next(-100000, 100000));
        // ss.planetRadius = planetSetting.minMaxSize.x + (float) prng.NextDouble() * (planetSetting.minMaxSize.y - planetSetting.minMaxSize.x);
        // lastOrbitDistance += ss.planetRadius + planetSetting.minMaxSize.y + (float) prng.NextDouble() * systemSettings.increasePlanetOrbitDistance;
        // po.shapeSettings = ss;
        // po.CreatePlanet();

        // //Add rotate script
        // RotatePlanet rp = go.AddComponent<RotatePlanet>();
        // rp.rotationSpeed = new Vector3(prng.Next(-5, 5), prng.Next(-5, 5), prng.Next(-5, 5));

        // //Place Planet to orbit of center star
        // go.transform.localPosition = PointOnUnitCircle() * lastOrbitDistance;
        // planets.Add(go);
    }

    public void TryToAddMoon(GameObject go, SystemSettings.PlanetSetting planetSetting, int availableMoonAmount, float distance) {
        if (availableMoonAmount == 0 || prng.NextDouble() > planetSetting.moonCreateChance)
            return;

        //Create Moon game object
        GameObject moon = new GameObject("Moon");
        moon.transform.parent = this.transform.GetChild(0).GetChild(0);

        //Create Moon body
        PlanetObject po = moon.AddComponent<PlanetObject>();
        ShapeSettings ss = new ShapeSettings(planetSetting.moonShapeSettings, prng.Next(-100000, 100000));
        ss.planetRadius = planetSetting.moonMinMaxSize.x + (float) prng.NextDouble() * (planetSetting.moonMinMaxSize.y - planetSetting.moonMinMaxSize.x);
        po.shapeSettings = ss;
        po.colorSettings = planetSetting.moonColorSettings;
        po.autoUpdate = false;
        po.resolution = planetSetting.moonResolution;
        po.CreatePlanet();

        //Place Moon to orbit of Planet
        distance += ss.planetRadius + planetSetting.moonMinMaxSize.y + (float) prng.NextDouble() * systemSettings.increaseMoonOrbitDistance;
        moon.transform.localPosition = go.transform.localPosition + PointOnUnitCircle() * distance;
        planets.Add(moon);

        //Add rotate script
        RotatePlanet rp = moon.AddComponent<RotatePlanet>();
        rp.rotationSpeed = new Vector3((float) prng.NextDouble() * 4 - 2, (float) prng.NextDouble() * 10 - 5, (float) prng.NextDouble() * 4 - 2);

        //Try to add another Moon to Planet
        TryToAddMoon(go, planetSetting, availableMoonAmount - 1, distance);
    }

    public Vector3 PointOnUnitCircle() {
        float angle = (float) prng.NextDouble() * Mathf.PI * 2;
        return Vector3.forward * Mathf.Sin(angle) + Vector3.right * Mathf.Cos(angle);
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
