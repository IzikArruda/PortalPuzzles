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

    /* A hashset of the chunks that need to be removed */
    private HashSet<Vector2> chunksToRemove;


    /* ----------- Update Functions ------------------------------------------------------------- */
    
    public ChunkCache() {
        /*
         * Upon creation, initialize the required variables used to track the data
         */
         
        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
        chunksToRemove = new HashSet<Vector2>();
    }
    
    public void UpdateCache() {
        /*
         * Update the terrain by going through the cache's collections
         */

        /* Remove any chunks that must be removed */
        RemoveChunksFromList();
    }


    /* ----------- Terrain Functions ------------------------------------------------------------- */

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


    /* ----------- Chunk Functions ------------------------------------------------------------- */
    
    public void RemoveChunksRequest(List<Vector2> chunks) {
        /*
         * Given a list of chunk keys, add them to the chunksToRemove collection to be removed
         */

        foreach(Vector2 key in chunks) {
            /* Check if the given key can be added to the toBeRemoved collection */
            if(CanRemoveChunk(key)) {
                chunksToRemove.Add(key);
            }
        }
    }

    private void RemoveChunk(Vector2 key) {
        /*
         * Remove the chunk defined by the given key from it's lists and the game
         */

        loadedChunks[key].Remove();
        loadedChunks.Remove(key);
        chunksToRemove.Remove(key);
    }


    /* ----------- Collections Functions ------------------------------------------------------------- */

    private void RemoveChunksFromList() {
        /*
         * Take the chunksToRemove hashset and remove some chunks if needed.
         * Removing chunks is a fast action, so we can do as many as we like at runtime
         */

        List<Vector2> removedChunks = chunksToRemove.ToList();

        foreach(Vector2 key in removedChunks) {

            /* Only remove the chunk once it's fully loaded */
            if(loadedChunks.ContainsKey(key)) {
                RemoveChunk(key);
            }
        }
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    public List<Vector2> GetLoadedChunks() {
        /*
         * Return a list of the keys/positions of each chunk that is currently loaded into the game
         */

        return loadedChunks.Keys.ToList();
    }

    public bool CanRemoveChunk(Vector2 key) {
        /*
         * Return whether the chunk defined by the given key can be removed or not.
         * Return true if the key is loaded and not currently in the to be removed list.
         */
        bool canBeRemoved = false;
        
        if(loadedChunks.ContainsKey(key) && !chunksToRemove.Contains(key)) {
            canBeRemoved = true;
        }

        return canBeRemoved;
    }
}
