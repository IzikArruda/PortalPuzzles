using UnityEngine;
using System.Collections;

/*
 * The room the player is expected to start in. This script simply builds the walls to the given variables.
 * It also requires a link to it's AttachedRoom that connects it to the next WaitingRoom to know it's sizes.
 */
public class StartingRoom : ConnectedRoom {

    /* The AttachedRoom that leads into this empty room */
    public AttachedRoom exit;

    /* Stairs that connects the room's exit to the floor */
    public StairsCreator stairs;
    public Material stairsStepMaterial;
    public Material stairsOtherMaterial;

    /* How much distance is between the exit and the man wall. Z axis. */
    public float roomDepth;

    /* How much extra width the room has. This does not include the exit's width. X axis. */
    public float extraRoomWidth;

    /* How much extra height the room has. This does not include the exit's height. Y axis. */
    public float extraHeight;

    /* How much distance the floor will be from the base of the room's exit */
    public float roomBellowHeight;


    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start () {
        /*
         * On startup, build the walls of the room
         */

        UpdateWalls();
	}

    void UpdateWalls() {
        /*
         * Build the walls of the room using the linked AttachedRoom as reference
         */
        float roomWidth = extraRoomWidth;
        float upperRoomHeight = extraHeight;
        Vector3 center = Vector3.zero;

        /* Extract desired values from the linked exit */
        if(exit != null) {
            roomWidth += exit.exitWidth;
            upperRoomHeight += exit.exitHeight;
            center = exit.exitPointBack.position + new Vector3(0, 0, -roomDepth/2f);
        }
        else {
            Debug.Log("WARNING: StartingRoom does not have a linked exit");
        }

        /* Get the height of the full room */
        float fullRoomHeight = upperRoomHeight + roomBellowHeight;
        
        /* Re-create each wall for the room */
        CreateObjects(ref roomWalls, 9, center);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        roomWalls[0].transform.position += new Vector3(0, -roomBellowHeight, 0);
        CreatePlane(roomWalls[0], roomWidth, roomDepth, 8, floorMaterial, 0, false);

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-roomWidth/2f, upperRoomHeight/2f - (fullRoomHeight - upperRoomHeight)/2f, 0);
        CreatePlane(roomWalls[1], fullRoomHeight, roomDepth, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position += new Vector3(roomWidth/2f, upperRoomHeight/2f - (fullRoomHeight - upperRoomHeight)/2f, 0);
        CreatePlane(roomWalls[2], fullRoomHeight, roomDepth, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position += new Vector3(0, upperRoomHeight, 0);
        CreatePlane(roomWalls[3], roomWidth, roomDepth, 8, ceilingMaterial, 0, true);

        roomWalls[4].name = "Back wall";
        roomWalls[4].transform.position += new Vector3(0, upperRoomHeight/2f - (fullRoomHeight - upperRoomHeight)/2f, -roomDepth/2f);
        CreatePlane(roomWalls[4], roomWidth, fullRoomHeight, 8, wallMaterial, 2, true);

        roomWalls[5].name = "Exit top wall";
        roomWalls[5].transform.position += new Vector3(0, upperRoomHeight - extraHeight/2f, roomDepth/2f);
        CreatePlane(roomWalls[5], roomWidth, extraHeight, 8, wallMaterial, 2, false);

        roomWalls[6].name = "Exit left wall";
        roomWalls[6].transform.position += new Vector3(-roomWidth/2f + extraRoomWidth/4f, (upperRoomHeight - extraHeight)/2f, roomDepth/2f);
        CreatePlane(roomWalls[6], extraRoomWidth/2f, (upperRoomHeight - extraHeight), 8, wallMaterial, 2, false);

        roomWalls[7].name = "Exit right wall";
        roomWalls[7].transform.position += new Vector3(roomWidth/2f - extraRoomWidth/4f, (upperRoomHeight - extraHeight)/2f, roomDepth/2f);
        CreatePlane(roomWalls[7], extraRoomWidth/2f, (upperRoomHeight - extraHeight), 8, wallMaterial, 2, false);
        
        roomWalls[8].name = "Exit Bottom wall";
        roomWalls[8].transform.position += new Vector3(0, -roomBellowHeight/2f, roomDepth/2f);
        CreatePlane(roomWalls[8], roomWidth, roomBellowHeight, 8, wallMaterial, 2, false);


        /* Position the key points of the stairs for this room */
        stairs.endPoint.position = center + new Vector3(0, 0, roomDepth/2f);
        stairs.startPoint.position = center + new Vector3(0, 0, roomDepth/2f) + new Vector3(0, -roomBellowHeight, -roomBellowHeight);
        stairs.stairsMaterial = stairsStepMaterial;
        stairs.otherMaterial = stairsOtherMaterial;
        stairs.stairsWidth = exit.exitWidth;
        stairs.upVector = new Vector3(0, 1, 0);
        stairs.updateStairs = true;
        stairs.resetAngle = true;
    }
}
