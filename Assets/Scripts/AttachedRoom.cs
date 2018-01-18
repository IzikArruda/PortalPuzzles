using UnityEngine;
using System.Collections;

/*
 * Track certain stats about a room for it to be properly position if being linked to a puzzle room
 */
public class AttachedRoom : MonoBehaviour {
    
    /* The bottom center of the room's exit. Used to connect rooms */
    public Transform exitPoint;

    /* The reset point of the room. Determines where the player will start. */
    public Transform resetPoint;

    /* The size of the exit of this room. Used by outside functions and requires user input to set. */
    public float exitWidth;
    public float exitHeight;


    public Transform ResetPlayer() {
        /*
         * Return the resetPoint of this room for the player to use as a reset point
         */

        return resetPoint;
    }
}