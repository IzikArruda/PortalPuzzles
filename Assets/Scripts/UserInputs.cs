using UnityEngine;
using System.Collections;

/*
 * Contains values for each input the user can use.
 */
public class UserInputs {

    public float playerMovementX;
    public float playerMovementY;
    public float playerMovementXRaw;
    public float playerMovementYRaw;
    public float mouseX;
    public float mouseY;
    public bool leftMouseButtonPressed;
    public bool leftMouseButtonHeld;
    public bool rightMouseButtonPressed;
    public bool rightMouseButtonHeld;
    public bool spaceBarPressed;
    public bool spaceBarHeld;

    public void UpdateInputs() {
        /*
         * Update the input values of the player for this frame
         */

        playerMovementX = Input.GetAxis("Horizontal");
        playerMovementY = Input.GetAxis("Vertical");
        playerMovementXRaw = Input.GetAxisRaw("Horizontal");
        playerMovementYRaw = Input.GetAxisRaw("Vertical");
        //playerMovementYRaw = 1;
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
        leftMouseButtonPressed = Input.GetMouseButtonDown(0);
        leftMouseButtonHeld = Input.GetMouseButton(0);
        rightMouseButtonPressed = Input.GetMouseButtonDown(1);
        rightMouseButtonHeld = Input.GetMouseButton(1);
        spaceBarPressed = Input.GetKeyDown("space");
        spaceBarHeld = Input.GetKey("space");
    }
}
