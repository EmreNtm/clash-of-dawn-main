using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarManager : MonoBehaviour
{

    public int starAmount;
    public int starVerticeAmount;
    public float minSize;
    public float maxSize;
    public float minBrightness;
    public float maxBrightness = 1;
    public float distance = 10;
    public Material mat;
    private Mesh mesh;

    public Gradient color;
    private Texture2D spectrum;
    
    // Start is called before the first frame update
    void Start()
    {
        // GenerateMesh();
        // TextureFromGradient(color, 64, ref spectrum);
        // mat.SetTexture("_Spectrum", spectrum);
    }

    private void FixedUpdate() {
        if (PlayerData.Instance == null || PlayerData.Instance.playerShip == null)
            return;

        transform.position = PlayerData.Instance.playerShip.transform.position;
    }

    public void CreateStars() {
        GenerateMesh();
        TextureFromGradient(color, 64, ref spectrum);
        mat.SetTexture("_Spectrum", spectrum);
    }

    private void TextureFromGradient (Gradient gradient, int width, ref Texture2D texture) {
		if (texture == null || texture.width != width || texture.height != 1) {
			texture = new Texture2D (width, 1);
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;
		}

		Color[] colours = new Color[width];
		for (int i = 0; i < width; i++) {
			float t = i / (width - 1f);
			colours[i] = gradient.Evaluate (t);
		}
		texture.SetPixels (colours);
		texture.Apply ();
	}

    private void GenerateMesh() {
        if (mesh != null) {
            mesh.Clear();
        }

        mesh = new Mesh();
        List<int> triangles = new();
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();

        Vector3 dir;
        for (int i = 0; i < starAmount; i++) {
            dir = OnUnitSphere(MapManager.prng);
            var (circleVertices, circleTriangles, circleUVs) = GenerateCircle(dir, vertices.Count);
            vertices.AddRange(circleVertices);
            triangles.AddRange(circleTriangles);
            uvs.AddRange(circleUVs);
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0 , true);
        mesh.SetUVs(0, uvs);
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        meshRenderer.sharedMaterial = mat;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    private (Vector3[], int[], Vector2[]) GenerateCircle(Vector3 dir, int offset) {
        float size = minSize + (float) MapManager.prng.NextDouble() * (maxSize - minSize);
        float brightness = minBrightness + (float) MapManager.prng.NextDouble() * (maxBrightness - minBrightness);
        float colorPicker = (float) MapManager.prng.NextDouble();
        Vector3 starPos = dir * distance;

        Vector3 axisA = Vector3.Cross(dir, Vector3.up).normalized;
		if (axisA == Vector3.zero) {
			axisA = Vector3.Cross(dir, Vector3.forward).normalized;
		}
		Vector3 axisB = Vector3.Cross(dir, axisA);

        Vector3[] vertices = new Vector3[starVerticeAmount + 1];
        int[] triangles = new int[starVerticeAmount * 3];
        Vector2[] uvs = new Vector2[starVerticeAmount + 1];

        vertices[0] = starPos;
        uvs[0] = new Vector2(brightness, colorPicker);
        for (int i = 0; i < starVerticeAmount; i++) {
            float angle = (i / (float) starVerticeAmount) * Mathf.PI * 2;
            Vector3 vertice = starPos + (axisA * Mathf.Sin(angle) + axisB * Mathf.Cos(angle)) * size;

            vertices[i + 1] = vertice;
            uvs[i + 1] = new Vector2(0, colorPicker);
            triangles[i * 3 + 0] = offset;
            triangles[i * 3 + 1] = i + 1 + offset;
            triangles[i * 3 + 2] = (i + 1) % starVerticeAmount + 1 + offset;
        }

        return (vertices, triangles, uvs);
    }

    private Vector3 OnUnitSphere(System.Random prng) {
        return new Vector3((float) prng.NextDouble() * 2f - 1f + 0.0001f, (float) prng.NextDouble() * 2f - 1f, (float) prng.NextDouble() * 2f - 1f).normalized;
    }
}
