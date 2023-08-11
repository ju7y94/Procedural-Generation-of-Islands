using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum GenerationMode {NoiseMap, ColourMap, Mesh, Falloff};
    public GenerationMode generationMode;
    public const int terrainMeshSize = 241;
    [Range(0,6)]
    public int levelOfDetailEditor;
    public int seed;
    public float noiseScale;
    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public Vector2 offset;
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightAnimationCurve;
    public bool autoUpdate;
    public bool useFalloff;
    float[,] falloffMap;
    public TerrainType[] terrainTypes;
    MeshCollider meshCollider;
    MeshFilter meshFilter;

    Queue<TerrainThreadInfo<MapData>> mapDataThreadInfo = new Queue<TerrainThreadInfo<MapData>>();
    Queue<TerrainThreadInfo<MeshData>> meshDataThreadInfo = new Queue<TerrainThreadInfo<MeshData>>();

    private void Awake() {
        float startTime = Time.realtimeSinceStartup;

        seed = MainMenuScene.mainMenuScene.seedNumber;
        falloffMap = FalloffMapGenerator.GenerateFalloffMap(terrainMeshSize);
        meshFilter = GetComponent<MapDisplay>().meshFilter;
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = meshFilter.mesh;

        float endTime = Time.realtimeSinceStartup;
        print("Time taken for map generation: " + (endTime - startTime));
    }

    

    private void Start() {
        meshCollider.enabled = !meshCollider.enabled;
    }

    MapData GenerateData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(terrainMeshSize, terrainMeshSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset);

        Color[] colourMap = new Color[terrainMeshSize * terrainMeshSize];
        for (int y = 0; y < terrainMeshSize; y++)
        {
            for (int x = 0; x < terrainMeshSize; x++)
            {
                if(useFalloff)
                {
                    noiseMap[x,y] = Mathf.Clamp01(noiseMap[x,y] - falloffMap[x,y]);
                }
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < terrainTypes.Length; i++)
                {
                    if (currentHeight <= terrainTypes[i].height) {
                        colourMap[y * terrainMeshSize + x] = terrainTypes[i].colour;
                        break;
                    }
                }
            }
        }
        return new MapData(noiseMap, colourMap);
    }

    private void SplitMeshIntoChunks(int chunkSize)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>(mesh.vertices);
        List<int> triangles = new List<int>(mesh.triangles);

        for (int i = 0; i < vertices.Count; i += chunkSize)
        {
            GameObject chunk = new GameObject("Chunk " + i);
            chunk.transform.parent = transform;
            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();

            Mesh newMesh = new Mesh();
            newMesh.vertices = vertices.GetRange(i, Mathf.Min(chunkSize, vertices.Count - i)).ToArray();
            newMesh.triangles = triangles.GetRange(i, Mathf.Min(chunkSize * 3, triangles.Count - i)).ToArray();
            meshFilter.mesh = newMesh;
        }
    }

    private void GenerateLODLevels(float[] lodScreenRelativeHeights)
    {
        LOD[] lods = new LOD[lodScreenRelativeHeights.Length];

        for (int i = 0; i < lodScreenRelativeHeights.Length; i++)
        {
            LOD lod = new LOD();
            lod.renderers = transform.GetChild(i).GetComponents<Renderer>();
            lod.screenRelativeTransitionHeight = lodScreenRelativeHeights[i];
            lods[i] = lod;
        }

        LODGroup lodGroup = GetComponent<LODGroup>();
        lodGroup.SetLODs(lods);
        lodGroup.RecalculateBounds();
    }

    public void DrawMapEditor()
    {
        MapData mapData = GenerateData(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (generationMode == GenerationMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (generationMode == GenerationMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, terrainMeshSize, terrainMeshSize));
        }
        else if (generationMode == GenerationMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightAnimationCurve, levelOfDetailEditor), TextureGenerator.TextureFromColourMap(mapData.colourMap, terrainMeshSize, terrainMeshSize));
        }
        else if (generationMode == GenerationMode.Falloff)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffMapGenerator.GenerateFalloffMap(terrainMeshSize)));
        }
    }

    public void RequestTerrainData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            TerrainDataThread(centre, callback);
        };
        
        new Thread(threadStart).Start();
    }
    void TerrainDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateData(centre);
        lock (mapDataThreadInfo)
        {
            mapDataThreadInfo.Enqueue(new TerrainThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback) {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, levelOfDetail, callback);
        };
        new Thread(threadStart).Start();
    }
    void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightAnimationCurve, levelOfDetail);
        lock (meshDataThreadInfo)
        {
            meshDataThreadInfo.Enqueue(new TerrainThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update() {
        if (mapDataThreadInfo.Count > 0)
        {
            for (int i=0; i< mapDataThreadInfo.Count; i++)
            {
                TerrainThreadInfo<MapData> threadInfo = mapDataThreadInfo.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfo.Count > 0)
        {
            for (int i=0; i< meshDataThreadInfo.Count; i++)
            {
                TerrainThreadInfo<MeshData> threadInfo = meshDataThreadInfo.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    private void OnValidate() {
        falloffMap = FalloffMapGenerator.GenerateFalloffMap(terrainMeshSize);

        if (octaves < 0)
        {
            octaves = 0;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }
    }
}

    struct TerrainThreadInfo<T> 
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public TerrainThreadInfo (Action<T> callback , T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }

[System.Serializable]
public struct TerrainType 
{
    public string name;
    public float height;
    public Color colour;
    public Texture texture;
}
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData (float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
