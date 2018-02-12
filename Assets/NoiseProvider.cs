using UnityEngine;
using System.Collections;

/*
 * Generate random noise given an X and Z coordinate. Uses a frequency and octave to add detail to the
 * built-in perlin noise generator.
 */
public class NoiseProvider {

    private float frequency;
    private int octave;
    
    public NoiseProvider(float freq, int oct) {
        /*
         * Set the frequency and octave of the noise when this object is created
         */
        frequency = freq;
        octave = oct;

        //Create a texture to show what the noise looks like
        //CreateTextureOfNoise();
    }

    public float GetNoise(float x, float z) {
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
