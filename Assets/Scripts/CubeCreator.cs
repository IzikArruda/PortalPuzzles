using UnityEngine;
using System.Collections;

/*
 * Use the given values to create a cube
 */
[ExecuteInEditMode]
public class CubeCreator : MonoBehaviour {

    /* The desired sizes of the cube */
    public float x;
    public float y;
    public float z;
    /* The last saved sizes of the cube */
    private float previousX;
    private float previousY;
    private float previousZ;

    /* Offset the position of the UV coordinates on the given face of the cube */
    public Vector2 XPositiveOffset;
    public Vector2 XNegativeOffset;
    public Vector2 YPositiveOffset;
    public Vector2 YNegativeOffset;
    public Vector2 ZPositiveOffset;
    public Vector2 ZNegativeOffset;
    public Vector2 UVScale;

    /* The materials used by the cube */
    public Material mainMaterial;
    public Material secondMaterial;

    /* If a second material is available, these booleans will set the given face to use the second material */
    public bool top;
    public bool bottom;
    public bool forward;
    public bool backward;
    public bool left;
    public bool right;

    public bool updateCube;

    void OnValidate() {
        /*
         * Update the box if any values in the script change
         */

        updateCube = true;
    }

    void Start() {
        /*
         * Ensure the object that has this script has the required components
         */

        InitializeComponents();
    }

    void Update() {
        /*
         * On every frame, check if there is a change with the box's size
         */
         
        if(updateCube) {
            UpdateBox();
            updateCube = false;
        }
    }

    void InitializeComponents() {
        /*
         * Reset the required components
         */

        if(GetComponent<MeshFilter>() != null) { DestroyImmediate(GetComponent<MeshFilter>()); }
        if(GetComponent<MeshRenderer>() != null) { DestroyImmediate(GetComponent<MeshRenderer>()); }
        if(GetComponent<BoxCollider>() != null) { DestroyImmediate(GetComponent<BoxCollider>()); }
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<BoxCollider>();
    }

    public void UpdateBox() {
        /*
         * Create the mesh of the cube using it's set parameters.
         */
        Mesh cubeMesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UV;
        int[] triangles = null, altTriangles = null;

        /* Check if the cube is going to use the second material */
        bool usesSecondMat = false;
        if(secondMaterial != null && (top || bottom || forward || backward || left || right)) {
            usesSecondMat = true;
        }

        /* Get the distance each vertex of the cube will be from it's center */
        float L = x/2f;
        float H = y/2f;
        float W = z/2f;

        /* Get the vertices that make up the cube */
        vertices = new Vector3[] {
            //X+ plane
            new Vector3(L, H, W),
            new Vector3(L, H, -W),
            new Vector3(L, -H, W),
            new Vector3(L, -H, -W),
            //X- plane
            new Vector3(-L, H, W),
            new Vector3(-L, H, -W),
            new Vector3(-L, -H, W),
            new Vector3(-L, -H, -W),
            //Y+ plane
            new Vector3(L, H, -W),
            new Vector3(L, H, W),
            new Vector3(-L, H, -W),
            new Vector3(-L, H, W),
            //Y- plane
            new Vector3(L, -H, -W),
            new Vector3(L, -H, W),
            new Vector3(-L, -H, -W),
            new Vector3(-L, -H, W),
            //Z+ plane
            new Vector3(L, H, W),
            new Vector3(-L, H, W),
            new Vector3(L, -H, W),
            new Vector3(-L, -H, W),
            //Z- plane
            new Vector3(L, H, -W),
            new Vector3(-L, H, -W),
            new Vector3(L, -H, -W),
            new Vector3(-L, -H, -W)
        };

        /* Set up the polygons that form the cube */
        CreateTriangles(ref triangles, true);
        if(usesSecondMat) {
            CreateTriangles(ref altTriangles, false);
        }
        
        /* Apply an offset to the UVs */
        if(UVScale != null && (UVScale.x != 0 && UVScale.y != 0)) {
            L = UVScale.y;
            H = UVScale.x;
            ZNegativeOffset = new Vector2(L, H);
        }

        /* Set the UVs of the cube */
        UV = new Vector2[] {
            //X+ plane
            new Vector2(-H, -W) + XPositiveOffset,
            new Vector2(-H, +W) + XPositiveOffset,
            new Vector2(+H, -W) + XPositiveOffset,
            new Vector2(+H, +W) + XPositiveOffset,
            //X- plane
            new Vector2(-H, -W) + XNegativeOffset,
            new Vector2(-H, W) + XNegativeOffset,
            new Vector2(H, -W) + XNegativeOffset,
            new Vector2(H, W) + XNegativeOffset,
            //Y+ plane
            new Vector2(W, L) + YPositiveOffset,
            new Vector2(-W, L) + YPositiveOffset,
            new Vector2(W, -L) + YPositiveOffset,
            new Vector2(-W, -L) + YPositiveOffset,
            //Y- plane
            new Vector2(-L, -W) + YNegativeOffset,
            new Vector2(-L, W) + YNegativeOffset,
            new Vector2(L, -W) + YNegativeOffset,
            new Vector2(L, W) + YNegativeOffset,
            //Z+ plane
            new Vector2(-H, -L) + ZPositiveOffset,
            new Vector2(-H, L) + ZPositiveOffset,
            new Vector2(H, -L) + ZPositiveOffset,
            new Vector2(H, L) + ZPositiveOffset,
            //Z- plane
            new Vector2(L, H) + ZNegativeOffset,
            new Vector2(-L, H) + ZNegativeOffset,
            new Vector2(L, -H) + ZNegativeOffset,
            new Vector2(-L, -H) + ZNegativeOffset
        };
        
        /* Assign the mesh to the meshRenderer and update the box collider */
        cubeMesh.vertices = vertices;
        cubeMesh.uv = UV;
        if(usesSecondMat) {
            cubeMesh.subMeshCount = 2;
            cubeMesh.SetTriangles(triangles, 0);
            cubeMesh.SetTriangles(altTriangles, 1);
        }
        else {
            cubeMesh.subMeshCount = 1;
            cubeMesh.triangles = triangles;
        }
        cubeMesh.RecalculateNormals();
        InitializeComponents();
        GetComponent<MeshFilter>().mesh = cubeMesh;
        GetComponent<BoxCollider>().size = new Vector3(x, y, z);

        /* Only set the material if there are materials given */
        if(mainMaterial != null && secondMaterial != null && usesSecondMat) {
            GetComponent<MeshRenderer>().sharedMaterials = new Material[] { mainMaterial, secondMaterial };
        }
        else if(usesSecondMat != true && mainMaterial != null) {
            GetComponent<MeshRenderer>().sharedMaterial = mainMaterial;
        }

        /* Update the values of the box */
        previousX = x;
        previousY = y;
        previousZ = z;
    }

    public void CreateTriangles(ref int[] triangles, bool firstMaterial) {
        /*
         * Create the triangles which are used by only one of the two materials.
         */

        /* Get how many faces this material will use */
        int faceCount = 0;
        if(forward) {
            faceCount++;
        }
        if(backward) {
            faceCount++;
        }
        if(top) {
            faceCount++;
        }
        if(bottom) {
            faceCount++;
        }
        if(left) {
            faceCount++;
        }
        if(right) {
            faceCount++;
        }
        if(firstMaterial) {
            faceCount = 8 - faceCount;
        }
        faceCount = 8;

        /* Populate the triangles array */
        triangles = new int[faceCount*6];
        int index = 0;
        if(firstMaterial ^ right) {
            AddToTriangles(ref triangles, ref index, 2, 1, 0, 2, 3, 1);
        }
        if(firstMaterial ^ left) {
            AddToTriangles(ref triangles, ref index, 4, 5, 6, 5, 7, 6);
        }
        if(firstMaterial ^ top) {
            AddToTriangles(ref triangles, ref index, 10, 9, 8, 10, 11, 9);
        }
        if(firstMaterial ^ bottom) {
            AddToTriangles(ref triangles, ref index, 12, 13, 14, 13, 15, 14);
        }
        if(firstMaterial ^ forward) {
            AddToTriangles(ref triangles, ref index, 16, 17, 18, 17, 19, 18);
        }
        if(firstMaterial ^ backward) {
            AddToTriangles(ref triangles, ref index, 22, 21, 20, 22, 23, 21);
        }
    }

    public void AddToTriangles(ref int[] triangles, ref int index, int i1, int i2, int i3, int i4, int i5, int i6) {
        /*
         * Add the 6 given ints to the triangle from the given index
         */

        triangles[index++] = i1;
        triangles[index++] = i2;
        triangles[index++] = i3;
        triangles[index++] = i4;
        triangles[index++] = i5;
        triangles[index++] = i6;
    }
}