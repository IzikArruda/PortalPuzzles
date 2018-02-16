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

    private float pathWidth = 0.1f;
    private float pathMeldWidth = 0.65f;
    private float centerOffset = 0f;
    private float pathHeight = 0.25f;

    /* The sizes of each biome. It should have a sum of 1. */
    private float[] biomeRange = new float[] { 0.1f, 0.25f, 0.3f, 0.25f, 0.1f };
    //private float[] biomeRange = new float[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
    //private float[] biomeRange = new float[] { 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };

    /* Each type of biome used. The order of the biomeRange and this enum is important */
    private enum biomeType {
        Water = 0,
        Plains = 1,
        Hills = 2,
        Moutains = 3,
        HighMoutains = 4
    }

    /* How quickly a biome blends into another. Cannot be larger than the smallest range */
    private float blendSize = 0.05f;


    private bool test = true;
    
    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public NoiseProvider(float freq, int oct) {
        /*
         * Set the frequency and octave of the noise when this object is created
         */
        frequency = freq;
        octave = oct;

        /* Update the biomeRange values to reflect the blendSize */
        for(int i = 0; i < biomeRange.Length; i++) {

        }

        //Create a texture to show what the noise looks like
        CreateTextureOfNoise();
    }


    /* ----------- Main Noise Functions ------------------------------------------------------------- */

    public float GetNoise(float x, float z) {
        /*
         * Given a coordinate of X and Z, return the value of the noise function.
         * The function used to obtain the noise value depends on other mappings.
         */
        float noiseSum = 0;
        float noiseValue;
        float remainingNoiseRatio = 1;
        float usedNoiseRatio = 0;

        /* Extract the path's ratio value */
        usedNoiseRatio = GetPathRatio(x);
        //if(usedNoiseRatio > 0) {
        /* Get the path's noise value and apply it to the noiseSum */
        //    noiseValue = GetPathNoise(x, z);
        //    noiseSum += noiseValue*usedNoiseRatio;
        //    remainingNoiseRatio -= usedNoiseRatio;
        //}

        /* With what remains of the noiseRatio, use it on the default noise map */
        //if(remainingNoiseRatio > 0) {
        //    noiseSum += DefaultGetNoise(x, z)*remainingNoiseRatio;
        //}
        









        /* Get the noise ratio for each biome */
        float currentBiomeRatio = 0;
        float totalRatio = 0;
        noiseSum = 0;
        for(int i = 0; i < biomeRange.Length; i++) {
            currentBiomeRatio = GetBiomeRatio(i, x, z);
            totalRatio += currentBiomeRatio;

            /* This biome gets used, so add it's noise height to the current noise sum */
            if(currentBiomeRatio > 0) {
                noiseValue = GetBiomeNoise(i, x, z);
                noiseSum += noiseValue*currentBiomeRatio;
            }
        }
        

        /* Set the noise to 1 if the tallied sum of the biomes ratios do not collectively reach 1 */
        if(totalRatio < 0.9999f || totalRatio > 1.000001f) {
            noiseSum = 1f;
            Debug.Log(totalRatio);
        }
        else {
            noiseSum = 0f;
        }

        return noiseSum;
    }


    /* ----------- Path Functions ------------------------------------------------------------- */

    public float GetPathRatio(float x) {
        /*
         * The path's ratio map uses only one dimension (X). The path has a similar representation as:
         * [0, 0, 0, 0.25, 0.5, 0.75, 1, 1, 1, 1, 0.75, 0.50, 0.25, 0, 0, 0]
         * 
         * PathWidth gives how wide the full path is, ie the white/1 part of the map.
         * PathMeldWidth gives how much space it takes for the path to go from 1 to 0.
         * 
         * 1 is the shown value, but the value that is actually used in practice is maxRatioValue.
         */
        float ratioValue = 0;
        float maxRatioValue = 0.85f;

        if(x < centerOffset - pathWidth - pathMeldWidth || x > centerOffset + pathWidth + pathMeldWidth) {
            /* The given position is on the outside of the path*/
            ratioValue = 0;
        }

        else if(x < centerOffset - pathWidth || x > centerOffset + pathWidth) {
            /* The given position is on the [0, 1] part of the path */
            if(x < centerOffset) {
                ratioValue = maxRatioValue*(1 - (-(x + centerOffset + pathWidth)/pathMeldWidth));
            }
            else {
                ratioValue = maxRatioValue*(1 - ((x - centerOffset - pathWidth)/pathMeldWidth));
            }
        }

        else {
            /* The given position is directly on the path */
            ratioValue = maxRatioValue;
        }

        return ratioValue;
    }

    public float GetPathNoise(float x, float z) {
        /*
         * Get the noise value of the path map at the given coordinates. The path's noise height value 
         * will always be 1 as the main way to control the path is using the path's ratio map.
         */
        float noiseValue = 0;

        /* The path's noise height value will always be a set value */
        noiseValue = 0.25f;

        return noiseValue;
    }


    /* ----------- Biome Functions ------------------------------------------------------------- */
    
    public float GetBiomeRatio(int biomeType, float x, float z) {
        /*
         * Get the value of the biome ratio map. Each biome type uses the same map, but the
         * range of said map is determined by the given biomeType.
         */
         
        /* Get the ranges of the given biome */
        float rangeStart = 0;
        float rangeEnd = 0;
        for(int i = 0; i < biomeType; i++) {
            rangeStart += biomeRange[i];
        }
        rangeEnd = rangeStart + biomeRange[biomeType];
        

        //at the start
        if(rangeStart == 0) {
            rangeStart -= blendSize;
        }
        if(rangeEnd == 1f) {
            rangeEnd += blendSize;
        }




        /* Given the position of X and Z, find where in the range the noise value resides */
        float biomeFrequency = 8f;
        float noiseValue = RawNoise(x, z, biomeFrequency);
        
        /* The position is not within the biome's range */
        if(noiseValue < rangeStart - blendSize/2f || noiseValue > rangeEnd + blendSize/2f) {
            noiseValue = 0;
        }

        /* The position is on the lower blend range */
        else if(noiseValue < rangeStart + blendSize/2f) {
            noiseValue = ((noiseValue - (rangeStart - blendSize/2f))/blendSize);
        }

        /* The position is on the higher blend range */
        else if(noiseValue > rangeEnd - blendSize/2f) {
            noiseValue = 1 - ((noiseValue - (rangeEnd - blendSize/2f))/blendSize);
        }

        /* The position is within the flat range */
        else {
            noiseValue = 1;
        }
        
        return noiseValue;
    }
    
    public float GetBiomeNoise(int biomeType, float x, float z) {
        /*
         * Get the noise value/height of the given position of the given biome type.
         */
        float noiseValue;

        //For now, have each biome use a set single value for any position
        noiseValue = ((float) biomeType / (biomeRange.Length-1));

        return noiseValue;
    }

    /* ----------- Noise Functions ------------------------------------------------------------- */

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
        float noiseValue = Mathf.PerlinNoise(1000 + frequency*x, 1000 + frequency*z);
        
        /* For some reason, PerlinNoise can return values above 1 and bellow 0. Clamp the values. */
        noiseValue = Mathf.Clamp(noiseValue, 0, 1);

        return noiseValue;
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

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
