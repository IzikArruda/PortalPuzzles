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
    public TerrainChunkSettings settings;

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
    private GameObject skySphere;
    public Texture2D skySphereTexture;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Start() {

        /* Initialize any objects that will be used */
        InitializeVariables();

        /* Set the settings for each chunk */
        settings.SetSettings(chunkResolution, chunkLength, height, transform, terrainMaterial, terrainTextures);

        /* Set the current chunk position */
        currentChunk = GetChunkPosition(focusPoint.position);

        /* Create the noiseProvider that will be used by all chunks */
        noiseProvider = new NoiseProvider(frequency, octave, height);

        /* Force the chunkCache to update it's chunks all at once */
        ForceCacheUpdate();

        /* Create the skySphere */
        CreateSkySphere();

        /* Set the fog of the world. This should be a sepperate function, but for now we can leave it in here */
        float playerViewLength = 0.5f*CustomPlayerController.cameraFarClippingPlane;
        //Have the fog start halfway from the player and their max view
        RenderSettings.fogStartDistance = playerViewLength*0.5f;
        //Have the fog end before the last 10% of the player's view
        RenderSettings.fogEndDistance = playerViewLength*0.9f;
    }
    
    void Update() {
        /*
         * Check whenever the position changes into a new chunk, updating the terrain when required.
         */
         
        /* Get the chunk that the position currently resides in */
        Vector2 newChunk = GetChunkPosition(focusPoint.position);

        if(newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {

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

        foreach(Vector2 key in chunks) {

            /* Check if the given chunk can be added to the collection */
            if(cache.CanAddChunk(key)) {
                TerrainChunk newChunk = new TerrainChunk(settings, noiseProvider, key);
                cache.chunksToBeGenerated.Add(key, newChunk);
            }
        }
    }

    private void ForceCacheUpdate() {
        /*
         * Force the cache to update all the terrain at once. This should only ever be run on 
         * startup as the game has not yet begun, giving it time to load everything.
         */
         
        List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(focusPoint.position), chunkViewRange);
        foreach(Vector2 chunkKey in newChunks) {

            /* Create the chunk and force it to load into the cache */
            TerrainChunk newChunk = new TerrainChunk(settings, noiseProvider, chunkKey);
            cache.ForceLoadChunk(newChunk);
        }
    }
    
    public void UpdateSkySphere(Vector3 focusPointPosition) {
        /*
         * Have the skySphere reposition given the new focusPointPosition
         */

        /* Reposition the sky sphere at the given window exit point */
        skySphere.transform.position = focusPointPosition;
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    void InitializeVariables() {
        /*
         * Initialize the variables used by this script
         */

        cache = new ChunkCache();
        settings = new TerrainChunkSettings();
    }
    
    void CreateSkySphere() {
        /*
         * Create the skySphere that surrounds the terrain. Use the player camera's far clipping plane
         * to judge how large the sky sphere will be.
         */

        /* Create the object */
        if(skySphere != null) { DestroyImmediate(skySphere); }
        skySphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        DestroyImmediate(skySphere.GetComponent<SphereCollider>());
        skySphere.transform.parent = transform;
        UpdateSkySphere(new Vector3(0, 0, 0));
        float renderDist = CustomPlayerController.cameraFarClippingPlane;
        skySphere.transform.localScale = new Vector3(renderDist*0.99f, renderDist*0.99f, renderDist*0.99f);
        skySphere.name = "Sky sphere";

        /* flip all it's triangles of the sphere to have it inside out */
        int[] triangles = skySphere.GetComponent<MeshFilter>().sharedMesh.triangles;
        if(triangles[0] == 0) {
            int tempInt;
            for(int i = 0; i < triangles.Length; i += 3) {
                tempInt = triangles[i + 0];
                triangles[i + 0] = triangles[i + 2];
                triangles[i + 2] = tempInt;
            }
            skySphere.GetComponent<MeshFilter>().sharedMesh.triangles = triangles;
        }

        /* Apply the skyTexture to the skySphere */
        Material skySphereMaterial = new Material(Shader.Find("Unlit/Fogless"));
        skySphereMaterial.SetTexture("_MainTex", skySphereTexture);
        skySphere.GetComponent<MeshRenderer>().sharedMaterial = skySphereMaterial;

        /* Put the SkySphere into the SkyDome layer as that is how it wil be treated by the cameras */
        skySphere.layer = PortalSet.maxLayer+1;
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

    private Vector2 GetChunkPosition(Vector3 worldPosition) {
        /*
         * Given a position in the world, get the chunk position it resides within
         */
        int x = (int) Mathf.Floor(worldPosition.x / settings.Length);
        int z = (int) Mathf.Floor(worldPosition.z / settings.Length);

        return new Vector2(x, z);
    }

}
