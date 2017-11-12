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

    /* --- Key Shape Values ------------------- */
    /* Sizes of the column's base */
    public float baseWidth;
    public float baseHeight;


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
            gameObject.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Default"));
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

        /* Create the base of the column */
        CreateBase();
    }

    void CreateBase() {
        /*
         * Creating a base is simply creating a box within a set size
         */

        /* Adjust the starting position of the base so it does not pass bellow this gameObject's negative y axis */
        Vector3 bottomBase = new Vector3(0, baseHeight/2f, 0);
        CreateBox(bottomBase, baseWidth, baseHeight);

        /* Add another base above to test out the mesh creation */
        Vector3 topBase = new Vector3(0, 2 + baseHeight/2f, 0);
        CreateBox(topBase, baseWidth, baseHeight);
    }


    /* ----------- Mesh Setting Functions ------------------------------------------------------------- */

    void CreateBox(Vector3 origin, float boxWidth, float boxHeight) {
        /*
         * Create a box mesh using the given width and height that expands outward of the origin equally.
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
        Debug.Log(indexOffset);
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
}
