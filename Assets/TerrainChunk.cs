﻿using UnityEngine;
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

    /* Use this to find the height of the terrain */
    private NoiseProvider noiseProvider;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public TerrainChunk(TerrainChunkSettings settings, NoiseProvider noise, Vector2 key) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        heightMapThreadLock = new object();
        Settings = settings;
        noiseProvider = noise;
        X = (int) key.x;
        Z = (int) key.y;
    }

    public TerrainChunk(TerrainChunkSettings settings, NoiseProvider noise, int x, int z) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        heightMapThreadLock = new object();
        Settings = settings;
        noiseProvider = noise;
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
                
                newHeightMap[z, x] = noiseProvider.GetNoise(xCoord, zCoord);
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


    /* ----------- Event Functions ------------------------------------------------------------- */

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
        ApplyTextures(terrainData);

        /* Create the object that will contain the terrain components */
        GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
        newTerrainGameObject.transform.position = new Vector3(X * Settings.Length, 0, Z * Settings.Length);
        newTerrainGameObject.transform.parent = Settings.chunkContainer;
        newTerrainGameObject.transform.name = "[" + X + ", " + Z + "]";
        
        /*  Set the material of the terrain */
        Terrain = newTerrainGameObject.GetComponent<Terrain>();
        Terrain.heightmapPixelError = 8;
        Terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        Terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
        Terrain.materialTemplate = Settings.terrainMaterial;
        Terrain.Flush();
    }

    private void ApplyTextures(TerrainData data) {
        /*
         * Apply texture to the terrain data depending on the shape of the terrain.
         * For now, use splatmapping to texture the terrain based on the steepness.
         */
        SplatPrototype flatSplat = new SplatPrototype();
        SplatPrototype steepSplat = new SplatPrototype();
        flatSplat.texture = Settings.flatTexture;
        steepSplat.texture = Settings.steepTexture;

        /* Create an array of splat prototypes used by the terrain */
        terrainData.splatPrototypes = new SplatPrototype[] {
            flatSplat,
            steepSplat
        };
        terrainData.RefreshPrototypes();

        /* Set the splatmap to switch textures as the terrain's steepness grows */
        float steepnessStretch = 1.5f;
        float normX, normZ, steepness, normSteepness;
        float[,,] splatMap = new float[data.alphamapResolution, data.alphamapResolution, 2];
        for(int z = 0; z < data.alphamapHeight; z++) {
            for(int x = 0; x < data.alphamapWidth; x++) {
                normX = (float) x / (terrainData.alphamapWidth - 1);
                normZ = (float) z / (terrainData.alphamapHeight - 1);
                steepness = terrainData.GetSteepness(normX, normZ);
                normSteepness = Mathf.Clamp(steepness/steepnessStretch, 0f, 1f);

                splatMap[z, x, 0] = 1f - normSteepness;
                splatMap[z, x, 1] = normSteepness;
            }
        }
        terrainData.SetAlphamaps(0, 0, splatMap);
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
