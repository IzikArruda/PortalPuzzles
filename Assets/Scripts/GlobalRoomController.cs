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
    public ConnectedRoom[] attachedRooms;
    public PuzzleRoomEditor[] puzzleRooms;
    public WaitingRoom[] waitingRooms;

    /* --- User customization ----------------------- */
    /* Setup variables */
    public bool RepopulatePuzzleRoomArray = false;
    public bool RepopulateAttachedRoomArray = false;
    public bool RepopulateWaitingRoomArray = false;
    public bool resetNames = false;


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
    }


    private void RenameRooms() {
        /*
         * Rename all the rooms to their expected names
         */

        /* Name the starting room */


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
            attachedRooms = new ConnectedRoom[attachedRoomContainer.transform.childCount];
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
}
