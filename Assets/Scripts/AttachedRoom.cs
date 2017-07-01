using UnityEngine;
using System.Collections;

/*
 * Track certain stats about a room for it to be properly position if being linked to a puzzle room
 */
public class AttachedRoom : MonoBehaviour {
    
    /* The bottom center of the room's exit */
    public Transform exitPoint;

    /* The width and height of the room's exitway */
    public float exitWidth;
    public float exitHeight;
}