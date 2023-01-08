using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class SystemSettings : ScriptableObject
{
    
    public int seed;
    public float scale;
    public float borderRadius;
    public float increasePlanetOrbitDistance;
    public float increaseMoonOrbitDistance;
    
    [System.Serializable]
    public struct PlanetSetting {
        public MapManager.PlanetType type;

        [Range(0f, 1f)]
        public float stationCreateChance;
        public float stationDistance;

        public ShapeSettings shapeSettings;
        public ColorSettings colorSettings;

        [Range(2, 256)]
        public int resolution;
        public float borderRadius;
        public Vector2 minMaxSize;
        public Vector2 minMaxAmount;

        public ShapeSettings moonShapeSettings;
        public ColorSettings moonColorSettings;

        public int maxMoonAmount;
        [Range(0, 1)]
        public float moonCreateChance;
        public Vector2 moonMinMaxSize;
        [Range(2, 256)]
        public int moonResolution;
    }
    public PlanetSetting[] planetSettings;

}
