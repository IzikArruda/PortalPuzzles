using UnityEngine;
using System.Collections;

/*
 * Create a set of stairs that connect a set of points. Each set contains two points.
 */
[ExecuteInEditMode]
public class StairsCreator : MonoBehaviour {

    /* The two points to connect witha set of stairs. Each point has  */
    public GameObject set1point1;
    public GameObject set1point2;
    public GameObject set2point1;
    public GameObject set2point2;

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
         * Re-create the stairs using the 4 points of the stairs
         */

        /* Create the stairs container if needed */
        if(stairsContainer == null) {
            CreateEmptyObject(ref stairsContainer, "Stairs", transform);
        }

        /* Get the direction for both sets to form the stairs */
        Vector3 difference1 = set2point1.transform.position - set1point1.transform.position;
        Vector3 difference2 = set2point2.transform.position - set1point2.transform.position;
        Vector3 direction1 = (difference1).normalized;
        Vector3 direction2 = (difference2).normalized;
        float distance1 = (difference1).magnitude;
        float distance2 = (difference2).magnitude;
        

        
        /* Re-create the array for the stairs */
        CreateObjectsArray(ref stairs, 6, new Vector3(0, 0, 0));
        int index = 0;

        /* Top of the stairs set1 */
        CreateEmptyObject(ref stairs[index], "S1P1 Circle", stairsContainer.transform);
        stairs[index].transform.position = set1point1.transform.position;
        index++;
        /* Top of the stairs set2 */
        CreateEmptyObject(ref stairs[index], "S1P2 Circle", stairsContainer.transform);
        stairs[index].transform.position = set1point2.transform.position;
        index++;

        /* Middle of the stairs set1 */
        CreateEmptyObject(ref stairs[index], "P1 midway Circle", stairsContainer.transform);
        stairs[index].transform.position = set1point1.transform.position + direction1*distance1*0.5f;
        index++;
        /* Middle of the stairs set2 */
        CreateEmptyObject(ref stairs[index], "P2 midway Circle", stairsContainer.transform);
        stairs[index].transform.position = set1point2.transform.position + direction2*distance2*0.5f;
        index++;

        /* Bottom of the stairs set1 */
        CreateEmptyObject(ref stairs[index], "Point2 Circle", stairsContainer.transform);
        stairs[index].transform.position = set2point1.transform.position;
        index++;
        /* Bottom of the stairs set2 */
        CreateEmptyObject(ref stairs[index], "Point2 Circle", stairsContainer.transform);
        stairs[index].transform.position = set2point2.transform.position;
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
