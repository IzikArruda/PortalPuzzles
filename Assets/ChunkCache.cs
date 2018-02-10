using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * This script handles all data involved in the terrain chunks
 */
public class ChunkCache {

    /* All chunks that have been loaded */
    public Dictionary<Vector2, TerrainChunk> loadedChunks;

    /* All chunks that must undergo generation to be properly loaded */
    public Dictionary<Vector2, TerrainChunk> ChunksBeingGenerated;

    /* A hashset of the chunks that need to be removed */
    public HashSet<Vector2> chunksToRemove;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public ChunkCache() {
        /*
         * Upon creation, initialize the required variables used to track the data
         */
         
        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
        ChunksBeingGenerated = new Dictionary<Vector2, TerrainChunk>();
        chunksToRemove = new HashSet<Vector2>();
    }


    /* ----------- Update Functions ------------------------------------------------------------- */

    public void UpdateCache() {
        /*
         * Update the terrain by going through the cache's collections
         */

        /* Remove any chunks that must be removed */
        RemoveChunksFromList();

        /* Load chunks into the cash that need to be generated */
        GenerateChunkFromList();
    }


    /* ----------- Terrain Functions ------------------------------------------------------------- */

    public void CreateTerrainChunk(TerrainChunkSettings settings, int x, int z) {
        /*
         * Create a single chunk of terrain given the coordinates of the terrain
         */

        /* Create a new TerrainChunk and link it's settings and position */
        TerrainChunk newChunk = new TerrainChunk(settings, x, z);
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


    /* ----------- Collections Functions ------------------------------------------------------------- */

    private void RemoveChunksFromList() {
        /*
         * Take the chunksToRemove hashset and remove the chunks that can be removed. 
         */

        List<Vector2> removedChunks = chunksToRemove.ToList();

        foreach(Vector2 key in removedChunks) {

            /* Remove the chunk once it is fully loaded */
            if(loadedChunks.ContainsKey(key)) {
                loadedChunks[key].Remove();
                loadedChunks.Remove(key);
                chunksToRemove.Remove(key);
            }

            /* The chunk has not yet been loaded, so it's save to remove them */
            else if(ChunksBeingGenerated.ContainsKey(key)) {
                ChunksBeingGenerated[key].Remove();
                ChunksBeingGenerated.Remove(key);
                chunksToRemove.Remove(key);
            }
        }
    }

    private void GenerateChunkFromList() {
        /*
         * Given the chunks in ChunksBeingGenerated, generate and load their terrain
         */
        var newChunks = ChunksBeingGenerated.ToList();

        /* Create the terrain for the chunk */
        foreach(var chunk in newChunks) {

            /* Generate and load the chunk into the game */
            chunk.Value.CreateTerrain();

            /* Place the chunk into the LoadedChunks collection */
            ChunksBeingGenerated.Remove(chunk.Key);
            loadedChunks.Add(chunk.Key, chunk.Value);
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
        
        if(loadedChunks.ContainsKey(key) || ChunksBeingGenerated.ContainsKey(key)) {
            canBeRemoved = true;
        }

        return canBeRemoved;
    }

    public bool CanAddChunk(Vector2 key) {
        /*
         * Determine if the chunk given by the key can be added to the ChunksBeingGenerated collection
         */
        bool canBeAdded = false;

        if(!(loadedChunks.ContainsKey(key) || ChunksBeingGenerated.ContainsKey(key))) {
            canBeAdded = true;
        }

        return canBeAdded;
    }
}
