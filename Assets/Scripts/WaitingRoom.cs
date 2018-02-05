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

    /* The previous and upcomming WaitingRooms, if applicable */
    public WaitingRoom previousRoom;
    public WaitingRoom nextRoom;

    /* The window used in this WaitingRoom along with it's stats */
    public Window window;
    public float frameThickness;
    public float frameDepth;
    public float windowHeight;
    public float windowWidth;
    //Where the outside window will be placed. Use the "Window Exit" object in it's Points of Interest container
    public Transform windowExit;

    /* Values set by this room upon it's creation. Used as a reference. */
    private float xDist;
    private float yDist;
    private float zDist;
    private Vector3 roomCenter;
    
    /* The materials and textures used by this room */
    public Material windowFrameMaterial;
    public Material windowGlassMaterial;
    public Texture skySphereTexture;

    /* -------- Built-In Functions ---------------------------------------------------- */

    void Awake() {
        /*
         * Ensure every room is disabled before the Start() functions start running to allow 
         * only the bare minimum required rooms being active once the player finishes loading.
         */

        /* do not disable the rooms if it is in the editor */
        if(!Application.isEditor) {
            entranceRoom.DisablePuzzleRoom();
            exitRoom.DisablePuzzleRoom();
            DisableRoom();
        }
    }

    void Start() {
        /*
         * On start-up, recreate the room's skeleton any puzzle rooms from the AttachedRooms.
         */

        /* Update the walls of the room */
        UpdateRoom();

        /* Place the window in a good position in the room */
        UpdateWindow();
    }

    void OnTriggerEnter(Collider player) {
        /*
         * When the player enters the room's trigger, enable both connected puzzle rooms
         * and their connected rooms
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            entranceRoom.EnablePuzzleRoom();
            exitRoom.EnablePuzzleRoom();
            if(previousRoom != null) { previousRoom.SoftEnable(); }
            if(nextRoom != null) { nextRoom.SoftEnable(); }
        }
    }

    void OnTriggerExit(Collider player) {
        /*
         * When the player leaves the room's trigger, Check which side of the room
         * the player left to determine which puzzle room to disable
         */

        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            Vector3 center = (entranceRoom.exitPointFront.position + exitRoom.exitPointBack.position)/2f;

            /* Player progressed forward */
            if(player.transform.position.z > center.z) {
                entranceRoom.DisablePuzzleRoom();
                exitRoom.EnablePuzzleRoom();
                if(nextRoom != null) { nextRoom.SoftEnable(); }
                if(previousRoom != null) { previousRoom.DisableRoom(); }
            }

            /* Player moved backwards through the puzzles */
            else {
                exitRoom.DisablePuzzleRoom();
                entranceRoom.EnablePuzzleRoom();
                if(previousRoom != null) { previousRoom.SoftEnable(); }
                if(nextRoom != null) { nextRoom.DisableRoom(); }
            }
        }
    }

    void OnTriggerStay(Collider player) {
        /*
         * Whenever the player is inside the waitingRoom, move the window's sky sphere
         * relative to the player's camera to this room's center.
         */
        Vector3 playerCameraPosition;
        Vector3 centerDifference;
         
        /* Ensure the collider entering the trigger is a player */
        if(player.GetComponent<CustomPlayerController>() != null) {
            playerCameraPosition = player.GetComponent<CustomPlayerController>().playerCamera.transform.position;
            centerDifference = playerCameraPosition - roomCenter;
            centerDifference = new Vector3(centerDifference.x, -centerDifference.y, centerDifference.z);
            window.OffsetSkySphere(centerDifference);
        }
    }

    /* -------- Event Functions ---------------------------------------------------- */

    void UpdateRoom() {
        /*
         * Given the position of the attached rooms, re-create this room's bounderies
         */
         
        /* Extract the needed values from the two AttachedRooms */
        float xEntranceDist = entranceRoom.exitWidth;
        float yEntranceDist = entranceRoom.exitHeight;
        float xExitDist = exitRoom.exitWidth;
        float yExitDist = exitRoom.exitHeight;

        /* Re-position the room to the center position between the two attachedRooms */
        roomCenter = (entranceRoom.exitPointFront.position + exitRoom.exitPointBack.position)/2f;
        roomCenter -= new Vector3((xEntranceDist/2f - xExitDist/2f)/2f, 0, 0);

        /* Calculate the sizes of this waitingRoom */
        xDist = Mathf.Abs(entranceRoom.exitPointFront.position.x - exitRoom.exitPointBack.position.x) + xEntranceDist/2f + xExitDist/2f;
        yDist = Mathf.Max(yEntranceDist, yExitDist);
        zDist = Mathf.Abs(entranceRoom.exitPointFront.position.z - exitRoom.exitPointBack.position.z);

        /* Re-create the trigger that is used to determine if the player has entered either AttachedRooms */
        CreateTrigger();
        
        /* Re-create each wall for the room as a default, centered, empty object */
        CreateObjects(ref roomWalls, 8, roomCenter);

        /* Set unique values for each individual wall */
        roomWalls[0].name = "Floor";
        CreatePlane(roomWalls[0], xDist, zDist, 8, floorMaterial, 0, false);
        //Attach a DetectPlayerLegRay script to the floor
        roomWalls[0].AddComponent<DetectPlayerLegRay>();

        roomWalls[1].name = "Left wall";
        roomWalls[1].transform.position += new Vector3(-xDist/2f, yDist/2f, 0);
        CreatePlane(roomWalls[1], yDist, zDist, 8, wallMaterial, 1, true);

        roomWalls[2].name = "Right wall";
        roomWalls[2].transform.position += new Vector3(xDist/2f, yDist/2f, 0);
        CreatePlane(roomWalls[2], yDist, zDist, 8, wallMaterial, 1, false);

        roomWalls[3].name = "Ceiling";
        roomWalls[3].transform.position += new Vector3(0, yDist, 0);
        CreatePlane(roomWalls[3], xDist, zDist, 8, ceilingMaterial, 0, true);

        roomWalls[4].name = "Entrance side wall";
        roomWalls[4].transform.position += new Vector3(xEntranceDist/2f, yDist/2f, -zDist/2f);
        CreatePlane(roomWalls[4], xDist - xEntranceDist, yDist, 8, wallMaterial, 2, true);

        roomWalls[5].name = "Exit side wall";
        roomWalls[5].transform.position += new Vector3(-xExitDist/2f, yDist/2f, zDist/2f);
        CreatePlane(roomWalls[5], xDist - xExitDist, yDist, 8, wallMaterial, 2, false);

        roomWalls[6].name = "Above Entrance wall";
        roomWalls[6].transform.position += new Vector3(-xDist/2f + xEntranceDist/2f, yDist - (yDist - yEntranceDist)/2f, -zDist/2f);
        CreatePlane(roomWalls[6], xEntranceDist, yDist - yEntranceDist, 8, wallMaterial, 2, true);

        roomWalls[7].name = "Above Exit wall";
        roomWalls[7].transform.position += new Vector3(xDist/2f - xExitDist/2f, yDist - (yDist - yExitDist)/2f, zDist/2f);
        CreatePlane(roomWalls[7], xExitDist, yDist - yExitDist, 8, wallMaterial, 2, false);
    }

    void UpdateWindow() {
        /*
         * Update the values of the window and position it in an appropriate spot in the room
         */

        /* Set the size stats of the Window script */
        window.frameThickness = frameThickness;
        window.frameDepth = frameDepth;
        window.windowHeight = windowHeight;
        window.windowWidth = windowWidth;
        
        /* --- Maybe add more options: place the window on the right wall, entrance wall, etc --- */
        /* Place the inside window/entrance portal on the left wall, halfway up the wall */
        window.insidePos = roomCenter + new Vector3(-xDist/2f, yDist/2f - window.windowHeight/2f, 0);
        window.insideRot = new Vector3(0, -90, 0);


        /* Place the outside window/exit portal using the windowExit transform given to this script  */
        window.outsidePos = windowExit.position;
        window.outsideRot = windowExit.eulerAngles;

        /* Set the materials that the window will use */
        window.frameMaterial = windowFrameMaterial;
        window.glassMaterial = windowGlassMaterial;
        window.skySphereTexture = skySphereTexture;

        /* Send a command to update the windows with the new given parameters */
        window.UpdateWindow();
    }

    void CreateTrigger() {
        /*
         * Create the trigger that encompasses both this WaitingRoom and both the connected AttachedRooms
         */
        Vector3 backPoint = entranceRoom.exitPointBack.transform.position;
        Vector3 frontPoint = exitRoom.exitPointFront.transform.position;

        /* Get the proper width of the collider to encompass both AttachedRooms */
        float xFull = Mathf.Abs(frontPoint.x - backPoint.x) + entranceRoom.exitWidth/2f + exitRoom.exitWidth/2f;
        float zFull = Mathf.Abs(frontPoint.z - backPoint.z);
        
        /* Get the Z axis offset of the room center due to inequal exit/entrance z sizes */
        float zDiff = entranceRoom.roomLength - exitRoom.roomLength;

        /* Update the collider with it's new stats */
        RecreateMainTrigger();
        roomTrigger.center = roomCenter + new Vector3(0, yDist/2f, -zDiff);
        //Dont use the full Z distance to prevent the player from hitting it from the puzzleRoom
        roomTrigger.size = new Vector3(xFull, yDist, zFull*0.95f);
    }

    public void DisableRoom() {
        /*
         * Disable the trigger, the objects that make this room and the connected rooms.
         */

        roomTrigger.enabled = false;
        window.gameObject.SetActive(false);
        roomObjectsContainer.gameObject.SetActive(false);
        entranceRoom.DisablePuzzleRoom();
        exitRoom.DisablePuzzleRoom();
        entranceRoom.DisableRoom();
        exitRoom.DisableRoom();
    }

    public void EnableRoom() {
        /*
         * Enable this WaitingRoom, it's AttachedRooms and their corresponding puzzleRooms 
         * and the two potential WaitingRooms that are behind and ahead of this room.
         */
         
        roomTrigger.enabled = true;
        window.gameObject.SetActive(true);
        roomObjectsContainer.gameObject.SetActive(true);
        entranceRoom.EnablePuzzleRoom();
        exitRoom.EnablePuzzleRoom();
        entranceRoom.EnableRoom();
        exitRoom.EnableRoom();

        /* Soft enable the other nearby WaitingRooms (if applicable) */
        if(previousRoom != null) { previousRoom.SoftEnable(); }
        if(nextRoom != null) { nextRoom.SoftEnable(); }
    }

    public void SoftEnable() {
        /*
         * Only enable this waitingRoom and it's AttachedRooms. Do not change any other
         * puzzle rooms and other waiting rooms.
         */

        roomTrigger.enabled = true;
        roomObjectsContainer.gameObject.SetActive(true);
        entranceRoom.EnableRoom();
        exitRoom.EnableRoom();
    }
}
