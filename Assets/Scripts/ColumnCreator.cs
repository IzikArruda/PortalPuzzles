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
    private float cylinderBumpRadius;
    /* How stretched the sin function will be when applying the bump */
    private float cylinderBumpStretch;
    /* An Offset applied to the center cylinder's bump amount */
    private float cylinderBumpOffset;
    /* The top and bottom radius of the center column */
    private float cylinderTopRadius;
    private float cylinderBottomRadius;


    /* Ratio of how much of the center pillar will be combined filer (excluding base) */
    public float fillerHeightRatio;
    private float fillerHeight;
    private float fillerHeightMin;
    private float fillerHeigthMax;

    /* An arraylist of each filler object to be used when creating this pillar's filler */
    public ArrayList fillerStats;
    
    /* Child object which holds all the pillar pieces */
    public GameObject pillarChildrenContainer;



    /* --- Filler Structs ---------------------- */

    /* How the filler of the pillar is made. Each fillerStat is of the form of a vector3 where:
     * x = the form of the filler. 0 is circular, 1 is box
     * y = the (ratio of baseWidth) width for box, radius for circular.
     * z = nothing yet */
    struct filler {
        /* Type of filler. 0 = square, 1 = circular */
        public int type;

        /* How much height this filler object occupies */
        public float height;

        /* Radius' that are used to define the filler shape. The filler radius should not go above
         * baseWidth and it should not go bellow the radius of the top/bottom of the main cylinder.
         * This is enforced by keeping the radius between the range of [1, 0] where 0 is the 
         * radius of the edge of the main cylinder and 1 is the baseWidth. 
         * It should be allowed to allow the radius to pass the ranges if needed. */
        public float[] radius;

        /* Values that further define the filler object, such as the offset and radian stretch of a circular mesh */
        public float[] extraValues;
    }


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

        /* Create the gameObject used to hold all the pieces of the pillar */
        if(pillarChildrenContainer == null) {
            RecreatePieceContainer();
        }
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

        /* Delete all the objects associated with the previous column model */
        RecreatePieceContainer();

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
        float cylinderPointDistance = cylinderHeight;

        /* If there is a bump in the center column, use more than 2 vertices to model it */
        if(cylinderBumpRadius > 0) {
            /* A larger bump radius will require a more defined set of vertices */
            cylinderPointDistance /= 5*(cylinderBumpRadius/(baseWidth/2f));

            /* A stretch value that's larger will require a more defined set of vertices */
            cylinderPointDistance /= 5*cylinderBumpStretch;

            /* As the center cylinder's radius gets small, use more vertices to define it's mesh */
            if(cylinderHeight < 3) {
                cylinderPointDistance /= 3*(1 - (cylinderHeight/3f));
            }
        }
        
        /* How much distance is between the points that form the pillar's center cylinder */
        int pointCount = 1 + Mathf.CeilToInt(cylinderHeight/cylinderPointDistance);

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
        float mainColumnEndRadius = cylinderTopRadius;
        int directionAdjustment = 1;

        /* Change how the position will increase/decrease depending on if the filler is added above or bellow */
        if(topBase) {
            directionAdjustment = -1;
            mainColumnEndRadius = cylinderBottomRadius;
        }

        /* Get the position to be at the start of the selected filler */
        currentYPos = baseHeight + fillerHeight + cylinderHeight/2f + directionAdjustment*cylinderHeight/2f;
        
        /* Create a filler box using the fillerStats. For now, each filler object will take up the entire fillerHeight */
        filler currFiller;
        float[] currExtra;
        float[] currRad;
        for(int i = 0; i < fillerStats.Count; i++) {
            /* Extract the values from the current filler struct to use */
            currFiller = (filler) fillerStats[i];
            currExtra = currFiller.extraValues;
            fillerPartHeight = directionAdjustment*currFiller.height;
            currentYPos += fillerPartHeight/2f;
            
            /* Get the array of radius used to define the filler */
            currRad = new float[currFiller.radius.Length];
            for(int ii = 0; ii < currFiller.radius.Length; ii++) {
                currRad[ii] = currFiller.radius[ii]*baseWidth/2f + (1-currFiller.radius[ii])*mainColumnEndRadius;
            }

            
            /* Create a circular mesh */
            if(currFiller.type == 0) {
                CreateFillerCircularMesh(currentYPos, fillerPartHeight, currRad[0], currRad[1]-currRad[0], currExtra[0], currExtra[1]);
            }

            /* Create a box */
            else if(currFiller.type == 1) {
                CreateFillerBox(currentYPos, fillerPartHeight, currRad[0]*2, currRad[1]*2);
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

        /* Flip the order of the widths if the hieght is negative, indicating the creation of the bottom filler */
        if(fillerSectionHeigth < 0) {
            float temp = boxTopWidth;
            boxTopWidth = boxBottomWidth;
            boxBottomWidth = temp;
        }
        
        /* Create a box using the given sizes */
        CreateBox(new Vector3(0, currentYPos, 0), Mathf.Abs(fillerSectionHeigth), boxTopWidth, boxBottomWidth);
    }

    void CreateFillerCircularMesh(float currentYPos, float fillerSectionHeigth,
			float radius, float maxBumpRadius, float startingRad, float radInc) {
        /*
         * Create a circular mesh using the given parameters. Use a sine function to add an offset
         * to the radius to allow bumbs (think of the outter half of a taurus).
         * Note we use width instead of radius because the sizes will most likely be relative to baseWidth.
         *
         * radius: the starting radius of the circular mesh for each vertex. The radius changes by a sine function.
         * maxBumpRadius: The extra width added sine function. 0 will prevent the radius from ever changing.
         * startingRad: What rad degrees the radius starts at from a range of [0, PI*2].
         * radInc: How much the rad will increase over the section's height. PI*2 is a whole sine wave.
         */
        float sineWaveOffset;
        currentYPos -= Mathf.Abs(fillerSectionHeigth)/2f;
        
        /* Calculate how many vertices will be used to define the curve of this circular mesh */
        float fillerPointDistance = fillerSectionHeigth;
        if(maxBumpRadius != 0) {
            /* A larger bump radius will require a more defined set of vertices */
            fillerPointDistance /= 5*(Mathf.Abs(maxBumpRadius)/(baseWidth/2f));

            /* A stretch value that's larger will require a more defined set of vertices */
            fillerPointDistance /= 5*radInc;
        }
        /* How much distance is between the points that form the pillar's center cylinder */
        int vertexCount = 1 + Mathf.CeilToInt(fillerSectionHeigth/fillerPointDistance);
        
        
        /* Populate the vertices array for the circular filler mesh */
        Vector3[] roundedEdgeVertices = new Vector3[vertexCount];

        /* Handle the radians differently depending on if it's the top or bottom filler of the pillar */
        if(fillerSectionHeigth < 0) {
            startingRad += radInc;
            radInc *= -1f;
        }

        /* Create the circular filler object */
        for(int i = 0; i < vertexCount; i++) {
        	sineWaveOffset = Mathf.Sin(startingRad + radInc*i/(float)(vertexCount-1));
            roundedEdgeVertices[i].x = (radius + maxBumpRadius*sineWaveOffset);
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
            new Vector2(-boxTopWidth/2f, boxTopWidth/2f),
            new Vector2(boxTopWidth/2f, boxTopWidth/2f),
            new Vector2(-boxTopWidth/2f, -boxTopWidth/2f),
            new Vector2(boxTopWidth/2f, -boxTopWidth/2f),
            //Y- plane
            new Vector2(boxBottomWidth/2f, boxBottomWidth/2f),
            new Vector2(-boxBottomWidth/2f, boxBottomWidth/2f),
            new Vector2(boxBottomWidth/2f, -boxBottomWidth/2f),
            new Vector2(-boxBottomWidth/2f, -boxBottomWidth/2f),
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
        
        /* Create the gameObject and it's colliders that will use the mesh */
        GameObject newFillerObject = CreatePillarColliders(origin.y, boxHeight, (boxBottomWidth + boxTopWidth)/2f, false);

        /* Add the calculated mesh to the newly created pillar piece */
        AddToMesh(vertices, triangles, UVs, newFillerObject);

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
        
        /* Set the circleVertexCount to be relative to the largest radius of the circular mesh to be created */
        float largestRadius = vertexPoints[0].x;
        for(int i = 1; i < vertexPoints.Length; i++) {
            largestRadius = Mathf.Min(largestRadius, vertexPoints[i].x);
        }
        circleVertexCount = 15 + Mathf.FloorToInt(largestRadius*10f);

        /* Set the amount of vertexes needed to render the curve of the series of vertexPoints. Cannot change? */
        sectionVertexCount = vertexPoints.Length;

        /* Set the vertex counts. circle is the amount when rotating a point around the center
         * while section is the amount of vertexes needed on the same y axis to define the pillar */
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
        
        /* Get the starting height, mesh height and average radius of the circular mesh */
        float meshHeight = (vertexPoints[vertexPoints.Length-1].y - vertexPoints[0].y);
        float centerHeight = (vertexPoints[0].y + vertexPoints[vertexPoints.Length-1].y)/2f;
        float meshAverageWidth = 0;
        foreach(Vector3 vec in vertexPoints) { meshAverageWidth += vec.x; }
        meshAverageWidth /= vertexPoints.Length;

        /* Create the gameObject and it's colliders that will use the mesh */
        GameObject newFillerObject = CreatePillarColliders(centerHeight, meshHeight, meshAverageWidth, true);
        
        /* Add the calculated mesh to the newly created pillar piece */
        AddToMesh(vertices, triangles, UVs, newFillerObject);
    }

    GameObject CreatePillarColliders(float startHeight, float height, float width, bool Circular) {
        /*
         * Use the given parameters to create colliders for the piece of the pillar that's being created.
         * Determine whether it's a circular or box piece and return the gameObject once finished.
         */

        /* Create a new gameObject for this mesh and add it as a child to this pillar */
        GameObject pillarPiece = new GameObject();
        pillarPiece.transform.parent = pillarChildrenContainer.transform;
        pillarPiece.name = "Pillar Piece " + pillarPiece.GetInstanceID();
        pillarPiece.transform.localPosition = new Vector3(0, 0, 0);
        pillarPiece.transform.localEulerAngles = new Vector3(0, 0, 0);
        pillarPiece.transform.localScale = new Vector3(1, 1, 1);


        /* Create the colliders used to define a circular mesh for the pillar */
        if(Circular) {
            
            /* The mesh's height is larger than it's average width, use a capsule collider to define it's mesh */
            if(height/2f > width) {
                CapsuleCollider capsule = pillarPiece.AddComponent<CapsuleCollider>();
                capsule.center = new Vector3(0, startHeight, 0);
                capsule.radius = width;
                capsule.height = height;
                /* Add the radius amount to the height so that the capsule overlaps onto the next section */
                capsule.height += capsule.radius;
            }

            /* With a small height, it's best to use a set of boxes as it's colliders */
            else {
                /* Create multiple box colliders to form a cylinder-like shape */
                int boxCount = 4;
                GameObject newColliderObject;
                for(int i = 0; i < boxCount; i++) {
                    /* Create the new box and apply a rotation to the local Y axis */
                    newColliderObject = new GameObject();
                    newColliderObject.name = "Circular Box Collider " + newColliderObject.GetInstanceID();
                    newColliderObject.transform.parent = pillarPiece.transform;
                    newColliderObject.transform.localPosition = new Vector3(0, 0, 0);
                    newColliderObject.transform.localScale = new Vector3(1, 1, 1);
                    newColliderObject.transform.localEulerAngles = new Vector3(0, i*(90/boxCount), 0);

                    /* Create the box collider for this box */
                    BoxCollider circularMeshCollider = newColliderObject.AddComponent<BoxCollider>();
                    circularMeshCollider.center = new Vector3(0, startHeight, 0);
                    float centerToEdge = Mathf.Sqrt(Mathf.Pow(width, 2)*2);
                    circularMeshCollider.size = new Vector3(centerToEdge, height, centerToEdge);
                }
            }
        }

        /* Create a single box collider */
        else {
            BoxCollider boxCollider = pillarPiece.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, startHeight, 0);
            boxCollider.size = new Vector3(width, height, width);
        }
        

        return pillarPiece;
    }

    void AddToMesh(Vector3[] addedVertices, int[] addedTriangles, Vector2[] addedUVs, GameObject GO) {
        /*
         * Add the given vertices, triangles and UVs to the given gameObject's current mesh.
         * If it currently does not have a mesh, create a new one using the given parameters.
         */
        Vector3[] currentVertices, newVertices;
        int[] currentTriangles, newTriangles;
        Vector2[] currentUVs, newUVs;
        Mesh newMesh = new Mesh();

        /* Create the neccesairy compoents for the given gameOjbect if it does not have them */
        if(GO.GetComponent<MeshFilter>() == null) {
            GO.AddComponent<MeshFilter>();
        }
        if(GO.GetComponent<MeshRenderer>() == null) {
            GO.AddComponent<MeshRenderer>();
            /* Update the material for the meshRenderer */
            GO.GetComponent<MeshRenderer>().sharedMaterial = columnMaterial;
        }


        /* Get the current mesh's arrays. If the mesh does not yet exist, use empty arrays */
        if(GO.GetComponent<MeshFilter>().sharedMesh != null) {
            currentVertices = GO.GetComponent<MeshFilter>().sharedMesh.vertices;
            currentTriangles = GO.GetComponent<MeshFilter>().sharedMesh.triangles;
            currentUVs = GO.GetComponent<MeshFilter>().sharedMesh.uv;
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
        GO.GetComponent<MeshFilter>().mesh = newMesh;
    }


    /* ----------- Event Functions ------------------------------------------------------------- */

    void RecreatePieceContainer() {
        /*
         * Delete each piece of the pillar by re-creating the container that holds each piece of the pillar
         */
         
        DestroyImmediate(pillarChildrenContainer);
        pillarChildrenContainer = new GameObject();
        pillarChildrenContainer.name = "Pillar Children";
        pillarChildrenContainer.transform.parent = gameObject.transform;
        pillarChildrenContainer.transform.localEulerAngles = new Vector3(0, 0, 0);
        pillarChildrenContainer.transform.localPosition = new Vector3(0, 0, 0);
        pillarChildrenContainer.transform.localScale = new Vector3(1, 1, 1);
    }
    
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
        float fillerEmptyChance = 0.2f;
        fillerHeightMin = baseHeight;
        fillerHeigthMax = baseHeight*10;

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

        /* Track the radius of the cylinder at it's top and bottom points */
        cylinderTopRadius = cylinderRadius + cylinderBumpRadius*Mathf.Sin(cylinderBumpOffset + cylinderBumpStretch);
        cylinderBottomRadius = cylinderRadius + cylinderBumpRadius*Mathf.Sin(cylinderBumpOffset);



        /*
		 * Filler stats
		 */
        /* Get the amount of distance the filler's height will need to occupy */
        fillerStats = new ArrayList();

        /* Populate the fillerStats arrayList with enough objects to meet the filler's height */
        CreateFillerObjects(ref fillerStats, fillerHeight);

        /* Reset the RNG's seed back to it's previous value */
        Random.state = previousRandomState;
    }


    /* ----------- Filler Creation Functions ------------------------------------------------------------- */

    ArrayList CreateFillerObjects(ref ArrayList fillerStats, float fillerHeight) {
        /*
         * Create random filler objects and add them to the given fillerStats arrayList.
         */
        float remainingFillerHeight = fillerHeight;
        float currentFillerHeight = remainingFillerHeight;
        /* The expected width to use. It's value is relative to the current height of the filler: 
         * an empty filler starts at 0. As it "fills in", it will increase towards 1. */
        float expectedFillerWidthStart = 0;
        float expectedFillerWidthEnd = 0;
        
        /* As long as there is filler height inaccounted for, continue making filler objects */
        while(remainingFillerHeight > 0) {

            /* Set the height of the new filler. Ensure each filler piece will not be smaller than baseWidth */
            if(remainingFillerHeight <= baseHeight) {
                currentFillerHeight = remainingFillerHeight;
            }
            else {
                currentFillerHeight = (Random.value*(remainingFillerHeight-baseHeight) + baseHeight);
                if(remainingFillerHeight - currentFillerHeight < baseHeight/2f) {
                    /* Add the remaining height to the current object */
                    currentFillerHeight = remainingFillerHeight;
                }
                else if(remainingFillerHeight - currentFillerHeight < baseHeight) {
                    /* Leave enough height for another filler object */
                    currentFillerHeight = remainingFillerHeight - baseHeight;
                }
            }

            /* Set the expected width of the current height */
            expectedFillerWidthStart = expectedFillerWidthEnd;
            expectedFillerWidthEnd = 1 - (remainingFillerHeight - currentFillerHeight) / fillerHeight;
            
            /* Generate some new filler for the given limits */
            CreateRandomFiller(ref fillerStats, currentFillerHeight, expectedFillerWidthStart, expectedFillerWidthEnd);
            
            /* reduce the remaining height quota before starting another loop */
            remainingFillerHeight -= currentFillerHeight;
        }

        return fillerStats;
    }
    
    void CreateRandomFiller(ref ArrayList fillerStats, float height, float startWidth, float endWidth) {
        /*
         * The function which will send the requests to create the filler. Look at the filler stats 
         * to determine what kind of filler to create.
         * 
         * To determine what filler object to use, whenever a filler object meets it's given conditions,
         * add it's ID to the possibleFillers array. Once all objects have been checked, pick
         * a random ID from the array and use it as the filler to be created. An object can
         * have a higher chance to be picked if it adds more of it's ID into the array.
         * 
         * Here are the list of IDs to their filler objects:
         *  - 0: CreateRandomSingleFiller
         *  - 1: CreateRandomSandwichFiller
         *  - 2: CreateLargeCircularFiller
         *  - 3: CreateRibbedColumnFiller
         *  - 4: CreateStairsFiller
         */
        float widthDifference = endWidth - startWidth;
        float maxColumnRadius = Mathf.Max(cylinderTopRadius, cylinderBottomRadius);
        ArrayList possibleFillers = new ArrayList();
        
        /* Sandwich a random filler between two identical filler */
        if(height > baseHeight*2 && maxColumnRadius < baseWidth/2.5f) {
            possibleFillers.Add(1);
            possibleFillers.Add(1);
        }

        /* Create three circular filler that covers a large width and height */
        if(startWidth == 0 && height > fillerHeight*0.75 && maxColumnRadius < baseWidth/2.5f) {
            possibleFillers.Add(2);
            possibleFillers.Add(2);
        }

        /* Create two filler objects that sandwich a circular equal to to the center column's radius */
        if(startWidth == 0 && height > fillerHeight*0.5f && height > fillerHeigthMax*0.3f) {
            possibleFillers.Add(3);
            possibleFillers.Add(3);
        }

        /* Create the stairs filler object */
        if(endWidth == 1 && widthDifference > 0.35f && maxColumnRadius < baseWidth/2.5f) {
            possibleFillers.Add(4);
            possibleFillers.Add(4);
        }
        
        /* Always add a random single filler to the list */
        possibleFillers.Add(0);


        /* Pick a random ID from the arrayList */
        int randomFillerID = (int) possibleFillers[Mathf.CeilToInt(Random.Range(-0.99f, possibleFillers.Count-1))];

        /* Create the filler object that meets the found ID */
        if(randomFillerID == 1) {
            //Debug.Log("Sandwich");
            CreateRandomSandwichFiller(ref fillerStats, widthDifference, height, startWidth);
        }
        else if(randomFillerID == 2) {
            //Debug.Log("Large Circular");
            CreateLargeCircularFiller(ref fillerStats, widthDifference, height, startWidth);
        }
        else if(randomFillerID == 3) {
            //Debug.Log("Ribbed Column");
            CreateRibbedColumnFiller(ref fillerStats, widthDifference, height, startWidth);
        }
        else if(randomFillerID == 4) {
            //Debug.Log("Stairs");
            CreateStairsFiller(ref fillerStats, widthDifference, height, startWidth);
        }
        else {
            //Debug.Log("Single Random");
            CreateRandomSingleFiller(ref fillerStats, widthDifference, height, startWidth, -1);
        }
    }
    
    void CreateRandomSandwichFiller(ref ArrayList fillerStats, float widthDifference, float height, float startWidth) {
        /*
         * Generate two random filler objects and use the same object for the top and bottom of the three.
         * This will sandwich a random filler between two identical filler.
         * The first and 3rd filler occupy 30% while the second filler uses 40%
         * 
         *  - The height of this set must be large enough to ensure the 3 fillers arent too small
         *  - There must be enough width between the base and the column for it to look good
         */
        int doubleFillerSeed;
        
        /* Create the filler objects, giving them their proper parameters */
        //Top
        doubleFillerSeed = CreateRandomSingleFiller(ref fillerStats, widthDifference*0.3f, height*0.3f, startWidth, -1);
        //Center
        CreateRandomSingleFiller(ref fillerStats, widthDifference*0.4f, height*0.4f, startWidth + widthDifference*0.3f, -1);
        //Bottom
        CreateRandomSingleFiller(ref fillerStats, widthDifference*0.3f, height*0.3f, startWidth + widthDifference*0.7f, doubleFillerSeed);
        
        
        /* Heres a good example. Can switch both object's bumpType and it is equally as good */
        //Top
        //CreateCircularFiller(ref fillerStats, height*0.3f, startWidth + widthDifference*0.0f, startWidth + widthDifference*0.3f, 0);
        //center
        //CreateCircularFiller(ref fillerStats, height*0.4f, startWidth + widthDifference*0.3f, startWidth + widthDifference*0.7f, 2);
        //bottom
        //CreateCircularFiller(ref fillerStats, height*0.3f, startWidth + widthDifference*0.7f, startWidth + widthDifference*1.0f, 0);
    }

    int CreateRandomSingleFiller(ref ArrayList fillerStats, float widthDifference, float height, float startWidth, int givenSeed) {
        /*
         * Create a single random filler object using a seed for CreateRandomSandwichFiller. Can take in an seed value
         * if the same filler object wants to be created again. If seed is negative, generate a random one.
         * Return the seed value so that the filler object can be created again if needed.
         */
        UnityEngine.Random.State previousRandomState = Random.state;
        int seed = givenSeed;

        /* Generate a new seed if needed */
        if(seed < 0) {
            seed = (int) (Random.value*999999);
        }

        /* Apply the seed to the RNG */
        Random.InitState(seed);

        /* Create a square filler object using random parameters */
        if(Random.value < 0.5f) {
            float topWidthDiff = 0;
            float bottomWidthDiff = widthDifference;

            /* The top and bottom radius of the square will be equal */
            if(Random.value < 0.5f) {
                topWidthDiff = Random.value*widthDifference;
                bottomWidthDiff = topWidthDiff;
            }

            /* Top and bottom radius are use different values */
            else {
                /* Set the possible values for the widthDifference */
                float maxDiffRange = 1.0f;
                float minDiffRange = 0.25f;
                float diffRange = (Random.value*(maxDiffRange-minDiffRange) + minDiffRange);

                /* Get a new start radius using the DiffRange to prevent it from going too far */
                float newStartWidth = widthDifference*(Random.value*(1 - diffRange));

                /* Apply the diffRange and the new startWidth to the new top and bottom widthDiff */
                topWidthDiff = newStartWidth;
                bottomWidthDiff = newStartWidth + diffRange*widthDifference;
            }

            /* Create the square filler using the randomly set parameters */
            CreateSquareFiller(ref fillerStats, height, startWidth + topWidthDiff, startWidth + bottomWidthDiff);
        }

        /* Create a circular filler object using random parameters */
        else {
            int type;
            float topWidthDiff = 0;
            float bottomWidthDiff = widthDifference;

            /* Create a circular filler that does not progress (top and bottom radius end at the same spot) */
            if(Random.value < 0.5f) {
                if(Random.value < 0.5f) {
                    type = 1;
                }else {
                    type = 3;
                }
            }

            /* Create a circular filler that progresses the width (starts at top and ends at bottom radius) */
            else {
                if(Random.value < 0.5f) {
                    type = 0;
                }
                else {
                    type = 2;
                }
            }

            /* Set the widthDiff. WidthDiff is either larger than 50% or exactly 0%. 0% occurs 20% of the time */
            float zeroDiffChance = 0.2f;
            float minDiffRange = 0.5f;
            float diffRange = (Random.value*(1 - minDiffRange) + minDiffRange);
            if(Random.value < zeroDiffChance) {
                diffRange = 0;
            }
            float newStartWidth = widthDifference*(Random.value*(1 - diffRange));

            /* Apply the diffRange and the new startWidth to the new top and bottom widthDiff */
            topWidthDiff = newStartWidth;
            bottomWidthDiff = newStartWidth + diffRange*widthDifference;

            /* Create the circular filler using the randomly set parameters */
            CreateCircularFiller(ref fillerStats, height, startWidth + topWidthDiff, startWidth + bottomWidthDiff, type);
        }
        

        /* Return the RNG back to it's previous state before this function if needed */
        if(givenSeed > -1) {
            Random.state = previousRandomState;
        }

        return seed;
    }
    
    void CreateLargeCircularFiller(ref ArrayList fillerStats, float widthDifference, float height, float startWidth) {
        /*
         * Use a large circular filler that covers a lot of height and width, along with two other above and bellow it.
         *  - Starts at the main column, but doesnt always end at the base
         *  - This object must take up most of the filler
         *  - There must be enough width between the base and the column for it to look good
         */

        //Top circular
        CreateCircularFiller(ref fillerStats, height*0.2f, startWidth, startWidth + widthDifference*0.2f, 1);

        //Large center circular
        CreateCircularFiller(ref fillerStats, height*0.60f, startWidth, startWidth + widthDifference*0.6f, 2);

        //Bottom object
        CreateCircularFiller(ref fillerStats, height*0.2f, startWidth + widthDifference*0.8f, startWidth + widthDifference*1, 1);
    }

    void CreateRibbedColumnFiller(ref ArrayList fillerStats, float widthDifference, float height, float startWidth) {
        /*
         * Sandwich a flat circular filler with two filler objects. The goal is to make the center column
         * seem to extend further into the filler. These are it's requirements:
         *  - Starts at the main column, but doesnt always end at the base
         *  - The height must be large enough (compared to the current filler size and the maximum filler size) 
         */

        //Top cylinder
        CreateCircularFiller(ref fillerStats, height*0.2f, startWidth, startWidth + widthDifference*0.25f, 1);

        //Center cylinder
        CreateCircularFiller(ref fillerStats, height*0.5f, startWidth, startWidth, 0);

        //Bottom square
        CreateSquareFiller(ref fillerStats, height*0.3f, startWidth + widthDifference*0.6f, startWidth + widthDifference*1.0f);

    }

    void CreateStairsFiller(ref ArrayList fillerStats, float widthDifference, float height, float startWidth) {
        /* 
         * Create a series of square filler that get smaller. It will form the shape of "stairs" and has these requirements:
         *  - Ends at the base, but doesnt always start at the center column (endWidth == 1).
         *  - The width difference must be above a certain limit or each step will be to close.
         *  - MaxColumnRadius must be bellow a certain amount of the stairs will be too small.
         */
        int stepCount = 5;
        float stepDifference = widthDifference / stepCount;

        /* Create each step of the stairs */
        for(float i = 0.5f; i < stepCount; i++) {
            CreateSquareFiller(ref fillerStats, height/stepCount, startWidth + stepDifference*i, startWidth + stepDifference*i);
        }
    }
    
    void CreateCircularFiller(ref ArrayList fillerStats, float height, float topRadius, float bottomRadius, int bumpType) {
        /*
         * Create a circular filler object using the given parameters and add it to the given filler list.
         * BumpType determines the radians and stretch of the object. The potential values are:
         *  - 0: Goes from topRad to bottomRad. Uses the first quarter of the sine wave.
         *  - 1: Starts and ends with topRad, but has a full bump. Uses half of a sine wave.
         *  - 2: Starts at bottomRad and ends at topRad using the first quarter of a sine wave.
         *  - 3: Stars and ends at topRad. Does not have a bump, does nothing with the bottom rad.
         */
        filler circularFiller = new filler();

        /* Set the stats of a circular filler */
        circularFiller.type = 0;
        circularFiller.height = height;

        /* Set the radius of the mesh */
        circularFiller.radius = new float[] { topRadius, bottomRadius};

        /* Set the offset and stretch of the circular mesh's radians */
        if(bumpType == 0) {
            circularFiller.extraValues = new float[] { 0, Mathf.PI/2f };
        }else if(bumpType == 1) {
            circularFiller.extraValues = new float[] { 0, Mathf.PI };
        }else if(bumpType == 2) {
            circularFiller.radius = new float[] { bottomRadius , topRadius };
            circularFiller.extraValues = new float[] { Mathf.PI/2f, Mathf.PI/2f };
        }
        else if(bumpType == 3) {
            circularFiller.radius = new float[] { topRadius, topRadius };
            circularFiller.extraValues = new float[] { 0, 0.1f };
        }
        else {
            Debug.Log("WARNING: BumpType not handled");
        }

        /* Add the filler to the filler list */
        fillerStats.Add(circularFiller);
    }
    
    void CreateSquareFiller(ref ArrayList fillerStats, float height, float topRadius, float bottomRadius) {
        /*
         * Create a square filler object using the given parameters and add it to the given arrayList
         */
        filler squareFiller = new filler();

        /* Set the stats of a square filler */
        squareFiller.type = 1;
        squareFiller.height = height;

        /* Set the radius of the square filler */
        squareFiller.radius = new float[] { bottomRadius, topRadius };

        /* There are no extra values that can define a square */
        squareFiller.extraValues = new float[] { };

        /* Add the filler to the filler list */
        fillerStats.Add(squareFiller);
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
        float x, y;

		/* Get the perimeter length of the circle to properly size the x UV value */
		float perimiter = Mathf.Ceil((vertices[0].z) * Mathf.PI * 2);

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
