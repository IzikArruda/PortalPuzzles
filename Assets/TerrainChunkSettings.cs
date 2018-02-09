using UnityEngine;
using System.Collections;

/*
 * Tracks the settings of a chunk. This script gets linked to many chunk objects upon their creation.
 */
public class TerrainChunkSettings : MonoBehaviour {

    public int HeightmapResolution;
    public int AlphamapResolution;
    public int Length;
    public int Height;

    public void SetSettings(int mapResolution, int length, int height) {
        /*
         * Set the settings of this script to the given values
         */

        HeightmapResolution = mapResolution;
        AlphamapResolution = mapResolution;
        Length = length;
        Height = height;
    }
}
