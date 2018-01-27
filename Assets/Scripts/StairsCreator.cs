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
    public GameObject stairsObject;
    public GameObject[] stairsBase;

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
    
    /* how wide the stairs are */
    public float stairsWidth;



    /* How far down the base goes from the steps */
    public float baseDepth;





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

        /* Create the parts of the mesh with enough room for each step */
        int vertCount = 18;
        Vector3[] vertices = new Vector3[stepCount*vertCount];
        Vector2[] UVs = new Vector2[stepCount*vertCount];
        int[] triangles = new int[stepCount*vertCount];
        /* Initialize required positions for each step */
        Vector3 start;
        Vector3 midway;
        Vector3 end = startPoint.position;
        /* Create each step for the stairs through a loop */
        for(int i = 0; i < stepCount; i++) {
            /* Get the position values for each part of this step */
            start = end;
            midway = start + stepUpwardDirection*stepForwardDistance;
            end = midway + stepForwardDirection*stepUpDistance;
            
            /* Add to the mesh components with this new step and it's positions */
            AddStep(start, midway, end, sideDif, i*vertCount, ref vertices, ref UVs, ref triangles);
        }

        /* Add the required components to the stairs object and apply the mesh */
        CreateEmptyObject(ref stairsObject, "Stairs object", stairsContainer.transform);
        Mesh stairsMesh = new Mesh();
        stairsMesh.vertices = vertices;
        stairsMesh.triangles = triangles;
        stairsMesh.uv = UVs;
        stairsObject.AddComponent<MeshFilter>().sharedMesh = stairsMesh;
        stairsObject.AddComponent<MeshRenderer>().sharedMaterial = stairsMaterial;

        
        /* Create a new object used for the base of the stairs */
        CreateObjectsArray(ref stairsBase, 10, new Vector3(0, 0, 0));
        int index = 0;
        Vector3 oldStart = startPoint.position;
        Vector3 newStart = oldStart;
        Vector3 oldEnd = endPoint.position;
        Vector3 newEnd = oldEnd;
        if(baseDepth > 0) {
            newStart -= baseDepth*stepUpwardDirection;
            newEnd -= baseDepth*stepUpwardDirection;

            /* Create planes that connect the stairs to it's base */
            CreateEmptyObject(ref stairsBase[index], "Start base", stairsContainer.transform);
            stairsBase[index].transform.position = Vector3.zero;
            CreatePlane(newStart + sideDif, newStart - sideDif, oldStart + sideDif, oldStart - sideDif, stairsBase[index], 1);
            index++;

            CreateEmptyObject(ref stairsBase[index], "Left base", stairsContainer.transform);
            stairsBase[index].transform.position = Vector3.zero;
            CreatePlane(oldStart - sideDif, newStart - sideDif, oldEnd - sideDif, newEnd - sideDif, stairsBase[index], 1);
            index++;
            
            CreateEmptyObject(ref stairsBase[index], "Right base", stairsContainer.transform);
            stairsBase[index].transform.position = Vector3.zero;
            CreatePlane(newStart + sideDif, oldStart + sideDif, newEnd + sideDif, oldEnd + sideDif, stairsBase[index], 1);
            index++;

            CreateEmptyObject(ref stairsBase[index], "End base", stairsContainer.transform);
            stairsBase[index].transform.position = Vector3.zero;
            CreatePlane(newEnd - sideDif, newEnd + sideDif, oldEnd - sideDif, oldEnd + sideDif, stairsBase[index], 1);
            index++;
        }

        /* Create the base directly bellow the stairs */
        Vector3 top1 = newStart + sideDif;
        Vector3 top2 = newStart - sideDif;
        Vector3 bot1 = newEnd + sideDif;
        Vector3 bot2 = newEnd - sideDif;
        CreateEmptyObject(ref stairsBase[index], "Main base", stairsContainer.transform);
        stairsBase[index].transform.position = Vector3.zero;
        CreatePlane(top2, top1, bot2, bot1, stairsBase[index], 1);
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
    
    public void AddStep(Vector3 start, Vector3 midway, Vector3 end, Vector3 sideDirection, 
            int index, ref Vector3[] vertices, ref Vector2[] UVs, ref int[] triangles) {
        /*
         * Given the positions of the next step, add it to the mesh's components.
         */

        /* Add the step's vertices to the array */
        //First plane
        vertices[index + 0] = start - sideDirection;
        vertices[index + 1] = start + sideDirection;
        vertices[index + 2] = midway - sideDirection;
        vertices[index + 3] = start + sideDirection;
        vertices[index + 4] = midway + sideDirection;
        vertices[index + 5] = midway - sideDirection;
        //Second plane
        vertices[index + 6] = midway - sideDirection;
        vertices[index + 7] = midway + sideDirection;
        vertices[index + 8] = end - sideDirection;
        vertices[index + 9] = midway + sideDirection;
        vertices[index + 10] = end + sideDirection;
        vertices[index + 11] = end - sideDirection;
        //Sides of the steps
        vertices[index + 12] = start - sideDirection;
        vertices[index + 13] = midway - sideDirection;
        vertices[index + 14] = end - sideDirection;
        vertices[index + 15] = start + sideDirection;
        vertices[index + 16] = midway + sideDirection;
        vertices[index + 17] = end + sideDirection;
        
        /* Add the proper triangles for the step */
        //First plane
        triangles[index + 0] = index + 2;
        triangles[index + 1] = index + 1;
        triangles[index + 2] = index + 0;
        triangles[index + 3] = index + 5;
        triangles[index + 4] = index + 4;
        triangles[index + 5] = index + 3;
        //Second plane
        triangles[index + 6] = index + 8;
        triangles[index + 7] = index + 7;
        triangles[index + 8] = index + 6;
        triangles[index + 9] = index + 11;
        triangles[index + 10] = index + 10;
        triangles[index + 11] = index + 9;
        //Sides of the steps
        triangles[index + 12] = index + 14;
        triangles[index + 13] = index + 13;
        triangles[index + 14] = index + 12;
        triangles[index + 15] = index + 15;
        triangles[index + 16] = index + 16;
        triangles[index + 17] = index + 17;

        /* Add the proper UVs for each vertex */
        UVs[index + 0] = vertices[index + 0];
        UVs[index + 1] = vertices[index + 1];
        UVs[index + 2] = vertices[index + 2];
        UVs[index + 3] = vertices[index + 3];
        UVs[index + 4] = vertices[index + 4];
        UVs[index + 5] = vertices[index + 5];
        UVs[index + 6] = vertices[index + 6];
        UVs[index + 7] = vertices[index + 7];
        UVs[index + 8] = vertices[index + 8];
        UVs[index + 9] = vertices[index + 9];
        UVs[index + 10] = vertices[index + 10];
        UVs[index + 11] = vertices[index + 11];
        UVs[index + 12] = vertices[index + 12];
        UVs[index + 13] = vertices[index + 13];
        UVs[index + 14] = vertices[index + 14];
        UVs[index + 15] = vertices[index + 15];
        UVs[index + 16] = vertices[index + 16];
        UVs[index + 17] = vertices[index + 17];
    }
}
