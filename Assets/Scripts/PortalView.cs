/*
 * Thanks to Aras Pranckevicius' MirrorReflection4
 * http://wiki.unity3d.com/index.php/MirrorReflection4 
 * 
 * Using an array of "scout cameras", Render a view from this portal to it's scout point. Use the camera that
 * is rendering it to rotate and position the view so that this mesh is a seamless window into the scoutPoint.
 */

using UnityEngine;
using System.Collections;

public class PortalView : MonoBehaviour {
    
    /* The position that this portal is scouting to when rendering a view */
    public Transform scoutPoint;
    
    /* The portal that is backwards relative to the scoutPoint */
    public PortalView scoutPointReversedPortal;

    /* The forward vector of the portal */
    public Vector3 faceNormal = Vector3.forward; 

    /* Offset to the camera's clipping plane when rendering. 0 is recommended */
    public float m_ClipPlaneOffset;
        
    /* Materials used on the portalMesh */
    private Material portalMaterial;
    public Material invisibleMaterial;

    /* The ID of the portalSet that this portalMesh is a child of */
    public string portalSetID;

    /* An array of cameras used for this portal's recursive portal rendering */
    private GameObject[] recursiveCameras;

    /* The max viewing depth for recursive portal calls */
    public int maxCameraDepth = 1;

    /* Values that track what the camera's culling layers will be */
    private int cameraIgnoreLayer = -1;
    private bool renderTerrain = false;
    private bool onlySkySphere = false;

    /* Used to determine when to add the flare layer to the cameras */
    private bool flareLayerToBeAdded = false;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    public void Start() {
        /*
         * Ensure the portal has all the required objects created and variables initialized.
         */
         
        /* Create the portal's components if they are not yet created */
        if(GetComponent<MeshFilter>() == null) {
            gameObject.AddComponent<MeshFilter>();
        }
        if(GetComponent<MeshRenderer>() == null) {
            gameObject.AddComponent<MeshRenderer>();
        }

        /* Check if the portalMaterial used for the portal is created and assigned to the portal */
        if(!portalMaterial) {
            portalMaterial = new Material(Shader.Find("Unlit/Portal"));
            portalMaterial.name = "__PortalMaterial" + GetInstanceID();
            GetComponent<MeshRenderer>().material = portalMaterial;
        } else if(GetComponent<MeshRenderer>().material.GetInstanceID() != portalMaterial.GetInstanceID()) {
            GetComponent<MeshRenderer>().material = portalMaterial;
        }
        
        /* Create the recursive rendering cameras for this portal */
        if(recursiveCameras == null || recursiveCameras.Length != maxCameraDepth) {

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
                recursiveCameras[i].GetComponent<CameraScript>().cameraDepth = i;
                recursiveCameras[i].GetComponent<Camera>().farClipPlane = CustomPlayerController.cameraFarClippingPlane;

                /* Remove the camera once the scene unloads */
                recursiveCameras[i].hideFlags = HideFlags.DontSave;

                /* Update the renderTexture for this camera */
                UpdateRenderTexture(ref recursiveCameras[i].GetComponent<CameraScript>().renderTexture, 
                        recursiveCameras[i].GetComponent<Camera>());

                /* Add the flare layer if required */
                if(flareLayerToBeAdded) {
                    recursiveCameras[i].AddComponent<FlareLayer>();
                }
            }

            /* Assign the proper renderingLayer to the cameras */
            UpdateCameraRenderingLayer();
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
        

        /* Ensure the camera rendering this portal has the proper CameraScript to handle portals */
        if(cameraScript) {
            
            /* If the rendering camera is not a scout camera, render the portal's view with it's first recursiveCamera */
            if(!cameraScript.scout) {
                //Debug.Log(name + " getting rendered");
                RenderCameraView(camera, recursiveCameras[0].GetComponent<Camera>(), ref cameraView);
                camera.GetComponent<CameraScript>().AssignMeshTo(gameObject, cameraView);

            }

            /* Dont let a scoutCamera render a mesh if it is behind the scout point */
            else if(MeshBehindMesh(GetComponent<MeshFilter>(), 
                    camera.transform.parent.GetComponent<PortalView>().scoutPointReversedPortal.GetComponent<MeshFilter>())) {
                //Debug.Log(camera.name + " is trying to render " + name + " | Skip due to portal being behind camera's focus portal");
            }
            
            /* Dont let the scoutCamera render a portal from it's own portalSet */
            else if(camera.transform.parent.GetComponent<PortalView>().portalSetID == portalSetID) {
                //Debug.Log(camera.name + " is trying to render " + name + " | Skip due to it being form the same portalSet");
            }
            
            /* If the camera is trying to render a portal facing away from the camera, dont do it */
            else if(!checkPortalVisibility(camera.transform)) {
                //Debug.Log(camera.name + " is trying to render " + name + " | Skip due to camera facing portal's backface");
            }
            
            /* Render the portal's texture for this camera */
            else {
                
                /* Use the next camera down the recursiveCameras list */
                if(cameraScript.cameraDepth >= 0 && cameraScript.cameraDepth < maxCameraDepth-1) {
                    RenderCameraView(camera, recursiveCameras[cameraScript.cameraDepth + 1].GetComponent<Camera>(), ref cameraView);
                    cameraScript.AssignMeshTo(gameObject, cameraView);
                }

                /* If it is using the last scoutCamera of the recursiveCamera list, apply a null texture */
                else if(cameraScript.cameraDepth >= maxCameraDepth-1) {
                    //Debug.Log("WARNING: CAMERA RENDER DEPTH REACHED");
                    cameraScript.AssignMeshTo(gameObject, cameraView);
                }

                /* The scoutCamera has a depth outside the depth range */
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
    

    /* -------- Camera Rendering Functions ---------------------------------------------------- */

    public void RenderCameraView(Camera viewingCamera, Camera scoutCamera, ref Texture extractedView) {
        /*
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the viewingCamera's position then render the image onto the extractedView.
         * 
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
         */

        /* Check if key gameObjects are active */
        if(!enabled || !scoutPoint || !viewingCamera || !scoutCamera) {
            return;
        }

        /* Check if the portal's linked renderer is properly initilized */
        var rend = GetComponent<Renderer>();
        if(!rend || !rend.sharedMaterial || !rend.enabled)
            return;

        /* Ensure the portal can be seen from the viewingCamera */
        if(!checkPortalVisibility(viewingCamera.transform)) {
            return;
        }

        /* Ensure the viewing camera can actually render a scene */
        if(viewingCamera.pixelWidth <= 1 || viewingCamera.pixelHeight <= 1) {
            Debug.Log("OUT OF VIEW FUSTRUM ERROR CAUGHT");
            return;
        }
        
        /* Extract the cameraScript from the scouting camera */
        CameraScript cameraScript = scoutCamera.GetComponent<CameraScript>();

        /* Ensure the renderTexture for the scoutCamera is properly created */
        UpdateRenderTexture(ref cameraScript.renderTexture, scoutCamera);

        /* Check if the scouting camera exists along with it's renderTexture */
        if(!scoutCamera || !cameraScript || !cameraScript.renderTexture) {
            return;
        }
        
        /* Set up values to properly position the camera */
        Vector3 pos = transform.position;
        Vector3 normal = transform.TransformDirection(faceNormal);

        /* Place the scoutCamera in a position relative to it's target portal as the viewing camera is to it's portal */
        scoutCamera.transform.position = scoutPoint.TransformPoint(transform.InverseTransformPoint(viewingCamera.transform.position));
        scoutCamera.transform.rotation = Quaternion.LookRotation(
                scoutPoint.TransformDirection(transform.InverseTransformDirection(viewingCamera.transform.forward)),
                scoutPoint.TransformDirection(transform.InverseTransformDirection(viewingCamera.transform.up)));

        /* Reset the projection matrix of this camera */
        SetRectDefault(scoutCamera);
        
        /* Set a new clip plane and set the projection matrix for the portal's scoutCamera */
        Vector4 clipPlane = CameraSpacePlane(viewingCamera, pos, normal, -1.0f);
        Matrix4x4 projection = viewingCamera.CalculateObliqueMatrix(clipPlane);
        scoutCamera.projectionMatrix = projection;

        /* Cut out the scoutCamera's edges so it does not render anything outside the portal's view. */
        Rect boundingEdges = CalculateViewingRect(viewingCamera);
        /* If the rect of the portal from the camera's view is very small, do not bother rendering it */
        if(boundingEdges.width > 0.001f && boundingEdges.height > 0.001f) {
            SetScissorRect(scoutCamera, boundingEdges);

            /* Render the scoutCamera's view with it's new projection matrix */
            scoutCamera.Render();

            /* Extract the scoutingCamera's view after rendering as a static texture */
            foreach(Material mat in rend.sharedMaterials) {
                if(mat.HasProperty("_PortalTex")) {
                    mat.SetTexture("_PortalTex", cameraScript.renderTexture);
                    extractedView = mat.GetTexture("_PortalTex");
                }
            }
        }
    }
    
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
        /*
         *  Aras Pranckevicius MirrorReflection4
         *  http://wiki.unity3d.com/index.php/MirrorReflection4 
         *  Given position/normal of the plane, calculates plane in camera space.
         */

        Vector3 offsetPos = pos + normal * -m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }
    
    public void SetScissorRect(Camera cam, Rect r) {
        /*
         * Apply an additive projection matrix to the given camera
         * 
         * https://forum.unity3d.com/threads/scissor-rectangle.37612/
         */
         
        /* Prevent the camera's rect from going past the default boundaries */
        if(r.x < 0) {
            r.width += r.x;
            r.x = 0;
        }
        if(r.y < 0) {
            r.height += r.y;
            r.y = 0;
        }
        r.width = Mathf.Min(1 - r.x, r.width);
        r.height = Mathf.Min(1 - r.y, r.height);
        

        //cam.rect = new Rect(0, 0, 1, 1);
        //cam.ResetProjectionMatrix();
        Matrix4x4 m = cam.projectionMatrix;
        //		print( cam.projectionMatrix );
        //		print( Mathf.Rad2Deg * Mathf.Atan( 1 / cam.projectionMatrix[ 0 ] ) * 2 );
        cam.rect = r;
        //		cam.projectionMatrix = m;
        //		print( cam.projectionMatrix );		
        //		print( Mathf.Rad2Deg * Mathf.Atan( 1 / cam.projectionMatrix[ 0 ] ) * 2 );
        //		print( cam.fieldOfView );
        //		print( Mathf.Tan( cam.projectionMatrix[ 1, 1 ] ) * 2 );
        //		cam.pixelRect = new Rect( 0, 0, Screen.width / 2, Screen.height );
        //      Matrix4x4 m1 = Matrix4x4.TRS(new Vector3(r.x, r.y, 0), Quaternion.identity, new Vector3(r.width, r.height, 1));
        //		Matrix4x4 m1 = Matrix4x4.TRS( Vector3.zero, Quaternion.identity, new Vector3( r.width, r.height, 1 ) );
        //		Matrix4x4 m2 = m1.inverse;
        //		print( m2 );
        Matrix4x4 m2 = Matrix4x4.TRS(new Vector3((1/r.width - 1), (1/r.height - 1), 0), Quaternion.identity, new Vector3(1/r.width, 1/r.height, 1));
        Matrix4x4 m3 = Matrix4x4.TRS(new Vector3(-r.x  * 2 / r.width, -r.y * 2 / r.height, 0), Quaternion.identity, Vector3.one);
        //		m2[ 0, 3 ] = r.x;
        //		m2[ 1, 3 ] = r.y;
        //		print( m3 );
        //		print( cam.projectionMatrix );
        cam.projectionMatrix = m3 * m2 * m;
        //		print( cam.projectionMatrix );		
    }
    
    public void SetRectDefault(Camera cam) {
        /*
         * Set the rect of the given camera to be back to the default.
         * 
         * Used in conjunction with SetScissorRect.
         */

        cam.rect = new Rect(0, 0, 1, 1);
        cam.ResetProjectionMatrix();
    }
    

    /* -------- Initialization/Update Functions ---------------------------------------------------- */

    private GameObject CreateScoutCamera() {
        /*
         * Create a gameObject with a camera component and a cameraScript to be used as a scoutCamera for this portal.
         */
        GameObject cameraParent = new GameObject();
        Camera camera = cameraParent.AddComponent<Camera>();

        /* Make it the child of this portalMesh */
        cameraParent.transform.parent = transform;

        /* Disable the camera since we will call it using .Render() */
        camera.enabled = false;

        /* Add a cameraScript to the camera to handle recursive portal rendering */
        cameraParent.AddComponent<CameraScript>().Start();

        /* Set the values of the CameraScript  */
        cameraParent.GetComponent<CameraScript>().scout = true;

        return cameraParent;
    }

    private void UpdateRenderTexture(ref RenderTexture renderTexture, Camera camera) {
        /*
         * Ensure the given renderTexture is properly updated. It will link itself to the given
         * camera's targetTexture if it needs to be recreated.
         */

        /* Recreate the texture if it doesnt exist or it's sizes are incorrect */
        if(!renderTexture || renderTexture.width != Screen.width || renderTexture.height != Screen.height) {

            /* Destroy the old texture if needed */
            if(renderTexture) {
                DestroyImmediate(renderTexture);
            }

            /* Create the new renderTexture, naming it using the portalMesh's ID */
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            renderTexture.name = "__PortalRendTex " + GetInstanceID();

            /* Set default values for the renderTexture */
            renderTexture.hideFlags = HideFlags.DontSave;
            renderTexture.isPowerOfTwo = true;
            
            /*  Reassign the renderTexture to it's camera */
            camera.targetTexture = renderTexture;
        }
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void SetSkySphereLayer(bool newValue) {
        /*
         * Set the onlySkySphere boolean to the given value and refresh the portal's camera's rendering layer
         */

        onlySkySphere = newValue;
        UpdateCameraRenderingLayer();
    }

    public void SetRenderTerrain(bool newValue) {
        /*
         * Set the renderTerrain boolean to the given value and refresh the portal's camera's rendering layer
         */

        renderTerrain = newValue;
        UpdateCameraRenderingLayer();
    }
    
    public void AssignCameraLayer(int layer) {
        /*
         * Have this portal's cameras ignore the given layer. This is mainly used when handling double sided portals.
         */

        cameraIgnoreLayer = layer;
        UpdateCameraRenderingLayer();
    }

    public void UpdateCameraRenderingLayer() {
        /*
         * Update the camera's rendering layer to reflect this script's previously set variables
         * (cameraIgnoreLayer, onlySkySphere, renderTerrain).
         * 
         * Assign the given layer to be ignored by all the scout camera's linked to this portal and 
         * any new cameras created. If the layer is -1, do not remove any layers from being rendered.
         */
        
        if(recursiveCameras != null) {

            /* Only render the skySphere layer */
            if(onlySkySphere) {
                for(int i = 0; i < recursiveCameras.Length; i++) {
                    recursiveCameras[i].GetComponent<Camera>().cullingMask = 1 << PortalSet.maxLayer + 1;
                }
            }

            else {

                /* If told to ignore layer -1, the camera will render all layers */
                if(cameraIgnoreLayer == -1) {
                    for(int i = 0; i < recursiveCameras.Length; i++) {
                        recursiveCameras[i].GetComponent<Camera>().cullingMask = cameraIgnoreLayer;
                    }
                }

                /* Any value other than -1 will properly be ignored */
                else {
                    for(int i = 0; i < recursiveCameras.Length; i++) {
                        recursiveCameras[i].GetComponent<Camera>().cullingMask = ~(1 << cameraIgnoreLayer);
                    }
                }

                /* If the renderTerrain boolean is false, remove the terrain layer from the camera */
                if(!renderTerrain) {
                    for(int i = 0; i < recursiveCameras.Length; i++) {
                        recursiveCameras[i].GetComponent<Camera>().cullingMask = recursiveCameras[i].GetComponent<Camera>().cullingMask & ~(1 << PortalSet.maxLayer + 2);
                    }
                }
                /* If renderTerrain is true, only render the terrain layer */
                else {
                    for(int i = 0; i < recursiveCameras.Length; i++) {
                        recursiveCameras[i].GetComponent<Camera>().cullingMask = 1 << PortalSet.maxLayer + 2;
                    }
                }
            }
        }
    }

    public void ChangeMaterial(bool isVisible) {
        /*
         * Change the material of this mesh. If true, set it to visible (portalMaterial).
         * If false, set it to not be visible (invisibileMaterial)
         */

        if(isVisible) {
            GetComponent<MeshRenderer>().material = portalMaterial;
        }else {
            GetComponent<MeshRenderer>().material = invisibleMaterial;
        }
    }

    public bool checkPortalVisibility(Transform cam) {
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

    public Rect CalculateViewingRect(Camera camera) {
        /*
         * Given a camera and this mesh, calculate the bounding rect that this mesh has for this camera.
         * 
         * The edges are ranged from 0-1 with (0, 0) being bottom-left. increasing X goes right, increasing Y goes up.
         * 
         * WARNING: THIS WILL ONLY WORK IF THE PORTAL MESH WAS CREATED USING CreateDefaultMesh()
         */
        ArrayList cameraBoundsVerts = new ArrayList();
        Rect boundingEdges = new Rect();
        RaycastHit hitInfo;
        Ray camToMesh;
        Vector3 vert;
        float bottomBound = 1;
        float topBound = 0;
        float leftBound = 1;
        float rightBound = 0;

        /*
         * Fire a ray from each corner of the camera's view and check if it collides with the portal mesh
         */
        /* Fire a ray from the camera's top left corner to see if it hits this mesh */
        camToMesh = camera.ViewportPointToRay(new Vector3(0, 1, 1));
        if(GetComponent<BoxCollider>().Raycast(camToMesh, out hitInfo, float.MaxValue)) {
            //Debug.DrawRay(camToMesh.origin, camToMesh.direction*hitInfo.distance, Color.green, 0.2f);
            //Debug.Log("-- TOP LEFT -- ");
            topBound = 1;
            leftBound = 0;
        }
        /* Fire a ray from the camera's top right corner to see if it hits this mesh */
        camToMesh = camera.ViewportPointToRay(new Vector3(1, 1, 1));
        if(GetComponent<BoxCollider>().Raycast(camToMesh, out hitInfo, float.MaxValue)) {
            //Debug.DrawRay(camToMesh.origin, camToMesh.direction*hitInfo.distance, Color.green, 0.2f);
            //Debug.Log("-- TOP RIGHT --");
            topBound = 1;
            rightBound = 1;
        }
        /* Fire a ray from the camera's bottom left corner to see if it hits this mesh */
        camToMesh = camera.ViewportPointToRay(new Vector3(0, 0, 1));
        if(GetComponent<BoxCollider>().Raycast(camToMesh, out hitInfo, float.MaxValue)) {
            //Debug.DrawRay(camToMesh.origin, camToMesh.direction*hitInfo.distance, Color.green, 0.2f);
            //Debug.Log("-- BOTTOM LEFT --");
            bottomBound = 0;
            leftBound = 0;
        }
        /* Fire a ray from the camera's bottom right corner to see if it hits this mesh */
        camToMesh = camera.ViewportPointToRay(new Vector3(1, 0, 1));
        if(GetComponent<BoxCollider>().Raycast(camToMesh, out hitInfo, float.MaxValue)) {
            //Debug.DrawRay(camToMesh.origin, camToMesh.direction*hitInfo.distance, Color.green, 0.2f);
            //Debug.Log("-- BOTTOM RIGHT --");
            bottomBound = 0;
            rightBound = 1;
        }


        /* Take each vertex that forms this mesh and find it's viewport position on the camera's screen */
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        for(int i = 0; i < vertices.Length; i++) {
            //Convert the vert to world space
            vertices[i] = transform.TransformPoint(vertices[i]);
            //Convert it to a position on the camera's view
            vertices[i] = camera.WorldToViewportPoint(vertices[i]);
        }

        /* Get the bounding edges of the mesh on the camera's view  */
        /* Go through each vertex forming this portal's mesh and sort them into their proper array */
        ArrayList verticesOutsideView = new ArrayList();
        for(int i = 0; i < vertices.Length; i++) {
            vert = vertices[i];
            
            /* Track the index of each vert that is not within the camera's view */
            if(vert.z < 0 || vert.x < 0 || vert.x > 1 || vert.y < 0 || vert.y > 1) {
                verticesOutsideView.Add(i);
            }
            
            /* Add each vertex to the boundsVert list */
            cameraBoundsVerts.Add(vert);
        }

        /* If there exists any vertices outside the camera's view, find new vertices to calculate the camera's bounds */
        if(verticesOutsideView.Count > 0) {
            
            /* Get an array of rays that are the edges of each vertice outside the screen bounderies */
            ArrayList edgeRays = new ArrayList();
            ArrayList edgeRayMaxDistance = new ArrayList();
            
            /* Get 4 planes that define the camera's viewport edges in world coordinates */
            Plane camTopPlane = new Plane(camera.transform.position,
                    camera.ViewportToWorldPoint(new Vector3(1, 1, 1)),
                    camera.ViewportToWorldPoint(new Vector3(0, 1, 1)));
            Plane camBottomPlane = new Plane(camera.transform.position,
                    camera.ViewportToWorldPoint(new Vector3(0, 0, 1)),
                    camera.ViewportToWorldPoint(new Vector3(1, 0, 1)));
            Plane camLeftPlane = new Plane(camera.transform.position,
                    camera.ViewportToWorldPoint(new Vector3(0, 1, 1)),
                    camera.ViewportToWorldPoint(new Vector3(0, 0, 1)));
            Plane camRightPlane = new Plane(camera.transform.position,
                    camera.ViewportToWorldPoint(new Vector3(1, 0, 1)),
                    camera.ViewportToWorldPoint(new Vector3(1, 1, 1)));
            
            /* Create an array for the vertices in world coordinates */
            Vector3[] worldVertices = GetComponent<MeshFilter>().mesh.vertices;
            for(int i = 0; i < worldVertices.Length; i++) {
                worldVertices[i] = transform.TransformPoint(worldVertices[i]);
            }
            
            /* For each vertex outside the camera's view, find where on it's connecting edges meets the camera's view  */
            Ray edge1 = new Ray();
            Ray edge2 = new Ray();
            float distance1 = 0;
            float distance2 = 0;
            for(int i = 0; i < verticesOutsideView.Count; i++) {

                /* If the vertex at index 0 is offscreen, use the two edges between 1 and 3 */
                if((int) verticesOutsideView[i] == 0) {
                    //Debug.Log("0");
                    /* Get two rays that define both the 0-1 and 0-3 edges */
                    edge1 = new Ray(worldVertices[1], worldVertices[0] - worldVertices[1]);
                    edge2 = new Ray(worldVertices[3], worldVertices[0] - worldVertices[3]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[0] - worldVertices[1]).magnitude;
                    distance2 = (worldVertices[0] - worldVertices[3]).magnitude;
                }

                /* If the vertex at index 1 is offscreen, use the two edges between 0 and 2 */
                else if((int) verticesOutsideView[i] == 1) {
                    //Debug.Log("1");
                    /* Get two rays that define both the 1-0 and 1-2 edges */
                    edge1 = new Ray(worldVertices[0], worldVertices[1] - worldVertices[0]);
                    edge2 = new Ray(worldVertices[2], worldVertices[1] - worldVertices[2]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[1] - worldVertices[0]).magnitude;
                    distance2 = (worldVertices[1] - worldVertices[2]).magnitude;

                }

                /* If the vertex at index 2 is offscreen, use the two edges between 1 and 3 */
                else if((int) verticesOutsideView[i] == 2) {
                    //Debug.Log("2");
                    /* Get two rays that define both the 2-1 and 2-3 edges */
                    edge1 = new Ray(worldVertices[1], worldVertices[2] - worldVertices[1]);
                    edge2 = new Ray(worldVertices[3], worldVertices[2] - worldVertices[3]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[2] - worldVertices[1]).magnitude;
                    distance2 = (worldVertices[2] - worldVertices[3]).magnitude;
                }

                /* If the vertex at index 3 is offscreen, use the two edges between 0 and 2 */
                else if((int) verticesOutsideView[i] == 3) {
                    //Debug.Log("3");
                    /* Get two rays that define both the 3-0 and 3-2 edges */
                    edge1 = new Ray(worldVertices[0], worldVertices[3] - worldVertices[0]);
                    edge2 = new Ray(worldVertices[2], worldVertices[3] - worldVertices[2]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[3] - worldVertices[0]).magnitude;
                    distance2 = (worldVertices[3] - worldVertices[2]).magnitude;
                }
                else {
                    Debug.Log("WANING: MESH HAS MORE THAN 4 VERTICES");
                }
                
                /* Add this data into the ray arrays to track them */
                edgeRays.Add(edge1);
                edgeRays.Add(edge2);
                edgeRayMaxDistance.Add(distance1);
                edgeRayMaxDistance.Add(distance2);
            }


            /*
             * Now that the ray arrayList is populated with all edges that potentially reach past the camera's bounderies,
             * Fire them all and save their collision points if they hit any of the camera plane's edges.
             * 
             * For each succesfull collision that occurs, take that collision point and save it as a worldVert
             */
            ArrayList newWorldVerts = new ArrayList();
            Vector3 newVert;
            Ray edgeRay;
            float edgeDistance;
            float rayDistance;
            for(int i = 0; i < edgeRays.Count; i++) {
                edgeRay = (Ray) edgeRays[i];
                edgeDistance = (float) edgeRayMaxDistance[i];
                
                /* Raycast for the top plane */
                if(camTopPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
                /* Raycast for the bottom plane */
                if(camRightPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
                /* Raycast for the left plane */
                if(camLeftPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
                /* Raycast for the right plane */
                if(camBottomPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
            }
            
            /* Convert all the new verts from world position to the camera's viewport position */
            for(int i = 0; i < newWorldVerts.Count; i++) {
                newVert = (Vector3) newWorldVerts[i];
                newVert = camera.WorldToViewportPoint(newVert);
                newWorldVerts[i] = newVert;
            }

            /* Remove any viewport verts that are too far outside the viewable range */
            for(int i = 0; i < newWorldVerts.Count; i++) {
                newVert = (Vector3) newWorldVerts[i];
                if(newVert.x < -0.01f || newVert.x > 1.01f || newVert.y < -0.01f || newVert.y > 1.01f || newVert.z < 0) {
                    //Debug.Log("REMOVED with: " + newVert.x + " | " + newVert.y + " | " + newVert.z);
                    newWorldVerts.RemoveAt(i);
                    i--;
                }
            }

            /* Add the remaining verts into the cameraBoundsVerts array to be used to calcualte the camera's bounds */
            for(int i = 0; i < newWorldVerts.Count; i++) {
                newVert = (Vector3) newWorldVerts[i];
                cameraBoundsVerts.Add(newVert);
            }
        }
        

        /* Using all the verts pertinent to the camera's bounds, calculate the minimum bounds to feature all the verts */
        for(int i = 0; i < cameraBoundsVerts.Count; i++) {
            vert = (Vector3) cameraBoundsVerts[i];

            if(bottomBound > vert.y) {
                bottomBound = vert.y;
            }
            if(topBound < vert.y) {
                topBound = vert.y;
            }
            if(leftBound > vert.x) {
                leftBound = vert.x;
            }
            if(rightBound < vert.x) {
                rightBound = vert.x;
            }
        }

        /* Prevent the edges from going outside the screen's bouderies (dont let this run yet) */
        if(bottomBound < 0) {
            bottomBound = 0;
        }
        if(topBound > 1) {
            topBound = 1;
        }
        if(leftBound < 0) {
            leftBound = 0;
        }
        if(rightBound > 1) {
            rightBound = 1;
        }

        /* Set the bounding edges to the rect to return */
        boundingEdges.xMin = leftBound;
        boundingEdges.xMax = rightBound;
        boundingEdges.yMin = bottomBound;
        boundingEdges.yMax = topBound;
        
        return boundingEdges;
    }

    public bool MeshBehindMesh(MeshFilter meshFilter1, MeshFilter meshFilter2) {
        /*
         * Given the two mesh filters, return true if mesh1 is fully behind mesh2. This is used
         * in the case where a camera that is focusing on mesh2 ends up drawing mesh1, 
         * despite mesh1 getting culled/removed from the camera's view.
         * 
         * The meshes in question are expected to be rectangles used to draw portals.
         */
        bool behind = true;
        Mesh mesh1 = meshFilter1.mesh;
        Mesh mesh2 = meshFilter2.mesh;
        
        /* Create a plane using the first triangle of the second mesh */
        Plane meshPlane = new Plane(
                meshFilter2.transform.TransformPoint(mesh2.vertices[mesh2.triangles[0]]),
                meshFilter2.transform.TransformPoint(mesh2.vertices[mesh2.triangles[1]]),
                meshFilter2.transform.TransformPoint(mesh2.vertices[mesh2.triangles[2]]));

        /* Check if any vertex of mesh1 is in front of the meshPlane */
        for(int i = 0; i < mesh1.vertexCount && behind; i++) {
            if(meshPlane.GetSide(meshFilter1.transform.TransformPoint(mesh1.vertices[i]))) {
                behind = false;
            }
        }

        return behind;
    }

    public void AddFlareLayer() {
        /*
         * Add the flare layer to each of the cameras used by the portal
         */

        /* If the cameras have not yet been created, mark them to have a flare layer */
        if(recursiveCameras == null) {
            flareLayerToBeAdded = true;
        }

        /* Add the flare layer onto the already defined cameras */
        else {
            for(int i = 0; i < recursiveCameras.Length; i++) {
                if(recursiveCameras[i].GetComponent<FlareLayer>() == null) {
                    recursiveCameras[i].AddComponent<FlareLayer>();
                }
            }
        }
    }
}