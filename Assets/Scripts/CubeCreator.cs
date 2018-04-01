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
    /* The sizes of each face's edges */
    private float[][] edgeSizes;
    public float[] topEdgeSize = new float[4];
    public float[] bottomEdgeSize = new float[4];
    public float[] leftEdgeSize = new float[4];
    public float[] rightEdgeSize = new float[4];
    public float[] forwardEdgeSize = new float[4];
    public float[] backEdgeSize = new float[4];

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
    public Material thirdMaterial;

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

    /* Set to true to force the cube to re-create itself */
    public bool updateCube;


    /* -------- Built-in Unity Functions ---------------------------------------------------- */

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
            edgeSizes = new float[6][];
            edgeSizes[0] = rightEdgeSize;
            edgeSizes[1] = leftEdgeSize;
            edgeSizes[2] = topEdgeSize;
            edgeSizes[3] = bottomEdgeSize;
            edgeSizes[4] = forwardEdgeSize;
            edgeSizes[5] = backEdgeSize;
            UpdateBox();
            updateCube = false;
        }
    }


    /* -------- Main Update Functions ---------------------------------------------------- */

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
        Vector2[] UV, UV2;
        int[] brightTriangles = null, darkTriangles = null, edgeTriangles = null;

        /* Get the distance each vertex of the cube will be from it's center */
        L = x/2f;
        H = y/2f;
        W = z/2f;
        
        /* Get the vertices that make up the cube */
        vertices = new Vector3[48];
        /* Go through each face of the cube */
        for(int i = 0; i < 6; i++) {
            /* Go through each corner of the current face */
            for(int ii = 0; ii < 4; ii++) {
                /* Add the outter edge vertice */
                vertices[i*4 + ii] = GetBoxVertice(i, ii, false);

                /* Add the inner edge vertice */
                vertices[24 + i*4 + ii] = GetBoxVertice(i, ii, true);
            }
        }
        
        /* Set the vertices used by the cube */
        vertices = new Vector3[96];
        for(int i = 0; i < 6; i++) {
            for(int ii = 0; ii < 4; ii++) {
                /* Add the vertices used for the first and second material */
                vertices[i*4 + ii] = GetBoxVertice(i, ii, false);
                vertices[24 + i*4 + ii] = GetBoxVertice(i, ii, true);

                /* Add the vertices used for the third/edge material */
                vertices[48 + i*4 + ii] = GetBoxVertice(i, ii, false);
                vertices[72 + i*4 + ii] = GetBoxVertice(i, ii, true);
            }
        }
        
        /* Set the UVs of the cube */
        UV = new Vector2[96];
        UV2 = new Vector2[96];
        float[] xPos = new float[] { H, H, L, L, L, L };
        float[] yPos = new float[] { W, W, W, W, H, H };
        Vector2[] offsets = new Vector2[] { XPositiveOffset, XNegativeOffset, YPositiveOffset, YNegativeOffset, ZPositiveOffset, ZNegativeOffset };
        for(int i = 0; i < 6; i++) {
            for(int ii = 0; ii < 4; ii++) {
                /* UVs of the corners used by the first two materials */
                UV[i*4 + ii] = GetVerticeUVs(xPos[i], yPos[i], i, ii, offsets[i], false);
                UV[24 + i*4 + ii] = GetVerticeUVs(xPos[i], yPos[i], i, ii, offsets[i], true);
                
                /* UVs for the vertices used by the third material */
                UV[48 + i*4 + ii] = GetVerticeUVs(xPos[i], yPos[i], i, ii, offsets[i], false);
                UV[72 + i*4 + ii] = GetVerticeUVs(xPos[i], yPos[i], i, ii, offsets[i], true);

                /* Set the UVs that determine the fading value of the edges */
                UV2[0 + i*4 + ii] = new Vector2(0, 0);
                UV2[24 + i*4 + ii] = new Vector2(0, 0);
                UV2[48 + i*4 + ii] = new Vector2(1, 1);
                UV2[72 + i*4 + ii] = new Vector2(0, 0);
            }
        }

        /* Set up the polygons that form the cube */
        SquareFaceTriangles(ref brightTriangles, true);
        SquareFaceTriangles(ref darkTriangles, false);
        EdgeTriangles(ref edgeTriangles);

        /* Assign the mesh to the meshRenderer and update the box collider */
        cubeMesh.vertices = vertices;
        cubeMesh.uv = UV;
        cubeMesh.uv2 = UV2;
        cubeMesh.subMeshCount = 3;
        cubeMesh.SetTriangles(brightTriangles, 0);
        cubeMesh.SetTriangles(darkTriangles, 1);
        cubeMesh.SetTriangles(edgeTriangles, 2);
        cubeMesh.RecalculateNormals();
        InitializeComponents();
        GetComponent<MeshFilter>().mesh = cubeMesh;
        GetComponent<BoxCollider>().size = new Vector3(x, y, z);

        /* Only set the material if there are materials given */
        thirdMaterial.SetTexture("_MainTex", mainMaterial.GetTexture("_MainTex"));
        thirdMaterial.SetTexture("_SecondTex", secondMaterial.GetTexture("_MainTex"));
        GetComponent<MeshRenderer>().sharedMaterials = new Material[] { mainMaterial, secondMaterial, thirdMaterial };

        /* Update the values of the box */
        previousX = x;
        previousY = y;
        previousZ = z;
    }


    /* -------- Vertice/UV Setting Functions ---------------------------------------------------- */

    public Vector3 GetBoxVertice(int side, int vertex, bool inner) {
        /*
         * Return the vector3 that defines the given side and vertex.
         * Sides go from 0 - 5 : X+, X-, Y+, Y-, Z+, Z-
         */
        Vector3 vector;
        int X = 1, Y = 1, Z = 1;

        /* Set the size of the edge depending on the given side and vertex */
        float currentEdgeSize1 = 0;
        float currentEdgeSize2 = 0;
        if(vertex == 0) {
            currentEdgeSize1 = edgeSizes[side][0];
            currentEdgeSize2 = edgeSizes[side][2];
        }
        else if(vertex == 1) {
            currentEdgeSize1 = edgeSizes[side][0];
            currentEdgeSize2 = edgeSizes[side][3];
        }
        else if(vertex == 2) {
            currentEdgeSize1 = edgeSizes[side][1];
            currentEdgeSize2 = edgeSizes[side][2];
        }
        else if(vertex == 3) {
            currentEdgeSize1 = edgeSizes[side][1];
            currentEdgeSize2 = edgeSizes[side][3];
        }

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
            }
            else {
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

        /* Apply an offset to move the vertex to be on the inner edge of the face */
        if(inner) {
            Vector3 offset = Vector3.zero;
            if(side == 0 || side == 1) {
                offset = new Vector3(0, Mathf.Sign(vector.y)*-currentEdgeSize1, Mathf.Sign(vector.z)*-currentEdgeSize2);
            }
            else if(side == 2 || side == 3) {
                offset = new Vector3(Mathf.Sign(vector.x)*-currentEdgeSize1, 0, Mathf.Sign(vector.z)*-currentEdgeSize2);
            }
            else if(side == 4 || side == 5) {
                offset = new Vector3(Mathf.Sign(vector.x)*-currentEdgeSize1, Mathf.Sign(vector.y)*-currentEdgeSize2, 0);
            }
            vector += offset;
        }

        return vector;
    }
    
    public Vector2 GetVerticeUVs(float x, float y, int side, int verticeIndex, Vector2 offset, bool innerEdge) {
        /*
         * Return the UV of the given vertex
         */

        /* Set the size of the edge depending on the given side and vertex */
        float currentEdgeSize1 = 0;
        float currentEdgeSize2 = 0;
        if(innerEdge) {
            if(verticeIndex == 0) {
                currentEdgeSize1 = edgeSizes[side][0];
                currentEdgeSize2 = edgeSizes[side][2];
            }
            else if(verticeIndex == 1) {
                currentEdgeSize1 = edgeSizes[side][0];
                currentEdgeSize2 = edgeSizes[side][3];
            }
            else if(verticeIndex == 2) {
                currentEdgeSize1 = edgeSizes[side][1];
                currentEdgeSize2 = edgeSizes[side][2];
            }
            else if(verticeIndex == 3) {
                currentEdgeSize1 = edgeSizes[side][1];
                currentEdgeSize2 = edgeSizes[side][3];
            }
        }
        
        /* Adjust the position of the vertex depending on the vertexIndex used */
        if(verticeIndex % 2 != 0) {
            y *= -1;
        }
        if(verticeIndex > 1) {
            x *= -1;
        }

        /* Apply an offset to the UV if it's on the inner edge */
        y -= Mathf.Sign(y)*currentEdgeSize2;
        x -= Mathf.Sign(x)*currentEdgeSize1;

        return new Vector2(x, y) + offset;
    }


    /* -------- Triangle Setting Functions ---------------------------------------------------- */
    
    public void AddToTrianglesCenter(ref int[] triangles, ref int index, int surfaceIndex) {
        /*
         * Add the triangles used to render the inner square of the surface defined by surfaceIndex.
         */

        triangles[index++] = surfaceIndex;
        triangles[index++] = surfaceIndex + 1;
        triangles[index++] = surfaceIndex + 2;
        triangles[index++] = surfaceIndex + 1;
        triangles[index++] = surfaceIndex + 3;
        triangles[index++] = surfaceIndex + 2;
    }
    
    public void SquareFaceTriangles(ref int[] triangles, bool ignoreEdges) {
        /*
         * Create the array and add the triangles needed to form the desired surface. If ignoreEdges
         * is false, the face's center square is used, which is effected by the face's edge sizes.
         * If ignoreEdges is true, then the edge sizes are ignored and the triangles use the entire face
         */
        int surfaceOffset = 0;
        if(ignoreEdges) { surfaceOffset = 24; }

        /* Get how many faces this material will use */
        int faceCount = 0;
        if(ignoreEdges ^ forward) {
            faceCount++;
        }
        if(ignoreEdges ^ backward) {
            faceCount++;
        }
        if(ignoreEdges ^ top) {
            faceCount++;
        }
        if(ignoreEdges ^ bottom) {
            faceCount++;
        }
        if(ignoreEdges ^ left) {
            faceCount++;
        }
        if(ignoreEdges ^ right) {
            faceCount++;
        }

        /* Populate the triangles array */
        triangles = new int[faceCount*6];
        int index = 0;
        if(ignoreEdges ^ right) {
            AddToTrianglesCenter(ref triangles, ref index, 0 + surfaceOffset);
        }
        if(ignoreEdges ^ left) {
            AddToTrianglesCenter(ref triangles, ref index, 4 + surfaceOffset);
        }
        if(ignoreEdges ^ top) {
            AddToTrianglesCenter(ref triangles, ref index, 8 + surfaceOffset);
        }
        if(ignoreEdges ^ bottom) {
            AddToTrianglesCenter(ref triangles, ref index, 12 + surfaceOffset);
        }
        if(ignoreEdges ^ forward) {
            AddToTrianglesCenter(ref triangles, ref index, 16 + surfaceOffset);
        }
        if(ignoreEdges ^ backward) {
            AddToTrianglesCenter(ref triangles, ref index, 20 + surfaceOffset);
        }
    }

    public void EdgeTriangles(ref int[] triangles) {
        /*
         * Set the triangles used for the edges of a face
         */
        int faceCount = 0;
        if(!forward) {
            faceCount++;
        }
        if(!backward) {
            faceCount++;
        }
        if(!top) {
            faceCount++;
        }
        if(!bottom) {
            faceCount++;
        }
        if(!left) {
            faceCount++;
        }
        if(!right) {
            faceCount++;
        }

        /* Populate the triangles array */
        triangles = new int[faceCount*24];
        int index = 0;
        if(!right) {
            AddToTrianglesOutter(ref triangles, ref index, 48 + 0);
        }
        if(!left) {
            AddToTrianglesOutter(ref triangles, ref index, 48 + 4);
        }
        if(!top) {
            AddToTrianglesOutter(ref triangles, ref index, 48 + 8);
        }
        if(!bottom) {
            AddToTrianglesOutter(ref triangles, ref index, 48 + 12);
        }
        if(!forward) {
            AddToTrianglesOutter(ref triangles, ref index, 48 + 16);
        }
        if(!backward) {
            AddToTrianglesOutter(ref triangles, ref index, 48 + 20);
        }
    }

    public void AddToTrianglesOutter(ref int[] triangles, ref int index, int surfaceIndex) {
        /*
         * Add the triangles used to render the outter edges of the surface defined by surfaceIndex
         */

        triangles[index++] = surfaceIndex+0 + 24;
        triangles[index++] = surfaceIndex+2 + 24;
        triangles[index++] = surfaceIndex+2;
        triangles[index++] = surfaceIndex+2;
        triangles[index++] = surfaceIndex+0;
        triangles[index++] = surfaceIndex+0 + 24;

        triangles[index++] = surfaceIndex+1 + 24;
        triangles[index++] = surfaceIndex+0 + 24;
        triangles[index++] = surfaceIndex+0;
        triangles[index++] = surfaceIndex+0;
        triangles[index++] = surfaceIndex+1;
        triangles[index++] = surfaceIndex+1 + 24;

        triangles[index++] = surfaceIndex+2 + 24;
        triangles[index++] = surfaceIndex+3 + 24;
        triangles[index++] = surfaceIndex+3;
        triangles[index++] = surfaceIndex+3;
        triangles[index++] = surfaceIndex+2;
        triangles[index++] = surfaceIndex+2 + 24;

        triangles[index++] = surfaceIndex+3 + 24;
        triangles[index++] = surfaceIndex+1 + 24;
        triangles[index++] = surfaceIndex+1;
        triangles[index++] = surfaceIndex+1;
        triangles[index++] = surfaceIndex+3;
        triangles[index++] = surfaceIndex+3+ 24;
    }
}