using UnityEngine;
using System.Collections;

/*
 * This script is used to connect puzzle rooms to non-puzzle rooms. Certain stats of this room 
 * need to be tracked so that it can properly connect the two rooms, such as the exit sizes.
 * This room always uses 4 flat planes as it's walls and is created procedurally using the exit points.
 */
[ExecuteInEditMode]
public class AttachedRoom : MonoBehaviour {

    /* The two exit points of the room's two exits. Used to connect rooms. */
    public Transform exitPointFront;
    public Transform exitPointBack;

    /* The reset point of the room. Determines where the player will spawn when they restart using this room */
    public Transform resetPoint;

    /* The objects that comprise this room */
    public GameObject floor;
    public GameObject leftWall;
    public GameObject rightWall;
    public GameObject ceiling;

    /* The container to hold all the objects in the room */
    public Transform roomObjectsContainer;

    /* The materials used for the room */
    public Material floorMaterial;
    public Material wallMaterial;
    public Material ceilingMaterial;

    /* The size of the exit of this room. Used by outside functions and requires user input to set. */
    public float exitWidth;
    public float exitHeight;

    /* The trigger that encompasses the entire room. Used to detect when the player enters it */
    public BoxCollider roomCollider;


    /* -------- Built-In Functions ---------------------------------------------------- */
    
    public void Start() {
        /*
         * On startup, update the walls for now
         */

        UpdateWalls();
    }

    void OnTriggerEnter(Collider player) {
        /*
         * When the player enters the room's trigger, change their linked attachedRoom.
         */
        Debug.Log("test");

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            /* Tell the CustomPlayerController to change their linked attachedRoom */
            player.GetComponent<CustomPlayerController>().ChangeLastRoom(this);
        }
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void UpdateWalls() {
        /*
         * Look at the position of the exit points and create the walls for the room
         */
        float depth = 0;

        /* Get the depth of the room using the exit points. */
        Vector3 pointDifference = exitPointFront.localPosition - exitPointBack.localPosition;
        /* Using localPosition means we are only expecting positive values in Z */
        if(pointDifference.x != 0 || pointDifference.y != 0 || pointDifference.z <= 0) {
            Debug.Log("WARNING: Attached room's exit points are not alligned properly");
        }else {
            depth = pointDifference.z;
        }

        /* Get the center position of the room's floor */
        Vector3 roomCenter = (exitPointFront.position + exitPointBack.position)/2f;

        /* Re-create the main trigger for the room with the new sizes */
        if(roomCollider != null) { DestroyImmediate(roomCollider); }
        roomCollider = gameObject.AddComponent<BoxCollider>();
        roomCollider.isTrigger = true;
        roomCollider.center = -transform.localPosition + roomCenter + new Vector3(0, exitHeight/2f, 0);
        roomCollider.size = new Vector3(exitWidth, exitHeight, depth);

        /* Delete the current parts of the room if they exist */
        DeleteRoom();

        /* Create the floor */
        floor = new GameObject();
        floor.name = "Floor";
        CreatePlane(floor, exitWidth, depth);
        floor.transform.parent = roomObjectsContainer;
        floor.transform.position = roomCenter;
        floor.transform.localEulerAngles = new Vector3(0, 0, 0);
        floor.transform.localScale = new Vector3(1, 1, 1);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;

        /* Create the left wall */
        leftWall = new GameObject();
        leftWall.name = "Left wall";
        CreatePlane(leftWall, exitHeight, depth);
        leftWall.transform.parent = roomObjectsContainer;
        leftWall.transform.position = roomCenter + new Vector3(-exitWidth/2f, exitHeight/2f, 0);
        leftWall.transform.localEulerAngles = new Vector3(0, 0, -90);
        leftWall.transform.localScale = new Vector3(1, 1, 1);
        leftWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the right wall */
        rightWall = new GameObject();
        rightWall.name = "Right wall";
        CreatePlane(rightWall, exitHeight, depth);
        rightWall.transform.parent = roomObjectsContainer;
        rightWall.transform.position = roomCenter + new Vector3(exitWidth/2f, exitHeight/2f, 0);
        rightWall.transform.localEulerAngles = new Vector3(0, 0, 90);
        rightWall.transform.localScale = new Vector3(1, 1, 1);
        rightWall.GetComponent<MeshRenderer>().sharedMaterial = wallMaterial;

        /* Create the ceiling */
        ceiling = new GameObject();
        ceiling.name = "Ceiling";
        CreatePlane(ceiling, exitWidth, depth);
        ceiling.transform.parent = roomObjectsContainer;
        ceiling.transform.position = roomCenter + new Vector3(0, exitHeight, 0);
        ceiling.transform.localEulerAngles = new Vector3(0, 0, 180);
        ceiling.transform.localScale = new Vector3(1, 1, 1);
        ceiling.GetComponent<MeshRenderer>().sharedMaterial = ceilingMaterial;

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
        if(ceiling != null) {
            DestroyImmediate(ceiling);
        }
    }
    
    public Transform ResetPlayer() {
        /*
         * Return the resetPoint of this room for the player to use as a reset point
         */

        return resetPoint;
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