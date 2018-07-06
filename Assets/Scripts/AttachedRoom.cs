using UnityEngine;
using System.Collections;

/*
 * This script is used to connect puzzle rooms to non-puzzle rooms. Certain stats of this room 
 * need to be tracked so that it can properly connect the two rooms, such as the exit sizes.
 * This room always uses 4 flat planes as it's walls and is created procedurally using the exit points.
 */
[ExecuteInEditMode]
public class AttachedRoom : ConnectedRoom {

    /* The globalRoomController which has links to all the rooms of the game */
    public GlobalRoomController globalRoomController;

    /* The two exit points of the room's two exits. Used to connect rooms. */
    public Transform exitPointFront;
    public Transform exitPointBack;

    /* The reset point of the room. Determines where the player will spawn when they restart using this room */
    public Transform resetPoint;
    
    /* The size of the exit of this room. Used by outside functions and requires user input to set. */
    public float exitWidth;
    public float exitHeight;
    public float roomLength;

    /* The gameObject of the puzzleRoom that this room is attached to */
    public GameObject puzzleRoomParent;

    /* The edges of the room that connect it to a puzzleRoom */
    public GameObject[] wallConnectors;
    public float wallConnectorSize;
    /* Controls what side the room connectors are on */
    [HideInInspector]
    public bool roomSide;
    /* Controls whether the top connector is placed above or bellow. Set in the editor. */
    public bool topConnectorSide;

    /* The material used for the puzzleRoom connectors */
    [HideInInspector]
    public Material wallConnectorMaterial;

    /* If set to true, the room will recreate it's walls on the next frame */
    public bool update;


    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start() {
        /*
         * On startup, update the walls for now
         */

        update = true;
        UpdateWalls();
    }

    public void Update() {
        /*
         * Recreate the walls if it needs to update
         */

        if(update) {
            update = false;
            UpdateWalls();
        }
    }

    void OnTriggerEnter(Collider player) {
        /*
         * When the player enters the room's trigger, change their linked attachedRoom.
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            /* Tell the CustomPlayerController to change their linked attachedRoom */
            player.GetComponent<CustomPlayerController>().ChangeLastRoom(this);

            /* If the player is "falling" backwards through the rooms, send a message to the global room controller */
            if(player.transform.up == new Vector3(0, 0, 1)) {
                globalRoomController.UpdateAllRoomTextures(this);
            }
        }
    }

    public void UpdateAttachedPuzzleRoom(GameObject parent) {
        /*
         * Called when a puzzle room updates itself, this function gives this room a link to
         * the main container for the puzzle room that is connected to it.
         * This will allow this room the capability to completely disable a puzzle room.
         */

        puzzleRoomParent = parent;
    }


    /* -------- Event Functions ---------------------------------------------------- */

    void UpdateWalls() {
        /*
         * Look at the position of the exit points and create the walls for the room
         */
        float depth = 0;

        /* Place the exitPoints using the new roomLength */
        exitPointFront.transform.localPosition = new Vector3(0, 0, roomLength);
        exitPointBack.transform.localPosition = new Vector3(0, 0, -roomLength);

        
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

        /* Re-create the trigger used to determine if the player entered this AttachedRoom */
        RecreateMainTrigger();
        roomTrigger.center = -transform.localPosition + roomCenter + new Vector3(0, exitHeight/2f, 0);
        roomTrigger.size = new Vector3(exitWidth, exitHeight, depth);
        
        /* Re-create each wall for the room */
        CreateObjects(ref roomWalls, 4, roomCenter);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        CreatePlane(roomWalls[0], exitWidth, depth, 8, floorMaterial, 0, false);
        //Attach a DetectPlayerLegRay script to the floor
        roomWalls[0].AddComponent<DetectPlayerLegRay>();
        roomWalls[0].GetComponent<DetectPlayerLegRay>().objectType = 0;

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-exitWidth/2f, exitHeight/2f, 0);
        CreatePlane(roomWalls[1], exitHeight, depth, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position += new Vector3(exitWidth/2f, exitHeight/2f, 0);
        CreatePlane(roomWalls[2], exitHeight, depth, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position += new Vector3(0, exitHeight, 0);
        CreatePlane(roomWalls[3], exitWidth, depth, 8, ceilingMaterial, 0, true);


        /* Update the wall connectors if this room is connected to a puzzleRoom */
        if(puzzleRoomParent != null && puzzleRoomParent.transform.GetChild(0).GetComponent<PuzzleRoomEditor>() != null) {
            UpdateWallConnectors(roomCenter, depth);
        }
    }

    private void UpdateWallConnectors(Vector3 roomCenter, float depth) {
        /*
         * Update the wall connectors that are placed between this attachedRoom and it's connected puzzleRoom
         */
        
        /* Create the wood wall connectors on the end of the room */
        CreateWallConnectors();
        Vector3 centerOffset = Vector3.zero;

        /* Create the cube on the left side of the wall */
        float ratio = 0.75f;
        wallConnectors[0].name = "Left wall connector";
        centerOffset = new Vector3(-exitWidth/2f - ratio*wallConnectorSize/2f, 
                exitHeight/2f + (topConnectorSide ? 1 : -1)*(1 - ratio)*wallConnectorSize/4f,
                (depth/2f + ratio*wallConnectorSize/2f)*(roomSide ? 1 : -1));
        wallConnectors[0].transform.position = roomCenter + centerOffset;
        wallConnectors[0].GetComponent<CubeCreator>().x = wallConnectorSize;
        wallConnectors[0].GetComponent<CubeCreator>().y = exitHeight - (1 - ratio)*wallConnectorSize/2f;
        wallConnectors[0].GetComponent<CubeCreator>().z = wallConnectorSize;
        wallConnectors[0].GetComponent<CubeCreator>().updateCube = true;

        /* Create the cube on the right side of the wall */
        wallConnectors[1].name = "Right wall connector";
        centerOffset = new Vector3(exitWidth/2f + ratio*wallConnectorSize/2f, 
                exitHeight/2f + (topConnectorSide ? 1 : -1)*(1 - ratio)*wallConnectorSize/4f, 
                (depth/2f + ratio*wallConnectorSize/2f)*(roomSide ? 1 : -1));
        wallConnectors[1].transform.position = roomCenter + centerOffset;
        wallConnectors[1].GetComponent<CubeCreator>().x = wallConnectorSize;
        wallConnectors[1].GetComponent<CubeCreator>().y = exitHeight - (1 - ratio)*wallConnectorSize/2f;
        wallConnectors[1].GetComponent<CubeCreator>().z = wallConnectorSize;
        wallConnectors[1].GetComponent<CubeCreator>().updateCube = true;

        /* Create the cube on the top side of the wall */
        wallConnectors[2].name = "Top wall connector";
        centerOffset = new Vector3(0,
                (ratio*wallConnectorSize/2f)*(topConnectorSide ? -1 : 1) + (exitHeight)*(topConnectorSide ? 0 : 1), 
                (depth/2f + ratio*wallConnectorSize/2f)*(roomSide ? 1 : -1));
        wallConnectors[2].transform.position = roomCenter + centerOffset;
        wallConnectors[2].GetComponent<CubeCreator>().x = exitWidth + ((1 - ratio)/2f + ratio)*wallConnectorSize*2;
        wallConnectors[2].GetComponent<CubeCreator>().y = wallConnectorSize;
        wallConnectors[2].GetComponent<CubeCreator>().z = wallConnectorSize;
        wallConnectors[2].GetComponent<CubeCreator>().updateCube = true;

        /* Place a smaller connector on the floor that connects the floor and the puzzleRoom */
        wallConnectors[3].name = "Floor connector";
        centerOffset = new Vector3(0,
                (ratio*0.75f*wallConnectorSize/2f)*(topConnectorSide ? 1 : -1) + (exitHeight)*(topConnectorSide ? 1 : 0),
                (depth/2f + ratio*wallConnectorSize/4f)*(roomSide ? 1 : -1));
        wallConnectors[3].transform.position = roomCenter + centerOffset;
        wallConnectors[3].GetComponent<CubeCreator>().x = exitWidth + ((1 - ratio)/2f + ratio)*wallConnectorSize*2;
        wallConnectors[3].GetComponent<CubeCreator>().y = 0.75f*wallConnectorSize;
        wallConnectors[3].GetComponent<CubeCreator>().z = wallConnectorSize/2f;
        wallConnectors[3].GetComponent<CubeCreator>().updateCube = true;
        wallConnectors[3].AddComponent<BoxCollider>().enabled = false;

    }
    
    public Transform ResetPlayer() {
        /*
         * Return the resetPoint of this room for the player to use as a reset point
         */

        return resetPoint;
    }
    
    public void DisablePuzzleRoom() {
        /*
         * Disable the attached puzzle room
         */
         
        if(puzzleRoomParent != null) {
            
            /* Check if the puzzle room uses portals */
            Transform portalContainer = puzzleRoomParent.transform.FindChild("Portals");
            if(portalContainer != null) {
                /* Disable the puzzleRoom's portal's */
                for(int i = 0; i < portalContainer.childCount; i++) {
                    portalContainer.GetChild(i).GetChild(0).GetComponent<PortalSet>().UpdatePortalState(false);
                }
            }

            /* Check if the puzzle room is actually the starting room */
            else if(puzzleRoomParent.GetComponent<StartingRoom>() != null) {
                /* Disable the startingRoom's portals and terrain */
                puzzleRoomParent.GetComponent<StartingRoom>().window.portalSet.UpdatePortalState(false);
                puzzleRoomParent.GetComponent<StartingRoom>().outsideTerrain.gameObject.SetActive(false);
            }
        }
    }

    public void EnablePuzzleRoom() {
        /*
         * Enable the attached puzzle room
         */
         
        if(puzzleRoomParent != null) {
            
            /* Check if the puzzle room uses portals */
            Transform portalContainer = puzzleRoomParent.transform.FindChild("Portals");
            if(portalContainer != null) {
                /* Enable the puzzleRoom's portal's */
                for(int i = 0; i < portalContainer.childCount; i++) {
                    portalContainer.GetChild(i).GetChild(0).GetComponent<PortalSet>().UpdatePortalState(true);
                }
            }

            /* Check if the puzzle room is actually the starting room */
            else if(puzzleRoomParent.GetComponent<StartingRoom>() != null) {
                /* Enable the startingRoom's portals and terrain */
                puzzleRoomParent.GetComponent<StartingRoom>().window.portalSet.UpdatePortalState(true);
                puzzleRoomParent.GetComponent<StartingRoom>().outsideTerrain.gameObject.SetActive(true);
            }
        }
    }

    public void CreateWallConnectors() {
        /*
         * Make sure the wall connectors are created and have the right components
         */

        /* Make sure the array is properly created */
        if(wallConnectors != null) {
            for(int i = 0; i < wallConnectors.Length; i++) {
                if(wallConnectors[i] != null) {
                    DestroyImmediate(wallConnectors[i]);
                }
            }
            wallConnectors = null;
        }

        if(wallConnectors == null) {
            wallConnectors = new GameObject[4];
        }

        /* Loop through each wall connector */
        for(int i = 0; i < wallConnectors.Length; i++) {

            /* Create the object and it's components if it doesn't yet exist */
            if(wallConnectors[i] == null) {
                wallConnectors[i] = new GameObject();
                wallConnectors[i].transform.parent = roomObjectsContainer;
                wallConnectors[i].AddComponent<CubeCreator>();
                wallConnectors[i].GetComponent<CubeCreator>().mainMaterial = wallConnectorMaterial;
            }

            wallConnectors[i].GetComponent<CubeCreator>().mainMaterial = wallConnectorMaterial;
        }
    }
}