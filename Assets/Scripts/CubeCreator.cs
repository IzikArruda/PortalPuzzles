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

    /* The size of the edge of the cube */
    public float edgeSize;

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

    /* values used when creating the cube */
    private float L;
    private float H;
    private float W;

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

        updateCube = true;
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









    public Vector3 GetBoxVertice(int side, int vertex) {
        /*
         * Return the vector3 that defines the given side and vertex.
         * Sides go from 0 - 5 : X+, X-, Y+, Y-, Z+, Z-
         */
        Vector3 vector;
        int X = 1, Y = 1, Z = 1;

        /* X */
        if(side == 0 || side == 1) {
            if(vertex % 2 != 0) {
                Z *= -1;
            }
            if(vertex > 1) {
                Y *= -1;
            }
            if(side == 0) {
                Y *= -1;
            }else {
                X *= -1;
            }
        }

        /* Y */
        if(side == 2 || side == 3) {
            if(vertex % 2 == 0) {
                Z *= -1;
            }
            if(vertex > 1) {
                X *= -1;
            }
            if(side == 2) {
                Z *= -1;
            }else {
                Y *= -1;
            }
        }

        /* Z */
        if(side == 4 || side == 5) {
            if(vertex % 2 != 0) {
                Y *= -1;
            }
            if(vertex > 1) {
                X *= -1;
            }
            if(side == 4) {
                X *= -1;
            }
            else {
                Z *= -1;
            }
        }
        

        vector = new Vector3(L*X, H*Y, W*Z);

        return vector;
    }
    

    public void UpdateBox() {
        /*
         * Create the mesh of the cube using it's set parameters.
         */
        Mesh cubeMesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UV;
        int[] triangles = null, altTriangles = null;

        /* Get the distance each vertex of the cube will be from it's center */
        L = x/2f;
        H = y/2f;
        W = z/2f;

        /* Get the vertices that make up the cube */
        vertices = new Vector3[] {
            //X+ plane
            GetBoxVertice(0, 0),
            GetBoxVertice(0, 1),
            GetBoxVertice(0, 2),
            GetBoxVertice(0, 3),
            //X- plane
            GetBoxVertice(1, 0),
            GetBoxVertice(1, 1),
            GetBoxVertice(1, 2),
            GetBoxVertice(1, 3),
            //Y+ plane
            GetBoxVertice(2, 0),
            GetBoxVertice(2, 1),
            GetBoxVertice(2, 2),
            GetBoxVertice(2, 3),
            //Y- plane
            GetBoxVertice(3, 0),
            GetBoxVertice(3, 1),
            GetBoxVertice(3, 2),
            GetBoxVertice(3, 3),
            //Z+ plane
            GetBoxVertice(4, 0),
            GetBoxVertice(4, 1),
            GetBoxVertice(4, 2),
            GetBoxVertice(4, 3),
            //Z- plane
            GetBoxVertice(5, 0),
            GetBoxVertice(5, 1),
            GetBoxVertice(5, 2),
            GetBoxVertice(5, 3),

            /* Place the center vectors of the top of the box */
            /*new Vector3(L - edgeSize, H, -W + edgeSize),
            new Vector3(L - edgeSize, H, W - edgeSize),
            new Vector3(-L + edgeSize, H, -W + edgeSize),
            new Vector3(-L + edgeSize, H, W - edgeSize)*/
        };

        /* Set up the polygons that form the cube */
        /*CreateTriangles(ref triangles, true);
        if(usesSecondMat) {
            CreateTriangles(ref altTriangles, false);
        }*/
        //Only create the triangles so it can test the edges
        altTriangles = new int[] {
            //How it was rendered before
            //10, 9, 8, 10, 11, 9
            //How the center is rendered
            //10+16, 9+16, 8+16, 10+16, 11+16, 9+16,

            //Render the edges of the surface
            /*10, 11, 11+16, 11+16, 10+16, 10,
            11, 9, 9+16, 9+16, 11+16, 11,
            9, 8, 8+16, 8+16, 9+16, 9,
            8, 10, 10+16, 10+16, 8+16, 8*/



            //Render the interior side of the surface
            //25, 26, 27, 25, 24, 27,
            //Render the top side of the triangle using the new vertices
            //10, 9, 8
        };
        triangles = new int[] {
            //Render the center
            10+16, 9+16, 8+16, 10+16, 11+16, 9+16,
        };
        CreateTriangles(ref triangles, true);

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

            /* Set the UVs of the top part of the box */
            /*new Vector2(W - edgeSize, L - edgeSize) + YPositiveOffset,
            new Vector2(-W + edgeSize, L - edgeSize) + YPositiveOffset,
            new Vector2(W - edgeSize, -L + edgeSize) + YPositiveOffset,
            new Vector2(-W + edgeSize, -L + edgeSize) + YPositiveOffset*/
        };
        
        /* Assign the mesh to the meshRenderer and update the box collider */
        cubeMesh.vertices = vertices;
        cubeMesh.uv = UV;
        cubeMesh.subMeshCount = 2;
        cubeMesh.SetTriangles(triangles, 0);
        cubeMesh.SetTriangles(altTriangles, 1);
        cubeMesh.RecalculateNormals();
        InitializeComponents();
        GetComponent<MeshFilter>().mesh = cubeMesh;
        GetComponent<BoxCollider>().size = new Vector3(x, y, z);

        /* Only set the material if there are materials given */
        GetComponent<MeshRenderer>().sharedMaterials = new Material[] { mainMaterial, secondMaterial };


        /* Set the two materials to the mesh */
        /*cubeMesh.subMeshCount = 2;
        cubeMesh.SetTriangles(triangles, 0);
        cubeMesh.SetTriangles(altTriangles, 1);
        GetComponent<MeshRenderer>().sharedMaterials = new Material[] { mainMaterial, secondMaterial };


        /* Set the mesh's components */
        /*cubeMesh.vertices = vertices;
        cubeMesh.uv = UV;
        cubeMesh.RecalculateNormals();
        InitializeComponents();
        GetComponent<MeshFilter>().mesh = cubeMesh;
        GetComponent<BoxCollider>().size = new Vector3(x, y, z);*/




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
            AddToTriangles(ref triangles, ref index, 0, 1, 2, 1, 3, 2);
        }
        if(firstMaterial ^ left) {
            AddToTriangles(ref triangles, ref index, 4, 5, 6, 5, 7, 6);
        }
        if(firstMaterial ^ top) {
            AddToTriangles(ref triangles, ref index, 8, 9, 10, 9, 11, 10);
        }
        if(firstMaterial ^ bottom) {
            AddToTriangles(ref triangles, ref index, 12, 13, 14, 13, 15, 14);
        }
        if(firstMaterial ^ forward) {
            AddToTriangles(ref triangles, ref index, 16, 17, 18, 17, 19, 18);
        }
        if(firstMaterial ^ backward) {
            AddToTriangles(ref triangles, ref index, 20, 21, 22, 21, 23, 22);
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