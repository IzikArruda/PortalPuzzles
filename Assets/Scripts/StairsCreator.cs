using UnityEngine;
using System.Collections;

/*
 * Create a set of stairs that connect a set of points.
 */
[ExecuteInEditMode]
public class StairsCreator : MonoBehaviour {

    /* The three points that define the stairs */
    public GameObject startPoint;
    public GameObject endPoint;
    public GameObject sideStartPoint;
    public GameObject stairAnglePoint;
    public GameObject tempTestPoint;

    /* The parent that holds all stair pieces */
    public GameObject stairsContainer;

    /* The GameObjects that make up the stairs */
    public GameObject[] stairs;

    /* Whether to update the stair's model on next frame or not */
    public bool updateStairs;

    /* The material used by the stairs */
    public Material stairsMaterial;

    /* Angles that define how the stairs are rotated. Ranges between [0, 1] */
    [Range(0, 1)]
    public float sideAngle;
    [Range(0, 180)]
    public float stairsAngle;

    /* How many steps the stairs will have */
    public int stepCount;
    
    /* Used to determine whether the steps will be above or bellow the plane, 
     * which also changes the type of step to start with */
    public bool bellowPlane;

    /* -------- Built-in Unity Functions ---------------------------------------------------- */

    void Update() {
        /*
         * Update the stairs if told to do so
         */

        //if(updateStairs) {
            UpdateStairs();
            updateStairs = false;
        //}
    }


    /* -------- Update Functions ---------------------------------------------------- */

    void UpdateStairs() {
        /*
         * Re-create the stairs using the the set of points given to this script
         */

        /* Create the stairs container if needed */
        if(stairsContainer == null) {
            CreateEmptyObject(ref stairsContainer, "Stairs", transform);
        }

        /* Get the direction and distance between the start and end point */
        Vector3 difference = endPoint.transform.position - startPoint.transform.position;
        Vector3 direction = (difference).normalized;
        float distance = (difference).magnitude;

        /* Make the start point face the end point and re-position the sideStart point to reflect the given sideAngle */
        startPoint.transform.rotation = Quaternion.LookRotation(difference);
        Vector2 sideRotation = new Vector2(Mathf.Cos(sideAngle*Mathf.PI*2), Mathf.Sin(sideAngle*Mathf.PI*2));
        sideStartPoint.transform.localPosition = new Vector3(sideRotation.x, sideRotation.y, 0);
        
        /* Get the sideDirection defined by the new position of the sideStart */
        Vector3 sideDirection = (sideStartPoint.transform.position - startPoint.transform.position).normalized;







        
        
        /* Position the StairAngle to be in it's default position, 1 unit from the plane's normal */
        stairAnglePoint.transform.localPosition = new Vector3(-Mathf.Sin(sideAngle*Mathf.PI*2), Mathf.Cos(sideAngle*Mathf.PI*2), 0);

        /* Rotate the stairAngle point relative to the given angle. It's new position marks how steep each step will be */
        stairAnglePoint.transform.RotateAround(startPoint.transform.position, sideDirection, stairsAngle);

        //NOTE: stairAnglePoint cannot have the value of 0 or 180 as thats too sharp

        //The first step goes from start to stairAnglePoint. Then, the direction needs to hit a 90 degree turn.
        //This 90 degree turn can be done by doing another RotateAround, but from the start point.
        tempTestPoint.transform.position = startPoint.transform.position;
        tempTestPoint.transform.RotateAround(stairAnglePoint.transform.position, sideDirection, -90);



        //OK NOW THE FIRST STEP CAN BE FOIND BY DOING THIS PATH:
        //STARTPOINT - STAIRANGLEPOINT - TEMPTESTPOINT
        //these are directions: not point to point. The first direction will be a given set amount i think.
        //The next direction (stairAngle to tempTest) will continue on until it hits the plane that makes up the stairs.
        //I believe this might be calculatable and not require a raytrace, as we know two directions(2 angles) and 1 distance. Trig can solve that


        Vector3 stairAngleUpDirection = (stairAnglePoint.transform.position - startPoint.transform.position).normalized;
        Vector3 stairAngleSideDirection = (tempTestPoint.transform.position - stairAnglePoint.transform.position).normalized;

        //Draw the first step

        float stepLength = distance/(float) stepCount;
        float angle1 = Vector3.Angle(direction, stairAngleUpDirection);
        float angle2 = 90;
        float angle3 = 180 - angle1 - angle2;
        Debug.Log(angle1 + " _ " + angle3);
        //Get the distance for the two other lengths
        // stepup / Sin(angle1) = stepLength 
        // stepSide / Sin(angle3)
        //
        float stepUp = stepLength * Mathf.Sin(Mathf.PI*angle1/180f);
        float stepSide = stepLength * Mathf.Sin(Mathf.PI*angle3/180f);

        //Draw a ray using these values
        Vector3 topStairPoint = startPoint.transform.position + stairAngleUpDirection*stepSide;
        Vector3 nextStairStartPoint = topStairPoint + stairAngleSideDirection*stepUp;
        Debug.DrawLine(startPoint.transform.position, topStairPoint);
        Debug.DrawLine(topStairPoint, nextStairStartPoint);







        /* Re-create the array for the stairs. Make sure the array is big enough for all the steps */
        CreateObjectsArray(ref stairs, stepCount*2 + 1, new Vector3(0, 0, 0));
        int index = 0;


        /*  Create each step for the stairs through a loop */
        Vector3 end = startPoint.transform.position;
        Vector3 start;
        Vector3 sideDir = sideDirection * 1f;
        Vector3 upProgress = stairAngleUpDirection;
        Vector3 forwardProgress = stairAngleSideDirection;
        if(bellowPlane) {
            upProgress = stairAngleSideDirection;
            forwardProgress = stairAngleUpDirection;
            float temp = stepSide;
            stepSide = stepUp;
            stepUp = temp;
        }
        for(int i = 0; i < stepCount; i++) {
            /* Update the position values by moving up to the next step */
            start = end;
            end += upProgress*stepSide;
            /* Make the "upwards" part of the step */
            CreateEmptyObject(ref stairs[index], "Step " + (i+1) + "(Up)", stairsContainer.transform);
            stairs[index].transform.position = Vector3.zero;
            CreatePlane(end - sideDir, end + sideDir, start - sideDir, start + sideDir, stairs[index], 1);
            index++;

            /* Move forward to finish the current step */
            start = end;
            end += forwardProgress*stepUp;
            /* Make the "forward" part of the step */
            CreateEmptyObject(ref stairs[index], "Step " + (i+1) + "(Forward)", stairsContainer.transform);
            stairs[index].transform.position = Vector3.zero;
            CreatePlane(end - sideDir, end + sideDir, start - sideDir, start + sideDir, stairs[index], 1);
            index++;
        }

        /* Create the plane of the stairs */
        Vector3 top1 = startPoint.transform.position + sideDirection*1;
        Vector3 top2 = startPoint.transform.position - sideDirection*1;
        Vector3 bot1 = endPoint.transform.position + sideDirection*1;
        Vector3 bot2 = endPoint.transform.position - sideDirection*1;
        CreateEmptyObject(ref stairs[index], "Main plane", stairsContainer.transform);
        stairs[index].transform.position = Vector3.zero;
        CreatePlane(top1, top2, bot1, bot2, stairs[index], 1);
        index++;

    }


    /* -------- Helper Functions ---------------------------------------------------- */

    public void CreateObjectsArray(ref GameObject[] objects, int size, Vector3 position) {
        /*
         * Re-create the given array of gameObjects
         */

        /* Ensure each object is empty before creating now ones */
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i] != null) {
                DestroyImmediate(objects[i]);
            }
        }

        /* Create a new array with the new given size if needed */
        if(objects.Length != size) { objects = new GameObject[size]; }
    }
    
    public void CreateEmptyObject(ref GameObject gameObject, string name, Transform parent) {
        /*
         * Create an empty object, resetting their local position.
         */

        //gameObject = new GameObject();
        gameObject = new GameObject();
        gameObject.name = name;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = new Vector3(0, 0, 0);
        gameObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    
    public void CreatePlane(Vector3 top1, Vector3 top2, Vector3 bot1, Vector3 bot2, GameObject wall, float UVScale) {
        /*
         * Create a plane onto the given gameObject using the 4 given vector positions. 
         * The position of the vertex in the world determines how the UVs will be placed. 
         */
        Mesh planeMesh = new Mesh();
        Vector3[] vertices = null;
        Vector2[] UV = null;
        int[] triangles = null;
        
        /* Get the vertices and triangles that will form the mesh of the plane */
        CreateMesh(top1, top2, bot1, bot2, ref vertices, ref triangles);

        /* Set the UVs of the plane */
        Vector3 properCenter = wall.transform.rotation * wall.transform.position;
        UV = new Vector2[] {
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[0].x, vertices[0].z))/UVScale,
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[1].x, vertices[1].z))/UVScale,
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[2].x, vertices[2].z))/UVScale,
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[3].x, vertices[3].z))/UVScale
        };

        /* Assign the parameters to the mesh */
        planeMesh.vertices = vertices;
        planeMesh.triangles = triangles;
        planeMesh.uv = UV;
        planeMesh.RecalculateNormals();

        /* Add a meshFilter and a meshRenderer to be able to draw the wall */
        wall.AddComponent<MeshFilter>();
        wall.GetComponent<MeshFilter>().mesh = planeMesh;
        wall.AddComponent<MeshRenderer>();
        wall.GetComponent<MeshRenderer>().sharedMaterial = stairsMaterial;
    }
    
    public void CreateMesh(Vector3 top1, Vector3 top2, Vector3 bot1, Vector3 bot2, ref Vector3[] vertices, ref int[] triangles) {
        /*
    	 * Create a mesh using the given scale values and save it's verts and triangles into the given references.
    	 * It expects the given arrays to not yet be initialized. The given boolean determines the order of the triangles.
         * 
         * Depending on the given wallType, place the vectors in their appropriate position
    	 */

        /* Create the vertices that form the plane given by the 4 vertices */
        vertices = new Vector3[4];
        vertices[0] = top1;
        vertices[1] = top2;
        vertices[2] = bot1;
        vertices[3] = bot2;
        
        /* Create the triangles for the plane */
        triangles = new int[]{
            0, 1, 2,
            3, 2, 1
        };
    }
}
