using UnityEngine;
using System.Collections;

/*
 * Create a marble column using the given parameters. A column is divided into multiple parts.
 * For now, only create the base that will sandwich the pillar.
 */
[ExecuteInEditMode]
public class ColumnCreator : MonoBehaviour {

    /* Key values given by the user that define the column */
    public float width;
    public float height;
    
    /* Detect when a key value has changed to recreate the column */
    public bool recreateObject;



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



    void CreateColumn() {
        /*
         * When the column needs to be created, this is the method that will build it
         */

        /* Create the base of the column */
        CreateBase();
    }

    void CreateBase() {
        /*
         * Creating a base is simply creating a box within a set size
         */

        CreateBox(width, height);
    }


    void CreateBox(float boxWidth, float boxHeight) {
        /*
         * Create a box mesh using the given width and height.
         * 
         * Assume the center is at the origin and the box expands outwards in equal directions
         */
        Mesh boxMesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UV;
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
        UV = new Vector2[] {
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

        
        /* Assign the mesh to the meshRenderer */
        boxMesh.vertices = vertices;
        boxMesh.triangles = triangles;
        boxMesh.uv = UV;
        boxMesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = boxMesh;
    }
}
