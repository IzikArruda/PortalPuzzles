using UnityEngine;
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

    //The material used by the pillar
    public Material columnMaterial;


    /* --- Key Shape Values ------------------- */
    /* Sizes of the column's base */
    public float baseWidth;
    public float baseHeight;

    /* Sizes of the pillar's cylinder */
    public float cylinderHeight;
    public float cylinderRadius;

    /* The amount of distance between the base and the center cylinder */
    public float fillerHeight;

    /* --- Change Detection Values ------------------- */
    /* Detect when a key value has changed to recreate the column */
    public bool recreateObject;


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

        if(recreateObject == true) {
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

        /* Create the base of the column that sits on the y = 0 point and the highest point of the column */
        Vector3 bottomBase = new Vector3(0, baseHeight/2f, 0);
        Vector3 topBase = new Vector3(0, baseHeight + fillerHeight*2 + cylinderHeight + baseHeight/2f, 0);
        CreateBox(bottomBase, baseWidth, baseHeight);
        CreateBox(topBase, baseWidth, baseHeight);

        /* Create a series of filler shapes that are placed between the base and the center cylinder */
        CreateFiller(true);
        CreateFiller(false);

        /* Create the cylinder center of the column */
        CreateCenterCylinder();
    }
    
    void CreateCenterCylinder() {
        /*
         * Create the center cylinder
         */

        /* Get the points that will be used to form one edge of the cylinder */
        Vector3 bottomPoint = new Vector3(cylinderRadius, baseHeight + fillerHeight, 0);
        Vector3 centerPoint = new Vector3(cylinderRadius, baseHeight + cylinderHeight/2f + fillerHeight, 0);
        Vector3 topPoint = new Vector3(cylinderRadius, baseHeight + fillerHeight + cylinderHeight, 0);
        Vector3[] cylinderPoints = new Vector3[3];
        cylinderPoints[0] = bottomPoint;
        cylinderPoints[1] = centerPoint;
        cylinderPoints[2] = topPoint;

        //Use a function that takes in a series of vector3s and circles them around the origin
        CreateCircularMesh(cylinderPoints);
    }

    void CreateFiller(bool bottomBase) {
        /*
         * Create a series of shapes to be placed between the base and the main cylinder of the column.
         * The given boolean will be true if the filler is added on the bottom base instead of the top base.
         */
        float currentYPos;
        float boxHeight, boxWidth;
        int directionAdjustment;

        /* Change how the position will increase/decrease depending on if the filler is added above or bellow */
        if(bottomBase) {
            directionAdjustment = 1;
            currentYPos = baseHeight;
        }
        else {
            directionAdjustment = -1;
            currentYPos = baseHeight + cylinderHeight + fillerHeight*2;
        }
        

        //Create a smaller box right above the main base
        boxWidth = baseWidth*0.85f;
        boxHeight = fillerHeight;
        currentYPos += directionAdjustment*boxHeight/2f;
        CreateBox(new Vector3(0, currentYPos, 0), boxWidth, boxHeight);
        currentYPos += directionAdjustment*boxHeight/2f;
    }

    /* ----------- Mesh Setting Functions ------------------------------------------------------------- */

    void CreateBox(Vector3 origin, float boxWidth, float boxHeight) {
        /*
         * Create a box mesh using the given width and height that expands outward of the origin equally.
         * Used to create the square bases on the top and bottom of the column.
         */
        Mesh boxMesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UVs;
        int[] triangles;

        /* Get the distance each vertex of the cube will be from it's center */
        float W = boxWidth/2f;
        float L = boxWidth/2f;
        float H = boxHeight/2f;

        /* Get the vertices that make up the cube */
        vertices = new Vector3[] {
            //X+ plane
            new Vector3(L, H, W),
            new Vector3(L, H, -W),
            new Vector3(L, -H, W),
            new Vector3(L, -H, -W),
            //X- plane
            new Vector3(-L, H, W),
            new Vector3(-L, H, -W),
            new Vector3(-L, -H, W),
            new Vector3(-L, -H, -W),
            //Y+ plane
            new Vector3(L, H, -W),
            new Vector3(L, H, W),
            new Vector3(-L, H, -W),
            new Vector3(-L, H, W),
            //Y- plane
            new Vector3(L, -H, -W),
            new Vector3(L, -H, W),
            new Vector3(-L, -H, -W),
            new Vector3(-L, -H, W),
            //Z+ plane
            new Vector3(L, H, W),
            new Vector3(-L, H, W),
            new Vector3(L, -H, W),
            new Vector3(-L, -H, W),
            //Z- plane
            new Vector3(L, H, -W),
            new Vector3(-L, H, -W),
            new Vector3(L, -H, -W),
            new Vector3(-L, -H, -W)
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

        /* Set the UVs of the cube */
        UVs = new Vector2[] {
            //X+ plane
            new Vector2(-H, -W),
            new Vector2(-H, +W),
            new Vector2(+H, -W),
            new Vector2(+H, +W),
            //X- plane
            new Vector2(-H, -W),
            new Vector2(-H, W),
            new Vector2(H, -W),
            new Vector2(H, W),
            //Y+ plane
            new Vector2(W, L),
            new Vector2(-W, L),
            new Vector2(W, -L),
            new Vector2(-W, -L),
            //Y- plane
            new Vector2(-L, -W),
            new Vector2(-L, W),
            new Vector2(L, -W),
            new Vector2(L, W),
            //Z+ plane
            new Vector2(-H, -L),
            new Vector2(-H, L),
            new Vector2(H, -L),
            new Vector2(H, L),
            //Z- plane
            new Vector2(L, H),
            new Vector2(-L, H),
            new Vector2(L, -H),
            new Vector2(-L, -H)
        };


        /* Add the vertices, triangles and UVs to the column's current mesh */
        AddToMesh(vertices, triangles, UVs);
    }
    
    void CreateCircularMesh(Vector3[] vertexPoints) {
        /*
         * Create a mesh that rotates around the origin using each vertex's x distance as a radius.
         */
        int circleVertexCount;
        int sectionVertexCount;
        Vector3[] vertices;
        
        /* Set the vertex counts. circle is the amount when rotating a point around the center
         * while section is the amount of vertexes needed on the same y axis to define the pillar */
        circleVertexCount = 20 +1;
        sectionVertexCount = vertexPoints.Length;


        /* Initialize the main vector array now that we know the amount of vectors to be used */
        vertices = new Vector3[sectionVertexCount * circleVertexCount];

        for(int i = 0; i < sectionVertexCount; i++) {
            /* Get an array of all vectors that form a circle by rotating the current vertex around the origin */
            Vector3[] vectorCircle = GetCircularVertices(vertexPoints[i].x, vertexPoints[i].y, circleVertexCount);

            /* Add the vectors that form the found circle into the main vector array */
            vectorCircle.CopyTo(vertices, circleVertexCount*i);
        }
        
        /* Get the array of triangles that define the polygons of the cylinder */
        int[] triangles = GetCircularTriangles(vertices, circleVertexCount, sectionVertexCount);
        
        /* With the array of vertices and triangles we can now add this cylinder to the mesh */
        Vector2[] UVs = GetCircularUVs(vertices, circleVertexCount, sectionVertexCount, 0, 1.2f);
        
        AddToMesh(vertices, triangles, UVs);
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
        int[] triangles = new int[circleVertexCount*sectionVertexCount*6];
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
                x = ii / ((float)circleVertexCount-1);

                /* Y is dependent on the Y position of the vector relative to the other vectors */
                y = startingHeight + height*((vertices[i*circleVertexCount + ii].y - lowestVertice) / (highestVertice - lowestVertice));

                UVs[i*circleVertexCount + ii] = new Vector2(x, y);
            }
        }

        return UVs;
    }
}
