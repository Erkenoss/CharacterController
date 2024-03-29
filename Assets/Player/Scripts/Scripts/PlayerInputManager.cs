using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    //Cam
    public Vector2 look;
    //Movement
    public Vector2 move;
    public bool sprint;
    //Aim
    public bool aim;
    //Jump
    public bool jump;
    
    //Check cam input
    public void OnLook(InputValue value)
    {
        LookInput(value.Get<Vector2>());
    }
    //Check move input
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }
    //Check sprint input
    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }
    //Check aim input
    public void OnAim(InputValue value)
    {
        AimInput(value.isPressed);
    }
    //Check Jump input
    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void LookInput(Vector2 newLookState)
    {
        look = newLookState;
    }
    public void AimInput(bool newAimState)
    {
        aim = newAimState;
    }
    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }
    public void MoveInput(Vector2 newMoveState)
    {
        move = newMoveState;
    }
    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }
}
