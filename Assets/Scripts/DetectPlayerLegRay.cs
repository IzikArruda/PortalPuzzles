using UnityEngine;
using System.Collections;

/*
 * When attached to an object, the player will call this script. This occurs when the player's leg ray trace
 * function collides with an object and checks for this script.
 */
public class DetectPlayerLegRay : MonoBehaviour {

    public void PlayerStep() {
        Debug.Log("Player stepped on " + gameObject.name);
    }

    public int ChangeStepSound() {
        /*
         * Used on an obect that will change how the player's footstep sounds like.
         * Return an integer that represents the index of the step sound. 
         * Which index relates to which sound can be found in the PlayerSounds script.
         */

        return 1;
    }
}
