using UnityEngine;
using System.Collections;

public class CubeRend : MonoBehaviour {

    public Camera cam1;
    public Camera cam2;
    public MeshRenderer mesh1;
    public MeshRenderer mesh2;

    public Material mat1;
    public Material mat2;
    public Material cam1ViewMat;
    public Material cam2ViewMat;
    public Material cam3ViewMat;

    public RenderTexture rend1;
    public RenderTexture rend2;

    void Start() {
        /*
         * default material to be used
         */

        mat1 = new Material(Shader.Find("Standard"));
        mat2 = new Material(Shader.Find("Standard"));

        rend1 = new RenderTexture(512, 512, 16);
        rend2 = new RenderTexture(512, 512, 16);

        cam1.targetTexture = rend1;
        cam2.targetTexture = rend2;
    }

	void OnWillRenderObject() {
        /*
         * When this object is gonna be rendered, tell us what camera will render it
         */
        Camera cam = Camera.current;

        //Debug.Log(cam.name);

        /* Set the cube's material to the proper material when when its about to be rendered */
        if(cam.name == "SceneCamera") {
            GetComponent<MeshRenderer>().material = cam3ViewMat;
        }
        else if(cam.name == "Camera1") {
            GetComponent<MeshRenderer>().material = cam1ViewMat;
        }
        else if(cam.name == "Camera2") {
            GetComponent<MeshRenderer>().material = cam2ViewMat;
        }





    }

    void Update() {
        /*
         * Every frame, re-render the camera's views
         */

        
        cam1.Render();
        //Once the cam1 is rendered, apply it's targetTexture to it's mesh
        mat1.mainTexture = cam1.targetTexture;
        mesh1.material = mat1;




        //Before rendering the camera, change the cube's texture/material to the proper cam1Mat
        cam2.Render();
        //Once the cam2 is rendered, apply it's targetTexture to it's mesh
        mat2.mainTexture = cam2.targetTexture;
        mesh2.material = mat2;
    }
}
