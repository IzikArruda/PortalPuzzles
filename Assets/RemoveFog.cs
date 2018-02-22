using UnityEngine;
using System.Collections;

/*
 * Attach this to an object that contains a camera component to remove the fog from the camera's rednering
 */
public class RemoveFog : MonoBehaviour {

    /* The state of the fog */
    private bool revertFogState;

    void OnPreRender() {
        /*
         * Save the fog's current state and disable it before the camera renders the scene
         */

        revertFogState = RenderSettings.fog;
        RenderSettings.fog = false;
    }

    void OnPostRender() {
        /*
         * Revert the fog's state back to what it was before the scene was rendered once the camera is done
         */

        RenderSettings.fog = revertFogState;
    }
}
