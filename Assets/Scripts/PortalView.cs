/*
 * Thanks to Aras Pranckevicius' MirrorReflection4
 * http://wiki.unity3d.com/index.php/MirrorReflection4 
 * 
 * To prevent portal overlap when handling duel sided portals (and recursive calls), Set all portal
 * mesh objects to be on the "Portal Mesh" layer and have all portalMesh scount camera's 
 * avoid rendering anything in the "Portal Mesh" layer. 
 * 
 * Ideally, we would only refuse to render the portal that is currently occupying the same space.
 * It will remain this way until I find a way for a camera to not render specific objects instead
 * of an entire layer to allow other portals to render other portalMeshes.
 */

using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class PortalView : MonoBehaviour {

    /* A point positioned at the partner portal's location */
    public Transform pointB;

    /* The child camera of this portal */
    public Camera scoutCamera;
    public Camera InternalCamera;

    /* The forward vector of the portal */
    public Vector3 faceNormal = Vector3.forward; 

    /* Quality of the texture rendered and applied to the portal mesh. 2048 is recommended. */
	public int m_TextureSize;
    private int m_OldPortalTextureSize = 0;

    /* Offset to the camera's clipping plane when rendering. 0 is recommended */
    public float m_ClipPlaneOffset;

    /* The renderTexture produced by the camera */
    private RenderTexture m_PortalTexture = null;
    private RenderTexture m_PortalTexture2 = null;

    /* Whether the portal is currently visible and being drawn to a camera */
    public bool beingDrawn = true;
        
    /* Materials used on the portalMesh */
    private Material portalMaterial;
    public Material invisibleMaterial;

    /* Static value used to track recursive portal rendering calls */
    private static bool s_InsideRendering = false;

    /* The ID of the portalSet that this portalMesh is a child of */
    public string portalSetID;
    private int count = 0;


    public Material newMat;

    public Material testMat;

    //private static ArrayList meshesToBeRendered;
    public Material MatFromSceneCam;
    public Material MatFromScoutCam;

    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {
        /*
         * Ensure the portal has a proper material 
         */


        if(!portalMaterial) {
            portalMaterial = new Material(Shader.Find("Unlit/Portal"));
            portalMaterial.name = "__PortalMaterial" + GetInstanceID();
            GetComponent<MeshRenderer>().material = portalMaterial;
        }

        //Set up a postRenderScript for the scout camera
        //ON PRE RENDER??????
        //Camera.onPreRender += MyPreRenderFunction;
        //Camera.onPostRender += MyPostRenderFunction;
        

        /* Ensure the arrayList is created */
        //if(meshesToBeRendered == null) {
        //    meshesToBeRendered = new ArrayList();
        //}
    }
    
    public void MyPreRenderFunction(Camera cam) {

        if(cam.name == "SceneCamera") {
            Debug.Log("pre-Drawing scenecam | " + name);
        }

    }
    

    public void MyPostRenderFunction(Camera cam) {
        string camName = ExtractPortalSetID(cam.name);

        /*
         * Once the sceneCam finished rendering this mesh, set it back to it's original mesh
         */
        if(cam.name == "SceneCamera") {
            Debug.Log("post-Drawing scenecam | " + name);
        }
    }
 
    public void LateUpdate() {
        /*
         * Reset the material for each portal before the rendering starts
         */
        //if(meshesToBeRendered.Count > 0) {
            //Debug.Log(meshesToBeRendered.Count);
        //    meshesToBeRendered.Clear();
        //}




        //THIS IS HOW ITS GOING TO GO.
        //EVERY FRAME STARTS WITH ALL PORTALS SETTING TO THE NEWMAT.
        //WHEN THEY GET RENDERED BY THE SCENECAM, CHANGE THEIR MESH TO X, RENDER THE SCENE, THEN SWITCH THE MESH BACK TO NEWMAT.

        /*
         * Reset the material for the portalMEsh On every frame update to start fresh
         */
        //GetComponent<MeshRenderer>().material = null;
    }



    public void OnRenderObject() {
        Camera cam = Camera.current;
        string camName = ExtractPortalSetID(cam.name);

        /* If the mesh was just rendered by a portalCamera, set it's mesh back to nothing */
        if(camName != null) {
            //GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));
        }
    }

    public void OnWillRenderObject() {
        /*
         * ITS ALL GONNA BE DONE HERE
         */

        /* Get the camera rendering this portal */
        Camera cam = Camera.current;
        string camName = ExtractPortalSetID(cam.name);

        /*
         * If this mesh is going to be rendered by the sceneCam, change it's material 
         */
        if(cam.name == "SceneCamera") {
            if(checkPortalVisibility(cam.transform)) {
                //Debug.Log(name + " about to be drawn");
                OnWillRenderObject123(cam);
                //Texture extractedTexture = GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_PortalTex");
                //cam.GetComponent<CameraScript>().AssignMeshTo(gameObject, extractedTexture);
                //GetComponent<MeshRenderer>().material = portalMaterial;
                //TheSceneMat = mat;
            }
        }


        /*
         * If this mesh is going to be rendered by a scoutCamera, change it's material 
         * If there is a portalMesh that will be rendered by a scoutCamera, 
         * tell the scoutCamera to change it's material to be null
         */
        if(camName != null && cam.GetComponent<CameraScript>()) {
            if(checkPortalVisibility(cam.transform)) {
                //Debug.Log(name + " |");
                Texture interportalTex = newMat.mainTexture;
                /*
                 * Use a different OnWillRenderObject123 function that, instead of setting the texture to the mesh,
                 * EXTRACTS the texture and returns it
                 */
                OnWillRenderObject123RETURNINSTEAD(cam, ref interportalTex);
                cam.GetComponent<CameraScript>().AssignMeshTo(gameObject, interportalTex);
            }
        }


        if(cam.name == "InternalCamera") {
            Debug.Log(cam.transform.parent.name + " an InternalCamera is drawing " + name);
            //Apply the internalCamera's rendTex to the thing
        }


        /*
         * 1: Send request to render scene
         * 2: This request will send a OnWillRenderObject() call to each mesh that will be rendered
         * 3: For now, Each rendered mesh will update their mesh by rendering their scoutCamera.
         *      This will however call the OnWillRenderObject() once more
         */
    }











    public void OnWillRenderObject123(Camera cam) {
        /*
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
         * 
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the player's position then render the image of that camera.
         */
        count++;

        /* Check if key gameObjects are active */
        if(!enabled || !scoutCamera || !pointB)
            return;

        /* Get the camera rendering this portal */
        //Camera cam = Camera.current;
        if(!cam)
            return;

        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;


        /* Extract the portalID from the camera rendering this portal if it is a porltaMesh's scount camera */
        int idEndIndex = cam.name.IndexOf("|");
        string camPortalSetID = "";
        if(idEndIndex >= 0) {
            camPortalSetID = cam.name.Substring(0, idEndIndex);
        }
        else {
            /* The camera being used is not a scount camera */
            camPortalSetID = null;
        }

        /* Set the material of the portal whether it is visible or not */
        if(checkPortalVisibility(cam.transform)) {
            SetMaterial(true);
        }
        else {
            SetMaterial(false);
            return;
        }

        /* Prevent recursive rendering */
        if(s_InsideRendering) {
            //return;
        }
        s_InsideRendering = true;









        /* Create the render texture and the portal material if they have not yet been created */
        CreateNeededObjects();

        /* Set up values to properly render the camera */
        Vector3 pos = transform.position;
        Vector3 normal = transform.TransformDirection(faceNormal);

        // this will make it depend on the points' position, rotation, and scale
        scoutCamera.transform.position = pointB.TransformPoint(transform.InverseTransformPoint(cam.transform.position));
        scoutCamera.transform.rotation = Quaternion.LookRotation(
                pointB.TransformDirection(transform.InverseTransformDirection(cam.transform.forward)),
                pointB.TransformDirection(transform.InverseTransformDirection(cam.transform.up)));

        // I don't know how this works it just does, I got lucky
        Vector4 clipPlane = CameraSpacePlane(cam, pos, normal, -1.0f);
        Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
        scoutCamera.projectionMatrix = projection;

        /* Apply the render texture to any rendered material using the proper portal material/shader */
        Material[] materials = rend.sharedMaterials;
        foreach(Material mat in materials) {
            if(mat.HasProperty("_PortalTex")) {
                mat.SetTexture("_PortalTex", m_PortalTexture);
            }
        }



        /////////////////
        ////////////////Get the render order, ie when a Render() is called, does it fully render that cam before continuing?
        ////////////////Does it add it to the list, do the precheck, THEN continue this call?
        //It has to go in reverse order - First the internal portal texture gets rendered, then the sceen cam


        /* Do not render this portal's view if it's being rendered by another portal's scout camera */
        /*if(camPortalSetID == null) {
            //Depending on the camera, change the material of this mesh
            if(cam.name == "SceneCamera") {
                //Debug.Log("using scene");
                scoutCamera.Render();
            }
            if(cam.name == "CameraTest") {
                //Debug.Log("using test");
                GetComponent<MeshRenderer>().material = newMat;
            }
        }*/

        /*
         * If the mesh is being rendered by the SceneCamera, do the normal portalView
         */
        if(cam.name == "SceneCamera") {
            //scoutCamera.Render();
        }

        /*
         * If the mesh is being rendered by another scoutCamera, set it's material to be the carpet
         */
        if(camPortalSetID != null) {
            //GetComponent<MeshRenderer>().material = newMat;
        }



        //Debug.Log("Send Req for scout Render");
        scoutCamera.Render();
        //Debug.Log("Done scount Render");




        /* Release the lock */
        s_InsideRendering = false;
    }



    public void OnWillRenderObject123RETURNINSTEAD(Camera cam, ref Texture thefinalTexture) {
        /*
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
         * 
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the player's position then render the image of that camera.
         */
        count++;

        /* Check if key gameObjects are active */
        if(!enabled || !scoutCamera || !pointB)
            return;

        /* Get the camera rendering this portal */
        //Camera cam = Camera.current;
        if(!cam)
            return;

        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;


        /* Extract the portalID from the camera rendering this portal if it is a porltaMesh's scount camera */
        int idEndIndex = cam.name.IndexOf("|");
        string camPortalSetID = "";
        if(idEndIndex >= 0) {
            camPortalSetID = cam.name.Substring(0, idEndIndex);
        }
        else {
            /* The camera being used is not a scount camera */
            camPortalSetID = null;
        }

        /* Set the material of the portal whether it is visible or not */
        if(checkPortalVisibility(cam.transform)) {
            SetMaterial(true);
        }
        else {
            SetMaterial(false);
            return;
        }

        /* Prevent recursive rendering */
        if(s_InsideRendering) {
            //return;
        }
        s_InsideRendering = true;









        /* Create the render texture and the portal material if they have not yet been created */
        CreateNeededObjects();

        /* Set up values to properly render the camera */
        Vector3 pos = transform.position;
        Vector3 normal = transform.TransformDirection(faceNormal);

        // this will make it depend on the points' position, rotation, and scale
        InternalCamera.transform.position = pointB.TransformPoint(transform.InverseTransformPoint(cam.transform.position));
        InternalCamera.transform.rotation = Quaternion.LookRotation(
                pointB.TransformDirection(transform.InverseTransformDirection(cam.transform.forward)),
                pointB.TransformDirection(transform.InverseTransformDirection(cam.transform.up)));

        // I don't know how this works it just does, I got lucky
        Vector4 clipPlane = CameraSpacePlane(cam, pos, normal, -1.0f);
        Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
        InternalCamera.projectionMatrix = projection;

        /* Apply the render texture to any rendered material using the proper portal material/shader */
        Material[] materials = rend.sharedMaterials;
        foreach(Material mat in materials) {
            if(mat.HasProperty("_PortalTex")) {
                //mat.SetTexture("_PortalTex", m_PortalTexture);
                thefinalTexture = mat.GetTexture("_PortalTex");
                thefinalTexture = null;
            }
        }



        //Debug.Log("Send Req for scout Render");
        //Debug.Log("Send RENDER REQ");
        Texture internalCamsRend = m_PortalTexture2;
        thefinalTexture = internalCamsRend;
        InternalCamera.Render();
        //Debug.Log("done RENDER REQ");
        //Debug.Log("Done scount Render");




        /* Release the lock */
        s_InsideRendering = false;
    }


















    // Aras Pranckevicius MirrorReflection4
    // http://wiki.unity3d.com/index.php/MirrorReflection4 
    // Cleanup all the objects we possibly have created
    void OnDisable() {

		if( m_PortalTexture ) {
			DestroyImmediate( m_PortalTexture );
			m_PortalTexture = null;
		}
	}


    /* -------- Event Functions ---------------------------------------------------- */

    // Aras Pranckevicius MirrorReflection4
    // http://wiki.unity3d.com/index.php/MirrorReflection4 
    // On-demand create any objects we need
    private void CreateNeededObjects() {

        // Reflection render texture
        if(!m_PortalTexture || m_OldPortalTextureSize != m_TextureSize) {
            if(m_PortalTexture)
                DestroyImmediate(m_PortalTexture);

            m_PortalTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);
            m_PortalTexture.name = "__PortalRenderTexture" + GetInstanceID();
            m_PortalTexture.isPowerOfTwo = true;
            m_PortalTexture.hideFlags = HideFlags.DontSave;
            m_OldPortalTextureSize = m_TextureSize;

            scoutCamera.targetTexture = m_PortalTexture;
        }

        // Reflection render texture
        if(!m_PortalTexture2 || m_OldPortalTextureSize != m_TextureSize) {
            if(m_PortalTexture2)
                DestroyImmediate(m_PortalTexture2);

            m_PortalTexture2 = new RenderTexture(m_TextureSize, m_TextureSize, 16);
            m_PortalTexture2.name = "__PortalRenderTexture" + GetInstanceID();
            m_PortalTexture2.isPowerOfTwo = true;
            m_PortalTexture2.hideFlags = HideFlags.DontSave;
            m_OldPortalTextureSize = m_TextureSize;

            InternalCamera.targetTexture = m_PortalTexture2;
        }
    }

	// Aras Pranckevicius MirrorReflection4
	// http://wiki.unity3d.com/index.php/MirrorReflection4 
	// Given position/normal of the plane, calculates plane in camera space.
	private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
		Vector3 offsetPos = pos + normal * -m_ClipPlaneOffset;
		Matrix4x4 m = cam.worldToCameraMatrix;
		Vector3 cpos = m.MultiplyPoint( offsetPos );
		Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
		return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
	}
        
    private bool checkPortalVisibility(Transform cam) {
        /*
         * Check if the given camera is on the right side of the portalMesh. Returns true if the camera
         * is on the rendered side of the mesh.
         */
        bool state = false;

        /* Create a plane that will define the portal */
        Vector3 planeNormal = transform.TransformDirection(faceNormal);
        Plane triggerPlane = new Plane(planeNormal, transform.position);
            
        /* Detect if the cam is on the proper side of the portal */
        if(triggerPlane.GetSide(cam.transform.position)) {
            state = true;
        }

        return state;
    }

    private void SetMaterial(bool visibility) {
        /*
         * Change the material of the portal depending on whether it is visible (true)
         * or not visible (false) from the camera's position. 
         */

        if(visibility) {
            GetComponent<MeshRenderer>().material = portalMaterial;
        }else {
            GetComponent<MeshRenderer>().material = invisibleMaterial;
        }
    }


    public string ExtractPortalSetID(string cam) {
        /*
         * Extract the portalSetID from the given camera name. Returns the name of the ID set it belongs to
         * or returns null if it is not a scout camera.
         */

        int idEndIndex = cam.IndexOf("|");
        string camPortalSetID = null;
        if(idEndIndex >= 0) {
            camPortalSetID = cam.Substring(0, idEndIndex);
        }

        return camPortalSetID;
    }

}