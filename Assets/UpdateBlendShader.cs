using UnityEngine;
using System.Collections;

/*
 * Update the value of the texture blending shader each frame
 */
public class UpdateBlendShader : MonoBehaviour {

    public GameObject focusObject;

	void Start() {
        /*
         * Set the radius of the shader that specifies how far the texture needs to be
         * for the texture to fully blend.
         */

        /* Set the radius of when the texture starts blending */
        GetComponent<Renderer>().material.SetFloat("_BlendDistance", 1f);

        /* Set the distance of how far it takes for the texture to start blending */
        GetComponent<Renderer>().material.SetFloat("_BlendRate", 5f);
    }

	void Update () {
        /*
         * Update the values within the connected renderer's blending shader
         */

        /* Update the shader's camera position with this script's focus object */
        if(focusObject != null) {
            GetComponent<Renderer>().material.SetVector("_CameraPos", focusObject.transform.position);
        }
	}
}
