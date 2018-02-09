using UnityEngine;
using System.Collections;

public class TerrainChunkSettings : MonoBehaviour {

    public int HeightmapResolution { get; private set; }
    public int AlphamapResolution { get; private set; }

    public int Length { get; private set; }
    public int Height { get; private set; }

    public TerrainChunkSettings(int resX, int resY, int size, int height) {
        HeightmapResolution = resX;
        AlphamapResolution = resY;
        Length = size;
        Height = height;
    }
}
