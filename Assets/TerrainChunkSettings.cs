using UnityEngine;
using System.Collections;

/*
 * Tracks the settings of a chunk. This script gets linked to many chunk objects upon their creation
 */
public class TerrainChunkSettings {
    
    public int HeightmapResolution;
    public int AlphamapResolution;
    public int Length;
    public int Height;
    public Transform chunkContainer;
    public Material terrainMaterial;
    public Texture2D flatTexture;
    public Texture2D steepTexture;


    public void SetSettings(int mapResolution, int length, int height, Transform container, 
            Material terrain, Texture2D flatTerrain, Texture2D steepTerrain) {
        /*
         * Set the settings of this script to the given values
         */

        HeightmapResolution = mapResolution;
        AlphamapResolution = mapResolution;
        Length = length;
        Height = height;
        chunkContainer = container;
        flatTexture = flatTerrain;
        steepTexture = steepTerrain;
    }
}
