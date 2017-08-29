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



    private GameObject level1Camera;
    private RenderTexture level1RendTex;
    private GameObject level2Camera;
    private RenderTexture level2RendTex;

    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {
        /*
         * Ensure the portal has a proper material created
         */
         
        if(!portalMaterial) {
            portalMaterial = new Material(Shader.Find("Unlit/Portal"));
            portalMaterial.name = "__PortalMaterial" + GetInstanceID();
            GetComponent<MeshRenderer>().material = portalMaterial;
        }



        /* Properly create each depth camera */
        level1Camera = new GameObject("ScoutCamera1");
        CreateScoutCamera(level1Camera);
        RenderTexture level1RendTex = CreateRenderTexture(level1Camera.GetComponent<Camera>());

        level2Camera = new GameObject("ScoutCamera2");
        CreateScoutCamera(level2Camera);
        RenderTexture level2RendTex = CreateRenderTexture(level2Camera.GetComponent<Camera>());
    }
    
 

    

    public void OnWillRenderObject() {
        /*
         * ITS ALL GONNA BE DONE HERE
         */

        /* Get the camera rendering this portal */
        Camera cam = Camera.current;
        string camName = ExtractPortalSetID(cam.name);

        /*
         * Update the playerCamera with the new texture for this portal
         */
        if(cam.name == "CameraTest") {
            if(checkPortalVisibility(cam.transform)) {
                //Debug.Log(name + " about to be drawn");

                /* Create a camera and it's renderTexture to be used for the view */
                /* Use the level1 camera for this render */
                Texture cameraView = null;

                /* Render the camera's view and get the proper texture from it and send it to the cameraScript */
                OnWillRenderObject123(cam, level1Camera.GetComponent<Camera>(), ref cameraView);
                cam.GetComponent<CameraScript>().AssignMeshTo(gameObject, cameraView);
            }
        }


        /*
         * If this mesh is going to be rendered by a scoutCamera, change it's material 
         * If there is a portalMesh that will be rendered by a scoutCamera, 
         * tell the scoutCamera to change it's material to be null
         */
        if(cam.name == "ScoutCamera1" && cam.GetComponent<CameraScript>()) {
            if(checkPortalVisibility(cam.transform)) {
                Texture cameraView = null;

                OnWillRenderObject123RETURNINSTEAD(cam, level2Camera.GetComponent<Camera>(), ref cameraView);
                cam.GetComponent<CameraScript>().AssignMeshTo(gameObject, cameraView);
            }
        }


        if(cam.name == "ScoutCamera2") {
            //Debug.Log(cam.transform.parent.name + " an InternalCamera is drawing " + name);
            //Apply the internalCamera's rendTex to the thing
            //Debug.Log("Tripple Depth getting rendered");

            /* To prevent infinite recursion, do not render portals beyond the third depth */
            Texture cameraView = null;
            cam.GetComponent<CameraScript>().AssignMeshTo(gameObject, cameraView);
        }


        /*
         * 1: Send request to render scene
         * 2: This request will send a OnWillRenderObject() call to each mesh that will be rendered
         * 3: For now, Each rendered mesh will update their mesh by rendering their scoutCamera.
         *      This will however call the OnWillRenderObject() once more
         */
    }










    public void OnWillRenderObject123(Camera viewingCamera, Camera scoutingCamera, ref Texture extractedView) {
        /*
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
         * 
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the player's position then render the image of that camera.
         */
        count++;

        /* Check if key gameObjects are active */
        if(!enabled || !scoutingCamera || !pointB)
            return;

        /* Ensure the viewingCamera used exists */
        if(!viewingCamera)
            return;

        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;


        /* Set the material of the portal whether it is visible or not */
        /* Ensure the portal can be seen from the viewingCamera */
        if(checkPortalVisibility(viewingCamera.transform)) {
            SetMaterial(true);
        }
        else {
            SetMaterial(false);
            return;
        }
        
        /* Ensure the renderTexture for the scoutCamera is properly created */
        UpdateRenderTexture(ref m_PortalTexture, scoutingCamera);

        
        /* Set up values to properly render the camera */
        Vector3 pos = transform.position;
        Vector3 normal = transform.TransformDirection(faceNormal);

        // this will make it depend on the points' position, rotation, and scale
        scoutingCamera.transform.position = pointB.TransformPoint(transform.InverseTransformPoint(viewingCamera.transform.position));
        scoutingCamera.transform.rotation = Quaternion.LookRotation(
                pointB.TransformDirection(transform.InverseTransformDirection(viewingCamera.transform.forward)),
                pointB.TransformDirection(transform.InverseTransformDirection(viewingCamera.transform.up)));

        // I don't know how this works it just does, I got lucky
        Vector4 clipPlane = CameraSpacePlane(viewingCamera, pos, normal, -1.0f);
        Matrix4x4 projection = viewingCamera.CalculateObliqueMatrix(clipPlane);
        scoutingCamera.projectionMatrix = projection;

        /* Render the scoutCamera */
        scoutingCamera.Render();
        /* Apply the render texture to any rendered material using the proper portal material/shader */
        Material[] materials = rend.sharedMaterials;
        foreach(Material mat in materials) {
            if(mat.HasProperty("_PortalTex")) {
                mat.SetTexture("_PortalTex", m_PortalTexture);
                //Extract the texture and use it as the view
                extractedView = mat.GetTexture("_PortalTex");
            }
        }








        /* Release the lock */
        s_InsideRendering = false;
    }



    public void OnWillRenderObject123RETURNINSTEAD(Camera viewingCam, Camera scoutingCam, ref Texture thefinalTexture) {
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
        if(!viewingCam)
            return;

        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;
        

        /* Set the material of the portal whether it is visible or not */
        if(checkPortalVisibility(viewingCam.transform)) {
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
        //CreateNeededObjects();
        //Ensure the internalCamera has a properly initilized renderTexture
        UpdateRenderTexture(ref m_PortalTexture2, scoutingCam);


        /* Set up values to properly render the camera */
        Vector3 pos = transform.position;
        Vector3 normal = transform.TransformDirection(faceNormal);

        // this will make it depend on the points' position, rotation, and scale
        scoutingCam.transform.position = pointB.TransformPoint(transform.InverseTransformPoint(viewingCam.transform.position));
        scoutingCam.transform.rotation = Quaternion.LookRotation(
                pointB.TransformDirection(transform.InverseTransformDirection(viewingCam.transform.forward)),
                pointB.TransformDirection(transform.InverseTransformDirection(viewingCam.transform.up)));

        // I don't know how this works it just does, I got lucky
        Vector4 clipPlane = CameraSpacePlane(viewingCam, pos, normal, -1.0f);
        Matrix4x4 projection = viewingCam.CalculateObliqueMatrix(clipPlane);
        scoutingCam.projectionMatrix = projection;

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
        thefinalTexture = m_PortalTexture2;
        scoutingCam.Render();
        //Debug.Log("done RENDER REQ");
        //Debug.Log("Done scount Render");




        /* Release the lock */
        s_InsideRendering = false;
    }


















    // Aras Pranckevicius MirrorReflection4
    // http://wiki.unity3d.com/index.php/MirrorReflection4 
    // Cleanup all the objects we possibly have created
    void OnDisable() {
        /*
         * Cleanup any dynamicly created objects such as cameras and renderTextures
         */

        if(m_PortalTexture) {
            DestroyImmediate(m_PortalTexture);
            m_PortalTexture = null;
        }

        if(m_PortalTexture2) {
            DestroyImmediate(m_PortalTexture2);
            m_PortalTexture2 = null;
        }
    }

    


    /* -------- Event Functions ---------------------------------------------------- */

    private void AssignTextureToCamera(Camera givenCamera, RenderTexture givenRendTex) {
        /*
         * Assign the given renderTexture texture to the given camera's targetTexture. 
         */

        /* Assign the texture */
        givenCamera.targetTexture = givenRendTex;
    }

    private void CreateScoutCamera(GameObject cameraParent) {
        /*
         * Create a scoutCamera that will be used to render a portal's view.
         * The camera will be a component of the given gameObject
         */
        Camera camera = cameraParent.AddComponent<Camera>();
        
        /* Disable the camera and the audio listener for the camera since we will call it using .Render() */
        Destroy(camera.GetComponent<AudioListener>());
        camera.enabled = false;

        /* Add a cameraScript to the camera to handle recursive portal rendering */
        cameraParent.AddComponent<CameraScript>().Start();
    }

    private RenderTexture CreateRenderTexture(Camera camera) {
        /*
         * Create a renderTexture to be assigned to the given camera
         */
        RenderTexture renderTexture = null;
        UpdateRenderTexture(ref renderTexture, camera);

        return renderTexture;
    }

    private void UpdateRenderTexture(ref RenderTexture renderTexture, Camera camera) {
        /*
         * Ensure the given renderTexture is properly updated. It will link itself to the given
         * camera's targetTexture if it needs to be recreated.
         */

        /* Recreate the texture if it doesnt exist or it's sizes are incorrect */
        if(!renderTexture || renderTexture.width != m_TextureSize || renderTexture.height != m_TextureSize) {

            /* Destroy the old texture if needed */
            if(renderTexture) {
                DestroyImmediate(renderTexture);
            }
            
            /* Create the new renderTexture, naming it using the portalMesh's ID */
            renderTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);
            renderTexture.name = "__PortalRendTex " + GetInstanceID();

            /* Set default values for the renderTexture */
            renderTexture.hideFlags = HideFlags.DontSave;
            renderTexture.isPowerOfTwo = true;

            /*  Reassign the renderTexture to it's camera */
            camera.targetTexture = renderTexture;
        }
    }



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