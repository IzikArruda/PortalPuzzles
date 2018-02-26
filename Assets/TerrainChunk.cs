using UnityEngine;
using System.Threading;

public class TerrainChunk {

    /* Coordinates of the chunk */
    public int X;
    public int Z;

    /* The terrain of the chunk */
    public Terrain terrain;
    private TerrainChunkSettings Settings;
    private float[,] heightMap;
    private float[,,] splatMap;
    private TerrainData terrainData;
    private SplatPrototype[] biomeSplatMaps;

    /* Use this lock to indicate whether this object is creating it's maps */
    private object heightMapThreadLock;
    private object terrainMapThreadLock;

    /* Use this to find the height of the terrain */
    private NoiseProvider noiseProvider;
    
    /* Sizes of the terrain's alpha map. Used for tracking purposes. */
    private int alphaRes;
    private int alphaHeight;
    private int alphaWidth;
    

    /* ----------- Constructor Functions ------------------------------------------------------------- */

    public TerrainChunk(TerrainChunkSettings settings, NoiseProvider noise, Vector2 key) {
        /*
         * Create a new terrainChunk with the given parameters
         */

        heightMapThreadLock = new object();
        terrainMapThreadLock = new object();
        Settings = settings;
        noiseProvider = noise;
        X = (int) key.x;
        Z = (int) key.y;
        terrainData = new TerrainData();
        terrainData.heightmapResolution = Settings.HeightmapResolution;
        terrainData.alphamapResolution = Settings.AlphamapResolution;
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
        float[,] newHeightMap = new float[Settings.HeightmapResolution, Settings.HeightmapResolution];
        for(int z = 0; z < Settings.HeightmapResolution; z++) {
            for(int x = 0; x < Settings.HeightmapResolution; x++) {
                float lengthModifier = Settings.Length/1000f;
                float xCoord = lengthModifier*(X + (float) x / (Settings.HeightmapResolution - 1));
                float zCoord = lengthModifier*(Z + (float) z / (Settings.HeightmapResolution - 1));

                newHeightMap[z, x] = noiseProvider.GetNoise(xCoord, zCoord);
            }
        }

        heightMap = newHeightMap;
    }

    public void GenerateTextureMap() {
        /*
         * Create the texture map
         */

        ApplyTextures();
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
         
        /* Apply the heightMap to the terrain */
        terrainData.SetHeights(0, 0, heightMap);
        terrainData.size = new Vector3(Settings.Length, Settings.Height, Settings.Length);



        /* Create the splatMaps for the terrain's texture */
        alphaRes = terrainData.alphamapResolution;
        alphaHeight = terrainData.alphamapHeight;
        alphaWidth = terrainData.alphamapWidth;
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

        /* Apply the splat prototypes onto the terrain */
        terrainData.splatPrototypes = biomeSplatMaps;
        terrainData.RefreshPrototypes();
        /* Apply the textured splatMap to the terrain */
        terrainData.SetAlphamaps(0, 0, splatMap);

        /* Create the object that will contain the terrain components */
        GameObject newTerrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
        newTerrainGameObject.transform.position = new Vector3(X * Settings.Length, 0, Z * Settings.Length);
        newTerrainGameObject.transform.parent = Settings.chunkContainer;
        newTerrainGameObject.transform.name = "[" + X + ", " + Z + "]";
        
        /* Set the material of the terrain and it's stats */
        terrain = newTerrainGameObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 8;
        terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        terrain.materialType = UnityEngine.Terrain.MaterialType.Custom;
        terrain.materialTemplate = Settings.terrainMaterial;
        terrain.heightmapPixelError = 1;
        terrain.basemapDistance = CustomPlayerController.cameraFarClippingPlane;
        terrain.Flush();
    }

    private void ApplyTextures() {
        /*
         * Apply texture to the terrain data depending on the shape of the terrain.
         * Use the ratio of the given position's biome to determine which texture to use.
         * Also use the steepness of the terrain to determines the texture used.
         */

        /* Create a splat map prototype for each texture that will be used */
        int biomeTextureCount = Mathf.Min(noiseProvider.biomeRange.Length*2, Settings.terrainTextures.Length);
        biomeSplatMaps = new SplatPrototype[biomeTextureCount];
        for(int i = 0; i < biomeTextureCount; i++) {
            biomeSplatMaps[i] = new SplatPrototype();
        }

        /* Assign a texture in the settings to each splat prototype */
        for(int i = 0; i < biomeTextureCount; i++) {
            biomeSplatMaps[i].texture = Settings.terrainTextures[i];
        }







        /* Set the splatmap to switch textures as the terrain's steepness grows */
        float normX, normZ, steepness, normSteepness;
        float[] maxAngle = new float[] { 15, 10, 30, 45, 70 };
        float[,,] newSplatMap = new float[alphaRes, alphaRes, biomeTextureCount];
        for(int z = 0; z < alphaHeight; z++) {
            for(int x = 0; x < alphaWidth; x++) {
                normX = (float) x / (alphaWidth - 1);
                normZ = (float) z / (alphaHeight - 1);
                
                /* Each biome is assigned two textures. Switch between them depending in the steepness */
                float lengthModifier = Settings.Length/1000f;
                float xCoord = lengthModifier*(X + (float) x / (Settings.HeightmapResolution - 1));
                float zCoord = lengthModifier*(Z + (float) z / (Settings.HeightmapResolution - 1));
                float usedTextureRatio = 0;
                for(int i = 0; i < biomeTextureCount/2; i++) {

                    /* Get the steepness of the terrain at this given position. Each biome has a different stepRatio */
                    steepness = terrainData.GetSteepness(normX, normZ);
                    normSteepness = Mathf.Clamp((steepness/maxAngle[i]), 0f, 1f);

                    /* Get the ratio of the texture that will be used for this biome */
                    usedTextureRatio = noiseProvider.GetBiomeRatio(i, xCoord, zCoord);

                    /* Split the texture ratio across the two textures used by this biome relative to the steepness */
                    newSplatMap[z, x, i*2 + 0] = usedTextureRatio*(normSteepness);
                    newSplatMap[z, x, i*2 + 1] = usedTextureRatio*(1 - normSteepness);
                }
            }
        }

        splatMap = newSplatMap;
    }

    public void Remove() {
        /*
         * Delete this chunk of terrain
         */

        Settings = null;
        heightMap = null;
        splatMap = null;
        if(terrain != null) {
            Debug.Log(terrain.name);
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
