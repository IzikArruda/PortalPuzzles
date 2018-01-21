using UnityEngine;
using System.Collections;

/*
 * A general script for a room that will create it's own walls
 */
public class ConnectedRoom : MonoBehaviour {

    /* The materials used for the room */
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;

    /* The container to hold all the objects in the room */
    public Transform roomObjectsContainer;

    /* The main walls that form the room */
    public GameObject[] roomWalls = null;

    /* A trigger used for this room */
    public BoxCollider roomTrigger;


    /* -------- Helper Functions ---------------------------------------------------- */

    public void CreateObjects(ref GameObject[] objects, int size, Vector3 position) {
        /*
         * Re-create the given array of gameObjects. Set only values that are idential for each objects.
         */
         
        /* Ensure each object is empty before creating now ones */
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i] != null) {
                DestroyImmediate(objects[i]);
            }
        }

        /* Create a new array with the new given size if needed */
        if(objects.Length != size) { objects = new GameObject[size]; }

        /* Create each new objects */
        for(int i = 0; i < objects.Length; i++) {
            objects[i] = new GameObject();
            objects[i].transform.parent = roomObjectsContainer;
            objects[i].transform.position = position;
            objects[i].transform.localEulerAngles = new Vector3(0, 0, 0);
            objects[i].transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public void CreatePlane(GameObject wall, float xScale, float zScale, float UVScale, Material material, int wallType, bool flip) {
        /*
         * Create a plane onto the given gameObject. The position of the vertex in the world
         * determines how the UVs will be placed. The given boolean sets whether the mesh should be flipped.
         * 
         * The given wallType determines how the UVs are set:
         *  - 0 : Floor, using [x, z]
         *  - 1 : X parallel Wall, using [z, y]
         *  - 2 : Y parallel Wall, using [x, y]
         */
        Mesh wallMesh = new Mesh();
        Vector3[] vertices = null;
        Vector2[] UV = null;
        int[] triangles = null;

        /* Use the meshCreator function to create the basics of the wall */
        CreateMesh(xScale, zScale, ref vertices, ref triangles, wallType, flip);

        /* Set the UVs of the plane */
        Vector3 properCenter = wall.transform.rotation * wall.transform.position;
        if(wallType == 0) {
            UV = new Vector2[] {
                (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[0].x, vertices[0].z))/UVScale,
                (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[1].x, vertices[1].z))/UVScale,
                (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[2].x, vertices[2].z))/UVScale,
                (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[3].x, vertices[3].z))/UVScale
            };
        }
        else if(wallType == 1) {
            UV = new Vector2[] {
                (new Vector2(properCenter.z, properCenter.y) + new Vector2(vertices[0].z, vertices[0].y))/UVScale,
                (new Vector2(properCenter.z, properCenter.y) + new Vector2(vertices[1].z, vertices[1].y))/UVScale,
                (new Vector2(properCenter.z, properCenter.y) + new Vector2(vertices[2].z, vertices[2].y))/UVScale,
                (new Vector2(properCenter.z, properCenter.y) + new Vector2(vertices[3].z, vertices[3].y))/UVScale
            };
        }
        else if(wallType == 2) {
            UV = new Vector2[] {
                (new Vector2(properCenter.x, properCenter.y) + new Vector2(vertices[0].x, vertices[0].y))/UVScale,
                (new Vector2(properCenter.x, properCenter.y) + new Vector2(vertices[1].x, vertices[1].y))/UVScale,
                (new Vector2(properCenter.x, properCenter.y) + new Vector2(vertices[2].x, vertices[2].y))/UVScale,
                (new Vector2(properCenter.x, properCenter.y) + new Vector2(vertices[3].x, vertices[3].y))/UVScale
            };
        }


        /* Assign the parameters to the mesh */
        wallMesh.vertices = vertices;
        wallMesh.triangles = triangles;
        wallMesh.uv = UV;
        wallMesh.RecalculateNormals();

        /* Add a meshFilter and a meshRenderer to be able to draw the wall */
        wall.AddComponent<MeshFilter>();
        wall.GetComponent<MeshFilter>().mesh = wallMesh;
        wall.AddComponent<MeshRenderer>();
        wall.GetComponent<MeshRenderer>().sharedMaterial = material;

        /* Use a thick box collider for the wall's collisions */
        BoxCollider wallBox = wall.AddComponent<BoxCollider>();
        float colliderDepth = 1;

        /* The wallType and its flip value controls the boxCollider's stats */
        if(flip) {
            colliderDepth *= -1;
        }
        if(wallType == 0) {
            wallBox.size = new Vector3(xScale, Mathf.Abs(colliderDepth), zScale);
            wallBox.center = new Vector3(0, -colliderDepth/2f, 0);
        }
        else if(wallType == 1) {
            wallBox.size = new Vector3(Mathf.Abs(colliderDepth), xScale, zScale);
            wallBox.center = new Vector3(colliderDepth/2f, 0, 0);
        }
        else {
            wallBox.size = new Vector3(xScale, zScale, Mathf.Abs(colliderDepth));
            wallBox.center = new Vector3(0, 0, colliderDepth/2f);
        }
    }

    public void CreateMesh(float xScale, float zScale, ref Vector3[] vertices, ref int[] triangles, int wallType, bool flip) {
        /*
    	 * Create a mesh using the given scale values and save it's verts and triangles into the given references.
    	 * It expects the given arrays to not yet be initialized. The given boolean determines the order of the triangles.
         * 
         * Depending on the given wallType, place the vectors in their appropriate position
    	 */

        vertices = new Vector3[4];

        /* Floor using [x, z]*/
        if(wallType == 0) {
            vertices[0] = new Vector3(0.5f*xScale, 0, 0.5f*zScale);
            vertices[1] = new Vector3(-0.5f*xScale, 0, 0.5f*zScale);
            vertices[2] = new Vector3(-0.5f*xScale, 0, -0.5f*zScale);
            vertices[3] = new Vector3(0.5f*xScale, 0, -0.5f*zScale);
        }

        /* Wall parallel to X axis using [z, y] */
        else if(wallType == 1) {
            vertices[0] = new Vector3(0, 0.5f*xScale, 0.5f*zScale);
            vertices[1] = new Vector3(0, -0.5f*xScale, 0.5f*zScale);
            vertices[2] = new Vector3(0, -0.5f*xScale, -0.5f*zScale);
            vertices[3] = new Vector3(0, 0.5f*xScale, -0.5f*zScale);
        }

        /* Wall parallel to Z axis using [x, y] */
        else {
            vertices[0] = new Vector3(0.5f*xScale, 0.5f*zScale, 0);
            vertices[1] = new Vector3(-0.5f*xScale, 0.5f*zScale, 0);
            vertices[2] = new Vector3(-0.5f*xScale, -0.5f*zScale, 0);
            vertices[3] = new Vector3(0.5f*xScale, -0.5f*zScale, 0);
        }


        /* Determine the facing direction of the mesh */
        if(flip) {
            triangles = new int[]{
                0, 1, 2,
                0, 2, 3
            };
        }
        else {
            triangles = new int[]{
                2, 1, 0,
                3, 2, 0
            };
        }

    }

}
