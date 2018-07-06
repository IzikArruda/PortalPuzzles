using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

/*  
 * The potential states the menu can be in. Some states are transition states
 * that require a certain amount of time to pass until it reaches another state.
 * When a new state is added, a new entry in each visible element of the state is needed.
 * 
 * When a transitional state is added, it must have it's own entry in transitionStates, and:
 * - Update the state in transitionStates
 * - Add the states to the elements's arrays in StateFunctionInit()
 * - Add a UElementState() function for each element that uses the state
 * - Update ChangeState() to reset the transition's remainingTime once we enter it's transition
 */
public enum MenuStates {
    //Idle states
    Main,
    Empty,
    Sensitivity,
    Video,
    //Transitional states
    Startup,
    EmptyToMain,
    MainToEmpty,
    MainToIntro,
    MainToQuit,
    MainToSens,
    MainToVideo,
    SensToMain,
    VideoToMain
};


/*
 * Each button used in the menu. They are placed in an enum so each one will 
 * have their own index in an array.
 */
public enum Buttons {
    Start,
    Video,
    Sens,
    Quit
}

/*
 * Each panel used by the menu. Adding a panel requires you to:
 * - Create and run the new panel's Setup function
 */
public enum Panels {
    Credit,
    Cover,
    Video,
    Sens
}

/*
 * A class that points a state to a function. These are used with UI elements to point
 * to what function is used to update them depending on the current state of the menu.
 */
public class StateFunction {
    public MenuStates state;
    public UnityAction function;

    public StateFunction(MenuStates s, UnityAction f) {
        state = s;
        function = f;
    }
}

/*
 * A transition is a state that after a certain time limit will transition into a new state
 */
public class Transition {
    /* The current state that it will begin with */
    public MenuStates from;
    /* The state that will be transitionned to */
    public MenuStates to;
    /* The timings of the transition */
    public float timeMax;
    public float timeRemaining;

    public Transition(MenuStates f, MenuStates t, float tm, float tr) {
        from = f;
        to = t;
        timeMax = tm;
        timeRemaining = tr;
    }
}


/*
 * The menu used by the player during the game. It expected each component to be 
 * already created and assigned in a canvas.
 */
public class Menu : MonoBehaviour {
    public MenuStates state;

    /*
     * Each transitional state and it's Transition object.
     * If a state will transition into a more unique condition (such as MainToQuit),
     * make the state transition into itself as it will be handlede manually in UpdateCurrentState().
     */
    Transition[] transitionStates = {
        new Transition(MenuStates.Startup, MenuStates.Main, 10.0f, 0f),
        new Transition(MenuStates.EmptyToMain, MenuStates.Main, 0.325f, 0f),
        new Transition(MenuStates.MainToEmpty, MenuStates.Empty, 0.325f, 0f),
        new Transition(MenuStates.MainToIntro, MenuStates.Empty, 0.5f, 0f),
        new Transition(MenuStates.MainToQuit, MenuStates.MainToQuit, 1.8f, 0f),
        new Transition(MenuStates.MainToSens, MenuStates.Sensitivity, 0.4f, 0f),
        new Transition(MenuStates.MainToVideo, MenuStates.Video, 0.4f, 0f),
        new Transition(MenuStates.VideoToMain, MenuStates.Main, 0.4f, 0f),
        new Transition(MenuStates.SensToMain, MenuStates.Main, 0.4f, 0f)
    };

    /*
     * Set the state functions for each UI element and every state. Each element requires
     * a state function for each state that the element is visible in.
     */
    StateFunction[] startButtonTransitions;
    StateFunction[] videoButtonTransitions;
    StateFunction[] sensButtonTransitions;
    StateFunction[] quitButtonTransitions;
    StateFunction[] CreditPanelTransitions;
    StateFunction[] coverPanelTransitions;
    StateFunction[] videoPanelTransitions;
    StateFunction[] sensPanelTransitions;

    /* Button height to width ratios. Set manually and is unique for each font + text content. */
    private float quitBonusSize = 0.75f;
    private float videoWidthRatio = 4.25f;
    private float sensWidthRatio = 8.25f;
    private float quitWidthRatio = 3.125f;
    private float resolutionWidthRatio = 7f;
    private float framerateWidthRatio = 3f;
    //The many width ratios the start button will use. Make sure to set it when changing text
    private float startWidthRatio;
    private float startBonusSize = 1.25f;
    private float startTextWidthRatio = 4.45f;
    private float continueTextWidthRatio = 7.125f;
    //Set this to the largest ratio we currently have. This is to make sure each element goes offscreen at the same speed
    private float largestRatio;
    //How much NOT hovering over the button will reduce the button's size
    private float hoverReductionAmount = 3;

    /* Panel sizes. These are set in their setup functions and used in their update functions */
    private float[] panelsWidth;
    private float[] panelsHeight;

    /* Global values used for sizes of UI elements */
    private float minHeight = 20f;
    private float maxHeight = 150f;
    private float buttonSizeMod = 0.5f;
    private float buttonHeight;
    private float buttonEdgeOffset = 35f;
    private float buttonSepperatorDistance = 20f;

    /* A link to the player's controller */
    private CustomPlayerController playerController;

    /* The fonts used for the text of the game */
    public Font buttonFont;
    public Font otherTextFont;

    /* The canvas that holds all the UI elements */
    public Canvas canvas;
    private RectTransform canvasRect;

    /* A basic button with a text child and an empty panel. Must be set before running. */
    public GameObject buttonReference;
    public GameObject panelReference;

    /* References to UI objects used by the menu */
    public Slider sensSliderReference;
    public Dropdown videoOptionsDropwdownReference;
    public Toggle videoOptionsToggleReference;

    /* The sensitivity slider and it's current value */
    private Slider sensitivitySlider;
    private Text sensitivitySliderValueText;
    private float sensMax = 25f;
    private float sensitivity;

    /* An array that holds the main buttons of the UI. Each index has it's own button */
    private Button[] buttons;
    private RectTransform[] buttonRects;

    /* An array of panels used by the menu */
    private RectTransform[] panelRects;

    /* Arrays that hold the hover values. Each index is a different button's hover time. True = hovered */
    private bool[] currentHoverState;
    private float[] currentHoverTime;
    private float maxHoverTime = 0.5f;

    /* Previous resolutions of the window */
    private float screenWidth;
    private float screenHeight;

    /* 
     * Other menu variables 
     */
    /* Values used when trying to quit the game */
    float quitValueCurrent = 0;
    float quitValueIncrease = 0.15f;
    float quitValueDecreaseMod = 0.3f;
    float quitValueMax = 1;

    /* Values used with the start button and once the game has begun */
    bool isGameStarted = false;

    /* Values used when testing the mouse sensitivity */
    private Vector3 extraCamRotation;
    private Vector3 savedRotation;
    private float recoveryTime;

    /* The main terrainController of the game. Used to access it's sunflare and loading progress */
    public TerrainController terrainController;

    /* Global values with minor/single uses */
    private Vector2 bonusQuitSize = new Vector2(0, 0);
    private bool mouseLock = false;

    /* Value that controls the main panel's position. Start at a negative value to delay the credits start */
    public float creditScrollValue = -0.5f;
    private bool isOutside = false;

    /* Values that handle the loading animation */
    //Animation timing values
    private float animationIncrementMod = 0.5f;
    private float loadingAnimationTime = 0;
    private float animationTimeMod = 1f;
    private float loadingAnimationLoopTime = 2.25f;
    private float loadingAnimationOffset = 0.175f;
    private float loadingStartingVisibilityDifference = 0.15f;
    /* Visibility ranges from [0, 1], with 0 being bellow the screen and 1 being fully visible.
     * The state marks which loadingBox will have it's visibility increased or decreased every frame. */
    private float[] loadingAnimationVisibility;
    private bool[] loadingAnimationState;
    //Positionnal and size values
    private int boxCount = 6;
    private float boxSize = 50f;
    private float boxSepperationSize = 30f;
    private float heightFromBottom = 35f;
    //References
    private RectTransform[] loadingBoxes;
    private RectTransform[] interiorLoadingBoxes;

    /* Values that handle the puzzle scene loading */
    private bool puzzleSceneLoaded = false;
    private bool terrainGenerated = false;
    private AsyncOperation puzzleSceneCoroutine;
    

    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Start() {
        /*
         * Initialize the menu and start loading the scene of the game
         */
         
        //If a key is held down, change the box count. This is for testing for the ideal amount of boxes
        int newBoxCount = 2;
        if(Input.GetKey("1")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("2")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("3")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("4")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("5")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("6")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("7")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("8")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("9")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("0")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("q")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("w")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("e")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("r")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("t")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("y")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("u")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("i")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("o")) { boxCount = newBoxCount; }
        newBoxCount++;
        if(Input.GetKey("p")) { boxCount = newBoxCount; }

        
        /* Initialize the menu */
        InitializeMenu();

        /* Start loading the puzzleScene */
        LoadPuzzleScene();
    }

    void Update() {
        /*
         * Check if the screen has been resized and run any per-frame update calls for any UI elements
         */

        /* Check if the screen has been resized */
        if(Screen.width != screenWidth || Screen.height != screenHeight) {
            Resize(true);
        }

        /* Update the hover values of the buttons */
        UpdateHoverValues();

        /* Update the camera for any mouse movement during the sensitivity testing */
        UpdateSensitivityCamera();

        /* Update the transition values. Only change states once the per-frame updates are done. */
        UpdateTransitionValues();

        /* 
         * Run the per-frame update functions of each UI element 
         */
        /* Start button */
        ExecuteElementFunctions(startButtonTransitions);
        /* Video button */
        ExecuteElementFunctions(videoButtonTransitions);
        /* Sensitivity button */
        ExecuteElementFunctions(sensButtonTransitions);
        /* Quit button */
        ExecuteElementFunctions(quitButtonTransitions);
        /* Main panel */
        ExecuteElementFunctions(CreditPanelTransitions);
        /* Cover panel */
        ExecuteElementFunctions(coverPanelTransitions);
        /* Video panel */
        ExecuteElementFunctions(videoPanelTransitions);
        /* Sens panel */
        ExecuteElementFunctions(sensPanelTransitions);
        
        /*
         * Handle the scene and terrain loader trackers 
         */
        if(!puzzleSceneLoaded && puzzleSceneCoroutine != null && puzzleSceneCoroutine.isDone) {
            /* The puzzle scene has loaded */
            LoadedPuzzleScene();
        }
        if(!terrainGenerated && terrainController != null && terrainController.GetLoadingPercent() >= 1) {
            /* The terrain has been generated */
            terrainGenerated = true;
        }
        
        /*
         * Handle the animation that is used to represent the loading progress
         */
       
        /* Update each individual loading box's visibility relative to their saved state */
        for(int i = 0; i < boxCount; i++) {

            /* Increment the visibility and prevent it from going above 1 */
            if(loadingAnimationState[i]) {
                loadingAnimationVisibility[i] += animationIncrementMod*Time.deltaTime;
                if(loadingAnimationVisibility[i] > 1) { loadingAnimationVisibility[i] = 1; }
            }

            /* Decrement and only prevent it from being decreased bellow 0. Let it start and increment from bellow 0. */
            else {
                loadingAnimationVisibility[i] -= animationIncrementMod*Time.deltaTime*2;
                if(loadingAnimationVisibility[i] < 0) { loadingAnimationVisibility[i] = 0; }
            }
        }
        
        /* Only animate the loading boxes if the final loading box is still visible */
        if(loadingAnimationVisibility[0] > 0) {
            /* Increment the time spent in the loading */
            loadingAnimationTime += animationTimeMod*Time.deltaTime;

            /* Update the animation effect */
            UpdateLoadingAnimation();
        }

        /* Change the current state if needed after all the per-frame update functions are done */
        UpdateCurrentState();
    }


    /* ----------- Initialization Functions ------------------------------------------------------------- */

    public void InitializeMenu() {
        /*
         * Sets up the main menu. Start the game in the IntroToMain transition state.
         */

        /* Make sure the window's sizes are properly set */
        Resize(false);

        /* Populate the StateFunction arrays before anything else */
        StateFunctionInit();

        /* Update the current starting state */
        state = MenuStates.Empty;
        ChangeState(MenuStates.Startup);

        /* Set the largestRatio to reflect the largest button */
        largestRatio = continueTextWidthRatio*startBonusSize;

        /* Create and populate the array of panels and their sizes used by the UI */
        canvasRect = canvas.GetComponent<RectTransform>();
        panelRects = new RectTransform[System.Enum.GetValues(typeof(Panels)).Length];
        panelsWidth = new float[System.Enum.GetValues(typeof(Panels)).Length];
        panelsHeight = new float[System.Enum.GetValues(typeof(Panels)).Length];
        for(int i = 0; i < panelRects.Length; i++) {
            panelRects[i] = CreatePanel().GetComponent<RectTransform>();
        }

        /* Run the initialSetup functions for each panel */
        SetupCreditPanel();
        SetupCoverPanel();
        SetupSensPanel();
        SetupVideoPanel();

        /* Create and populate the buttons and hover arrays */
        buttons = new Button[System.Enum.GetValues(typeof(Buttons)).Length];
        buttonRects = new RectTransform[System.Enum.GetValues(typeof(Buttons)).Length];
        currentHoverState = new bool[System.Enum.GetValues(typeof(Buttons)).Length];
        currentHoverTime = new float[System.Enum.GetValues(typeof(Buttons)).Length];
        for(int i = 0; i < buttons.Length; i++) {
            buttons[i] = CreateButton().GetComponent<Button>();
            buttonRects[i] = buttons[i].GetComponent<RectTransform>();
            currentHoverState[i] = false;
            currentHoverTime[i] = 0;
        }

        /* Run the initialSetup functions for each of the buttons */
        SetupStartButton();
        SetupVideoButton();
        SetupSensButton();
        SetupQuitButton();

        /* Run the setup for the loading boxes */
        SetupLoadingBoxes();

        /* After setting up each component, make sure they are properly sized */
        Resize(true);

        /* Re-order the hierarchy so that certain objects are rendered ontop of others */
        ReorderHeirarchy();
    }

    public void StateFunctionInit() {
        /*
         * Set the state functions for each UI element and every state. Each element requires
         * a state function for each state that the element is visible in.
         */
        startButtonTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, UStartButtonStartup),
            new StateFunction(MenuStates.EmptyToMain, UStartButtonEmptyToMain),
            new StateFunction(MenuStates.MainToEmpty, UStartButtonMainToEmpty),
            new StateFunction(MenuStates.Main, UStartButtonMain),
            new StateFunction(MenuStates.MainToIntro, UStartButtonMainToIntro),
            new StateFunction(MenuStates.MainToQuit, UStartButtonMainToQuit),
            new StateFunction(MenuStates.MainToSens, UStartButtonMainToSens),
            new StateFunction(MenuStates.MainToVideo, UStartButtonMainToSens),
            new StateFunction(MenuStates.SensToMain, UStartButtonSensToMain),
            new StateFunction(MenuStates.VideoToMain, UStartButtonSensToMain)
        };
        videoButtonTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, UVideoButtonStartup),
            new StateFunction(MenuStates.EmptyToMain, UVideoButtonEmptyToMain),
            new StateFunction(MenuStates.MainToEmpty, UVideoButtonMainToEmpty),
            new StateFunction(MenuStates.Main, UVideoButtonMain),
            new StateFunction(MenuStates.MainToIntro, UVideoButtonMainToIntro),
            new StateFunction(MenuStates.MainToQuit, UVideoButtonMainToQuit),
            new StateFunction(MenuStates.MainToSens, UVideoButtonMainToSens),
            new StateFunction(MenuStates.MainToVideo, UVideoButtonMainToVideo),
            new StateFunction(MenuStates.SensToMain, UVideoButtonSensToMain),
            new StateFunction(MenuStates.VideoToMain, UVideoButtonVideoToMain),
            new StateFunction(MenuStates.Video, UVideoButtonVideo)
        };
        sensButtonTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, USensButtonStartup),
            new StateFunction(MenuStates.EmptyToMain, USensButtonEmptyToMain),
            new StateFunction(MenuStates.MainToEmpty, USensButtonMainToEmpty),
            new StateFunction(MenuStates.Main, USensButtonMain),
            new StateFunction(MenuStates.MainToIntro, USensButtonMainToIntro),
            new StateFunction(MenuStates.MainToQuit, USensButtonMainToQuit),
            new StateFunction(MenuStates.MainToSens, USensButtonMainToSens),
            new StateFunction(MenuStates.MainToVideo, USensButtonMainToVideo),
            new StateFunction(MenuStates.SensToMain, USensButtonSensToMain),
            new StateFunction(MenuStates.Sensitivity, USensButtonSensitivity),
            new StateFunction(MenuStates.VideoToMain, USensButtonVideoToMain)
        };
        quitButtonTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, UQuitButtonStartup),
            new StateFunction(MenuStates.EmptyToMain, UQuitButtonEmptyToMain),
            new StateFunction(MenuStates.MainToEmpty, UQuitButtonMainToEmpty),
            new StateFunction(MenuStates.Main, UQuitButtonMain),
            new StateFunction(MenuStates.MainToIntro, UQuitButtonMainToIntro),
            new StateFunction(MenuStates.MainToQuit, UQuitButtonMainToQuit),
            new StateFunction(MenuStates.MainToVideo, UQuitButtonMainToSens),
            new StateFunction(MenuStates.MainToSens, UQuitButtonMainToSens),
            new StateFunction(MenuStates.SensToMain, UQuitButtonSensToMain),
            new StateFunction(MenuStates.VideoToMain, UQuitButtonSensToMain)
        };
        CreditPanelTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Empty, UCreditPanelEmptyOrMain),
            new StateFunction(MenuStates.Main, UCreditPanelEmptyOrMain),
            new StateFunction(MenuStates.MainToEmpty, UCreditPanelEmptyOrMain),
            new StateFunction(MenuStates.EmptyToMain, UCreditPanelEmptyOrMain),
            new StateFunction(MenuStates.VideoToMain, UCreditPanelVideoToMain),
            new StateFunction(MenuStates.MainToVideo, UCreditPanelMainToVideo),
            new StateFunction(MenuStates.SensToMain, UCreditPanelSensToMain),
            new StateFunction(MenuStates.MainToSens, UCreditPanelMainToSens),
            new StateFunction(MenuStates.MainToQuit, UCreditPanelQuit)

        };
        coverPanelTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, UCoverPanelStartup),
            new StateFunction(MenuStates.MainToQuit, UCoverPanelMainToQuit)
        };
        videoPanelTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Video, UVideoPanelVideo),
            new StateFunction(MenuStates.MainToVideo, UVideoPanelMainToVideo),
            new StateFunction(MenuStates.VideoToMain, UVideoPanelVideoToMain)
        };
        sensPanelTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Sensitivity, USensPanelSensitivity),
            new StateFunction(MenuStates.MainToSens, USensPanelMainToSens),
            new StateFunction(MenuStates.SensToMain, USensPanelSensToMain)
        };
    }

    public GameObject CreatePanel() {
        /*
         * Duplicate and return the panel reference
         */
        GameObject panelObject = Instantiate(panelReference);
        panelObject.transform.SetParent(canvas.transform);
        panelObject.SetActive(true);

        /* Let the mouse cursor select objects behind the panel */
        panelObject.GetComponent<Image>().raycastTarget = false;

        return panelObject;
    }

    public GameObject CreateButton() {
        /*
         * Duplicate and return the button reference
         */
        GameObject buttonObject = Instantiate(buttonReference);
        buttonObject.transform.SetParent(canvas.transform);
        buttonObject.SetActive(true);

        return buttonObject;
    }

    public void Resize(bool updateUI) {
        /*
         * Update the sizes of the ui to reflect the current screen size.
         */
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        /* Update the buttonHeight value used by all buttons */
        buttonHeight = buttonSizeMod*Mathf.Clamp(screenHeight*0.15f, minHeight, maxHeight);
        //heightRatio = buttonHeight/avgHeight;

        /* Run the reset functions for each UI element be updated from the new size */
        if(updateUI) {
            CreditPanelReset();
            CoverPanelReset();
            VideoPanelReset();
            SensPanelReset();
            StartButtonReset();
            VideoButtonReset();
            SensButtonReset();
            QuitButtonReset();
        }
    }

    void ReorderHeirarchy() {
        /*
         * Reorder the hierarchy of the canvas.
         */

        /* Have the cover panel above all else */
        panelRects[(int) Panels.Cover].transform.SetAsLastSibling();

        /* Have the loading boxes above the cover panel */
        for(int i = 0; i < loadingBoxes.Length; i++) {
            loadingBoxes[i].SetAsLastSibling();
        }
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void SetupButtonText(Text textObject, string textString) {
        /*
         * Set the properties of the button's text object. This is to keep all button text objects consistent
         */

        /* Set the font */
        textObject.font = buttonFont;
        textObject.fontStyle = FontStyle.Normal;
        textObject.text = textString;

        /* Set the outlines for the text. There will be two outlines used for each text. */
        if(textObject.gameObject.GetComponent<Outline>() == null) {
            textObject.gameObject.AddComponent<Outline>();
            textObject.gameObject.AddComponent<Outline>();
        }

        /* Set size relative values for the text */
        textObject.alignment = TextAnchor.MiddleLeft;
        textObject.alignByGeometry = true;
        textObject.resizeTextForBestFit = true;
        textObject.resizeTextMinSize = 1;
        textObject.resizeTextMaxSize = 10000;
    }

    public void SetupButtonEvents(ref Button button, UnityAction mouseEnter, UnityAction mouseExit) {
        /*
         * Attach the hover events to the given button
         */

        /* Create the trigger events */
        EventTrigger buttonTrigger = button.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry buttonEnter = new EventTrigger.Entry();
        EventTrigger.Entry buttonExit = new EventTrigger.Entry();

        /* Link the given functions to the mouse events */
        buttonEnter.eventID = EventTriggerType.PointerEnter;
        buttonExit.eventID = EventTriggerType.PointerExit;
        buttonEnter.callback.AddListener((data) => { mouseEnter(); });
        buttonExit.callback.AddListener((data) => { mouseExit(); });

        /* Add the events to the triggers */
        buttonTrigger.triggers.Add(buttonEnter);
        buttonTrigger.triggers.Add(buttonExit);
    }

    void SetupLoadingBoxes() {
        /*
         * Create the loading boxes used to display the progress of the game loading
         */

        /* Create the boxes to be used during the loading process */
        loadingBoxes = new RectTransform[boxCount];
        interiorLoadingBoxes = new RectTransform[boxCount];
        loadingAnimationVisibility = new float[boxCount];
        loadingAnimationState = new bool[boxCount];
        for(int i = 0; i < boxCount; i++) {

            /* Set the starting visibility value of the box */
            loadingAnimationVisibility[i] = 0 - i*loadingStartingVisibilityDifference;
            loadingAnimationState[i] = true;

            /* Create the base and interior loading boxes */
            loadingBoxes[i] = CreatePanel().GetComponent<RectTransform>();
            loadingBoxes[i].name = "Loading Box " + (i+1);
            interiorLoadingBoxes[i] = CreatePanel().GetComponent<RectTransform>();
            interiorLoadingBoxes[i].name = "Interior Loading Box " + (i+1);

            /* Set the position and size of the loading box */
            loadingBoxes[i].anchorMin = new Vector2(1, 0);
            loadingBoxes[i].anchorMax = new Vector2(1, 0);
            loadingBoxes[i].sizeDelta = new Vector2(boxSize, boxSize);

            /* Set the interior box to be a child of it's parent loading box */
            interiorLoadingBoxes[i].SetParent(loadingBoxes[i]);
            interiorLoadingBoxes[i].anchoredPosition = new Vector2(0, 0);
            interiorLoadingBoxes[i].sizeDelta = new Vector2(0, 0);

            /* Set the color of the boxes */
            loadingBoxes[i].GetComponent<Image>().color = new Color(0, 0, 0, 1);
            interiorLoadingBoxes[i].GetComponent<Image>().color = new Color(1, 1, 1, 1);
        }
    }

    void SetupCreditPanel() {
        /*
         * Setup the credits panel that scrolls up into the main menu
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform mainPanel = panelRects[panelEnum];
        mainPanel.name = "Credit panel";


        /* The main panel covers half the X width and all the Y height */
        panelsWidth[panelEnum] = 0.5f;
        panelsHeight[panelEnum] = 1.0f;

        /* Set the anchors so it's centered on the right wall */
        mainPanel.anchorMin = new Vector2(0.5f, 0);
        mainPanel.anchorMax = new Vector2(1, 1);

        /* Set the color so that the panel is invisible */
        mainPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        /* 
         * Create the "Thank you for playing" text that is placed on the bottom
         */
        GameObject thanksTextObject = new GameObject("Thank you text", typeof(RectTransform));
        Text thanksText = thanksTextObject.AddComponent<Text>();
        RectTransform thanksTextRect = thanksTextObject.GetComponent<RectTransform>();
        thanksTextRect.SetParent(mainPanel);
        /* Set the anchors so that the text stays in the center top of the panel */
        thanksTextRect.anchorMin = new Vector2(0.025f, 0.025f);
        thanksTextRect.anchorMax = new Vector2(0.975f, 0.25f);
        thanksTextRect.anchoredPosition = new Vector2(0, 0);
        thanksTextRect.sizeDelta = new Vector2(0, 0);
        /* Set the text properties */
        thanksText.font = buttonFont;
        thanksText.fontStyle = FontStyle.Normal;
        thanksText.gameObject.AddComponent<Outline>();
        thanksText.alignment = TextAnchor.MiddleCenter;
        thanksText.resizeTextForBestFit = true;
        thanksText.resizeTextMinSize = 1;
        thanksText.resizeTextMaxSize = 10000;
        thanksText.color = new Color(1, 1, 1, 1);
        thanksText.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 1);
        thanksText.text = "thanks for playing";
        //Set the line spacing to a high value to ensure it will stay on one line
        thanksText.lineSpacing = 100;

        /* 
         * Create the text that indicates the following names are the songs used in the game  
         */
        GameObject songListObject = new GameObject("Song list text", typeof(RectTransform));
        Text songListText = songListObject.AddComponent<Text>();
        RectTransform songListRect = songListObject.GetComponent<RectTransform>();
        songListRect.SetParent(mainPanel);
        /* Set the anchors so that the text stays on the top left side of the panel */
        songListRect.anchorMin = new Vector2(0, 0.8f);
        songListRect.anchorMax = new Vector2(1, 0.875f);
        songListRect.anchoredPosition = new Vector2(0, 0);
        songListRect.sizeDelta = new Vector2(0, 0);
        /* Set the text properties */
        songListText.font = buttonFont;
        songListText.fontStyle = FontStyle.Normal;
        songListText.gameObject.AddComponent<Outline>();
        songListText.alignment = TextAnchor.MiddleLeft;
        songListText.resizeTextForBestFit = true;
        songListText.resizeTextMinSize = 1;
        songListText.resizeTextMaxSize = 10000;
        songListText.color = new Color(1, 1, 1, 1);
        songListText.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 1);
        songListText.text = " song list";

        /*
         * Create a text for each song used in the game
         */
        string[] songs = { "Erik Satie - Gymnopédie No.1",
            "Animal Crossing: New Leaf - 5PM (Remix by JezDayy)",
            "Chrono Trigger - Black Omen (Remix by Malcolm Robinson Music)",
            "Pikmin - The Forest of Hope (Remix by Scruffy)",
            "Donkey Kong Country - Aquatic Ambience (Remix by iSWM)",
            "Donkey Kong Country 2 - Stickerbrush Symphony (Remix by PPF)",
            "DJ Okawari - A Cup of Coffee",
            "DJ Okawari - Pack Light" };
        float anchorSpacing = 0.0125f;
        float songAnchorSize = 0.04f;
        float currentAnchorY = songListRect.anchorMin.y - anchorSpacing;
        GameObject songObject;
        Text songText;
        RectTransform songRect;
        for(int i = 0; i < songs.Length; i++) {
            songObject = new GameObject("Song " + i, typeof(RectTransform));
            songText = songObject.AddComponent<Text>();
            songRect = songObject.GetComponent<RectTransform>();
            songRect.SetParent(mainPanel);
            /* Set the anchors so that the text stays on the left side of the panel */
            songRect.anchorMin = new Vector2(0, currentAnchorY - songAnchorSize);
            songRect.anchorMax = new Vector2(1, currentAnchorY);
            currentAnchorY -= songAnchorSize + anchorSpacing;
            songRect.anchoredPosition = new Vector2(0, 0);
            songRect.sizeDelta = new Vector2(0, 0);
            /* Set the text properties */
            songText.font = otherTextFont;
            songText.fontStyle = FontStyle.Normal;
            songText.gameObject.AddComponent<Outline>();
            songText.alignment = TextAnchor.MiddleLeft;
            songText.resizeTextForBestFit = true;
            songText.resizeTextMinSize = 1;
            songText.resizeTextMaxSize = 10000;
            songText.color = new Color(1, 1, 1, 1);
            songText.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 1);
            songText.text = songs[i];
        }

    }

    void SetupCoverPanel() {
        /*
         * Setup the cover panel that covers the whole screen
         */
        RectTransform mainPanel = panelRects[(int) Panels.Cover];
        mainPanel.name = "Cover panel";

        /* Set the anchors so it becomes a full stretch layout */
        mainPanel.anchorMin = new Vector2(0, 0);
        mainPanel.anchorMax = new Vector2(1, 1);

        /* Set the color and remove the image so that the panel is invisible */
        mainPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);

    }

    void SetupVideoPanel() {
        /*
         * Setup the video panel which will be used when in the video state
         */
        int panelEnum = (int) Panels.Video;
        RectTransform videoPanel = panelRects[panelEnum];

        /*
         * Setup the video panel
         */
        videoPanel.name = "Video panel";

        /* Set the anchors so it's centered on the right wall and closer towards the bottom */
        videoPanel.anchorMin = new Vector2(1, 0.25f);
        videoPanel.anchorMax = new Vector2(1, 0.25f);
        /* The size of the panel should be 80% the screen width and 50% for height */
        panelsWidth[panelEnum] = 0.8f;
        panelsHeight[panelEnum] = 0.5f;
        /* Set the color so that the panel is invisible */
        videoPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);


        float optionPanelHeight = buttonHeight/2f;
        /*
         * Setup the panel for each option along with it's text component
         */
        string[] videoButtonTexts = { "Windowed", "Lock mouse", "Run without focus", "Resolution", "Lock framerate" };
        GameObject[] videoOptionPanels = new GameObject[videoButtonTexts.Length];
        for(int i = 0; i < videoOptionPanels.Length; i++) {
            /* Create the panel and set it's anchors */
            videoOptionPanels[i] = new GameObject("Option panel [" + videoButtonTexts[i] + "]", typeof(RectTransform));
            RectTransform panelRect = videoOptionPanels[i].GetComponent<RectTransform>();
            panelRect.SetParent(videoPanel);
            panelRect.anchorMin = new Vector2(0.1f, 0.4f);
            panelRect.anchorMax = new Vector2(1, 0.4f);

            /* Create the text component used for each option */
            GameObject textObject = new GameObject("Option text [" + videoButtonTexts[i] + "]", typeof(RectTransform));
            Text text = textObject.AddComponent<Text>();
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(panelRect);
            /* Set the anchors so that the text stays on the left side of the panel */
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(0.49f, 1);
            textRect.anchoredPosition = new Vector2(0, 0);
            textRect.sizeDelta = new Vector2(0, 0);
            /* Set the text properties */
            text.font = otherTextFont;
            text.fontStyle = FontStyle.Normal;
            text.text = videoButtonTexts[i];
            text.gameObject.AddComponent<Outline>();
            text.alignment = TextAnchor.MiddleRight;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 1;
            text.resizeTextMaxSize = 10000;

            /* The last two options will be on the same height, so set their anchors to not overlap */
            if(i == videoOptionPanels.Length - 2) {
                panelRect.anchorMin = new Vector2(0, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            }
            else if(i == videoOptionPanels.Length - 1) {
                panelRect.anchorMin = new Vector2(0.6f, 0.5f);
                panelRect.anchorMax = new Vector2(1f, 0.5f);
            }
        }


        /*
         * Add the UI toggle components for the defined amount of toggle options
         */
        string[] toggleOptions = { "Windowed toggle", "Mouse lock", "Focus pause" };
        for(int i = 0; i < toggleOptions.Length; i++) {
            /* Create the transforms and objects for the toggle options */
            RectTransform panel = videoOptionPanels[i].GetComponent<RectTransform>();
            GameObject toggleObject = Instantiate(videoOptionsToggleReference.gameObject);
            toggleObject.name = "Toggle [" + toggleOptions[i] + "]";
            toggleObject.SetActive(true);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
            toggleRect.SetParent(panel);

            /* Link a function to it's onValueChange */
            if(i == 0) {
                toggle.onValueChanged.AddListener(delegate { UpdatedWindowedToggle(toggle); });
                toggle.isOn = !Screen.fullScreen;
            }
            else if(i == 1) {
                toggle.onValueChanged.AddListener(delegate { LockMouseToggle(toggle); });
                mouseLock = Cursor.lockState == CursorLockMode.Confined;
                toggle.isOn = mouseLock;
                UpdateCursorState();
            }
            else if(i == 2) {
                toggle.onValueChanged.AddListener(delegate { RunWithoutFocusToggle(toggle); });
                toggle.isOn = Application.runInBackground;
            }

            /* Position the toggle to be placed on the right side of it's panel */
            toggleRect.anchorMin = new Vector2(0.51f, 0);
            toggleRect.anchorMax = new Vector2(1, 1);
            toggleRect.anchoredPosition = new Vector2(0, 0);
            toggleRect.sizeDelta = new Vector2(optionPanelHeight, 0);

            /* Resize the images of the button to reflect the panel's height */
            RectTransform background = toggleRect.GetChild(0).GetComponent<RectTransform>();
            background.anchorMin = new Vector2(0, 0);
            background.anchorMax = new Vector2(0, 1);
            RectTransform checkbox = background.GetChild(0).GetComponent<RectTransform>();
            checkbox.anchorMin = new Vector2(0, 0);
            checkbox.anchorMax = new Vector2(1, 1);
            checkbox.sizeDelta = new Vector2(0, 0);
        }


        /*
         * Add the UI dropdown menu component for the defined amount of option panels
         */
        string[] dropdownOptions = { "Resolution Dropdown", "Framerate Dropdown" };
        for(int i = 0; i < dropdownOptions.Length; i++) {
            /* Create the transforms and objects for the dropdown options */
            int optionIndex = videoOptionPanels.Length-dropdownOptions.Length + i;
            RectTransform panel = videoOptionPanels[optionIndex].GetComponent<RectTransform>();
            GameObject dropObject = Instantiate(videoOptionsDropwdownReference.gameObject);
            Dropdown drop = dropObject.GetComponent<Dropdown>();
            RectTransform dropRect = dropObject.GetComponent<RectTransform>();
            dropObject.name = "Dropdown [" + dropdownOptions[i] + "]";
            dropObject.SetActive(true);
            dropRect.SetParent(panel);
            /* Position the object to be placed on the right side of it's panel */
            dropRect.anchorMin = new Vector2(0.5f, 0);
            dropRect.anchorMax = new Vector2(0.5f, 1);
            dropRect.anchoredPosition = new Vector2((optionPanelHeight/2f)*resolutionWidthRatio/2f, 0);
            dropRect.sizeDelta = new Vector2((optionPanelHeight/2f)*resolutionWidthRatio, 0);

            /* Link a function to it's onValueChanged and populate the dropdown options */
            List<string> options = new List<string>();
            int selectedOption = 0;
            if(i == 0) {
                /* Get the usable resolutions by the current screen */
                drop.onValueChanged.AddListener(delegate { UpdatedResolutionDropdown(drop); });
                Resolution[] resolutions = Screen.resolutions;
                string currentResolution = Screen.currentResolution.width + "x" + Screen.currentResolution.height;
                for(int j = 0; j < resolutions.Length; j++) {
                    options.Add(resolutions[j].width + "x" + resolutions[j].height);
                    /* Set the selected resolution to the currently used resolution */
                    if(currentResolution.Equals(resolutions[j].width + "x" + resolutions[j].height)) {
                        selectedOption = j;
                    }
                }
            }
            else if(i == 1) {
                /* Use a list of pre-defined framerates */
                drop.onValueChanged.AddListener(delegate { UpdateLockedFramerate(drop); });
                options = new List<string>() { "Inf", "10", "30", "60", "69", "144", "420" };
                /* Set the currently selected framerate to unlocked */
                selectedOption = 0;
            }
            /* Link the new options to the dropdown menu */
            drop.ClearOptions();
            drop.AddOptions(options);
            drop.value = selectedOption;
            drop.RefreshShownValue();
        }

        /* End by setting the position/rotation of the panel after all components have been properly set */
        VideoPanelPositionUpdate(0);
    }

    void SetupSensPanel() {
        /*
         * Setup the sensitivity panel which will be used when in the sensitivity state
         */
        int panelEnum = (int) Panels.Sens;
        RectTransform sensPanel = panelRects[panelEnum];
        sensPanel.name = "Sensitivity panel";


        /* Set the anchors so it's position based in the bottom right corner */
        sensPanel.anchorMin = new Vector2(1, 0);
        sensPanel.anchorMax = new Vector2(1, 0);

        /* The size of the panel should be 60% the screen width and 27.5% for height */
        panelsWidth[panelEnum] = 0.6f;
        panelsHeight[panelEnum] = 0.275f;
        float panelWidth = Screen.width*panelsWidth[panelEnum];
        float panelHeight = Screen.height*panelsHeight[panelEnum];
        sensPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        /* Set the color so that the panel is invisible */
        sensPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        /*
         * Add new components to the panel
         */
        /* Add a slider to the center of the panel used to control the sensitivity */
        GameObject sliderObject = Instantiate(sensSliderReference.gameObject);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sensitivitySlider = sliderObject.GetComponent<Slider>();
        sliderObject.transform.SetParent(sensPanel);
        sliderObject.SetActive(true);
        sliderObject.name = "Sensitivity slider";
        /* Set the sizes of the slider */
        sliderRect.anchorMin = new Vector2(0, 0.5f);
        sliderRect.anchorMax = new Vector2(1, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0, 0);
        sliderRect.sizeDelta = new Vector2(0, 20);
        /* Assign a function to when the slider updates */
        sensitivitySlider.maxValue = sensMax;
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.onValueChanged.AddListener(delegate { UpdateSensitivitySlider(-1); });

        /* Add text bellow the slider giving instructions */
        GameObject sliderText = new GameObject("Slider text", typeof(RectTransform));
        Text text = sliderText.AddComponent<Text>();
        RectTransform rectTex = sliderText.GetComponent<RectTransform>();
        sliderText.transform.SetParent(sensPanel);
        sliderText.SetActive(true);
        /* Set the text properties */
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 1);
        text.text = "Hold right-click to test the mouse sensitivity";
        text.font = otherTextFont;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 1;
        text.resizeTextMaxSize = 10000;
        text.raycastTarget = false;

        /* Add text above the slider giving the sensitivity */
        GameObject sliderValue = new GameObject("Slider value", typeof(RectTransform));
        sensitivitySliderValueText = sliderValue.AddComponent<Text>();
        RectTransform valueRect = sliderValue.GetComponent<RectTransform>();
        sliderValue.transform.SetParent(sensPanel);
        sliderValue.SetActive(true);
        /* Set the text properties */
        sensitivitySliderValueText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 1);
        sensitivitySliderValueText.text = ""+sensitivity;
        sensitivitySliderValueText.font = otherTextFont;
        sensitivitySliderValueText.alignment = TextAnchor.MiddleCenter;
        sensitivitySliderValueText.resizeTextForBestFit = true;
        sensitivitySliderValueText.resizeTextMinSize = 1;
        sensitivitySliderValueText.resizeTextMaxSize = 10000;
        sensitivitySliderValueText.raycastTarget = false;
    }

    void SetupStartButton() {
        /*
         * Set the variables of the button that makes it the "Start" button
         */
        Button button = buttons[(int) Buttons.Start];
        button.name = "Start button";

        /* Setup the text of the button */
        SetupButtonText(button.GetComponentInChildren<Text>(), "START");
        startWidthRatio = startTextWidthRatio;

        /* Setup the event triggers for mouse clicks and hovers */
        button.onClick.AddListener(StartButtonClick);
        SetupButtonEvents(ref button, StartButtonMouseEnter, StartButtonMouseExit);
    }

    void SetupVideoButton() {
        /*
         * Set the variables of the video button
         */
        Button button = buttons[(int) Buttons.Video];
        button.name = "Video button";

        /* Setup the text of the button */
        SetupButtonText(button.GetComponentInChildren<Text>(), "VIDEO");

        /* Setup the event triggers for mouse clicks and hovers */
        button.onClick.AddListener(VideoButtonClick);
        SetupButtonEvents(ref button, VideoButtonMouseEnter, VideoButtonMouseExit);
    }

    void SetupSensButton() {
        /*
         * Set the variables of the button that makes it the "Sensitivity" button
         */
        Button button = buttons[(int) Buttons.Sens];
        button.name = "Sensitivity button";

        /* Setup the text of the button */
        SetupButtonText(button.GetComponentInChildren<Text>(), "SENSITIVITY");

        /* Setup the event triggers for mouse clicks and hovers */
        button.onClick.AddListener(SensButtonClick);
        SetupButtonEvents(ref button, SensButtonMouseEnter, SensButtonMouseExit);
    }

    void SetupQuitButton() {
        /*
         * Set the variables of the button that makes it the "Quit" button
         */
        Button button = buttons[(int) Buttons.Quit];
        button.name = "Quit button";

        /* Setup the text of the button */
        SetupButtonText(button.GetComponentInChildren<Text>(), "QUIT");

        /* Setup the event triggers for mouse clicks and hovers */
        button.onClick.AddListener(QuitButtonClick);
        SetupButtonEvents(ref button, QuitButtonMouseEnter, QuitButtonMouseExit);
    }

    
    /* ----------- Update Functions ------------------------------------------------------------- */

    void ExecuteElementFunctions(StateFunction[] stateFunction) {
        /*
         * Given an array of stateFunctions, run the function that is linked to the current state
         */

        for(int i = 0; i < stateFunction.Length; i++) {
            if(state == stateFunction[i].state) {
                stateFunction[i].function();
            }
        }
    }

    void UpdateTransitionValues() {
        /*
         * Update transition values and prevent them from going past their lower limit of 0. 
         * Do not update the state as we want the per-frame update functions to atleast run 
         * once the transition states have reached 0.
         */

        for(int i = 0; i < transitionStates.Length; i++) {
            /* Find the transition that links to the current state we are in */
            if(state == transitionStates[i].from) {
                /* Update the current transition value */
                UpdateTransitionValue(ref transitionStates[i].timeRemaining);
                i = transitionStates.Length;
            }
        }

        /* Update the more unique per-frame update values */
        switch(state) {
            /* Update the value used to determine when to quit the game */
            case MenuStates.Main:
                quitValueCurrent -= Time.deltaTime*quitValueDecreaseMod;
                if(quitValueCurrent < 0) { quitValueCurrent = 0; }
                break;
            /* In startup, only reduce the transition timer while fully loaded. Increment the animation timing. */
            case MenuStates.Startup:
                /* Reset the startup timing if the puzzle scene or the terrain have not yet loaded */
                if(!FinishedLoading()) {
                    ResetRemainingTime(state);
                }

                break;
        }

        /* In any of the states that will show the credit panel, increment the creditScrollValue value */
        if(isOutside && (
            state == MenuStates.Empty ||
            state == MenuStates.Main ||
            state == MenuStates.EmptyToMain ||
            state == MenuStates.MainToEmpty)) {
            creditScrollValue += Time.deltaTime/20f;
        }
    }

    void UpdateTransitionValue(ref float current) {
        /*
         * Update the given transition value and prevent it from going bellow 0
         */

        current -= Time.deltaTime;
        if(current < 0) {
            current = 0;
        }
    }

    void UpdateCurrentState() {
        /*
         * Check the current state and the transition values. Transition values will either be
         * at or above 0. Once they are at 0, we can transition to the next state.
         */

        for(int i = 0; i < transitionStates.Length; i++) {
            /* Find the transition that links to the current state we are in */
            if(state == transitionStates[i].from) {
                /* Check if the remaining time requires us to transition the next state */
                if(transitionStates[i].timeRemaining == 0) {
                    ChangeState(transitionStates[i].to);
                }
                i = transitionStates.Length;
            }
        }

        /* Check for the more unique transitions using non-Transition object values */
        switch(state) {
            case MenuStates.Main:
                /* Check if the player clicked the quit button enough */
                if(quitValueCurrent >= quitValueMax) { ChangeState(MenuStates.MainToQuit); }
                break;
        }
    }

    void UpdateHoverValues() {
        /*
         * Update the hover values in currentHoverTime. Only increase the values when the button is clickable
         */

        /* 
         * Force certain hover values onto buttons at certain states 
         */
        /* When the sensitivity menu is open(ing), force the button to be hovered */
        if(state == MenuStates.MainToSens || state == MenuStates.Sensitivity) {
            currentHoverState[(int) Buttons.Sens] = true;
        }
        /* When the video menu is open(ing), force the button to be hovered */
        if(state == MenuStates.MainToVideo || state == MenuStates.Video) {
            currentHoverState[(int) Buttons.Video] = true;
        }

        for(int i = 0; i < System.Enum.GetValues(typeof(Buttons)).Length; i++) {
            /* Increase the hover value if the button is clickable AND it's being hovered */
            if(IsButtonClickable((Buttons) i) && currentHoverState[i]) {
                currentHoverTime[i] += Time.deltaTime;
                if(currentHoverTime[i] > maxHoverTime) { currentHoverTime[i] = maxHoverTime; }
            }

            /*  Decrease the hover value if it's not being increased */
            else {
                currentHoverTime[i] -= Time.deltaTime;
                if(currentHoverTime[i] < 0) { currentHoverTime[i] = 0; }
            }
        }
    }

    void UpdateSensitivityCamera() {
        /*
         * If it's in the right state, update the camera's current rotation 
         * to test the new sensitivty of the mouse.
         * 
         * Also, if the player is not yet linked to the menu, do not run anything
         */

        if(playerController != null) {
            /* Holding right-click in the Sensitivity state will update the testingRotation value */
            if(state == MenuStates.Sensitivity && Input.GetMouseButton(1)) {

                /* Get the player's sensitivity modifier */
                float sens = playerController.mouseSens / playerController.mouseSensMod;

                /* Apply the mouse movement to the extraCamRotation */
                extraCamRotation += sens*new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
                recoveryTime = 0;
                savedRotation = extraCamRotation;
            }

            /* Animate the camera moving back to it's savedRotation */
            else {
                recoveryTime += Time.deltaTime;
                if(recoveryTime > 1) { recoveryTime = 1; }
                extraCamRotation = Mathf.Cos(recoveryTime*Mathf.PI/2f)*savedRotation;
            }

            /* Set the camera's rotation */
            playerController.extraCamRot = extraCamRotation;
        }
    }

    void UpdateSunFlare() {
        /*
         * Update the intensity mod of the sunflare depending on the current state of the menu. 
         * This is used in the intro of the menu opening to transition from the white startup to the menu.
         */
        float bonus = 1;
        
        Transition transition = GetTransitionFromState(state);
        /* Start fading the sun flare amount 5% in, ending 75% into the startup */
        float transitionBonus = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.05f, 0.75f);
        /* Use a cosine function to smooth out the flare changes */
        transitionBonus = (Mathf.Cos(transitionBonus*Mathf.PI)+1)/2f;

        /* Make the fade out use a sin function */
        bonus += transitionBonus*40;

        terrainController.UpdateSunFlareMod(bonus);
    }

    void UpdateLoadingAnimation() {
        /*
         * Update the loading boxes as we wait for the scene to load.
         */
         
        /* Position the boxes in their default position to begin with */
        for(int i = 0; i < loadingBoxes.Length; i++) {
            loadingBoxes[i].anchoredPosition = new Vector2(-(i+0.5f)*boxSize -boxSepperationSize*(i + 1), +boxSize/2f + heightFromBottom);
        }

        /* Position the boxes either in or out of view relative to the loadingAnimationVisible value */
        for(int i = 0; i < loadingBoxes.Length; i++) {
            loadingBoxes[i].anchoredPosition += (1 - Mathf.Sin((Mathf.PI/2f)*(loadingAnimationVisibility[i])))*new Vector2(0, -boxSize -heightFromBottom*5);
        }

        /* Animate each loadingBox relative to the current time spent loading */
        for(int i = 0; i < loadingBoxes.Length; i++) {
            AnimateLoadingBox(i, loadingAnimationTime - loadingAnimationOffset*i);
        }

        /* Get the progress of the loading process and update the loading boxes */
        UpdateLoadingProgress();
    }

    void UpdateLoadingProgress() {
        /*
         * Resize the inner boxes of each loading box relative to the progress of the loading time.
         * The progress will track both the scene loading and the terrain generation.
         * 
         * Through multiple test while loading the game, it was discovered that the game spends 
         * most of it's scene loading time between the ranges of [0.0107, 0.012]. Therefore, adjust the
         * puzzleScene progress to use scale around that range.
         * 
         * LoadRatio determines how much of the progress uses the scene loading (use a range of [0, 1]).
         */
        float loadRatio = 0.66f;
        float progress, boxSize;
        
        /* Get the progress of the scene loading */
        if(!puzzleSceneLoaded) {
            progress = loadRatio*RangeBetween(puzzleSceneCoroutine.progress, 0.0107f, 0.012f);
        }
        /* Get the progress of the terrain generation */
        else {
            progress = loadRatio + (1 - loadRatio)*terrainController.GetLoadingPercent();
        }

        /* Go through each box and adjust their inner box size to meassure the loading progress */
        for(int i = 0; i < interiorLoadingBoxes.Length; i++) {
            boxSize = 0.1f + 0.4f*RangeBetween(progress, ((i+0f)/loadingBoxes.Length), ((i+1f)/loadingBoxes.Length));
            interiorLoadingBoxes[(interiorLoadingBoxes.Length-1) - i].anchorMax = new Vector2((1 - boxSize), (1 - boxSize));
            interiorLoadingBoxes[(interiorLoadingBoxes.Length-1) - i].anchorMin = new Vector2(boxSize, boxSize);

            /* If the box has been filled, set it's loadingAnimationState to false so it starts leaving the screen */
            if(boxSize >= 0.5f) {
                loadingAnimationState[(interiorLoadingBoxes.Length-1) - i] = false;
            }
        }
    }

    void AnimateLoadingBox(int boxIndex, float loadingTime) {
        /*
         * Animate the loading box given by the index with the given timing.
         */
        RectTransform loadingBox = loadingBoxes[boxIndex];
        Vector2 animation = Vector2.zero;
        //Adjust the time so it cycles through a range of [0, 1]
        loadingTime = Mathf.Sin(Mathf.PI*((loadingTime) % loadingAnimationLoopTime)/loadingAnimationLoopTime);

        /* Animate the position of the box */
        animation = new Vector2(0, heightFromBottom*Mathf.Sin(Mathf.PI*loadingTime));
        //Add a set amount of height depending on how far into the time it is
        animation += new Vector2(0, heightFromBottom*loadingTime);
        loadingBox.anchoredPosition += animation;

        /* Animate the rotation of the box */
        float rotationTime = Mathf.Sin(-Mathf.PI*0.025f + Mathf.PI*1.05f*loadingTime);
        if(rotationTime < 0) { rotationTime = 0; }
        loadingBox.localEulerAngles = new Vector3(0, 0, 180*rotationTime);
    }
    

    /* ----------- UI Element Update Functions ------------------------------------------------------------- */

    #region Credit Panel Updates
    void UCreditPanelEmptyOrMain() {
        /*
         * Place the panel in the center. The panel will only be visible if the player is outside.
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform mainPanel = panelRects[panelEnum];

        CreditPanelPositionUpdate(creditScrollValue);
    }

    void UCreditPanelVideoToMain() {
        /*
         * Animate the credits panel as it rotates from the top of the screen to be viewed by the player
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform rect = panelRects[panelEnum];

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Make the panel rotate down from the top into it's current scroll position */
        rect.pivot = new Vector2(0.5f, 1);
        rect.localEulerAngles = new Vector3(90 - 90*transitionFade, 0, 0);
        CreditPanelPositionUpdate(creditScrollValue);
    }

    void UCreditPanelMainToVideo() {
        /*
         * Animate the credits panel as it rotates off the screen from the bottom edge
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform rect = panelRects[panelEnum];

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Make the panel rotate down off screen from it's current scroll position */
        rect.pivot = new Vector2(0.5f, 0);
        rect.localEulerAngles = new Vector3(90*transitionFade, 0, 0);
        CreditPanelPositionUpdate(creditScrollValue);
    }

    void UCreditPanelMainToSens() {
        /*
         * Animate the credits panel as it slides back down off-screen
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform rect = panelRects[panelEnum];

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = 1 - Mathf.Cos((Mathf.PI/2f)*AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1));

        /* Get the current scrolling position of the panel */
        CreditPanelPositionUpdate(creditScrollValue);

        /* Slide the panel down */
        float panelHeight = screenHeight*panelsHeight[panelEnum];
        rect.anchoredPosition -= new Vector2(0, transitionFade*panelHeight);
    }

    void UCreditPanelSensToMain() {
        /*
         * Animate the credits panel as it slides back up into it's current scrolling position from the right
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform rect = panelRects[panelEnum];

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Get the current scrolling position of the panel */
        CreditPanelPositionUpdate(creditScrollValue);

        /* Slide the panel into view from the right */
        float panelWidth = screenWidth*panelsWidth[panelEnum];
        rect.anchoredPosition -= new Vector2(-(1 - transitionFade)*panelWidth, 0);
    }

    void UCreditPanelQuit() {
        /*
         * Slide the panel off the right side of the screen
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform rect = panelRects[panelEnum];

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = 1 - Mathf.Cos((Mathf.PI/2f)*AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1));

        /* Get the current scrolling position of the panel */
        CreditPanelPositionUpdate(creditScrollValue);

        /* Slide the panel out of view to the right */
        float panelWidth = screenWidth*panelsWidth[panelEnum];
        rect.anchoredPosition -= new Vector2(-(transitionFade)*panelWidth, 0);
    }

    void CreditPanelPositionUpdate(float sideRatio) {
        /*
         * Position the panel from either above the bottom line or bellow
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform rect = panelRects[panelEnum];

        /* Get the proper position of the panel */
        float panelHeight = screenHeight*panelsHeight[panelEnum];

        /* [0, 1] controls it's Y position */
        rect.anchoredPosition = new Vector2(0, -panelHeight*(1 - AdjustRatio(sideRatio, 0, 1)));
        rect.sizeDelta = new Vector2(0, 0);
    }

    void CreditPanelReset() {
        /*
         * Reset the sizes of the cover panel. The content does not need to be resized.
         */
        int panelEnum = (int) Panels.Credit;
        RectTransform mainPanel = panelRects[panelEnum];
    }
    #endregion

    #region Cover Panel Updates
    void UCoverPanelStartup() {
        /*
         * Have the panel fade out from pure black to completely opaque for the intro
         */
        int panelEnum = (int) Panels.Cover;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        float transitionFade;
        Transition transition = GetTransitionFromState(state);

        /* Start the cover panel as a white cover that starts fading out 5% in and is completely gone at 25%*/
        transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.05f, 0.25f);
        rectImage.color = new Color(1, 1, 1, 1 - transitionFade);



        /* Only run the following lines if the playerController and terrainController are linked to the menu */
        if(playerController != null && terrainController != null) {
            /* Update the camera's rotation and the sun flare's power during the startup */
            UpdateSunFlare();
            transitionFade = TimeRatio(transition.timeRemaining, transition.timeMax);
            /* Start adjusting the camera's position 50% into the startup, ending 90% in */
            float ratio = AdjustRatio(transitionFade, 0.5f, 0.9f);
            /* Use a cosine function to smooth out the movement */
            ratio = (Mathf.Cos(ratio*Mathf.PI)+1)/2f;

            /* Get the angle that will make the camera face the sun */
            float camX = -terrainController.directionalLight.transform.eulerAngles.x;
            float camY = terrainController.directionalLight.transform.eulerAngles.y;
            /* Adjust the roation amount to prevent rotations above 180 degrees */
            if(camX / 360 > 0) { camX -= ((int) camX/360)*360; }
            if(camX / 180 > 0) { camX -= ((int) camX/180)*360; }
            if(camY / 360 > 0) { camY -= ((int) camY/360)*360; }
            if(camY / 180 > 0) { camY -= ((int) camY/180)*360; }
            extraCamRotation = new Vector3(camX, camY, 0);

            /* Reduce the angle as we are leaving the startup */
            extraCamRotation *= ratio;

            /* Apply the angle to the camera */
            playerController.extraCamRot = extraCamRotation;
        }
    }

    void UCoverPanelMainToQuit() {
        /*
         * While the game is about to quit, Change the color of the cover panel to block the view.
         */
        int panelEnum = (int) Panels.Cover;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //The transition face value starts fading 25% into the transition and finishes 80% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.25f, 0.8f);

        /* Fade the color out relative to the remaining time before the game closes */
        rectImage.color = new Color(0, 0, 0, transitionFade);
    }

    void CoverPanelReset() {
        /*
         * Reset the sizes of the cover panel 
         */
        RectTransform mainPanel = panelRects[(int) Panels.Cover];

        /* Set the anchors so it becomes a full stretch layout */
        mainPanel.anchorMin = new Vector2(0, 0);
        mainPanel.anchorMax = new Vector2(1, 1);

        /* Set the sizes to match the screen size */
        mainPanel.anchoredPosition = new Vector2(0, 0);
        mainPanel.sizeDelta = new Vector2(0, 0);
    }
    #endregion

    #region Sens Panel Updates
    void USensPanelSensitivity() {
        /*
         * While in the Sensitivity state, make sure the panel occupies the bottom edge of the screen
         */

        /* Place the panel so it can be seen */
        SensPanelPositionUpdate(1);
    }

    void USensPanelMainToSens() {
        /*
         * Animate the panel comming into view
         */

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place the panel so it can be seen */
        SensPanelPositionUpdate(transitionFade);
    }

    void USensPanelSensToMain() {
        /*
         * Animate the panel leaving the view
         */

        /* Use a cos function to smooth out the animation */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = Mathf.Cos((Mathf.PI/2f)*AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1));

        /* Place the panel so it can be seen */
        SensPanelPositionUpdate(2 - transitionFade);
    }

    void SensPanelPositionUpdate(float sideRatio) {
        /*
         * Update the position of the Sensitivity Panel. It is anchored to the bottom-right corner.
         * 
         * The given sideRatio value determines where the panel is placed. The potential values are:
         * 0 - Place the wall above the bottom line and to the right of the right wall
         * 1 - Place the wall above the bottom line and to the left of the right wall
         * 2 - Place the wall bellow the bottom line and to the left of the right wall
         */
        int panelEnum = (int) Panels.Sens;
        RectTransform rect = panelRects[panelEnum];

        /* Get the proper position of the panel */
        float panelWidth = screenWidth*panelsWidth[panelEnum];
        float panelHeight = screenHeight*panelsHeight[panelEnum];

        /* [0, 1] controls it's X position while [1, 2] controls it's Y position */
        panelWidth = panelWidth/2f - panelWidth*AdjustRatio(sideRatio, 0, 1);
        panelHeight = panelHeight/2f - panelHeight*AdjustRatio(sideRatio, 1, 2);
        rect.anchoredPosition = new Vector2(panelWidth, panelHeight);
    }

    void SensPanelReset() {
        /*
         * Reset the size and position of the panel
         */
        int panelEnum = (int) Panels.Sens;
        RectTransform sensPanel = panelRects[panelEnum];

        /* Set the anchors so it's position based in the bottom right corner */
        sensPanel.anchorMin = new Vector2(1, 0);
        sensPanel.anchorMax = new Vector2(1, 0);

        /* Reset the size of the panel */
        float panelWidth = Screen.width*panelsWidth[panelEnum];
        float panelHeight = Screen.height*panelsHeight[panelEnum];
        sensPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        /* Adjust the size of the text above and bellow the slider */
        RectTransform bellowText = sensPanel.GetChild(1).GetComponent<RectTransform>();
        RectTransform aboveText = sensPanel.GetChild(2).GetComponent<RectTransform>();
        /* Set the sizes of the sens value text above */
        aboveText.anchorMin = new Vector2(0, 0.8f);
        aboveText.anchorMax = new Vector2(1, 0.8f);
        aboveText.anchoredPosition = new Vector2(0, 0);
        aboveText.sizeDelta = new Vector2(0, panelHeight/3f);
        aboveText.GetComponent<Outline>().effectDistance = panelHeight*new Vector2(0.009f, 0.009f);
        /* Set the sizes of the description text bellow */
        bellowText.anchorMin = new Vector2(0, 0.2f);
        bellowText.anchorMax = new Vector2(1, 0.2f);
        bellowText.anchoredPosition = new Vector2(0, 0);
        bellowText.sizeDelta = new Vector2(0, panelHeight/4f);
        bellowText.GetComponent<Outline>().effectDistance = panelHeight*new Vector2(0.009f, 0.009f);
        SensPanelPositionUpdate(2);
    }
    #endregion

    #region Video Panel Updates
    void UVideoPanelVideo() {
        /*
         * While in the Video state, make sure the panel occupies the right side of the screen
         */

        /* Place the panel so it can be seen */
        VideoPanelPositionUpdate(1);
    }

    void UVideoPanelMainToVideo() {
        /*
         * Animate the panel comming into view
         */

        /* Get the transition ratio for the current state */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place the panel so it can be seen */
        VideoPanelPositionUpdate(transitionFade);
    }

    void UVideoPanelVideoToMain() {
        /*
         * Animate the panel leaving the view
         */

        /* Use a cos function to smooth out the animation */
        Transition transition = GetTransitionFromState(state);
        float transitionFade = Mathf.Cos((Mathf.PI/2f)*AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1));

        /* Place the panel so it can be seen */
        VideoPanelPositionUpdate(2 - transitionFade);
    }

    void VideoPanelPositionUpdate(float sideRatio) {
        /*
         * Update the position of the Video Panel. It is anchored to the right side of the screen.
         * 
         * The given sideRatio value determines where the panel is placed. The potential values are:
         * 0 - Place the wall on the top edge and rotated
         * 1 - Place the wall in the center and with no rotation
         * 2 - Place the wall on the bottom edge and rotated
         */
        int panelEnum = (int) Panels.Video;
        RectTransform rect = panelRects[panelEnum];

        /* Set the rotation ratio */
        rect.pivot = new Vector2(0.5f, 1 - sideRatio/2f);
        rect.localEulerAngles = new Vector3(90 - 180*sideRatio/2f, 0, 0);

        /* Get the sizes of the panel */
        float panelWidth = screenWidth*panelsWidth[panelEnum];
        float panelHeight = screenHeight*panelsHeight[panelEnum];

        /* Position the Y position of the panel */
        panelWidth = -panelWidth/2f;
        panelHeight = panelHeight/2f - (panelHeight/2f)*sideRatio;
        rect.anchoredPosition = new Vector2(panelWidth, panelHeight);
    }

    void VideoPanelReset() {
        /*
         * Reset the position and size of the video panel and it's components
         */
        int panelEnum = (int) Panels.Video;
        RectTransform panelRect;
        RectTransform toggleRect;
        RectTransform checkBoxRect;
        RectTransform videoPanel = panelRects[panelEnum];
        Text text;
        float optionPanelHeight = buttonHeight/2f;
        float fromCenterToTopOfVideoPanel = (screenHeight*panelsHeight[panelEnum])/2f - buttonHeight/2f;

        /* Set the new size of the video panel */
        videoPanel.sizeDelta = new Vector2(screenWidth*panelsWidth[panelEnum], screenHeight*panelsHeight[panelEnum]);

        /*
         * Windowed, Mouse lock & Mouse focus options
         */
        for(int i = 0; i < 3; i++) {
            /* Get the components of the option */
            panelRect = videoPanel.GetChild(i).GetComponent<RectTransform>();
            toggleRect = panelRect.GetChild(1).GetComponent<RectTransform>();
            checkBoxRect = toggleRect.GetChild(0).GetComponent<RectTransform>();

            /* Resize the panel */
            panelRect.anchoredPosition = new Vector2(0, fromCenterToTopOfVideoPanel -i*optionPanelHeight*3/2f);
            panelRect.sizeDelta = new Vector2(0, optionPanelHeight);

            /* Resize the toggle and checkbox */
            toggleRect.sizeDelta = new Vector2(optionPanelHeight, 0);
            checkBoxRect.anchoredPosition = new Vector2(optionPanelHeight, 0);
            checkBoxRect.sizeDelta = new Vector2(optionPanelHeight, 0);
        }

        /*
         * Resolution & Framerate dropdowns
         */
        for(int i = 0; i < 2; i++) {
            /* Get the components of the dropdown */
            panelRect = videoPanel.GetChild(videoPanel.childCount-1 - i).GetComponent<RectTransform>();
            RectTransform dropdownRect = panelRect.GetChild(1).GetComponent<RectTransform>();
            RectTransform dropdownContent = dropdownRect.GetChild(2).GetChild(0).GetChild(0).GetComponent<RectTransform>();
            RectTransform dropdownItem = dropdownContent.GetChild(0).GetComponent<RectTransform>();

            /* Set the sizes of the panel */
            panelRect.anchoredPosition = new Vector2(0, fromCenterToTopOfVideoPanel -(videoPanel.childCount-1.5f)*optionPanelHeight*3/2f);
            panelRect.sizeDelta = new Vector2(0, optionPanelHeight);

            /* Update the dropdown rect's size */
            float wdithRatio;
            if(i == 0) {
                wdithRatio = framerateWidthRatio;
            }
            else {
                wdithRatio = resolutionWidthRatio;
            }
            dropdownRect.anchoredPosition = new Vector2((optionPanelHeight/2f)*wdithRatio/2f, 0);
            dropdownRect.sizeDelta = new Vector2((optionPanelHeight/2f)*wdithRatio, 0);

            /* Update the dropdown template's height to reflect the new height of the window */
            dropdownRect.GetChild(2).GetComponent<RectTransform>().sizeDelta = new Vector3(0, screenHeight/2f);

            /* Update the size of the content & items of the resolution dropdown list */
            float optionHeight = panelRect.GetComponent<RectTransform>().sizeDelta.y;
            dropdownContent.sizeDelta = new Vector2(0f, optionHeight*0.75f);
            dropdownItem.sizeDelta = new Vector2(0, dropdownContent.sizeDelta.y);
            dropdownRect.GetChild(0).GetComponent<Outline>().effectDistance = 0.01f*new Vector2(optionHeight, optionHeight);
            dropdownItem.GetChild(1).GetComponent<Outline>().effectDistance = 0.01f*new Vector2(optionHeight, optionHeight);
        }

        /*
         * Adjust the outline of the text of each option to reflect the size change
         */
        for(int i = 0; i < videoPanel.childCount; i++) {
            panelRect = videoPanel.GetChild(i).GetComponent<RectTransform>();
            text = panelRect.GetChild(0).GetComponent<Text>();
            float optionHeight = panelRect.GetComponent<RectTransform>().sizeDelta.y;
            text.GetComponent<Outline>().effectDistance = 0.025f*new Vector2(optionHeight, optionHeight);
        }

        VideoPanelPositionUpdate(0);
    }
    #endregion

    #region Start Button Updates
    void UStartButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        //Start fading in the button 90% into the intro, finish 100% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.875f, 1f);

        /* Position the button to already be in it's main position */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1);

        /* Change the opacity to reflect the transition state */
        Color col = button.GetComponentInChildren<Text>().color;
        button.GetComponentInChildren<Text>().color = new Color(col.r, col.g, col.b, transitionFade);
    }

    void UStartButtonEmptyToMain() {
        /*
         * During this transition state, move the button so it's back onto the screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRatio);

        /* Slide the button into it's main position from off-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(transitionFade);
    }

    void UStartButtonMainToEmpty() {
        /*
         * During this transition state, Quickly move the button off-screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRatio);

        /* Slide the button off-screen from it's main position */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1 - transitionFade);
    }

    void UStartButtonMain() {
        /*
         * Make sure the button's position is properly updated after updating it's hover value
         */

        /* Place the button in it's main position */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1);
    }

    void UStartButtonMainToIntro() {
        /*
         * Update the start button's position and outlines to animate it fading away in a unique way.
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        //Use the transition value very basically
        Transition transition = GetTransitionFromState(state);
        float transitionFade = TimeRatio(transition.timeRemaining, transition.timeMax);

        /* Make sure the button is properly positionned. Don't run any changes through StartButtonHoverUpdate */
        //StartButtonHoverUpdate();
        StartButtonPositionUpdate(1);

        /* Update the color of the text and change the outline's distance to reflect the current fade value */
        button.GetComponentInChildren<Text>().color -= new Color(0, 0, 0, transitionFade);
        outlines[0].effectDistance = new Vector2(2 + 5f*transitionFade*buttonHeight, 2 + 5f*transitionFade*buttonHeight);
        outlines[1].effectDistance = new Vector2(2 + 10f*transitionFade*buttonHeight, 2 + 10f*transitionFade*buttonHeight);
        outlines[0].effectColor = new Color(0, 0, 0, 0.75f - transitionFade);
        outlines[1].effectColor = new Color(0, 0, 0, 0.75f - transitionFade);

        /* Place the button off the screen on the final frame in the MainToIntro state */
        if(transition.timeRemaining == 0) {
            /* Position the button off screen */
            StartButtonPositionUpdate(0);
        }
    }

    void UStartButtonMainToQuit() {
        /*
         * Animate the button slidding off the left side of the screen as the game quits
         */
        //The transition starts fading the button at the start and ends 50% through
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*(startWidthRatio*startBonusSize)/largestRatio);

        /* Move the button from the main position to off-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1 - transitionFade);
    }
    
    void UStartButtonMainToSens() {
        /*
         * Slide the button off the left side
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRatio);

        /* Move the button from the main position to off-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1 - transitionFade);
    }

    void UStartButtonSensToMain() {
        /*
         * Slide the button back into view from the left side
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRatio);

        /* Move the button into the main position from off-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(transitionFade);
    }

    void StartButtonPositionUpdate(float sideRatio) {
        /*
         * Update the position of the start button. The start button will be anchored to the left wall.
         * 
         * Depending on the given side value, place it either on the right or left side of the wall.
         * 0 is completely on the left and 1 is completely on the right.
         */
        int buttonEnum = (int) Buttons.Start;
        RectTransform rect = buttonRects[buttonEnum];

        rect.position = new Vector3(-(rect.sizeDelta.x + buttonEdgeOffset)/2f + (rect.sizeDelta.x + buttonEdgeOffset)*sideRatio, canvasRect.position.y/1.5f + buttonHeight/2f, 0);
    }

    void StartButtonHoverUpdate() {
        /*
         * Set the sizes and colors of the start button to reflect it's current hover value
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = HoverRatio(buttonEnum, true);
        float extraHoverWidth = -hoverReductionAmount*(1 - hoverRatio)*buttonHeight *startBonusSize*startWidthRatio/largestRatio;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(startBonusSize*buttonHeight*startWidthRatio + extraHoverWidth, startBonusSize*buttonHeight);

        /* Add a portion of the height into the label object to prevent the text from overflowing using the current font */
        rect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        rect.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(0, -20f * (buttonHeight/100f));

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = (buttonHeight/100f)*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = (buttonHeight/100f)*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[0].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
        outlines[1].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
    }

    void StartButtonReset() {
        /*
         * Reset the sizes of the start button and position it off-screem
         */

        StartButtonPositionUpdate(0);
        StartButtonHoverUpdate();
    }
    #endregion

    #region Video Button Updates
    void UVideoButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main position
         */
        int buttonEnum = (int) Buttons.Video;
        Button button = buttons[buttonEnum];
        //Start fading in the button 90% into the intro, finish 100% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.9f, 1f);

        /* Position the button to already be in it's main position */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1);

        /* Change the opacity to reflect the transition state */
        Color col = button.GetComponentInChildren<Text>().color;
        button.GetComponentInChildren<Text>().color = new Color(col.r, col.g, col.b, transitionFade);
    }

    void UVideoButtonEmptyToMain() {
        /*
         * During this transition state, move the button so it's back onto the screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRatio);

        /* Slide the button into it's main position from off-screen */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(transitionFade);
    }

    void UVideoButtonMainToEmpty() {
        /*
         * During this transition state, Quickly move the button off-screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRatio);

        /* Slide the button off-screen from it's main position */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1 - transitionFade);
    }

    void UVideoButtonMainToIntro() {
        /*
         * Animate the video button when entering the intro. The button slides out to the left
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRatio);

        /* Move the button from the main to off-screen position */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1 - transitionFade);
    }

    void UVideoButtonMainToQuit() {
        /*
         * Animate the button slidding off the left side of the screen as the game quits
         */
        //The transition starts fading the button at the start and ends 50% through
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*videoWidthRatio/largestRatio);

        /* Move the button from the main position to off-screen */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1 - transitionFade);
    }

    void UVideoButtonMainToSens() {
        /*
         * Animate the button slidding off the left side of the screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRatio);

        /* Place the button in it's main position */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1 - transitionFade);
    }

    void UVideoButtonMain() {
        /*
         * Place the button in it's default position
         */

        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1);
    }

    void UVideoButtonVideo() {
        /*
         * Place the button in it's default position
         */

        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1);
    }

    void UVideoButtonMainToVideo() {
        /*
         * Place the button in it's default position
         */

        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1);
    }

    void UVideoButtonVideoToMain() {
        /*
         * Place the button in it's default position
         */

        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(1);
    }

    void UVideoButtonSensToMain() {
        /*
         * Slide the button back into view
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (videoWidthRatio)/largestRatio);

        /* Move the button from the main position to off-screen */
        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(transitionFade);
    }

    void VideoButtonPositionUpdate(float sideRatio) {
        /*
         * Update the position of the video button. The button will be anchored to the left wall
         * and will be directly bellow the start button.
         * 
         * Depending on the given side value, place it either on the right or left side of the wall.
         * 0 is completely on the left and 1 is completely on the right.
         */
        int buttonEnum = (int) Buttons.Video;
        RectTransform rect = buttonRects[buttonEnum];
        /* The sens button will be placed bellow the start button */
        RectTransform aboveButton = buttonRects[(int) Buttons.Start];

        float relativeHeight = aboveButton.position.y - (aboveButton.sizeDelta.y + buttonSepperatorDistance)/2f - buttonHeight/2f;
        rect.position = new Vector3(-(rect.sizeDelta.x + buttonEdgeOffset)/2f + (rect.sizeDelta.x + buttonEdgeOffset)*sideRatio, relativeHeight, 0);
    }

    void VideoButtonHoverUpdate() {
        /*
         * Set the sizes and colors of the video button to reflect it's current hover value
         */
        int buttonEnum = (int) Buttons.Video;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = HoverRatio(buttonEnum, true);
        float extraHoverWidth = -hoverReductionAmount*(1 - hoverRatio)*buttonHeight *videoWidthRatio/largestRatio;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(buttonHeight*videoWidthRatio + extraHoverWidth, buttonHeight);

        /* Add a portion of the height into the label object to prevent the text from overflowing using the current font */
        rect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        rect.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(0, -20f * (buttonHeight/100f));

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = (buttonHeight/100f)*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = (buttonHeight/100f)*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[0].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
        outlines[1].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
    }

    void VideoButtonReset() {
        /*
         * Reset the video button's size and position
         */

        VideoButtonHoverUpdate();
        VideoButtonPositionUpdate(0);
    }
    #endregion

    #region Sensitivity Button Updates
    void USensButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Sens;
        Button button = buttons[buttonEnum];
        //Start fading in the button 90% into the intro, finish 100% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.925f, 1f);

        /* Position the button to already be in it's main position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1);

        /* Change the opacity to reflect the transition state */
        Color col = button.GetComponentInChildren<Text>().color;
        button.GetComponentInChildren<Text>().color = new Color(col.r, col.g, col.b, transitionFade);
    }

    void USensButtonEmptyToMain() {
        /*
         * During this transition state, move the button so it's back onto the screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, sensWidthRatio/largestRatio);

        /* Slide the button into it's main position from off-screen */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(transitionFade);
    }

    void USensButtonMainToEmpty() {
        /*
         * During this transition state, Quickly move the button off-screen
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, sensWidthRatio/largestRatio);

        /* Slide the button off-screen from it's main position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1 - transitionFade);
    }

    void USensButtonMain() {
        /*
         * Make sure the button's position is properly updated after updating it's hover value
         */

        /* Place the button in it's main position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1);
    }

    void USensButtonMainToVideo() {
        /*
         * Slide the button off the left side
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (sensWidthRatio)/largestRatio);

        /* Move the button from the main position to off-screen */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1 - transitionFade);
    }

    void USensButtonVideoToMain() {
        /*
         * Slide the button into view from the left side
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (sensWidthRatio)/largestRatio);

        /* Move the button into the main position from off-screen */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(transitionFade);
    }

    void USensButtonMainToIntro() {
        /*
         * Animate the sensitivity button when entering the intro. The button slides out to the left
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, sensWidthRatio/largestRatio);

        /* Move the button from the main to off-screen position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1 - transitionFade);
    }

    void USensButtonMainToQuit() {
        /*
         * Animate the button slidding off the left side of the screen as the game quits
         */
        //The transition starts fading the button at the start and ends 50% through
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*sensWidthRatio/largestRatio);

        /* Move the button from the main position to off-screen */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1 - transitionFade);
    }

    void USensButtonMainToSens() {
        /*
         * Keep the button in it's main position
         */

        /* Place the button in it's main position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1);
    }

    void USensButtonSensToMain() {
        /*
         * Keep the button in it's main position
         */

        /* Place the button in it's main position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1);
    }

    void USensButtonSensitivity() {
        /*
         * Make sure the button is in the main position
         */

        /* Place the button in it's main position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(1);
    }

    void SensButtonPositionUpdate(float sideRatio) {
        /*
         * Update the position of the sensitivity button. The button will be anchored to the left wall
         * and will be directly bellow the video button.
         * 
         * Depending on the given side value, place it either on the right or left side of the wall.
         * 0 is completely on the left and 1 is completely on the right.
         */
        int buttonEnum = (int) Buttons.Sens;
        RectTransform rect = buttonRects[buttonEnum];
        /* The sens button will be placed bellow the video button */
        RectTransform aboveButton = buttonRects[(int) Buttons.Video];

        float relativeHeight = aboveButton.position.y - (aboveButton.sizeDelta.y + buttonSepperatorDistance)/2f - buttonHeight/2f;
        rect.position = new Vector3(-(rect.sizeDelta.x + buttonEdgeOffset)/2f + (rect.sizeDelta.x + buttonEdgeOffset)*sideRatio, relativeHeight, 0);
    }

    void SensButtonHoverUpdate() {
        /*
         * Set the sizes and colors of the sensitivity button to reflect it's current hover value
         */
        int buttonEnum = (int) Buttons.Sens;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = HoverRatio(buttonEnum, true);
        float extraHoverWidth = -hoverReductionAmount*(1 - hoverRatio)*buttonHeight * sensWidthRatio/largestRatio;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(buttonHeight*sensWidthRatio + extraHoverWidth, buttonHeight);

        /* Add a portion of the height into the label object to prevent the text from overflowing using the current font */
        rect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        rect.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(0, -20f * (buttonHeight/100f));

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = (buttonHeight/100f)*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = (buttonHeight/100f)*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[0].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
        outlines[1].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
    }

    void SensButtonReset() {
        /*
         * Reset the sensitivity button's position and size
         */

        SensButtonHoverUpdate();
        SensButtonPositionUpdate(0);
    }
    #endregion

    #region Quit Button Updates
    void UQuitButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Quit;
        Button button = buttons[buttonEnum];
        //Start fading in the button 90% into the intro, finish 100% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.95f, 1f);

        /* Position the button at it's main position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1);

        /* Change the opacity to reflect the transition state */
        Color col = button.GetComponentInChildren<Text>().color;
        button.GetComponentInChildren<Text>().color = new Color(col.r, col.g, col.b, transitionFade);
    }

    void UQuitButtonEmptyToMain() {
        /*
         * Update the quit button as the menu enters the main from empty
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1*quitWidthRatio/largestRatio);

        /* Move the button from it's off-screen position to the main position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(transitionFade);
    }

    void UQuitButtonMainToEmpty() {
        /*
         * Update the quit button as the menu quickly closes
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1*quitWidthRatio/largestRatio);

        /* Move the button from it's main position to off-screen */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1 - transitionFade);
    }

    void UQuitButtonMain() {
        /*
         * Update the quit button while in the Main state.
         */

        /* Keep the button in it's main position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1);
    }
    
    void UQuitButtonMainToIntro() {
        /*
         * Animate the quit button when entering the intro. The quit button slides out to the left
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1*quitWidthRatio/largestRatio);

        /* Move the button from the main to off-screen position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1 - transitionFade);
    }

    void UQuitButtonMainToQuit() {
        /*
         * Animate the button as it approaches the center of the screen
         */

        /* Move the button from the main to off-screen position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1);
    }

    void UQuitButtonMainToSens() {
        /*
         * Update the quit button as it slides out of view
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1*quitWidthRatio/largestRatio);

        /* Move the button from it's main position to off-screen */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1 - transitionFade);
    }

    void UQuitButtonSensToMain() {
        /*
         * Update the quit button as it comes back into view
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1*quitWidthRatio/largestRatio);

        /* Move the button from it's off-screen position to the main position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(transitionFade);
    }

    void QuitButtonPositionUpdate(float sideRatio) {
        /*
         * Update the position of the quit button. The quit button will be anchored 
         * to the left wall and bellow the start button.
         * 
         * Depending on the given side value, place it either on the right or left side of the wall.
         * 0 is completely on the left and 1 is completely on the right.
         */
        int buttonEnum = (int) Buttons.Quit;
        RectTransform rect = buttonRects[buttonEnum];
        /* The button that this quit button will be placed bellow */
        RectTransform aboveButton = buttonRects[(int) Buttons.Sens];

        float relativeHeight = aboveButton.position.y - (aboveButton.sizeDelta.y + buttonSepperatorDistance)/2f - rect.sizeDelta.y/2f;
        rect.position = new Vector3(-(rect.sizeDelta.x + buttonEdgeOffset)/2f + (rect.sizeDelta.x + buttonEdgeOffset)*sideRatio, relativeHeight, 0);

        /* Depending on the current quitValueCurrent value, adjust certain aspects of the quit button */
        Text quitText = rect.GetChild(0).GetComponent<Text>();
        RectTransform quitRect = rect.GetChild(0).GetComponent<RectTransform>();
        if(quitText != null) {
            float ratioColor = AdjustRatio(quitValueCurrent, 0, quitValueMax);
            float ratioPos = AdjustRatio(quitValueCurrent, quitValueMax*0.3f, quitValueMax);
            float ratioSize = AdjustRatio(quitValueCurrent, quitValueMax*0.6f, quitValueMax);
            float ratioDist = AdjustRatio(quitValueCurrent, 0, quitValueMax*0.7f);
            float ratioOutlineCol = AdjustRatio(quitValueCurrent, 0.25f, quitValueMax*0.9f);
            float baseExtraWidth = 0;
            float baseExtraHeight = 0;
            float quitTimeRatio = 1;
            /* Increase some values if we are transitionning to quitting */
            if(state == MenuStates.MainToQuit) {
                Transition transition = GetTransitionFromState(state);
                quitTimeRatio = transition.timeRemaining/transition.timeMax;
                baseExtraHeight = (1 - quitTimeRatio)*buttonHeight*0.075f;
                baseExtraWidth = (1 - quitTimeRatio)*baseExtraHeight*quitBonusSize*quitWidthRatio;
                ratioDist += 10*(1 - quitTimeRatio);
            }
            //Set the color of the quit button
            quitText.color = new Color(1, 1 - ratioColor, 1 - ratioColor, 1);
            //Reposition the text position
            quitRect.anchoredPosition += ratioPos*10*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
            //Resize the text's size
            if(ratioSize > 0) {
                //Increase the size
                bonusQuitSize += ratioSize*2*new Vector2(Random.Range(-0.25f, 1f), Random.Range(-0.25f, 1f)) + 2*new Vector2(baseExtraHeight, baseExtraWidth);
            }
            else {
                //Start resetting the size to reach back to 0
                bonusQuitSize = (quitValueCurrent/quitValueMax*0.9f)*bonusQuitSize;
            }
            quitRect.sizeDelta += bonusQuitSize;
            //reposition the outline's distance
            Outline[] outlines = quitRect.gameObject.GetComponents<Outline>();
            outlines[0].effectDistance += ratioDist*4*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
            outlines[1].effectDistance += ratioDist*4*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
            //Recolor the outline's color
            outlines[0].effectColor = new Color(ratioOutlineCol*0.5f, 0, 0, outlines[0].effectColor.a);
            outlines[1].effectColor = new Color(ratioOutlineCol*0.5f, 0, 0, outlines[1].effectColor.a);

            /* Depending on how long along the mainToQuit transition we are in, reposition the text near the center */
            if(state == MenuStates.MainToQuit) {
                rect.position = rect.position*quitTimeRatio + new Vector3(screenWidth/2f, screenHeight/2f, 0)*(1 - quitTimeRatio);
            }
        }
    }

    void QuitButtonHoverUpdate() {
        /*
         * Set the sizes and colors of the start button to reflect it's current hover value
         */
        int buttonEnum = (int) Buttons.Quit;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = HoverRatio(buttonEnum, true);
        float extraHoverWidth = -hoverReductionAmount*(1 - hoverRatio)*buttonHeight *quitBonusSize*quitWidthRatio/largestRatio;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(quitBonusSize*buttonHeight*quitWidthRatio + extraHoverWidth, buttonHeight);

        /* Add a portion of the height into the label object to prevent the text from overflowing using the current font */
        rect.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        rect.GetChild(0).GetComponent<RectTransform>().sizeDelta = new Vector2(0, -20f * (buttonHeight/100f));

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = (buttonHeight/100f)*quitBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = (buttonHeight/100f)*quitBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[0].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
        outlines[1].effectColor = new Color(0, 0, 0, 0.5f + 0.25f*hoverRatio);
    }

    void QuitButtonReset() {
        /*
         * Reset the position and size of the quit button
         */

        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(0);
    }
    #endregion


    /* ----------- Event/Listener Functions ------------------------------------------------------------- */

    void UpdateSensitivitySlider(float overrideValue) {
        /*
         * Runs everytime the value in the slider is updated. update the current mouse sensitivity.
         * If the given overrideState value is above negative, then we do not need to be in the right state.
         */
         
        /* Only let the slider change the sensitivity value if we are in the Sensitivity state */
        if(state == MenuStates.Sensitivity || overrideValue >= 0) {
            if(overrideValue >= 0) { sensitivity = overrideValue; }
            else { sensitivity = sensitivitySlider.value; }
            sensitivitySlider.value = sensitivity;

            sensitivitySliderValueText.text = "" + Mathf.Round(sensitivity*100)/100f;
            playerController.mouseSens = sensitivity;
            /* Change the color of the text depending on how close it is to the edges */
            float red = 0.8f*Mathf.Clamp((sensitivity - sensitivitySlider.maxValue/2f) / (sensitivitySlider.maxValue/2f), 0, 1);
            float green = 0.8f*Mathf.Clamp((sensitivitySlider.maxValue/2f - sensitivity) / (sensitivitySlider.maxValue/2f), 0, 1);
            sensitivitySliderValueText.color = new Color(1 - green, 1 - red, 1 - green - red, 1);
        }

        /* Return the sensitivity to it's original value */
        else {
            if(sensitivity != sensitivitySlider.value) {
                //////Debug.Log("WARNING: SENSITIVITY HAS CHANGED OUTSIDE THE SENS STATE");
            }
            sensitivitySlider.value = sensitivity;
        }
    }

    Transition GetTransitionFromState(MenuStates givenState) {
        /*
         * Return the Transition that is used for the given state
         */
        Transition transition = null;

        for(int i = 0; i < transitionStates.Length; i++) {
            if(givenState == transitionStates[i].from) {
                transition = transitionStates[i];
                i = transitionStates.Length;
            }
        }

        if(transition == null) {
            Debug.Log("WARNING: GetTransitionFromState returning null");
        }

        return transition;
    }

    public bool PlayerRequestMenuChange() {
        /*
         * The player sent a request to change the menu. This will either 
         * close the menu if it's open or open the menu if it's closed.
         * 
         * Return true if the player will stay in the menu state
         * and return false if the player is now out of the menu state.
         */
        bool inMenu = true;

        /* If the menu is in the startup, skip to the main menu */
        if(state == MenuStates.Startup) {
            //Check if the thing is loaded
            if(terrainController.GetLoadingPercent() >= 1) {
                GetTransitionFromState(state).timeRemaining = 0;
            }
        }

        /* If the menu is empty, Start opening the main menu */
        else if(state == MenuStates.Empty) {
            ChangeState(MenuStates.EmptyToMain);
        }

        /* Wanting to quit the sensitivity menu will bring the game back to the main menu */
        else if(state == MenuStates.Sensitivity) {
            ChangeState(MenuStates.SensToMain);
        }

        /* Wanting to quit the sensitivity menu will bring the game back to the main menu */
        else if(state == MenuStates.Video) {
            ChangeState(MenuStates.VideoToMain);
        }

        /* give the player control if they are exiting the menu */
        else if(state == MenuStates.MainToEmpty || state == MenuStates.MainToIntro) {
            inMenu = false;
        }

        /*
         * When wanting to leave the menu while on the main menu, make sure the game has already begun.
         * If the game did not start yet (determined by isGameStarted), do not close the menu.
         */
        else if(state == MenuStates.Main) {
            /* Only close the game has already started */
            if(isGameStarted) {
                ChangeState(MenuStates.MainToEmpty);
                inMenu = false;
            }
        }

        /* Update the mouse's visibility depending on the menu state */
        if(!inMenu) {
            Cursor.lockState = CursorLockMode.Locked;
        }
        else {
            UpdateCursorState();
        }

        return inMenu;
    }

    void ChangeState(MenuStates newState) {
        /*
         * This is called when changing the states of the menu. This used so that
         * any values that need to be reset upon state change can be adjusted in one function.
         * 
         * When ChangeState is called using the same state that the menu is currently in, 
         * a more unique function may be run. This is mostly to get certain functions
         * to run once a transition state ends, such as ending the game after MainToQuit ends.
         */

        /* Make sure the state being changed to is actually a new state */
        if(state != newState) {

            /*
             * Check the new state
             */
            /* Entering a transition state will start it's tranistion value */
            if(newState == MenuStates.Startup ||
                    newState == MenuStates.EmptyToMain ||
                    newState == MenuStates.MainToEmpty ||
                    newState == MenuStates.MainToIntro ||
                    newState == MenuStates.MainToQuit ||
                    newState == MenuStates.MainToSens ||
                    newState == MenuStates.SensToMain ||
                    newState == MenuStates.MainToVideo ||
                    newState == MenuStates.VideoToMain) {
                ResetRemainingTime(newState);
                /* Entering the MainToVideo or MainToSens will enable the appropriate panel */
                if(newState == MenuStates.MainToVideo) { SetVideoPanelStatus(true); }
                if(newState == MenuStates.MainToSens) { SetSensivityPanelStatus(true); }

                /* Handle the credits panel's state depending on the outside state and menu state */
                if(isOutside) {
                    /* Entering the Video or Sensitivity state will disable the credits panel */
                    if(newState == MenuStates.Video || newState == MenuStates.Sensitivity) {

                    }
                    /* Entering a transition state from the Video or Sens states will re-enable the panel */
                    else if(newState == MenuStates.VideoToMain || newState == MenuStates.SensToMain) {

                    }
                }

                /* Changing the transition state (other than entering the quitting state) will reset the quitting value */
                if(newState != MenuStates.MainToQuit) {
                    quitValueCurrent = 0;
                    bonusQuitSize = new Vector2(0, 0);
                }
            }

            /* Entering Main will reset the quitValueCurrent */
            else if(newState == MenuStates.Main) {
                quitValueCurrent = 0;

                /* If we entered the Main state from the video or sens state, disable the appropriate panel */
                if(state == MenuStates.VideoToMain) { SetVideoPanelStatus(false); }
                if(state == MenuStates.SensToMain) { SetSensivityPanelStatus(false); }
            }

            /* Entering the Sensitivity state will reset the extraCamRotation */
            else if(newState == MenuStates.Sensitivity) {
                extraCamRotation = Vector3.zero;
            }


            /*
             * Check the current state
             */
            /* Leaving the MainToIntro state will change the start button */
            if(state == MenuStates.MainToIntro) {
                buttons[(int) Buttons.Start].GetComponentInChildren<Text>().text = "CONTINUE";
                startWidthRatio = continueTextWidthRatio;
                isGameStarted = true;
            }

            /* Leaving the VideoToMain state will set the video panel to be inactive */
            else if(state == MenuStates.VideoToMain) {

            }

            /* Leaving the SensToMain state will set the sensitivity panel to be inactive */
            else if(state == MenuStates.SensToMain) {

            }

            /* Change the current state */
            state = newState;
        }

        /* More unique events will end up calling for a state change into it's current state */
        else {

            /* Quit the game once MainToQuit finishes it's transition */
            if(state == MenuStates.MainToQuit) {
                QuitGame();
            }
        }
    }

    void ResetRemainingTime(MenuStates givenState) {
        /*
         * Given a menu state, find the transition object of said state and reset
         * it's remainingTime back to it's max.
         */

        for(int i = 0; i < transitionStates.Length; i++) {
            if(givenState == transitionStates[i].from) {
                /* Reset it's current transition time */
                transitionStates[i].timeRemaining = transitionStates[i].timeMax;
            }
        }
    }

    void QuitGame() {
        /*
         * Called when the game needs to quit. 
         */

        //UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }

    void UpdatedResolutionDropdown(Dropdown dropdown) {
        /*
         * When the user selects a new resolution in the video options, this function will run
         */

        /* Update the window's size to reflect the current selected dropdown */
        string res = dropdown.options[dropdown.value].text;
        int width = int.Parse(res.Substring(0, res.IndexOf("x")));
        int height = int.Parse(res.Substring(res.IndexOf("x")+1, res.Length-res.IndexOf("x")-1));
        Screen.SetResolution(width, height, Screen.fullScreen, 0);
    }

    void UpdateLockedFramerate(Dropdown dropdown) {
        /*
         * When the user selects a new locked framerate in the video options, this function will run
         */

        int framerate = -1;
        QualitySettings.vSyncCount = 0;
        if(dropdown.options[dropdown.value].text.Equals("Inf")) {
            framerate = -1;
        }
        else {
            framerate = int.Parse(dropdown.options[dropdown.value].text);
        }

        Application.targetFrameRate = framerate;
    }

    void UpdatedWindowedToggle(Toggle toggle) {
        /*
         * Runs when the user toggles the windowed toggle option
         */

        /* Change to windowed */
        if(toggle.isOn) {
            Screen.fullScreen = false;
        }

        /* Change to fullscreen */
        else {
            Screen.fullScreen = true;
        }
    }

    void LockMouseToggle(Toggle toggle) {
        /*
         * Runs when the user toggles the lock mouse option
         */

        /* Lock the mouse in the window */
        if(toggle.isOn) {
            mouseLock = true;
        }
        /* Unlock the mouse from the window */
        else {
            mouseLock = false;
        }

        UpdateCursorState();
    }

    void RunWithoutFocusToggle(Toggle toggle) {
        /*
         * Runs when the user toggles the run without focus option
         */

        /* Make the game run without focus */
        if(toggle.isOn) {
            Application.runInBackground = true;
        }

        /* Make the game pause when not in focus */
        else {
            Application.runInBackground = false;
        }
    }

    void PlayerClickSound() {
        /*
         * Play the sound of the player clicking a button with the mouse
         */

        playerController.PlayClickSound();
    }

    public void UpdateCursorState() {
        /*
         * Update the mouse lock state
         */

        /* Lock the mouse (confined) during the menu */
        if(mouseLock) {
            Cursor.lockState = CursorLockMode.Confined;
        }

        /* Let the mouse go off the screen */
        else {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void PlayerEnteredOutside() {
        /*
         * This is run once the player has entered the outside state. 
         * Enable the credits panel at this time as it will soon begin to scroll into view.
         */

        isOutside = true;
    }

    public void SetVideoPanelStatus(bool active) {
        /*
         * Set the clickable elements of the video panel to the given boolean 
         */
        int panelEnum = (int) Panels.Video;
        RectTransform videoPanel = panelRects[panelEnum];

        /* Set the status of the toggle options */
        for(int i = 0; i < 3; i++) {
            videoPanel.GetChild(i).GetChild(1).GetComponent<Toggle>().interactable = active;
        }

        /* Set the status of the dropdown options */
        for(int i = 3; i < 5; i++) {
            videoPanel.GetChild(i).GetChild(1).GetComponent<Dropdown>().interactable = active;
        }
    }

    public void SetSensivityPanelStatus(bool active) {
        /*
         * Set the clickable elements of the sensivity panel to the given boolean
         */
        int panelEnum = (int) Panels.Sens;
        RectTransform sensPanel = panelRects[panelEnum];

        /* Set the status of the sens slider */
        sensPanel.GetChild(0).GetComponent<Slider>().interactable = active;
    }
    
    public void LoadPuzzleScene() {
        /*
         * Call this when the game should start loading the puzzleScene
         */
         
        StartCoroutine(PuzzleSceneSceneCoroutine());
    }

    IEnumerator PuzzleSceneSceneCoroutine() {
        /*
         * Use a coroutine to load the puzzleScene
         */
        puzzleSceneCoroutine = SceneManager.LoadSceneAsync("PuzzleScene", LoadSceneMode.Additive);

        yield return null;
    }

    void LoadedPuzzleScene() {
        /*
         * Runs when the puzzle scene has loaded into the game
         */
        Scene menuScene = SceneManager.GetSceneAt(0);
        Scene puzzleScene = SceneManager.GetSceneAt(1);
        GameObject[] objects;
        puzzleSceneLoaded = true;

        /* Disable the camera of the menu scene */
        Camera cam = null;
        objects = menuScene.GetRootGameObjects();
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i].GetComponent<Camera>() != null) {
                cam = objects[i].GetComponent<Camera>();
            }
        }
        cam.gameObject.SetActive(false);
        
        /* Get the terrainController from the puzzleRoom scene */
        TerrainController TC = null;
        objects = puzzleScene.GetRootGameObjects();
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i].GetComponent<TerrainController>() != null) {
                TC = objects[i].GetComponent<TerrainController>();
            }
        }

        /* Get the PlayerController from the puzzleRoom scene */
        CustomPlayerController CPC = null;
        objects = puzzleScene.GetRootGameObjects();
        for(int i = 0; i < objects.Length; i++) {
            if(objects[i].name == "PlayerCharacter") {
                CPC = objects[i].transform.GetChild(0).GetComponent<CustomPlayerController>();
            }
        }

        /* Merge the menu scene into the puzzle scene */
        SceneManager.SetActiveScene(puzzleScene);
        SceneManager.MergeScenes(menuScene, puzzleScene);

        /* Link the player and terrain to the menu */
        playerController = CPC;
        CPC.playerMenu = this;
        terrainController = TC;

        /* Update the player and the menu's sensitivity */
        UpdateSensitivitySlider(5);
    }


    /* ----------- Mouse Enter/Hover Functions ------------------------------------------------------------- */

    void StartButtonClick() {
        /*
         * When the user presses the start key during the Menu state, change into the MenuToIntro state.
         */

        if(IsButtonClickable(Buttons.Start)) {
            /* Play a mouse click sound when selecting the button in the right state */
            if(state == MenuStates.Main) {
                PlayerClickSound();
            }

            /* Start the game by entering the intro state */
            if(!isGameStarted) {
                ChangeState(MenuStates.MainToIntro);
                playerController.StartButtonPressed();
            }

            /* Continue the game by entering the empty state */
            else {
                ChangeState(MenuStates.MainToEmpty);
                playerController.ContinueButtonPressed();
            }
        }
    }

    void StartButtonMouseEnter() {
        /*
         * The mouse entered the start Button's clickable area
         */

        currentHoverState[(int) Buttons.Start] = true;
    }

    void StartButtonMouseExit() {
        /*
         * The mouse entered the start Button's clickable area
         */

        currentHoverState[(int) Buttons.Start] = false;
    }

    void VideoButtonClick() {
        /*
         * Clicking on the Video button will change from the main state to the Video state
         */

        if(IsButtonClickable(Buttons.Video)) {
            /* Play a mouse click sound when selecting the button in the right state */
            if(state == MenuStates.Main || state == MenuStates.Video) {
                PlayerClickSound();
            }

            /* Clicking the button on the main menu will bring it to the video state */
            if(state == MenuStates.Main) {
                ChangeState(MenuStates.MainToVideo);
            }

            /* Clicking it in the video state will bring it to the main menu */
            else if(state == MenuStates.Video) {
                ChangeState(MenuStates.VideoToMain);
            }
        }
    }

    void VideoButtonMouseEnter() {
        /*
         * The mouse entered the Video button's clickable area
         */

        currentHoverState[(int) Buttons.Video] = true;
    }

    void VideoButtonMouseExit() {
        /*
         * The mouse entered the Video Button's clickable area
         */

        currentHoverState[(int) Buttons.Video] = false;
    }

    void SensButtonClick() {
        /*
         * Clicking on the Sensitivity button changes the main menu to enter the sensitivity menu
         */

        if(IsButtonClickable(Buttons.Sens)) {
            /* Play a mouse click sound when selecting the button in the right state */
            if(state == MenuStates.Main || state == MenuStates.Sensitivity) {
                PlayerClickSound();
            }

            /* Clicking the button in the main menu will bring it to the sensitivity menu */
            if(state == MenuStates.Main) {
                ChangeState(MenuStates.MainToSens);
            }

            /* Clicking the button while in the Sensitivity state will go back to the main channel */
            else if(state == MenuStates.Sensitivity) {
                ChangeState(MenuStates.SensToMain);
            }
        }
    }

    void SensButtonMouseEnter() {
        /*
         * The mouse entered the Sens button's clickable area
         */

        currentHoverState[(int) Buttons.Sens] = true;
    }

    void SensButtonMouseExit() {
        /*
         * The mouse entered the Sens Button's clickable area
         */

        currentHoverState[(int) Buttons.Sens] = false;
    }

    void QuitButtonClick() {
        /*
         * When clicking on the quit button in the Main state, Increase currentQuitValue.
         */

        if(IsButtonClickable(Buttons.Quit)) {
            /* Play a mouse click sound when selecting the button in the right state */
            if(state == MenuStates.Main) {
                PlayerClickSound();
            }

            /* Clicking the quit button will increase the current quit value */
            if(quitValueCurrent == 0) {
                quitValueCurrent += quitValueIncrease*3;
            }
            else {
                quitValueCurrent += quitValueIncrease;
            }
        }
    }

    void QuitButtonMouseEnter() {
        /*
         * The mouse entered the quit button's clickable area
         */

        currentHoverState[(int) Buttons.Quit] = true;
    }

    void QuitButtonMouseExit() {
        /*
         * The mouse entered the Quit Button's clickable area
         */

        currentHoverState[(int) Buttons.Quit] = false;
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    float HoverRatio(int buttonEnum, bool sinFunction) {
        /*
         * Given an index to the currentHover array, return the value on a [0, 1] range with maxHovertime
         * being the upper limit. If sinFunction is true, then apply a sin function to smooth out the ratio.
         * The sin ratio ranges from [-PI/2, PI/2] for ratio values of [0, 1].
         */
        float hoverRatio = 0;

        hoverRatio = currentHoverTime[buttonEnum]/maxHoverTime;
        if(sinFunction) {
            hoverRatio = (Mathf.Sin(Mathf.PI*hoverRatio - 0.5f*Mathf.PI)+1)/2f;
        }

        return hoverRatio;
    }

    float TimeRatio(float remainingTime, float maxTime) {
        /*
         * Given a current time which starts at the given max and ends at 0, get the
         * ratio of it's current time on a [0, 1] range.
         */

        return (maxTime - remainingTime) / maxTime;
    }

    float AdjustRatio(float value, float min, float max) {
        /*
         * Given a value that ranges from [0, 1], change it's minimum and max limits,
         * then saturate it so it returns to a [0, 1] range. Example:
         * Entry index/numbering:
         * 0.0 -- 1.0 -- 2.0 -- 3.0 -- 4.0 -- 5.0
         * Normal Value:
         * 0.0 -- 0.2 -- 0.4 -- 0.6 -- 0.8 -- 1.0
         * After AdjustRatio(normal value, 0.2, 0.8):
         * 0.0 -- 0.0 -- 0.3 -- 0.6 -- 1.0 -- 1.0
         */
        float adjustedValue = 0;
        float limitRange = max - min;

        adjustedValue = Mathf.Clamp((value - min) / limitRange, 0, 1);

        return adjustedValue;
    }

    bool IsButtonClickable(Buttons button) {
        /*
         * Return true if the current state can click on the given button
         */
        bool clickable = false;

        /* Start button... */
        if(button == Buttons.Start) {
            /* ...Is only clickable in the main state */
            if(state == MenuStates.Main) {
                clickable = true;
            }
        }

        /* Quit button... */
        else if(button == Buttons.Quit) {
            /* ...Is only clickable in the main state */
            if(state == MenuStates.Main) {
                clickable = true;
            }
        }

        /* Sensitivity button... */
        else if(button == Buttons.Sens) {
            /* ...Is clickable in the main menu and the sensitivity states */
            if(state == MenuStates.Main || state == MenuStates.Sensitivity ||
                    state == MenuStates.SensToMain || state == MenuStates.MainToSens) {
                clickable = true;
            }
        }

        /* Video button... */
        else if(button == Buttons.Video) {
            /* ...Is clickable in the main menu and it's other video states */
            if(state == MenuStates.Main || state == MenuStates.Video ||
                    state == MenuStates.MainToVideo || state == MenuStates.VideoToMain) {
                clickable = true;
            }
        }

        else {
            Debug.Log("WARNING: button not handled in IsButtonClickable");
        }

        return clickable;
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

    bool FinishedLoading() {
        /*
         * Return true if everything has finished loading. This includes 
         * the puzzle scene and the terrain generation.
         */
        bool loaded = false;

        if(puzzleSceneLoaded && terrainGenerated) {
            loaded = true;
        }

        return loaded;
    }
}
