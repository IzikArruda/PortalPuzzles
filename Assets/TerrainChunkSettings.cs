using UnityEngine;
using System.Collections;

public class TerrainChunkSettings : MonoBehaviour {

    public int HeightmapResolution { get; private set; }
    public int AlphamapResolution { get; private set; }

    public int Length { get; private set; }
    public int Height { get; private set; }

    public void SetSettings(int mapResolution, int length, int height) {
        HeightmapResolution = mapResolution;
        AlphamapResolution = mapResolution;
        Length = length;
        Height = height;
    }
}
