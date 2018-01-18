using UnityEngine;
using System.Collections;

/*
 * Contains values for each input the user can use.
 */
public class UserInputs {

    /* Player movement */
    public float playerMovementX;
    public float playerMovementY;
    public float playerMovementXRaw;
    public float playerMovementYRaw;

    /* Mouse movement */
    public float mouseX;
    public float mouseY;

    /* Mouse keys */
    public bool leftMouseButtonPressed;
    public bool leftMouseButtonHeld;
    public bool rightMouseButtonPressed;
    public bool rightMouseButtonHeld;

    /* Keyboard keys */
    public bool spaceBarPressed;
    public bool spaceBarHeld;
    public bool rKeyPressed;


    /* ----------- Update Functions ------------------------------------------------------------- */

    public void UpdateInputs() {
        /*
         * Update the input values of the player for this frame
         */

        playerMovementX = Input.GetAxis("Horizontal");
        playerMovementY = Input.GetAxis("Vertical");
        playerMovementXRaw = Input.GetAxisRaw("Horizontal");
        playerMovementYRaw = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        leftMouseButtonPressed = Input.GetMouseButtonDown(0);
        leftMouseButtonHeld = Input.GetMouseButton(0);
        rightMouseButtonPressed = Input.GetMouseButtonDown(1);
        rightMouseButtonHeld = Input.GetMouseButton(1);
        spaceBarPressed = Input.GetKeyDown("space");
        spaceBarHeld = Input.GetKey("space");
        rKeyPressed = Input.GetKeyDown(KeyCode.R);
    }
}
