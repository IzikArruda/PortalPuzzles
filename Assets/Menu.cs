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
    Empty,
    EmptyToMain,
    Main,
    MainToIntro,
};

/*
 * Each button used in the menu. They are placed in an enum so each one will 
 * have their own index in an array.
 */
public enum Buttons {
    Start
}

/*
 * The menu used by the player during the game. It expected each component to be 
 * already created and assigned in a canvas.
 */
public class Menu : MonoBehaviour {
    private MenuStates state;

    /* Button height to width ratios. Set manually and is unique for each font + text content. */
    private float startWidthRatio = 3;

    /* Global values used for sizes of UI elements */
    private float minHeight = 25;
    private float maxHeight = 200;
    private float buttonHeight;
    
    /* A link to the player's controller */
    private CustomPlayerController playerController;

    /* A basic button object with a text child */
    public GameObject buttonReference;

    /* The font used for the text of the game */
    public Font usedFont;

    /* The canvas that holds all the UI elements */
    public Canvas canvas;
    public RectTransform canvasRect;

    /* An array that holds the main buttons of the UI. Each index has it's own button */
    public Button[] buttons;

    /* The timing values of the transition states */
    private float mainToIntroMax = 0.8f;
    private float mainToIntroRemaining;





    /* The main "Start" button */
    public Button startButton;
    public RectTransform startButtonRect;

    /* Arrays that hold the hover values. Each index is a different button's hover time */
    private bool[] currentHoverState = new bool[] { false, false, false };
    private float[] currentHoverTime = new float[] { 0, 0, 0 };
    private float maxHoverTime = 0.8f;

    /* Previous resolutions of the window */
    public float screenWidth;
    public float screenHeight;


    /* ----------- Built-in Functions ------------------------------------------------------------- */
    
    void Update() {
        /*
         * Check if the screen has been resized and run any per-frame update calls for any UI elements
         */

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
         * Run the per-frame update functions of each button 
         */
        /* Start button */
        if(IsStartVisible()) {
            switch(state) {
                case MenuStates.EmptyToMain:
                    UStartButtonEmptyToMain();
                    break;
                case MenuStates.Main:
                    UStartButtonMain();
                    break;
                case MenuStates.MainToIntro:
                    UStartButtonMainToIntro();
                    break;
                default:
                    Debug.Log("ERROR: Menu item does not handle current state");
                    break;
            }
        }
        
        /* Change the current state if needed after all the per-frame update functions are done */
        UpdateCurrentState();
    }


    /* ----------- Set-up Functions ------------------------------------------------------------- */

    public void InitializeMenu(CustomPlayerController controller) {
        /*
         * Sets up the main menu. Requires a link to the playerController to add functionallity to the buttons
         */
        state = MenuStates.Main;

        /* Link the global variables of the script */
        playerController = controller;
        canvasRect = canvas.GetComponent<RectTransform>();
        
        /* Create and populate the buttons and hover arrays */
        buttons = new Button[System.Enum.GetValues(typeof(Buttons)).Length];
        currentHoverState = new bool[System.Enum.GetValues(typeof(Buttons)).Length];
        currentHoverTime = new float[System.Enum.GetValues(typeof(Buttons)).Length];
        for(int i = 0; i < buttons.Length; i++) {
            buttons[i] = CreateButton().GetComponent<Button>();
            currentHoverState[i] = false;
            currentHoverTime[i] = 0;
        }

        /* Run the initialSetup functions for each of the buttons */
        SetupStartButton();
    }

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

    public void SetupButtonEvents(Button button, UnityAction mouseEnter, UnityAction mouseExit) {
        /*
         * Attach the hover events to the given button
         */
        EventTrigger startButtonTrigger = startButton.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry startButtonEnter = new EventTrigger.Entry();
        startButtonEnter.eventID = EventTriggerType.PointerEnter;
        startButtonEnter.callback.AddListener((data) => { mouseEnter(); });
        startButtonTrigger.triggers.Add(startButtonEnter);
        EventTrigger.Entry startButtonExit = new EventTrigger.Entry();
        startButtonExit.eventID = EventTriggerType.PointerExit;
        startButtonExit.callback.AddListener((data) => { mouseExit(); });
        startButtonTrigger.triggers.Add(startButtonExit);
    }

    public void SetupStartButton() {
        /*
         * Set the values and variables needed for the start button
         */

        /* Create the button */
        startButton = CreateButton().GetComponent<Button>();

        /* Get the components used on the button */
        startButton.onClick.AddListener(StartButtonClick);
        startButtonRect = startButton.GetComponent<RectTransform>();

        /* Set the sizes and content of the button */
        SetupText(startButton.GetComponentInChildren<Text>());
        startButton.GetComponentInChildren<Text>().text = "START";
        startButton.GetComponentInChildren<Text>().fontSize = 100;

        /* Add an event trigger for when the mosue hovers over the button */
        SetupButtonEvents(startButton, StartButtonMouseEnter, StartButtonMouseExit);
    }

    public GameObject CreateButton() {
        /*
         * Duplicate and return the button reference
         */
        GameObject buttonObject = Instantiate(buttonReference);
        buttonObject.transform.parent = canvas.transform;
        
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
    

    /* ----------- Update Functions ------------------------------------------------------------- */

    void UpdateTransitionValues() {
        /*
         * Update transition values and prevent them from going past their lower limit of 0. 
         * Do not update the state as we want the per-frame update functions to atleast run 
         * once the transition states have reached 0.
         */

        switch(state) {
            case MenuStates.MainToIntro:
                mainToIntroRemaining -= Time.deltaTime;
                if(mainToIntroRemaining < 0) { mainToIntroRemaining = 0; }
                break;
        }
    }

    void UpdateCurrentState() {
        /*
         * Check the current state and the transition values. Transition values will either be
         * at or above 0. Once they are at 0, we can transition to the next state.
         */

        switch(state) {
            case MenuStates.MainToIntro:
                if(mainToIntroRemaining == 0) { ChangeState(MenuStates.Empty); }
                break;
        }
    }

    #region Start Button Updates
    void UStartButtonEmptyToMain() {
        /*
         * Update the start button while in the EmptyToMain state. 
         */

        /* For now, do the same as the normal Menu state */
        UStartButtonMain();
    }

    void UStartButtonMain() {
        /*
         * Update the start button while in the Main state.
         */
        Outline[] outlines = startButton.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float hoverRatio = (Mathf.Sin(Mathf.PI*currentHoverTime[(int) Buttons.Start]/maxHoverTime - 0.5f*Mathf.PI)+1)/2f;
        float extraHoverWidth = hoverRatio*buttonHeight*0.5f;

        /* The position and color is effected by the current hover value */
        startButtonRect.sizeDelta = 1.5f*new Vector2(buttonHeight*startWidthRatio + extraHoverWidth, buttonHeight);
        startButtonRect.position = new Vector3(startButtonRect.sizeDelta.x/2f, canvasRect.position.y + buttonHeight/2f, 0);
        float hoverColor = 1f - 0.25f*hoverRatio;
        startButton.GetComponentInChildren<Text>().color = new Color(hoverColor, hoverColor, hoverColor, 1);
    }

    void UStartButtonMainToIntro() {
        /*
         * Update the start button while in the Main to Intro state. This state will not move the button
         * but it will change the opacity of the text along with the outline's distances.
         */
        Outline[] outlines = startButton.GetComponentInChildren<Text>().gameObject.GetComponents<Outline>();
        float transitionFade = 1 - (mainToIntroRemaining / mainToIntroMax);

        /* Change the color of the text and change the outline's distance */
        startButton.GetComponentInChildren<Text>().color = new Color(1, 1, 1, 1 - transitionFade);
        outlines[0].effectDistance = new Vector2(0.5f + 45f*transitionFade, 0.5f + 45f*transitionFade);
        outlines[1].effectDistance = new Vector2(0.5f + 30f*transitionFade, 0.5f + 30f*transitionFade);
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

            /* Going into the MainToIntro state will start it's transition value */
            if(newState == MenuStates.MainToIntro) {
                mainToIntroRemaining = mainToIntroMax;
            }

            /* Change the current state */
            state = newState;
        }
    }

    public void StartButtonClick() {
        /*
         * When the user presses the start key during the Menu state, change into the MenuToIntro state.
         */

        if(state == MenuStates.Main) {
            ChangeState(MenuStates.MainToIntro);
            playerController.StartButtonPressed();
        }

    }

    public void StartButtonMouseEnter() {
        /*
         * The mouse entered the startButton's clickable area
         */

        currentHoverState[(int) Buttons.Start] = true;
    }

    public void StartButtonMouseExit() {
        /*
         * The mouse entered the startButton's clickable area
         */

        currentHoverState[(int) Buttons.Start] = false;
    }


    /* ----------- Helper Functions ------------------------------------------------------------- */

    bool IsStartVisible() {
        /*
         * Return true if the current state shows the start button
         */
        bool visible = false;

        if(state == MenuStates.EmptyToMain || state == MenuStates.Main || state == MenuStates.MainToIntro) {
            visible = true;
        }

        return visible;
    }
}
