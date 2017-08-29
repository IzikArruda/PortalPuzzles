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



    /* The max viewing depth for recursive portal calls */
    private static int maxCameraDepth = 2;

    /* An array of cameras used for this portal's recursive portal rendering */
    private GameObject[] recursiveCameras;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {
        /*
         * Ensure the portal has all it's objects properly created.
         */
         
        /* Check if the portalMaterial used for the portal is created */
        if(!portalMaterial) {
            portalMaterial = new Material(Shader.Find("Unlit/Portal"));
            portalMaterial.name = "__PortalMaterial" + GetInstanceID();
            GetComponent<MeshRenderer>().material = portalMaterial;
        }

        /* Create the recursive rendering cameras for this portal */
        //For now, always recreate the array
        if(true || recursiveCameras == null || recursiveCameras.Length != maxCameraDepth) {

            /* Empty the current camera array before re-creating it */
            if(recursiveCameras != null) {
                foreach(GameObject go in recursiveCameras) {
                    DestroyImmediate(go);
                }
            }

            /* Create the camera array and populate it */
            recursiveCameras = new GameObject[maxCameraDepth];
            for(int i = 0; i < recursiveCameras.Length; i++) {
                recursiveCameras[i] = CreateScoutCamera();
                recursiveCameras[i].name = "ScoutCamera | Depth " + i;
                recursiveCameras[i].transform.parent = transform;
                recursiveCameras[i].GetComponent<CameraScript>().cameraDepth = i;
                recursiveCameras[i].GetComponent<CameraScript>().portalSetID = portalSetID;
                recursiveCameras[i].GetComponent<CameraScript>().scout = true;

                /* Keep the cameras in the editor after creation */
                recursiveCameras[i].hideFlags = HideFlags.DontSave;

                /* Create a renderTexture for this camera */
                recursiveCameras[i].GetComponent<CameraScript>().renderTexture = 
                        CreateRenderTexture(recursiveCameras[i].GetComponent<Camera>());

            }
        }
    }
    
    public void OnWillRenderObject() {
        /*
         * When this portal is scheduled to get rendered, use the camera that will render it to determine
         * what texture the portal will have. It all depends on whether the camera is a scoutCamera
         * and how deep the camera is into it's recursive rendering.
         */
        Texture cameraView = null;
        Camera camera = Camera.current;
        CameraScript cameraScript = camera.GetComponent<CameraScript>();
        
        /* Ensure the camera rendering this portal has the proper CameraScript */
        if(cameraScript) {

            /* Use the view of the first recursiveCamera to render the portal */
            if(!cameraScript.scout) {
                OnWillRenderObject123(camera, recursiveCameras[0].GetComponent<CameraScript>(), ref cameraView);
                camera.GetComponent<CameraScript>().AssignMeshTo(gameObject, cameraView);
            }else {

                /* Use the next camera down the recursiveCameras list */
                if(cameraScript.cameraDepth >= 0 && cameraScript.cameraDepth < maxCameraDepth-1) {
                    OnWillRenderObject123(camera, recursiveCameras[cameraScript.cameraDepth + 1].GetComponent<CameraScript>(), ref cameraView);
                    cameraScript.AssignMeshTo(gameObject, cameraView);
                }

                /* If it is using the last scoutCamera of the recursiveCamera list, apply a null texture */
                else if(cameraScript.cameraDepth >= maxCameraDepth-1) {
                    //Debug.Log("WARNING: CAMERA RENDER DEPTH REACHED");
                    cameraScript.AssignMeshTo(gameObject, cameraView);
                }

                /* A scoutCamera has a depth outside the depth range */
                else {
                    Debug.Log("WARNING: A CAMERA HAS A CAMERADEPTH OUTSIDE THE POSSIBLE RANGE | " + cameraScript.cameraDepth);
                }
            }
        }

        /* Catch any cameras trying to render a portal without using a cameraScript */
        else if(camera.name != "SceneCamera" && camera.name != "Preview Camera") {
            Debug.Log("WARNING: CAMERA " + camera.name + " RENDERING A PORTAL WITHOUT A CAMERASCRIPT");
        }
    }
    
    public void OnWillRenderObject123(Camera viewingCamera, CameraScript cameraScript, ref Texture extractedView) {
        /*
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
         * 
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the viewingCamera's position then render the image onto the extractedView.
         */

        /* Check if key gameObjects are active */
        if(!enabled || !pointB || !viewingCamera || !cameraScript) {
            return;
        }

        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;
        
        /* Ensure the portal can be seen from the viewingCamera */
        if(checkPortalVisibility(viewingCamera.transform)) {
            SetMaterial(true);
        }
        else {
            SetMaterial(false);
            return;
        }

        /* Extract the scouting camera from the cameraScript */
        Camera scoutingCamera = cameraScript.GetComponent<Camera>();

        /* Check if the scouting camera exists along with it's renderTexture */
        if(!scoutingCamera || !cameraScript.renderTexture) {
            return;
        }

        /* Ensure the renderTexture for the scoutCamera is properly created */
        UpdateRenderTexture(ref cameraScript.renderTexture, scoutingCamera);
        

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
        
        /* Extract the scoutingCamera's view after rendering as a static texture */
        Material[] materials = rend.sharedMaterials;
        foreach(Material mat in materials) {
            if(mat.HasProperty("_PortalTex")) {
                mat.SetTexture("_PortalTex", cameraScript.renderTexture);
                extractedView = mat.GetTexture("_PortalTex");
            }
        }
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

    private GameObject CreateScoutCamera() {
        /*
         * Create a gameObject with a camera component with a cameraScript
         * to be used as a scoutCamera for this portal.
         */
        GameObject cameraParent = new GameObject();
        Camera camera = cameraParent.AddComponent<Camera>();
        
        /* Disable the camera since we will call it using .Render() */
        camera.enabled = false;

        /* Add a cameraScript to the camera to handle recursive portal rendering */
        cameraParent.AddComponent<CameraScript>().Start();

        return cameraParent;
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