using UnityEngine;
using System.Collections;

/* 
 * A script that builds the walls around the puzzle room. 
 * 
 * Requires a link to the exits and entrances that connect to the room.
 */
[ExecuteInEditMode]
public class PuzzleRoomEditor : MonoBehaviour {

    /* The attached rooms that connect to the exit and entrance holes */
    public AttachedRoom entrance;
    public AttachedRoom exit;

    /* The position that the entrance and the exit attached rooms will be in */
    public Transform puzzleRoomEntrancePoint;
    public Transform puzzleRoomExitPoint;

    /* The parent of all the walls that form the puzzle room */
    public GameObject puzzleRoomWalls;
    public GameObject puzzleRoomClouds;

    /* The walls that make up the puzzle room */
    public GameObject entrenceUpperWall;
    [HideInInspector]
    public GameObject entrenceLowerWall;
    [HideInInspector]
    public GameObject entrenceSideWall1;
    [HideInInspector]
    public GameObject entrenceSideWall2;
    [HideInInspector]
    public GameObject exitUpperWall;
    [HideInInspector]
    public GameObject exitLowerWall;
    [HideInInspector]
    public GameObject exitSideWall1;
    [HideInInspector]
    public GameObject exitSideWall2;
    [HideInInspector]
    public GameObject sideWall1;
    [HideInInspector]
    public GameObject sideWall2;

    /* The upper and lower "clouds" which make the puzzle room seem infinite */
    public Material cloudMaterial;
    [HideInInspector]
    public GameObject[] upperClouds;
    [HideInInspector]
    public GameObject[] lowerClouds;
    /* How many cloud meshes are used in each set */
    public int cloudAmount;
    /* The amout of distance that each cloud mesh set covers */
    public float cloudDensity;
    /* How much an offset from the center the cloud sets are */
    public float cloudOffset = 0;
    /* How much distance the clouds are from the room's center */
    public float cloudHeight;


    /* The given sizes of the room */
    public float givenRoomWidth;
    public float givenRoomHeight;
    /* The actual sizes of the room */
    private float roomWidth;
    private float attachedRoomMaxWidth;
    private float roomLength;
    private float roomHeight;

    /* The material used on the walls */
    public Material wallMaterial;

    /* Set this value to true to update the walls */
    public bool updateWalls;

    /* A box collider placed around the whole room to determine if the player is inside the puzzle room */
    public BoxCollider playerDetector;

    /* The minimum distance the player needs to be from the center before the clouds move in confunction with them.
     * Any portals should be within this range to prevent sudden cloud position changes when teleporting */
    public float minYClouds;

    /* the maximum distance the player can fall from the levels center before getting teleported to the other side. */
    public float minYTeleport;

    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {
        /*
		 * force the walls to update on startup
		 */

        updateWalls = true;
        Update();
    }

    void Update() {
        /*
         * Update the walls that form the puzzle room
         */

        if(updateWalls) {

            /* Position the attached rooms into their given positions */
            UpdateAttachedRooms();

            /* Ensure the linked wall objects are all created and properly positioned */
            CreateWalls();
            AssignMeshToWalls();

            /* Create the clouds that block the players vision from seeing to far above or bellow */
            CreateClouds();
            AssignMeshToClouds();

            /* Create and place the playerDetector collider */
            CreatePlayerDetector();

            Debug.Log("Updated walls");
            updateWalls = false;
        }

        /* Update the position of the clouds using cloudOffset */
        UpdateClouds();
    }

    void OnTriggerStay(Collider collider) {
        /*
         * If the player is within the puzzle room, check their relative height from the center.
         * If the distance is more than minYClouds, have the clouds follow the player.
         * If the distance is more than minYTeleport, teleport the player from top to bottom or vice versa.
         */
        if(collider.tag == "Player") {
            Vector3 centerPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;
            float playerFromCenter = collider.transform.position.y - centerPoint.y;

            /* Teleport the player to the other top/bottom boundary */
            if(playerFromCenter > minYTeleport || playerFromCenter < -minYTeleport) {
                /* Teleport the player to the other side */
                float newHeight = playerFromCenter - minYTeleport*2;
                collider.transform.position -= new Vector3(0, Mathf.Sign(playerFromCenter)*minYTeleport*2, 0);

                /* If the player teleported, update the playerFromCenter value */
                playerFromCenter = collider.transform.position.y - centerPoint.y;
            }

            /* Have the clouds follow the player, with it being more centered the further the player */
            if(playerFromCenter > minYClouds || playerFromCenter < -minYClouds) {
                cloudOffset = playerFromCenter;
                cloudOffset -= (minYClouds*Mathf.Sign(playerFromCenter))
                        *(1 - ((Mathf.Abs(playerFromCenter) - minYClouds)/minYTeleport));
            }

            /* Player is within the room's minimum size, do not offset the clouds */
            else {
                cloudOffset = 0;
            }

        }
    }

    /* -------- Update Functions ---------------------------------------------------- */

    private void UpdateAttachedRooms() {
        /*
         * Move the linked attached rooms to the givenpoints defined by the two puzzleRoomPoint transforms.
         * The attached rooms will be moved so their exit point shares the same transform as
         * the corresponding puzzleRoomPoint.
         */
        Vector3 positionDifference;

        /* Reposition the entrance room */
        positionDifference = entrance.exitPoint.position - puzzleRoomEntrancePoint.position;
        entrance.transform.position -= positionDifference;

        /* Reposition the exit room */
        positionDifference = exit.exitPoint.position - puzzleRoomExitPoint.position;
        exit.transform.position -= positionDifference;
    }

    private void AssignMeshToWalls() {
        /*
         * Assign a custom mesh to each wall that forms the puzzle room.
         * 
         * Also properly position and rotate the walls in this function for convenience.
         */
        float attachedEntranceWidth = entrance.exitWidth;
        float attachedEntranceHeight = entrance.exitHeight;
        float attachedExitWidth = exit.exitWidth;
        float attachedExitHeight = exit.exitHeight;


        /* Calculate the sizes of the puzzle room using the distance between the two connected rooms */
        Vector3 positionDifference = entrance.exitPoint.position - exit.exitPoint.position;
        roomLength = Mathf.Abs(positionDifference.z);
        roomWidth = givenRoomWidth + Mathf.Abs(positionDifference.x);
        attachedRoomMaxWidth = Mathf.Max(attachedEntranceWidth, attachedExitWidth)/2f;
        roomHeight = givenRoomHeight + Mathf.Abs(positionDifference.y);
        /* Track the widthDifference to properly allign the corners of the puzzle room */
        float widthDifference = positionDifference.x;


        /* Place the puzzle room's side walls to reflect the room's current width. To 
         * do this we need the point that is in the center between the two connected rooms. */
        Vector3 centerPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;
        sideWall1.transform.position = centerPoint + new Vector3(roomWidth/2f + attachedRoomMaxWidth + attachedRoomMaxWidth/2f, 0, 0);
        CreateWallMesh(sideWall1, roomLength, roomHeight);
        sideWall1.transform.localEulerAngles = new Vector3(90, -90, 0);
        sideWall2.transform.position = centerPoint + new Vector3(-roomWidth/2f - attachedRoomMaxWidth - attachedRoomMaxWidth/2f, 0, 0);
        CreateWallMesh(sideWall2, roomLength, roomHeight);
        sideWall2.transform.localEulerAngles = new Vector3(90, 90, 0);


        /* Place the walls that are situated above and bellow the puzzle room's entrance/exit */
        entrenceUpperWall.transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, roomHeight/4f + attachedEntranceHeight, 0);
        CreateWallMesh(entrenceUpperWall, attachedEntranceWidth, roomHeight/2 - attachedEntranceHeight*2);
        entrenceUpperWall.transform.localEulerAngles = new Vector3(90, 0, 0);

        entrenceLowerWall.transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, -roomHeight/4f, 0);
        CreateWallMesh(entrenceLowerWall, attachedEntranceWidth, roomHeight/2);
        entrenceLowerWall.transform.localEulerAngles = new Vector3(90, 0, 0);

        exitUpperWall.transform.position = puzzleRoomExitPoint.position + new Vector3(0, roomHeight/4f + attachedExitHeight, 0);
        CreateWallMesh(exitUpperWall, attachedEntranceWidth, roomHeight/2 - attachedExitHeight*2f);
        exitUpperWall.transform.localEulerAngles = new Vector3(-90, 0, 0);

        exitLowerWall.transform.position = puzzleRoomExitPoint.position + new Vector3(0, -roomHeight/4f, 0);
        CreateWallMesh(exitLowerWall, attachedEntranceWidth, roomHeight/2);
        exitLowerWall.transform.localEulerAngles = new Vector3(-90, 0, 0);


        /* Place the walls that are on the side of the entrance/exit */
        entrenceSideWall1.transform.position = puzzleRoomEntrancePoint.position + new Vector3(roomWidth/4f + attachedEntranceWidth/2f - widthDifference/4f + attachedRoomMaxWidth/4f, 0, 0);
        CreateWallMesh(entrenceSideWall1, roomWidth/2f - widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        entrenceSideWall1.transform.localEulerAngles = new Vector3(90, 0, 0);

        entrenceSideWall2.transform.position = puzzleRoomEntrancePoint.position + new Vector3(-roomWidth/4f - attachedEntranceWidth/2f - widthDifference/4f - attachedRoomMaxWidth/4f, 0, 0);
        CreateWallMesh(entrenceSideWall2, roomWidth/2f + widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        entrenceSideWall2.transform.localEulerAngles = new Vector3(90, 0, 0);

        exitSideWall1.transform.position = puzzleRoomExitPoint.position + new Vector3(-roomWidth/4f - attachedExitWidth/2f + widthDifference/4f - attachedRoomMaxWidth/4f, 0, 0);
        CreateWallMesh(exitSideWall1, roomWidth/2f - widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        exitSideWall1.transform.localEulerAngles = new Vector3(-90, 0, 0);

        exitSideWall2.transform.position = puzzleRoomExitPoint.position + new Vector3(+roomWidth/4f + attachedExitWidth/2f + widthDifference/4f + attachedRoomMaxWidth/4f, 0, 0);
        CreateWallMesh(exitSideWall2, roomWidth/2f + widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        exitSideWall2.transform.localEulerAngles = new Vector3(-90, 0, 0);
    }

    private void AssignMeshToClouds() {
        /*
         * Assign a mesh to each cloud object and position them on the top and bottom edges of the room
         */
        Vector3 centerPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;


        /* Position the upper clouds */
        for(int i = 0; i < upperClouds.Length; i++) {
            upperClouds[i].transform.position = centerPoint + new Vector3(0, cloudHeight - cloudDensity*((float) i/upperClouds.Length), 0);
            upperClouds[i].transform.localEulerAngles = new Vector3(180, 0, 0);
            CreateWallMesh(upperClouds[i], roomWidth + attachedRoomMaxWidth*2 + attachedRoomMaxWidth, roomLength);
        }

        /* Position the lower clouds */
        for(int i = 0; i < lowerClouds.Length; i++) {
            lowerClouds[i].transform.position = centerPoint + new Vector3(0, -cloudHeight + cloudDensity*((float) i/upperClouds.Length), 0);
            CreateWallMesh(lowerClouds[i], roomWidth + attachedRoomMaxWidth*2 + attachedRoomMaxWidth, roomLength);
        }
    }

    public void UpdateClouds() {
        /*
		 * Set the position of the cloud's container to move them all relative to the player's position
		 */

        puzzleRoomClouds.transform.position = new Vector3(0, cloudOffset, 0);
    }

    /* -------- Initilizing Functions ---------------------------------------------------- */

    private void CreateWalls() {
        /*
         * Re-create the walls that form the puzzle room.
         */

        /* Use an array to hold all the walls that will be used to form the puzzle room */
        GameObject[] walls = { entrenceUpperWall, entrenceLowerWall, entrenceSideWall1, entrenceSideWall2,
                exitUpperWall, exitLowerWall, exitSideWall1, exitSideWall2, sideWall1, sideWall2};

        /* Each wall must be capable of rendering an object and have the proper parent, name and material used */
        for(int i = 0; i < walls.Length; i++) {
            if(walls[i] != null) {
                DestroyImmediate(walls[i]);
            }

            walls[i] = new GameObject();
            walls[i].AddComponent<MeshRenderer>();
            walls[i].transform.parent = puzzleRoomWalls.transform;
            walls[i].name = "Infinite Wall";

            /* Assign a material to the wall */
            walls[i].GetComponent<MeshRenderer>().material = wallMaterial;
        }


        /* Reassign each wall to the changed values in the array */
        //If this needs to run, imdoing something wrong
        entrenceUpperWall = walls[0];
        entrenceLowerWall = walls[1];
        entrenceSideWall1 = walls[2];
        entrenceSideWall2 = walls[3];
        exitUpperWall = walls[4];
        exitLowerWall = walls[5];
        exitSideWall1 = walls[6];
        exitSideWall2 = walls[7];
        sideWall1 = walls[8];
        sideWall2 = walls[9];
    }

    private void CreateClouds() {
        /*
         * Initlize flat meshes at the upper and lower limits of the puzzle room if they do not already exist
         */

        /* Empty the clouds array of old clouds before recreating them */
        if(upperClouds != null) {
            for(int i = 0; i < upperClouds.Length; i++) {
                if(upperClouds[i] != null) {
                    DestroyImmediate(upperClouds[i]);
                }
            }
        }
        if(lowerClouds != null) {
            for(int i = 0; i < lowerClouds.Length; i++) {
                if(lowerClouds[i] != null) {
                    DestroyImmediate(lowerClouds[i]);
                }
            }
        }

        /* Resize the arrays that hold the clouds */
        upperClouds = new GameObject[cloudAmount];
        lowerClouds = new GameObject[cloudAmount];

        /* Change the clouds texture to reflect the cloudDensity */
        cloudMaterial.color = new Color(0, 0, 0, (1f/cloudAmount)/2f);


        /* The clouds */
        for(int i = 0; i < upperClouds.Length; i++) {
            //upperClouds[i] = GameObject.CreatePrimitive(PrimitiveType.Plane);
            upperClouds[i] = new GameObject();
            upperClouds[i].AddComponent<MeshRenderer>();
            upperClouds[i].transform.parent = puzzleRoomClouds.transform;
            upperClouds[i].name = "Upper Clouds";
            upperClouds[i].GetComponent<MeshRenderer>().material = cloudMaterial;

        }

        for(int i = 0; i < lowerClouds.Length; i++) {
            //lowerClouds[i] = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lowerClouds[i] = new GameObject();
            lowerClouds[i].AddComponent<MeshRenderer>();
            lowerClouds[i].transform.parent = puzzleRoomClouds.transform;
            lowerClouds[i].name = "Lower Clouds";
            lowerClouds[i].GetComponent<MeshRenderer>().material = cloudMaterial;

        }
    }

    private void CreatePlayerDetector() {
        //ADD A CLOUD OFFSET VALUE ALONG WITH A MAX HEIGHT/MIN HEIGHT

        if(playerDetector != null) {
            DestroyImmediate(playerDetector);
        }

        /* Attach the playerDetector to this object to have access to it */
        playerDetector = gameObject.AddComponent<BoxCollider>();
        playerDetector.isTrigger = true;

        /* Set the sizes of the collider to be equal to the whole room */
        playerDetector.center = new Vector3(0, 0, roomLength/2f);
        playerDetector.size = new Vector3(roomWidth+attachedRoomMaxWidth*3, roomHeight, roomLength);


    }

    /* -------- Helper Functions ---------------------------------------------------- */

    private void CreateWallMesh(GameObject wall, float xScale, float zScale) {
        /*
         * Use the given parameters to create the mesh that forms a wall.
         * The mesh will be centered at (0, 0, 0).
         * 
         * The given gameObject will have the mesh used in it's meshFilter component changed.
         */
        Mesh wallMesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] UV;
        int[] triangles;


        /* Set the vertices of the plane */
        vertices[0] = new Vector3(0.5f*xScale, 0, 0.5f*zScale);
        vertices[1] = new Vector3(-0.5f*xScale, 0, 0.5f*zScale);
        vertices[2] = new Vector3(-0.5f*xScale, 0, -0.5f*zScale);
        vertices[3] = new Vector3(0.5f*xScale, 0, -0.5f*zScale);


        /* Set the two triangles that form the plane */
        triangles = new int[]{
            2, 1, 0,
            3, 2, 0
        };

        /* Set the UVs of the plane */
        UV = new Vector2[] {
            new Vector2(vertices[0].x, vertices[0].z),
            new Vector2(vertices[1].x, vertices[1].z),
            new Vector2(vertices[2].x, vertices[2].z),
            new Vector2(vertices[3].x, vertices[3].z)
        };


        /* Assign the parameters to the mesh */
        wallMesh.vertices = vertices;
        wallMesh.triangles = triangles;
        wallMesh.uv = UV;
        wallMesh.RecalculateNormals();

        /* Add a meshFilter and a meshCollider that will use the newly created mesh */
        wall.AddComponent<MeshFilter>();
        wall.AddComponent<MeshCollider>();

        /* Assign the mesh to the given wall */
        wall.GetComponent<MeshFilter>().mesh = wallMesh;
        wall.GetComponent<MeshCollider>().sharedMesh = wallMesh;

    }
}
