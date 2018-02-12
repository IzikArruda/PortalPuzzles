using UnityEngine;
using System.Collections;

/*
 * Generate random noise given an X and Z coordinate. Uses a frequency and octave to add detail to the
 * built-in perlin noise generator. Can use multiple maps/layers to generate the noise.
 * 
 * Each extra layer to the overall noise uses a ratio map and a height map.
 * The ratio map determines how much of the layer/noise value is used, ranging from [0 = 0%, 1 = 100%].
 * The height map determines the actual noise/height value of the map.
 */
public class NoiseProvider {

    private float frequency;
    private int octave;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public NoiseProvider(float freq, int oct) {
        /*
         * Set the frequency and octave of the noise when this object is created
         */
        frequency = freq;
        octave = oct;

        //Create a texture to show what the noise looks like
        //CreateTextureOfNoise();
    }
    

    /* ----------- Noise Functions ------------------------------------------------------------- */

    public float GetNoise(float x, float z) {
        /*
         * Given a coordinate of X and Z, return the value of the noise function.
         * The function used to obtain the noise value depends on other mappings
         */
        float noiseSum = 0;
        float noiseValue;
        float remainingNoiseRatio = 1;
        float usedNoiseRatio = 0;
        
        /* Extract the path's ratio value */
        usedNoiseRatio = GetPathRatio(x);
        if(usedNoiseRatio > 0) {
            /* Get the path's noise value and apply it to the noiseSum */
            noiseValue = GetPathNoise(x, z, ref usedNoiseRatio);
            noiseSum += noiseValue*usedNoiseRatio;
            remainingNoiseRatio -= usedNoiseRatio;
        }
        
        /* With what remains of the noiseRatio, use it on the default noise map */
        if(remainingNoiseRatio > 0) {
            noiseSum += DefaultGetNoise(x, z)*remainingNoiseRatio;
        }
        
        return noiseSum;
    }
    
    public float GetPathRatio(float x) {
        /*
         * The path's ratio map uses only one dimension (X). The path has a similar representation as:
         * [0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1, 0.75, 0.50, 0.25, 0, 0, 0]
         * 
         * PathWidth gives how wide the full path is, ie the white/1 part of the map.
         * PathMeldWidth gives how much space it takes for the path to go from 1 to 0.
         */
        float ratioValue = 0;
        float pathWidth = 0.25f;
        float pathMeldWidth = 0.1f;
        float centerOffset = 0f;
        
        if(x < centerOffset - pathWidth - pathMeldWidth || x > centerOffset + pathWidth + pathMeldWidth) {
            /* The given position is on the outside of the path*/
            ratioValue = 0;
        }

        else if(x < centerOffset - pathWidth || x > centerOffset + pathWidth) {
            /* The given position is on the [0, 1] part of the path */
            if(x < centerOffset) {
                ratioValue = 1 - (-(x + centerOffset + pathWidth)/pathMeldWidth);
            }
            else {
                ratioValue = 1 - ((x - centerOffset - pathWidth)/pathMeldWidth);
            }
        }

        else {
            /* The given position is directly on the path */
            ratioValue = 1;
        }

        return ratioValue;
    }
    
    public float GetPathNoise(float x, float z, ref float ratioUsed) {
        /*
         * Get the noise value of the path map at the given coordinates.
         * For now, the path's value is simply 1 all over.
         */
        float noiseValue = 0;

        /* The path's noise height is completely flat at the highest point */
        noiseValue = 1;

        return noiseValue;
    }

    public float GetPathNoise(float x, float z) {
        /*
         * Return the noise value of the path map at the given position.
         * The heightMap of the path is 0.2f at every point
         */

        return 0.2f;
    }




    public float DefaultGetNoise(float x, float z) {
        /*
         * Given an X and Z coordinate, return the value of the noise function given the coordinates
         * Requires a frequency that zooms out the noise and an octave that adds detail at lower levels      
         */
        float tempFreq = frequency;
        float noiseSum = RawNoise(x, z, tempFreq);

        /* For every octave, add to the noise and increase the range respectively */
        float amplitude = 1;
        float range = 1;
        for(int i = 1; i < octave; i++) {
            tempFreq *= 2;
            amplitude *= 0.5f;
            range += amplitude;
            noiseSum += RawNoise(x, z, tempFreq)*amplitude;
        }
        
        return noiseSum / range;
    }



    public float RawNoise(float x, float z, float frequency) {
        /*
         * Return the raw value of the perlin noise at the given coordinates
         */

        return Mathf.PerlinNoise(frequency*x, frequency*z);
    }

    
    public void CreateTextureOfNoise() {
        /*
         * Create a plane with a noise function to show how the noise looks
         */

        /* Create the object and it's texture */
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Noise Texture object";
        quad.transform.position = new Vector3(0, 20, -20);
        int texRes = 256;
        Texture2D texture = new Texture2D(texRes, texRes, TextureFormat.RGB24, true);
        quad.GetComponent<MeshRenderer>().material.mainTexture = texture;

        /* Fill the texture with noise */
        for(int y = 0; y < texRes; y++) {
            for(int x = 0; x < texRes; x++) {
                float xCoord = (float) x / texRes;
                float yCoord = (float) y / texRes;
                texture.SetPixel(x, y, Color.white * GetNoise(xCoord, yCoord));
            }
        }
        texture.Apply();
    }
}
