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


    /* -------- Built-In Functions ---------------------------------------------------- */

    void Start() {
        returnValue = 0;
    }


    /* -------- Event Functions ---------------------------------------------------- */

    public void PlayerStep(GameObject playerObject) {
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
            BreakGlass(playerObject);
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

    public void BreakGlass(GameObject playerObject) {
        /*
         * Send a call to a function of the script that created the hit window.
         */
         
        /* From the glass object, get the StartingRoom it's a part of and run it's BreakGlass function */
        if(transform.parent != null) {
            if(transform.parent.parent != null) {
                if(transform.parent.parent.parent != null) {
                    if(transform.parent.parent.parent.GetComponent<StartingRoom>() != null) {
                        transform.parent.parent.parent.GetComponent<StartingRoom>().BreakGlass(playerObject);
                    }
                }
            }
        }
    }
}
