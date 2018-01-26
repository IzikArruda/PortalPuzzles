using UnityEngine;
using System.Collections;

/*
 * Create a set of stairs using the given parameters. The set of Transforms required are used
 * to get positions relevent to building the stairs procedurally.
 */
[ExecuteInEditMode]
public class StairsCreator : MonoBehaviour {

    /* Transforms that are placed in key positons that are used to define the skeleton of the stairs */
    public Transform startPoint;
    public Transform endPoint;
    private GameObject sideEdgePoint;
    private GameObject stairsUpwards;
    private GameObject stairsForwards;

    /* The parent that holds all stair pieces */
    public GameObject stairsContainer;

    /* Each individual GameObject that will make up the stairs */
    public GameObject[] stairs;

    /* Whether to update the stair's model on next frame or not */
    public bool updateStairs;

    /* The material used by the stairs */
    public Material stairsMaterial;

    /* Angles that define how the stairs are rotated */
    [Range(0, 1)]
    public float sideAngle;
    [Range(0, 180)]
    public float stairsAngle;

    /* How many steps the stairs will have */
    public int stepCount;
    
    /* Used to determine whether the steps will be above or bellow the plane. changes the type of step to start with */
    public bool bellowPlane;

    /* how wide the stairs are */
    public float stairsWidth;


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
         
        /* Create the three positionnal gameObjects */
        CreateEmptyObject(ref sideEdgePoint, "Side point", startPoint);
        CreateEmptyObject(ref stairsUpwards, "Step Up point", startPoint);
        CreateEmptyObject(ref stairsForwards, "Step Forward point", startPoint);
        
        /* Create the stairs container if needed */
        if(stairsContainer == null) {
            CreateEmptyObject(ref stairsContainer, "Stairs", transform);
        }

        /* Re-create the array for the stairs. Make sure the array is big enough for all the steps */
        CreateObjectsArray(ref stairs, stepCount*2 + 1, new Vector3(0, 0, 0));
        int index = 0;

        /* Properly position and rotate the transforms that represent the important points of the stairs */
        RepositionGivenTransforms();

        /* Extract the desired directions */
        Vector3 startEndDif = endPoint.position - startPoint.position;
        Vector3 sideDif = stairsWidth*0.5f*(sideEdgePoint.transform.position - startPoint.position).normalized;
        Vector3 stepUpwardDirection = (stairsUpwards.transform.position - startPoint.position).normalized;
        Vector3 stepForwardDirection = (stairsForwards.transform.position - stairsUpwards.transform.position).normalized;

        /* Extract the desired distances */
        float stepUpAngle = Vector3.Angle((startEndDif).normalized, stepUpwardDirection);
        float stepForwardAngle = 90 - stepUpAngle;
        float stepDistance = (startEndDif).magnitude/(float) stepCount;
        float stepUpDistance = stepDistance * Mathf.Sin(Mathf.PI*stepUpAngle/180f);
        float stepForwardDistance = stepDistance * Mathf.Sin(Mathf.PI*stepForwardAngle/180f);
        
        /* If we want to start with a forward and not upward step, switch the directions and distances */
        if(bellowPlane) {
            Vector3 tempDir = stepUpwardDirection;
            float tempDist = stepUpDistance;
            stepUpwardDirection = stepForwardDirection;
            stepForwardDirection = tempDir;
            stepUpDistance = stepForwardDistance;
            stepForwardDistance = tempDist;
        }

        /*  Create each step for the stairs through a loop */
        Vector3 end = startPoint.position;
        Vector3 start;
        for(int i = 0; i < stepCount; i++) {
            /* Update the position values by moving up to the next step */
            start = end;
            end += stepUpwardDirection*stepForwardDistance;
            /* Make the "upwards" part of the step */
            CreateEmptyObject(ref stairs[index], "Step " + (i+1) + "(Up)", stairsContainer.transform);
            stairs[index].transform.position = Vector3.zero;
            CreatePlane(end - sideDif, end + sideDif, start - sideDif, start + sideDif, stairs[index], 1);
            index++;

            /* Move forward to finish the current step */
            start = end;
            end += stepForwardDirection*stepUpDistance;
            /* Make the "forward" part of the step */
            CreateEmptyObject(ref stairs[index], "Step " + (i+1) + "(Forward)", stairsContainer.transform);
            stairs[index].transform.position = Vector3.zero;
            CreatePlane(end - sideDif, end + sideDif, start - sideDif, start + sideDif, stairs[index], 1);
            index++;
        }

        /* Create the plane of the stairs */
        Vector3 top1 = startPoint.position + sideDif;
        Vector3 top2 = startPoint.position - sideDif;
        Vector3 bot1 = endPoint.position + sideDif;
        Vector3 bot2 = endPoint.position - sideDif;
        CreateEmptyObject(ref stairs[index], "Main plane", stairsContainer.transform);
        stairs[index].transform.position = Vector3.zero;
        CreatePlane(top1, top2, bot1, bot2, stairs[index], 1);
        index++;

    }

    void RepositionGivenTransforms() {
        /*
         * Reposition the list of transforms that are used to define key positions of the stairs
         */

        /* Make the start point face the end point and re-position the sideStart point to reflect the given sideAngle */
        startPoint.rotation = Quaternion.LookRotation(endPoint.transform.position - startPoint.position);
        sideEdgePoint.transform.localPosition = new Vector3(Mathf.Cos(sideAngle*Mathf.PI*2), Mathf.Sin(sideAngle*Mathf.PI*2), 0);

        /* Get the sideDirection defined by the new position of the sideStart */
        Vector3 sideDirection = (sideEdgePoint.transform.position - startPoint.position).normalized;

        /* Position the StairAngle to be in it's default position, 1 unit along the plane's normal */
        stairsUpwards.transform.localPosition = new Vector3(-Mathf.Sin(sideAngle*Mathf.PI*2), Mathf.Cos(sideAngle*Mathf.PI*2), 0);

        /* Rotate the stairAngle point relative to the given angle. It's new position marks how steep each step will be */
        stairsUpwards.transform.RotateAround(startPoint.position, sideDirection, stairsAngle);

        /* The first step goes from start to stairAnglePoint. Then, the direction needs to hit a 90 degree turn. */
        stairsForwards.transform.position = startPoint.position;
        stairsForwards.transform.RotateAround(stairsUpwards.transform.position, sideDirection, -90);
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

        /* Delete the previous object if it exists */
        if(gameObject != null) { DestroyImmediate(gameObject); }

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
