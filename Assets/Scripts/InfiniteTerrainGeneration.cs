using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainGeneration : MonoBehaviour
{
const float viewerMoveDistForUpdate = 10f;
const float squaredViewerMoveDistForUpdate = viewerMoveDistForUpdate * viewerMoveDistForUpdate;
public LODInfo[] detailStages;
public static float maxViewDistance;
public Transform playerView;
public static Vector2 currentViewPos;
Vector2 oldViewPos;
static MapGenerator mapGenerator;
public Material mapMaterial;
int meshSize;
int meshesVisibleInDistance;

Dictionary<Vector2, TerrainMesh> meshDictionary = new Dictionary<Vector2, TerrainMesh>();
static List<TerrainMesh> meshesToClear = new List<TerrainMesh>();

private void Start() 
{
    mapGenerator = FindObjectOfType<MapGenerator>();
    maxViewDistance = detailStages[detailStages.Length - 1].distanceThreshold;
    meshSize = MapGenerator.terrainMeshSize - 1;
    meshesVisibleInDistance = Mathf.RoundToInt(maxViewDistance / meshSize); 
}

private void Update() {
    currentViewPos = new Vector2(playerView.position.x, playerView.position.z);
    if((oldViewPos - currentViewPos).sqrMagnitude > squaredViewerMoveDistForUpdate)
    {
        oldViewPos = currentViewPos;
        UpdateVisibleMeshes();
    }
}

void UpdateVisibleMeshes()
{
    for (int i = 0; i<meshesToClear.Count; i++)
    {
        meshesToClear[i].SetVisible(false);
    }
    meshesToClear.Clear();

    int currentMeshX = Mathf.RoundToInt(currentViewPos.x / meshSize);
    int currentMeshY = Mathf.RoundToInt(currentViewPos.y / meshSize);
    for (int yOffset = -meshesVisibleInDistance; yOffset <= meshesVisibleInDistance; yOffset++)
    {
        for (int xOffset = -meshesVisibleInDistance; xOffset <= meshesVisibleInDistance; xOffset++)
        {
            Vector2 viewedMeshCoordinates = new Vector2 (currentMeshX + xOffset, currentMeshY + yOffset);

            if (meshDictionary.ContainsKey(viewedMeshCoordinates))
            {
                meshDictionary[viewedMeshCoordinates].UpdateMesh();

            }
            else
            {
                meshDictionary.Add(viewedMeshCoordinates, new TerrainMesh(viewedMeshCoordinates, meshSize, detailStages, transform, mapMaterial));
            }
        }
    }
}

public class TerrainMesh {
    GameObject meshObject;
    Vector2 position;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;
    LODInfo[] detailStages;
    LODMesh[] lodMeshes;
    LODMesh collisionMesh;
    MapData terrainData;
    bool terrainDataReceived;
    int previousLODIndex = -1;
    
    public TerrainMesh(Vector2 coordinates, int size, LODInfo[] detailStages, Transform parent, Material material) {
        {
            this.detailStages = detailStages;
            position = coordinates * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionVector3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Mesh");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionVector3;
            meshObject.transform.parent = parent;
            SetVisible(false);

            lodMeshes = new LODMesh[detailStages.Length];
            for (int i=0; i<detailStages.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailStages[i].lod, UpdateMesh);
                if(detailStages[i].useCollision)
                {
                    collisionMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestTerrainData(position, OnTerrainDataReceived);
        }
    }

    void OnTerrainDataReceived(MapData terrainData)
    {
        float startTime = Time.realtimeSinceStartup;

        this.terrainData = terrainData;
        terrainDataReceived = true;

        Texture2D texture = TextureGenerator.TextureFromColourMap(terrainData.colourMap, MapGenerator.terrainMeshSize, MapGenerator.terrainMeshSize);
        meshRenderer.material.mainTexture = texture;

        UpdateMesh();

        float endTime = Time.realtimeSinceStartup;
        Debug.Log("Time taken to GENERATE MAP after DATA RECEIVED: " + (endTime - startTime));
    }

    public void UpdateMesh()
    {
        float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(currentViewPos));
        bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;
        if (visible)
        {
            int lodIndex=0;
            for(int i=0; i<detailStages.Length-1; i++)
            {
                if(viewerDistanceFromNearestEdge > detailStages[i].distanceThreshold)
                {
                    lodIndex = i + 1;
                }
                else
                {
                    break;
                }
            }
            if(lodIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if(lodMesh.hasMesh)
                {
                    previousLODIndex = lodIndex;
                    meshFilter.mesh = lodMesh.mesh;
                }
                else if(!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(terrainData);
                }
            }
            if(lodIndex == 0)
            {
                if(collisionMesh.hasMesh)
                {
                    meshCollider.sharedMesh = collisionMesh.mesh;
                }
                else if(!collisionMesh.hasRequestedMesh)
                {
                    collisionMesh.RequestMesh(terrainData);
                }
            }

            meshesToClear.Add(this);
        }

        SetVisible(visible);
    }
    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }
    public bool IsVisible()
    {
        return meshObject.activeSelf;
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    System.Action updateCallback;

    public LODMesh(int lod, System.Action updateCallback)
    {
        this.lod = lod;
        this.updateCallback = updateCallback;
    }

    void OnDataReceived(MeshData meshData)
    {
        mesh = meshData.CreateMesh();
        hasMesh = true;
        updateCallback();
    }
    public void RequestMesh(MapData mapData)
    {
        hasRequestedMesh = true;
        mapGenerator.RequestMeshData(mapData, lod, OnDataReceived);
    }

}
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float distanceThreshold;
        public bool useCollision;
    }
}
