using UnityEngine;
using System.Collections;

public class TerrainChunk {

    /* Coordinates of the chunk */
    public int X;
    public int Z;

    /* The terrain of the chunk */
    private Terrain Terrain;
    private TerrainChunkSettings Settings;

    public TerrainChunk(TerrainChunkSettings settings, Vector2 key) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        Settings = settings;
        X = (int) key.x;
        Z = (int) key.y;
    }

    public TerrainChunk(TerrainChunkSettings settings, int x, int z) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        Settings = settings;
        X = x;
        Z = z;
    }

    public void SetChunkCoordinates(int x, int z) {
        /*
         * Set the coordinates of where the chunk is placed in the noise function
         */

        X = x;
        Z = z;
    }

    public void LinkSettings(TerrainChunkSettings newSettings) {
        /*
         * Set the new terrainChunkSettings to the given script
         */

        Settings = newSettings;
    }

    public void CreateTerrain() {
        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = Settings.HeightmapResolution;
        terrainData.alphamapResolution = Settings.AlphamapResolution;

        float[,] heightmap = GetHeightmap();
        terrainData.SetHeights(0, 0, heightmap);
        terrainData.size = new Vector3(Settings.Length, Settings.Height, Settings.Length);

        GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
        newTerrainGameObject.transform.position = new Vector3(X * Settings.Length, 0, Z * Settings.Length);
        Terrain = newTerrainGameObject.GetComponent<Terrain>();
        Terrain.Flush();

        /* Make the terrain a child to this object, changing it's name to reflect it's coordinates */
        newTerrainGameObject.transform.parent = Settings.chunkContainer;
        newTerrainGameObject.transform.name = "[" + X + ", " + Z + "]";
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

    private float[,] GetHeightmap() {
        var heightmap = new float[Settings.HeightmapResolution, Settings.HeightmapResolution];

        for(var zRes = 0; zRes < Settings.HeightmapResolution; zRes++) {
            for(var xRes = 0; xRes < Settings.HeightmapResolution; xRes++) {
                var xCoordinate = X + (float) xRes / (Settings.HeightmapResolution - 1);
                var zCoordinate = Z + (float) zRes / (Settings.HeightmapResolution - 1);

                heightmap[zRes, xRes] = Mathf.PerlinNoise(xCoordinate, zCoordinate);
            }
        }

        return heightmap;
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
