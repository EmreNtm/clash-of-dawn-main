using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum FilterMode {
        Simple,
        Ridged
    };

    public static float GetNoiseAt(Vector3 location, NoiseConfig nc) {
        nc.InitializeOffsets();

        float x = location.x;
        float y = location.y;
        float z = location.z;

        int octaves = nc.octaves;
        float persistance = nc.persistance;
        float lacunarity = nc.lacunarity;
        float heightMultiplier = nc.heightMultiplier;
        float scale = nc.scale;
        Vector3[] octaveOffsets = nc.octaveOffsets;
        float noiseSubValue = nc.noiseSubValue;
        FilterMode filterMode = nc.filterMode;

        float perlinValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        float sampleX;
        float sampleY;
        float sampleZ;
        float noise;
        float weight = 1;

        for(int i = 0; i < octaves; i++) {
            sampleX = x / scale * frequency + octaveOffsets[i].x;
            sampleY = y / scale * frequency + octaveOffsets[i].y;
            sampleZ = z / scale * frequency + octaveOffsets[i].z;
           
            // Get the perlin value at that octave, filter it and add it to the sum
            noise = Perlin.Noise(sampleX, sampleY, sampleZ);

            // filter
            if (filterMode == FilterMode.Ridged) {
                noise = 1 - Mathf.Abs(noise);
                //noise = noise * 2 - 1;
                noise *= noise;
                noise *= weight;
                weight = noise;
            } else if (filterMode == FilterMode.Simple) {
                noise = (noise + 1) / 2;
            }

            // add
            perlinValue += noise * amplitude;
            
            // Decrease the amplitude and the frequency
            amplitude *= persistance;
            frequency *= lacunarity;
        }
        
        
        perlinValue = perlinValue - noiseSubValue;

        // Return the noise value
        return perlinValue * heightMultiplier;
    }

    [System.Serializable]
    public class NoiseConfig {
        public int octaves = 1;
        public float persistance;
        public float lacunarity;
        public float heightMultiplier;
        public float scale;
        public int seed;
        public Vector3 offset;
        [HideInInspector]
        public Vector3[] octaveOffsets;
        public float noiseSubValue;
        public FilterMode filterMode;

        public NoiseConfig(ShapeSettings.NoiseLayer nl) {
            this.octaves = nl.octaves;
            this.persistance = nl.persistance;
            this.lacunarity = nl.lacunarity;
            this.heightMultiplier = nl.heightMultiplier;
            this.scale = nl.scale;
            this.seed = nl.seed;
            this.offset = nl.offset;
            this.noiseSubValue = nl.noiseSubValue;
            this.filterMode = nl.filterMode;
        }

        public void InitializeOffsets() {
            System.Random prng = new System.Random(seed);
            this.octaveOffsets = new Vector3[octaves];
            float frequency = 1f;
            for (int i = 0; i < octaves; i++) {
                float offsetX = prng.Next(-100000, 100000) + offset.x * frequency;
                float offsetY = prng.Next(-100000, 100000) + offset.y * frequency;
                float offsetZ = prng.Next(-100000, 100000) + offset.z * frequency;
                octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
                frequency *= lacunarity;
            }

            if (scale <= 0) {
                scale = 0.0001f;
            }
        }
    }

}
