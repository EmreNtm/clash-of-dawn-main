using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(), SerializeField]
public class ShapeSettings : ScriptableObject
{
    public float planetRadius = 1f;
    public NoiseData noiseData;

    public ShapeSettings(ShapeSettings defaultSettings, int seed) {
        this.planetRadius = defaultSettings.planetRadius;
        this.noiseData = defaultSettings.noiseData;

        for (int i = 0; i < noiseData.noiseLayers.Length; i++) {
            noiseData.noiseLayers[i].seed = seed;
        }
    }

    [System.Serializable]
    public struct NoiseData {
        public NoiseLayer[] noiseLayers;
        [HideInInspector]
        public Noise.NoiseConfig[] noiseConfigs;
    }

    [System.Serializable]
    public struct NoiseLayer {
        public bool isEnabled;
        public bool isUsingMask;
        public Noise.FilterMode filterMode;

        [Range(1, 8)]
        public int octaves;
        public float persistance;
        public float lacunarity;
        public float heightMultiplier;
        public float scale;
        public int seed;
        public Vector3 offset;
        public float noiseSubValue;
    }
}
