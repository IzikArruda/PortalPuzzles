using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MeshCombiner : MonoBehaviour {

    public bool combineMeshes;
	
	// Update is called once per frame
	void Update () {


        if(combineMeshes) {

            /* Combine the list of meshes */
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length]; int i = 0;
            while(i < meshFilters.Length) {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false);
                i++;
            }
            transform.GetComponent<MeshFilter>().mesh = new Mesh();
            transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
            transform.gameObject.SetActive(true);

            Debug.Log("ADDED");
            combineMeshes = false;
        }
	}
}
