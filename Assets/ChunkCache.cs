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
    public Dictionary<Vector2, TerrainChunk> chunksGeneratingHeightMap;
    public Dictionary<Vector2, TerrainChunk> chunksFinishedHeightMaps;
    public Dictionary<Vector2, TerrainChunk> chunksGeneratingTextureMap;

    /* All chunks that have been loaded */
    public Dictionary<Vector2, TerrainChunk> loadedChunks;

    /* A hashset of the chunks that need to be removed */
    public HashSet<Vector2> chunksToRemove;

    /* All coroutines */
    private IEnumerator coroutines;
    

    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public ChunkCache() {
        /*
         * Upon creation, initialize the required variables used to track the data
         */
         
        loadedChunks = new Dictionary<Vector2, TerrainChunk>();
        chunksToBeGenerated = new Dictionary<Vector2, TerrainChunk>();
        chunksGeneratingHeightMap = new Dictionary<Vector2, TerrainChunk>();
        chunksFinishedHeightMaps = new Dictionary<Vector2, TerrainChunk>();
        chunksGeneratingTextureMap = new Dictionary<Vector2, TerrainChunk>();
        chunksToRemove = new HashSet<Vector2>();
    }


    /* ----------- Update Functions ------------------------------------------------------------- */

    public void UpdateCache() {
        /*
         * Update the terrain by going through the cache's collections
         */
        //Debug.Log(chunksToBeGenerated.ToList().Count + " " + chunksGeneratingHeightMap.ToList().Count + " " + chunksFinishedHeightMaps.ToList().Count + " " + chunksGeneratingTextureMap.ToList().Count + " " + loadedChunks.ToList().Count);
        //Debug.Log(chunksToRemove.ToList().Count);

        /* Remove any chunks that must be removed */
        RemoveChunks();

        /* Start generating the height map for chunks if possible */
        StartGeneratingHeightMaps();

        /* Check if any maps have finished generating their height maps */
        MoveGeneratedHeightMaps();

        /* Start generating the texture map for the chunks if possible */
        StartGeneratingTextureMaps();

        /* Check if any maps have finished generating their texture maps */
        MoveGeneratedTextureMaps();
    }

    public void ForceLoadChunk(TerrainChunk newChunk) {
        /*
         * Force the given chunk to be fully loaded, regardless of thread limits. This is
         * run at startup as the game will load everything first.
         */
         
        /* Create the entire chunk without using a thread */
        newChunk.ForceLoad();

        /* Add the chunk to the loadedChunks list */
        loadedChunks.Add(newChunk.GetChunkCoordinates(), newChunk);

        /* Set the neighbors of this chunk */
        SetChunkNeighborhood(newChunk);
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

            /* Remove the chunk if it's waiting to start generating it's texture map */
            else if(chunksFinishedHeightMaps.ContainsKey(key)) {
                chunksFinishedHeightMaps[key].Remove();
                chunksFinishedHeightMaps.Remove(key);
                chunksToRemove.Remove(key);
            }

            /* If the chunk is not being generated (cant remove a chunk whilst generating), then the chunk has already been removed */
            else if(!chunksGeneratingHeightMap.ContainsKey(key) && !chunksGeneratingTextureMap.ContainsKey(key)) {
                chunksToRemove.Remove(key);
            }
        }
    }

    public void StartGeneratingHeightMaps() {
        /*
         * Start generating the height map for certain chunks. If the thread count allows it, 
         * move chunks from the toBeGenerated to the beingGenerated collection.
         */

        /* Check if we can add atleast one more chunk to the generatingHeightMap dictionary */
        if(chunksToBeGenerated.Count() > 0 && chunksGeneratingHeightMap.Count() < maxChunkThreads) {

            /* Get enough chunks to fill the height generation thread */
            var chunksToGenerate = chunksToBeGenerated.Take(maxChunkThreads - chunksGeneratingHeightMap.Count());

            /* Start generating the chunk's height map and add it to chunksGeneratingHeightMap */
            foreach(var chunk in chunksToGenerate) {
                chunksGeneratingHeightMap.Add(chunk.Key, chunk.Value);
                chunksToBeGenerated.Remove(chunk.Key);
                chunk.Value.GenerateHeightMapRequest();
            }
        }
    }

    public void StartGeneratingTextureMaps() {
        /*
         * Start generating the texture map for chunks that have completed their heightMapGeneration
         */

        /* Check if we can add atleast one more chunk to the generatingTextureMap dictionary */
        if(chunksFinishedHeightMaps.Count() > 0 && chunksGeneratingTextureMap.Count() < maxChunkThreads) {

            /* Get enough chunks to fill the height generation thread */
            var chunksToGenerate = chunksFinishedHeightMaps.Take(maxChunkThreads - chunksGeneratingTextureMap.Count());

            /* Start generating the chunk's texture map and add it to chunksGeneratingTextureMap */
            foreach(var chunk in chunksToGenerate) {
                chunksGeneratingTextureMap.Add(chunk.Key, chunk.Value);
                chunksFinishedHeightMaps.Remove(chunk.Key);
                chunk.Value.GenerateTextureMapRequest();
            }
        }
    }

    private void MoveGeneratedHeightMaps() {
        /*
         * Check the maps currently generating their height maps and move any that have completed into the
         * chunksFinishedHeightMaps dictionary.
         */
        var chunks = chunksGeneratingHeightMap.ToList();

        /* Check each chunk whether they have finished their heightMap */
        foreach(var chunk in chunks) {
            if(chunk.Value.IsHeightmapReady()) {

                /* Initialize some values for the texture map before making it ready for texture generation */
                chunk.Value.SetupTextureMap();

                /* Move the chunk to chunksFinishedHeightMaps */
                chunksGeneratingHeightMap.Remove(chunk.Key);
                chunksFinishedHeightMaps.Add(chunk.Key, chunk.Value);
            }
        }
    }

    private void MoveGeneratedTextureMaps() {
        /*
         * Check for maps that have finished generating their texture map and create their gameObject.
         * Put them into the loadedChunks dictionary and set up their neighbooring chunks.
         */
        var chunks = chunksGeneratingTextureMap.ToList();

        /* Check each chunk whether they have finished their heightMap */
        foreach(var chunk in chunks) {
            if(chunk.Value.IsTerrainMapReady()) {

                /* Create the chunk's object and add it to the loaded chunks dictionary */
                //Time how long it takes to finish this set of actions
                System.DateTime after = System.DateTime.Now;

                chunk.Value.UpdateTerrainLive();
                chunksGeneratingTextureMap.Remove(chunk.Key);
                loadedChunks.Add(chunk.Key, chunk.Value);
                SetChunkNeighborhood(chunk.Value);

                System.DateTime before = System.DateTime.Now;
                System.TimeSpan duration = after.Subtract(before);
                //Debug.Log("Time to create chunk object: " + duration.Milliseconds);
            }
        }
    }

    
    /* ----------- Event Functions ------------------------------------------------------------- */

    void SetChunkNeighborhood(TerrainChunk chunk) {
        /*
         * Given a chunk, set the neighbors of it and it's neighbors' neighbors. 
         * This is done to complete the connection of the given chunk.
         */
        TerrainChunk neighbor;

        /* Set the neighbors of the given chunk and it's neighbors */
        SetChunkNeighbors(chunk);
        if(loadedChunks.TryGetValue(new Vector2(chunk.X, chunk.Z + 1), out neighbor)) {
            SetChunkNeighbors(neighbor);
        }
        if(loadedChunks.TryGetValue(new Vector2(chunk.X + 1, chunk.Z), out neighbor)) {
            SetChunkNeighbors(neighbor);
        }
        if(loadedChunks.TryGetValue(new Vector2(chunk.X, chunk.Z - 1), out neighbor)) {
            SetChunkNeighbors(neighbor);
        }
        if(loadedChunks.TryGetValue(new Vector2(chunk.X - 1, chunk.Z), out neighbor)) {
            SetChunkNeighbors(neighbor);
        }
    }

    void SetChunkNeighbors(TerrainChunk chunk) {
        /*
         * Set the neighbors of the given chunk.
         */
        TerrainChunk Xn, Zp, Xp, Zn;

        /* Either get the chunk neighbor if it exists or use a null value */
        if(!loadedChunks.TryGetValue(new Vector2(chunk.X - 1, chunk.Z), out Xn)) {
            Xn = null;
        }
        if(!loadedChunks.TryGetValue(new Vector2(chunk.X, chunk.Z + 1), out Zp)) {
            Zp = null;
        }
        if(!loadedChunks.TryGetValue(new Vector2(chunk.X + 1, chunk.Z), out Xp)) {
            Xp = null;
        }
        if(!loadedChunks.TryGetValue(new Vector2(chunk.X, chunk.Z - 1), out Zn)) {
            Zn = null;
        }

        chunk.SetNeighbors(Xn, Zp, Xp, Zn);
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    public List<Vector2> GetLoadedChunks() {
        /*
         * Return a list of the keys/positions of each chunk that is currently loaded into the game
         */

        return loadedChunks.Keys.ToList();
    }

    public List<Vector2> GetAllChunks() {
        /*
         * Return a list of all chunks that are loaded, being generated or to be generated
         */

        List<Vector2> allChunks = GetLoadedChunks().Union(chunksToBeGenerated.Keys.ToList()).ToList();
        allChunks = allChunks.Union(chunksGeneratingHeightMap.Keys.ToList()).ToList();
        allChunks = allChunks.Union(chunksFinishedHeightMaps.Keys.ToList()).ToList();
        allChunks = allChunks.Union(chunksGeneratingTextureMap.Keys.ToList()).ToList();

        return allChunks;
    }

    public bool CanRemoveChunk(Vector2 key) {
        /*
         * Return whether the chunk defined by the given key can be removed or not.
         * Return true if the key is loaded and not currently in the to be removed list.
         */
        bool canBeRemoved = false;
        
        if(
                loadedChunks.ContainsKey(key) ||
                chunksGeneratingHeightMap.ContainsKey(key) ||
                chunksFinishedHeightMaps.ContainsKey(key) ||
                chunksGeneratingTextureMap.ContainsKey(key) ||
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
                chunksGeneratingHeightMap.ContainsKey(key) ||
                chunksFinishedHeightMaps.ContainsKey(key) ||
                chunksGeneratingTextureMap.ContainsKey(key) ||
                chunksToBeGenerated.ContainsKey(key))) {
            canBeAdded = true;
        }

        return canBeAdded;
    }
}
