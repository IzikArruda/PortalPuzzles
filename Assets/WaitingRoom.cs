using UnityEngine;
using System.Collections;

/* 
 * A WaitingRoom is a room that connects two AttachedRooms. It is not a puzzle room 
 * and serves to put more distance between each puzzle room. A waiting room
 * has the shape of a Z tetromino to ensure the player will not see more than 2 puzzle rooms at once.
 */
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
        CreatePlane(floor, width, lengthDifference);
        floor.transform.parent = roomObjectsContainer;
        floor.transform.position = center;
        floor.transform.localEulerAngles = new Vector3(0, 0, 0);
        floor.transform.localScale = new Vector3(1, 1, 1);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

        /* Create the left wall */
        leftWall = new GameObject();
        leftWall.name = "Left wall";
        CreatePlane(leftWall, height, length);
        leftWall.transform.parent = roomObjectsContainer;
        leftWall.transform.position = center + new Vector3(-width/2f, height/2f, 0);
        leftWall.transform.localEulerAngles = new Vector3(0, 0, -90);
        leftWall.transform.localScale = new Vector3(1, 1, 1);
        leftWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the right wall */
        rightWall = new GameObject();
        rightWall.name = "Right wall";
        CreatePlane(rightWall, height, length);
        rightWall.transform.parent = roomObjectsContainer;
        rightWall.transform.position = center + new Vector3(width/2f, height/2f, 0);
        rightWall.transform.localEulerAngles = new Vector3(0, 0, 90);
        rightWall.transform.localScale = new Vector3(1, 1, 1);
        rightWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the ceiling */
        ceiling = new GameObject();
        ceiling.name = "Ceiling";
        CreatePlane(ceiling, width, lengthDifference);
        ceiling.transform.parent = roomObjectsContainer;
        ceiling.transform.position = center + new Vector3(0, height, 0);
        ceiling.transform.localEulerAngles = new Vector3(0, 0, 180);
        ceiling.transform.localScale = new Vector3(1, 1, 1);
        ceiling.GetComponent<MeshRenderer>().sharedMaterial = ceilingMaterial;

        /* Create the Entrance side wall */
        entranceWall = new GameObject();
        entranceWall.name = "Entrance wall";
        CreatePlane(entranceWall, width - entranceWidth, height);
        entranceWall.transform.parent = roomObjectsContainer;
        entranceWall.transform.position = center + new Vector3(entranceWidth/2f, height/2f, -length/2f);
        entranceWall.transform.localEulerAngles = new Vector3(90, 0, 0);
        entranceWall.transform.localScale = new Vector3(1, 1, 1);
        entranceWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the Exit side wall */
        exitWall = new GameObject();
        exitWall.name = "Entrance wall";
        CreatePlane(exitWall, width - exitWidth, height);
        exitWall.transform.parent = roomObjectsContainer;
        exitWall.transform.position = center + new Vector3(-exitWidth/2f, height/2f, length/2f);
        exitWall.transform.localEulerAngles = new Vector3(-90, 0, 0);
        exitWall.transform.localScale = new Vector3(1, 1, 1);
        exitWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the Above Entrance wall */
        aboveEntranceWall = new GameObject();
        aboveEntranceWall.name = "Above Entrance wall";
        CreatePlane(aboveEntranceWall, entranceWidth, height - entranceHeight);
        aboveEntranceWall.transform.parent = roomObjectsContainer;
        aboveEntranceWall.transform.position = center + new Vector3(-width/2f + entranceWidth/2f, height - (height - entranceHeight)/2f, -length/2f);
        aboveEntranceWall.transform.localEulerAngles = new Vector3(90, 0, 0);
        aboveEntranceWall.transform.localScale = new Vector3(1, 1, 1);
        aboveEntranceWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the Above Exit wall */
        aboveExitWall = new GameObject();
        aboveExitWall.name = "Above Exit wall";
        CreatePlane(aboveExitWall, exitWidth, height - exitHeight);
        aboveExitWall.transform.parent = roomObjectsContainer;
        aboveExitWall.transform.position = center + new Vector3(width/2f - exitWidth/2f, height - (height - exitHeight)/2f, length/2f);
        aboveExitWall.transform.localEulerAngles = new Vector3(-90, 0, 0);
        aboveExitWall.transform.localScale = new Vector3(1, 1, 1);
        aboveExitWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;
    }

    public void DeleteRoom() {
        /*
         * Delete each renderable gameObject used to make up the room
         */

        if(floor != null) {
            DestroyImmediate(floor);
        }
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    public void CreatePlane(GameObject wall, float xScale, float zScale) {
        /*
         * Create a plane onto the given gameObject
         */
        Mesh wallMesh = new Mesh();
        Vector3[] vertices = null;
        Vector2[] UV;
        int[] triangles = null;

        /* Use the meshCreator function to create the basics of the wall */
        CreateMesh(xScale, zScale, ref vertices, ref triangles);

        /* Set the UVs of the plane */
        UV = new Vector2[] {
            new Vector2(0, 0),
            new Vector2(xScale/8f, 0),
            new Vector2(xScale/8f, zScale/8f),
            new Vector2(0, zScale/8f)
        };

        /* Assign the parameters to the mesh */
        wallMesh.vertices = vertices;
        wallMesh.triangles = triangles;
        wallMesh.uv = UV;
        wallMesh.RecalculateNormals();

        /* Add a meshFilter and a meshRenderer to be able to draw the wall */
        wall.AddComponent<MeshFilter>();
        wall.GetComponent<MeshFilter>().mesh = wallMesh;
        wall.AddComponent<MeshRenderer>();

        /* Use a thick box collider for the wall's collisions */
        BoxCollider wallBox = wall.AddComponent<BoxCollider>();
        float colliderDepth = 1;
        wallBox.center = new Vector3(0, -colliderDepth/2f, 0);
        wallBox.size = new Vector3(xScale, colliderDepth, zScale);
    }

    public void CreateMesh(float xScale, float zScale, ref Vector3[] vertices, ref int[] triangles) {
        /*
    	 * Create a mesh using the given scale values and save it's verts and triangles into the given references.
    	 *
    	 * It expects the given arrays to not yet be initialized.
    	 */

        vertices = new Vector3[4];
        vertices[0] = new Vector3(0.5f*xScale, 0, 0.5f*zScale);
        vertices[1] = new Vector3(-0.5f*xScale, 0, 0.5f*zScale);
        vertices[2] = new Vector3(-0.5f*xScale, 0, -0.5f*zScale);
        vertices[3] = new Vector3(0.5f*xScale, 0, -0.5f*zScale);

        triangles = new int[]{
            2, 1, 0,
            3, 2, 0
        };
    }

}
