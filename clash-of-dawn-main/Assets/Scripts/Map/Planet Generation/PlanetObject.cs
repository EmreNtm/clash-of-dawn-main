using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetObject : MonoBehaviour
{

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    [HideInInspector]
    public SystemSettings.PlanetSetting planetSetting;
    [HideInInspector]
    public bool isMoon = false;
    [HideInInspector]
    public GameObject parentPlanet;

    public float orbitDistance;
    [HideInInspector]
    public List<GameObject> moons = new();
    [HideInInspector]
    public bool serverMoonListFlag = false;

    [Range(2, 128)]
    public int resolution = 5;
    public bool autoUpdate = false;
    public enum RenderOptions {
        All,
        Top
    };
    public RenderOptions renderOptions;

    private MeshFilter[] meshFilters;

    private float minHeight;
    private float maxHeight;
    private Texture2D texture;
    private const int textureResolution = 50;

    private Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    private void OnValidate() {
        if (!autoUpdate)
            return;

        CreatePlanet();
    }

    public void CreatePlanet() {
        if (shapeSettings.noiseData.noiseLayers == null || shapeSettings.noiseData.noiseLayers.Length == 0)
            return;

        InitializePlanet();
        UpdatePlanetShape();
        UpdatePlanetColor();
    }

    private void InitializePlanet() {
        if (meshFilters == null || meshFilters.Length == 0) {
            meshFilters = new MeshFilter[6];
        }

        for (int i = 0; i < 6; i++) {
            if (meshFilters[i] == null) {
                GameObject meshObj = new GameObject("TerrainFace");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();
            }
            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMaterial;
            meshFilters[i].gameObject.SetActive(renderOptions == RenderOptions.All ||  (int) renderOptions - 1 == i);
        }

        SphereCollider spherec = this.gameObject.AddComponent<SphereCollider>();

        shapeSettings.noiseData.noiseConfigs = new Noise.NoiseConfig[shapeSettings.noiseData.noiseLayers.Length];
        for (int i = 0; i < shapeSettings.noiseData.noiseConfigs.Length; i++) {
            shapeSettings.noiseData.noiseConfigs[i] = new Noise.NoiseConfig(shapeSettings.noiseData.noiseLayers[i]);
        }
    }

    private void UpdatePlanetShape() {
        if (shapeSettings.noiseData.noiseLayers == null || shapeSettings.noiseData.noiseLayers.Length == 0)
            return;

        minHeight = float.MaxValue;
        maxHeight = float.MinValue;

        int i = 0;
        foreach (MeshFilter mf in meshFilters) {
            SetMeshData(mf.sharedMesh, resolution, directions[i++], shapeSettings);
        }

        this.gameObject.GetComponent<SphereCollider>().radius = shapeSettings.planetRadius + maxHeight;

        //Give planetMat min & max height
        UpdatePlanetMaterial();
    }

    private void UpdatePlanetMaterial() {
        colorSettings.planetMaterial.SetVector("_heightMinMax", new Vector4(minHeight, maxHeight));
    }
    
    private void UpdatePlanetColor() {
        // foreach (MeshFilter mf in meshFilters) {
        //     mf.GetComponent<MeshRenderer>().sharedMaterial.color = colorSettings.planetColor;
        // }
        UpdatePlanetTexture();
        int i = 0;
        foreach (MeshFilter mf in meshFilters) {
            UpdateMeshUV(mf.sharedMesh, directions[i++]);
        }
    }

    private void UpdatePlanetTexture() {
        //if (texture == null || texture.height != colorSettings.biomeSettings.biomes.Length)
            texture = new Texture2D(textureResolution * 2, colorSettings.biomeSettings.biomes.Length, TextureFormat.RGBA32, false);

        Color[] colors = new Color[texture.width * texture.height];
        int i = 0;
        foreach (ColorSettings.BiomeSettings.Biome biome in colorSettings.biomeSettings.biomes) {
            for (int j = 0; j < textureResolution * 2; j++) {
                Color gradient;
                if (j < textureResolution) {
                    gradient = colorSettings.oceanColor.Evaluate(j / (textureResolution - 1f));
                } else {
                    gradient = biome.gradient.Evaluate((j - textureResolution) / (textureResolution - 1f));
                }
                Color tint = biome.tint;
                colors[i] = gradient * (1 - biome.tintPercentage) + tint * biome.tintPercentage;
                i++;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        colorSettings.planetMaterial.SetTexture("_texture", texture);
    }

    public void OnPlanetShapeUpdate() {
        if (!autoUpdate)
            return;

        InitializePlanet();
        UpdatePlanetShape();
    }

    public void OnPlanetColorUpdate() {
        if (!autoUpdate)
            return;

        InitializePlanet();
        UpdatePlanetColor();
    }

    private void SetMeshData(Mesh mesh, int resolution, Vector3 _direction, ShapeSettings shapeSettings) {
        Vector3 direction = _direction.normalized;
        Vector3 firstAxis = new Vector3(direction.y, direction.z, direction.x);
        Vector3 secondAxis = Vector3.Cross(firstAxis, direction);
        Vector3[] vertices = new Vector3[resolution * resolution];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uv = mesh.uv.Length == vertices.Length ? mesh.uv : new Vector2[vertices.Length];
        int[] triangles = new int[((resolution - 1) * (resolution - 1) * 2) * 3];

        int index, triangleCount = 0;
        Vector3 startPos = direction;
        Vector2 percent;
        Vector3 vertice;
        float height;
        for (int i = 0; i < resolution; i++) {
            for (int j = 0; j < resolution; j++) {
                index = i * resolution + j;
                percent = new Vector2(i, j) / (resolution - 1);
                vertice = startPos + (percent.x - 0.5f) * 2 * firstAxis + (percent.y - 0.5f) * 2 * secondAxis;
                height = GetPlanetHeight(vertice);
                normals[index] = vertice.normalized;
                vertices[index] = this.GetPlanetVertice(vertice.normalized, height);
                uv[index].y = height;

                if (i != resolution - 1 && j != resolution - 1) {
                    triangles[triangleCount] = index;
                    triangles[triangleCount + 1] = index + 1;
                    triangles[triangleCount + 2] = index + 1 + resolution;

                    triangles[triangleCount + 3] = index;
                    triangles[triangleCount + 4] = index + 1 + resolution;
                    triangles[triangleCount + 5] = index + resolution;

                    triangleCount += 6;
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uv;
    }

    private float GetPlanetHeight(Vector3 loc) {
        float radius = shapeSettings.planetRadius;

        float mask = 0;
        float height = 0;
        if (shapeSettings.noiseData.noiseLayers.Length > 0) {
            mask = Noise.GetNoiseAt(loc, shapeSettings.noiseData.noiseConfigs[0]);
            if (shapeSettings.noiseData.noiseLayers[0].isEnabled)
                height += mask;
        }

        for (int i = 1; i < shapeSettings.noiseData.noiseConfigs.Length; i++) {
            if (shapeSettings.noiseData.noiseLayers[i].isEnabled) {
                mask = shapeSettings.noiseData.noiseLayers[i].isUsingMask ? mask : 1;
                height += Noise.GetNoiseAt(loc, shapeSettings.noiseData.noiseConfigs[i]) * mask;
            }
        }

        if (height > maxHeight) {
            maxHeight = height;
        } else if (height < minHeight) {
            minHeight = height;
        }
        //height = Mathf.Max(radius, height);
        return height;
    }

    private Vector3 GetPlanetVertice(Vector3 vertice, float height) {
        height = Mathf.Max(0, height);
        height = shapeSettings.planetRadius * (1 + height);
        return vertice.normalized * height;
    }

    private void UpdateMeshUV(Mesh mesh, Vector3 _direction) {
        Vector2[] uv = mesh.uv;
        Vector3 direction = _direction.normalized;
        Vector3 firstAxis = new Vector3(direction.y, direction.z, direction.x);
        Vector3 secondAxis = Vector3.Cross(firstAxis, direction);
        int index = 0;
        Vector3 startPos = direction;
        Vector2 percent;
        Vector3 vertice;
        for (int i = 0; i < resolution; i++) {
            for (int j = 0; j < resolution; j++) {
                index = i * resolution + j;
                percent = new Vector2(i, j) / (resolution - 1);
                vertice = startPos + (percent.x - 0.5f) * 2 * firstAxis + (percent.y - 0.5f) * 2 * secondAxis;

                uv[index].x = GetBiomePos(vertice.normalized);
            }
        }
        mesh.uv = uv;
    }

    private float GetBiomePos(Vector3 normalisedVertex) {
        float vertexPos = (normalisedVertex.y + 1) / 2f;
        vertexPos += (Noise.GetNoiseAt(normalisedVertex, colorSettings.biomeSettings.biomeBlendNoiseConfig) - colorSettings.biomeSettings.noiseOffset)
                        * colorSettings.biomeSettings.noiseStrength;
        float biomePos = 0;
        int biomeAmount = colorSettings.biomeSettings.biomes.Length;
        if (biomeAmount == 1)
            return 0;

        float blendRange = colorSettings.biomeSettings.blendAmount / 2f + 0.001f;

        float distance, weight;
        for (int i = 0; i < biomeAmount; i++) {
            distance = vertexPos - colorSettings.biomeSettings.biomes[i].startPos;
            weight = Mathf.InverseLerp(-blendRange, blendRange, distance);
            biomePos *= (1 - weight);
            biomePos += i * weight;
        }

        return biomePos / (biomeAmount - 1);
    }

}
