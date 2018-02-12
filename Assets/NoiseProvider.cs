using UnityEngine;
using System.Collections;

/*
 * Generate random noise given an X and Z coordinate. For now, simply parse the built-in PerlinNoise function.
 */
public class NoiseProvider {
    

    public float GetNoise(float x, float z) {
        /*
         * Given an X and Z coordinate, return the value of the noise function given the coordinates
         */

        return Mathf.PerlinNoise(x, z);
    }
}
