using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightAnimationCurve, int levelOfDetail)
    {
        AnimationCurve nHeightCurve = new AnimationCurve(heightAnimationCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;
        
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement +1;
        

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y=0; y<height; y += meshSimplificationIncrement)
        {
            for (int x=0; x<width; x += meshSimplificationIncrement, vertexIndex++)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, nHeightCurve.Evaluate(heightMap[x,y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);

                if (x < width -1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                
            }
        }
        return meshData;
    }
}
public class MeshData
{
    public Vector3[] vertices;
    int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;
    
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }
    public void AddTriangle(int firstIndex, int secondIndex, int thirdIndex) 
    {
        triangles[triangleIndex] = firstIndex;
        triangles[triangleIndex + 1] = secondIndex;
        triangles[triangleIndex + 2] = thirdIndex;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        float startTime = Time.realtimeSinceStartup;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        float endTime = Time.realtimeSinceStartup;
        Debug.Log("Time taken to GENERATE MESH: " + (endTime-startTime));

        return mesh;
    }
}
