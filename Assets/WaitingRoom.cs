using UnityEngine;
using System.Collections;

/* 
 * A WaitingRoom is a room that connects two AttachedRooms. It is not a puzzle room 
 * and serves to put more distance between each puzzle room. A waiting room
 * has the shape of a Z tetromino to ensure the player will not see more than 2 puzzle rooms at once.
 */
[ExecuteInEditMode]
public class WaitingRoom : MonoBehaviour {

    /* The two connected AttachedRooms */
    public AttachedRoom entranceRoom;
    public AttachedRoom exitRoom;

    /* The main objects that form the room */
    public GameObject floor;
    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject entranceWall;
    public GameObject exitWall;
    public GameObject aboveEntranceWall;
    public GameObject aboveExitWall;
    public GameObject ceiling;
    
    /* The container to hold all the objects in the room */
    public Transform roomObjectsContainer;

    /* The materials used for the room */
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;



    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start () {
        /*
         * On start-up, recreate the room's skeleton
         */

        UpdateRoom();
	}


    /* -------- Event Functions ---------------------------------------------------- */

    void UpdateRoom() {
        /*
         * Given the position of the attached rooms, re-create this room's bounderies
         */


        /* Get the sizes of the two attached room */
        float entranceWidth = entranceRoom.exitWidth;
        float entranceHeight = entranceRoom.exitHeight;
        float exitWidth = exitRoom.exitWidth;
        float exitHeight = exitRoom.exitHeight;
        float widthDifference = Mathf.Abs(entranceRoom.exitPointFront.position.x - exitRoom.exitPointBack.position.x);
        float lengthDifference = Mathf.Abs(entranceRoom.exitPointFront.position.z - exitRoom.exitPointBack.position.z);

        /* Re-position the room to the center position between the two attachedRooms */
        Vector3 center = (entranceRoom.exitPointFront.position + exitRoom.exitPointBack.position)/2f;
        center += new Vector3(Mathf.Abs(entranceWidth/2f - exitWidth/2f)/2f, 0, 0);

        /* Calculate the sizes of this waitingRoom */
        float width = widthDifference + entranceWidth/2f + exitWidth/2f;
        float length = lengthDifference;
        float height = Mathf.Max(entranceHeight, exitHeight);

        /* Delete the current parts of the room if they exist */
        DeleteRoom();

        /* Create the floor */
        floor = new GameObject();
        floor.name = "Floor";
        floor.transform.parent = roomObjectsContainer;
        floor.transform.position = center;
        floor.transform.localEulerAngles = new Vector3(0, 0, 0);
        floor.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(floor, width, lengthDifference, 8, floorMaterial, 0, false);

        /* Create the left wall */
        leftWall = new GameObject();
        leftWall.name = "Left wall";
        leftWall.transform.parent = roomObjectsContainer;
        leftWall.transform.position = center + new Vector3(-width/2f, height/2f, 0);
        leftWall.transform.localEulerAngles = new Vector3(0, 0, 0);
        leftWall.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(leftWall, height, length, 8, wallMaterial, 1, true);

        /* Create the right wall */
        rightWall = new GameObject();
        rightWall.name = "Right wall";
        rightWall.transform.parent = roomObjectsContainer;
        rightWall.transform.position = center + new Vector3(width/2f, height/2f, 0);
        rightWall.transform.localEulerAngles = new Vector3(0, 0, 0);
        rightWall.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(rightWall, height, length, 8, wallMaterial, 1, false);

        /* Create the ceiling */
        ceiling = new GameObject();
        ceiling.name = "Ceiling";
        ceiling.transform.parent = roomObjectsContainer;
        ceiling.transform.position = center + new Vector3(0, height, 0);
        ceiling.transform.localEulerAngles = new Vector3(0, 0, 0);
        ceiling.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(ceiling, width, lengthDifference, 8, ceilingMaterial, 0, true);

        /* Create the Entrance side wall */
        entranceWall = new GameObject();
        entranceWall.name = "Entrance side wall";
        entranceWall.transform.parent = roomObjectsContainer;
        entranceWall.transform.position = center + new Vector3(entranceWidth/2f, height/2f, -length/2f);
        entranceWall.transform.localEulerAngles = new Vector3(0, 0, 0);
        entranceWall.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(entranceWall, width - entranceWidth, height, 8, wallMaterial, 2, true);

        /* Create the Exit side wall */
        exitWall = new GameObject();
        exitWall.name = "Exit side wall";
        exitWall.transform.parent = roomObjectsContainer;
        exitWall.transform.position = center + new Vector3(-exitWidth/2f, height/2f, length/2f);
        exitWall.transform.localEulerAngles = new Vector3(0, 0, 0);
        exitWall.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(exitWall, width - exitWidth, height, 8, wallMaterial, 2, false);

        /* Create the Above Entrance wall */
        aboveEntranceWall = new GameObject();
        aboveEntranceWall.name = "Above Entrance wall";
        aboveEntranceWall.transform.parent = roomObjectsContainer;
        aboveEntranceWall.transform.position = center + new Vector3(-width/2f + entranceWidth/2f, height - (height - entranceHeight)/2f, -length/2f);
        aboveEntranceWall.transform.localEulerAngles = new Vector3(0, 0, 0);
        aboveEntranceWall.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(aboveEntranceWall, entranceWidth, height - entranceHeight, 8, wallMaterial, 2, true);

        /* Create the Above Exit wall */
        aboveExitWall = new GameObject();
        aboveExitWall.name = "Above Exit wall";
        aboveExitWall.transform.parent = roomObjectsContainer;
        aboveExitWall.transform.position = center + new Vector3(width/2f - exitWidth/2f, height - (height - exitHeight)/2f, length/2f);
        aboveExitWall.transform.localEulerAngles = new Vector3(0, 0, 0);
        aboveExitWall.transform.localScale = new Vector3(1, 1, 1);
        CreatePlane(aboveExitWall, exitWidth, height - exitHeight, 8, wallMaterial, 2, false);
    }

    public void DeleteRoom() {
        /*
         * Delete each renderable gameObject used to make up the room
         */

        if(floor != null) {
            DestroyImmediate(floor);
        }
        if(leftWall != null) {
            DestroyImmediate(leftWall);
        }
        if(rightWall != null) {
            DestroyImmediate(rightWall);
        }
        if(entranceWall != null) {
            DestroyImmediate(entranceWall);
        }
        if(exitWall != null) {
            DestroyImmediate(exitWall);
        }
        if(aboveEntranceWall != null) {
            DestroyImmediate(aboveEntranceWall);
        }
        if(aboveExitWall != null) {
            DestroyImmediate(aboveExitWall);
        }
        if(ceiling != null) {
            DestroyImmediate(ceiling);
        }
}


    /* -------- Helper Functions ---------------------------------------------------- */

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
        else if(wallType == 1){
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
        wallBox.center = new Vector3(0, -colliderDepth/2f, 0);
        wallBox.size = new Vector3(xScale, colliderDepth, zScale);
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
