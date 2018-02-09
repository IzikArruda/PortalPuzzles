using UnityEngine;
using System.Collections;

/*
 * Used to control the creation of terrain using the TerrainChunk script.
 * Attach this to an empty object to turn it into a container for perlin noise generated terrain.
 */
public class TerrainController : MonoBehaviour {

    void Start() {
        /*
         * For now, just create a single random terrain
         */
         
        CreateTerrainChunk();
    }
    

    void CreateTerrainChunk() {
        /*
         * Create a single chunk of terrain 
         */

        /* Create a new object and attach a TerrainChunk and it's settings */
        GameObject newChunkObject = new GameObject();
        newChunkObject.name = "Terrain Chunk";
        newChunkObject.transform.parent = transform;
        TerrainChunk newChunk = newChunkObject.AddComponent<TerrainChunk>();
        TerrainChunkSettings newChunkSettings = newChunkObject.AddComponent<TerrainChunkSettings>();

        /* Link the settings to the terrain chunk and generate new terrain */
        newChunk.LinkSettings(newChunkSettings);
        newChunk.GenerateTerrain(0, 0);
    }
}
