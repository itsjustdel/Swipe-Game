using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XInputDotNetPure; // Required in C#

public class Inputs : MonoBehaviour {
    
    PlayerIndex playerIndex;
    public GamePadState state;
    GamePadState prevState;
    public bool playerIndexSet = false;
    // Use this for initialization
    public float rightStickAxisX;
    public float rightStickAxisY;
    public int playerNumber;

    public bool blocking0 = false;
    public bool blocking1 = false;

    private void Start()
    {
        
    }
    void FixedUpdate()
    {
        
       // if(!playerIndexSet)  //testing network stuff, when working, remove comment
        {
            
            playerNumber = GetComponent<PlayerInfo>().playerNumber;
            playerIndex = (PlayerIndex)playerNumber;

            playerIndexSet = true;
        }

        prevState = state;
        state = GamePad.GetState(playerIndex);

        if (!prevState.IsConnected)
            return;

        //debug
        rightStickAxisX =  state.ThumbSticks.Right.X;
        rightStickAxisY = state.ThumbSticks.Right.Y;


        if (state.Buttons.LeftShoulder == XInputDotNetPure.ButtonState.Pressed)
        {
            blocking0 = true;
        }
        else
            blocking0 = false;

        if (state.Buttons.RightShoulder == XInputDotNetPure.ButtonState.Pressed)
        {
            blocking1 = true;
        }
        else
            blocking1 = false;

        



    }
}

