/*
 * Thanks to Aras Pranckevicius' MirrorReflection4
 * http://wiki.unity3d.com/index.php/MirrorReflection4 
 * 
 * The cameras used to render the texture of the portals cannot recieve shadows. This means if we
 * want to have a light cast shadows, there should be no way this light can cast a shadow
 * that is visible from a portal.
 */

using UnityEngine;
using System.Collections;

namespace BLINDED_AM_ME{

	[ExecuteInEditMode]
	public class PortalView : MonoBehaviour {


        public float left = -0.2F;
        public float right = 0.2F;
        public float top = 0.2F;
        public float bottom = -0.2F;

        public Transform pointB;
        public Camera scoutCamera;
        public Vector3  faceNormal = Vector3.forward; // relative to self


		public int m_TextureSize;
		public float m_ClipPlaneOffset;

		private RenderTexture m_PortalTexture = null;
		private int m_OldPortalTextureSize = 0;

        public bool beingDrawn = true;
        public Material portalMaterial;
        public Material emptyMaterial;

		private static bool s_InsideRendering = false;
        


		public void OnWillRenderObject()
		{

			if(!enabled || !scoutCamera || !pointB)
				return;

			Camera cam = Camera.current;
			if( !cam )
				return;


            // Safeguard from recursive reflections.        
            if( s_InsideRendering )
				return;
			s_InsideRendering = true;


			var rend = GetComponent<Renderer>();
			if (!enabled || !rend || !rend.sharedMaterial || !rend.enabled)
				return;

            //Check if the portal is visible
            checkPortalVisibility(cam.transform);


            CreateNeededObjects();


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

            if(!scoutCamera.enabled){ // make it manual
				scoutCamera.Render();
			}else
				scoutCamera.enabled = false;


			Material[] materials = rend.sharedMaterials;
			foreach( Material mat in materials ) {
				if( mat.HasProperty("_PortalTex") )
					mat.SetTexture( "_PortalTex", m_PortalTexture );
			}

			s_InsideRendering = false;
		}


		// Aras Pranckevicius MirrorReflection4
		// http://wiki.unity3d.com/index.php/MirrorReflection4 
		// Cleanup all the objects we possibly have created
		void OnDisable()
		{
			if( m_PortalTexture ) {
				DestroyImmediate( m_PortalTexture );
				m_PortalTexture = null;
			}
		}

		// Aras Pranckevicius MirrorReflection4
		// http://wiki.unity3d.com/index.php/MirrorReflection4 
		// On-demand create any objects we need
		private void CreateNeededObjects()
		{

			// Reflection render texture
			if( !m_PortalTexture || m_OldPortalTextureSize != m_TextureSize )
			{
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
		private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * -m_ClipPlaneOffset;
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 cpos = m.MultiplyPoint( offsetPos );
			Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
			return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
		}
			

        void checkPortalVisibility(Transform cam) {
            /*
             * Return true if a camera at the given location can view the portal's rendered side.
             * Positive side means the player can see the portal. 
             * 
             * We need to change the use of this function - When it's called, it should not bother doing anything with the portal
             * because the rendered mesh to display the portal will be disabled.
             * 
             * Later, we might ad a check to see if the player can even see the portal with their current FoV.
             */

            /* Create a plane that will define the portal */
            Vector3 planeNormal = transform.TransformDirection(faceNormal);
            Plane triggerPlane = new Plane(planeNormal, transform.position);
            
            /* Detect if the cam is on the other side of the portal */
            if(triggerPlane.GetSide(cam.transform.position)) {
                swapMaterial(true);
            }
            else {
                swapMaterial(false);
            }
        }

        void swapMaterial(bool isVisible) {
            /*
             * Swap the portal's current material and the isVisible boolean between 
             * the proper portal material and an empty material depending on the given boolean.
             * This is to keep the portal invisible if the player is on the wrong side
             */

            /* Create a new portal texture if the portal has not been assigned one yet */
            if(portalMaterial == null) {
                portalMaterial = new Material(Shader.Find("Unlit/Portal"));
                portalMaterial.name = "__PortalMaterial" + GetInstanceID();
            }

            if(isVisible && !beingDrawn) {
                /* Switch the portal's material to be the proper portal material for the player to see */
                beingDrawn = true;
                GetComponent<MeshRenderer>().material = portalMaterial;
            }
            else if(!isVisible && beingDrawn) {
                /* The portal is currently visible, so swap the active material to be invisible */
                beingDrawn = false;
                GetComponent<MeshRenderer>().material = emptyMaterial;
            }
        }



    }
}