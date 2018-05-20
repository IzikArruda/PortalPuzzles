using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
    private MenuStates state;

    /*
     * Each transitional state and it's Transition object.
     * If a state will transition into a more unique condition (such as MainToQuit),
     * make the state transition into itself as it will be handlede manually in UpdateCurrentState().
     */
    Transition[] transitionStates = {
        new Transition(MenuStates.Startup, MenuStates.Main, 8.0f, 0f),
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
    StateFunction[] coverPanelTransitions;
    StateFunction[] videoPanelTransitions;
    StateFunction[] sensPanelTransitions;

    /* Button height to width ratios. Set manually and is unique for each font + text content. */
    private float startBonusSize = 1.3f;
    private float startWidthRatio = 3;
    private float continueWidthRatio = 4.65f;
    private float videoWidthRatio = 2.9f;
    private float sensWidthRatio = 5.65f;
    private float quitWidthRatio = 2.25f;
    private float resolutionWidthRatio = 7f;
    private float framerateWidthRatio = 3f;
    //Set this to the largest ratio we currently have. This is to make sure each element goes offscreen at the same speed
    private float largestRaio;

    /* Panel sizes. These are set in their setup functions and used in their update functions */
    private float[] panelsWidth;
    private float[] panelsHeight;

    /* Global values used for sizes of UI elements */
    private float minHeight = 20f;
    private float maxHeight = 150f;
    private float avgHeight = 100f;
    private float buttonHeight;
    private float heightRatio;

    /* A link to the player's controller */
    private CustomPlayerController playerController;

    /* The font used for the text of the game */
    public Font usedFont;
    
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
    public RectTransform loadingBox;

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

    /* The main terrainController of the game. Used to access it's sunflare functions */
    public TerrainController terrainController;

    /* Global values with minor/single uses */
    private Vector2 newQuitSize = new Vector2(0, 0);


    /* ----------- Built-in Functions ------------------------------------------------------------- */

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
        /* Cover panel */
        ExecuteElementFunctions(coverPanelTransitions);
        /* Video panel */
        ExecuteElementFunctions(videoPanelTransitions);
        /* Sens panel */
        ExecuteElementFunctions(sensPanelTransitions);
           
        /* Change the current state if needed after all the per-frame update functions are done */
        UpdateCurrentState();
    }


    /* ----------- Initialization Functions ------------------------------------------------------------- */

    public void InitializeMenu(CustomPlayerController controller) {
        /*
         * Sets up the main menu. Requires a link to the playerController to add functionallity to the buttons.
         * Start the game in the IntroToMain transition state.
         */

        /* Make sure the window's sizes are properly set */
        Resize(false);

        /* Populate the StateFunction arrays before anything else */
        StateFunctionInit();

        /* Update the current starting state */
        state = MenuStates.Empty;
        ChangeState(MenuStates.Startup);

        /* Set the largestRaio to reflect the largest button */
        largestRaio = startWidthRatio*startBonusSize;
        largestRaio = Mathf.Max(largestRaio, continueWidthRatio*startBonusSize);
        largestRaio = Mathf.Max(largestRaio, sensWidthRatio);
        largestRaio = Mathf.Max(largestRaio, quitWidthRatio);

        /* Link the global variables of the script */
        playerController = controller;
        canvasRect = canvas.GetComponent<RectTransform>();

        /* Create and populate the array of panels and their sizes used by the UI */
        panelRects = new RectTransform[System.Enum.GetValues(typeof(Panels)).Length];
        panelsWidth = new float[System.Enum.GetValues(typeof(Panels)).Length];
        panelsHeight = new float[System.Enum.GetValues(typeof(Panels)).Length];
        for(int i = 0; i < panelRects.Length; i++) {
            panelRects[i] = CreatePanel().GetComponent<RectTransform>();
        }

        /* Run the initialSetup functions for each panel */
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
            new StateFunction(MenuStates.MainToVideo, UStartButtonMainToVideo),
            new StateFunction(MenuStates.SensToMain, UStartButtonSensToMain),
            new StateFunction(MenuStates.VideoToMain, UStartButtonVideoToMain)
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
            new StateFunction(MenuStates.MainToVideo, UQuitButtonMainToVideo),
            new StateFunction(MenuStates.MainToSens, UQuitButtonMainToSens),
            new StateFunction(MenuStates.SensToMain, UQuitButtonSensToMain),
            new StateFunction(MenuStates.VideoToMain, UQuitButtonVideoToMain)
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
        buttonHeight = Mathf.Clamp(screenHeight*0.15f, minHeight, maxHeight);
        heightRatio = buttonHeight/avgHeight;

        /* Run the reset functions for each UI element be updated from the new size */
        if(updateUI) {
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

        /* Have the loading bar above the cover panel */
        loadingBox.SetAsLastSibling();
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void SetupText(Text text) {
        /*
         * Set the properties of the text object. This is to keep all text objects consistent
         */

        /* Set the font */
        text.font = usedFont;
        text.fontStyle = FontStyle.Normal;

        /* Set the outlines for the text. There will be two outlines used for each text. */
        if(text.gameObject.GetComponent<Outline>() == null) { text.gameObject.AddComponent<Outline>(); text.gameObject.AddComponent<Outline>(); }
        Outline[] outlines = text.gameObject.GetComponents<Outline>();

        /* Set size relative values for the text */
        text.alignment = TextAnchor.MiddleRight;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 1;
        text.resizeTextMaxSize = 10000;

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

    void SetupCoverPanel() {
        /*
         * Setup the cover panel that covers the whole screen
         */
        RectTransform mainPanel = panelRects[(int) Panels.Cover];
        mainPanel.name = "Cover panel";

        /* Set the anchors so it becomes a full stretch layout */
        mainPanel.anchorMin = new Vector2(0, 0);
        mainPanel.anchorMax = new Vector2(1, 1);

        /* Set the sizes to match the screen size */
        mainPanel.anchoredPosition = new Vector3(0, 0, 0);
        mainPanel.sizeDelta = new Vector2(0, 0);
        
        /* Set the color so that the panel is invisible */
        mainPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
    }

    void SetupVideoPanel() {
        /*
         * Setup the video panel which will be used when in the video state
         */
        int panelEnum = (int) Panels.Video;
        RectTransform videoPanel = panelRects[panelEnum];
        videoPanel.name = "Video panel";
        videoPanel.gameObject.SetActive(false);

        /* Set the anchors so it's centered on the right wall */
        videoPanel.anchorMin = new Vector2(1, 0.5f);
        videoPanel.anchorMax = new Vector2(1, 0.5f);

        /* The size of the panel should be 80% the screen width and 100% for height */
        panelsWidth[panelEnum] = 0.8f;
        panelsHeight[panelEnum] = 1f;
        float panelWidth = Screen.width*panelsWidth[panelEnum];
        float panelHeight = Screen.height*panelsHeight[panelEnum];
        float optionPanelHeight = buttonHeight/2f;
        videoPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        /* Set the color so that the panel is invisible */
        videoPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);

        /*
         * Add the components that make up the panel. The order of the options controls their positions.
         */
        string[] videoButtonTexts = { "Resolution", "Windowed", "Lock framerate", "Lock mouse", "Run without focus" };
        GameObject[] videoOptionPanels = new GameObject[videoButtonTexts.Length];
        /* Create each panel that is used as an option in the video panel */
        for(int i = 0; i < videoOptionPanels.Length; i++) {
            /* Create the option and add it's components */
            videoOptionPanels[i] = new GameObject("Option panel " + videoButtonTexts[i], typeof(RectTransform));
            RectTransform panelRect = videoOptionPanels[i].GetComponent<RectTransform>();
            panelRect.SetParent(videoPanel);
            videoOptionPanels[i].AddComponent<Image>().color = new Color(0, 1, 0, 0.3f);//Add an image so we can see the placement
            /* The panel will fill the video panel's width */
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(1, 0.5f);
            /* The position of the panel depends on it's index */
            panelRect.anchoredPosition = new Vector3(0, (panelHeight/2f-buttonHeight/2f) -i*optionPanelHeight*3/2f, 0);
            panelRect.sizeDelta = new Vector2(0, optionPanelHeight);

            /* Add a text element that will occupy the left side of the panel */
            GameObject textObject = new GameObject("Option text " + videoButtonTexts[i], typeof(RectTransform));
            Text text = textObject.AddComponent<Text>();
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(panelRect);
            /* Set the anchors so that the text stays on the left side of the panel */
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(0.5f, 1);
            textRect.anchoredPosition = new Vector3(0, 0, 0);
            textRect.sizeDelta = new Vector2(0, 0);
            /* Set the text properties */
            text.gameObject.AddComponent<Outline>().effectDistance = heightRatio*new Vector2(1, 1);
            text.text = videoButtonTexts[i];
            text.font = usedFont;
            text.alignment = TextAnchor.MiddleRight;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 1;
            text.resizeTextMaxSize = 10000;
        }

        /* 
         * Add components to the right side of each newly added panels in the video panel 
         */
        /* Panel 1 controls the screen's resolution using a dropdown menu of the monitor's usable resolutions.
         * Duplicate the dropdown object currently referenced by this script */
        GameObject dropdownObject = Instantiate(videoOptionsDropwdownReference.gameObject);
        dropdownObject.name = "Resolution Dropdown";
        dropdownObject.SetActive(true);
        Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
        RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
        RectTransform dropdownPanel = videoOptionPanels[0].GetComponent<RectTransform>();
        dropdownRect.SetParent(dropdownPanel);
        /* Populate the dropdown with the resolutions */
        Resolution[] res = Screen.resolutions;
        List<string> newOptions = new List<string>();
        for(int i = 0; i < res.Length; i++) {
            newOptions.Add(res[i].width + "x" + res[i].height);
        }
        dropdown.ClearOptions();
        dropdown.AddOptions(newOptions);
        dropdown.RefreshShownValue();
        /* Link a function to it's onValueChanged */
        dropdown.onValueChanged.AddListener(delegate { UpdatedResolutionDropdown(dropdown); });
        /* Position the object to be placed on the right side of it's panel */
        dropdownRect.anchorMin = new Vector2(0.5f, 0);
        dropdownRect.anchorMax = new Vector2(0.5f, 1);
        dropdownRect.anchoredPosition = new Vector3((optionPanelHeight/2f)*resolutionWidthRatio/2f, 0, 0);
        dropdownRect.sizeDelta = new Vector2((optionPanelHeight/2f)*resolutionWidthRatio, 0);
        /* Set the currently selected dropwdown item menu to reflect the current resolution */
        string currentRes = Screen.currentResolution.width + "x" + Screen.currentResolution.height;
        dropdown.value = res.Length-1;
        for(int i = 0; i < res.Length; i++) {
            if(currentRes.Equals(res[i].width + "x" + res[i].height)) {
                dropdown.value = i;
                i = res.Length;
            }
        }

        /* Panel 2 controls whether the game will run in windowed mode using a toggle */
        GameObject windowToggleObject = Instantiate(videoOptionsToggleReference.gameObject);
        windowToggleObject.name = "Windowed toggle";
        windowToggleObject.SetActive(true);
        Toggle windowedToggle = windowToggleObject.GetComponent<Toggle>();
        RectTransform windowedToggleRect = windowToggleObject.GetComponent<RectTransform>();
        RectTransform windowedTogglePanel = videoOptionPanels[1].GetComponent<RectTransform>();
        windowedToggleRect.SetParent(windowedTogglePanel);
        /* Link a function to it's onValueChange */
        windowedToggle.onValueChanged.AddListener(delegate { UpdatedWindowedToggle(windowedToggle); });
        /* Position the object to be placed on the right side of it's panel */
        windowedToggleRect.anchorMin = new Vector2(0.5f, 0);
        windowedToggleRect.anchorMax = new Vector2(1, 1);
        windowedToggleRect.anchoredPosition = new Vector3(0, 0, 0);
        windowedToggleRect.sizeDelta = new Vector2(optionPanelHeight, 0);
        /* Resize the images of the button to reflect the panel's height */
        //Resize the background object, ie the checkbox
        RectTransform backgroundRect = windowedToggle.transform.GetChild(0).GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector3(buttonHeight/2f, 0, 0);
        backgroundRect.sizeDelta = new Vector2(buttonHeight/2f, 0);
        //Resize the tick indicator, ie the checkmark
        RectTransform checkRect = backgroundRect.GetChild(0).GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(buttonHeight/2f, buttonHeight/2f);
        /* Set the current toggle setting to reflect the current window state */
        windowedToggle.isOn = !Screen.fullScreen;

        /* Panel 3 controls the framerate lock/target framerate */
        GameObject framerateDropdownObject = Instantiate(videoOptionsDropwdownReference.gameObject);
        framerateDropdownObject.name = "Framerate Dropdown";
        framerateDropdownObject.SetActive(true);
        Dropdown framerateDropdown = framerateDropdownObject.GetComponent<Dropdown>();
        RectTransform framerateDropdownRect = framerateDropdownObject.GetComponent<RectTransform>();
        RectTransform framerateDropdownPanel = videoOptionPanels[2].GetComponent<RectTransform>();
        framerateDropdownRect.SetParent(framerateDropdownPanel);
        /* Link a function to it's onValueChange */
        framerateDropdown.onValueChanged.AddListener(delegate { UpdateLockedFramerate(framerateDropdown); });
        /* Populate the dropdown with potential locked framerates */
        List<string> newFramerates = new List<string>() { "Inf", "10", "30", "60", "69", "144", "420"};
        framerateDropdown.ClearOptions();
        framerateDropdown.AddOptions(newFramerates);
        framerateDropdown.RefreshShownValue();
        /* Position the object to be placed on the right side of it's panel */
        framerateDropdownRect.anchorMin = new Vector2(0.5f, 0);
        framerateDropdownRect.anchorMax = new Vector2(0.5f, 1);
        framerateDropdownRect.anchoredPosition = new Vector3((optionPanelHeight/2f)*framerateWidthRatio/2f, 0, 0);
        framerateDropdownRect.sizeDelta = new Vector2((optionPanelHeight/2f)*framerateWidthRatio, 0);
        /* Start the framerate as unlocked */
        framerateDropdown.value = 0;
        UpdateLockedFramerate(framerateDropdown);

        /* Panel 4 controls whether the mouse should be locked within the window */
        GameObject mouseLockObject = Instantiate(videoOptionsToggleReference.gameObject);
        mouseLockObject.name = "Mouse lock";
        mouseLockObject.SetActive(true);
        Toggle mouseToggle = mouseLockObject.GetComponent<Toggle>();
        RectTransform mouseToggleRect = mouseLockObject.GetComponent<RectTransform>();
        RectTransform mouseTogglePanel = videoOptionPanels[3].GetComponent<RectTransform>();
        mouseToggleRect.SetParent(mouseTogglePanel);
        /* Link a function to it's onValueChange */
        mouseToggle.onValueChanged.AddListener(delegate { LockMouseToggle(mouseToggle); });
        /* Position the object to be placed on the right side of it's panel */
        mouseToggleRect.anchorMin = new Vector2(0.5f, 0);
        mouseToggleRect.anchorMax = new Vector2(1, 1);
        mouseToggleRect.anchoredPosition = new Vector3(0, 0, 0);
        mouseToggleRect.sizeDelta = new Vector2(optionPanelHeight, 0);
        //Resize the background object, ie the checkbox
        backgroundRect = mouseToggle.transform.GetChild(0).GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector3(buttonHeight/2f, 0, 0);
        backgroundRect.sizeDelta = new Vector2(buttonHeight/2f, 0);
        //Resize the tick indicator, ie the checkmark
        checkRect = backgroundRect.GetChild(0).GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(buttonHeight/2f, buttonHeight/2f);
        /* Set the toggles starting state to reflect the current mouse state */
        mouseToggle.isOn = Cursor.lockState == CursorLockMode.Confined;

        /* Panel 5 controls whether the game will still run when out of focus */
        GameObject focusObject = Instantiate(videoOptionsToggleReference.gameObject);
        focusObject.name = "Pause when not in focus";
        focusObject.SetActive(true);
        Toggle focusToggle = focusObject.GetComponent<Toggle>();
        RectTransform focusToggleRect = focusObject.GetComponent<RectTransform>();
        RectTransform focusTogglePanel = videoOptionPanels[4].GetComponent<RectTransform>();
        focusToggleRect.SetParent(focusTogglePanel);
        /* Link a function to it's onValueChange */
        focusToggle.onValueChanged.AddListener(delegate { RunWithoutFocusToggle(focusToggle); });
        /* Position the object to be placed on the right side of it's panel */
        focusToggleRect.anchorMin = new Vector2(0.5f, 0);
        focusToggleRect.anchorMax = new Vector2(1, 1);
        focusToggleRect.anchoredPosition = new Vector3(0, 0, 0);
        focusToggleRect.sizeDelta = new Vector2(optionPanelHeight, 0);
        //Resize the background object, ie the checkbox
        backgroundRect = focusToggle.transform.GetChild(0).GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector3(buttonHeight/2f, 0, 0);
        backgroundRect.sizeDelta = new Vector2(buttonHeight/2f, 0);
        //Resize the tick indicator, ie the checkmark
        checkRect = backgroundRect.GetChild(0).GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(buttonHeight/2f, buttonHeight/2f);
        /* Set the toggle's state to reflect the game's current running without focus status */
        focusToggle.isOn = Application.runInBackground;

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
        sensPanel.gameObject.SetActive(false);

        /* Set the anchors so it's position based in the bottom right corner */
        sensPanel.anchorMin = new Vector2(1, 0);
        sensPanel.anchorMax = new Vector2(1, 0);

        /* The size of the panel should be 80% the screen width and 30% for height */
        panelsWidth[panelEnum] = 0.8f;
        panelsHeight[panelEnum] = 0.3f;
        float panelWidth = Screen.width*panelsWidth[panelEnum];
        float panelHeight = Screen.height*panelsHeight[panelEnum];
        sensPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        /* Set the color so that the panel is invisible */
        sensPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.1f);

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
        sliderRect.anchoredPosition = new Vector3(0, 0, 0);
        sliderRect.sizeDelta = new Vector2(0, 20);
        /* Assign a function to when the slider updates */
        sensitivitySlider.maxValue = sensMax;
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.value = playerController.mouseSensMod;
        sensitivity = sensitivitySlider.value;
        playerController.mouseSens = sensitivity;
        sensitivitySlider.onValueChanged.AddListener(delegate { UpdateSensitivitySlider(); });
        
        /* Add text bellow the slider giving instructions */
        GameObject sliderText = new GameObject("Slider text", typeof(RectTransform));
        Text text = sliderText.AddComponent<Text>();
        RectTransform rectTex = sliderText.GetComponent<RectTransform>();
        sliderText.transform.SetParent(sensPanel);
        sliderText.SetActive(true);
        /* Set the text properties */
        text.gameObject.AddComponent<Outline>().effectDistance = heightRatio*new Vector2(1, 1);
        text.text = "Hold right-click to test the mouse sensitivity";
        text.font = usedFont;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 1;
        text.resizeTextMaxSize = 10000;
        text.raycastTarget = false;
        /* Set the sizes of the text */
        rectTex.anchorMin = new Vector2(0, 0.25f);
        rectTex.anchorMax = new Vector2(1, 0.25f);
        rectTex.anchoredPosition = new Vector3(0, 0, 0);
        rectTex.sizeDelta = new Vector2(0, panelHeight/2f);

        /* Add text above the slider giving the sensitivity */
        GameObject sliderValue = new GameObject("Slider value", typeof(RectTransform));
        sensitivitySliderValueText = sliderValue.AddComponent<Text>();
        RectTransform valueRect = sliderValue.GetComponent<RectTransform>();
        sliderValue.transform.SetParent(sensPanel);
        sliderValue.SetActive(true);
        /* Set the text properties */
        sensitivitySliderValueText.gameObject.AddComponent<Outline>().effectDistance = heightRatio*new Vector2(1, 1);
        sensitivitySliderValueText.text = ""+sensitivity;
        sensitivitySliderValueText.font = usedFont;
        sensitivitySliderValueText.alignment = TextAnchor.MiddleCenter;
        sensitivitySliderValueText.resizeTextForBestFit = true;
        sensitivitySliderValueText.resizeTextMinSize = 1;
        sensitivitySliderValueText.resizeTextMaxSize = 10000;
        sensitivitySliderValueText.raycastTarget = false;
        /* Set the sizes of the value */
        valueRect.anchorMin = new Vector2(0, 0.75f);
        valueRect.anchorMax = new Vector2(1, 0.75f);
        valueRect.anchoredPosition = new Vector3(0, 0, 0);
        valueRect.sizeDelta = new Vector2(0, panelHeight/2f);
    }

    void SetupStartButton() {
        /*
         * Set the variables of the button that makes it the "Start" button
         */
        Button button = buttons[(int) Buttons.Start];
        button.name = "Start button";

        /* Setup the text of the button */
        SetupText(button.GetComponentInChildren<Text>());
        button.GetComponentInChildren<Text>().text = "START";

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
        SetupText(button.GetComponentInChildren<Text>());
        button.GetComponentInChildren<Text>().text = "VIDEO";

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
        SetupText(button.GetComponentInChildren<Text>());
        button.GetComponentInChildren<Text>().text = "SENSITIVITY";

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
        SetupText(button.GetComponentInChildren<Text>());
        button.GetComponentInChildren<Text>().text = "QUIT";

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
            /* In the startup, prevent the timine from being reduced if the terrain is not 100% loaded */
            case MenuStates.Startup:
                if(terrainController.GetLoadingPercent() < 1) {
                    //Reset the remainingTime of the current state
                    ResetRemainingTime(state);
                }
                break;
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
         * to test the new sensitivty of the mouse
         */
         
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
        else{
            recoveryTime += Time.deltaTime;
            if(recoveryTime > 1) { recoveryTime = 1; }
            extraCamRotation = Mathf.Cos(recoveryTime*Mathf.PI/2f)*savedRotation;
        }

        /* Set the camera's rotation */
        playerController.extraCamRot = extraCamRotation;
    }

    void UpdateSunFlare() {
        /*
         * Update the intensity mod of the sunflare depending on the current state of the menu. 
         * This is used in the intro of the menu opening to transition from the white startup to the menu.
         */
        float bonus = 1;
        if(state == MenuStates.Startup) {
            Transition transition = GetTransitionFromState(state);
            /* Start fading the sun flare amount 5% in, ending 75% into the startup */
            float transitionBonus = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.05f, 0.75f);
            /* Use a cosine function to smooth out the flare changes */
            transitionBonus = (Mathf.Cos(transitionBonus*Mathf.PI)+1)/2f;

            /* Make the fade out use a sin function */
            bonus += transitionBonus*20;
        }
        terrainController.UpdateSunFlareMod(bonus);
    }

    void UpdateLoadingBar() {
        /*
         * Update the sizes and colors of the loading bar to reflect the current
         */
        float transitionFade;
        Transition transition = GetTransitionFromState(state);
        loadingBox.gameObject.SetActive(true);
        RectTransform loadingBar = loadingBox.GetChild(0).gameObject.GetComponent<RectTransform>();

        /* Update the colors of the bar/box relative to the timing of the current state */
        transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.05f, 0.1f);
        loadingBar.GetComponent<Image>().color = new Color(0, 0, 0, 1 - transitionFade*2);
        if(transitionFade > 0) {
            loadingBox.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        }else {
            loadingBox.GetComponent<Image>().color = new Color(0, 0, 0, 1 - transitionFade*2);
        }

        /* Update the size of the loading box */
        loadingBox.anchorMin = new Vector2(0.2f, 0.1f);
        loadingBox.anchorMax = new Vector2(0.8f, 0.2f);
        loadingBox.sizeDelta = new Vector2(0, 0);
        loadingBox.anchoredPosition = new Vector2(0, 0);

        /* Update the size of the loading bar relative to the loading completion rate */
        float loadingRatio = terrainController.GetLoadingPercent();
        loadingBar.anchorMax = new Vector2(loadingRatio, 1);
        loadingBar.sizeDelta = new Vector2(0, 0);
        loadingBar.anchoredPosition = new Vector3(0, 0, 0);
    }

    /* ----------- UI Element Update Functions ------------------------------------------------------------- */

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
        if(camY / 360 > 0) { camY -= ((int)camY/360)*360; }
        if(camY / 180 > 0) { camY -= ((int) camY/180)*360; }
        extraCamRotation = new Vector3(camX, camY, 0);

        /* Reduce the angle as we are leaving the startup */
        extraCamRotation *= ratio;

        /* Apply the angle to the camera */
        playerController.extraCamRot = extraCamRotation;

        /* Update the loading bar used in the intro */
        UpdateLoadingBar();
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
        mainPanel.anchoredPosition = new Vector3(0, 0, 0);
        mainPanel.sizeDelta = new Vector2(0, 0);
    }
    #endregion

    #region Sens Panel Updates
    void USensPanelSensitivity() {
        /*
         * While in the Sensitivity state, make sure the panel occupies the bottom edge of the screen
         */
        int panelEnum = (int) Panels.Sens;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();

        /* Place the panel so it can be seen */
        SensPanelPositionUpdate(1);
    }

    void USensPanelMainToSens() {
        /*
         * Animate the panel comming into view
         */
        int panelEnum = (int) Panels.Sens;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //Get the transition ratio for the current state
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place the panel so it can be seen */
        SensPanelPositionUpdate(transitionFade);
    }

    void USensPanelSensToMain() {
        /*
         * Animate the panel leaving the view
         */
        int panelEnum = (int) Panels.Sens;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //Use a cos function to smooth out the animation
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
        rect.anchoredPosition = new Vector3(panelWidth, panelHeight, 0);
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

        /* The size of the panel should be 80% the screen width and 30% for height */
        panelsWidth[panelEnum] = 0.8f;
        panelsHeight[panelEnum] = 0.3f;
        float panelWidth = Screen.width*panelsWidth[panelEnum];
        float panelHeight = Screen.height*panelsHeight[panelEnum];
        sensPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        /* Adjust the size of the text above and bellow the slider */
        RectTransform bellowText = sensPanel.GetChild(1).GetComponent<RectTransform>();
        RectTransform aboveText = sensPanel.GetChild(2).GetComponent<RectTransform>();
        /* Set the sizes of the sens value text above */
        aboveText.anchorMin = new Vector2(0, 0.75f);
        aboveText.anchorMax = new Vector2(1, 0.75f);
        aboveText.anchoredPosition = new Vector3(0, 0, 0);
        aboveText.sizeDelta = new Vector2(0, panelHeight/2f);
        /* Set the sizes of the description text bellow */
        bellowText.anchorMin = new Vector2(0, 0.25f);
        bellowText.anchorMax = new Vector2(1, 0.25f);
        bellowText.anchoredPosition = new Vector3(0, 0, 0);
        bellowText.sizeDelta = new Vector2(0, panelHeight/2f);
        SensPanelPositionUpdate(0);
    }
    #endregion
    
    #region Video Panel Updates
    void UVideoPanelVideo() {
        /*
         * While in the Video state, make sure the panel occupies the right side of the screen
         */
        int panelEnum = (int) Panels.Video;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();

        /* Place the panel so it can be seen */
        VideoPanelPositionUpdate(1);
    }

    void UVideoPanelMainToVideo() {
        /*
         * Animate the panel comming into view
         */
        int panelEnum = (int) Panels.Video;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //Get the transition ratio for the current state
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place the panel so it can be seen */
        VideoPanelPositionUpdate(transitionFade);
    }

    void UVideoPanelVideoToMain() {
        /*
         * Animate the panel leaving the view
         */
        int panelEnum = (int) Panels.Video;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //Use a cos function to smooth out the animation
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
        rect.anchoredPosition = new Vector3(panelWidth, panelHeight, 0);
    }

    void VideoPanelReset() {
        /*
         * Reset the video panel's position and size
         */
        int panelEnum = (int) Panels.Video;
        RectTransform videoPanel = panelRects[panelEnum];

        /* Set the anchors so it's centered on the right wall */
        videoPanel.anchorMin = new Vector2(1, 0.5f);
        videoPanel.anchorMax = new Vector2(1, 0.5f);

        /* The size of the panel should be 80% the screen width and 100% for height */
        panelsWidth[panelEnum] = 0.8f;
        panelsHeight[panelEnum] = 1f;
        float panelWidth = Screen.width*panelsWidth[panelEnum];
        float panelHeight = Screen.height*panelsHeight[panelEnum];
        videoPanel.sizeDelta = new Vector2(panelWidth, panelHeight);

        /* Resize the option panels to fit the screen */
        RectTransform panelRect;
        float optionPanelHeight = buttonHeight/2f;
        for(int i = 0; i < videoPanel.childCount; i++) {
            panelRect = videoPanel.GetChild(i).GetComponent<RectTransform>();
            if(panelRect != null) {
                panelRect.anchoredPosition = new Vector3(0, (panelHeight/2f-buttonHeight/2f) -i*optionPanelHeight*3/2f, 0);
                panelRect.sizeDelta = new Vector2(0, optionPanelHeight);
            }
        }

        /* Update the windowed toggle box */
        RectTransform windowedToggle = videoPanel.GetChild(1).GetChild(1).GetComponent<RectTransform>();
        windowedToggle.anchoredPosition = new Vector3(0, 0, 0);
        windowedToggle.sizeDelta = new Vector2(optionPanelHeight, 0);
        //Resize the background object, ie the checkbox
        RectTransform backgroundRect = windowedToggle.transform.GetChild(0).GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector3(buttonHeight/2f, 0, 0);
        backgroundRect.sizeDelta = new Vector2(buttonHeight/2f, 0);
        //Resize the tick indicator, ie the checkmark
        RectTransform checkRect = backgroundRect.GetChild(0).GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(buttonHeight/2f, buttonHeight/2f);

        /* Update the mouse lock toggle box */
        RectTransform mouseToggle = videoPanel.GetChild(3).GetChild(1).GetComponent<RectTransform>();
        mouseToggle.anchoredPosition = new Vector3(0, 0, 0);
        mouseToggle.sizeDelta = new Vector2(optionPanelHeight, 0);
        //Resize the background object, ie the checkbox
        backgroundRect = mouseToggle.transform.GetChild(0).GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector3(buttonHeight/2f, 0, 0);
        backgroundRect.sizeDelta = new Vector2(buttonHeight/2f, 0);
        //Resize the tick indicator, ie the checkmark
        checkRect = backgroundRect.GetChild(0).GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(buttonHeight/2f, buttonHeight/2f);

        /* Update the mouse focus toggle box */
        RectTransform focusToggle = videoPanel.GetChild(4).GetChild(1).GetComponent<RectTransform>();
        focusToggle.anchoredPosition = new Vector3(0, 0, 0);
        focusToggle.sizeDelta = new Vector2(optionPanelHeight, 0);
        //Resize the background object, ie the checkbox
        backgroundRect = focusToggle.transform.GetChild(0).GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector3(buttonHeight/2f, 0, 0);
        backgroundRect.sizeDelta = new Vector2(buttonHeight/2f, 0);
        //Resize the tick indicator, ie the checkmark
        checkRect = backgroundRect.GetChild(0).GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.5f, 0.5f);
        checkRect.anchorMax = new Vector2(0.5f, 0.5f);
        checkRect.sizeDelta = new Vector2(buttonHeight/2f, buttonHeight/2f);

        /* Update the Resolution dropdown size */
        RectTransform resolutionDropdownRect = videoPanel.GetChild(0).GetChild(1).GetComponent<RectTransform>();
        resolutionDropdownRect.anchoredPosition = new Vector3((optionPanelHeight/2f)*resolutionWidthRatio/2f, 0, 0);
        resolutionDropdownRect.sizeDelta = new Vector2((optionPanelHeight/2f)*resolutionWidthRatio, 0);
        /////
        //This gets the Dropdown object
        Debug.Log(resolutionDropdownRect.name);


        /////

        /* Update the framerate limit dropdown size */
        RectTransform framerateDropdownRect = videoPanel.GetChild(2).GetChild(1).GetComponent<RectTransform>();
        framerateDropdownRect.anchoredPosition = new Vector3((optionPanelHeight/2f)*framerateWidthRatio/2f, 0, 0);
        framerateDropdownRect.sizeDelta = new Vector2((optionPanelHeight/2f)*framerateWidthRatio, 0);



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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.775f, 1f);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRaio);
        
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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*(startWidthRatio*startBonusSize)/largestRaio);

        /* Move the button from the main position to off-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1 - transitionFade);
    }

    void UStartButtonMainToVideo() {
        /*
         * Rotate the button off the left side
         */
        int buttonEnum = (int) Buttons.Start;
        RectTransform rect = buttonRects[buttonEnum];
        //Use a basic transition value that is the same speed for each button
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place teh button on-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(0.5f);

        /* Set it's pivot point onto the edge of the screen/button */
        rect.pivot = new Vector3(0, 0.5f);

        /* Set it's rotation to reflect the current transition value */
        rect.localEulerAngles = new Vector3(0, 90f*transitionFade, 0);
    }

    void UStartButtonVideoToMain() {
        /*
         * Rotate the button as we leave the video state
         */
        int buttonEnum = (int) Buttons.Start;
        RectTransform rect = buttonRects[buttonEnum];
        //Use a basic transition value that is the same speed for each button
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place teh button on-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(0.5f);

        /* Set it's pivot point onto the edge of the screen/button */
        rect.pivot = new Vector3(0, 0.5f);

        /* Set it's rotation to reflect the current transition value */
        rect.localEulerAngles = new Vector3(0, 90f-90f*transitionFade, 0);

        /* Reset the pivot point once we are done leaving the video state */
        if(transition.timeRemaining == 0) {
            rect.pivot = new Vector3(0.5f, 0.5f);
            StartButtonPositionUpdate(1);
        }
    }

    void UStartButtonMainToSens() {
        /*
         * Slide the button off the left side
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRaio);

        /* Move the button from the main position to off-screen */
        StartButtonHoverUpdate();
        StartButtonPositionUpdate(1 - transitionFade);
    }

    void UStartButtonSensToMain() {
        /*
         * Slide the button back into view
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (startWidthRatio*startBonusSize)/largestRaio);

        /* Move the button from the main position to off-screen */
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

        rect.position = new Vector3(-rect.sizeDelta.x/2f + rect.sizeDelta.x*sideRatio, canvasRect.position.y + buttonHeight/2f, 0);
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
        float extraHoverWidth = hoverRatio*buttonHeight*startBonusSize;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(startBonusSize*buttonHeight*startWidthRatio + extraHoverWidth, startBonusSize*buttonHeight);

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = heightRatio*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = heightRatio*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.8f, 1f);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*videoWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, videoWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, (videoWidthRatio*startBonusSize)/largestRaio);

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

        float relativeHeight = aboveButton.position.y - aboveButton.sizeDelta.y/2f - buttonHeight/2f;
        rect.position = new Vector3(-rect.sizeDelta.x/2f + rect.sizeDelta.x*sideRatio, relativeHeight, 0);
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
        float extraHoverWidth = hoverRatio*buttonHeight;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(buttonHeight*videoWidthRatio + extraHoverWidth, buttonHeight);

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = heightRatio*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = heightRatio*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.825f, 1f);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, sensWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, sensWidthRatio/largestRaio);

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
         * Rotate the sens button as we enter the video state
         */
        int buttonEnum = (int) Buttons.Sens;
        RectTransform rect = buttonRects[buttonEnum];
        //Use a basic transition value that is the same speed for each button
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Move the button from the main to off-screen position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(0.5f);

        /* Set it's pivot point onto the edge of the screen/button */
        rect.pivot = new Vector3(0, 0.5f);

        /* Set it's rotation to reflect the current transition value */
        rect.localEulerAngles = new Vector3(0, 90f*transitionFade, 0);
    }

    void USensButtonVideoToMain() {
        /*
         * Rotate the sens button as we leave the video state
         */
        int buttonEnum = (int) Buttons.Sens;
        RectTransform rect = buttonRects[buttonEnum];
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Move the button from the main to off-screen position */
        SensButtonHoverUpdate();
        SensButtonPositionUpdate(0.5f);

        /* Set it's pivot point onto the edge of the screen/button */
        rect.pivot = new Vector3(0, 0.5f);

        /* Set it's rotation to reflect the current transition value */
        rect.localEulerAngles = new Vector3(0, 90f-90f*transitionFade, 0);

        /* Reset the pivot point once we are done leaving the video state */
        if(transition.timeRemaining == 0) {
            rect.pivot = new Vector3(0.5f, 0.5f);
            SensButtonPositionUpdate(1);
        }
    }

    void USensButtonMainToIntro() {
        /*
         * Animate the sensitivity button when entering the intro. The button slides out to the left
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, sensWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*sensWidthRatio/largestRaio);

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

        float relativeHeight = aboveButton.position.y - aboveButton.sizeDelta.y/2f - buttonHeight/2f;
        rect.position = new Vector3(-rect.sizeDelta.x/2f + rect.sizeDelta.x*sideRatio, relativeHeight, 0);
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
        float extraHoverWidth = hoverRatio*buttonHeight;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(buttonHeight*sensWidthRatio + extraHoverWidth, buttonHeight);

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = heightRatio*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = heightRatio*startBonusSize*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.85f, 1f);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, quitWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, quitWidthRatio/largestRaio);

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

    void UQuitButtonMainToVideo() {
        /*
         * Rotate the quit button as we enter the video state
         */
        int buttonEnum = (int) Buttons.Quit;
        RectTransform rect = buttonRects[buttonEnum];
        //Use a basic transition value that is the same speed for each button
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Move the button from it's main position to off-screen */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(0.5f);

        /* Set it's pivot point onto the edge of the screen/button */
        rect.pivot = new Vector3(0, 0.5f);

        /* Set it's rotation to reflect the current transition value */
        rect.localEulerAngles = new Vector3(0, 90f*transitionFade, 0);
    }

    void UQuitButtonVideoToMain() {
        /*
         * Rotate the quit button as we leave the video state
         */
        int buttonEnum = (int) Buttons.Quit;
        RectTransform rect = buttonRects[buttonEnum];
        //Use a basic transition value that is the same speed for each button
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Move the button from it's main position to off-screen */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(0.5f);

        /* Set it's pivot point onto the edge of the screen/button */
        rect.pivot = new Vector3(0, 0.5f);

        /* Set it's rotation to reflect the current transition value */
        rect.localEulerAngles = new Vector3(0, 90f-90f*transitionFade, 0);

        /* Reset the pivot point once we are done leaving the video state */
        if(transition.timeRemaining == 0) {
            rect.pivot = new Vector3(0.5f, 0.5f);
            QuitButtonPositionUpdate(1);
        }
    }

    void UQuitButtonMainToIntro() {
        /*
         * Animate the quit button when entering the intro. The quit button slides out to the left
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, quitWidthRatio/largestRaio);

        /* Move the button from the main to off-screen position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1 - transitionFade);
    }
    
    void UQuitButtonMainToQuit() {
        /*
         * Animate the button slidding off the left side of the screen as the game quits
         * 
         * Animate the button as it approaches the center of the screen
         */
        //The transition starts fading at the start and ends 50% through
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*quitWidthRatio/largestRaio);

        /* Move the button from the main to off-screen position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1);

        //Reposition the button to be near the center of the screen
    }

    void UQuitButtonMainToSens() {
        /*
         * Update the quit button as it slides out of view
         */
        //Adjust the transition to reflect the button's size
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, quitWidthRatio/largestRaio);

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
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, quitWidthRatio/largestRaio);

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
        
        float relativeHeight = aboveButton.position.y - aboveButton.sizeDelta.y/2f - buttonHeight/2f;
        rect.position = new Vector3(-rect.sizeDelta.x/2f + rect.sizeDelta.x*sideRatio, relativeHeight, 0);

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
                baseExtraHeight = buttonHeight*0.075f;
                baseExtraWidth = baseExtraHeight*quitWidthRatio;
                ratioDist += 10*(1 - quitTimeRatio);
            }
            //Set the color of the quit button
            quitText.color = new Color(1, 1 - ratioColor, 1 - ratioColor, 1);
            //Reposition the text position
            quitRect.anchoredPosition = ratioPos*10*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
            //Resize the text's size
            if(ratioSize > 0.6f) {
                //Increase the size
                quitRect.sizeDelta = quitRect.sizeDelta - ratioSize*5*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1)) + new Vector2(baseExtraHeight, baseExtraWidth);
                newQuitSize = quitRect.sizeDelta;
            }
            else {
                //Start resetting the size to reach back to 0
                quitRect.sizeDelta = (quitValueCurrent/quitValueMax*0.6f)*newQuitSize;
                newQuitSize = quitRect.sizeDelta;
            }
            //reposition the outline's distance
            Outline[] outlines = quitRect.gameObject.GetComponents<Outline>();
            outlines[0].effectDistance += ratioDist*4*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
            outlines[1].effectDistance += ratioDist*4*new Vector2(Random.Range(-1, 1), Random.Range(-1, 1));
            //Recolor the outline's color
            outlines[0].effectColor = new Color(ratioOutlineCol*0.5f, 0, 0, outlines[0].effectColor.a);
            outlines[1].effectColor = new Color(ratioOutlineCol*0.5f, 0, 0, outlines[1].effectColor.a);

            /* Depending on how long along the mainToQuit transition we are in, reposition the text */
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
        float extraHoverWidth = hoverRatio*buttonHeight;

        /* Set the color of the button to reflect the current hover value */
        float hoverColor = 1f - 0.10f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Set the button's size to reflect the current hover value */
        rect.sizeDelta = new Vector2(buttonHeight*quitWidthRatio + extraHoverWidth, buttonHeight);

        /* Set the outline's distance relative to the hover value */
        outlines[0].effectDistance = heightRatio*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
        outlines[1].effectDistance = heightRatio*new Vector2(0.25f + 1f*hoverRatio, 0.25f + 1f*hoverRatio);
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

    void UpdateSensitivitySlider() {
        /*
         * Runs everytime the value in the slider is updated. update the current mouse sensitivity.
         */

        /* Only let the slider change the sensitivity value if we are in the Sensitivity state */
        if(state == MenuStates.Sensitivity) {
            sensitivity = sensitivitySlider.value;
            sensitivitySliderValueText.text = ""+Mathf.Round(sensitivity*100)/100f;
            playerController.mouseSens = sensitivity;
            /* Change the color of the text depending on how close it is to the edges */
            float red = 0.8f*Mathf.Clamp((sensitivity - sensitivitySlider.maxValue/2f) / (sensitivitySlider.maxValue/2f), 0, 1);
            float green = 0.8f*Mathf.Clamp((sensitivitySlider.maxValue/2f - sensitivity) / (sensitivitySlider.maxValue/2f), 0, 1);
            sensitivitySliderValueText.color = new Color(1 - green, 1 - red, 1 - green - red, 1);
        }

        /* Return the sensitivity to it's original value */
        else {
            if(sensitivity != sensitivitySlider.value) {
                Debug.Log("WARNING: SENSITIVITY HAS CHANGED OUTSIDE THE SENS STATE");
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

        /* If the menu is empty, Start opening the main menu */
        if(state == MenuStates.Empty) {
            ChangeState(MenuStates.EmptyToMain);
        }

        /* Wanting to quit the sensitivity menu will bring the game back to the main menu */
        else if(state == MenuStates.Sensitivity) {
            ChangeState(MenuStates.SensToMain);
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
                /* Entering the MainToVideo or MainToSens states will set their panels to be active */
                if(newState == MenuStates.MainToVideo) { panelRects[(int) Panels.Video].gameObject.SetActive(true); }
                if(newState == MenuStates.MainToSens) { panelRects[(int) Panels.Sens].gameObject.SetActive(true); }
            }
            
            /* Entering Main will reset the quitValueCurrent */
            else if(newState == MenuStates.Main) {
                quitValueCurrent = 0;
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
                startWidthRatio = continueWidthRatio;
                isGameStarted = true;
            }
            
            /* Leaving the VideoToMain state will set the video panel to be inactive */
            else if(newState == MenuStates.VideoToMain) {
                panelRects[(int) Panels.Video].gameObject.SetActive(false);
            }

            /* Leaving the SensToMain state will set the sensitivity panel to be inactive */
            else if(newState == MenuStates.SensToMain) {
                panelRects[(int) Panels.Sens].gameObject.SetActive(false);
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
            Cursor.lockState = CursorLockMode.Confined;
        }

        /* Unlock the mouse from the window */
        else {
            Cursor.lockState = CursorLockMode.None;
        }
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


    /* ----------- Mouse Enter/Hover Functions ------------------------------------------------------------- */

    void StartButtonClick() {
        /*
         * When the user presses the start key during the Menu state, change into the MenuToIntro state.
         */

        if(IsButtonClickable(Buttons.Start)) {
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
            if(quitValueCurrent == 0) {
                quitValueCurrent += quitValueIncrease*3;
            }else {
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
}
