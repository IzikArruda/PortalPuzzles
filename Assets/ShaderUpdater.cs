using UnityEngine;
using System.Collections;

/*
 * Attach this to an object with a cubeCreator which uses a material using the CustomUnlit shader.
 */
 [ExecuteInEditMode]
public class ShaderUpdater : MonoBehaviour {
    
	
	void Update () {
        GetComponent<MeshRenderer>().sharedMaterial.SetVector("_BoxPos", transform.position);
	}
}
