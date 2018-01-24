using UnityEngine;
using System.Collections;

/*
 * Create a set of stairs that connect the two points
 */
[ExecuteInEditMode]
public class StairsCreator : MonoBehaviour {

    /* The two points to connect witha set of stairs */
    public GameObject point1;
    public GameObject point2;

    /* The parent that holds all stair pieces */
    public GameObject stairsContainer;

    /* The GameObjects that make up the stairs */
    public GameObject[] stairs;

    public bool updateStairs;


    /* -------- Built-in Unity Functions ---------------------------------------------------- */
    
    void Update() {
        /*
         * Update the stairs if told to do so
         */

        if(updateStairs) {
            UpdateStairs();
            updateStairs = false;
        }
    }


    /* -------- Update Functions ---------------------------------------------------- */

    void UpdateStairs() {
        /*
         * Re-create the stairs
         */

        /* Create the stairs container if needed */
        if(stairsContainer == null) {
            CreateEmptyObject(ref stairsContainer, "Stairs", transform);
        }

        /* Get the rotation that directs a vector from point 1 to 2 */
        /* Get the distance and direction that goes from point 1 to point 2 */
        Vector3 direction = (point2.transform.position - point1.transform.position).normalized;
        float distance = (point2.transform.position - point1.transform.position).magnitude;
        

        
        /* Re-create the array for the stairs */
        CreateObjectsArray(ref stairs, 4, new Vector3(0, 0, 0));
        int index = 0;

        /* Top of the stairs */
        CreateEmptyObject(ref stairs[index], "Point1 Circle", stairsContainer.transform);
        stairs[index].transform.position = point1.transform.position;
        index++;

        /* 1st third of the stairs */
        CreateEmptyObject(ref stairs[index], "First 3rd Circle", stairsContainer.transform);
        stairs[index].transform.position = point1.transform.position + direction*distance*0.33f;
        index++;

        /* 2nd third of the stairs */
        CreateEmptyObject(ref stairs[index], "Second 3rd Circle", stairsContainer.transform);
        stairs[index].transform.position = point2.transform.position - direction*distance*0.33f;
        index++;

        /* Bottom of the stairs */
        CreateEmptyObject(ref stairs[index], "Point2 Circle", stairsContainer.transform);
        stairs[index].transform.position = point2.transform.position;
        index++;
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    public void CreateObjectsArray(ref GameObject[] objects, int size, Vector3 position) {
        /*
         * Re-create the given array of gameObjects
         */

        /* Ensure each object is empty before creating now ones */
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i] != null) {
                DestroyImmediate(objects[i]);
            }
        }

        /* Create a new array with the new given size if needed */
        if(objects.Length != size) { objects = new GameObject[size]; }
    }
    
    public void CreateEmptyObject(ref GameObject gameObject, string name, Transform parent) {
        /*
         * Create an empty object, resetting their local position.
         */

        //gameObject = new GameObject();
        gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = new Vector3(0, 0, 0);
        gameObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

}
