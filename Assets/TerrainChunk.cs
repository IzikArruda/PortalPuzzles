using UnityEngine;
using System.Collections;

public class TerrainChunk : MonoBehaviour {

    public int X { get; private set; }

    public int Z { get; private set; }

    private Terrain Terrain { get; set; }

    private TerrainChunkSettings Settings { get; set; }
    
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
        newTerrainGameObject.transform.parent = transform;
        transform.name = "[" + X + ", " + Z + "]";
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

        /* Update the settings of the chunk and it's coordinates in the noise function */
        Settings.SetSettings(129, 100, 20);
        SetChunkCoordinates(x, z);

        /* Generate the terrain */
        CreateTerrain();
    }
}
