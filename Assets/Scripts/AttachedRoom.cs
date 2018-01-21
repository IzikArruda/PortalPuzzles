using UnityEngine;
using System.Collections;

/*
 * This script is used to connect puzzle rooms to non-puzzle rooms. Certain stats of this room 
 * need to be tracked so that it can properly connect the two rooms, such as the exit sizes.
 * This room always uses 4 flat planes as it's walls and is created procedurally using the exit points.
 */
[ExecuteInEditMode]
public class AttachedRoom : ConnectedRoom {

    /* The two exit points of the room's two exits. Used to connect rooms. */
    public Transform exitPointFront;
    public Transform exitPointBack;

    /* The reset point of the room. Determines where the player will spawn when they restart using this room */
    public Transform resetPoint;
    
    /* The size of the exit of this room. Used by outside functions and requires user input to set. */
    public float exitWidth;
    public float exitHeight;
    

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
        
        /* Re-create the trigger used to determine if the player entered this AttachedRoom */
        if(roomTrigger != null) { DestroyImmediate(roomTrigger); }
        roomTrigger = gameObject.AddComponent<BoxCollider>();
        roomTrigger.isTrigger = true;
        roomTrigger.center = -transform.localPosition + roomCenter + new Vector3(0, exitHeight/2f, 0);
        roomTrigger.size = new Vector3(exitWidth, exitHeight, depth);
        
        /* Re-create each wall for the room */
        CreateObjects(ref roomWalls, 4, roomCenter);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        CreatePlane(roomWalls[0], exitWidth, depth, 8, floorMaterial, 0, false);

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-exitWidth/2f, exitHeight/2f, 0);
        CreatePlane(roomWalls[1], exitHeight, depth, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position = roomCenter + new Vector3(exitWidth/2f, exitHeight/2f, 0);
        CreatePlane(roomWalls[2], exitHeight, depth, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position = roomCenter + new Vector3(0, exitHeight, 0);
        CreatePlane(roomWalls[3], exitWidth, depth, 8, ceilingMaterial, 0, true);
    }
    
    public Transform ResetPlayer() {
        /*
         * Return the resetPoint of this room for the player to use as a reset point
         */

        return resetPoint;
    }
    
}