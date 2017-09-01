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

public class PortalView : MonoBehaviour {

    /* A point positioned at the partner portal's location */
    public Transform pointB;

    /* The forward vector of the portal */
    public Vector3 faceNormal = Vector3.forward; 

    /* Quality of the texture rendered and applied to the portal mesh. 2048 is recommended. */
	public static int m_TextureSize = 2048;

    /* Offset to the camera's clipping plane when rendering. 0 is recommended */
    public float m_ClipPlaneOffset;

    /* Whether the portal is currently visible and being drawn to a camera */
    public bool beingDrawn = true;
        
    /* Materials used on the portalMesh */
    private Material portalMaterial;

    /* The ID of the portalSet that this portalMesh is a child of */
    public string portalSetID;

    /* The max viewing depth for recursive portal calls */
    private static int maxCameraDepth = 6;

    /* An array of cameras used for this portal's recursive portal rendering */
    private GameObject[] recursiveCameras;






    public Rect defaultRect = new Rect(0, 0, 1, 1);


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

                /* Keep the cameras in the editor after creation */
                recursiveCameras[i].hideFlags = HideFlags.DontSave;

                /* Update the renderTexture for this camera */
                UpdateRenderTexture(ref recursiveCameras[i].GetComponent<CameraScript>().renderTexture, 
                        recursiveCameras[i].GetComponent<Camera>());
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



        /* Print out some stuff for debuigging */
        if(camera.name != "SceneCamera" && camera.name != "Preview Camera" && cameraScript.scout != true) {
            //Debug.Log(name + " rendered using scout camera");

            /* Calculate the bounding edges this mesh has on the camera */
            SetRectDefault(camera);
            Rect boundingEdges = CalculateViewingRect(camera);
            SetScissorRect(camera, boundingEdges);
            


            /*
             * 
             * ALRIGHT SO CURRENTLY IT WORKS THROUGH PORTALS: IE THE RECT STAYS FROM PROTAL TO PORTAL.
             * ITS WORTH TESTING WITH MULTIPLE PORTALS NOW.
             * 
             * 
             * WHAT NEEDS TO BE FIXED:
             * 1.HANDLE WHEN A POINT OF THE MESH IS BEHIND THE CAMERA WHEN USING CalculateViewingRect
             * 
             * 
             */


        }















        /* Ensure the camera rendering this portal has the proper CameraScript */
        if(cameraScript) {

            /* Use the view of the first recursiveCamera to render the portal */
            if(!cameraScript.scout) {
                //Debug.Log(name + " getting rendered");
                RenderCameraView(camera, recursiveCameras[0].GetComponent<CameraScript>(), ref cameraView);
                camera.GetComponent<CameraScript>().AssignMeshTo(gameObject, cameraView);

            }
            /* If the camera is trying to render it' own parent portal, dont do it */
            else if(camera.transform.parent.GetInstanceID() == transform.GetInstanceID()) {
                //Debug.Log("CAMERA TRYING TO RENDER ITS OWN PORTAL DONT");
            }            
            /* Do not have a camera render a portal from it's own portalSetID */
            else if(camera.transform.parent.GetComponent<PortalView>().portalSetID == portalSetID) {
                //Debug.Log("Own portal set getting rendered");
            }
            /* If the camera is trying to render a portal behind the exit point, dont do it */
            else if(!camera.transform.parent.GetComponent<PortalView>().checkPortalVisibility(camera.transform)) {
                //Debug.Log("Camera is trying to render a portal that it cannot see");
            }
            /* Use the scoutCamera to render it's view */
            else {

                //Debug.Log(camera.transform.parent.name + " RENDERING " + name);

                /* Use the next camera down the recursiveCameras list */
                if(cameraScript.cameraDepth >= 0 && cameraScript.cameraDepth < maxCameraDepth-1) {
                    RenderCameraView(camera, recursiveCameras[cameraScript.cameraDepth + 1].GetComponent<CameraScript>(), ref cameraView);
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
    

    /* -------- Camera Rendering Functions ---------------------------------------------------- */

    public void RenderCameraView(Camera viewingCamera, CameraScript cameraScript, ref Texture extractedView) {
        /*
         * When this portalMesh is about to be rendered, position the scout camera relative to the 
         * partner portal's location and the viewingCamera's position then render the image onto the extractedView.
         * 
         * Have a bunch of prerequisites check for any unset values before doing any rendering.
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
        if(!checkPortalVisibility(viewingCamera.transform)) {
            return;
        }

        /* Extract the scouting camera from the cameraScript */
        Camera scoutingCamera = cameraScript.GetComponent<Camera>();

        /* Ensure the renderTexture for the scoutCamera is properly created */
        UpdateRenderTexture(ref cameraScript.renderTexture, scoutingCamera);

        /* Check if the scouting camera exists along with it's renderTexture */
        if(!scoutingCamera || !cameraScript.renderTexture) {
            return;
        }

        /* Set up values to properly render the camera */
        Vector3 pos = transform.position;
        Vector3 normal = transform.TransformDirection(faceNormal);

        
        // this will make it depend on the points' position, rotation, and scale
        scoutingCamera.transform.position = pointB.TransformPoint(transform.InverseTransformPoint(viewingCamera.transform.position));
        scoutingCamera.transform.rotation = Quaternion.LookRotation(
                pointB.TransformDirection(transform.InverseTransformDirection(viewingCamera.transform.forward)),
                pointB.TransformDirection(transform.InverseTransformDirection(viewingCamera.transform.up)));

        /* Reset the projection matrix of this camera */
        SetRectDefault(scoutingCamera);

        // I don't know how this works it just does, I got lucky
        Vector4 clipPlane = CameraSpacePlane(viewingCamera, pos, normal, -1.0f);
        Matrix4x4 projection = viewingCamera.CalculateObliqueMatrix(clipPlane);
        scoutingCamera.projectionMatrix = projection;


        /*
         * Apply a new projection that limits the cameras view
         */
        Rect boundingEdges = CalculateViewingRect(viewingCamera);
        SetScissorRect(scoutingCamera, boundingEdges);
        //SetRectDefault(camera);
        //Rect boundingEdges = CalculateViewingRect(camera);

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
    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
        Vector3 offsetPos = pos + normal * -m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }


    /* -------- Event Functions ---------------------------------------------------- */

    private GameObject CreateScoutCamera() {
        /*
         * Create a gameObject with a camera component with a cameraScript to be used as a scoutCamera for this portal.
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
        cameraParent.GetComponent<CameraScript>().portalSetID = portalSetID;
        cameraParent.GetComponent<CameraScript>().scout = true;

        return cameraParent;
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




    public Rect CalculateViewingRect(Camera camera) {
        /*
         * Given a camera and this mesh, calculate the bounding rect that this mesh has for this camera.
         * 
         * The edges are ranged from 0-1 with (0, 0) being bottom-left. increasing X goes right, increasing Y goes up.
         */
        Rect boundingEdges = new Rect();
        Vector3 vert;
        ArrayList verticesBehindCamera = new ArrayList();
        ArrayList cameraBoundsVerts = new ArrayList();

        /* Get all the points used to define this portal mesh being drawn */
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;

        /* Take each vertex that forms this mesh and find it's pixel position on the camera's screen */
        for(int i = 0; i < vertices.Length; i++) {
            //Convert the vert to world space
            vertices[i] = transform.TransformPoint(vertices[i]);
            //Convert it to a position on the camera's view
            vertices[i] = camera.WorldToViewportPoint(vertices[i]);
        }

        /* Get the bounding edges of the mesh on the camera's view  */
        float mostBottom = 1;
        float mostTop = 0;
        float mostLeft = 1;
        float mostRight = 0;
        ArrayList verticesOutsideView = new ArrayList();
        for(int i = 0; i < vertices.Length; i++) {
            vert = vertices[i];

            /* Track the index of each vert that is not within the camera's view */
            if(vert.z < 0 || vert.x < 0 || vert.x > 1 || vert.y < 0 || vert.y > 1) {
                verticesOutsideView.Add(i);
            }

            /* If this vert is fully in the camera's view, add it to the boundsVert list immediatly */
            else {
                cameraBoundsVerts.Add(vert);
            }
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
                    /* Get two rays that define both the 0-1 and 0-3 edges */
                    edge1 = new Ray(worldVertices[1], worldVertices[0] - worldVertices[1]);
                    edge2 = new Ray(worldVertices[3], worldVertices[0] - worldVertices[3]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[0] - worldVertices[1]).magnitude;
                    distance2 = (worldVertices[0] - worldVertices[3]).magnitude;
                }

                /* If the vertex at index 1 is offscreen, use the two edges between 0 and 2 */
                else if((int) verticesOutsideView[i] == 1) {
                    Debug.Log("INDEX 1 OFF");
                    //Debug.DrawRay(worldVertices[0], worldVertices[1] - worldVertices[0], Color.blue, 0.5f);
                    //Debug.DrawRay(worldVertices[2], worldVertices[1] - worldVertices[2], Color.blue, 0.5f);
                    /* Get two rays that define both the 1-0 and 1-2 edges */
                    edge1 = new Ray(worldVertices[0], worldVertices[1] - worldVertices[0]);
                    edge2 = new Ray(worldVertices[2], worldVertices[1] - worldVertices[2]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[1] - worldVertices[0]).magnitude;
                    distance2 = (worldVertices[1] - worldVertices[2]).magnitude;

                }

                /* If the vertex at index 2 is offscreen, use the two edges between 1 and 3 */
                else if((int) verticesOutsideView[i] == 2) {
                    Debug.Log("INDEX 2 OFF");
                    //Debug.DrawRay(worldVertices[1], worldVertices[2] - worldVertices[1], Color.blue, 0.5f);
                    //Debug.DrawRay(worldVertices[3], worldVertices[2] - worldVertices[3], Color.blue, 0.5f);
                    /* Get two rays that define both the 2-1 and 2-3 edges */
                    edge1 = new Ray(worldVertices[1], worldVertices[2] - worldVertices[1]);
                    edge2 = new Ray(worldVertices[3], worldVertices[2] - worldVertices[3]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[2] - worldVertices[1]).magnitude;
                    distance2 = (worldVertices[2] - worldVertices[3]).magnitude;
                }

                /* If the vertex at index 3 is offscreen, use the two edges between 0 and 2 */
                else if((int) verticesOutsideView[i] == 3) {
                    Debug.Log("INDEX 3 OFF");
                    //Debug.DrawRay(worldVertices[0], worldVertices[3] - worldVertices[0], Color.blue, 0.5f);
                    //Debug.DrawRay(worldVertices[2], worldVertices[3] - worldVertices[2], Color.blue, 0.5f);
                    /* Get two rays that define both the 3-0 and 3-2 edges */
                    edge1 = new Ray(worldVertices[0], worldVertices[3] - worldVertices[0]);
                    edge2 = new Ray(worldVertices[2], worldVertices[3] - worldVertices[2]);
                    /* Get the distance between each vertex for both edges/rays */
                    distance1 = (worldVertices[3] - worldVertices[0]).magnitude;
                    distance2 = (worldVertices[3] - worldVertices[2]).magnitude;
                }
                else {
                    Debug.Log("WANING: I THINK THE MESH USED HAS MORE THAN 4 VERTS?");
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
                        /* The ray collided with this plane within the proper distance/edge length */
                        //Debug.Log("edge hit top " + rayDistance);
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
                /* Raycast for the bottom plane */
                if(camRightPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        /* The ray collided with this plane within the proper distance/edge length */
                        //Debug.Log("edge hit right " + rayDistance);
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
                /* Raycast for the left plane */
                if(camLeftPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        /* The ray collided with this plane within the proper distance/edge length */
                        //Debug.Log("edge hit left " + rayDistance);
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
                /* Raycast for the right plane */
                if(camBottomPlane.Raycast(edgeRay, out rayDistance)) {
                    if(rayDistance >= 0 && rayDistance <= edgeDistance) {
                        /* The ray collided with this plane within the proper distance/edge length */
                        //Debug.Log("edge hit bottom " + rayDistance);
                        newVert = edgeRay.origin + edgeRay.direction*rayDistance;
                        newWorldVerts.Add(newVert);
                    }
                }
            }


            /*
             * Now we have a list of all possible collision points for the camera, but in world positions.
             * 
             * First, transform them into vert positions on the camera
             * 
             * Then, remove any that are outside the camera's bounderies
             * 
             */

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
        
        /* Draw all the verts that will be used */
        for(int i = 0; i < cameraBoundsVerts.Count; i++) {
            vert = camera.ViewportToWorldPoint((Vector3) cameraBoundsVerts[i]);
            Debug.DrawLine(camera.transform.position, vert, Color.blue, 1f);
        }

        /* Using all the verts pertinent to the camera's bounds, calculate the minimum bounds to feature all the verts */
        for(int i = 0; i < cameraBoundsVerts.Count; i++) {
            vert = (Vector3) cameraBoundsVerts[i];

            if(mostBottom > vert.y) {
                mostBottom = vert.y;
            }
            if(mostTop < vert.y) {
                mostTop = vert.y;
            }
            if(mostLeft > vert.x) {
                mostLeft = vert.x;
            }
            if(mostRight < vert.x) {
                mostRight = vert.x;
            }
        }













        /* Print out the index of each vertex that is behind the camera */
        for(int i = 0; i < verticesBehindCamera.Count; i++) {
            Debug.Log("POINT INDEX OF " + verticesBehindCamera[i] + " IS BEHIND CAMERA");
        }




        /*  If there exists a vertex behind the camera, go through extra calcualtions to get it's bounderies */
        if(verticesBehindCamera.Count > 0) {

            /* Create a temp array to hold the remaining vertices that have not found a connection */
            ArrayList remainingVertices = (ArrayList) verticesBehindCamera.Clone();

            /* Search the mesh's triangles for all connections between a vertex behind the camera and a vertex infront of the camera */
            int[] triangles = GetComponent<MeshFilter>().mesh.triangles;
            ArrayList connectionInfront = new ArrayList();
            ArrayList connectionBehind = new ArrayList();
            int backCount;
            for(int i = 0; i < triangles.Length; i += 3) {
                Debug.Log("triangle " + triangles[i] + triangles[i+1] + triangles[i+2]);
                backCount = 0;

                /* Check if this triangle set uses any of the remainingVertices  */
                if(remainingVertices.Contains(triangles[i]) || remainingVertices.Contains(triangles[i+1]) || remainingVertices.Contains(triangles[i+2])) {
                    
                    /* Check if this triangle set is a contender for a connection (1infront/2behind OR 2infront/1behind) */
                    if(verticesBehindCamera.Contains(triangles[i])) {
                        backCount++;
                    }
                    if(verticesBehindCamera.Contains(triangles[i+1])) {
                        backCount++;
                    }
                    if(verticesBehindCamera.Contains(triangles[i+2])) {
                        backCount++;
                    }

                    /* This triangle set has a viable connection */
                    if(backCount == 1 || backCount == 2) {

                        /* Find the connections for each remaining vertex */
                        for(int ii = 0; ii < remainingVertices.Count; ii++) {
                            int currVer = (int) remainingVertices[ii];
                            int verToAdd = -1;


                            /*
                             * Track each point that is not within view. Each point not visible will 
                             * do it's own collisions checks between it's neighbor points, which will 
                             * always be the same ones due to a RESTRICTION PUT ONTO PORTAL MESHES - THEY
                             * MUST ALWAYS BE MADE BY THE SCRIPT AS RECTANGLES.
                             * 
                             * Anyway each vertex checks for collisons between them and their partner if they collide
                             * with the camera's plane edges/borders. A collision means that collision point
                             * is the edge of the screen, so we use that as a vert when calculating the bounds of the rect.
                             */


                            if(triangles[i] == currVer) {
                                if(!verticesBehindCamera.Contains(triangles[i+1])) {
                                    //0-1 connection is viable
                                    verToAdd = i+1;
                                    connectionBehind.Add(currVer);
                                    connectionInfront.Add(triangles[verToAdd]);
                                }
                                if(!verticesBehindCamera.Contains(triangles[i+2])) {
                                    //0-2 connection is viable
                                    verToAdd = i+2;
                                    connectionBehind.Add(currVer);
                                    connectionInfront.Add(triangles[verToAdd]);
                                }
                            }
                            else if(triangles[i+1] == currVer) {
                                if(!verticesBehindCamera.Contains(triangles[i])) {
                                    //1-0 connection is viable
                                    verToAdd = i;
                                    connectionBehind.Add(currVer);
                                    connectionInfront.Add(triangles[verToAdd]);
                                }
                                if(!verticesBehindCamera.Contains(triangles[i+2])) {
                                    //1-2 connection is viable
                                    verToAdd = i+2;
                                    connectionBehind.Add(currVer);
                                    connectionInfront.Add(triangles[verToAdd]);
                                }
                            }
                            else if(triangles[i+2] == currVer) {
                                if(!verticesBehindCamera.Contains(triangles[i])) {
                                    //2-0 connection is viable
                                    verToAdd = i;
                                    connectionBehind.Add(currVer);
                                    connectionInfront.Add(triangles[verToAdd]);
                                }
                                if(!verticesBehindCamera.Contains(triangles[i+1])) {
                                    //2-1 connection is viable
                                    verToAdd = i+1;
                                    connectionBehind.Add(currVer);
                                    connectionInfront.Add(triangles[verToAdd]);
                                }
                            }

                            /* If a connection was found, verToAdd would be equal to the frontVertex */
                            if(verToAdd != -1) {
                                connectionBehind.Add(currVer);
                                connectionInfront.Add(triangles[verToAdd]);
                            }
                        }
                    }
                }
            }

            /* Remove any duplicate connections */
            for(int i = 0; i < connectionBehind.Count; i++) {
                for(int ii = i+1; ii < connectionBehind.Count; ii++) {

                    /* If two entries have the same front and behind indexes, remove it */
                    if((int) connectionBehind[ii] == (int) connectionBehind[i]) {
                        if((int) connectionInfront[ii] == (int) connectionInfront[i]) {
                            connectionBehind.RemoveAt(ii);
                            connectionInfront.RemoveAt(ii);
                            ii--;
                        }
                    }
                }
            }

            /* For each connection, get both vertices' viewport positions */
            Debug.Log(connectionBehind.Count + " Connections");
            Vector3 behindVert, infrontVert;
            /* Get the planes that define the camera's viewing edges */
            Plane camTopPlane = new Plane(camera.transform.position,
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(1, 1, 1)),
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(0, 1, 1)));
            Plane camBottomPlane = new Plane(camera.transform.position,
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(0, 0, 1)),
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(1, 0, 1)));
            Plane camLeftPlane = new Plane(camera.transform.position,
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(0, 1, 1)),
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(0, 0, 1)));
            Plane camRightPlane = new Plane(camera.transform.position,
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(1, 0, 1)),
                    camera.transform.position + camera.ViewportToScreenPoint(new Vector3(1, 1, 1)));

            for(int i = 0; i < connectionBehind.Count; i++) {
                behindVert = camera.ViewportToWorldPoint(vertices[(int) connectionBehind[i]]);
                infrontVert = camera.ViewportToWorldPoint(vertices[(int) connectionInfront[i]]);
                //behindVert = vertices[(int) connectionBehind[i]];
                //infrontVert = vertices[(int) connectionInfront[i]];

                /* Idea: make a plane that is the top fultrum of the camera's view, then check for the 
                 * point of collision with the line formed by the connection.
                 * 
                 * The plane can be made using the cams pos, it's topleft and topright points using viewpoint to world */
                 /*
                  * TO EXPAND ON TYHIS: TRACK ANY VERT THAT GOES OFFSCREEN. THIS WILL REQUIRE CHANGING A LOT THO
                  */

                /* Draw the line that defines this connection */
                Debug.DrawLine(infrontVert, behindVert, Color.blue, 1f);

                /* Get the point of collision where the connection hits the top plane */
                Ray connectionRay = new Ray(infrontVert, behindVert);
                float distance;
                if(camTopPlane.Raycast(connectionRay, out distance)) {
                    Debug.Log("hit plane, " + distance);
                }

                //Assume the connection goes off the top
                Debug.Log(infrontVert.y + " | " + behindVert.y);
            }

        }












        /* Prevent the edges from going outside the screen's bouderies (dont let this run yet) */
        if(mostBottom < 0) {
            //mostBottom = 0;
        }
        if(mostTop > camera.pixelHeight) {
            //mostTop = camera.pixelHeight;
        }
        if(mostLeft < 0) {
            //mostLeft = 0;
        }
        if(mostRight > camera.pixelWidth) {
            //mostRight = camera.pixelWidth;
        }

        /* Set the bounding edges to the rect to return */
        boundingEdges.xMin = mostLeft;
        boundingEdges.xMax = mostRight;
        boundingEdges.yMin = mostBottom;
        boundingEdges.yMax = mostTop;

        return boundingEdges;
    }
    

    public static Vector2 WorldToGUIPoint(Vector3 world) {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(world);
        screenPoint.y = (float) Screen.height - screenPoint.y;
        return screenPoint;
    }


    public void SetScissorRect(Camera cam, Rect r) {
        /*
         * Apply an additive projection matrix to the given camera
         * 
         * https://forum.unity3d.com/threads/scissor-rectangle.37612/
         */
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
        //		print( r );

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
        Matrix4x4 m1 = Matrix4x4.TRS(new Vector3(r.x, r.y, 0), Quaternion.identity, new Vector3(r.width, r.height, 1));
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
         * Set the rect of the given camera to be back to the default so SetScissorRect can be recalled
         */

        cam.rect = new Rect(0, 0, 1, 1);
        cam.ResetProjectionMatrix();
    }

}