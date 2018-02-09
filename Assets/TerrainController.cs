using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * Used to control the creation of terrain using the TerrainChunk script.
 * Attach this to an empty object to turn it into a container for perlin noise generated terrain.
 */
public class TerrainController : MonoBehaviour {

    /* All chunks that have been loaded */
    private Dictionary<Vector2, TerrainChunk> loadedChunks;

    /* The chunk settings used by the chunks */
    public TerrainChunkSettings settings;

    /* The position that the terrain will center around */
    public Vector3 position;
    public Vector2 currentChunk;

    
    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Start() {
        /*
         * Create a large circle of terrain around the origin
         */

        /* Initialize any objects that will be used */
        InitializeVariables();

        /* Set the settings of the chunkSettings */
        settings.SetSettings(129, 100, 20);

        //For now, create the land to begin with
        List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(position), 3);
        CreateTerrainChunks(newChunks);
    }
    
    void Update() {
        /*
         * Check whenever the position changes into a new chunk, updating the terrain when required.
         */
        Vector2 newChunk = GetChunkPosition(position);

        if(newChunk.x != currentChunk.x || newChunk.y != currentChunk.y) {
            
            /* Get the keys/positions of all the chunks that are currently loaded */
            List<Vector2> loadedChunks = GetLoadedChunks();

            /* Get the keys/position of all the chunks the new position requires */
            List<Vector2> newChunks = GetVisibleChunksFromPosition(GetChunkPosition(position), 3);

            /* Get the keys/position of all the chunks that are loaded and not a part of the required chunks */
            List<Vector2> chunksToRemove = loadedChunks.Except(newChunks).ToList();

            /* Get the keys/position of all the required chunks that have not yet been loaded */
            List<Vector2> chunksToLoad = newChunks.Except(loadedChunks).ToList();

            /* Remove the unnecessary chunks */
            RemoveChunks(chunksToRemove);

            /* Load the unloaded chunks */
            CreateTerrainChunks(chunksToLoad);

            
            Debug.Log("Regenerated");
            currentChunk = newChunk;
        }
    }
    

    /* ----------- Set-up Functions ------------------------------------------------------------- */
    
    void InitializeVariables() {
        /*
         * Initialize the varaibles used by this script
         */

        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
    }
    

    /* ----------- Chunk Functions ------------------------------------------------------------- */
    
    void CreateTerrainChunk(int x, int z) {
        /*
         * Create a single chunk of terraingiven the coordinates of the terrain
         */

        /* Create a new object and attach a TerrainChunk and it's settings */
        GameObject newChunkObject = new GameObject();
        newChunkObject.name = "Terrain Chunk";
        newChunkObject.transform.parent = transform;
        TerrainChunk newChunk = newChunkObject.AddComponent<TerrainChunk>();

        /* Link the settings to the terrain chunk and generate new terrain */
        newChunk.LinkSettings(settings);
        newChunk.GenerateTerrain(x, z);

        /* Add the chunk to the loadedChunks dictionary */
        loadedChunks.Add(new Vector2(newChunk.X, newChunk.Z), newChunk);
    }

    void CreateTerrainChunks(List<Vector2> chunks) {
        /*
         * Create each chunk described by the given list
         */

        foreach(Vector2 chunk in chunks) {
            CreateTerrainChunk((int)chunk.x, (int)chunk.y);
        }
    }
    
    private void RemoveChunk(int x, int z) {
        /*
         * Remove the chunk in the given position using the dictionary of created chunks
         */
        Vector2 key = new Vector2(x, z);

        /* Remove the chunk from the list of loaded chunks and delete the object it's attached to */
        TerrainChunk removedChunk = loadedChunks[key];
        loadedChunks.Remove(key);
        Destroy(removedChunk.gameObject);
    }

    private void RemoveChunks(List<Vector2> chunks) {
        /*
         * Remove the chunks given by the list
         */

        foreach(Vector2 chunk in chunks) {
            RemoveChunk((int) chunk.x, (int) chunk.y);
        }
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

    private List<Vector2> GetLoadedChunks() {
        /*
         * Return a list of the keys/positions of each chunk that is currently loaded into the game
         */

        return loadedChunks.Keys.ToList();
    }
}
