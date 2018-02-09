using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Used to control the creation of terrain using the TerrainChunk script.
 * Attach this to an empty object to turn it into a container for perlin noise generated terrain.
 */
public class TerrainController : MonoBehaviour {

    /* All chunks that have been loaded */
    private Dictionary<Vector2, TerrainChunk> loadedChunks;

    /* The chunk settings used by the chunks */
    public TerrainChunkSettings settings;


    void Start() {
        /*
         * Create a large circle of terrain around the origin
         */

        InitializeVariables();

        List<Vector2> newChunks = GetVisibleChunksFromPosition(new Vector2(0, 0), 5);
        CreateTerrainChunks(newChunks);

        //Delete a chunk
        RemoveChunk(1, 0);
    }

    void InitializeVariables() {
        /*
         * Initialize the varaibles used by this script
         */

        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
    }


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
}
