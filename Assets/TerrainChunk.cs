using UnityEngine;
using System.Collections;

public class TerrainChunk {

    /* Coordinates of the chunk */
    public int X;
    public int Z;

    /* The terrain of the chunk */
    private Terrain Terrain;
    private TerrainChunkSettings Settings;
    private float[,] heightMap;
    private TerrainData terrainData;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public TerrainChunk(TerrainChunkSettings settings, Vector2 key) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        Settings = settings;
        X = (int) key.x;
        Z = (int) key.y;
        GenerateHeightMap();
    }

    public TerrainChunk(TerrainChunkSettings settings, int x, int z) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        Settings = settings;
        X = x;
        Z = z;
        GenerateHeightMap();
    }
    
    /* ----------- Update Functions ------------------------------------------------------------- */

    public void GenerateHeightMap() {
        /*
         * Generate the heightMap for this terrainChunk
         */

        float[,] newHeightMap = new float[Settings.HeightmapResolution, Settings.HeightmapResolution];

        /* Populate the heightMap by going through it's resolution */
        for(int z = 0; z < Settings.HeightmapResolution; z++) {
            for(int x = 0; x < Settings.HeightmapResolution; x++) {
                float xCoord = X + (float) x / (Settings.HeightmapResolution - 1);
                float zCoord = Z + (float) z / (Settings.HeightmapResolution - 1);
                newHeightMap[z, x] = Mathf.PerlinNoise(xCoord, zCoord);
            }
        }

        heightMap = newHeightMap;
    }
    
    public void SetChunkCoordinates(int x, int z) {
        /*
         * Set the coordinates of where the chunk is placed in the noise function
         */

        X = x;
        Z = z;
    }
    
    public void CreateTerrain() {
        /*
         * Create the terrain and the GameObject to hold it
         */

        /* Create the terrainData that is used to define the terrain to create */
        terrainData = new TerrainData();
        terrainData.heightmapResolution = Settings.HeightmapResolution;
        terrainData.alphamapResolution = Settings.AlphamapResolution;
        terrainData.SetHeights(0, 0, heightMap);
        terrainData.size = new Vector3(Settings.Length, Settings.Height, Settings.Length);

        /* Create the object that will contain the terrain components */
        GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
        newTerrainGameObject.transform.position = new Vector3(X * Settings.Length, 0, Z * Settings.Length);
        newTerrainGameObject.transform.parent = Settings.chunkContainer;
        newTerrainGameObject.transform.name = "[" + X + ", " + Z + "]";
        Terrain = newTerrainGameObject.GetComponent<Terrain>();
        Terrain.Flush();
    }

    public void Remove() {
        /*
         * Delete this chunk of terrain
         */

        Settings = null;
        if(Terrain != null) {
            GameObject.Destroy(Terrain.gameObject);
        }
    }

    public void GenerateTerrain(int x, int z) {
        /*
         * Generate terrain with the given settings. 
         */

        /* Set the coordinates of this chunk */
        SetChunkCoordinates(x, z);

        /* Generate the terrain */
        CreateTerrain();
    }
}
