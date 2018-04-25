using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * Used to control the creation of terrain using the TerrainChunk script.
 * Attach this to an empty object to turn it into a container for perlin noise generated terrain.
 * 
 * http://code-phi.com/infinite-terrain-generation-in-unity-3d/ was used as a tutorial to this terrain generation
 */
public class TerrainController : MonoBehaviour {
    
    /* The chunk settings used by the chunks */
    public TerrainChunkSettings chunkSettings;
    private GameObject terrainContainer;

    /* The object that the terrain will center around. Have this set before startup. */
    public Transform focusPoint;
    public Vector2 currentChunk;

    /* How long/wide a chunk is. Chunks are always square shaped. */
    public int chunkLength;

    /* How high/low the terrain will reach */
    public int height;

    /* The resolution of each chunk */
    public int chunkResolution;

    /* All data saved on the current collection of chunks  */
    private ChunkCache cache;

    /* How far the player can see in chunks */
    public int chunkViewRange;

    /* The material used by the terrain */
    public Material terrainMaterial;

    /* The textures used by the terrain. Each biome type requires two textures it switches between by the steepness. */
    public Texture2D[] terrainTextures;

    /* Calculates the noise for the chunks */
    private NoiseProvider noiseProvider;

    /* Values that control the detail in the noise that generates the terrain */
    public float frequency;
    public int octave;

    /* The skySphere and it's texture that will surround the focus point */
    public Texture2D skySphereTexture;
    private GameObject skySphere;
    private SkySphere skySphereScript;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    public void StartAlt() {
        /*
         * Acts as unity's built-in Start function, but is instead called by the StartingRoom's Start function. 
         */

        /* Initialize any objects that will be used */
        InitializeVariables();

        /* Populate the chunk cache with default chunks */
        cache = new ChunkCache(GetVisibleChunksFromPositionCount(new Vector2(0, 0), chunkViewRange), chunkSettings, noiseProvider);
        
        /* Set the current chunk position */
        currentChunk = GetChunkPosition(focusPoint.position);
        
        /* Force the chunkCache to update it's chunks all at once */
        ForceCacheUpdate();

        /* Create the skySphere */
        CreateSkySphere();
    }
    
    void Update() {
        /*
         * Check whenever the position changes into a new chunk, updating the terrain when required.
         */

        /* Get the chunk that the position currently resides in */
        Vector2 newChunk = GetChunkPosition(focusPoint.position);

        /* If we enter a new chunk or there are remaining inactive chunks, update the active chunks */
        if((newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) || cache.GetRemainingInactiveChunks() != 0) {

            /* Get a series of lists that represent a unique group of positions */
            List<Vector2> allChunks = cache.GetAllChunks();
            List<Vector2> newChunks = GetVisibleChunksFromPosition(newChunk, chunkViewRange);
            List<Vector2> chunksToRemove = allChunks.Except(newChunks).ToList();
            List<Vector2> chunksToLoad = newChunks.Except(allChunks).ToList();

            /* Remove the unnecessary chunks */
            RemoveChunksRequest(chunksToRemove);

            /* Load the unloaded chunks */
            AddChunksRequest(chunksToLoad);
            
            /* Update the current coordinates */
            currentChunk = newChunk;
        }

        /* Update the cache */
        cache.UpdateCache();

        /* Reposition the skySphere */
        UpdateSkySphere(focusPoint.position);
    }


    /* ----------- Update Functions ------------------------------------------------------------- */

    void RemoveChunksRequest(List<Vector2> chunks) {
        /*
         * Given a list of chunk keys, add them to the chunksToRemove collection to be removed
         */

        foreach(Vector2 key in chunks) {

            /* Check if the given key can be added to the toBeRemoved collection */
            if(cache.CanRemoveChunk(key)) {
                cache.chunksToRemove.Add(key);
            }
        }
    }

    public void AddChunksRequest(List<Vector2> chunks) {
        /*
         * Given a list of chunk positions, add new chunks to the chunksToBeGenerated collection
         */

        foreach(Vector2 chunkKey in chunks) {

            /* Send a request to add the given key to the chunk collection */
            cache.RequestNewChunk(chunkKey);
        }
    }

    private void ForceCacheUpdate() {
        /*
         * Force the cache to update all the terrain at once. This should only ever be run on 
         * startup as the game has not yet begun, giving it time to load everything.
         */
         
        List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(focusPoint.position), chunkViewRange);
        foreach(Vector2 chunkKey in newChunks) {

            /* Request the cache to load a given coordinate */
            cache.ForceLoadChunk(chunkKey);
        }
    }
    
    public void UpdateSkySphere(Vector3 focusPointPosition) {
        /*
         * Have the skySphere reposition given the new focusPointPosition
         */

        /* Reposition the sky sphere at the given window exit point */
        skySphereScript.UpdateSkySpherePosition(focusPointPosition);
    }
    

    /* ----------- Set-up Functions ------------------------------------------------------------- */

    void InitializeVariables() {
        /*
         * Initialize the variables used by this script
         */

        /* Create the container that holds all the terrain objects */
        terrainContainer = new GameObject();
        terrainContainer.name = "Terrain Container";
        terrainContainer.transform.parent = transform;
        terrainContainer.transform.localPosition = new Vector3(0, 0, 0);
        terrainContainer.transform.localEulerAngles = new Vector3(0, 0, 0);
        terrainContainer.transform.localScale = new Vector3(1, 1, 1);

        /* Set the settings for each chunk */
        chunkSettings = new TerrainChunkSettings();
        chunkSettings.SetSettings(chunkResolution, chunkLength, height, terrainContainer.transform, terrainMaterial, terrainTextures, PortalSet.maxLayer + 2);

        /* Create the noiseProvider that will be used by all chunks */
        noiseProvider = new NoiseProvider(frequency, octave, height);

    }

    void CreateSkySphere() {
        /*
         * Create the skySphere that surrounds the terrain. Use the player camera's far clipping plane
         * to judge how large the sky sphere will be.
         */

        /* Create the sphere object and add the skySphere script to it */
        if(skySphere != null) { DestroyImmediate(skySphere); }
        skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        skySphere.transform.parent = transform;
        skySphereScript = skySphere.AddComponent<SkySphere>();
        UpdateSkySphere(new Vector3(0, 0, 0));

        /* Apply the skyTexture to the skySphere */
        skySphereScript.ApplyColor(new Color(0.45f, 0.50f, 0.65f));
    }
    

    /* ----------- Helper Functions ------------------------------------------------------------- */

    private List<Vector2> GetVisibleChunksFromPosition(Vector2 chunkPosition, int radius) {
        /*
         * Return a list of all chunks that should be rendered given the position.
         * The position and radius values are relative to chunk sizes.
         */
        List<Vector2> visibleChunks = new List<Vector2>();

        /* Add each visible chunk to the chunk list */
        for(int x = -radius; x < radius; x++) {
            for(int z = -radius; z < radius; z++) {
                if(x*x + z*z < radius*radius) {
                    visibleChunks.Add(new Vector2(chunkPosition.x + x, chunkPosition.y + z));
                }
            }
        }

        return visibleChunks;
    }

    private int GetVisibleChunksFromPositionCount(Vector2 chunkPosition, int radius) {
        /*
         * Get the count of how many chunks are visible from a given position.
         */
        int chunkCount = 0;
        
        for(int x = -radius; x < radius; x++) {
            for(int z = -radius; z < radius; z++) {
                if(x*x + z*z < radius*radius) {
                    chunkCount++;
                }
            }
        }

        return chunkCount;
    }

    private Vector2 GetChunkPosition(Vector3 worldPosition) {
        /*
         * Given a position in the world, get the chunk position it resides within
         */
        int x = (int) Mathf.Floor(worldPosition.x / chunkSettings.Length);
        int z = (int) Mathf.Floor(worldPosition.z / chunkSettings.Length);

        return new Vector2(x, z);
    }

    public float GetTerrainHeightAt(float x, float z) {
        /*
         * Given an X and Z coordinate, return the height of the terrain in the cache.
         */
        float terrainHeight = 0;

        /* Make the X and Z coordinate relative to the heightMap resolution to properly place it in the noise */
        float properX = (x / chunkSettings.Length)*chunkSettings.HeightmapResolution;
        float properZ = (z / chunkSettings.Length)*chunkSettings.HeightmapResolution;

        /* Parse the noiseProvider in the same maner as the terrain chunks */
        float lengthModifier = chunkSettings.Length/1000f;
        float xCoord = lengthModifier*((float) properX / (chunkSettings.HeightmapResolution - 1));
        float zCoord = lengthModifier*((float) properZ / (chunkSettings.HeightmapResolution - 1));
        terrainHeight = noiseProvider.GetNoise(xCoord, zCoord);
        
        return terrainHeight;
    }
}
