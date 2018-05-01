using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

/*  
 * The potential states the menu can be in. Some states are transition states
 * that require a certain amount of time to pass until it reaches another state.
 */
public enum MenuStates {
    Startup,
    Empty,
    EmptyToMain,
    Main,
    MainToIntro,
    MainToQuit
};

/*
 * Each button used in the menu. They are placed in an enum so each one will 
 * have their own index in an array.
 */
public enum Buttons {
    Start,
    Quit
}

/*
 * Each panel used by the menu
 */
public enum Panels {
    Cover
}

/*
 * The menu used by the player during the game. It expected each component to be 
 * already created and assigned in a canvas.
 */
public class Menu : MonoBehaviour {
    private MenuStates state;

    /* Button height to width ratios. Set manually and is unique for each font + text content. */
    private float startWidthRatio = 3;
    private float continueWidthRatio = 4.65f;
    private float quitWidthRatio = 2.25f;

    /* Global values used for sizes of UI elements */
    private float minHeight = 25;
    private float maxHeight = 200;
    private float buttonHeight;
    
    /* A link to the player's controller */
    private CustomPlayerController playerController;
    
    /* A basic button with a text child and an empty panel. Must be set before running. */
    public GameObject buttonReference;
    public GameObject panelReference;

    /* The font used for the text of the game */
    public Font usedFont;

    /* The canvas that holds all the UI elements */
    public Canvas canvas;
    public RectTransform canvasRect;

    /* An array that holds the main buttons of the UI. Each index has it's own button */
    public Button[] buttons;
    public RectTransform[] buttonRects;

    /* An array of panels used by the menu */
    public RectTransform[] panelRects;

    /* The timing values of the transition states */
    private float startupMax = 3;
    private float startupRemaining;
    private float emptyToMainMax = 1.2f;
    private float emptyToMainRemaining;
    private float mainToIntroMax = 0.8f;
    private float mainToIntroRemaining;
    private float mainToQuitMax = 1.5f;
    private float mainToQuitRemaining;

    /* Arrays that hold the hover values. Each index is a different button's hover time */
    private bool[] currentHoverState;
    private float[] currentHoverTime;
    private float maxHoverTime = 0.8f;

    /* Previous resolutions of the window */
    public float screenWidth;
    public float screenHeight;

    /* 
     * Other menu variables 
     */
    /* Values used when trying to quit the game */
    float quitValueCurrent = 0;
    float quitValueIncrease = 0.15f;
    float quitValueDecreaseMod = 0.3f;
    float quitValueMax = 1;

    /* Values used with the start button and once the game has begun */
    bool startButtonState = true;


    /* ----------- Built-in Functions ------------------------------------------------------------- */

    void Update() {
        /*
         * Check if the screen has been resized and run any per-frame update calls for any UI elements
         */


        //Pressing Y will try to open up the menu again if in the empty state
        if(state == MenuStates.Empty) {
            if(Input.GetKeyDown("y")) {
                ChangeState(MenuStates.Main);
            }
        }
        Debug.Log(state);

        /* Check if the screen has been resized */
        if(Screen.width != screenWidth || Screen.height != screenHeight) {
            Resize();
        }

        /* Update the hover values */
        for(int i = 0; i < currentHoverState.Length; i++) {
            if(currentHoverState[i]) {
                currentHoverTime[i] += Time.deltaTime;
                if(currentHoverTime[i] > maxHoverTime) { currentHoverTime[i] = maxHoverTime; }
            }
            else {
                currentHoverTime[i] -= Time.deltaTime;
                if(currentHoverTime[i] < 0) { currentHoverTime[i] = 0; }
            }
        }

        /* Update the transition values. Only change states once the per-frame updates are done. */
        UpdateTransitionValues();
        
        /* 
         * Run the per-frame update functions of each UI element 
         */
        /* Start button */
        if(IsStartVisible()) {
            switch(state) {
                case MenuStates.Startup:
                    UStartButtonStartup();
                    break;
                case MenuStates.EmptyToMain:
                    UStartButtonEmptyToMain();
                    break;
                case MenuStates.Main:
                    UStartButtonMain();
                    break;
                case MenuStates.MainToIntro:
                    UStartButtonMainToIntro();
                    break;
                case MenuStates.MainToQuit:
                    UStartButtonMainToQuit();
                    break;
                default:
                    Debug.Log("ERROR: Menu item does not handle current state");
                    break;
            }
        }
        /* Quit button */
        if(isQuitVisible()) {
            switch(state) {
                case MenuStates.Startup:
                    UQuitButtonStartup();
                    break;
                case MenuStates.EmptyToMain:
                    UQuitButtonEmptyToMain();
                    break;
                case MenuStates.Main:
                    UQuitButtonMain();
                    break;
                case MenuStates.MainToIntro:
                    UQuitButtonMainToIntro();
                    break;
                case MenuStates.MainToQuit:
                    UQuitButtonMainToQuit();
                    break;
                default:
                    Debug.Log("ERROR: Menu item does not handle current state");
                    break;
            }
        }
        /* Cover panel */
        if(isCoverPanelVisible()) {
            switch(state) {
                case MenuStates.Startup:
                    UCoverPanelStartup();
                    break;
                case MenuStates.MainToQuit:
                    UCoverPanelMainToQuit();
                    break;
                default:
                    Debug.Log("ERROR: Menu item does not handle current state");
                    break;
            }
        }

        /* Change the current state if needed after all the per-frame update functions are done */
        UpdateCurrentState();
    }


    /* ----------- Initialization Functions ------------------------------------------------------------- */

    public void InitializeMenu(CustomPlayerController controller) {
        /*
         * Sets up the main menu. Requires a link to the playerController to add functionallity to the buttons.
         * Start the game in the IntroToMain transition state.
         */
        state = MenuStates.Empty;
        ChangeState(MenuStates.Startup);
        
        /* Link the global variables of the script */
        playerController = controller;
        canvasRect = canvas.GetComponent<RectTransform>();

        /* Create and populate the array of panels used by the UI */
        panelRects = new RectTransform[System.Enum.GetValues(typeof(Panels)).Length];
        for(int i = 0; i < panelRects.Length; i++) {
            panelRects[i] = CreatePanel().GetComponent<RectTransform>();
        }

        /* Run the initialSetup functions for each panel */
        SetupCoverPanel();

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
        SetupQuitButton();

        /* Re-order the hierarchy so that certain objects are rendered ontop of others */
        ReorderHeirarchy();
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

        /* Set the sizes to match the screen size */
        mainPanel.anchoredPosition = new Vector3(0, 0, 0);
        mainPanel.sizeDelta = new Vector2(0, 0);

        /* Set the color so that the panel is invisible */
        mainPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
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

    void UpdateTransitionValues() {
        /*
         * Update transition values and prevent them from going past their lower limit of 0. 
         * Do not update the state as we want the per-frame update functions to atleast run 
         * once the transition states have reached 0.
         */
         
        /* Update the generic StateTransition values */
        switch(state) {
            case MenuStates.Startup:
                UpdateTransitionValue(ref startupRemaining);
                break;
            case MenuStates.EmptyToMain:
                UpdateTransitionValue(ref emptyToMainRemaining);
                break;
            case MenuStates.MainToIntro:
                UpdateTransitionValue(ref mainToIntroRemaining);
                break;
            case MenuStates.MainToQuit:
                UpdateTransitionValue(ref mainToQuitRemaining);
                break;
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

        switch(state) {
            case MenuStates.Startup:
                if(startupRemaining == 0) { ChangeState(MenuStates.Main); }
                break;
            case MenuStates.EmptyToMain:
                if(emptyToMainRemaining == 0) { ChangeState(MenuStates.Main); }
                break;
            case MenuStates.MainToIntro:
                if(mainToIntroRemaining == 0) { ChangeState(MenuStates.Empty); }
                break;
            case MenuStates.MainToQuit:
                if(mainToQuitRemaining == 0) { QuitGame(); }
                break;
            case MenuStates.Main:
                if(quitValueCurrent >= quitValueMax) { ChangeState(MenuStates.MainToQuit); }
                break;
        }
    }

    #region Cover Panel Updates
    void UCoverPanelStartup() {
        /*
         * Do nothing with this for now
         */
        int panelEnum = (int) Panels.Cover;
        Image rectImage = panelRects[panelEnum].GetComponent<Image>();
        //Start fading 10% into the startup and end 80% into it
        float transitionFade = AdjustRatio((startupMax - startupRemaining) / startupMax, 0.1f, 0.8f);

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
        float transitionFade = AdjustRatio((mainToQuitMax - mainToQuitRemaining) / mainToQuitMax, 0.25f, 0.8f);

        /* Fade the color out relative to the remaining time before the game closes */
        rectImage.color = new Color(0, 0, 0, transitionFade);
    }
    #endregion

    #region Start Button Updates
    void UStartButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        //Start fading in the button 50% into the intro, finish 90% in
        float transitionFade = AdjustRatio(TimeRatio(startupRemaining, startupMax), 0.5f, 0.9f);

        /* Leave the positions as their default intro positions */
        rect.sizeDelta = new Vector2(1.5f*buttonHeight*startWidthRatio, 1.5f*buttonHeight);
        rect.position = new Vector3(rect.sizeDelta.x/2f, canvasRect.position.y + buttonHeight/2f, 0);
        outlines[0].effectDistance = new Vector2(0.5f, 0.5f);
        outlines[1].effectDistance = new Vector2(0.5f, 0.5f);

        /* Change the opacity to reflect the transition state */
        button.GetComponentInChildren<Text>().color = new Color(1, 1, 1, transitionFade);

        /////////////////////////
        //Make the button clickable once the startup is in it's final update
        if(startupRemaining == 0) {
            button.GetComponent<Image>().raycastTarget = true;
        }
    }

    void UStartButtonEmptyToMain() {
        /*
         * During this transition state, move the button so it's back onto the screen
         */
        int buttonEnum = (int) Buttons.Start;
        RectTransform rect = buttonRects[buttonEnum];
        float transitionFade = Mathf.Sin((Mathf.PI/2f)*TimeRatio(emptyToMainRemaining, emptyToMainMax));
        float hoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime[buttonEnum]/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;
        float extraHoverWidth = hoverRatio*buttonHeight*0.5f;

        /* Animate the button slidding in from the left side */
        rect.sizeDelta = new Vector2(1.5f*buttonHeight*startWidthRatio + extraHoverWidth, 1.5f*buttonHeight);
        rect.position = new Vector3(-rect.sizeDelta.x/2f + rect.sizeDelta.x*transitionFade, canvasRect.position.y + buttonHeight/2f, 0);
    }

    void UStartButtonMain() {
        /*
         * Update the start button while in the Main state.
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime[buttonEnum]/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;
        float extraHoverWidth = hoverRatio*buttonHeight*0.5f;

        /* The position and color is effected by the current hover value */
        rect.sizeDelta = new Vector2(1.5f*buttonHeight*startWidthRatio + extraHoverWidth, 1.5f*buttonHeight);
        rect.position = new Vector3(rect.sizeDelta.x/2f, canvasRect.position.y + buttonHeight/2f, 0);
        float hoverColor = 1f - 0.25f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);
    }

    void UStartButtonMainToIntro() {
        /*
         * Update the start button while in the Main to Intro state. This state will not move the button
         * but it will change the opacity of the text along with the outline's distances.
         */
        int buttonEnum = (int) Buttons.Start;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float transitionFade = TimeRatio(mainToIntroRemaining, mainToIntroMax);

        /* Change the color of the text and change the outline's distance */
        button.GetComponentInChildren<Text>().color = new Color(1, 1, 1, 1 - transitionFade);
        outlines[0].effectDistance = new Vector2(0.5f + 45f*transitionFade, 0.5f + 45f*transitionFade);
        outlines[1].effectDistance = new Vector2(0.5f + 30f*transitionFade, 0.5f + 30f*transitionFade);

        /* Place the button off the screen on the final frame in the MainToIntro state */
        if(mainToIntroRemaining == 0) {
            /* Position the button off screen */
            rect.position = new Vector3(-rect.sizeDelta.x/2f, canvasRect.position.y + buttonHeight/2f, 0);

            /* Reset the outlines of the button */
            outlines[0].effectDistance = new Vector2(0.5f, 0.5f);
            outlines[1].effectDistance = new Vector2(0.5f, 0.5f);
        }
    }

    void UStartButtonMainToQuit() {
        /*
         * Animate the button slidding off the left side of the screen as the game quits
         */
        int buttonEnum = (int) Buttons.Start;
        RectTransform rect = buttonRects[buttonEnum];
        //The transition starts fading the button at the start and ends 50% through
        float transitionFade = AdjustRatio(TimeRatio(mainToQuitRemaining, mainToQuitMax), 0, 0.5f);

        /* Move the button out to the left side of the screen */
        rect.position = new Vector3(rect.sizeDelta.x/2f - rect.sizeDelta.x*transitionFade, rect.position.y, 0);
    }
    #endregion

    #region Quit Button Updates
    void UQuitButtonStartup() {
        /*
         * Fade the button into view. Have it already placed in it's main menu position
         */
        int buttonEnum = (int) Buttons.Quit;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        /* The button that this quit button will be placed bellow */
        RectTransform aboveButton = buttonRects[(int) Buttons.Start];
        //Start fading in the button 60% into the intro, finish 100% in
        float transitionFade = AdjustRatio(TimeRatio(startupRemaining, startupMax), 0.6f, 1.0f);

        /* Leave the positions as their default intro positions */
        rect.sizeDelta = new Vector2(buttonHeight*quitWidthRatio, buttonHeight);
        float relativeHeight = aboveButton.position.y - aboveButton.sizeDelta.y/2f - buttonHeight/2f;
        rect.position = new Vector3(rect.sizeDelta.x/2f, relativeHeight, 0);
        outlines[0].effectDistance = new Vector2(0.5f, 0.5f);
        outlines[1].effectDistance = new Vector2(0.5f, 0.5f);

        /* Change the opacity to reflect the transition state */
        button.GetComponentInChildren<Text>().color = new Color(1, 1, 1, transitionFade);
    }

    void UQuitButtonEmptyToMain() {
        /*
         * Update the quit button as the menu enters the main from empty
         */
        int buttonEnum = (int) Buttons.Quit;
        RectTransform rect = buttonRects[buttonEnum];
        float hoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime[buttonEnum]/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;
        float extraHoverWidth = hoverRatio*buttonHeight*0.5f;
        float transitionFade = Mathf.Sin((Mathf.PI/2f)*TimeRatio(emptyToMainRemaining, emptyToMainMax));
        /* The button that this quit button will be placed bellow */
        RectTransform aboveButton = buttonRects[(int) Buttons.Start];

        /* For now, do the same as the normal Menu state */
        rect.sizeDelta = new Vector2(buttonHeight*quitWidthRatio + extraHoverWidth, buttonHeight);
        float relativeHeight = aboveButton.position.y - aboveButton.sizeDelta.y/2f - buttonHeight/2f;
        rect.position = new Vector3(-rect.sizeDelta.x/2f + rect.sizeDelta.x*transitionFade, relativeHeight, 0);
    }

    void UQuitButtonMain() {
        /*
         * Update the quit button while in the Main state.
         */
        int buttonEnum = (int) Buttons.Quit;
        Button button = buttons[buttonEnum];
        RectTransform rect = buttonRects[buttonEnum];
        Outline[] outlines = button.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime[buttonEnum]/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;
        float extraHoverWidth = hoverRatio*buttonHeight*0.5f;
        /* The button that this quit button will be placed bellow */
        RectTransform aboveButton = buttonRects[(int) Buttons.Start];

        /* The position and color is effected by it's hover values and the button above it */
        rect.sizeDelta = new Vector2(buttonHeight*quitWidthRatio + extraHoverWidth, buttonHeight);
        float relativeHeight = aboveButton.position.y - aboveButton.sizeDelta.y/2f - buttonHeight/2f;
        rect.position = new Vector3(rect.sizeDelta.x/2f, relativeHeight, 0);
        float hoverColor = 1f - 0.25f*hoverRatio;
        button.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);

        /* Depending on the quitValueCurrent value, update a visual element around the mouse */
        //Add a circle around the mouse depending on the quitValueCurrent/quitValueMax ratio
    }

    void UQuitButtonMainToIntro() {
        /*
         * Animate the quit button when entering the intro. The quit button slides out to the left
         */
        int buttonEnum = (int) Buttons.Quit;
        RectTransform rect = buttonRects[buttonEnum];
        //The transition fade values aims to go from 0 to 2 over the transition state.
        float transitionFade = TimeRatio(mainToIntroRemaining, mainToIntroMax);

        /* Move the button out to the left side of the screen */
        rect.position = new Vector3(rect.sizeDelta.x/2f - rect.sizeDelta.x*transitionFade, rect.position.y, 0);
    }
    
    void UQuitButtonMainToQuit() {
        /*
         * Animate the button slidding off the left side of the screen as the game quits
         */
        int buttonEnum = (int) Buttons.Quit;
        RectTransform rect = buttonRects[buttonEnum];
        //The transition starts fading at the start and ends 50% through
        float transitionFade = AdjustRatio(TimeRatio(mainToQuitRemaining, mainToQuitMax), 0, 0.5f);

        /* Move the button out to the left side of the screen */
        rect.position = new Vector3(rect.sizeDelta.x/2f - rect.sizeDelta.x*transitionFade, rect.position.y, 0);
    }
    #endregion


    /* ----------- Event/Listener Functions ------------------------------------------------------------- */

    void ChangeState(MenuStates newState) {
        /*
         * This is called when changing the states of the menu. This used so that
         * any values that need to be reset upon state change can be adjusted in one function.
         */

        /* Make sure the state being changed to is actually a new state */
        if(state != newState) {

            /* Entering Startup will start it's transition value */
            if(newState == MenuStates.Startup) {
                startupRemaining = startupMax;
            }

            /* Entering EmptyToMain will start it's transition value */
            else if(newState == MenuStates.EmptyToMain) {
                emptyToMainRemaining = emptyToMainMax;
            }

            /* Entering MainToIntro will start it's transition value */
            else if(newState == MenuStates.MainToIntro) {
                mainToIntroRemaining = mainToIntroMax;
            }

            /* Entering Main will reset the quitValueCurrent */
            else if(newState == MenuStates.Main) {
                quitValueCurrent = 0;
            }

            /* Entering MainToQuit will start the time to quit transition value */
            else if(newState == MenuStates.MainToQuit) {
                mainToQuitRemaining = mainToQuitMax;
            }


            /* Leaving the MainToIntro state will change the start button */
            if(state == MenuStates.MainToIntro) {
                buttons[(int) Buttons.Start].GetComponentInChildren<Text>().text = "CONTINUE";
                startWidthRatio = continueWidthRatio;
                startButtonState = false;
            }


            /* Change the current state */
            state = newState;
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


        if(state == MenuStates.Main) {

            /* Start the game by entering the intro state */
            if(startButtonState) {
                ChangeState(MenuStates.MainToIntro);
                playerController.StartButtonPressed();
            }
            else {
                /* Continue the game by entering the empty state */
                ChangeState(MenuStates.Empty);
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

    void QuitButtonClick() {
        /*
         * When clicking on the quit button in the Main state, Increase currentQuitValue.
         */

        if(state == MenuStates.Main) {
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
         * The mouse entered the start Button's clickable area
         */

        currentHoverState[(int) Buttons.Quit] = false;
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

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
         * RealTime:
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

    bool IsStartVisible() {
        /*
         * Return true if the current state shows the start button
         */
        bool visible = false;

        if(state == MenuStates.Startup ||
            state == MenuStates.EmptyToMain || 
            state == MenuStates.Main || 
            state == MenuStates.MainToIntro || 
            state == MenuStates.MainToQuit) {
            visible = true;
        }

        return visible;
    }

    bool isQuitVisible() {
        /*
         * Return true if the current state shows the quit button
         */
        bool visible = false;

        if(state == MenuStates.Startup ||
            state == MenuStates.EmptyToMain || 
            state == MenuStates.Main || 
            state == MenuStates.MainToIntro ||
            state == MenuStates.MainToQuit) {
            visible = true;
        }

        return visible;
    }

    bool isCoverPanelVisible() {
        /*
         * Return true if the cover panel is used
         */
        bool visible = false;

        if(state == MenuStates.Startup ||
            state == MenuStates.MainToQuit) {
            visible = true;
        }

        return visible;
    }
}
