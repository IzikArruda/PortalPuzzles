using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class CameraScript : MonoBehaviour {

    private int currentLayer;

    private ArrayList gameObjects;
    private ArrayList newTex;
    private ArrayList oldTex;


    void Start() {
        currentLayer = 0;

        gameObjects = new ArrayList();
        newTex = new ArrayList();
        oldTex = new ArrayList();
    }

    void OnPreRender() {
        currentLayer++;
        //Debug.Log(currentLayer + " Camera pre-render | " + transform.parent.name);

        /* Give the given gameObjects a new material for only this camera to render */
        GameObject go;
        for(int i = 0; i < gameObjects.Count; i++) {
            go = (GameObject) gameObjects[i];

            /* Set the portalTex property to be the saved texture */
            if(go.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_PortalTex")) {
                go.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_PortalTex", (Texture) newTex[i]);
            }
            //go.GetComponent<MeshRenderer>().material = (Material) newTex[i];
        }

    }
    void OnPostRender() {
        //Debug.Log(currentLayer + " Camera post-render" + transform.parent.name);
        currentLayer--;

        /* Give the gameObjects their original material back after they are finished rendering */
        GameObject go;
        for(int i = 0; i < gameObjects.Count; i++) {
            go = (GameObject) gameObjects[i];

            /* Set the portalTex property to be the saved texture */
            if(go.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_PortalTex")) {
                go.GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_PortalTex", (Texture) oldTex[i]);
            }
            //go.GetComponent<MeshRenderer>().material = (Material) oldTex[i];
        }

        /* Once the objects have had their mesh changed back to their original material, remove them */
        gameObjects.Clear();
        newTex.Clear();
        oldTex.Clear();
    }



    public void AssignMeshTo(GameObject objectToChange, Texture textureToBeUsed) {
        /*
         * Take note of a list of objects and their texture. These will be changed once the
         * camera this script is attached to renders it's scene
         */

        /* Extract the current texture from the given object's meshRenderer */
        Texture extractedTexture = null;
        if(objectToChange.GetComponent<MeshRenderer>().sharedMaterial.HasProperty("_PortalTex")) {
            extractedTexture = objectToChange.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_PortalTex");
        }

        /* Put the given objects and it's textures into their respective arrays to be set once drawn */
        gameObjects.Add(objectToChange);
        newTex.Add(textureToBeUsed);
        oldTex.Add(extractedTexture);

    }
}
