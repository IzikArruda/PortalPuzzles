using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * This script handles all data involved in the terrain chunks
 */
public class ChunkCache {

    /* All chunks that have been loaded */
    private Dictionary<Vector2, TerrainChunk> loadedChunks;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    public ChunkCache() {
        /*
         * Upon creation, initialize the required variables used to track the data
         */
         
        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
    }

    
    /* ----------- Chunk Functions ------------------------------------------------------------- */

    public void CreateTerrainChunk(TerrainChunkSettings settings, int x, int z) {
        /*
         * Create a single chunk of terrain given the coordinates of the terrain
         */

        /* Create a new TerrainChunk and link it's settings and position */
        TerrainChunk newChunk = new TerrainChunk();
        newChunk.LinkSettings(settings);
        newChunk.GenerateTerrain(x, z);

        /* Add the chunk to the loadedChunks dictionary */
        loadedChunks.Add(new Vector2(newChunk.X, newChunk.Z), newChunk);
    }

    public void CreateTerrainChunks(List<Vector2> chunks, TerrainChunkSettings settings) {
        /*
         * Create each chunk described by the given list
         */

        foreach(Vector2 chunk in chunks) {
            CreateTerrainChunk(settings, (int) chunk.x, (int) chunk.y);
        }
    }

    public void RemoveChunk(int x, int z) {
        /*
         * Remove the chunk in the given position using the dictionary of created chunks
         */
        Vector2 key = new Vector2(x, z);

        /* Remove the chunk from the list of loaded chunks and delete the object it's attached to */
        TerrainChunk removedChunk = loadedChunks[key];
        loadedChunks.Remove(key);
        removedChunk.Remove();
    }

    public void RemoveChunks(List<Vector2> chunks) {
        /*
         * Remove the chunks given by the list
         */

        foreach(Vector2 chunk in chunks) {
            RemoveChunk((int) chunk.x, (int) chunk.y);
        }
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    public List<Vector2> GetLoadedChunks() {
        /*
         * Return a list of the keys/positions of each chunk that is currently loaded into the game
         */

        return loadedChunks.Keys.ToList();
    }
}
