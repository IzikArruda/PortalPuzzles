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

    private TerrainChunkSettings chunkSettings;
    private float frequency;
    private int octave;

    private float pathWidth = 0.1f;
    private float pathMeldWidth = 0.65f;
    private float centerOffset = 0f;
    private float pathHeight = 0.25f;

    /* The maximum height of the terrain given by the TerrainController */
    private float maxTerrainHeight;

    /* The sizes of each biome. It should have a sum of 1. */
    public float[] biomeRange = new float[] { 0.10f, 0.25f, 0.3f, 0.25f, 0.10f };

    /* Each type of biome used. The order of the biomeRange and this enum is important */
    private enum biomeTypes {
        Water = 0,
        Plains = 1,
        Hills = 2,
        Moutains = 3,
        HighMoutains = 4
    }

    /* How quickly a biome blends into another. Cannot be larger than the smallest range */
    private float blendSize = 0.10f;

    /* Controls the size of biomes */
    private float biomeFrequency = 0.1f;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public NoiseProvider(float freq, int oct, float maxHeight, TerrainChunkSettings settings) {
        /*
         * Set the frequency and octave of the noise when this object is created.
         */
        frequency = freq;
        octave = oct;
        maxTerrainHeight = maxHeight;
        chunkSettings = settings;


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
        
        /* If the calculated ranges reach the end of the noise's range [0, 1], extend them so they do not blend */
        if(rangeStart == 0) {
            rangeStart -= blendSize;
        }
        if(rangeEnd == 1f) {
            rangeEnd += blendSize;
        }

        /* Given the position of X and Z, find where in the range the noise value resides */
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
        float noiseValue = 0;
        float maxRange = 0;
        float minRange = 0;
        //The highest points the bump can reach
        float bumpHeight = 0;
        //Increasing frequency will increase the amount of bumps/change height faster
        float bumpFrequency = 0;

        /* The Water biome is deep and flat */
        if(biomeType == (int) biomeTypes.Water) {
            maxRange = 0;
            minRange = 0;

            /* Basic bumps are used */
            bumpHeight = 15;
            bumpFrequency = 15;
        }

        /* The plains biome is more zoomed in than the other biomes */
        else if(biomeType == (int) biomeTypes.Plains) {
            maxRange = 0.50f;
            minRange = 0.15f;
            x = x/3f;
            z = z/3f;

            /* More flat bumps are used */
            bumpHeight = 10;
            bumpFrequency = 10;
        }

        /* The hills biome is a basic rolling hills terrain */
        else if(biomeType == (int) biomeTypes.Hills) {
            maxRange = 0.60f;
            minRange = 0.25f;

            /* bumps are larger */
            bumpHeight = 20;
            bumpFrequency = 10;
        }

        /* The moutain biomes are more zoomed out to be more sharp */
        else if(biomeType == (int) biomeTypes.Moutains) {
            maxRange = 0.80f;
            minRange = 0.45f;
            x = x*3f;
            z = z*3f;

            /* bumps are much larger */
            bumpHeight = 25;
            bumpFrequency = 15;
        }

        else if(biomeType == (int) biomeTypes.HighMoutains) {
            maxRange = 0.90f;
            minRange = 0.70f;

            /* bumps are much sharper */
            bumpHeight = 20;
            bumpFrequency = 35;
        }

        /* Force the noise value to be within the given range */
        noiseValue = DefaultGetNoise(x, z);
        noiseValue = noiseValue*(maxRange-minRange) + minRange;

        /* Apply another random noise value to make the ground seem much less even */
        noiseValue += bumpHeight*RawNoise(x, z, bumpFrequency)/maxTerrainHeight;

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

    public float GetHeightFromWorldPos(float x, float z) {
        /*
         * Given a position in the world, translate it to a format that can be handled similar to
         * how the terrainChunks parse for terrain height.
         */
        
        /* Map the given coords relative to the heightmap resolution without using a chunk coordinate */
        float mapX = (x / chunkSettings.Length)*chunkSettings.HeightmapResolution;
        float mapZ = (z / chunkSettings.Length)*chunkSettings.HeightmapResolution;

        return GetHeightFromChunkCoords(0, 0, mapX, mapZ);
    }

    public float GetHeightFromChunkCoords(int chunkX, int chunkZ, float mapX, float mapZ) {
        /*
         * Given the coordinates of a chunk and the position in it, return the height of the terrain.
         * 
         * chunkX and chunkZ are the coordinates of the chunk.
         * mapX and mapZ are the coordinates within the chunk's heightMap.
         */

        float lengthModifier = chunkSettings.Length/chunkSettings.terrainStretch;
        float xCoord = lengthModifier*(chunkX + (float) mapX / (chunkSettings.HeightmapResolution - 1));
        float zCoord = lengthModifier*(chunkZ + (float) mapZ / (chunkSettings.HeightmapResolution - 1));

        return GetNoise(xCoord, zCoord);
    }
}
