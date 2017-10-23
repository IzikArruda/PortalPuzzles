﻿using UnityEngine;
using System.Collections;

/*
 * Use the given values to create a cube
 */
[ExecuteInEditMode]
public class CubeCreator : MonoBehaviour {

    public float width;
    public float height;
    public float length;

    /* Offset the position of the UV coordinates on the given face of the cube */
    public Vector2 XPositiveOffset;
    public Vector2 XNegativeOffset;
    public Vector2 YPositiveOffset;
    public Vector2 YNegativeOffset;
    public Vector2 ZPositiveOffset;
    public Vector2 ZNegativeOffset;


    void Update () {
        /*
         * Create the mesh of the cube using it's set parameters
         */
        Mesh cubeMesh = new Mesh();
        Vector3[] vertices;
        Vector2[] UV;
        int[] triangles;

        /* Get the distance each vertex of the cube will be from it's center */
        float L = length/2f;
        float H = height/2f;
        float W = width/2f;

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
        triangles = new int[] {
            //X+ plane
            2, 1, 0, 2, 3, 1,
            //X- plane
            4, 5, 6, 5, 7, 6,
            //Y+ plane
            10, 9, 8, 10, 11, 9,
            //Y- plane
            12, 13, 14, 13, 15, 14,
            //Z+ plane
            16, 17, 18, 17, 19, 18,
            //Z- plane
            22, 21, 20, 22, 23, 21
        };

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
        cubeMesh.triangles = triangles;
        cubeMesh.uv = UV;
        cubeMesh.RecalculateNormals();
        GetComponent<MeshFilter>().mesh = cubeMesh;
        GetComponent<BoxCollider>().size = new Vector3(length, height, width);
    }
}