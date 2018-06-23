using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class StartupTester : MonoBehaviour {

    public TextMesh text;
    
	void Start () {
        text.text = "" + Time.realtimeSinceStartup;
    }

    void Update() {
        /*
         * Reload the testArea on a button press
         */

        if(Input.GetKeyDown("f")) {

            /* Start loading the new scene */
            StartCoroutine(LoadGame());
        }

        /* Update the text with the current time */
        text.text = "" + Time.realtimeSinceStartup;
    }

    IEnumerator LoadGame() {

        AsyncOperation async = SceneManager.LoadSceneAsync("TestArea");

        // While the asynchronous operation to load the new scene is not yet complete, continue waiting until it's done.
        while(!async.isDone) {
            yield return null;
        }
    }
}
