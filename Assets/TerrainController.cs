using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * Used to control the creation of terrain using the TerrainChunk script.
 * Attach this to an empty object to turn it into a container for perlin noise generated terrain.
 */
public class TerrainController : MonoBehaviour {
    
    /* The chunk settings used by the chunks */
    public TerrainChunkSettings settings;

    /* The position that the terrain will center around */
    public Vector3 position;
    public Vector2 currentChunk;

    /* The radius of the circle that defines how far the terrain will render */
    public float maxRenderDistance;

    /* How long/wide a chunk is. Chunks are always square shaped. */
    public int chunkLength;

    /* How high/low the terrain will reach */
    public int height;

    /* The resolution of each chunk */
    public int chunkResolution;

    /* All data saved on the current collection of chunks  */
    private ChunkCache cache;

    
    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Start() {

        /* Initialize any objects that will be used */
        InitializeVariables();

        /* Set the settings for each chunk */
        settings.SetSettings(chunkResolution, chunkLength, height, transform);


        /* Set the current chunk different than the current position to force the terrain to update */
        currentChunk = new Vector2(1, 1) + GetChunkPosition(position);
    }
    
    void Update() {
        /*
         * Check whenever the position changes into a new chunk, updating the terrain when required.
         */
        Vector2 newChunk = GetChunkPosition(position);

        if(newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {
            
            /* Get a series of lists that represent a unique group of positions */
            List<Vector2> loadedChunks = cache.GetLoadedChunks();
            List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(position), 3);
            List<Vector2> chunksToRemove = loadedChunks.Except(newChunks).ToList();
            List<Vector2> chunksToLoad = newChunks.Except(loadedChunks).ToList();

            /* Remove the unnecessary chunks */
            RemoveChunksRequest(chunksToRemove);

            /* Load the unloaded chunks */
            AddChunksRequest(chunksToLoad);
            
            /* Update the current coordinates */
            currentChunk = newChunk;
        }

        /* Update the cache */
        cache.UpdateCache();
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
                TerrainChunk newChunk = new TerrainChunk(settings, key);
                cache.chunksToBeGenerated.Add(key, newChunk);
            }
        }
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    void InitializeVariables() {
        /*
         * Initialize the variables used by this script
         */

        cache = new ChunkCache();
        settings = new TerrainChunkSettings();
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
