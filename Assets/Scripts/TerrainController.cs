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

    /* The object that the terrain, skysphere and sun will center around. Set to the player's camera */
    public Transform focusPoint;
    public Transform playerCam;
    public Transform windowCam;
    public Transform windowExitPoint;
    public Vector2 currentChunk;

    /* How long/wide a chunk is. Chunks are always square shaped. */
    public int chunkLength;

    /* How high/low the terrain will reach */
    public int height;

    /* The resolution of each chunk */
    public int chunkResolution;
    
    /* Many chunks will load along each direction of the center chunk */
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
    private SkySphere skySphereScript;

    /* The lighting used in the game */
    public Light directionalLight;
    public Light pointLight;
    public float flareIntensity;
    private float flareIntensityMod = 1;
    public float heightAngle;
    public float horizonAngle;
    public float distance;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    public void StartAlt() {
        /*
         * Acts as unity's built-in Start function, but is instead called by the StartingRoom's Start function. 
         */

        /* Remove any previous terrain and skySpheres already built */
        for(int i = transform.childCount; i > 0; i--) {
            GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
        }

        /* Initialize any objects that will be used */
        InitializeVariables();

        /* Populate the chunk cache with default chunks */
        cache = new ChunkCache(GetVisibleChunksFromPositionCount(new Vector2(0, 0), chunkViewRange), chunkSettings, noiseProvider);

        /* Set the current chunk position */
        UpdateFocusPoint();
        currentChunk = GetChunkPosition(focusPoint.position);

        /* Create the skySphere */
        CreateSkySphere();

        Debug.Log(terrainContainer.transform.childCount);
    }
    
    void Update() {
        /*
         * Check whenever the position changes into a new chunk, updating the terrain when required.
         */

        /* Get the chunk that the position currently resides in */
        UpdateFocusPoint();
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

    void OnDisable() {
        /*
         * Delete the unused terrain chunks when the controller is disabled.
         * This is to ensure the terrainChunks are not saved into the editor.
         */
         
        //Debug.Log(terrainContainer.transform.childCount);
        for(int i = terrainContainer.transform.childCount-1; i >= 0; i--) {
            //DestroyImmediate(terrainContainer.transform.GetChild(i).gameObject);
        }
        //Debug.Log(terrainContainer.transform.childCount);
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

        /* Reposition the point light of the scene that is used for the sun flare */
        UpdateLight();
    }

    public void UpdateFocusPoint() {
        /*
         * Update the current camera that is used for the focus point
         */

        /* Both cameras are potential focus points - decide which to focus on */
        if(playerCam != null && windowCam != null) {
            /* Check which point is placed further along the Z axis to determine which to focus on */
            /* This hack is fairly consistent if we keep the rooms going along the +Z axis and the 
             * outside along the -Z axis. To further solildify this hack, we could set the 
             * window's camera to null once the player has fallen far enough past the window once outside. */
            if(windowExitPoint.position.z > playerCam.position.z) {
                focusPoint = playerCam;
            }
            else {
                focusPoint = windowCam;
            }
        }

        /* Assign the camera that is not null */
        else if(playerCam != null) {
            focusPoint = playerCam;
        }
        else if(windowCam != null) {
            focusPoint = windowCam;
        }

        /* Both cameras are null - there is no point to focus on */
        else {
            Debug.Log("WARNING: TERRAINCONTROLLER HAS NO FOCUS POINT");
        }
    }


    /* ----------- Lighting Functions ------------------------------------------------------------- */

    void UpdateLight() {
        /*
         * Update the lights and the sun's position. The lighting of the scene is determined by 
         * the direction light, while the sun and it's flare are determined by the point light.
         */

        /* Get the direction of the sun's position from the focus point */
        Vector3 direction = Quaternion.AngleAxis(horizonAngle, Vector3.up)*Quaternion.AngleAxis(heightAngle, Vector3.right)*Vector3.back;

        /* Place the sun */
        pointLight.transform.position = skySphereScript.transform.position + distance*direction;
        pointLight.transform.eulerAngles = new Vector3(0, 0, 0);

        /* Update the directional light to match the sun's placement */
        directionalLight.transform.eulerAngles = new Vector3(heightAngle, horizonAngle, 0);

        /* Update the intensity of the flare */
        pointLight.GetComponent<LensFlare>().brightness = flareIntensity*flareIntensityMod;
    }

    public void UpdateSunFlareMod(float mod) {
        /*
         * Update the flareIntensityMod value used to multiply the flare intensity
         */

        flareIntensityMod = mod;
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

        /* Set the chunkSettings script that is used for each chunk */
        chunkSettings = new TerrainChunkSettings();
        chunkSettings.SetSettings(chunkResolution, chunkLength, height, terrainContainer.transform, terrainMaterial, terrainTextures, PortalSet.maxLayer + 2, 850f);

        /* Create the noiseProvider that will be used by all chunks */
        noiseProvider = new NoiseProvider(frequency, octave, height, chunkSettings);

    }

    void CreateSkySphere() {
        /*
         * Create the skySphere that surrounds the terrain. Use the player camera's far clipping plane
         * to judge how large the sky sphere will be.
         */

        /* Make sure we have a skySphere script */
        skySphereScript = new GameObject().AddComponent<SkySphere>();
        skySphereScript.transform.parent = transform;

        /* Set the radius of the sphere to be relative to the terrain chunk's reach */
        skySphereScript.radius = chunkLength*(chunkViewRange - 1.25f);

        /* Create the sphere object and add the skySphere script to it */
        skySphereScript.CreateSkySphere();
        UpdateSkySphere(new Vector3(0, 0, 0));

        /* Apply the skyTexture to the skySphere */
        skySphereScript.ApplyTexture(skySphereTexture);

        /* Put the sky sphere in the terrain layer as it will only be used when outside */
        skySphereScript.gameObject.layer = LayerMask.NameToLayer("Terrain");
    }
    

    /* ----------- Helper Functions ------------------------------------------------------------- */

    public List<Vector2> GetVisibleChunksFromPosition(Vector2 chunkPosition, int radius) {
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

    public int GetVisibleChunksFromPositionCount(Vector2 chunkPosition, int radius) {
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
        
        terrainHeight = noiseProvider.GetHeightFromWorldPos(x, z);
        
        return terrainHeight;
    }

    public int GetReadyTerrain() {
        /*
         * Return an integer of how many chunks are fully loaded
         */
        int readyChunks = 0;

        /* Get the amount of loaded chunks in the area */

        return readyChunks;
    }

    public int GetChunkState(Vector2 chunkCoords) {
        /*
         * Return an integer that determines what state the given chunk is at. 
         * 0 : Chunk is not used by the game and will/is unloaded
         * 1 : Chunk is idle waiting to start generating it's heightmap
         * 2 : Chunk is generating it's heightmap
         * 3 : Chunk is idle with a complete heightmap
         * 4 : Chunk is generating it's textures
         * 5 : Chunk is finished loading
         */
        int chunkState = -1;

        /* Chunk is Due to start generating - Currently is data and not an object */
        if(cache.chunksToBeGenerated.ContainsKey(chunkCoords)) {
            chunkState = 1;
        }

        /* Chunk is currently generating it's height map */
        else if(cache.chunksGeneratingHeightMap.ContainsKey(chunkCoords)) {
            chunkState = 2;
        }

        /* Chunk has finished it's height map and is waiting to start generating it's textures */
        else if(cache.chunksFinishedHeightMaps.ContainsKey(chunkCoords)) {
            chunkState = 3;
        }

        /* Chunk is generating it's textures */
        else if(cache.chunksGeneratingTextureMap.ContainsKey(chunkCoords)) {
            chunkState = 4;
        }

        /* Chunk is fully generated */
        else if(cache.loadedChunks.ContainsKey(chunkCoords)) {
            chunkState = 5;
        }

        /* Chunk will not be used by the game */
        else {
            chunkState = 0;
        }

        return chunkState;
    }

    public float GetLoadingPercent() {
        /*
         * Return a 0 to 1 value which represents how much of the terrain is loaded.
         * A terrain is considered loaded once it's heightmap is loaded in.
         */
        float loadingPercent = 0;
        
        /* Print the state of the chunks */
        List<Vector2> visibleChunks = GetVisibleChunksFromPosition(currentChunk, chunkViewRange);
        /* Print the state of each chunk */
        for(int i = 0; i < visibleChunks.Count; i++) {
            if(GetChunkState(visibleChunks[i]) > 2) {
                loadingPercent++;
            }
        }
        loadingPercent /= visibleChunks.Count;

        return loadingPercent;
    }
}
