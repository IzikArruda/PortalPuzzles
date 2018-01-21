using UnityEngine;
using System.Collections;

/* 
 * A WaitingRoom is a ConnectedRoom that connects two AttachedRooms. It is not a puzzle room 
 * and serves to put more distance between each puzzle room. A waiting room
 * has the shape of a Z tetromino to ensure the player will not see more than 2 puzzle rooms at once.
 */
[ExecuteInEditMode]
public class WaitingRoom : ConnectedRoom {

    /* The two AttachedRooms that will be connected  */
    public AttachedRoom entranceRoom;
    public AttachedRoom exitRoom;
    

    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start () {
        /*
         * On start-up, recreate the room's skeleton
         */

        UpdateRoom();
	}

    void OnTriggerEnter(Collider player) {
        /*
         * When the player enters the room's trigger, do something
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            Debug.Log("test");
        }
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
        float fullLength = Mathf.Abs(entranceRoom.exitPointBack.position.z - exitRoom.exitPointFront.position.z);

        /* Re-position the room to the center position between the two attachedRooms */
        Vector3 center = (entranceRoom.exitPointFront.position + exitRoom.exitPointBack.position)/2f;
        center += new Vector3(Mathf.Abs(entranceWidth/2f - exitWidth/2f)/2f, 0, 0);

        /* Calculate the sizes of this waitingRoom */
        float width = widthDifference + entranceWidth/2f + exitWidth/2f;
        float length = lengthDifference;
        float height = Mathf.Max(entranceHeight, exitHeight);

        /* Re-create the trigger that is used to determine if the player has entered either AttachedRooms */
        CreateTrigger(height);

        /* Re-create each wall for the room */
        CreateObjects(ref roomWalls, 8, center);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        CreatePlane(roomWalls[0], width, lengthDifference, 8, floorMaterial, 0, false);

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-width/2f, height/2f, 0);
        CreatePlane(roomWalls[1], height, length, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position += new Vector3(width/2f, height/2f, 0);
        CreatePlane(roomWalls[2], height, length, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position += new Vector3(0, height, 0);
        CreatePlane(roomWalls[3], width, lengthDifference, 8, ceilingMaterial, 0, true);

        roomWalls[4].name = "Entrance side wall";
        roomWalls[4].transform.position += new Vector3(entranceWidth/2f, height/2f, -length/2f);
        CreatePlane(roomWalls[4], width - entranceWidth, height, 8, wallMaterial, 2, true);

        roomWalls[5].name = "Exit side wall";
        roomWalls[5].transform.position += new Vector3(-exitWidth/2f, height/2f, length/2f);
        CreatePlane(roomWalls[5], width - exitWidth, height, 8, wallMaterial, 2, false);

        roomWalls[6].name = "Above Entrance wall";
        roomWalls[6].transform.position += new Vector3(-width/2f + entranceWidth/2f, height - (height - entranceHeight)/2f, -length/2f);
        CreatePlane(roomWalls[6], entranceWidth, height - entranceHeight, 8, wallMaterial, 2, true);

        roomWalls[7].name = "Above Exit wall";
        roomWalls[7].transform.position += new Vector3(width/2f - exitWidth/2f, height - (height - exitHeight)/2f, length/2f);
        CreatePlane(roomWalls[7], exitWidth, height - exitHeight, 8, wallMaterial, 2, false);
    }

    void CreateTrigger(float height) {
        /*
         * Create the trigger that encompasses both this WaitingRoom and both the connected AttachedRooms
         */
        Vector3 back = entranceRoom.exitPointBack.transform.position;
        Vector3 front = exitRoom.exitPointFront.transform.position;
        Vector3 center = (front + back)/2f;

        /* Get the proper width of the collider */
        float properWidth = Mathf.Abs(back.x - front.x) + entranceRoom.exitWidth/2f + exitRoom.exitWidth/2f;
        float widthDiff = entranceRoom.exitWidth - exitRoom.exitWidth;

        /* Get the proper length. Make it slightly shorter so the player hits it a few steps into the room */
        float properLength = Mathf.Abs(back.z - front.z)*0.9f;

        if(roomTrigger != null) { DestroyImmediate(roomTrigger); }
        roomTrigger = gameObject.AddComponent<BoxCollider>();
        roomTrigger.isTrigger = true;
        roomTrigger.center = center + new Vector3(-widthDiff/4f, height/2f, 0);
        roomTrigger.size = new Vector3(properWidth, height, properLength);
    }

}
