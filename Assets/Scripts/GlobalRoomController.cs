using UnityEngine;
using System.Collections;

/*
 * Control how the puzzle rooms, attached rooms and waiting rooms are placed.
 * 
 * Here are requirements that must be met by the hierarchy:
 * - This script must be fed values for a startingRoom and the containers.
 * - Each room container (puzzle, waiting and attached) must only have objects with the corresponding script attached to them
 * - The order of the rooms in their container is important as it represents the order of appearence of the rooms in the game
 * - Each puzzleRoom requires two attachedRooms in entrance and exit, even it's the last puzzleRoom in the array.
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
    public bool puzzleRoomMultipleUpdate = false;
    public int puzzleRoomNumber;
    public Vector3 puzzleRoomDistance;

    /* WaitingRoom */
    public bool waitingRoomUpdate = false;
    public int waitingRoomIndex;
    public Vector3 waitingRoomDistance;

    /* Room creation */
    public int roomIndexToCreate = -1;
    public bool AddRoom;

    /* Room deletion */
    public int roomIndexToDelete = -1;
    public bool deleteRoom;
    


    /* ----------- Built-in Functions ------------------------------------------------------------- */
    
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
        /* Reposition multiple puzzleRooms */
        if(puzzleRoomMultipleUpdate) {
            puzzleRoomMultipleUpdate = false;
            RepositionMultipleRoomsRequest();
        }

        /* Reposition the waitingRoom */
        if(waitingRoomUpdate) {
            waitingRoomUpdate = false;
            RepositionWaitingRoomRequest();
        }

        /* Create a new room */
        if(AddRoom) {
            AddRoom = false;
            CreateRoomRequest();
        }

        /* Delete a room */
        if(deleteRoom) {
            deleteRoom = false;
            DeleteRoomRequest();
        }
    }


    /* ----------- Positonal Updating Functions ------------------------------------------------------------- */
    
    private void RepositionMultipleRooms(int index, Vector3 distance) {
        /*
         * When wanting to reposition a puzzleRoom, reposition all upcomming rooms to reflect the change.
         */

        /* Move each puzzleRoom at the index and above */
        for(int i = index; i < puzzleRooms.Length; i++) {
            RepositionPuzzleRoom(i, distance);
        }
    }

    private void RepositionMultipleRoomsRequest() {
        /*
         * Request to move a puzzleRoom along with any other puzzleRooms ahead of it
         */
        bool validValues = true;

        /* Check that the user has given a valid puzzleRoom number (NOT index) */
        if(puzzleRoomNumber < 1 || puzzleRoomNumber > puzzleRooms.Length) {
            validValues = false;
            Debug.Log("Warning: Given puzzleRoom number is not a valid puzzle room");
        }

        /* If the request is valid, commit to the distance change */
        if(validValues) {
            RepositionMultipleRooms(puzzleRoomNumber - 1, puzzleRoomDistance);
            puzzleRoomDistance = Vector3.zero;
        }
    }

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
        puzzleRooms[index].transform.parent.transform.position += distanceXZ;

        /* Any height change is passed down to the entrance/exit and their linked waitingRoom */
        if(distance.y != 0) {
            RepositionWaitingRoom(index, distanceY);
            if(index < waitingRooms.Length - 1) {
                RepositionWaitingRoom(index + 1, distanceY);
            }
        }

        /* Recreate the puzzleRoom */
        puzzleRooms[index].updateWalls = true;
        puzzleRooms[index].Update();

        /* If there was any movement along the X or Z axis, also update the two adjacent waitingRooms */
        if(distanceXZ.magnitude != 0) {
            waitingRooms[index].Start();
            if(index + 1 < waitingRooms.Length) {
                waitingRooms[index + 1].Start();
            }
        }
    }

    private void RepositionWaitingRoomRequest() {
        /*
         * Request to move a waitingRoom and it's two AttachedRooms. 
         */
        bool validValues = true;

        /* Check that the user has given a valid waitingRoom index */
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

        /* Recreate the waitingRoom */
        waitingRooms[index].Start();
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
            puzzleRooms[puzzleRoomIndex].Update();
        }

        /* Linked to a startingRoom */
        else if(room.puzzleRoomParent.GetComponent<StartingRoom>() != null) {
            /* Moving the startingRoom simply requires moving it's attachedRoom */
            room.transform.position += distance;

            /* Update the startingRoom */
            //startingRoom.Start();
        }

        /* Unknown room link */
        else {
            Debug.Log("WARNING: attachedRoom is not linked to a known room");
            Debug.Log("Puzzle " + (puzzleRoomIndex+1));
        }
    }


    /* ----------- Event Functions ------------------------------------------------------------- */
    
    private void DeleteRoomRequest() {
        /*
         * Request to delete a puzzle room, a waiting room and two attachedRooms.
         */
        bool validValues = true;

        /* Check if the index of the room that will be delete exists */
        if(roomIndexToDelete < 0 || roomIndexToDelete > puzzleRooms.Length-1) {
            validValues = false;
            Debug.Log("Warning: The selected room index to delete does not exist");
        }

        /* Do not delete the last room */
        else if(puzzleRooms.Length < 2) {
            validValues = false;
            Debug.Log("Warning: Cannot delete the final puzzleRoom");
        }

        /* If the request is valid, delete a set of rooms */
        if(validValues) {
            DeleteRoom(roomIndexToDelete);
            roomIndexToDelete = -1;
        }
    }

    private void DeleteRoom(int index) {
        /*
         * Delete the puzzleRoom along with it's waitingRoom and it's attachedRoom.
         */

        /* Move back the puzzleRooms ahead to fill in the empty space created by the missing room */
        float removedRoomLength = puzzleRooms[index].exit.exitPointFront.position.z -
                waitingRooms[index].entranceRoom.exitPointFront.transform.position.z;
        float removedRoomWidth = puzzleRooms[index].puzzleRoomExitPoint.position.x -
                puzzleRooms[index].puzzleRoomEntrancePoint.position.x +
                waitingRooms[index].GetRoomWidth();
        RepositionMultipleRooms(index + 1, new Vector3(-removedRoomWidth, 0, -removedRoomLength));

        /* Delete the two attachedRooms connected to the puzzleRoom */
        GameObject.DestroyImmediate(puzzleRooms[index].entrance.gameObject);
        GameObject.DestroyImmediate(puzzleRooms[index].exit.gameObject);
        
        /* Delete the puzzleRoom at the given index */
        GameObject.DestroyImmediate(puzzleRooms[index].transform.parent.gameObject);

        /* Delete the waitingRoom at the given index */
        GameObject.DestroyImmediate(waitingRooms[index].gameObject);
        
        /* Repopulate, rename and relink the rooms of the game */
        RepopulateArrays();
        RenameRooms();
        RelinkRooms();

        /* If a center room is deleted, re-create a waitingRoom to reconnect the rooms */
        if(index < waitingRooms.Length) {
            waitingRooms[index].Start();
        }
    }

    private void CreateRoomRequest() {
        /*
         * Request to create a new puzzle room along with it's waitingRoom and two attachedRooms
         */
        bool validValues = true;

        /* check if the given index points to an actual room */
        if(roomIndexToCreate < 0 || roomIndexToCreate > puzzleRooms.Length-1) {
            validValues = false;
            Debug.Log("Warning: The selected room index to create does not exist");
        }

        /* If the request is valid, delete a set of rooms */
        if(validValues) {
            CreateRoom(roomIndexToCreate);
            roomIndexToCreate = -1;
        }
    }

    private void CreateRoom(int index) {
        /*
         * Create a new puzzle room and it's required waiting and attached rooms
         */
        float waitingRoomSetWidth = 10f;
        float waitingRoomLength = 6f;
        PuzzleRoomEditor puzzleRoom = puzzleRooms[index];
        WaitingRoom waitingRoom = waitingRooms[index];
        
        /* Duplicate and position the puzzleRoom */
        GameObject newPuzzleRoomObject = GameObject.Instantiate(puzzleRoom.transform.parent.gameObject);
        PuzzleRoomEditor newPuzzleRoom = newPuzzleRoomObject.transform.GetChild(0).GetComponent<PuzzleRoomEditor>();
        float previousPuzzleSize = puzzleRoom.exit.exitPointFront.position.z - puzzleRoom.entrance.exitPointBack.position.z;
        newPuzzleRoomObject.transform.position += new Vector3(waitingRoomSetWidth, 0, previousPuzzleSize + waitingRoomLength);
        
        /* Duplicate two new attachedRooms from the exit of the previously duplicated puzzleRoom */
        AttachedRoom entrance, exit;
        entrance = GameObject.Instantiate(puzzleRoom.entrance.gameObject).GetComponent<AttachedRoom>();
        exit = GameObject.Instantiate(puzzleRoom.exit.gameObject).GetComponent<AttachedRoom>();
        /* Update the attachedRooms */
        exit.update = true;
        exit.Update();
        entrance.update = true;
        entrance.Update();
        
        /* Move any rooms ahead in the array to make room for the new room */
        float newPuzzleRoomLength = newPuzzleRoom.exit.exitPointFront.position.z - newPuzzleRoom.entrance.exitPointBack.position.z;
        RepositionMultipleRooms(index + 1, new Vector3(waitingRoomSetWidth, 0, newPuzzleRoomLength + waitingRoomLength));
        
        /* Duplicate a waitingRoom */
        WaitingRoom newWaitingRoom = GameObject.Instantiate(waitingRoom.gameObject).GetComponent<WaitingRoom>();
        
        /* Reorder the rooms in the heirarchy to reflect the position they are in the room order */
        newPuzzleRoomObject.transform.parent = puzzleRoomContainer.transform;
        newPuzzleRoomObject.transform.SetSiblingIndex(index + 1);
        newWaitingRoom.transform.SetSiblingIndex(index + 1);
        newWaitingRoom.transform.parent = waitingRoomContainer.transform;
        entrance.transform.parent = attachedRoomContainer.transform;
        exit.transform.parent = attachedRoomContainer.transform;
        //The index used for the entrance/exit's child position is NOT index+1. This is because of how exit/entrance is accesed once it is duplicated.
        entrance.transform.SetSiblingIndex((index + 1)*2 + 1);
        exit.transform.SetSiblingIndex((index + 1)*2 + 2);

        /* Update the puzzleRooms to reflect their new positions */
        newPuzzleRoom.updateWalls = true;
        newPuzzleRoom.Update();
        puzzleRoom.updateWalls = true;
        puzzleRoom.Update();

        /* Reset the arrays, names and links to all the rooms */
        RepopulateArrays();
        RenameRooms();
        RelinkRooms();

        /* Recreate each waitingRoom as they require recreating after this for some reason */
        for(int i = 0; i < waitingRooms.Length; i++) {
            waitingRooms[i].Start();
        }
    }

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
    
    public void UpdateAllRoomTextures(AttachedRoom calledRoom) {
        /*
         * Run when the player enters the first attachedRoom while "falling" backwards through the rooms
         */

        /* Get the waitingRoom linked to the given AttachedRoom */
        for(int i = 0; i < waitingRooms.Length; i++) {
            if(waitingRooms[i].entranceRoom == calledRoom || waitingRooms[i].exitRoom == calledRoom) {
                //////Debug.Log("entered waitingRoom: " + i);

                /* Send a command to each previous waiting and puzzle rooms to change their textures */
                startingRoom.ChangeTextures();
                for(int j = 0; j < i; j++) {
                    puzzleRooms[j].ChangeTextures();
                    waitingRooms[j].ChangeTextures();
                }

                i = waitingRooms.Length;
            }
        }
    }
}
