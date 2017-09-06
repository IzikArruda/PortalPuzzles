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

    /* The walls that make up the puzzle room. Each wall has a specific place in the array and the room */
    [HideInInspector]
    public GameObject[] walls;

    /* The upper and lower "clouds" which make the puzzle room seem infinite */
    public Material cloudMaterial;
    [HideInInspector]
    public GameObject upperClouds;
    [HideInInspector]
    public GameObject lowerClouds;
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

    /* Stats on the room calculated once initialized */
    //Distance between
    private float roomLength;
    private float roomHeight;
    private float roomWidth;
    private float attachedRoomMaxWidth;
    private float fullRoomWidth;
    private Vector3 roomPositionDifference;
    private Vector3 roomCenterPoint;

    /* The material used on the walls */
    public Material wallMaterial;

    /* Set this value to true to update the walls */
    public bool updateWalls;

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

			/* Calculate the new positionnal stats of this puzzleRoom */
			UpdateSizes();

            /* Move the attached rooms into their given positions */
            UpdateAttachedRooms();

            /* Ensure the walls and clouds that form the room are properly created and positioned */
            CreateWalls();
            CreateClouds();

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
            //Get the distance that the playerCamera is from the room's center
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
                        *(1 - ((Mathf.Abs(playerFromCenter) - minYClouds)/(minYTeleport-minYClouds)));
            }

            /* Player is within the room's minimum size, do not offset the clouds */
            else {
                cloudOffset = 0;
            }
        }
    }


    /* -------- Update Functions ---------------------------------------------------- */

	public void UpdateSizes(){
        /*
	     * Calculate the sizes of the room to be used with the creation functions
	     */

        /* Calculate the sizes of the puzzle room using the distance between the two connected rooms */
        roomPositionDifference = entrance.exitPoint.position - exit.exitPoint.position;
        roomLength = Mathf.Abs(roomPositionDifference.z);
        roomHeight = givenRoomHeight + Mathf.Abs(roomPositionDifference.y);
        roomWidth = givenRoomWidth + Mathf.Abs(roomPositionDifference.x);
        attachedRoomMaxWidth = Mathf.Max(entrance.exitWidth, exit.exitWidth)/2f;
        roomCenterPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;
        fullRoomWidth = roomWidth + attachedRoomMaxWidth*3;
    }
    
    private void UpdateAttachedRooms() {
        /*
         * Move the linked attached rooms to the givenpoints defined by the two puzzleRoomPoint transforms.
         * The attached rooms will be moved so their exit point shares the same transform as
         * the corresponding puzzleRoomPoint.
         */
        Vector3 distanceToExit;

        /* Reposition the entrance room */
        distanceToExit = entrance.exitPoint.position - puzzleRoomEntrancePoint.transform.position;
        entrance.transform.position -= distanceToExit;

        /* Reposition the exit room */
        distanceToExit = exit.exitPoint.position - puzzleRoomExitPoint.transform.position;
        exit.transform.position -= distanceToExit;
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
        float attachedEntranceWidth = entrance.exitWidth;
        float attachedEntranceHeight = entrance.exitHeight;
        float attachedExitWidth = exit.exitWidth;
        float attachedExitHeight = exit.exitHeight;
        float widthDifference = roomPositionDifference.x;

        /* Ensure the walls array is emptied before creating new ones */
        if(walls != null) {
            for(int i = 0; i < walls.Length; i++) {
                if(walls[i] != null) {
                    DestroyImmediate(walls[i]);
                }
            }
        }

        /* Create the new walls array for the 10 walls that form the puzzle room */
        walls = new GameObject[10];
        
        /* Create and place the room's side walls that reflect the room's width */
        walls[0] = new GameObject();
        walls[0].name = "Left Wall";
        CreateWallMesh(walls[0], roomLength, roomHeight);
        walls[0].transform.parent = puzzleRoomWalls.transform;
        walls[0].transform.position = roomCenterPoint + new Vector3(fullRoomWidth/2f, 0, 0);
        walls[0].transform.localEulerAngles = new Vector3(90, -90, 0);

        walls[1] = new GameObject();
        walls[1].name = "Right wall";
        CreateWallMesh(walls[1], roomLength, roomHeight);
        walls[1].transform.parent = puzzleRoomWalls.transform;
        walls[1].transform.position = roomCenterPoint + new Vector3(-fullRoomWidth/2f, 0, 0);
        walls[1].transform.localEulerAngles = new Vector3(90, 90, 0);


        /* Create and place the walls that are situated above and bellow the room's entrance/exit */
        walls[2] = new GameObject();
        walls[2].name = "Entrance wall above";
        CreateWallMesh(walls[2], attachedEntranceWidth, roomHeight/2 - attachedEntranceHeight*2);
        walls[2].transform.parent = puzzleRoomWalls.transform;
        walls[2].transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, roomHeight/4f + attachedEntranceHeight, 0);
        walls[2].transform.localEulerAngles = new Vector3(90, 0, 0);

        walls[3] = new GameObject();
        walls[3].name = "Entrance wall bellow";
        CreateWallMesh(walls[3], attachedEntranceWidth, roomHeight/2);
        walls[3].transform.parent = puzzleRoomWalls.transform;
        walls[3].transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, -roomHeight/4f, 0);
        walls[3].transform.localEulerAngles = new Vector3(90, 0, 0);

        walls[4] = new GameObject();
        walls[4].name = "Exit wall above";
        CreateWallMesh(walls[4], attachedEntranceWidth, roomHeight/2 - attachedExitHeight*2f);
        walls[4].transform.parent = puzzleRoomWalls.transform;
        walls[4].transform.position = puzzleRoomExitPoint.position + new Vector3(0, roomHeight/4f + attachedExitHeight, 0);
        walls[4].transform.localEulerAngles = new Vector3(-90, 0, 0);

        walls[5] = new GameObject();
        walls[5].name = "Exit wall lower";
        CreateWallMesh(walls[5], attachedEntranceWidth, roomHeight/2);
        walls[5].transform.parent = puzzleRoomWalls.transform;
        walls[5].transform.position = puzzleRoomExitPoint.position + new Vector3(0, -roomHeight/4f, 0);
        walls[5].transform.localEulerAngles = new Vector3(-90, 0, 0);


        /* Create and place the walls that are on the left and right of the entrance/exit */
        walls[6] = new GameObject();
        walls[6].name = "Entrance wall left";
        CreateWallMesh(walls[6], roomWidth/2f - widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        walls[6].transform.parent = puzzleRoomWalls.transform;
        walls[6].transform.position = puzzleRoomEntrancePoint.position + new Vector3(roomWidth/4f + attachedEntranceWidth/2f - widthDifference/4f + attachedRoomMaxWidth/4f, 0, 0);
        walls[6].transform.localEulerAngles = new Vector3(90, 0, 0);

        walls[7] = new GameObject();
        walls[7].name = "Entrance right wall";
        walls[7].transform.parent = puzzleRoomWalls.transform;
        walls[7].transform.position = puzzleRoomEntrancePoint.position + new Vector3(-roomWidth/4f - attachedEntranceWidth/2f - widthDifference/4f - attachedRoomMaxWidth/4f, 0, 0);
        CreateWallMesh(walls[7], roomWidth/2f + widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        walls[7].transform.localEulerAngles = new Vector3(90, 0, 0);

        walls[8] = new GameObject();
        walls[8].name = "Exit wall left";
        CreateWallMesh(walls[8], roomWidth/2f - widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        walls[8].transform.parent = puzzleRoomWalls.transform;
        walls[8].transform.position = puzzleRoomExitPoint.position + new Vector3(-roomWidth/4f - attachedExitWidth/2f + widthDifference/4f - attachedRoomMaxWidth/4f, 0, 0);
        walls[8].transform.localEulerAngles = new Vector3(-90, 0, 0);

        walls[9] = new GameObject();
        walls[9].name = "Exit wall right";
        CreateWallMesh(walls[9], roomWidth/2f + widthDifference/2f + attachedRoomMaxWidth/2f, roomHeight);
        walls[9].transform.parent = puzzleRoomWalls.transform;
        walls[9].transform.position = puzzleRoomExitPoint.position + new Vector3(+roomWidth/4f + attachedExitWidth/2f + widthDifference/4f + attachedRoomMaxWidth/4f, 0, 0);
        walls[9].transform.localEulerAngles = new Vector3(-90, 0, 0);
    }

    private void CreateClouds() {
        /*
         * Create two sets of flat meshes at the upper and lower bounds of the puzzle room.
         * They are used to make the puzzle room seem endless to the player and to mask the 
         * teleport the player undergoes if they fall too far from the room's center.
         */

        /* Remove the previously created clouds before recreating them */
        if(upperClouds != null) {
            DestroyImmediate(upperClouds);
        }
        if(lowerClouds != null) {
            DestroyImmediate(lowerClouds);
        }

        /* Change the clouds texture to reflect the cloudDensity */
        cloudMaterial.color = new Color(0, 0, 0, (1f/cloudAmount)/2f);
        
        /* Create and position new upperClouds */
        upperClouds = new GameObject();
        CreateCloudsMesh(upperClouds, roomWidth + attachedRoomMaxWidth*3, roomLength, cloudAmount, cloudDensity);
        upperClouds.name = "Upper Clouds";
        upperClouds.transform.parent = puzzleRoomClouds.transform;
        upperClouds.transform.position = roomCenterPoint + new Vector3(0, cloudHeight, 0);
        upperClouds.transform.localEulerAngles = new Vector3(180, 0, 0);

        /* Create and position new lowerclouds */
        lowerClouds = new GameObject();
        CreateCloudsMesh(lowerClouds, roomWidth + attachedRoomMaxWidth*3, roomLength, cloudAmount, cloudDensity);
        lowerClouds.name = "Lower Clouds";
        lowerClouds.transform.parent = puzzleRoomClouds.transform;
        lowerClouds.transform.position = roomCenterPoint + new Vector3(0, -cloudHeight, 0);
    }

    private void CreatePlayerDetector() {
        /*
         * Ensure there is a BoxCollider attached to this gameObject that will be used 
         * to detect the player's position in the room. It spans the entire room.
         */

        /* Make sure the BoxCollider exists */
        if(GetComponent<BoxCollider>() == null) {
            gameObject.AddComponent<BoxCollider>();
        }

        /* It must be a trigger to allow the player to move inside the room */
        GetComponent<BoxCollider>().isTrigger = true;

        /* Set the sizes of the collider to be equal to the whole room */
        GetComponent<BoxCollider>().center = new Vector3(0, 0, roomLength/2f);
        GetComponent<BoxCollider>().size = new Vector3(fullRoomWidth, roomHeight, roomLength);
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    private void CreateWallMesh(GameObject wall, float xScale, float zScale) {
        /*
         * Use the given parameters to create the mesh that forms a wall.
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

        /* Add a meshFilter and a meshRenderer to be able to draw the wall */
        wall.AddComponent<MeshFilter>();
        wall.GetComponent<MeshFilter>().mesh = wallMesh;
        wall.AddComponent<MeshRenderer>();
        wall.GetComponent<MeshRenderer>().material = wallMaterial;

        /* Add a meshCollider to make the walls work as walls */
        wall.AddComponent<MeshCollider>();
        wall.GetComponent<MeshCollider>().sharedMesh = wallMesh;
    }
    
    public void CreateCloudsMesh(GameObject clouds, float xScale, float zScale, int cloudCount, float cloudDepth){
    	/*
    	 * Create and assign the mesh used to represent the clouds in the room's top and bottom bounds.
    	 */
    	Mesh cloudMesh = new Mesh();
        Vector3[] vertices = new Vector3[4*cloudCount];
        Vector2[] UV = new Vector2[4*cloudCount];
        int[] triangles = new int[6*cloudCount];
        float currentHeight;
        
        /* Create the cloud mesh one layer at a time */
        for(int i = 0; i < cloudCount; i++){
        	
        	/* Create and assign the vertices for this layer */
        	currentHeight = -cloudDepth * ((float)i / (float)(cloudCount-1));
        	vertices[4*i + 0] = new Vector3(+0.5f*xScale, currentHeight, +0.5f*zScale);
        	vertices[4*i + 1] = new Vector3(-0.5f*xScale, currentHeight, +0.5f*zScale);
        	vertices[4*i + 2] = new Vector3(-0.5f*xScale, currentHeight, -0.5f*zScale);
        	vertices[4*i + 3] = new Vector3(+0.5f*xScale, currentHeight, -0.5f*zScale);
        
        	/* Create and assign the triangles using the previously set vertices */
        	triangles[6*i + 0] = 4*i + 2;
        	triangles[6*i + 1] = 4*i + 1;
        	triangles[6*i + 2] = 4*i + 0;
        	triangles[6*i + 3] = 4*i + 3;
        	triangles[6*i + 4] = 4*i + 2;
        	triangles[6*i + 5] = 4*i + 0;

            /* Set the UVs if this truangle set */
            UV[4*i + 0] = new Vector2(0, 0);
            UV[4*i + 1] = new Vector2(1, 0);
            UV[4*i + 2] = new Vector2(1, 1);
            UV[4*i + 3] = new Vector2(0, 1);
        }
        
        /* Assign the parameters to the mesh */
        cloudMesh.vertices = vertices;
        cloudMesh.triangles = triangles;
        cloudMesh.uv = UV;
        cloudMesh.RecalculateNormals();

        /* Add a meshFilter and meshRenderer to be able to draw the clouds */
        clouds.AddComponent<MeshFilter>();
        clouds.GetComponent<MeshFilter>().mesh = cloudMesh;
        clouds.AddComponent<MeshRenderer>();
        clouds.GetComponent<MeshRenderer>().material = cloudMaterial;
    }
}
