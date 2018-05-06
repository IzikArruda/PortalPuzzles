﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

/*  
 * The potential states the menu can be in. Some states are transition states
 * that require a certain amount of time to pass until it reaches another state.
 * When a new state is added, a new entry in each visible element of the state is needed.
 * 
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
    //Transitional states
    Startup,
    EmptyToMain,
    MainToEmpty,
    MainToIntro,
    MainToQuit,
    MainToSens,
    SensToMain
};


/*
 * Each button used in the menu. They are placed in an enum so each one will 
 * have their own index in an array.
 */
public enum Buttons {
    Start,
    Sens,
    Quit
}

/*
 * Each panel used by the menu. Adding a panel requires you to:
 * - Create and run the new panel's Setup function
 */
public enum Panels {
    Cover,
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
        new Transition(MenuStates.Startup, MenuStates.Main, 1.0f, 0f),
        new Transition(MenuStates.EmptyToMain, MenuStates.Main, 0.325f, 0f),
        new Transition(MenuStates.MainToEmpty, MenuStates.Empty, 0.325f, 0f),
        new Transition(MenuStates.MainToIntro, MenuStates.Empty, 0.5f, 0f),
        new Transition(MenuStates.MainToQuit, MenuStates.MainToQuit, 1.8f, 0f),
        new Transition(MenuStates.MainToSens, MenuStates.Sensitivity, 0.4f, 0f),
        new Transition(MenuStates.SensToMain, MenuStates.Main, 0.4f, 0f)
    };

    /*
     * Set the state functions for each UI element and every state. Each element requires
     * a state function for each state that the element is visible in.
     */
    StateFunction[] startButtonTransitions;
    StateFunction[] sensButtonTransitions;
    StateFunction[] quitButtonTransitions;
    StateFunction[] coverPanelTransitions;
    StateFunction[] sensPanelTransitions;

    /* Button height to width ratios. Set manually and is unique for each font + text content. */
    private float startBonusSize = 1.3f;
    private float startWidthRatio = 3;
    private float continueWidthRatio = 4.65f;
    private float sensWidthRatio = 5.65f;
    private float quitWidthRatio = 2.25f;
    //Set this to the largest ratio we currently have. This is to make sure each element goes offscreen at the same speed
    private float largestRaio;

    /* Panel sizes. These are set in their setup functions and used in their update functions */
    private float[] panelsWidth;
    private float[] panelsHeight;

    /* Global values used for sizes of UI elements */
    private float minHeight = 40;//40
    private float maxHeight = 175;//175
    private float avgHeight = 100;
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

    /* The sensitivity slider and it's current value */
    private Slider sensitivitySlider;
    private Text sensitivitySliderValueText;
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


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Update() {
        /*
         * Check if the screen has been resized and run any per-frame update calls for any UI elements
         */
        Debug.Log(state);
        
        /* Check if the screen has been resized */
        if(Screen.width != screenWidth || Screen.height != screenHeight) {
            Resize();
        }

        /* Update the hover values of the buttons */
        UpdateHoverValues();

        /* Update the transition values. Only change states once the per-frame updates are done. */
        UpdateTransitionValues();

        /* 
         * Run the per-frame update functions of each UI element 
         */
        /* Start button */
        ExecuteElementFunctions(startButtonTransitions);
        /* Sensitivity button */
        ExecuteElementFunctions(sensButtonTransitions);
        /* Quit button */
        ExecuteElementFunctions(quitButtonTransitions);
        /* Cover panel */
        ExecuteElementFunctions(coverPanelTransitions);
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
        Resize();

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
            new StateFunction(MenuStates.SensToMain, UStartButtonSensToMain)
        };
        sensButtonTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, USensButtonStartup),
            new StateFunction(MenuStates.EmptyToMain, USensButtonEmptyToMain),
            new StateFunction(MenuStates.MainToEmpty, USensButtonMainToEmpty),
            new StateFunction(MenuStates.Main, USensButtonMain),
            new StateFunction(MenuStates.MainToIntro, USensButtonMainToIntro),
            new StateFunction(MenuStates.MainToQuit, USensButtonMainToQuit),
            new StateFunction(MenuStates.MainToSens, USensButtonMainToSens),
            new StateFunction(MenuStates.SensToMain, USensButtonSensToMain),
            new StateFunction(MenuStates.Sensitivity, USensButtonSensitivity),
        };
        quitButtonTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, UQuitButtonStartup),
            new StateFunction(MenuStates.EmptyToMain, UQuitButtonEmptyToMain),
            new StateFunction(MenuStates.MainToEmpty, UQuitButtonMainToEmpty),
            new StateFunction(MenuStates.Main, UQuitButtonMain),
            new StateFunction(MenuStates.MainToIntro, UQuitButtonMainToIntro),
            new StateFunction(MenuStates.MainToQuit, UQuitButtonMainToQuit),
            new StateFunction(MenuStates.MainToSens, UQuitButtonMainToSens),
            new StateFunction(MenuStates.SensToMain, UQuitButtonSensToMain)
        };
        coverPanelTransitions = new StateFunction[] {
            new StateFunction(MenuStates.Startup, UCoverPanelStartup),
            new StateFunction(MenuStates.MainToQuit, UCoverPanelMainToQuit)
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

    public void Resize() {
        /*
         * Update the sizes of the ui to reflect the current screen size.
         */
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        /* Update the buttonHeight value used by all buttons */
        buttonHeight = Mathf.Clamp(screenHeight*0.2f, minHeight, maxHeight);
        heightRatio = buttonHeight/avgHeight;

        //Also we are going to need to update the position of everything, or have each element ALWAYS 
        //run a function that either places the element off or on screen
    }
    
    void ReorderHeirarchy() {
        /*
         * Reorder the hierarchy of the canvas.
         */

        /* So far, simply have the cover panel above all else */
        panelRects[(int) Panels.Cover].transform.SetAsLastSibling();
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
        text.resizeTextMaxSize = 300;

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

        /* The size of the panel should be 80% the screen width and 40% for height */
        panelsWidth[panelEnum] = 0.8f;
        panelsHeight[panelEnum] = 0.4f;
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
        sensitivitySlider.maxValue = 10;
        sensitivitySlider.minValue = 0f;
        sensitivitySlider.value = sensitivitySlider.maxValue/2f;
        sensitivity = sensitivitySlider.value;
        sensitivitySlider.onValueChanged.AddListener(delegate { UpdateSensitivitySlider(); });
        
        /* Add text bellow the slider giving instructions */
        GameObject sliderText = new GameObject("Slider text", typeof(RectTransform));
        Text text = sliderText.AddComponent<Text>();
        RectTransform rectTex = sliderText.GetComponent<RectTransform>();
        sliderText.transform.SetParent(sensPanel);
        sliderText.SetActive(true);
        /* Set the text properties */
        text.text = "Hold right-click to test the mouse sensitivity";
        text.font = usedFont;
        text.alignment = TextAnchor.MiddleCenter;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 1;
        text.resizeTextMaxSize = 300;
        /* Set the sizes of the text */
        rectTex.anchorMin = new Vector2(0, 0.5f);
        rectTex.anchorMax = new Vector2(1, 0.5f);
        rectTex.anchoredPosition = new Vector3(0, -10 -buttonHeight/4f, 0);
        rectTex.sizeDelta = new Vector2(0, buttonHeight/2f);

        /* Add text above the slider giving the sensitivity */
        GameObject sliderValue = new GameObject("Slider value", typeof(RectTransform));
        sensitivitySliderValueText = sliderValue.AddComponent<Text>();
        RectTransform valueRect = sliderValue.GetComponent<RectTransform>();
        sliderValue.transform.SetParent(sensPanel);
        sliderValue.SetActive(true);
        /* Set the text properties */
        sensitivitySliderValueText.text = ""+sensitivity;
        sensitivitySliderValueText.font = usedFont;
        sensitivitySliderValueText.alignment = TextAnchor.MiddleCenter;
        sensitivitySliderValueText.resizeTextForBestFit = true;
        sensitivitySliderValueText.resizeTextMinSize = 1;
        sensitivitySliderValueText.resizeTextMaxSize = 300;
        /* Set the sizes of the value */
        valueRect.anchorMin = new Vector2(0, 0.5f);
        valueRect.anchorMax = new Vector2(1, 0.5f);
        valueRect.anchoredPosition = new Vector3(0, 10 + 1.5f*buttonHeight/4f, 0);
        valueRect.sizeDelta = new Vector2(0, 1.5f*buttonHeight/2f);
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
            case MenuStates.Main:
                quitValueCurrent -= Time.deltaTime*quitValueDecreaseMod;
                if(quitValueCurrent < 0) { quitValueCurrent = 0; }
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

        /* Force certain hover values onto buttons at certain states */
        if(state == MenuStates.MainToSens || state == MenuStates.Sensitivity) {
            /* When the sensitivity menu is open(ing), force the button to be hovered */
            currentHoverState[(int)Buttons.Sens] = true;
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


    /* ----------- UI Element Update Functions ------------------------------------------------------------- */

    #region Cover Panel Updates
    void UCoverPanelStartup() {
        /*
         * Have the panel fade out from pure black to completely opaque for the intro
         */
        int panelEnum = (int) Panels.Cover;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //Start fading 10% into the startup and end 80% into it
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.1f, 0.8f);

        /* Fade the color out relative to the remaining time before the game closes */
        rectImage.color = new Color(0, 0, 0, 1 - transitionFade);
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
        //Get the transition ratio for the current state
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 1);

        /* Place the panel so it can be seen */
        SensPanelPositionUpdate(1 + transitionFade);
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

    #endregion

    #region Start Button Updates
    void UStartButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        //Start fading in the button 50% into the intro, finish 90% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.5f, 0.9f);

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
    #endregion

    #region Sensitivity Button Updates
    void USensButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Sens;
        Button button = buttons[buttonEnum];
        //Start fading in the button 50% into the intro, finish 90% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.5f, 0.9f);

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
         * and will be directly bellow the start button.
         * 
         * Depending on the given side value, place it either on the right or left side of the wall.
         * 0 is completely on the left and 1 is completely on the right.
         */
        int buttonEnum = (int) Buttons.Sens;
        RectTransform rect = buttonRects[buttonEnum];
        /* The sens button will be placed bellow the start button */
        RectTransform aboveButton = buttonRects[(int) Buttons.Start];

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
    #endregion

    #region Quit Button Updates
    void UQuitButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Quit;
        Button button = buttons[buttonEnum];
        //Start fading in the button 60% into the intro, finish 100% in
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0.6f, 1.0f);
        
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
         */
        //The transition starts fading at the start and ends 50% through
        Transition transition = GetTransitionFromState(state);
        float transitionFade = AdjustRatio(TimeRatio(transition.timeRemaining, transition.timeMax), 0, 0.5f*quitWidthRatio/largestRaio);

        /* Move the button from the main to off-screen position */
        QuitButtonHoverUpdate();
        QuitButtonPositionUpdate(1 - transitionFade);
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
    #endregion


    /* ----------- Event/Listener Functions ------------------------------------------------------------- */

    void UpdateSensitivitySlider() {
        /*
         * Runs everytime the value in the slider is updated. update the current mouse sensitivity.
         */
        sensitivity = sensitivitySlider.value;
        sensitivitySliderValueText.text = ""+sensitivity;

        Debug.Log(sensitivity);
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
        else if(state == MenuStates.MainToEmpty) {
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

            /* Entering a transition state will start it's tranistion value */
            if(newState == MenuStates.Startup ||
                newState == MenuStates.EmptyToMain ||
                newState == MenuStates.MainToEmpty ||
                newState == MenuStates.MainToIntro ||
                newState == MenuStates.MainToQuit ||
                newState == MenuStates.MainToSens ||
                newState == MenuStates.SensToMain) {
                ResetRemainingTime(newState);
            }
            
            /* Entering Main will reset the quitValueCurrent */
            else if(newState == MenuStates.Main) {
                quitValueCurrent = 0;
            }
            
            /* Leaving the MainToIntro state will change the start button */
            if(state == MenuStates.MainToIntro) {
                buttons[(int) Buttons.Start].GetComponentInChildren<Text>().text = "CONTINUE";
                startWidthRatio = continueWidthRatio;
                isGameStarted = true;
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

        Application.Quit();
        UnityEditor.EditorApplication.isPlaying = false;
    }

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
    
    void SensButtonClick() {
        /*
         * Clicking on the Sensitivity button does nothing yet
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

        else {
            Debug.Log("WARNING: button not handled in IsButtonClickable");
        }

        return clickable;
    }
}
