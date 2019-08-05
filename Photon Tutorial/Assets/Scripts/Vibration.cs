using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XInputDotNetPure; // Required in C#

public class Vibration : MonoBehaviour
{ 
    PlayerGlobalInfo pgi;

    public float cellHeightShakeAmount = 0.2f;
    public float walkShakeAmount = 1f;
    public float walkShakeLength = .1f;
    

    void FixedUpdate()
    {
        if (pgi == null)
            pgi = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>();

        VibrationForPlayers();

    }

   
    void VibrationForPlayers()
    {
        CellHeightAdjust();
        Walk();
    }

    void Walk()
    {
        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            PlayerIndex playerIndex = (PlayerIndex)i;
            GamePadState state = GamePad.GetState(playerIndex);
            if (pgi.playerGlobalList[i].GetComponent<PlayerMovement>().walking)
            {
                //shake controller for this player

                if(pgi.playerGlobalList[i].GetComponent<PlayerVibration>().walkVibrate)
                    GamePad.SetVibration(playerIndex, walkShakeAmount, walkShakeAmount);
                
            }
            else
            {
                GamePad.SetVibration(playerIndex, 0f, 0f);
            }
        }
    }

    void CellHeightAdjust()
    {
        //detect if anyone is changing a cell height

        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            PlayerIndex playerIndex = (PlayerIndex)i;
            GamePadState state = GamePad.GetState(playerIndex);
            if (pgi.playerGlobalList[i].GetComponent<PlayerMovement>().adjustingCellHeight)
            {
                //shake controller for this player


                GamePad.SetVibration(playerIndex, cellHeightShakeAmount, cellHeightShakeAmount);
            }
            else
            {
                GamePad.SetVibration(playerIndex, 0f, 0f);
            }
        }
    }
}
