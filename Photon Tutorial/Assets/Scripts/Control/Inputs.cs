using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XInputDotNetPure; // Required in C#

public class Inputs : MonoBehaviour {

    public bool useKeys = false;
    public float keySpeed = .05f;

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
   // public bool raisingCell = false;
   // public bool loweringCell = false;
    public bool startButtonPressed;
    CellHeights cellHeights;


    public float x;
    public float y;
    private void Start()
    {
        cellHeights = GetComponent<CellHeights>();
    }
    void FixedUpdate()
    {
        
       // if(!playerIndexSet)  //testing network stuff, when working, remove comment
        {
            
            playerNumber = GetComponent<PlayerInfo>().playerNumber;
            playerIndex = (PlayerIndex)playerNumber;

            playerIndexSet = true;
        }

        if (useKeys)
            Keys();
        else
            Pad();

    }

    void Keys()
    {
        KeyCode upKey = KeyCode.W;
        KeyCode downKey = KeyCode.S;
        KeyCode leftKey = KeyCode.A;
        KeyCode rightKey = KeyCode.D;
        KeyCode block0 = KeyCode.LeftControl;
        KeyCode raiseCell = KeyCode.R;
        KeyCode lowerCell = KeyCode.F;

        if (GetComponent<PlayerInfo>().playerNumber == 1)
        {
            upKey = KeyCode.UpArrow;
            downKey = KeyCode.DownArrow;
            leftKey = KeyCode.LeftArrow;
            rightKey = KeyCode.RightArrow;

            block0 = KeyCode.RightControl;
            raiseCell = KeyCode.PageUp;
            lowerCell = KeyCode.PageDown;
        }
        //use keyboard
        if (Input.GetKey(leftKey))
            x -= keySpeed;
        else if (Input.GetKey(rightKey))
            x += keySpeed;
        else if (x < 0)
            x += keySpeed;
        else if (x > 0)
            x -= keySpeed;


        if (Input.GetKey(upKey))
            y -= keySpeed;

        else if (Input.GetKey(downKey))
            y += keySpeed;
        else if (y < 0)
            y += keySpeed;
        else if (y > 0)
            y -= keySpeed;


        x = Mathf.Clamp(x, -1f, 1f);
        y = Mathf.Clamp(y, -1f, 1f);

        if (Input.GetKeyDown(block0))
            blocking0 = true;
        else
            blocking0 = false;

        //cell heights
        if (Input.GetKey(raiseCell))
            cellHeights.SetCellRaising();
        else
            cellHeights.DisableCellRaising();

        if (Input.GetKey(lowerCell))
            cellHeights.SetCellLowering();

        else
            cellHeights.DisableCellLowering();
    }

    void Pad()
    {
        x = state.ThumbSticks.Left.X;
        y = -state.ThumbSticks.Left.Y;//inverted         

        prevState = state;
        state = GamePad.GetState(playerIndex);

        if (!prevState.IsConnected)
            return;

        //debug
        rightStickAxisX = state.ThumbSticks.Right.X;
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

        if (state.Buttons.Start == XInputDotNetPure.ButtonState.Pressed)
        {
            startButtonPressed = true;
        }
        else
            startButtonPressed = false;

        //cell heights
        if (state.Buttons.B == XInputDotNetPure.ButtonState.Pressed)        
            cellHeights.SetCellRaising();        
        else        
            cellHeights.DisableCellRaising();       

        if (state.Buttons.Y == XInputDotNetPure.ButtonState.Pressed)
            cellHeights.SetCellLowering();        
        else        
            cellHeights.DisableCellLowering();
        
    }
    
}

