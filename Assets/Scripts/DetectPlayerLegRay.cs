using UnityEngine;
using System.Collections;

/*
 * When attached to an object, the player will call this script. This occurs when the player's leg ray trace
 * function collides with an object and checks for this script.
 */
public class DetectPlayerLegRay : MonoBehaviour {

    /* Depending on what value objectType is, the action to take once the player steps on this will be different */
    public int objectType;

    /* Values that are set and read by outside functions */
    public int returnValue;
    public GameObject partnerWindow;
    public Collider linkedCollider;


    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start() {
        returnValue = 0;
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void PlayerStep() {
        /*
         * Runs when the player steps on this object with any leg. Run a unique function depending on the value of objectType
         */

        if(objectType == -1) {
            /* Do nothing with this object type */
        }

        else if(objectType == 0) {
            ChangeStepSound();
        }

        else if(objectType == 1) {
            BreakGlass();
        }

        else {
            Debug.Log("WARNING: PLAYER STEP NOT HANDLED");
        }
    }

    public void ChangeStepSound() {
        /*
         * Used on an obect that will change how the player's footstep sounds like.
         * Return an integer that represents the index of the step sound. 
         * Which index relates to which sound can be found in the PlayerSounds script.
         */

        returnValue = 1;
    }

    public void BreakGlass() {
        /*
         * Destroy the linked glass objects
         */

        Debug.Log("Destroy the glass objects");

        /* Set this window and it's other window to inactive */
        partnerWindow.SetActive(false);
        partnerWindow.GetComponent<DetectPlayerLegRay>().objectType = -1;
        gameObject.SetActive(false);
        objectType = -1;

        /* Disable the linked wall's collision once the glass breaks to let the player fall through */
        linkedCollider.enabled = false;
    }
}
