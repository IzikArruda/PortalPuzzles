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
    public bool repopulateArrays = false;
    public bool resetNames = false;
    public bool relink = false;
    
    /* PuzzleRoom */
    public bool puzzleRoomUpdate = false;
    public int puzzleRoomNumber;
    public Vector3 puzzleRoomDistance;

    /* WaitingRoom */
    public bool waitingRoomUpdate = false;
    public int waitingRoomIndex;
    public Vector3 waitingRoomDistance;











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

        /* Repopulate the arrays that track the rooms */
        if(repopulateArrays) {
            repopulateArrays = false;
            RepopulateArrays();
        }

        /* Reset the names of the rooms using user given defaults */
        if(resetNames) {
            resetNames = false;
            RenameRooms();
        }

        /* Relink the references that each room requires to eachother */
        if(relink) {
            relink = false;
            RelinkRooms();
        }

        /* Reposition the puzzleRooms */
        if(puzzleRoomUpdate) {
            puzzleRoomUpdate = false;
            RepositionPuzzleRoomRequest();
        }

        /* Reposition the waitingRoom */
        if(waitingRoomUpdate) {
            waitingRoomUpdate = false;
            RepositionWaitingRoomRequest();
        }
    }


    /* ----------- Positonal Updating Functions ------------------------------------------------------------- */
    
    private void RepositionPuzzleRoomRequest() {
        /*
         * Request to move a puzzle room. The room and the distance is controlled by a user set value in the editor.
         */
        bool validValues = true;

        /* Check that the user has given a valid puzzleRoom number (NOT index) */
        if(puzzleRoomNumber < 1 || puzzleRoomNumber > puzzleRooms.Length) {
            validValues = false;
            Debug.Log("Warning: Given puzzleRoom number is not a valid puzzle room");
        }

        /* If the request is valid, commit to the distance change */
        if(validValues) {
            RepositionPuzzleRoom(puzzleRoomNumber - 1, puzzleRoomDistance);
            puzzleRoomDistance = Vector3.zero;
        }
    }

    private void RepositionPuzzleRoom(int index, Vector3 distance) {
        /*
         * Move the puzzleRoom defined by the given index by the given distance amount.
         */
        Vector3 distanceXZ = new Vector3(distance.x, 0, distance.z);
        Vector3 distanceY = new Vector3(0, distance.y, 0);

        /* Move the puzzleRoom itself by the given X and Z distances */
        puzzleRooms[index].transform.parent.transform.position += distance;
        
        /* Any height change is passed down to the entrance/exit and their linked waitingRoom */
        RepositionWaitingRoom(index, distanceY);
        RepositionWaitingRoom(index + 1, distanceY);

        /* Force the room to update */
        puzzleRooms[index].updateWalls = true;
        //NOTE: EACH ROOM SHOULD HAVE A WAY TO FORCE ITSELF TO BE UPDATED SO THAT THIS FUNCTION CAN CALL IT
    }

    private void RepositionWaitingRoomRequest() {
        /*
         * Request to move a waitingRoom and it's two AttachedRooms. 
         */
        bool validValues = true;

        /* Checkl that the user has given a valid waitingRoom index */
        if(waitingRoomIndex < 0 || waitingRoomIndex >= waitingRooms.Length) {
            validValues = false;
            Debug.Log("Warning: Given waitingRoom index is not a valid index");
        }

        /* If the request is valid, move the waitingRoom and it's parts */
        if(validValues) {
            //So far the waitingRoom can only be moved in the Y and X directions
            Vector3 distance = new Vector3(waitingRoomDistance.x, waitingRoomDistance.y, 0);
            RepositionWaitingRoom(waitingRoomIndex, distance);
            waitingRoomDistance = Vector3.zero;
        }
    }

    private void RepositionWaitingRoom(int index, Vector3 distance) {
        /*
         * Reposition the given waiting room along with it's two attachedRooms. 
         * The act of moving a waitingRoom is simply by moving the two attachedRooms.
         */

        /* Move both the attachedRooms */
        RepositionAttachedRoom(waitingRooms[index].entranceRoom, false, index-1, distance);
        RepositionAttachedRoom(waitingRooms[index].exitRoom, true, index, distance);

        /* Update the waitingRoom after the other rooms are updated */
        waitingRooms[index].UpdateRoom();
    }


    private void RepositionAttachedRoom(AttachedRoom room, bool entrance, int puzzleRoomIndex, Vector3 distance) {
        /*
         * Given an attachedRoom, reposition it in the world. Moving the room requires 
         * looking at what room it is linked to first. Determine what room it
         * is linked to by looking at the name.
         */

        /* Linked to a puzzleRoom */
        if(room.puzzleRoomParent.gameObject.name == "Puzzle " + (puzzleRoomIndex+1)) {
            /* Get the linked puzzleRoom's entrance/exit point */
            Transform transToMove;
            if(entrance) {
                transToMove = puzzleRooms[puzzleRoomIndex].puzzleRoomEntrancePoint;
            }else {
                transToMove = puzzleRooms[puzzleRoomIndex].puzzleRoomExitPoint;
            }

            /* Move the entrance/exit point */
            transToMove.position += distance;

            /* Update the puzzleRoom's walls */
            puzzleRooms[puzzleRoomIndex].updateWalls = true;
        }

        /* Linked to a startingRoom */
        else if(room.puzzleRoomParent.GetComponent<StartingRoom>() != null) {
            /* Moving the startingRoom simply requires moving it's attachedRoom */
            room.transform.position += distance;
        }

        /* Unknown room link */
        else {
            Debug.Log("WARNING: attachedRoom is not linked to a known room");
            Debug.Log("Puzzle " + (puzzleRoomIndex+1));
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

        /* Repopulate the array of puzzleRooms */
        puzzleRooms = new PuzzleRoomEditor[puzzleRoomContainer.transform.childCount];
        for(int i = 0; i < puzzleRoomContainer.transform.childCount; i++) {
            puzzleRooms[i] = puzzleRoomContainer.transform.GetChild(i).GetChild(0).GetComponent<PuzzleRoomEditor>();
        }

        /* Repopulate the array of attachedRooms */
        attachedRooms = new AttachedRoom[attachedRoomContainer.transform.childCount];
        for(int i = 0; i < attachedRoomContainer.transform.childCount; i++) {
            attachedRooms[i] = attachedRoomContainer.transform.GetChild(i).GetComponent<AttachedRoom>();
        }

        /* Repopulate the array of waitingRooms */
        waitingRooms = new WaitingRoom[waitingRoomContainer.transform.childCount];
        for(int i = 0; i < waitingRoomContainer.transform.childCount; i++) {
            waitingRooms[i] = waitingRoomContainer.transform.GetChild(i).GetComponent<WaitingRoom>();
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
            puzzleRooms[i].exit = attachedRooms[i*2 + 2];

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

    private void RepositionRooms() {
        /*
         * Reposition the rooms in the game. Certain repositioning requires a group of rooms to move
         */

        //To move the puzzleRoom's entrance/exit: move their linked entrance and exit gameobject.
        //This will move the walls of the room, the attached room and then the waiting room(that one requires a run)

        //Note: puzzle room's origin is the same origin as the entrance. IE, assuming the entrance hieght is 0,
        //the entrance transform will be (0, 0, 0)
        Vector3 repositionPuzzleRoom = new Vector3(0, 0, 0);
        int puzzleRoomIndexToMove = 0;

        /* Any change in height (Y), will be pushed down to both entrance and exit transforms */
        Vector3 puzzleRoomPositionChange = new Vector3(repositionPuzzleRoom.x, 0, repositionPuzzleRoom.z);
        float puzzleRoomHeightChange = repositionPuzzleRoom.y;








        //When moving a waitingRoom, make sure to move the linked attachedRooms too, which in effect will move the puzzleRoom's exit/entrance
        //Note that moving the fisrt waitingRoom will need to move the attachedRoom instead of the attachedRoom's puzzleRoom.
        //
        /*
         * 
         * 
         * NOTE: when moving the StartingRoom, we must move the attachedRoom instead as the startingRoom will not auto-move it's 
         * attachedRoom and instead the startginRoom is moved along with the attachedRoom
         * 
         * 
         * 
         * 
         */






        //StartingRoom will move with it's linked exit.
    }
}
