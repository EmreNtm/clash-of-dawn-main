using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TerrainFace: Sets mesh data for one side of a sphere cube.
public class TerrainFace
{

    private Mesh mesh;
    private int resolution;
    private Vector3 direction;
    private Vector3 firstAxis;
    private Vector3 secondAxis;

    public TerrainFace(Mesh mesh, int resolution, Vector3 direction) {
        this.mesh = mesh;
        this.resolution = resolution;
        this.direction = direction;

        this.direction = this.direction.normalized;
        this.firstAxis = new Vector3(this.direction.y, this.direction.z, this.direction.x);
        this.secondAxis = Vector3.Cross(firstAxis, this.direction);
    }

    public void SetMeshData() {
        Vector3[] vertices = new Vector3[resolution * resolution];
        int[] triangles = new int[((resolution - 1) * (resolution - 1) * 2) * 3];

        int index, triangleCount = 0;
        Vector3 startPos = direction;
        Vector2 percent;
        Vector3 vertice;
        for (int i = 0; i < resolution; i++) {
            for (int j = 0; j < resolution; j++) {
                index = i * resolution + j;
                percent = new Vector2(i, j) / (resolution - 1);
                vertice = startPos + (percent.x - 0.5f) * 2 * firstAxis + (percent.y - 0.5f) * 2 * secondAxis;
                vertices[index] = vertice.normalized;

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
        mesh.RecalculateNormals();
    }

}
