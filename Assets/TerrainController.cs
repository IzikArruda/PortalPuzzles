using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
 * Used to control the creation of terrain using the TerrainChunk script.
 * Attach this to an empty object to turn it into a container for perlin noise generated terrain.
 */
public class TerrainController : MonoBehaviour {

    void Start() {
        /*
         * Create a large circle of terrain around the origin
         */

        List<Vector2> newChunks = GetVisibleChunksFromPosition(new Vector2(0, 0), 5);
        CreateTerrainChunks(newChunks);
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
        TerrainChunkSettings newChunkSettings = newChunkObject.AddComponent<TerrainChunkSettings>();

        /* Link the settings to the terrain chunk and generate new terrain */
        newChunk.LinkSettings(newChunkSettings);
        newChunk.GenerateTerrain(x, z);
    }

    void CreateTerrainChunks(List<Vector2> chunks) {
        /*
         * Create each chunk described by the given list
         */

        foreach(Vector2 chunk in chunks) {
            CreateTerrainChunk((int)chunk.x, (int)chunk.y);
        }
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
