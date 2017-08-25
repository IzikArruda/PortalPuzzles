﻿/*
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

    /* The forward vector of the portal */
    public Vector3 faceNormal = Vector3.forward; 

    /* Quality of the texture rendered and applied to the portal mesh. 2048 is recommended. */
	public int m_TextureSize;
    private int m_OldPortalTextureSize = 0;

    /* Offset to the camera's clipping plane when rendering. 0 is recommended */
    public float m_ClipPlaneOffset;

    /* The renderTexture produced by the camera */
	private RenderTexture m_PortalTexture = null;

    /* Whether the portal is currently visible and being drawn to a camera */
    public bool beingDrawn = true;
        
    /* Materials used on the portalMesh */
    private Material portalMaterial;
    public Material invisibleMaterial;

    /* Static value used to track recursive portal rendering calls */
    private static bool s_InsideRendering = false;

    /* The ID of the portalSet that this portalMesh is a child of */
    public string portalSetID;


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
    }

	public void OnWillRenderObject() {
        /*
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
         * 
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the player's position then render the image of that camera.
         */

        /* Check if key gameObjects are active */
        if(!enabled || !scoutCamera || !pointB)
			return;

        /* Get the camera rendering this portal */
		Camera cam = Camera.current;
		if( !cam )
			return;
        
        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;

        










        /* Extract the portalID from the camera rendering this portal if it is a porltaMesh's scount camera */
        int idEndIndex = cam.name.IndexOf("|");
        string portalID = "";
        if(idEndIndex >= 0) {
            portalID = cam.name.Substring(0, idEndIndex);
        }else {
            /* The camera being used is not a scount camera */
            portalID = null;
        }
        
        /* Do not render if the camera's portalSetID shares the same portalID as this mesh's linked portalSet ID */
        if(portalID == portalSetID) {
            //return;
            //This needs to stay like this until the object is "rendered", then go back to visible mesh
            Debug.Log("Set portlaMeshInvisible" + GetInstanceID());
        }
        //check if the there mesh3 camera is being rendered when we arent looking at it, but a portal is
        if(name == "There Mesh3") {
            //Debug.Log("MESH3 DRAWING USING " + cam.name);
            //WE NEED TO LET THIS DRAW WHEN NOT USING THE SCENE CAMERA
            //AKA, allow recursion when not using scene camera
        }














        /* Prevent recursive reflections by using the lock */
        //do not allow recursive calls if it's done by a non-scount camera.
        //This may still cause a loop if two portals face eachother, so there should be a natural stopping point
        //ONLY ALLOW RECURSION WHEN THE CAMERA AND PORTALMESH TO RENDER ARE FROM DIFFERENT SETS
        ////////if(s_InsideRendering_depth1 == false && portalID != null && portalID != portalSetID) {


        /* Prevent recursive reflections when using non-scout cameras, unless working with Mesh3, the onyl portal allowed for recursion */
        if(name == "There Mesh3") {
            Debug.Log("RENDERING USING " + cam.name);
        }

        else {
            if(s_InsideRendering) {
                return;
            }
            s_InsideRendering = true;
        }



        
        //IDEA: ONCE IT GETS RENDERED, DO NOT RENDER IT AGAIN (IR PUT A LOCK) UNTIL THE SCENE CAMERA RENDERS THE SCENE
        //by this i mean do not rerender the camera/mesh once its been done before










        /* Set the material of the portal whether it is visible or not */
        if(checkPortalVisibility(cam.transform)) {
            SetMaterial(true);
        }else {
            SetMaterial(false);
        }
            
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
        Vector4 clipPlane = CameraSpacePlane( cam, pos, normal, -1.0f );
		Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
        scoutCamera.projectionMatrix = projection;
            
        if(!scoutCamera.enabled) { // make it manual
			scoutCamera.Render();
		} else
			scoutCamera.enabled = false;
            
        /* Apply the render texture to any rendered material using the proper portal material/shader */
        Material[] materials = rend.sharedMaterials;
		foreach( Material mat in materials ) {
			if( mat.HasProperty("_PortalTex")) {
                mat.SetTexture( "_PortalTex", m_PortalTexture );
            }
        }

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
        if( !m_PortalTexture || m_OldPortalTextureSize != m_TextureSize ){
			if( m_PortalTexture )
				DestroyImmediate( m_PortalTexture );

			m_PortalTexture = new RenderTexture( m_TextureSize, m_TextureSize, 16 );
			m_PortalTexture.name = "__PortalRenderTexture" + GetInstanceID();
			m_PortalTexture.isPowerOfTwo = true;
			m_PortalTexture.hideFlags = HideFlags.DontSave;
			m_OldPortalTextureSize = m_TextureSize;

            scoutCamera.targetTexture = m_PortalTexture;
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
}