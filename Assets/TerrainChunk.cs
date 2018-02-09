using UnityEngine;
using System.Collections;

public class TerrainChunk : MonoBehaviour {

    public int X { get; private set; }

    public int Z { get; private set; }

    private Terrain Terrain { get; set; }

    private TerrainChunkSettings Settings { get; set; }

    public void Start() {
        Test();
    }

    public TerrainChunk(TerrainChunkSettings settings, int x, int z) {
        Settings = settings;
        X = x;
        Z = z;
    }

    public void CreateTerrain() {
        var terrainData = new TerrainData();
        terrainData.heightmapResolution = Settings.HeightmapResolution;
        terrainData.alphamapResolution = Settings.AlphamapResolution;

        var heightmap = GetHeightmap();
        terrainData.SetHeights(0, 0, heightmap);
        terrainData.size = new Vector3(Settings.Length, Settings.Height, Settings.Length);

        var newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
        newTerrainGameObject.transform.position = new Vector3(X * Settings.Length, 0, Z * Settings.Length);
        Terrain = newTerrainGameObject.GetComponent<Terrain>();
        Terrain.Flush();
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

    void Test() {
        TerrainChunkSettings settings = new TerrainChunkSettings(129, 129, 100, 20);
        TerrainChunk terrain = new TerrainChunk(settings, 0, 0);
        terrain.CreateTerrain();
    }
}
