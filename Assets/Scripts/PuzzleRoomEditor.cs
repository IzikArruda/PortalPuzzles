using UnityEngine;
using System.Collections;

/*
 * A script that builds the walls around the puzzle room. 
 * 
 * Requires a link to the exits and entrances that connect to the room.
 */
[ExecuteInEditMode]
public class PuzzleRoomEditor : MonoBehaviour {

    /* The rooms that connect to the exit and entrance holes */
    public AttachedRoom entrance;
    public AttachedRoom exit;

    /* The position that the entrance and the exit rooms will be */
    public Transform puzzleRoomEntrancePoint;
    public Transform puzzleRoomExitPoint;

    /* The parent of all the walls that form the puzzle room */
    public Transform puzzleRoomWalls;

    /* The walls that make up the puzzle room */
    private GameObject entrenceUpperWall;
    private GameObject entrenceLowerWall;
    private GameObject entrenceSideWall1;
    private GameObject entrenceSideWall2;
    private GameObject exitUpperWall;
    private GameObject exitLowerWall;
    private GameObject exitSideWall1;
    private GameObject exitSideWall2;
    private GameObject sideWall1;
    private GameObject sideWall2;
    
    /* The sizes/ideal sizes of the room */
    public float givenRoomWidth;
    public float givenRoomHeight;
    private float roomWidth;
    private float roomLength;
    private float roomHeight;

    /* The material used on the walls */
    public Material wallMaterial;


    void Update () {
        /*
         * Update the walls that form the puzzle room when the scene updates
         */

        /* Ensure the linked wall objects are all created before positioning them */
        CreateWalls();
        
        /* Position the attached rooms into their given positions */
        RepositionAttachedRooms();
        
        /* Update the transform of the walls that form the puzzle room */
        UpdateWalls();
    }
    
    private void CreateWalls() {
        /*
         * Check if each wall used to form the puzzle room is properly initiated with the proper components
         */

        /* Use an array to hold all the walls that will be used to form the puzzle room */
        GameObject[] walls = { entrenceUpperWall, entrenceLowerWall, entrenceSideWall1, entrenceSideWall2,
                exitUpperWall, exitLowerWall, exitSideWall1, exitSideWall2, sideWall1, sideWall2};

        
        /* Each wall must be capable of rendering an object and have the proper parent, name and material used */
        for(int i = 0; i < walls.Length; i++) {
            if(walls[i] == null) {
                walls[i] = GameObject.CreatePrimitive(PrimitiveType.Plane);
                walls[i].transform.parent = puzzleRoomWalls;
                walls[i].name = "Infinite Wall";

                /* Assign a material to the wall */
                walls[i].GetComponent<MeshRenderer>().material = wallMaterial;
            }
        }


        /* Reassign each wall to the changed values in the array */
        entrenceUpperWall = walls[0];
        entrenceLowerWall = walls[1];
        entrenceSideWall1 = walls[2];
        entrenceSideWall2 = walls[3];
        exitUpperWall = walls[4];
        exitLowerWall = walls[5];
        exitSideWall1 = walls[6];
        exitSideWall2 = walls[7];
        sideWall1 = walls[8];
        sideWall2 = walls[9];
    }
    
    private void RepositionAttachedRooms() {
        /*
         * Move the linked attached rooms to the givenpoints defined by the two puzzleRoomPoint transforms.
         * The attached rooms will be moved so their exit point shares the same transform as
         * the corresponding puzzleRoomPoint.
         */
        Vector3 positionDifference;
        
        /* Reposition the entrance room */
        positionDifference = entrance.exitPoint.position - puzzleRoomEntrancePoint.position;
        entrance.transform.position -= positionDifference;
        
        /* Reposition the exit room */
        positionDifference = exit.exitPoint.position - puzzleRoomExitPoint.position;
        exit.transform.position -= positionDifference;
    }
    
    private void UpdateWalls() {
        /*
         * Update the position, rotation and scale of the walls that form the puzzle room to adjust 
         * to the sizes of the room given and the position of the attached rooms.
         */
         

        /* Get the sizes of the attached rooms's exits */
        float attachedEntranceWidth = entrance.exitWidth;
        float attachedEntranceHeight = entrance.exitHeight;
        float attachedExitWidth = exit.exitWidth;
        float attachedExitHeight = exit.exitHeight;


        /* Calcualte the sizes of the puzzle room using the distance between the two connected rooms */
        Vector3 positionDifference = entrance.exitPoint.position - exit.exitPoint.position;
        roomLength = Mathf.Abs(positionDifference.z);
        roomWidth = givenRoomWidth + Mathf.Abs(positionDifference.x);
        roomHeight = givenRoomHeight + Mathf.Abs(positionDifference.y);
        /* Track the widthDifference to properly allign the corners of the puzzle room */
        float widthDifference = positionDifference.x;


        /* Place the puzzle room's side walls to reflect the room's current width. To 
         * do this we need the point that is in the center between the two connected rooms. */
        Vector3 centerPoint = (puzzleRoomEntrancePoint.position + puzzleRoomExitPoint.position)/2f;
        sideWall1.transform.position = centerPoint + new Vector3(roomWidth + Mathf.Min(attachedEntranceWidth, attachedExitWidth)/2f, 0, 0);
        CreateWallMesh(sideWall1, roomLength, roomHeight);
        sideWall1.transform.localEulerAngles = new Vector3(90, -90, 0);
        sideWall2.transform.position = centerPoint + new Vector3(-roomWidth - Mathf.Min(attachedEntranceWidth, attachedExitWidth)/2f, 0, 0);
        CreateWallMesh(sideWall2, roomLength, roomHeight);
        sideWall2.transform.localEulerAngles = new Vector3(90, 90, 0);
        

        /* Place the walls that are situated above and bellow the puzzle room's entrance/exit */
        entrenceUpperWall.transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, roomHeight/4f + attachedEntranceHeight, 0);
        CreateWallMesh(entrenceUpperWall, attachedEntranceWidth, roomHeight/2 - attachedEntranceHeight*2);
        entrenceUpperWall.transform.localEulerAngles = new Vector3(90, 0, 0);

        entrenceLowerWall.transform.position = puzzleRoomEntrancePoint.position + new Vector3(0, -roomHeight/4f, 0);
        CreateWallMesh(entrenceLowerWall, attachedEntranceWidth, roomHeight/2);
        entrenceLowerWall.transform.localEulerAngles = new Vector3(90, 0, 0);

        exitUpperWall.transform.position = puzzleRoomExitPoint.position + new Vector3(0, roomHeight/4f + attachedExitHeight, 0);
        CreateWallMesh(exitUpperWall, attachedEntranceWidth, roomHeight/2 - attachedExitHeight*2f);
        exitUpperWall.transform.localEulerAngles = new Vector3(-90, 0, 0);

        exitLowerWall.transform.position = puzzleRoomExitPoint.position + new Vector3(0, -roomHeight/4f, 0);
        CreateWallMesh(exitLowerWall, attachedEntranceWidth, roomHeight/2);
        exitLowerWall.transform.localEulerAngles = new Vector3(-90, 0, 0);


        /* Place the walls that are on the side of the entrance/exit */
        entrenceSideWall1.transform.position = puzzleRoomEntrancePoint.position + new Vector3(roomWidth/2f + attachedEntranceWidth/2f - widthDifference/4f, 0, 0);
        CreateWallMesh(entrenceSideWall1, roomWidth - widthDifference/2f, roomHeight);
        entrenceSideWall1.transform.localEulerAngles = new Vector3(90, 0, 0);

        entrenceSideWall2.transform.position = puzzleRoomEntrancePoint.position + new Vector3(-roomWidth/2f - attachedEntranceWidth/2f - widthDifference/4f, 0, 0);
        CreateWallMesh(entrenceSideWall2, roomWidth + widthDifference/2f, roomHeight);
        entrenceSideWall2.transform.localEulerAngles = new Vector3(90, 0, 0);

        exitSideWall1.transform.position = puzzleRoomExitPoint.position + new Vector3(-roomWidth/2f - attachedExitWidth/2f + widthDifference/4f, 0, 0);
        CreateWallMesh(exitSideWall1, roomWidth - widthDifference/2f, roomHeight);
        exitSideWall1.transform.localEulerAngles = new Vector3(-90, 0, 0);

        exitSideWall2.transform.position = puzzleRoomExitPoint.position + new Vector3(+roomWidth/2f + attachedExitWidth/2f + widthDifference/4f, 0, 0);
        CreateWallMesh(exitSideWall2, roomWidth + widthDifference/2f, roomHeight);
        exitSideWall2.transform.localEulerAngles = new Vector3(-90, 0, 0);
    }




    private void CreateWallMesh(GameObject wall, float xScale, float zScale) {
        /*
         * Use the given parameters to create the mesh that forms a wall.
         * The mesh will be centered at (0, 0, 0).
         * 
         * The given gameObject will have it's mesh used in it's meshFilter component changed.
         */
        Mesh wallMesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] UV;
        int[] triangles;


        /* Set the vertices of the plane */
        vertices[0] = new Vector3(0.5f*xScale, 0, 0.5f*zScale);
        vertices[1] = new Vector3(-0.5f*xScale, 0, 0.5f*zScale);
        vertices[2] = new Vector3(-0.5f*xScale, 0, -0.5f*zScale);
        vertices[3] = new Vector3(0.5f*xScale, 0, -0.5f*zScale);


        /* Set the two triangles that form the plane */
        triangles = new int[]{
            2, 1, 0,
            3, 2, 0 
        };

        /* Set the UVs of the plane */
        UV = new Vector2[] {
            new Vector2(vertices[0].x, vertices[0].z),
            new Vector2(vertices[1].x, vertices[1].z),
            new Vector2(vertices[2].x, vertices[2].z),
            new Vector2(vertices[3].x, vertices[3].z)
        };


        /* Assign the parameters to the mesh */
        wallMesh.vertices = vertices;
        wallMesh.triangles = triangles;
        wallMesh.uv = UV;
        wallMesh.RecalculateNormals();

        /* Assign the mesh to the given wall */
        wall.GetComponent<MeshFilter>().mesh = wallMesh;
        wall.GetComponent<MeshCollider>().sharedMesh = wallMesh;

    }
}
