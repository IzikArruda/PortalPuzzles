using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/*
 * This script handles all data involved in the terrain chunks
 */
public class ChunkCache {

    /* How many chunks that can be generating at the same time */
    public readonly int maxChunkThreads = 1;
    
    /* All chunks that will be generated but have not yet been handled */
    public Dictionary<Vector2, TerrainChunk> chunksToBeGenerated;
    
    /* All chunks that must undergo generation to be properly loaded */
    public Dictionary<Vector2, TerrainChunk> chunksBeingGenerated;

    /* All chunks that have been loaded */
    public Dictionary<Vector2, TerrainChunk> loadedChunks;

    /* A hashset of the chunks that need to be removed */
    public HashSet<Vector2> chunksToRemove;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public ChunkCache() {
        /*
         * Upon creation, initialize the required variables used to track the data
         */
         
        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
        chunksToBeGenerated = new Dictionary<Vector2, TerrainChunk>();
        chunksBeingGenerated = new Dictionary<Vector2, TerrainChunk>();
        chunksToRemove = new HashSet<Vector2>();
    }


    /* ----------- Update Functions ------------------------------------------------------------- */

    public void UpdateCache() {
        /*
         * Update the terrain by going through the cache's collections
         */

        /* Remove any chunks that must be removed */
        RemoveChunks();

        /* Start generating the height map for chunks if possible */
        StartGeneratingHeightMaps();

        /* Create the terrain for chunks that have finished generating their height map */
        CreateTerrainForGeneratedChunks();
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

    private void RemoveChunks() {
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
            else if(chunksToBeGenerated.ContainsKey(key)) {
                chunksToBeGenerated[key].Remove();
                chunksToBeGenerated.Remove(key);
                chunksToRemove.Remove(key);
            }

            /* If the chunk is not being generated (which it cant be removed from), then the chunk has already been removed */
            else if(!chunksBeingGenerated.ContainsKey(key)) {
                chunksToRemove.Remove(key);
            }
        }
    }

    public void StartGeneratingHeightMaps() {
        /*
         * Start generating the height map for certain chunks. If the thread count allows it, 
         * move chunks from the toBeGenerated to the beingGenerated collection.
         */

        /* Check if we can add atleast one more chunk to the beingGenerated dictionary */
        if(chunksToBeGenerated.Count() > 0 && chunksBeingGenerated.Count() < maxChunkThreads) {

            /* Get enough chunks to fill the chunk generation thread */
            var chunksToGenerate = chunksToBeGenerated.Take(maxChunkThreads - chunksBeingGenerated.Count());

            /* Add each chunk to the beingGenerated collection and start generating their heightMap */
            foreach(var chunk in chunksToGenerate) {
                chunksBeingGenerated.Add(chunk.Key, chunk.Value);
                chunksToBeGenerated.Remove(chunk.Key);
                chunk.Value.GenerateHeightMap();
            }
        }
    }

    private void CreateTerrainForGeneratedChunks() {
        /*
         * Given a list of chunks currently being generated, check if any chunks have finished generating.
         * Any chunks that generating have their terrain created then moved to the loadedChunks collection.
         */
        var chunks = chunksBeingGenerated.ToList();

        /* Check each chunk if they have finished generation */
        foreach(var chunk in chunks) {
            if(chunk.Value.IsHeightmapReady()) {

                /* Create the chunk's terrain and move it to the loadedChunks dictionary */
                chunk.Value.CreateTerrain();
                loadedChunks.Add(chunk.Key, chunk.Value);
                chunksBeingGenerated.Remove(chunk.Key);
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
        
        if(
                loadedChunks.ContainsKey(key) || 
                chunksBeingGenerated.ContainsKey(key) || 
                chunksToBeGenerated.ContainsKey(key)) {
            canBeRemoved = true;
        }

        return canBeRemoved;
    }

    public bool CanAddChunk(Vector2 key) {
        /*
         * Determine if the chunk given by the key can be added to the chunksToBeGenerated collection
         */
        bool canBeAdded = false;

        if(!(
                loadedChunks.ContainsKey(key) || 
                chunksBeingGenerated.ContainsKey(key) || 
                chunksToBeGenerated.ContainsKey(key))) {
            canBeAdded = true;
        }

        return canBeAdded;
    }
}
