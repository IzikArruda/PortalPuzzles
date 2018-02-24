using UnityEngine;
using System.Collections;

/*
 * Creates and updates the skySphere that is used in the outside terrain of the game.
 * Attach this script to a sphere primitive.
 */
public class SkySphere : MonoBehaviour {



	void Start () {
        /*
         * Once this script is created and attached onto a sphere primitive, ensure it's properly sized.
         */
        gameObject.name = "Sky sphere";

        /* Remove the primitive's sphere collider */
        DestroyImmediate(GetComponent<SphereCollider>());

        /* Set the sphere's sizes to match the player's render distance */
        float renderDist = CustomPlayerController.cameraFarClippingPlane;
        transform.localScale = new Vector3(renderDist*0.99f, renderDist*0.99f, renderDist*0.99f);

        /* flip all it's triangles of the sphere to have it inside out */
        int[] triangles = GetComponent<MeshFilter>().sharedMesh.triangles;
        if(triangles[0] == 0) {
            int tempInt;
            for(int i = 0; i < triangles.Length; i += 3) {
                tempInt = triangles[i + 0];
                triangles[i + 0] = triangles[i + 2];
                triangles[i + 2] = tempInt;
            }
            GetComponent<MeshFilter>().sharedMesh.triangles = triangles;
        }

        /* Set the fog of the world. This should be a sepperate function, but for now we can leave it in here */
        float playerViewLength = 0.5f*CustomPlayerController.cameraFarClippingPlane;
        //Have the fog start halfway from the player and their max view
        RenderSettings.fogStartDistance = playerViewLength*0.5f;
        //Have the fog end before the last 25% of the player's view
        RenderSettings.fogEndDistance = playerViewLength*0.75f;
    }
	
    public void ApplyTexture(Texture2D skySphereTexture) {
        /*
         * Given a texture, apply it to the skysphere's material.
         */

        Material skySphereMaterial = new Material(Shader.Find("Unlit/Fogless"));
        skySphereMaterial.SetTexture("_MainTex", skySphereTexture);
        GetComponent<MeshRenderer>().sharedMaterial = skySphereMaterial;
    }


    public void UpdateSkySpherePosition(Vector3 focusPointPosition) {
        /*
         * Have the skySphere reposition given the new focusPointPosition
         */

        /* Reposition the sky sphere at the given window exit point */
        transform.position = focusPointPosition;
    }
}
