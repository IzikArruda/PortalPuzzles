using UnityEngine;
using System.Collections;


[ExecuteInEditMode]
public class Scissors : MonoBehaviour {
    public Rect scissorRect = new Rect(0, 0, 1, 1);

    // Update is called once per frame
    void OnPreRender() {
        //SetScissorRect(GetComponent<Camera>(), scissorRect);
    }
}
