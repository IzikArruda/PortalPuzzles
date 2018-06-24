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
    public GameObject loadingBox4;

    /* Values used with timings of the loading animation */
    float startTime = 0;
    float endTime = -1;
    float startingAnimationTiming = 2;
    float endingAnimationTiming = 1.3f;
    float box1AnimationLoopTime = 2.25f;
    float box2AnimationLoopTime = 2.25f;
    float box3AnimationLoopTime = 2.25f;
    float box4AnimationLoopTime = 2.25f;
    float boxAnimationOffset = -0.12f;

    /* The values ranges for the box's step counts */
    //Position
    float box1PosStepCount = 60;
    float box2PosStepCount = 18;
    float box3PosStepCount = 12;
    float box4PosStepCount = 5;
    //Rotation
    float box1RotStepCount = 5;
    float box2RotStepCount = 12;
    float box3RotStepCount = 18;
    float box4RotStepCount = 60;


    /* -------- Built-In Unity Functions ---------------------------------------------------- */

    void Start() {

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
            //StartCoroutine(LoadGame());
            endTime = Time.realtimeSinceStartup;
        }

        /* Position the loading boxes into their default position, relative to the screen size */
        PositionBoxes();


        /* Once the scene has finished loading, animate the boxes off-screen */
        if(endTime > 0) {
            AnimateBoxesIntro(1 - ((Time.realtimeSinceStartup - endTime) / endingAnimationTiming));
        }

        /* Make sure the boxes start off-screen and animate into view */
        else if(Time.realtimeSinceStartup < startTime + startingAnimationTiming) {
            AnimateBoxesIntro((Time.realtimeSinceStartup - startTime) / startingAnimationTiming);
        }

        /* Animate the boxes relative to the current game time */
        AnimateBox1();
        AnimateBox2();
        AnimateBox3();
        AnimateBox4();
    }


    /* -------- Event Functions ---------------------------------------------------- */

    void PositionBoxes() {
        /*
         * Position the loading boxes on the bottom-right side of the screen
         */
        Vector3 bottomRight = sceneCamera.ViewportToWorldPoint(new Vector3(1, 0, 10));

        /* Reset the rotation of the boxes */
        loadingBox1.transform.localEulerAngles = new Vector3(0, 0, 0);
        loadingBox2.transform.localEulerAngles = new Vector3(0, 0, 0);
        loadingBox3.transform.localEulerAngles = new Vector3(0, 0, 0);
        loadingBox4.transform.localEulerAngles = new Vector3(0, 0, 0);

        /* Position the boxes */
        loadingBox1.transform.position = bottomRight + new Vector3(-1, 1, 0);
        loadingBox2.transform.position = bottomRight + new Vector3(-3, 1, 0);
        loadingBox3.transform.position = bottomRight + new Vector3(-5, 1, 0);
        loadingBox4.transform.position = bottomRight + new Vector3(-7, 1, 0);
    }

    void AnimateBoxesIntro(float timeFrame) {
        /*
         * Animate the boxes slidding into view. The given timeFrame will range from 0 to 1
         */
        Vector3 animation = Vector3.zero;
        float entryHeight = 5;

        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*timeFrame)), 0);
        loadingBox1.transform.position += animation;

        /* Use steps for the other two cubes */
        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*Mathf.FloorToInt((timeFrame)*box2PosStepCount)/box2PosStepCount)), 0);
        loadingBox2.transform.position += animation;

        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*Mathf.FloorToInt((timeFrame)*box3PosStepCount)/box3PosStepCount)), 0);
        loadingBox3.transform.position += animation;

        animation = new Vector3(0, -entryHeight + entryHeight*(Mathf.Sin((Mathf.PI/2f)*Mathf.FloorToInt((timeFrame)*box4PosStepCount)/box4PosStepCount)), 0);
        loadingBox4.transform.position += animation;
    }

    void AnimateBox1() {
        /*
         * Animate box 1 as it moves up and down and rotates. The position and rotation have sepperate step amounts.
         */
        float maxTime = box1AnimationLoopTime;
        float time = Mathf.Sin(Mathf.PI*((Time.realtimeSinceStartup % maxTime)/maxTime));
        Vector3 animation = Vector3.zero;


        /* Animate the box relative to the time */
        time = Mathf.FloorToInt((time)*box1PosStepCount)/box1PosStepCount;
        animation = new Vector3(0, Mathf.Sin(Mathf.PI*time), 0);
        //Add a set amount of height depending on how far into the time it is
        float extraHeight = 0.75f*time;
        animation += new Vector3(0, extraHeight, 0);
        loadingBox1.transform.position += animation;

        
        /* Adjust the rotation of the box relative to the time */
        time = Mathf.Sin(Mathf.PI*((Time.realtimeSinceStartup % maxTime)/maxTime));
        time = Mathf.FloorToInt((time)*box1RotStepCount)/box1RotStepCount;
        float rotationTime = Mathf.Sin(-Mathf.PI*0.025f + Mathf.PI*1.05f*time);
        if(rotationTime < 0) { rotationTime = 0; }
        loadingBox1.transform.localEulerAngles += new Vector3(0, 0, 360*rotationTime);
    }

    void AnimateBox2() {
        /*
         * Animate box2 as it moves up and down and rotates. The position and rotation have sepperate step amounts.
         */
        float maxTime = box2AnimationLoopTime;
        float time = Mathf.Sin(Mathf.PI*(((boxAnimationOffset + Time.realtimeSinceStartup) % maxTime)/maxTime));
        Vector3 animation = Vector3.zero;


        /* Animate the box relative to the time */
        time = Mathf.FloorToInt((time)*box2PosStepCount*1.01f)/box2PosStepCount;
        animation = new Vector3(0, Mathf.Sin(Mathf.PI*time), 0);
        //Add a set amount of height depending on how far into the time it is
        float extraHeight = 0.75f*time;
        animation += new Vector3(0, extraHeight, 0);
        loadingBox2.transform.position += animation;


        /* Adjust the rotation of the box relative to the time */
        time = Mathf.Sin(Mathf.PI*(((boxAnimationOffset + Time.realtimeSinceStartup) % maxTime)/maxTime));
        time = Mathf.FloorToInt((time)*box2RotStepCount*1.01f)/box2RotStepCount;
        float rotationTime = Mathf.Sin(-Mathf.PI*0.025f + Mathf.PI*1.05f*time);
        if(rotationTime < 0) { rotationTime = 0; }
        loadingBox2.transform.localEulerAngles += new Vector3(0, 0, 360*rotationTime);
    }

    void AnimateBox3() {
        /*
         * Animate box3 as it moves up and down and rotates. The position and rotation have sepperate step amounts.
         */
        float maxTime = box3AnimationLoopTime;
        float time = Mathf.Sin(Mathf.PI*(((2*boxAnimationOffset + Time.realtimeSinceStartup) % maxTime)/maxTime));
        Vector3 animation = Vector3.zero;


        /* Animate the box relative to the time */
        time = Mathf.FloorToInt((time)*box3PosStepCount*1.01f)/box3PosStepCount;
        animation = new Vector3(0, Mathf.Sin(Mathf.PI*time), 0);
        //Add a set amount of height depending on how far into the time it is
        float extraHeight = 0.75f*time;
        animation += new Vector3(0, extraHeight, 0);
        loadingBox3.transform.position += animation;


        /* Adjust the rotation of the box relative to the time */
        time = Mathf.Sin(Mathf.PI*(((2*boxAnimationOffset + Time.realtimeSinceStartup) % maxTime)/maxTime));
        time = Mathf.FloorToInt((time)*box3RotStepCount*1.01f)/box3RotStepCount;
        float rotationTime = Mathf.Sin(-Mathf.PI*0.025f + Mathf.PI*1.05f*time);
        if(rotationTime < 0) { rotationTime = 0; }
        loadingBox3.transform.localEulerAngles += new Vector3(0, 0, 360*rotationTime);
    }

    void AnimateBox4() {
        /*
         * Animate box4 as it moves up and down and rotates. The position and rotation have sepperate step amounts.
         */
        float maxTime = box4AnimationLoopTime;
        float time = Mathf.Sin(Mathf.PI*(((3*boxAnimationOffset + Time.realtimeSinceStartup) % maxTime)/maxTime));
        Vector3 animation = Vector3.zero;


        /* Animate the box relative to the time */
        time = Mathf.FloorToInt((time)*box4PosStepCount*1.01f)/box4PosStepCount;
        animation = new Vector3(0, Mathf.Sin(Mathf.PI*time), 0);
        //Add a set amount of height depending on how far into the time it is
        float extraHeight = 0.75f*time;
        animation += new Vector3(0, extraHeight, 0);
        loadingBox4.transform.position += animation;


        /* Adjust the rotation of the box relative to the time */
        time = Mathf.Sin(Mathf.PI*(((2*boxAnimationOffset + Time.realtimeSinceStartup) % maxTime)/maxTime));
        time = Mathf.FloorToInt((time)*box4RotStepCount*1.01f)/box4RotStepCount;
        float rotationTime = Mathf.Sin(-Mathf.PI*0.025f + Mathf.PI*1.05f*time);
        if(rotationTime < 0) { rotationTime = 0; }
        loadingBox4.transform.localEulerAngles += new Vector3(0, 0, 360*rotationTime);
    }

    IEnumerator LoadGame() {
        /*
         * Used to start a coroutine of loading the game's scene
         */
        AsyncOperation async = SceneManager.LoadSceneAsync("TestArea");

        /* Wait for the scene to be loaded before starting the ending animation by setting endTime */
        while(!async.isDone) {
            endTime = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(endingAnimationTiming + 0.25f);
            yield return null;
        }
    }


    /* -------- Helper Functions ---------------------------------------------------- */

    float SinWave(float offset, float rate) {
        /*
         * Return the value of a sin wave from [0, 1] using the given offset and rate values.
         * Offset determines how far into the wave it starts, starting at 0 and looping at 1.
         */
        float sinValue = 0;

        sinValue = (Mathf.Sin(Mathf.PI*2*offset + Mathf.PI*2*rate) + 1) / 2f;

        return sinValue;
    }
}
