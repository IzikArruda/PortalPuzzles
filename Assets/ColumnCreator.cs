﻿using UnityEngine;
using System.Collections;

/*
 * Create a marble column using the given parameters. A column is divided into multiple parts.
 * For now, only create the base that will sandwich the pillar. The column is seperated into sections:
 * 
 * Base: The top and bottom of the column, a very simple shape that reaches the pillar's size limits.
 * The lowest and highest points are part of the base.
 */
[ExecuteInEditMode]
public class ColumnCreator : MonoBehaviour {

    /* The material used by the pillar */
    public Material columnMaterial;

    /* The seed used to set the random values used to define the column */
    public int seed;
    public bool newSeed;
    
    /* When this is true, the next update frame will recreate the column */
    public bool recreateObject;

    
    /* --- User Given Pillar Stats ------------------- */
    /* The pillar's total height. It will not reach above or bellow this value */
    public float totalHeight;

    /* The width of the base and it's min/max ratio controls the width of all over pillar elements */
    public float baseWidth;
    public float baseHeight;
    [Range(1, 0)]
    public float minWidthRatio;
    [Range(1, 2)]
    public float maxWidthRatio;


    /* --- Seed Given Pillar Stats ---------------------- */

    //Cylinder
    private float cylinderHeight;
    private float cylinderRadius;
    /* How much extra radius is applied to the cylinder by the bump */
    public float cylinderBumpRadius;
    /* How stretched the sin function will be when applying the bump */
    //default 1. 
    public float cylinderBumpStretch;
    /* An Offset applied to the center cylinder's bump amount */
    //only from 0 to 1
    public float cylinderBumpOffset;

    //Filler
    /* How the filler of the pillar is made. Each fillerStat is of the form of a vector3 where:
     * x = the form of the filler. 0 is circular, 1 is box
     * y = the (ratio of baseWidth) width for box, radius for circular.
     * z = nothing yet */
    public Vector3[] fillerStats;
    
    /* Ratio of how much of the center pillar will be combined filer (excluding base) */
    public float fillerHeightRatio;
    private float fillerHeight;


    /* ----------- Built-in Unity Functions ------------------------------------------------------------- */

    void Start() {
        /*
         * On startup, add the approrpiate components for the column if they have not yet been created
         */

        if(gameObject.GetComponent<MeshFilter>() == null) {
            gameObject.AddComponent<MeshFilter>();
        }
        if(gameObject.GetComponent<MeshRenderer>() == null) {
            gameObject.AddComponent<MeshRenderer>();
        }

        /* Update the material for the meshRenderer */
        if(gameObject.GetComponent<MeshRenderer>().sharedMaterial == null) {
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = columnMaterial;
        }

        /* Set a value to indicate the column needs to be created */
        recreateObject = true;
    }
    
    void Update() {
        /*
         * Check if any new values will force the column to update it's shape
         */

        /* Get a new seed value if needed. Getting a new seed will re-create the object. */
        if(newSeed) {
            seed = (int) (Random.value*999999);
            newSeed = false;
            recreateObject = true;
        }

        /* Re-create the column */
        if(recreateObject) {
            SetColumnStats();
            CreateColumn();
            recreateObject = false;
        }
    }


    /* ----------- Column Creation Functions ------------------------------------------------------------- */

    void CreateColumn() {
        /*
         * When the column needs to be created, this is the method that will build it
         */

        /* Remove the current mesh of the column */
        GetComponent<MeshFilter>().sharedMesh = null;

        /* Remove any colliders associated with the previous column model */
        DeleteColliders();

        /* Create the base of the column that sits on the y = 0 point and the highest point of the column */
        Vector3 bottomBase = new Vector3(0, baseHeight/2f, 0);
        Vector3 topBase = new Vector3(0, totalHeight - baseHeight/2, 0);
        CreateBox(bottomBase, baseHeight, baseWidth, baseWidth);
        CreateBox(topBase, baseHeight, baseWidth, baseWidth);

        /* Create a series of filler shapes that are placed between the base and the center cylinder */
        if(fillerHeight > 0) {
            CreateFiller(true);
            CreateFiller(false);
        }

        /* Create the cylinder center of the column */
        if(cylinderHeight > 0) {
            CreateCenterCylinder();
        }
    }
    
    void CreateCenterCylinder() {
        /*
         * Create the center cylinder of the pillar. The amount of vertices used to define the pillar 
         * increase as the pillar's height increases.
         */
          
        /* How much distance is between the points that form the pillar's center cylinder */
        float cylinderPointDistance = 0.2f;
        int pointCount = Mathf.CeilToInt(cylinderHeight/cylinderPointDistance);

        /* Create an array of points that represent the center cylinder */
        Vector3[] cylinderPoints = new Vector3[pointCount];
        for(int i = 0; i < pointCount; i++) {
            float width = cylinderRadius + cylinderBumpRadius*Mathf.Sin(cylinderBumpOffset + i*cylinderBumpStretch/(pointCount-1));
            float height = baseHeight + fillerHeight + i*(cylinderHeight/(pointCount-1));
            cylinderPoints[i] = new Vector3(width, height, 0);
        }
        
        CreateCircularMesh(cylinderPoints);
    }

    void CreateFiller(bool topBase) {
        /*
         * Create a series of shapes to be placed between the base and the main cylinder of the column.
         * The given boolean will be true if the filler is added on the bottom base instead of the top base.
         */
        float currentYPos;
        float fillerPartHeight;
        int directionAdjustment = 1;

        /* Change how the position will increase/decrease depending on if the filler is added above or bellow */
        if(topBase) {
            directionAdjustment = -1;
        }

        /* Get the position to be at the start of the selected filler */
        currentYPos = baseHeight + fillerHeight + cylinderHeight/2f + directionAdjustment*cylinderHeight/2f;
        
        /* Create each filler object */
        //fillerPartHeight = directionAdjustment*fillerHeight/2f;
        //currentYPos += fillerPartHeight/2f;
        //CreateFillerCircularMesh(currentYPos, fillerPartHeight, 25, 0.5f*baseWidth, 0.25f*baseWidth, 0, 1f);
        //currentYPos += fillerPartHeight/2f;
        
        //fillerPartHeight = directionAdjustment*fillerHeight/2f;
        //currentYPos += fillerPartHeight/2f;
        //CreateFillerBox(currentYPos, fillerPartHeight, baseWidth*0.9f, baseWidth*0.5f);
        //currentYPos += fillerPartHeight/2f;


        /* Create a filler box using the fillerStats. For now, each filler object will take up the entire fillerHeightt */
        for(int i = 0; i < fillerStats.Length; i++) {
            fillerPartHeight = directionAdjustment*fillerHeight;
            currentYPos += fillerPartHeight/2f;

            /* Create a circular mesh */
            if(fillerStats[i].x == 0) {
                float radiusRatio = fillerStats[i].y;
                CreateFillerCircularMesh(currentYPos, fillerPartHeight, 25, radiusRatio*baseWidth, 0*baseWidth, 0, 1f);
            }

            /* Create a box */
            else if(fillerStats[i].x == 1) {
                float boxWidthRatio = fillerStats[i].y;
                CreateFillerBox(currentYPos, fillerPartHeight, baseWidth*boxWidthRatio, baseWidth*boxWidthRatio);
            }

            currentYPos += fillerPartHeight/2f;
        }
    }

    void CreateFillerBox(float currentYPos, float fillerSectionHeigth, float boxTopWidth, float boxBottomWidth) {
        /*
         * Use the given variables to create a box that will be used as a filler mesh placed between
         * the base and the main cylinder of the column.
         * 
         * If the fillerSectionHeight is negative, then reverse the top and bottom widths as the box will be flipped.
         */
        float boxHeight;

        if(fillerSectionHeigth < 0) {
            float temp = boxTopWidth;
            boxTopWidth = boxBottomWidth;
            boxBottomWidth = temp;
        }
        
        /* Create a box using the given sizes */
        CreateBox(new Vector3(0, currentYPos, 0), Mathf.Abs(fillerSectionHeigth), boxTopWidth, boxBottomWidth);
    }

    void CreateFillerCircularMesh(float currentYPos, float fillerSectionHeigth, int vertexCount,
			float width, float maxBumpWidth, float startingRad, float radInc) {
        /*
         * Create a circular mesh using the given parameters. Use a sine function to add an offset
         * to the radius to allow bumbs (think of the outter half of a taurus).
         * Note we use width instead of radius because the sizes will most likely be relative to baseWidth.
         *
         * radius: the starting radius of the circular mesh for each vertex. The radius changes by a sine function.
         * maxBumpRadius: The extra width added sine function. 0 will prevent the radous from ever changing.
         * startingRad: What rad degrees the radius starts at from a 0 to 1 scale with 1 being PI*2.
         * radInc: How much the rad will increase over the section's height. 1 is equal to PI*2, ie the whole sine wave.
         */
        float sineWaveOffset;
        currentYPos -= Mathf.Abs(fillerSectionHeigth)/2f;

        /* If fillerSectionHeigth is negative, change the rad values to flip the filler mesh */
        if(fillerSectionHeigth < 0) {
            startingRad =+ radInc;
            radInc *= -1f;
        }
        
        /* Populate the vertices array for the circular filler mesh */
        Vector3[] roundedEdgeVertices = new Vector3[vertexCount];
        for(int i = 0; i < vertexCount; i++) {
        	sineWaveOffset = Mathf.Sin(startingRad*Mathf.PI*2f + radInc*(Mathf.PI*2f)*i/(float)(vertexCount-1));
            roundedEdgeVertices[i].x = (width + maxBumpWidth*sineWaveOffset)/2f;
            roundedEdgeVertices[i].y = currentYPos;
            roundedEdgeVertices[i].z = 0;
            currentYPos += Mathf.Abs(fillerSectionHeigth)/(vertexCount-1);
        }

        CreateCircularMesh(roundedEdgeVertices);
    }


    /* ----------- Mesh Setting Functions ------------------------------------------------------------- */

    void CreateBox(Vector3 origin, float boxHeight, float boxTopWidth, float boxBottomWidth) {
        /*
         * Create a box mesh using the given width and height that expands outward of the origin equally.
         * Used to create the square bases on the top and bottom of the column.
         */
        Mesh boxMesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UVs;
        int[] triangles;
        float currentHeight = origin.y - boxHeight/2f;

        /* Get the distance each vertex of the cube will be from it's center */
        float WT = boxTopWidth/2f;
        float WB = boxBottomWidth/2f;
        float H = boxHeight/2f;

        /* Get the vertices that make up the cube */
        vertices = new Vector3[] {
            //X+ plane
            new Vector3(WT, H, WT),
            new Vector3(WT, H, -WT),
            new Vector3(WB, -H, WB),
            new Vector3(WB, -H, -WB),
            //X- plane
            new Vector3(-WT, H, WT),
            new Vector3(-WT, H, -WT),
            new Vector3(-WB, -H, WB),
            new Vector3(-WB, -H, -WB),
            //Y+ plane
            new Vector3(WT, H, -WT),
            new Vector3(WT, H, WT),
            new Vector3(-WT, H, -WT),
            new Vector3(-WT, H, WT),
            //Y- plane
            new Vector3(WB, -H, -WB),
            new Vector3(WB, -H, WB),
            new Vector3(-WB, -H, -WB),
            new Vector3(-WB, -H, WB),
            //Z+ plane
            new Vector3(WT, H, WT),
            new Vector3(-WT, H, WT),
            new Vector3(WB, -H, WB),
            new Vector3(-WB, -H, WB),
            //Z- plane
            new Vector3(WT, H, -WT),
            new Vector3(-WT, H, -WT),
            new Vector3(WB, -H, -WB),
            new Vector3(-WB, -H, -WB)
        };
        //Adjust the vertices to be centered around the origin point
        for(int i = 0; i < vertices.Length; i++) {
            vertices[i] += origin;
        }

        /* Set up the polygons that form the cube */
        triangles = new int[] {
            //X+ plane
            2, 1, 0, 2, 3, 1,
            //X- plane
            4, 5, 6, 5, 7, 6,
            //Y+ plane
            10, 9, 8, 10, 11, 9,
            //Y- plane
            12, 13, 14, 13, 15, 14,
            //Z+ plane
            16, 17, 18, 17, 19, 18,
            //Z- plane
            22, 21, 20, 22, 23, 21
        };

        /* Set the UVs of the cube. Make sure the UV positions that get used 
         * are integer numbers to allow perfect looping of the texture. */
        /* Change the UV depending on if the box has the same height on top and bottom */
        float diffBot, diffTop, largest;
        if(boxBottomWidth == boxTopWidth) {
            WB = Mathf.Ceil(boxBottomWidth);
            WT = Mathf.Ceil(boxTopWidth);
            largest = WB;
            diffBot = 0;
            diffTop = 0;
        }
        /* Set the values depending on which width is the largest */
        else if(WT > WB) {
            WT = boxTopWidth;
            WB = boxBottomWidth;
            largest = WT;
            diffBot = WT - WB;
            diffTop = 0;
        }
        else {
            WT = boxTopWidth;
            WB = boxBottomWidth;
            largest = WB;
            diffTop = WB - WT;
            diffBot = 0;
        }
        UVs = new Vector2[] {
            //X+ plane
            new Vector2(WT + diffTop/2f, currentHeight + boxHeight),
            new Vector2(diffTop/2f, currentHeight + boxHeight),
            new Vector2(WB + diffBot/2f, currentHeight),
            new Vector2(diffBot/2f, currentHeight),
            //X- plane
            new Vector2(largest*2 + 0 + diffTop/2f, currentHeight + boxHeight),
            new Vector2(largest*2 + WT + diffTop/2f, currentHeight + boxHeight),
            new Vector2(largest*2 + diffBot/2f, currentHeight),
            new Vector2(largest*2 + WB + diffBot/2f, currentHeight),
            //Y+ plane
            new Vector2(-WT/2f, WT/2f),
            new Vector2(WT/2f, WT/2f),
            new Vector2(-WT/2f, -WT/2f),
            new Vector2(WT/2f, -WT/2f),
            //Y- plane
            new Vector2(WB/2f, WB/2f),
            new Vector2(-WB/2f, WB/2f),
            new Vector2(WB/2f, -WB/2f),
            new Vector2(-WB/2f, -WB/2f),
            //Z+ plane
            new Vector2(largest + 0 + diffTop/2f, currentHeight + boxHeight),
            new Vector2(largest + WT + diffTop/2f, currentHeight + boxHeight),
            new Vector2(largest + diffBot/2f, currentHeight),
            new Vector2(largest + WB + diffBot/2f, currentHeight),
            //Z- plane
            new Vector2(largest*3 + WT + diffTop/2f, currentHeight + boxHeight),
            new Vector2(largest*3 + 0 + diffTop/2f, currentHeight + boxHeight),
            new Vector2(largest*3 + WB + diffBot/2f, currentHeight),
            new Vector2(largest*3 + diffBot/2f, currentHeight)
        };
        
        /* Add the vertices, triangles and UVs to the column's current mesh */
        AddToMesh(vertices, triangles, UVs);



        /* Create a box collider mesh that represents this position */
        BoxCollider box1 = gameObject.AddComponent<BoxCollider>();
        box1.center = origin;
        float averageWidth = (boxBottomWidth + boxTopWidth)/2f;
        box1.size = new Vector3(averageWidth, boxHeight, averageWidth);
    }
    
    void CreateCircularMesh(Vector3[] vertexPoints) {
        /*
         * Create a mesh that rotates around the origin using each vertex's x distance as a radius.
         * The goal is to produce a cylinder that is closed off on both top and bottom.
         * circleVertexCount is the amount of vertexes used with one rotation around the Y axis.
         * sectionVertexCount is how many times this rotation around the Y axis will occur.
         */
        int circleVertexCount;
        int sectionVertexCount;
        Vector3[] vertices;
        
        /* Set the vertex counts. circle is the amount when rotating a point around the center
         * while section is the amount of vertexes needed on the same y axis to define the pillar */
        circleVertexCount = 20 +1;
        sectionVertexCount = vertexPoints.Length;

        /* Initialize the main vector array now that we know the amount of vectors to be used */
        vertices = new Vector3[(sectionVertexCount+2) * circleVertexCount + 2];


        /*
         * Set the vertices of the circular mesh
         */
        /* Get an array for each circle that rotates around the Y axis and add them to the main vertices array */
        for(int i = 0; i < sectionVertexCount; i++) {
            Vector3[] vectorCircle = GetCircularVertices(vertexPoints[i].x, vertexPoints[i].y, circleVertexCount);
            vectorCircle.CopyTo(vertices, circleVertexCount*i);
        }

        /* Set the vertices that are used to close off the top and bottom of the mesh */
        vertices[(sectionVertexCount+1)*circleVertexCount] = new Vector3(0, vertices[0].y, 0);
        vertices[(sectionVertexCount+2)*circleVertexCount + 1] = new Vector3(0, vertices[sectionVertexCount*circleVertexCount-1].y, 0);
        for(int i = 0; i < circleVertexCount; i++) {
            vertices[sectionVertexCount*circleVertexCount + i] = vertices[i];
            vertices[(sectionVertexCount+1)*circleVertexCount + 1 + i] = vertices[(sectionVertexCount-1)*circleVertexCount + i];
        }


        /*
         * Set the triangles of the circular mesh
         */
        /* Get the array of triangles that define the polygons of the cylinder */
        int[] triangles = GetCircularTriangles(vertices, circleVertexCount, sectionVertexCount);

        /* Add the triangles that close off the top and bottom */
        for(int i = 0; i < circleVertexCount; i++) {
            triangles[circleVertexCount*sectionVertexCount*6 + i*6 + 0] = sectionVertexCount*circleVertexCount+i + 1;
            triangles[circleVertexCount*sectionVertexCount*6 + i*6 + 1] = sectionVertexCount*circleVertexCount+i;
            //Bottom Center vertex
            triangles[circleVertexCount*sectionVertexCount*6 + i*6 + 2] = (sectionVertexCount+1)*circleVertexCount;

            triangles[circleVertexCount*sectionVertexCount*6 + i*6 + 3] = (sectionVertexCount+1)*circleVertexCount+1+i;
            triangles[circleVertexCount*sectionVertexCount*6 + i*6 + 4] = (sectionVertexCount+1)*circleVertexCount+1+i + 1;
            //Top Center vertex
            triangles[circleVertexCount*sectionVertexCount*6 + i*6 + 5] = (sectionVertexCount+2)*circleVertexCount+1;
        }


        /*
         * Set the UVs of the circular mesh
         */
        /* With the array of vertices and triangles we can now add this cylinder to the mesh */
        Vector2[] UVs = GetCircularUVs(vertices, circleVertexCount, sectionVertexCount, 0, 1.2f);

        /* Set the UVs for the top and bottom closed off ends of the mesh */
        for(int i = sectionVertexCount*circleVertexCount; i < UVs.Length; i++) {
            UVs[i].x = vertices[i].x;
            UVs[i].y = vertices[i].z;
        }


        AddToMesh(vertices, triangles, UVs);

        /* Create the capsule collider used to define this mesh */
        float capsuleHeight = (vertexPoints[vertexPoints.Length-1].y - vertexPoints[0].y);
        float centerHeigth = (vertexPoints[0].y + vertexPoints[vertexPoints.Length-1].y)/2f;
        float capsuleWidth = 0;
        foreach(Vector3 vec in vertexPoints) { capsuleWidth+= vec.x; }
        capsuleWidth /= vertexPoints.Length;
        CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0, centerHeigth, 0);
        capsule.radius = capsuleWidth;
        capsule.height = capsuleHeight;

        /* Add the radius amount to the height so that the capsule overlaps onto the next section */
        capsule.height += capsule.radius;
    }

    void AddToMesh(Vector3[] addedVertices, int[] addedTriangles, Vector2[] addedUVs) {
        /*
         * Add the given vertices, triangles and UVs to the column's current mesh.
         */
        Vector3[] currentVertices, newVertices;
        int[] currentTriangles, newTriangles;
        Vector2[] currentUVs, newUVs;
        Mesh newMesh = new Mesh();

        /* Get the current mesh's arrays. If the mesh does not yet exist, use empty arrays */
        if(GetComponent<MeshFilter>().sharedMesh != null) {
            currentVertices = GetComponent<MeshFilter>().sharedMesh.vertices;
            currentTriangles = GetComponent<MeshFilter>().sharedMesh.triangles;
            currentUVs = GetComponent<MeshFilter>().sharedMesh.uv;
        }else {
            currentVertices = new Vector3[0];
            currentTriangles = new int[0];
            currentUVs = new Vector2[0];
        }

        /* Concatenate the added vertices and UVs to the current arrays */
        newVertices = new Vector3[currentVertices.Length + addedVertices.Length];
        currentVertices.CopyTo(newVertices, 0);
        addedVertices.CopyTo(newVertices, currentVertices.Length);
        newUVs = new Vector2[currentUVs.Length + addedUVs.Length];
        currentUVs.CopyTo(newUVs, 0);
        addedUVs.CopyTo(newUVs, currentUVs.Length);

        /* Update the triangles so they reflect the new position of the vertices in the concatenated array */
        int indexOffset = currentVertices.Length;
        for(int i = 0; i < addedTriangles.Length; i++) {
            addedTriangles[i] += indexOffset;
        }
        newTriangles = new int[currentTriangles.Length + addedTriangles.Length];
        currentTriangles.CopyTo(newTriangles, 0);
        addedTriangles.CopyTo(newTriangles, currentTriangles.Length);

        /* Set the new mesh to the meshFilter */
        newMesh.name = "Pillar";
        newMesh.vertices = newVertices;
        newMesh.triangles = newTriangles;
        newMesh.uv = newUVs;
        newMesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = newMesh;
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    void SetColumnStats() {
        /*
         * Use the seed to set the stats of the column.
         */
        
        /* Set the seed and generate the needed values */
        UnityEngine.Random.State previousRandomState = Random.state;
        Random.InitState(seed);


		/*
		 * Section heights
		 */
        /* Set how much pillar space the filler will occupy */
        float fillerHeightRatioMin = 0.025f;
        float fillerHeightRatioMax = 0.25f;
        float fillerHeightMin = baseHeight;
        float fillerHeigthMax = baseHeight*10;
        float fillerEmptyChance = 0.2f;

        /* Calculate the filler height. Theres a chance the filler will be empty. */
        fillerHeight = 0;
        if(Random.value > fillerEmptyChance) {
        	fillerHeightRatio = Random.value*(fillerHeightRatioMax - fillerHeightRatioMin) + fillerHeightRatioMin;

            /* Calculate the fillerHeight and keep it within it's limits */
            fillerHeight = (totalHeight - baseHeight*2) * fillerHeightRatio;
            if(fillerHeight < fillerHeightMin) {
                fillerHeight = fillerHeightMin;
            }
            else if(fillerHeight > fillerHeigthMax) {
                fillerHeight = fillerHeigthMax;
            }
        }
        
        /* Get the height of the center cylinder */
        cylinderHeight = (totalHeight - baseHeight*2) - fillerHeight*2;

        /* Check if the pillar's section heights do not expand past it's limits */
        if(cylinderHeight < 0) {
            Debug.Log("WARNING: Pillar exceeds it's totalHeight limit");
        }


        /*
		 * Center Cylinder Stats
		 */
        /* Set the limits for the cylinder's radius and it's bump radius */
        float cylinderRadiusRatioMax = 1f;
		float cylinderRadiusRatioMin = 0.5f;
		float cylinderBumpRadiusRatioMax = 0.25f;
		float cylinderBumpRadiusRatioMin = 0.05f;
        float cylinderBumplessChance = 0.15f;

        /* Set the radius of the main cylinder. The radius is calculated using base radius */
        cylinderRadius = (baseWidth/2f) * (Random.value*(cylinderRadiusRatioMax - cylinderRadiusRatioMin) + cylinderRadiusRatioMin);
        
        /* Set the cylinder's bump stats */
        cylinderBumpRadius = 0;
        cylinderBumpStretch = 0;
        cylinderBumpOffset = 0;
        if(Random.value > cylinderBumplessChance) {
			cylinderBumpRadius = (baseWidth/2f) * (Random.value*(cylinderBumpRadiusRatioMax - cylinderBumpRadiusRatioMin) + cylinderBumpRadiusRatioMin);

            /* Handles how often a full "bump" occurs on the column. Mathf.PI*2 indicates a single full sin wave. */
            cylinderBumpStretch = Random.value*2*Mathf.PI;

            /* The offset of the bump. Keep it within [0, 2*Mathf.PI]. */
            cylinderBumpOffset = Random.value*2*Mathf.PI;
        }

        
        /* Get the Highest point on the sin function the bump reaches to prevent the pillar's radius from passing it's limit */
        if(cylinderBumpRadius > 0) {
            float HighestRange = -1;

            /* Get the amount of distance from the bump's start point to the sin wave's apex */
            float distanceToApex;
            if(cylinderBumpOffset > Mathf.PI/2) {
                distanceToApex = Mathf.PI*2 - (cylinderBumpOffset - Mathf.PI/2f);
            }
            else {
                distanceToApex = Mathf.PI/2 - cylinderBumpOffset;
            }

            /* If the cylinderBumpStretch does not reach the apex, use the edge with the highest point */
            if(distanceToApex > cylinderBumpStretch) {
                HighestRange = Mathf.Max(Mathf.Sin(cylinderBumpOffset), Mathf.Sin(cylinderBumpOffset + cylinderBumpStretch));
            }
            else {
                HighestRange = 1;
            }

            /* Using the highestRange, Check to see if the bump will push the main radius past the pillar's width limit */
            if(HighestRange > 0) {
                float extraRadius = (cylinderRadius + HighestRange*cylinderBumpRadius) - (baseWidth/2f);

                /* If there is any extra radius, reduce it from the column's radius */
                if(extraRadius > 0) {
                    cylinderRadius -= extraRadius;
                }
            }
        }
        

        /*
		 * Filler stats
		 */
        /* Set the stats of the filler (if it has more than 0 height) */
        fillerStats = null;
        float remainingHeight = totalHeight - baseHeight*2f;
		if(fillerHeight > 0) {
            fillerStats = new Vector3[] { new Vector3(1f, 1.1f, 0)};
        }

        /* Reset the RNG's seed back to it's previous value */
        Random.state = previousRandomState;
    }

    void DeleteColliders() {
        /*
         * Get each collider associated with this column and delete them.
         */

        Collider col = gameObject.GetComponent<Collider>();
        while(col != null) {
            DestroyImmediate(col);
            col = gameObject.GetComponent<Collider>();
        }
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */
    Vector3[] GetCircularVertices(float radius, float height, int vertexCount) {
        /*
         * Return an array of vertexes that form a circle by rotating a point around the y axis.
         */
        Vector3[] circle = new Vector3[vertexCount];
        float x, z;

        /* Create each vertex used to define the circle */
        for(int i = 0; i < circle.Length; i++) {
            x = radius*Mathf.Sin(2*Mathf.PI*i/(circle.Length-1));
            z = radius*Mathf.Cos(2*Mathf.PI*i/(circle.Length-1));
            circle[i] = new Vector3(x, height, z);
        }

        return circle;
    }

    int[] GetCircularTriangles(Vector3[] vertices, int circleVertexCount, int sectionVertexCount) {
        /*
         * Assuming the array of vertices are ordered from section to section starting with the bottom
         * and going up, use the given vertex counts to generate an array of triangles to define the mesh.
         */
        int[] triangles = new int[circleVertexCount*sectionVertexCount*6 + circleVertexCount*6];
        int triangleIndex;
        int vertexIndex;

        /* Define the meshes for each section of the column */
        for(int i = 0; i < sectionVertexCount-1; i++) {

            /* Define the circular mesh of each section */
            for(int ii = 0; ii < circleVertexCount-1; ii++) {
                triangleIndex = i*(circleVertexCount*6) + ii*6;
                vertexIndex = i*circleVertexCount + ii;
                triangles[triangleIndex + 0] = vertexIndex;
                triangles[triangleIndex + 1] = vertexIndex + 1;
                triangles[triangleIndex + 2] = vertexIndex + circleVertexCount + 1;
                triangles[triangleIndex + 3] = vertexIndex + circleVertexCount + 1;
                triangles[triangleIndex + 4] = vertexIndex + circleVertexCount;
                triangles[triangleIndex + 5] = vertexIndex;
            }
        }

        return triangles;
    }

    Vector2[] GetCircularUVs(Vector3[] vertices, int circleVertexCount, int sectionVertexCount, 
            float startingHeight, float height) {
        /*
         * Use the given array of vertices to calculate the proper UVs. How the UVs are calculated is done
         * using the given height values in conjunction with the vertice's y position.
         */
        Vector2[] UVs;
        float lowestVertice;
        float highestVertice;
        float x, y;

		/* Get the perimeter length of the circle to properly size the x UV value */
		float perimiter = Mathf.Ceil((vertices[0].z) * Mathf.PI * 2);

        /* Get the limits of the vertices on the y axis to properly value each vert's UV */
        lowestVertice = vertices[0].y;
        highestVertice = vertices[vertices.Length-1].y;

        /* Create the UVs */
        UVs = new Vector2[vertices.Length];
        /* Go through each section of the column */
        for(int i = 0; i < sectionVertexCount; i++) {
            /* Set the UV for each vertice on this section's series of cirular vertices */
            for(int ii = 0; ii < circleVertexCount; ii++) {

                /* X is dependent on what index position the vector is in */
                x = -perimiter*ii / ((float)circleVertexCount-1);

                /* Y is dependent on the Y position of the vector relative to the other vectors */
                y = vertices[i*circleVertexCount + ii].y;

                UVs[i*circleVertexCount + ii] = new Vector2(x, y);
            }
        }

        return UVs;
    }

}
