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
    public Material otherMaterial;

    /* Angles that define how the stairs are rotated */
    [Range(0, 1)]
    public float sideAngle;
    [Range(0, 180)]
    public float stairsAngle;

    /* Set to true if you want to reset the angle */
    public bool resetAngle;

    /* How long each step is expected to be */
    public float stepSize;
    
    /* how wide the stairs are */
    public float stairsWidth;
    
    /* How far down the base goes from the steps */
    public float baseDepth;

    

    /* -------- Built-in Unity Functions ---------------------------------------------------- */

    void Update() {
        /*
         * Update the stairs if told to do so
         */

        /* Prevent the stepSize from being too small */
        float minStepSize = 0.001f;
        if(stepSize < minStepSize) {
            stepSize = minStepSize;
        }

        /* Reset the angle so the stairs are flat relative to the stair's transform's axis */
        if(resetAngle) {
            resetAngle = false;
            Vector3 currentAngle = (endPoint.transform.position - startPoint.transform.position).normalized;
            Vector3 neutralAngle = transform.rotation*Vector3.up;
            stairsAngle = 90 - Vector3.Angle(currentAngle, neutralAngle);

            /* Prevent the angle from going out of it's range */
            while(stairsAngle < 0) {
                stairsAngle += 90;
            }
            while(stairsAngle > 90) {
                stairsAngle -= 90;
            }
        }

        /* Update the stairs  */
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

        /* Find the distance the steps will cover and using the given stepLength, find how many steps to create */
        float totalStepDistance = (startEndDif).magnitude;
        int stepCount = Mathf.CeilToInt(totalStepDistance / stepSize);
        
        /* Extract the desired distances */
        float stepUpAngle = Vector3.Angle((startEndDif).normalized, stepUpwardDirection);
        float stepForwardAngle = 90 - stepUpAngle;
        float stepDistance = totalStepDistance/(float) stepCount;
        float stepUpDistance = stepDistance * Mathf.Sin(Mathf.PI*stepUpAngle/180f);
        float stepForwardDistance = stepDistance * Mathf.Sin(Mathf.PI*stepForwardAngle/180f);

        /* Create the parts of the mesh with enough room for each step */
        int vertCount = 18;
        Vector3[] vertices = new Vector3[stepCount*vertCount];
        Vector2[] UVs = new Vector2[stepCount*vertCount];
        int[] triangles = new int[stepCount*(vertCount-12)];
        int[] trianglesAlt = new int[stepCount*12];
        /* Initialize required positions for each step */
        Vector3 start;
        Vector3 midway;
        Vector3 end = startPoint.position;
        float UVPos = 0;
        /* Create each step for the stairs through a loop */
        for(int i = 0; i < stepCount; i++) {
            /* Get the position values for each part of this step */
            start = end;
            midway = start + stepUpwardDirection*stepForwardDistance;
            end = midway + stepForwardDirection*stepUpDistance;
            
            /* Add to the mesh components with this new step and it's positions */
            AddStep(start, midway, end, sideDif, i, vertCount, ref vertices, ref UVs, ref triangles, ref trianglesAlt, ref UVPos, stairsWidth, stepDistance, stepForwardDistance, stepUpDistance);
        }

        /* Add the required components to the stairs object and apply the mesh */
        CreateEmptyObject(ref stairsObject, "Stairs object", stairsContainer.transform);
        Mesh stairsMesh = new Mesh();
        stairsMesh.name = "Accurate Stairs mesh";
        stairsMesh.subMeshCount = 2;
        stairsMesh.vertices = vertices;
        stairsMesh.SetTriangles(triangles, 0);
        stairsMesh.SetTriangles(trianglesAlt, 1);
        stairsMesh.uv = UVs;
        stairsObject.AddComponent<MeshFilter>().sharedMesh = stairsMesh;
        stairsObject.AddComponent<MeshRenderer>().sharedMaterials = new Material[] { stairsMaterial, otherMaterial };
        
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
            stairsBase[index].transform.localPosition = Vector3.zero;
            CreatePlane(newStart + sideDif, newStart - sideDif, oldStart + sideDif, oldStart - sideDif, stairsBase[index], otherMaterial, stairsWidth, 0.2f, baseDepth);
            index++;

            CreateEmptyObject(ref stairsBase[index], "Left base", stairsContainer.transform);
            stairsBase[index].transform.localPosition = Vector3.zero;
            CreatePlane(newStart - sideDif, newEnd - sideDif, oldStart - sideDif, oldEnd - sideDif, stairsBase[index], otherMaterial, 1, 0.2f, baseDepth);
            index++;
            
            CreateEmptyObject(ref stairsBase[index], "Right base", stairsContainer.transform);
            stairsBase[index].transform.localPosition = Vector3.zero;
            CreatePlane(newEnd + sideDif, newStart + sideDif, oldEnd + sideDif, oldStart + sideDif, stairsBase[index], otherMaterial, 1, 0.2f, baseDepth);
            index++;

            CreateEmptyObject(ref stairsBase[index], "End base", stairsContainer.transform);
            stairsBase[index].transform.localPosition = Vector3.zero;
            CreatePlane(newEnd - sideDif, newEnd + sideDif, oldEnd - sideDif, oldEnd + sideDif, stairsBase[index], otherMaterial, stairsWidth, 0.2f, baseDepth);
            index++;
        }

        /* Create the base directly bellow the stairs */
        Vector3 top1 = newStart + sideDif;
        Vector3 top2 = newStart - sideDif;
        Vector3 bot1 = newEnd + sideDif;
        Vector3 bot2 = newEnd - sideDif;
        CreateEmptyObject(ref stairsBase[index], "Main base", stairsContainer.transform);
        stairsBase[index].transform.localPosition = Vector3.zero;
        CreatePlane(top2, top1, bot2, bot1, stairsBase[index], stairsMaterial, stairsWidth, 1, totalStepDistance);
        index++;
        
        /* Create a mesh collider of the stairs. The steps part will be a slope. */
        Mesh roughStairsMesh = RoughStairsMesh(stepUpwardDirection*stepForwardDistance,
                stepForwardDirection*stepUpDistance, oldStart, oldEnd, newStart, newEnd, sideDif);
        if(stairsContainer.GetComponent<MeshCollider>() == null) { stairsContainer.AddComponent<MeshCollider>(); }
        stairsContainer.GetComponent<MeshCollider>().sharedMesh = roughStairsMesh;
        stairsContainer.GetComponent<MeshCollider>().convex = true;

        /* Move the stair container so the stairs are properly placed in the world relative to this transform */
        stairsContainer.transform.position = new Vector3(0, 0, 0);
        stairsContainer.transform.eulerAngles = new Vector3(0, 0, 0);
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
    
    public void CreatePlane(Vector3 top1, Vector3 top2, Vector3 bot1, Vector3 bot2, GameObject wall, 
            Material material, float UVScaleX, float UVScaleY, float stepDistance) {
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
        /*UV = new Vector2[] {
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[0].x, vertices[0].z))/UVScaleX,
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[1].x, vertices[1].z))/UVScaleX,
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[2].x, vertices[2].z))/UVScaleX,
            (new Vector2(properCenter.x, properCenter.z) + new Vector2(vertices[3].x, vertices[3].z))/UVScaleX
        };*/
        UV = new Vector2[] {
            new Vector2(+UVScaleX/2f, -stepDistance*UVScaleY),
            new Vector2(-UVScaleX/2f, -stepDistance*UVScaleY),
            new Vector2(+UVScaleX/2f, 0),
            new Vector2(-UVScaleX/2f, 0),
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
        wall.GetComponent<MeshRenderer>().sharedMaterial = material;
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
            int i, int vertCount, ref Vector3[] vertices, ref Vector2[] UVs, ref int[] triangles,
            ref int[] trianglesAlt, ref float UVPos, float stairWidth, float stepDist, float stepUpDistance, float stepForwardDistance) {
        /*
         * Given the positions of the next step, add it to the mesh's components.
         */

        /* Add the step's vertices to the array */
        //First plane
        vertices[i*vertCount + 0] = start - sideDirection;
        vertices[i*vertCount + 1] = start + sideDirection;
        vertices[i*vertCount + 2] = midway - sideDirection;
        vertices[i*vertCount + 3] = start + sideDirection;
        vertices[i*vertCount + 4] = midway + sideDirection;
        vertices[i*vertCount + 5] = midway - sideDirection;
        //Second plane
        vertices[i*vertCount + 6] = midway - sideDirection;
        vertices[i*vertCount + 7] = midway + sideDirection;
        vertices[i*vertCount + 8] = end - sideDirection;
        vertices[i*vertCount + 9] = midway + sideDirection;
        vertices[i*vertCount + 10] = end + sideDirection;
        vertices[i*vertCount + 11] = end - sideDirection;
        //Sides of the steps
        vertices[i*vertCount + 12] = start - sideDirection;
        vertices[i*vertCount + 13] = midway - sideDirection;
        vertices[i*vertCount + 14] = end - sideDirection;
        vertices[i*vertCount + 15] = start + sideDirection;
        vertices[i*vertCount + 16] = midway + sideDirection;
        vertices[i*vertCount + 17] = end + sideDirection;

        /* Add the proper triangles for the step */
        //Upwards plane
        triangles[i*(vertCount-12) + 0] = i*vertCount + 8;
        triangles[i*(vertCount-12) + 1] = i*vertCount + 7;
        triangles[i*(vertCount-12) + 2] = i*vertCount + 6;
        triangles[i*(vertCount-12) + 3] = i*vertCount + 11;
        triangles[i*(vertCount-12) + 4] = i*vertCount + 10;
        triangles[i*(vertCount-12) + 5] = i*vertCount + 9;
        //Forwards plane
        trianglesAlt[i*12 + 0] = i*vertCount + 2;
        trianglesAlt[i*12 + 1] = i*vertCount + 1;
        trianglesAlt[i*12 + 2] = i*vertCount + 0;
        trianglesAlt[i*12 + 3] = i*vertCount + 5;
        trianglesAlt[i*12 + 4] = i*vertCount + 4;
        trianglesAlt[i*12 + 5] = i*vertCount + 3;
        //Sides of the steps
        trianglesAlt[i*12 + 6] = i*vertCount + 14;
        trianglesAlt[i*12 + 7] = i*vertCount + 13;
        trianglesAlt[i*12 + 8] = i*vertCount + 12;
        trianglesAlt[i*12 + 9] = i*vertCount + 15;
        trianglesAlt[i*12 + 10] = i*vertCount + 16;
        trianglesAlt[i*12 + 11] = i*vertCount + 17;

        /* Add the proper UVs for each vertex */
        UVs[i*vertCount + 0] = new Vector2(-stairsWidth/2f, UVPos);
        UVs[i*vertCount + 1] = new Vector2(+stairsWidth/2f, UVPos);
        UVs[i*vertCount + 2] = new Vector2(-stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 3] = new Vector2(+stairsWidth/2f, UVPos);
        UVs[i*vertCount + 4] = new Vector2(+stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 5] = new Vector2(-stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 6] = new Vector2(-stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 7] = new Vector2(+stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 8] = new Vector2(-stairsWidth/2f, UVPos + stepUpDistance/5f + stepForwardDistance);
        UVs[i*vertCount + 9] = new Vector2(+stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 10] = new Vector2(+stairsWidth/2f, UVPos + stepUpDistance/5f + stepForwardDistance);
        UVs[i*vertCount + 11] = new Vector2(-stairsWidth/2f, UVPos + stepUpDistance/5f + stepForwardDistance);
        UVs[i*vertCount + 12] = new Vector2(-stairsWidth/2f, UVPos);
        UVs[i*vertCount + 13] = new Vector2(-stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 14] = new Vector2(-stairsWidth/2f - stepForwardDistance, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 15] = new Vector2(+stairsWidth/2f, UVPos);
        UVs[i*vertCount + 16] = new Vector2(+stairsWidth/2f, UVPos + stepUpDistance/5f);
        UVs[i*vertCount + 17] = new Vector2(+stairsWidth/2f + stepForwardDistance, UVPos + stepUpDistance/5f);
        UVPos += stepForwardDistance + stepUpDistance/5f;
    }

    public Mesh RoughStairsMesh(Vector3 stepUpVector, Vector3 stepForwardVector,
            Vector3 start, Vector3 end, Vector3 baseStart, Vector3 baseEnd, Vector3 sideDif) {
        /*
         * Create a rough estimate of the stairs as a mesh. The base will be accurate, but the
         * steps part will be a slope. Use the given positions and directions to return the mesh.
         */
        Mesh mesh = new Mesh();

        /* Use the step vectors to create a more accurate plane for the steps */
        start += stepUpVector/2f;
        Vector3[] vertices = new Vector3[] {
            start - sideDif,
            baseStart - sideDif,
            start + sideDif,
            baseStart + sideDif,
            end + sideDif,
            baseEnd + sideDif,
            end - sideDif,
            baseEnd - sideDif,
            end - stepForwardVector/2f + sideDif,
            end - stepForwardVector/2f - sideDif
        };

        int[] triangles = new int[] {
            0, 2, 1,
            2, 3, 1,
            2, 4, 3,
            4, 5, 3,
            4, 6, 5,
            6, 7, 5,
            6, 0, 7,
            0, 1, 7,
            5, 7, 1,
            5, 1, 3,
            0, 9, 8,
            0, 8, 2,
            4, 9, 6,
            4, 8, 9,
            0, 6, 9,
            8, 4, 2
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        return mesh;
    }
}
