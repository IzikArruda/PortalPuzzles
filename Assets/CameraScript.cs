using UnityEngine;
using System.Collections;

/*
 * A script to be placed on all gameObjects that have a Camera component attached. It allows
 * the camera to properly render portalMeshes by placing an order of textures that get 
 * rendered to a list of given portals each frame.
 */
[ExecuteInEditMode]
public class CameraScript : MonoBehaviour {
    

    /* The type of camera this is, default non-scout (scout being used with portals to recursivly render) */
    public bool scout = false;

    public int cameraDepth;
    public string portalSetID;

    public ArrayList gameObjects;
    public ArrayList newTex;
    public ArrayList oldTex;

    public RenderTexture renderTexture;


    public void Start() {
        
        gameObjects = new ArrayList();
        newTex = new ArrayList();
        oldTex = new ArrayList();
    }

    public void OnDestroy() {
        /*
         * When this camera is destroyed, destroy the camera's renderTexture
         */

        DestroyImmediate(renderTexture);
    }


    void OnPreRender() {

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
