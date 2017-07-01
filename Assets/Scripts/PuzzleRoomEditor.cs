using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class PuzzleRoomEditor : MonoBehaviour {

    /* The rooms that connect to the exit and entrance holes */
    public AttachedRoom entrance;
    public AttachedRoom exit;

    /* The position that the entrance and the exit rooms will be */
    public Transform puzzleRoomEntrancePoint;
    public Transform puzzleRoomExitPoint;

    /* The walls that make up the puzzle room */
    public GameObject entrenceUpperWall;
    public GameObject entrenceLowerWall;
    public GameObject entrenceSideWall1;
    public GameObject entrenceSideWall2;
    public GameObject exitUpperWall;
    public GameObject exitLowerWall;
    public GameObject exitSideWall1;
    public GameObject exitSideWall2;
    public GameObject sideWall1;
    public GameObject sideWall2;
    
    /* The sizes/ideal sizes of the room */
    public float idealRoomWidth;
    private float roomWidth;
    private float roomLength;
    public float roomHeight;


    void Update () {
        //For now, all room attached will be 5 units wide and 1.5 units tall
        float attachedRoomWidth = 5;
        float attachedRoomHeight = 1.5f;
        Vector3 positionDifference;

        /* Position the attached rooms in their given positions */
        //Fix the entrance's position
        positionDifference = entrance.exitPoint.position - puzzleRoomEntrancePoint.position;
        entrance.transform.position -= positionDifference;
        //Fix the exit's position
        positionDifference = exit.exitPoint.position - puzzleRoomExitPoint.position;
        exit.transform.position -= positionDifference;
        

        /* Use the distance between the two attached room points to adjust the puzzle room's current sizes */
        //Get the distance between the two connected rooms
        positionDifference = entrance.exitPoint.position - exit.exitPoint.position;
        //Update the room's current length
        roomLength = Mathf.Abs(positionDifference.z);
        Debug.Log(Mathf.Abs(positionDifference.x));
        //Update the room's current width
        roomWidth = idealRoomWidth + Mathf.Abs(positionDifference.x);
        //Track the widthDifference to properly allign the corners of the puzzle room
        float widthDifference = positionDifference.x;


        /* Place the puzzle room's side walls to reflect the room's current width. To 
         * do this we need the point that is in the center between the two connected rooms. */
        Vector3 centerPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;
        sideWall1.transform.position = centerPoint + new Vector3(roomWidth + attachedRoomWidth/2f, 0, 0);
        sideWall2.transform.position = centerPoint + new Vector3(-roomWidth - attachedRoomWidth/2f, 0, 0);
        //Fix the width of the wall to reflect the room's length
        sideWall1.transform.localScale = new Vector3(roomLength/10f, 1, roomHeight/10f);
        sideWall2.transform.localScale = new Vector3(roomLength/10f, 1, roomHeight/10f);


        /* Place the walls that are situated above and bellow the puzzle room's entrance/exit */
        //Place the upper entrance wall
        entrenceUpperWall.transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, roomHeight/4f + attachedRoomHeight, 0);
        entrenceUpperWall.transform.localScale = new Vector3(attachedRoomWidth/10f, 1, roomHeight/20f - attachedRoomHeight/5f);
        //Place the lower entrance wall
        entrenceLowerWall.transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, -roomHeight/4f, 0);
        entrenceLowerWall.transform.localScale = new Vector3(attachedRoomWidth/10f, 1, roomHeight/20f);
        //place the upper exit wall
        exitUpperWall.transform.position = puzzleRoomExitPoint.position + new Vector3(0, roomHeight/4f + attachedRoomHeight, 0);
        exitUpperWall.transform.localScale = new Vector3(attachedRoomWidth/10f, 1, roomHeight/20f - attachedRoomHeight/5f);
        //place the lower exit wall
        exitLowerWall.transform.position = puzzleRoomExitPoint.position + new Vector3(0, -roomHeight/4f, 0);
        exitLowerWall.transform.localScale = new Vector3(attachedRoomWidth/10f, 1, roomHeight/20f);


        /* Place the walls that are on the side of the entrance/exit */
        //Place the two walls on the entrance side
        entrenceSideWall1.transform.position = puzzleRoomEntrancePoint.position + new Vector3(roomWidth/2f + attachedRoomWidth/2f - widthDifference/4f, 0, 0);
        entrenceSideWall1.transform.localScale = new Vector3(roomWidth/10f - widthDifference/20f, 1, roomHeight/10f);
        entrenceSideWall2.transform.position = puzzleRoomEntrancePoint.position + new Vector3(-roomWidth/2f - attachedRoomWidth/2f - widthDifference/4f, 0, 0);
        entrenceSideWall2.transform.localScale = new Vector3(roomWidth/10f + widthDifference/20f, 1, roomHeight/10f);
        //Place the two walls on the exit side
        exitSideWall1.transform.position = puzzleRoomExitPoint.position + new Vector3(-roomWidth/2f - attachedRoomWidth/2f + widthDifference/4f, 0, 0);
        exitSideWall1.transform.localScale = new Vector3(roomWidth/10f - widthDifference/20f, 1, roomHeight/10f);
        exitSideWall2.transform.position = puzzleRoomExitPoint.position + new Vector3(+roomWidth/2f + attachedRoomWidth/2f + widthDifference/4f, 0, 0);
        exitSideWall2.transform.localScale = new Vector3(roomWidth/10f + widthDifference/20f, 1, roomHeight/10f);



    }
}
