using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/*
 * Handle the start-up sequence of the game. It starts a coroutine of loading the actual scene of the game.
 */
public class StartupTester : MonoBehaviour {

    /* The camera used to render the scene */
    public Camera sceneCamera;

    /* The boxes that are animated during the loading scene */
    public GameObject loadingBox1;
    public GameObject loadingBox2;
    public GameObject loadingBox3;

    /* Values used with timings of the loading animation */
    float startTime = 0;
    float startingAnimationTiming = 2;
    float box2StepCount = 18;
    float box3StepCount = 7;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start () {

        /* Track when the scene has loaded */
        startTime = Time.realtimeSinceStartup;
    }

    void Update() {
        /*
         * Update the loading boxes as we wait for the scene to load
         */

        /* Check if we want to load the scene. This is used for debugging as we want to stay on this screen */
        if(Input.GetKeyDown("f")) {
            /* Start loading the new scene */
            StartCoroutine(LoadGame());
        }

        /* Position the loading boxes into their default position, relative to the screen size */
        PositionBoxes();


        /* Make sure the boxes start off-screen and animate into view */
        if(Time.realtimeSinceStartup < startTime + startingAnimationTiming) {
            AnimateBoxesIntro();
        }

        /* Animate the boxes relative to the current game time */
        AnimateBox1();
        AnimateBox2();
        AnimateBox3();
    }


    /* -------- Event Functions ---------------------------------------------------- */

    void PositionBoxes() {
        /*
         * Position the loading boxes on the bottom-right side of the screen
         */
        Vector3 bottomRight = sceneCamera.ViewportToWorldPoint(new Vector3(1, 0, 10));

        /* Position the boxes */
        loadingBox1.transform.position = bottomRight + new Vector3(-1, 1, 0);
        loadingBox2.transform.position = bottomRight + new Vector3(-3, 1, 0);
        loadingBox3.transform.position = bottomRight + new Vector3(-5, 1, 0);
    }

    void AnimateBoxesIntro() {
        /*
         * Animate the boxes slidding into view
         */
        float animationTiming = (Time.realtimeSinceStartup - startTime) / startingAnimationTiming;
        Vector3 animation = Vector3.zero;
        float entryHeight = 5;

        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*animationTiming)), 0);
        loadingBox1.transform.position += animation;

        /* Use steps for the other two cubes */
        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*Mathf.FloorToInt((animationTiming)*box2StepCount)/box2StepCount)), 0);
        loadingBox2.transform.position += animation;
        
        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*Mathf.FloorToInt((animationTiming)*box3StepCount)/box3StepCount)), 0);
        loadingBox3.transform.position += animation;
    }

    void AnimateBox1() {
        /*
         * Animate box 1 as it moves up and down
         */
        float animationMaxTime = 1.25f;
        float time = Time.realtimeSinceStartup % animationMaxTime;
        Vector3 animation = Vector3.zero;

        animation = new Vector3(0, 1 + Mathf.Cos(Mathf.PI*2*time/animationMaxTime), 0);

        loadingBox1.transform.position += animation;
    }

    void AnimateBox2() {
        /*
         * Animate box2 moving up and down but with steps in the timing
         */
        float animationMaxTime = 1.25f;
        float time = Time.realtimeSinceStartup % animationMaxTime;
        Vector3 animation = Vector3.zero;

        /* Force the timing of this animation to use steps */
        time = Mathf.FloorToInt((time)*box2StepCount)/box2StepCount;

        animation = new Vector3(0, 1 + Mathf.Cos(Mathf.PI*2*time/animationMaxTime), 0);

        loadingBox2.transform.position += animation;
    }

    void AnimateBox3() {
        /*
         * Animate box3 moving up and down but with steps in the timing
         */
        float animationMaxTime = 1.25f;
        float time = Time.realtimeSinceStartup % animationMaxTime;
        Vector3 animation = Vector3.zero;

        /* Force the timing of this animation to use steps */
        time = Mathf.FloorToInt((time)*box3StepCount)/box3StepCount;

        animation = new Vector3(0, 1 + Mathf.Cos(Mathf.PI*2*time/animationMaxTime), 0);

        loadingBox3.transform.position += animation;
    }

    IEnumerator LoadGame() {

        AsyncOperation async = SceneManager.LoadSceneAsync("TestArea");

        // While the asynchronous operation to load the new scene is not yet complete, continue waiting until it's done.
        while(!async.isDone) {
            yield return null;
        }
    }
}
