using UnityEngine;
using System.Collections;

/*
 * Tracks the settings of a chunk. This script gets linked to many chunk objects upon their creation
 */
public class TerrainChunkSettings {

    [HideInInspector]
    public int HeightmapResolution;
    [HideInInspector]
    public int AlphamapResolution;
    [HideInInspector]
    public int Length;
    [HideInInspector]
    public int Height;
    [HideInInspector]
    public Transform chunkContainer;


    public void SetSettings(int mapResolution, int length, int height, Transform container) {
        /*
         * Set the settings of this script to the given values
         */

        HeightmapResolution = mapResolution;
        AlphamapResolution = mapResolution;
        Length = length;
        Height = height;
        chunkContainer = container;
    }
}
