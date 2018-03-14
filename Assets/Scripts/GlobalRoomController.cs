using UnityEngine;
using System.Collections;

/*
 * Control how the puzzle rooms, attached rooms and waiting rooms are placed.
 * 
 * Here are requirements that must be met by the hierarchy:
 * - This script must be fed values for a startingRoom and the containers.
 * - Each room container (puzzle, waiting and attached) must only have objects with the corresponding script attached to them
 * - The order of the rooms in their container is important as it represents the order of appearence of the rooms in the game
 */
[ExecuteInEditMode]
public class GlobalRoomController : MonoBehaviour {

    /* --- Required Links ----------------------- */
    /* Names for the rooms */
    public string startingRoomNamePrefix;
    public string puzzleRoomNamePrefix;

    /* Containers of the rooms */
    public StartingRoom startingRoom;
    public GameObject puzzleRoomContainer;
    public GameObject attachedRoomContainer;
    public GameObject waitingRoomContainer;


    /* --- Script controlled values ----------------------- */
    /* Arrays of the rooms */
    public AttachedRoom[] attachedRooms;
    public PuzzleRoomEditor[] puzzleRooms;
    public WaitingRoom[] waitingRooms;


    /* --- User customization ----------------------- */
    /* Setup variables */
    public bool RepopulatePuzzleRoomArray = false;
    public bool RepopulateAttachedRoomArray = false;
    public bool RepopulateWaitingRoomArray = false;
    public bool resetNames = false;
    public bool relink = false;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Start () {
        /*
         * Set up values and check if the requires values exist
         */

	    if(startingRoom == null || puzzleRoomContainer == null || attachedRoomContainer == null || waitingRoomContainer == null) {
            Debug.Log("WARNING: GlobalRoomController is missing a required linked object");
        }
	}
	
	void Update () {

        RepopulateArrays();

        if(resetNames) {
            resetNames = false;
            RenameRooms();
        }

        if(relink) {
            relink = false;
            RelinkRooms();
        }
    }

    
    /* ----------- Event Functions ------------------------------------------------------------- */

    private void RenameRooms() {
        /*
         * Rename all the rooms to their expected names
         */

        /* Name the starting room */
        startingRoom.name = startingRoomNamePrefix;

        /* Name the puzzle rooms */
        for(int i = 0; i < puzzleRooms.Length; i++) {
            puzzleRooms[i].transform.parent.name = puzzleRoomNamePrefix + " " + (i+1);
        }

        /* Name the attached rooms */
        attachedRooms[0].name = startingRoomNamePrefix + " Exit";
        int currentPuzzle = 1;
        for(int i = 1; i < attachedRooms.Length; i++) {
            attachedRooms[i].name = puzzleRoomNamePrefix + " " + currentPuzzle + " Entrance";
            attachedRooms[++i].name = puzzleRoomNamePrefix + " " + currentPuzzle + " Exit";
            currentPuzzle++;
        }

        /* Name the waiting rooms */
        waitingRooms[0].name = startingRoomNamePrefix + " -> " + puzzleRoomNamePrefix + " 1";
        for(int i = 1; i < waitingRooms.Length; i++) {
            waitingRooms[i].name = puzzleRoomNamePrefix + " " + i + " -> " + puzzleRoomNamePrefix + " " + (i+1);
        }
    }

    private void RepopulateArrays() {
        /*
         * If the proper boolean is set to true, an array will repopulate itself by searching it's related
         * container and relink itself to all it's desired rooms (puzzleRoom, WaitingRoom, etc).
         * 
         * Note the starting room cannot be repopulated as there is only one starting room.
         */

        /* Repopulate the array of puzzle rooms */
        if(RepopulatePuzzleRoomArray) {
            RepopulatePuzzleRoomArray = false;

            /* Create and repopulate the array */
            puzzleRooms = new PuzzleRoomEditor[puzzleRoomContainer.transform.childCount];
            for(int i = 0; i < puzzleRoomContainer.transform.childCount; i++) {
                puzzleRooms[i] = puzzleRoomContainer.transform.GetChild(i).GetChild(0).GetComponent<PuzzleRoomEditor>();
            }
        }

        /* Repopulate the array of attachedRooms */
        if(RepopulateAttachedRoomArray) {
            RepopulateAttachedRoomArray = false;

            /* Create and repopulate the array */
            attachedRooms = new AttachedRoom[attachedRoomContainer.transform.childCount];
            for(int i = 0; i < attachedRoomContainer.transform.childCount; i++) {
                attachedRooms[i] = attachedRoomContainer.transform.GetChild(i).GetComponent<AttachedRoom>();
            }
        }

        /* Repopulate the array of waitingRooms */
        if(RepopulateWaitingRoomArray) {
            RepopulateWaitingRoomArray = false;

            /* Create and repopulate the array */
            waitingRooms = new WaitingRoom[waitingRoomContainer.transform.childCount];
            for(int i = 0; i < waitingRoomContainer.transform.childCount; i++) {
                waitingRooms[i] = waitingRoomContainer.transform.GetChild(i).GetComponent<WaitingRoom>();
            }
        }
    }

    private void RelinkRooms() {
        /*
         * Set the script links that all the rooms share between eachother
         */

        /* Link the startingRoom and it's attached room */
        startingRoom.exit = attachedRooms[0];
        attachedRooms[0].puzzleRoomParent = startingRoom.gameObject;
        
        /* Link the puzzleRooms and their connected AttachedRooms */
        for(int i = 0; i < puzzleRooms.Length; i++) {
            /* Linked the attachedRooms to each puzzleRoom */
            puzzleRooms[i].entrance = attachedRooms[i*2 + 1];
            puzzleRooms[i].entrance = attachedRooms[i*2 + 2];

            /* Link the puzzleRooms to the attachedRooms */
            attachedRooms[i*2 + 1].puzzleRoomParent = puzzleRooms[i].transform.parent.gameObject;
            attachedRooms[i*2 + 2].puzzleRoomParent = puzzleRooms[i].transform.parent.gameObject;
        }

        /* Link the waitingRoom's references to their two attachedRooms */
        for(int i = 0; i < waitingRooms.Length; i++) {
            waitingRooms[i].entranceRoom = attachedRooms[i*2];
            waitingRooms[i].exitRoom = attachedRooms[i*2 + 1];
        }

        /* Link the waitingRooms to their adjacent waitingRoom */
        for(int i = 1; i < waitingRooms.Length - 1; i++) {
            waitingRooms[i].previousRoom = waitingRooms[i-1];
            waitingRooms[i].nextRoom = waitingRooms[i+1];
        }
        //Link the edge cases
        if(waitingRooms.Length > 1) {
            waitingRooms[0].nextRoom = waitingRooms[1];
            waitingRooms[waitingRooms.Length-1].previousRoom = waitingRooms[waitingRooms.Length-2];
        }
    }
}
