using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/*
 * Handle the start-up sequence of the game. It starts a coroutine of loading the actual scene of the game.
 */
public class StartupTester : MonoBehaviour {

    /* The camera used to render the scene */
    public Camera sceneCamera;

    /* The coroutine that loads the game */
    AsyncOperation async;

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

        /* Start loading the new scene */
        //StartCoroutine(LoadGame());
    }

    void Update() {
        /*
         * Update the loading boxes as we wait for the scene to load
         */

        /* Check if we want to load the scene. This is used for debugging as we want to stay on this screen */
        if(Input.GetKeyDown("f")) {
            endTime = Time.realtimeSinceStartup;
        }

        /* Position the loading boxes into their default position, relative to the screen size */
        PositionBoxes();
        
        /* Update the scale of the inner boxes to represent the progress of the loading */
        UpdateLoadingProgress();

        /* Once the scene has finished loading, animate the boxes off-screen */
        if(endTime > 0) {
            AnimateBoxesIntro(1 - ((Time.realtimeSinceStartup - endTime) / endingAnimationTiming));

            /* Activate the scene and leave this loadingScreen once we are done the ending animation */
            if(Time.realtimeSinceStartup - endTime > endingAnimationTiming) {
                async.allowSceneActivation = true;
            }
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
        loadingBox2.transform.position = bottomRight + new Vector3(-2, 1, 0);
        loadingBox3.transform.position = bottomRight + new Vector3(-3, 1, 0);
        loadingBox4.transform.position = bottomRight + new Vector3(-4, 1, 0);
    }

    void UpdateLoadingProgress() {
        /*
         * Resize the inner boxes of each loading box relative to the progress of the scene loading.
         * 
         * Through multiple test while loading the game, it was discovered that the game spends 
         * most of it's loading time before the ranges of [0.013, 0.0145]. Therefore, adjust the
         * progress to use scale around that range.
         */
        //float progress = RangeBetween(async.progress, 0.013f, 0.0145f);
        float progress = RangeBetween(0, 0.013f, 0.0145f);
        int boxCount = 4;
        float maxboxSize = 0.8f;
        float boxSize;
        
        /* Set the size of the fourth box */
        boxSize = maxboxSize*(1 - RangeBetween(progress, (0f/boxCount), (1f/boxCount)));
        loadingBox4.transform.GetChild(0).transform.localScale = new Vector3(boxSize, boxSize, 1);

        /* Set the size of the third box */
        boxSize = maxboxSize*(1 - RangeBetween(progress, (1f/boxCount), (2f/boxCount)));
        loadingBox3.transform.GetChild(0).transform.localScale = new Vector3(boxSize, boxSize, 1);

        /* Set the size of the second box */
        boxSize = maxboxSize*(1 - RangeBetween(progress, (2f/boxCount), (3f/boxCount)));
        loadingBox2.transform.GetChild(0).transform.localScale = new Vector3(boxSize, boxSize, 1);

        /* Set the size of the first box */
        boxSize = maxboxSize*(1 - RangeBetween(progress, (3f/boxCount), (4f/boxCount)));
        loadingBox1.transform.GetChild(0).transform.localScale = new Vector3(boxSize, boxSize, 1);
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
         * Used to start a coroutine of loading the game's scene. 
         */
        async = SceneManager.LoadSceneAsync("TestArea");
        async.allowSceneActivation = false;
        
        /* The game is still loading the scene */
        while(!async.isDone) {

            /* Check if the loading has finished */
            if(async.progress >= 0.9f) {
                /* Set the endTime value to start preparing to finish the scene loading */
                if(endTime == -1) {
                    endTime = Time.realtimeSinceStartup;
                }
            }

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

    float RangeBetween(float value, float min, float max) {
        /*
         * Given a value and a min and max, return between [0, 1] whether the value is close to
         * the minimum (0) or close to the maximum (1). 
         */
        float range;

        range = Mathf.Clamp((value - min) / (max - min), 0, 1);

        return range;
    }
}
