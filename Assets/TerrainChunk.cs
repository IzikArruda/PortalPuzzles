using UnityEngine;
using System.Threading;
using System.Collections;

public class TerrainChunk : MonoBehaviour{

    /* Each terrainChunk shares the same settings and noise provider */
    public TerrainChunkSettings settings;

    /* Coordinates of the chunk */
    public int X;
    public int Z;

    /* The terrain of the chunk */
    public Terrain terrain;
    public float[,] heightMap;
    public float[,,] splatMap;
    public TerrainData terrainData;
    public SplatPrototype[] biomeSplatMaps;

    /* Use this lock to indicate whether this object is creating it's maps */
    private object heightMapThreadLock;
    private object terrainMapThreadLock;

    /* Use this to find the height of the terrain */
    public NoiseProvider noise;

    /* The steepness of the terrain of each biome */
    private float[] maxAngle = new float[] { 15, 10, 30, 45, 70 };

    /* The coroutine container for when calculating the steepness in realtime */
    private IEnumerator coroutines;


    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public void Constructor(TerrainChunkSettings chunkSettings, NoiseProvider noiseProvider) {
        /*
         * Create a new terrainChunk with the given parameters. This will also set universal values that 
         * will be used no matter this chunk's key, such as terrainData's size and it's SplatPrototype reference.
         */
        settings = chunkSettings;
        noise = noiseProvider;

        /* Setup the gameObject */
        gameObject.transform.parent = settings.chunkContainer;
        gameObject.layer = settings.terrainLayer;

        /* Create the lock objects */
        heightMapThreadLock = new object();
        terrainMapThreadLock = new object();

        /* Setup the terrainData object */
        terrainData = new TerrainData();
        terrainData.heightmapResolution = settings.HeightmapResolution;
        terrainData.alphamapResolution = settings.AlphamapResolution;
        gameObject.AddComponent<Terrain>().terrainData = terrainData;
        gameObject.AddComponent<TerrainCollider>().terrainData = terrainData;
        terrainData.size = new Vector3(settings.Length, settings.Height, settings.Length);
        
        /* Setup the terrain object */
        terrain = GetComponent<Terrain>();
        SetTerrainStats();

        /* Create a splat map prototype for each texture that will be used */
        int biomeTextureCount = Mathf.Min(noiseProvider.biomeRange.Length*2, settings.terrainTextures.Length);
        biomeSplatMaps = new SplatPrototype[biomeTextureCount];
        for(int i = 0; i < biomeTextureCount; i++) {
            biomeSplatMaps[i] = new SplatPrototype();
        }

        /* Assign a texture in the settings to each splat prototype */
        for(int i = 0; i < biomeTextureCount; i++) {
            biomeSplatMaps[i].texture = settings.terrainTextures[i];
        }
        terrainData.splatPrototypes = biomeSplatMaps;
        terrainData.RefreshPrototypes();
    }

    public void SetKey(Vector2 key) {
        /*
         * Set the key of this chunk, which represents it's X and Z coordinate.
         * This is usually run when an unloaded chunk starts to load in.
         */
         
        X = (int) key.x;
        Z = (int) key.y;
        gameObject.transform.parent = settings.chunkContainer;
        gameObject.transform.position = new Vector3(X * settings.Length, 0, Z * settings.Length);
        gameObject.transform.name = "[" + X + ", " + Z + "]";
        gameObject.SetActive(true);
    }
    

    /* ----------- Map Generation Functions ------------------------------------------------------------- */

    public void GenerateHeightMapRequest() {
        /*
         * Start the thread that generates the height map
         */

        Thread thread = new Thread(GenerateHeightMapThread);
        thread.Start();
    }

    public void GenerateTextureMapRequest() {
        /*
         * Start the thread that generates the texture map
         */

        Thread thread = new Thread(GenerateTextureMapThread);
        thread.Start();
    }

    private void GenerateHeightMapThread() {
        /*
         * Generate the height map for this terrainChunk within a thread and a lock.
         */

        /* Lock the thread until it fully generates the height map */
        lock(heightMapThreadLock) {

            /* Generate the heightMap for the terrainData */
            GenerateHeightMap();
        }
    }

    private void GenerateTextureMapThread() {
        /*
         * Generate the texture map for this terrainChunk within a thread and a lock.
         * This requires the heightMap to be generated.
         */

        /* Lock the thread until it fully generates the texture map */
        lock(terrainMapThreadLock) {

            /* Generate the texture map */
            GenerateTextureMap();
        }
    }

    public void GenerateHeightMap() {
        /*
         * Generate the heightMap for this terrainChunk
         */
         
        /* Populate the heightMap by going through it's resolution */
        float[,] newHeightMap = new float[settings.HeightmapResolution, settings.HeightmapResolution];
        for(int z = 0; z < settings.HeightmapResolution; z++) {
            for(int x = 0; x < settings.HeightmapResolution; x++) {
                float lengthModifier = settings.Length/1000f;
                float xCoord = lengthModifier*(X + (float) x / (settings.HeightmapResolution - 1));
                float zCoord = lengthModifier*(Z + (float) z / (settings.HeightmapResolution - 1));

                newHeightMap[z, x] = noise.GetNoise(xCoord, zCoord);
            }
        }

        heightMap = newHeightMap;
    }

    public void GenerateTextureMap() {
        /*
         * Apply texture to the terrain data depending on the shape of the terrain.
         * Use the ratio of the given position's biome to determine which texture to use.
         * Use two splatMaps for each biomes and set the ratio for both of them.
         * Later, a function will pass through and will change the splatMap values depending on the steepness.
         */
        int biomeTextureCount = Mathf.Min(noise.biomeRange.Length*2, settings.terrainTextures.Length);
        
        /* Cycle through each vertices of the terrain, getting which splatMaps have what values due to biome ratios */
        float usedTextureRatio, lengthModifier, xCoord, zCoord;
        float[] maxAngle = new float[] { 15, 10, 30, 45, 70 };
        float[,,] newSplatMap = new float[settings.AlphamapResolution, settings.AlphamapResolution, biomeTextureCount];
        for(int z = 0; z < settings.AlphamapResolution; z++) {
            for(int x = 0; x < settings.AlphamapResolution; x++) {

                /* Each biome is assigned two textures. Switch between them depending in the steepness */
                lengthModifier = settings.Length/1000f;
                xCoord = lengthModifier*(X + (float) x / (settings.AlphamapResolution - 1));
                zCoord = lengthModifier*(Z + (float) z / (settings.AlphamapResolution - 1));
                for(int i = 0; i < biomeTextureCount/2; i++) {

                    /* Get the ratio of the texture that will be used for this biome and apply it to the splatMap */
                    usedTextureRatio = noise.GetBiomeRatio(i, xCoord, zCoord);
                    newSplatMap[z, x, i*2 + 0] = usedTextureRatio;
                    newSplatMap[z, x, i*2 + 1] = usedTextureRatio;
                }
            }
        }

        splatMap = newSplatMap;
    }

    public bool IsHeightmapReady() {
        /*
         * Return true if the height map is fully generated and the terrain has not yet been generated
         */

        return (heightMap != null);
    }

    public bool IsTerrainMapReady() {
        /*
         * Return true if the terrain map is fully generated and the terrain has not yet been generated
         */

        return (splatMap != null);
    }

    public void SetupTextureMap() {
        /*
         * Set up values for the texture map so it can properly texture the terrain.
         * This will run after the height map is generated and before the texture generation starts.
         */
        System.DateTime before = System.DateTime.Now;





        /* Apply the heightMap to the terrain */
        terrainData.SetHeights(0, 0, heightMap);




        System.DateTime after = System.DateTime.Now;
        System.TimeSpan duration = after.Subtract(before);
        //Debug.Log("Texturemap setup: " + duration.Milliseconds);
    }


    /* ----------- Terrain Texture Functions ------------------------------------------------------------- */

    private IEnumerator ApplyTerrainSteepnessCoroutine() {
        /*
         * Go through the terrain's vertices and adjust it's splatMaps depending on the steepness of each vert.
         * This will be done as a coroutine, so use yield statements to not run for too long.
         */
        int biomeTextureCount = Mathf.Min(noise.biomeRange.Length*2, settings.terrainTextures.Length);
        int stopCount = 1;
        int coroutineLoops = 64;
        float currLimit = settings.AlphamapResolution/coroutineLoops;
        /* Set the splatmap to switch textures as the terrain's steepness grows */
        float[,,] newSplatMap = splatMap;
        for(int z = 0; z < settings.AlphamapResolution; z++) {

            for(int x = 0; x < settings.AlphamapResolution; x++) {
                UpdateTexturesToSplatmap(x, z, biomeTextureCount, ref newSplatMap);
            }

            /* Yield the generation once it has updated the textures of a certain amount of rows */
            if(z > stopCount*currLimit) {
                stopCount++;
                yield return null;
            }
        }

        /* Once the new splatMap is defined, we can apply it to the terrain */
        yield return null;
        splatMap = newSplatMap;
        terrainData.SetAlphamaps(0, 0, splatMap);
        terrain.Flush();

    }

    private void ApplyTerrainSteepness() {
        /*
         * Go through the terrain's vertices and adjust it's splatMaps depending on the steepness of each vert.
         */
        int biomeTextureCount = Mathf.Min(noise.biomeRange.Length*2, settings.terrainTextures.Length);
        
        /* Set the splatmap to switch textures as the terrain's steepness grows */
        float[,,] newSplatMap = splatMap;
        for(int z = 0; z < settings.AlphamapResolution; z++) {
            for(int x = 0; x < settings.AlphamapResolution; x++) {
                UpdateTexturesToSplatmap(x, z, biomeTextureCount, ref newSplatMap);
            }
        }

        /* Once the new splatMap is defined, we can apply it to the terrain */
        splatMap = newSplatMap;
        terrainData.SetAlphamaps(0, 0, splatMap);
        terrain.Flush();
    }

    private void UpdateTexturesToSplatmap(int x, int z, int biomeTextureCount, ref float[,,] splatmap) {
        /*
         * Given an X and Z position along with a splatMap, update the splatMap's values.
         * This function is purely to ensure ApplyTerrainSteepness and ApplyTerrainSteepnessCoroutine
         * accomplish the same thing by running the same function.
         */
        float normX = (float) x / (settings.AlphamapResolution );
        float normZ = (float) z / (settings.AlphamapResolution );
        


        float steepness, normSteepness;
        for(int i = 0; i < biomeTextureCount/2; i++) {

            /* Get the steepness of the terrain at this given position. Each biome has a different stepRatio */
            steepness = terrainData.GetSteepness(normX, normZ);
            normSteepness = Mathf.Clamp(steepness/maxAngle[i], 0f, 1f);

            if(x == settings.AlphamapResolution - 1 || z == settings.AlphamapResolution - 1) {
                /*normX = (float) x-1 / (settings.AlphamapResolution - 1);
                normZ = (float) z-1 / (settings.AlphamapResolution - 1);
                steepness = terrainData.GetSteepness(normX, normZ);
                normSteepness = Mathf.Clamp(steepness/maxAngle[i], 0f, 1f);*/
            }

            /* Split the texture ratio across the two textures used by this biome relative to the steepness */
            splatmap[z, x, i*2 + 0] = splatmap[z, x, i*2 + 0]*(normSteepness);
            splatmap[z, x, i*2 + 1] = splatmap[z, x, i*2 + 1]*(1 - normSteepness);
        }
    }
    
    private void SetTerrainStats() {
        /*
         * Set the stats of the terrain, such as the material and bumpmap distance
         */

        terrain.heightmapPixelError = 4;
        terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        terrain.castShadows = false;
        terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
        terrain.materialTemplate = settings.terrainMaterial;
        terrain.basemapDistance = CustomPlayerController.cameraFarClippingPlane;
        terrain.Flush();
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    public void ForceLoad() {
        /*
         * Force the main thread to load the given chunk without using threads
         */

        GenerateHeightMap();
        SetupTextureMap();
        GenerateTextureMap();
        ApplyTerrainSteepness();
    }
    
    public void UpdateTerrainLive() {
        /*
         * Update the terrain's splatmap using a coroutine to prevent frame loss. 
         * Runs after the height and texture maps have been loaded.
         */

        /* Start the coroutine which will set the splatmap of the terrain depending on the steepness */
        StartCoroutine(ApplyTerrainSteepnessCoroutine());
    }

    public void Remove() {
        /*
         * Delete certain values of this chunk, returning it to it's default state.
         */
         
        heightMap = null;
        splatMap = null;
        gameObject.name = "Inactive chunk";
        gameObject.SetActive(false);
    }

    public void SetNeighbors(TerrainChunk Xn, TerrainChunk Zp, TerrainChunk Xp, TerrainChunk Zn) {
        /*
         * Set the neighbors of this chunk. This is to ensure the chunks are properly connected.
         * Even if this chunk has not yet finished it's texture generation and splatmap adjusting,
         * we can still assign the terrain's 
         */
        Terrain left = null;
        Terrain up = null;
        Terrain right = null;
        Terrain down = null;

        if(Xn != null) {
            left = Xn.GetComponent<Terrain>();
        }
        if(Zp != null) {
            up = Zp.GetComponent<Terrain>();
        }
        if(Xp != null) {
            right = Xp.GetComponent<Terrain>();
        }
        if(Zn != null) {
            down = Zn.GetComponent<Terrain>();
        }

        /* Set the neighboors for the terrain to have a seamless connection */
        GetComponent<Terrain>().SetNeighbors(left, up, right, down);

        /* If the terrain value has not yet been set, then the terrain will already flush itself later */
        if(terrain != null) {
            GetComponent<Terrain>().Flush();
        }
    }
    

    /* ----------- Helper Functions ------------------------------------------------------------- */

    public Vector2 GetChunkCoordinates() {
        /*
         * Return the coordinates of this chunk. This serves as it's key in the chunk dicitionaries
         */

        return new Vector2(X, Z);
    }
}
