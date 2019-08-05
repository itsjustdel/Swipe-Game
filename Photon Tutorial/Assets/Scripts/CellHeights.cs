using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellHeights : MonoBehaviour
{
    PlayerInfo playerInfo;
    PlayerMovement playerMovement;
    Inputs inputs;
    public float heightSpeed = .1f;
    public float heightSpeedForFrontline = .05f;
    
    // Start is called before the first frame update
    void Start()
    {

        playerInfo = GetComponent<PlayerInfo>();
        playerMovement = GetComponent<PlayerMovement>();
        inputs = GetComponent<Inputs>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Heights();
    }

    void Heights()
    {
        if (playerMovement.walking)
            return;

        if (playerInfo.currentCell == null)
        {
        }
        else if (playerInfo.currentCell.GetComponent<AdjacentCells>().frontlineCell)
        {
            //limit to adjacent hieghts
            //find highest adjacent cell
            float highest = 0f;

            for (int k = 0; k < playerInfo.currentCell.GetComponent<AdjacentCells>().adjacentCells.Count; k++)
            {
                if (playerInfo.currentCell.GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y > highest)
                {
                    highest = playerInfo.currentCell.GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y;
                }


            }

            if (inputs.state.Buttons.B == XInputDotNetPure.ButtonState.Pressed)
            {
                float targetY = playerInfo.currentCell.transform.localScale.y + heightSpeed;
                //worked out max height controlled by how many adjacents there are in OverlayDrawer, saved in adjacent cell script on each cell
                if (targetY > highest)
                    targetY = highest;

                playerInfo.currentCell.transform.localScale = new Vector3(1f, targetY, 1f);

                playerMovement.adjustingCellHeight = true;
              //  playerMovement.cellGoingUp = true;
            }
            else if (inputs.state.Buttons.Y == XInputDotNetPure.ButtonState.Pressed)
            {
                float targetY = playerInfo.currentCell.transform.localScale.y - heightSpeed;
                if (targetY < 1f)
                    targetY = 1f;
                playerInfo.currentCell.transform.localScale = new Vector3(1f, targetY, 1f);

                playerMovement.adjustingCellHeight = true;
              //  playerMovement.cellGoingDown = true;
            }

        }
        else
        {
            if (inputs.state.Buttons.B == XInputDotNetPure.ButtonState.Pressed)
            {
                float targetY = playerInfo.currentCell.transform.localScale.y + heightSpeed;
                //worked out max height controlled by how many adjacents there are in OverlayDrawer, saved in adjacent cell script on each cell
                if (targetY > playerInfo.currentCell.GetComponent<AdjacentCells>().targetY)
                    targetY = playerInfo.currentCell.GetComponent<AdjacentCells>().targetY;

                playerInfo.currentCell.transform.localScale = new Vector3(1f, targetY, 1f);

                playerMovement.adjustingCellHeight = true;
              //  playerMovement.cellGoingUp = true;

            }
            else if (inputs.state.Buttons.Y == XInputDotNetPure.ButtonState.Pressed)
            {
                float targetY = playerInfo.currentCell.transform.localScale.y - heightSpeed;
                if (targetY < 1f)
                    targetY = 1f;
                playerInfo.currentCell.transform.localScale = new Vector3(1f, targetY, 1f);

                playerMovement.adjustingCellHeight = true;
               // playerMovement.cellGoingDown = true;

            }


          
        }

        if (inputs.state.Buttons.Y == XInputDotNetPure.ButtonState.Pressed)
        {
            playerMovement.cellGoingDown = true;
        }
        else
        {
            playerMovement.cellGoingDown = false;
        }
        if (inputs.state.Buttons.B == XInputDotNetPure.ButtonState.Pressed)
        {
           playerMovement.cellGoingUp = true;
        }
        else
        {
            playerMovement.cellGoingUp = false;
        }


        if (inputs.state.Buttons.Y == XInputDotNetPure.ButtonState.Released && inputs.state.Buttons.B == XInputDotNetPure.ButtonState.Released)
        {
            playerMovement.adjustingCellHeight = false;


            //stop shaking the camera!
            // Camera.main.GetComponent<CameraShake>().cellShake--;
        }
        else
        {
            //shake the camera!
            // Camera.main.GetComponent<CameraShake>().cellShake++;
        }

        //doubled code here above
    }
}
