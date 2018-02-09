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
        /*
         * Create a large circle of terrain around the origin
         */

        /* Initialize any objects that will be used */
        InitializeVariables();

        /* Set the settings for each chunk */
        settings.SetSettings(chunkResolution, chunkLength, height, transform);

        //For now, create the land to begin with
        List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(position), 3);
        cache.CreateTerrainChunks(newChunks, settings);
    }
    
    void Update() {
        /*
         * Check whenever the position changes into a new chunk, updating the terrain when required.
         */
        Vector2 newChunk = GetChunkPosition(position);

        if(newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {
            
            /* Get the keys/positions of all the chunks that are currently loaded */
            List<Vector2> loadedChunks = cache.GetLoadedChunks();

            /* Get the keys/position of all the chunks the new position requires */
            List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(position), 3);

            /* Get the keys/position of all the chunks that are loaded and not a part of the required chunks */
            List<Vector2> chunksToRemove = loadedChunks.Except(newChunks).ToList();

            /* Get the keys/position of all the required chunks that have not yet been loaded */
            List<Vector2> chunksToLoad = newChunks.Except(loadedChunks).ToList();

            /* Remove the unnecessary chunks */
            cache.RemoveChunksRequest(chunksToRemove);

            /* Load the unloaded chunks */
            cache.AddChunksRequest(chunksToLoad, settings);

            
            Debug.Log("Regenerated");
            currentChunk = newChunk;
        }

        /* Update the cache */
        cache.UpdateCache();
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
