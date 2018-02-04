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
}
