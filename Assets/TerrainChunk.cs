using UnityEngine;
using System.Threading;

public class TerrainChunk {

    /* Coordinates of the chunk */
    public int X;
    public int Z;

    /* The terrain of the chunk */
    private Terrain Terrain;
    private TerrainChunkSettings Settings;
    private float[,] heightMap;
    private TerrainData terrainData;

    /* Use this lock to indicate whether this object is creating it's heightmap */
    private object heightMapThreadLock;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public TerrainChunk(TerrainChunkSettings settings, Vector2 key) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        heightMapThreadLock = new object();
        Settings = settings;
        X = (int) key.x;
        Z = (int) key.y;
    }

    public TerrainChunk(TerrainChunkSettings settings, int x, int z) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        heightMapThreadLock = new object();
        Settings = settings;
        X = x;
        Z = z;
    }

    /* ----------- Heightmap Functions ------------------------------------------------------------- */
    
    public void GenerateHeightMapRequest() {
        /*
         * Start the thread that generates the heightmap
         */

        Thread thread = new Thread(GenerateHeightMapThread);
        thread.Start();
    }

    private void GenerateHeightMapThread() {
        /*
         * Generate the heightMap for this terrainChunk, but have it done within a lock.
         */

        /* Lock the thread until it fully generates the heightmap */
        lock(heightMapThreadLock) {
            GenerateHeightMap();
        }
    }

    public void GenerateHeightMap() {
        /*
         * Generate the heightMap for this terrainChunk
         */
         
        /* Populate the heightMap by going through it's resolution */
        float[,] newHeightMap = new float[Settings.HeightmapResolution, Settings.HeightmapResolution];
        for(int z = 0; z < Settings.HeightmapResolution; z++) {
            for(int x = 0; x < Settings.HeightmapResolution; x++) {
                float xCoord = X + (float) x / (Settings.HeightmapResolution - 1);
                float zCoord = Z + (float) z / (Settings.HeightmapResolution - 1);
                newHeightMap[z, x] = Mathf.PerlinNoise(xCoord, zCoord);
            }
        }

        heightMap = newHeightMap;
    }

    public bool IsHeightmapReady() {
        /*
         * Return true if the heightmap is fully generated and the terrain has not yet been generated
         */

        return (Terrain == null && heightMap != null);
    }


    /* ----------- Update Functions ------------------------------------------------------------- */

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


    /* ----------- Helper Functions ------------------------------------------------------------- */

    public Vector2 GetChunkCoordinates() {
        /*
         * Return the coordinates of this chunk. This serves as it's key in the chunk dicitionaries
         */

        return new Vector2(X, Z);
    }
}
