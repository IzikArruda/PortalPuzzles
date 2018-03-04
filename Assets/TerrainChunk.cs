using UnityEngine;
using System.Threading;
using System.Collections;

public class TerrainChunk : MonoBehaviour{

    /* Coordinates of the chunk */
    public int X;
    public int Z;

    /* The terrain of the chunk */
    public Terrain terrain;
    private TerrainChunkSettings settings;
    private float[,] heightMap;
    private float[,,] splatMap;
    private TerrainData terrainData;
    private SplatPrototype[] biomeSplatMaps;

    /* Use this lock to indicate whether this object is creating it's maps */
    private object heightMapThreadLock;
    private object terrainMapThreadLock;

    /* Use this to find the height of the terrain */
    private NoiseProvider noiseProvider;
    
    /* All coroutines */
    private IEnumerator coroutines;
    private Coroutine steepnessRoutine;

    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public TerrainChunk(TerrainChunkSettings chunkSettings, NoiseProvider noise, Vector2 key) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        heightMapThreadLock = new object();
        terrainMapThreadLock = new object();
        settings = chunkSettings;
        noiseProvider = noise;
        X = (int) key.x;
        Z = (int) key.y;
        terrainData = new TerrainData();
        terrainData.heightmapResolution = settings.HeightmapResolution;
        terrainData.alphamapResolution = settings.AlphamapResolution;
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

                newHeightMap[z, x] = noiseProvider.GetNoise(xCoord, zCoord);
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
                    usedTextureRatio = noiseProvider.GetBiomeRatio(i, xCoord, zCoord);
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

        return (terrain == null && heightMap != null);
    }

    public bool IsTerrainMapReady() {
        /*
         * Return true if the terrain map is fully generated and the terrain has not yet been generated
         */

        return (terrain == null && splatMap != null);
    }

    public void SetupTextureMap() {
        /*
         * Set up values for the texture map so it can properly texture the terrain.
         * This will run after the height map is generated and before the texture generation starts.
         */
        System.DateTime before = System.DateTime.Now;





        /* Apply the heightMap to the terrain */
        terrainData.SetHeights(0, 0, heightMap);
        terrainData.size = new Vector3(settings.Length, settings.Height, settings.Length);




        System.DateTime after = System.DateTime.Now;
        System.TimeSpan duration = after.Subtract(before);
        Debug.Log("Texturemap setup: " + duration.Milliseconds);
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    public void ForceLoad() {
        /*
         * Force the main thread to load the given chunk without using threads
         */

        GenerateHeightMap();
        SetupTextureMap();
        GenerateTextureMap();
        CreateObject();
    }

    public void SetChunkCoordinates(int x, int z) {
        /*
         * Set the coordinates of where the chunk is placed in the noise function
         */

        X = x;
        Z = z;
    }
    
    public void CreateObject() {
        /*
         * Create the gameObject of the terrain. Runs after the height and texture maps have been loaded.
         */
        System.DateTime after = System.DateTime.Now;
        System.DateTime before = System.DateTime.Now;
        System.TimeSpan duration = after.Subtract(before);




        /* Get the steepness of the terrain and adjust the terrain's specific textures depending on it */
        steepnessRoutine = StartCoroutine(SteepnessCo());



        after = System.DateTime.Now;
        duration = after.Subtract(before);
        Debug.Log("steepness " + duration.Milliseconds);
        before = System.DateTime.Now;




        /* Apply the splat prototypes onto the terrain */
        terrainData.splatPrototypes = biomeSplatMaps;
        terrainData.RefreshPrototypes();
        terrainData.SetAlphamaps(0, 0, splatMap);






        after = System.DateTime.Now;
        duration = after.Subtract(before);
        Debug.Log("splats " + duration.Milliseconds);
        before = System.DateTime.Now;




        /* Create the object that will contain the terrain components */
        GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
        newTerrainGameObject.transform.position = new Vector3(X * settings.Length, 0, Z * settings.Length);
        newTerrainGameObject.transform.parent = settings.chunkContainer;
        newTerrainGameObject.transform.name = "[" + X + ", " + Z + "]";
        newTerrainGameObject.layer = settings.terrainLayer;






        after = System.DateTime.Now;
        duration = after.Subtract(before);
        Debug.Log("object " + duration.Milliseconds);
        before = System.DateTime.Now;




        /* Set the material of the terrain and it's stats */
        terrain = newTerrainGameObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 4;
        terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        terrain.castShadows = false;
        terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
        terrain.materialTemplate = settings.terrainMaterial;
        terrain.basemapDistance = CustomPlayerController.cameraFarClippingPlane;
        terrain.Flush();


        after = System.DateTime.Now;
        duration = after.Subtract(before);
        Debug.Log("terrain " + duration.Milliseconds);
    }

    IEnumerator SteepnessCo() {
        yield return new ApplyTerrainSteepness();
    }

    private IEnumerator ApplyTerrainSteepness() {
        /*
         * Go through the terrain's vertices and adjust it's splatMaps depending on the steepness of each vert.
         */
        int biomeTextureCount = Mathf.Min(noiseProvider.biomeRange.Length*2, settings.terrainTextures.Length);

        /* Set the splatmap to switch textures as the terrain's steepness grows */
        float normX, normZ, steepness, normSteepness;
        float[] maxAngle = new float[] { 15, 10, 30, 45, 70 };
        float[,,] newSplatMap = splatMap;
        for(int z = 0; z < settings.AlphamapResolution; z++) {
            for(int x = 0; x < settings.AlphamapResolution; x++) {

                normX = (float) x / (settings.AlphamapResolution - 1);
                normZ = (float) z / (settings.AlphamapResolution - 1);
                for(int i = 0; i < biomeTextureCount/2; i++) {

                    /* Get the steepness of the terrain at this given position. Each biome has a different stepRatio */
                    steepness = terrainData.GetSteepness(normX, normZ);
                    normSteepness = Mathf.Clamp(steepness/maxAngle[i], 0f, 1f);

                    /* Split the texture ratio across the two textures used by this biome relative to the steepness */
                    newSplatMap[z, x, i*2 + 0] = newSplatMap[z, x, i*2 + 0]*(normSteepness);
                    newSplatMap[z, x, i*2 + 1] = newSplatMap[z, x, i*2 + 1]*(1 - normSteepness);
                }
            }
        }

        splatMap = newSplatMap;
        Debug.Log("Finishes steepness");
        yield return null;
    }

    public void Remove() {
        /*
         * Delete this chunk of terrain
         */

        settings = null;
        heightMap = null;
        splatMap = null;
        if(terrain != null) {
            GameObject.Destroy(terrain.gameObject);
        }
    }

    public void SetNeighbors(TerrainChunk Xn, TerrainChunk Zp, TerrainChunk Xp, TerrainChunk Zn) {
        /*
         * Set the neighbors of this chunk. This is to ensure the chunks are properly connected.
         */
        Terrain left = null;
        Terrain up = null;
        Terrain right = null;
        Terrain down = null;

        if(Xn != null) {
            left = Xn.terrain;
        }
        if(Zp != null) {
            up = Zp.terrain;
        }
        if(Xp != null) {
            right = Xp.terrain;
        }
        if(Zn != null) {
            down = Zn.terrain;
        }

        terrain.SetNeighbors(left, up, right, down);
        terrain.Flush();
    }



    /* ----------- Helper Functions ------------------------------------------------------------- */

    public Vector2 GetChunkCoordinates() {
        /*
         * Return the coordinates of this chunk. This serves as it's key in the chunk dicitionaries
         */

        return new Vector2(X, Z);
    }
}
