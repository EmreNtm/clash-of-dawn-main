using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(),]
public class ColorSettings : ScriptableObject
{
    public Material planetMaterial;
    public Gradient oceanColor;
    public BiomeSettings biomeSettings;

    [System.Serializable]
    public class BiomeSettings {

        public Biome[] biomes;
        public Noise.NoiseConfig biomeBlendNoiseConfig;
        public float noiseOffset;
        public float noiseStrength;
        [Range(0, 1)]
        public float blendAmount;

        [System.Serializable]
        public class Biome {
            public string name;
            public Gradient gradient;
            public Color tint;

            [Range(0, 1)]
            public float tintPercentage;

            [Range(0, 1)]
            public float startPos;
        }
    }
}
