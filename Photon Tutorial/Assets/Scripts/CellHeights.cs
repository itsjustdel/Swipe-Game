using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

using ExitGames.Client.Photon;

public class CellHeights : MonoBehaviour
{
    //attached to each player

    PlayerInfo playerInfo;
    PlayerMovement playerMovement;
    Inputs inputs;
    OverlayDrawer overlayDrawer;
    public float heightSpeed = 1f;// put in pgi
    public float heightSpeedForFrontline = .05f;

    public bool raisingCell;
    public bool loweringCell;
    public double eventTime;
    public float startingScaleY;
    
    // Start is called before the first frame update
    void Start()
    {
        overlayDrawer = GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>();
        playerInfo = GetComponent<PlayerInfo>();
        playerMovement = GetComponent<PlayerMovement>();
        inputs = GetComponent<Inputs>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //client only? - no, we will do networked players too by updating their inputs from network event
       // if (GetComponent<PhotonView>().IsMine)
            Height();
    }

    void Height()
    {
        if (!raisingCell && !loweringCell)
            return;

        if (playerMovement.walking)
            return;

        if (playerInfo.currentCell == null)
        {
        }
        else
        {
            //if frontline cell
            //limit to adjacent hieghts
            //find highest adjacent cell
            float highest = 0f;

            float thisHeightSpeed = heightSpeed;
            
            if (playerInfo.currentCell.GetComponent<AdjacentCells>().controlledBy == -1)
            {
                //frontline cells are slower to pull up
                //change temp value that we use for this frame
                thisHeightSpeed = heightSpeedForFrontline;

                for (int k = 0; k < playerInfo.currentCell.GetComponent<AdjacentCells>().adjacentCells.Count; k++)
                {
                    if (playerInfo.currentCell.GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y > highest)
                    {
                        //worked out max height controlled by how many adjacents there are in OverlayDrawer, (saved in adjacent cell script on each cell)
                        highest = playerInfo.currentCell.GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y;
                    }
                }
            }

            float targetY = 0f;
            float fracComplete = 0f;
            float lerpedY = 0f;

            if (raisingCell)
            {
                targetY = playerInfo.currentCell.GetComponent<AdjacentCells>().targetY;
                fracComplete = (float)((PhotonNetwork.Time - eventTime) / thisHeightSpeed);
                lerpedY = Mathf.Lerp(startingScaleY, targetY, fracComplete);

                //stop if frontline cell and has got higher than another adjacent cell
                if (playerInfo.currentCell.GetComponent<AdjacentCells>().controlledBy == -1)
                {
                    if (lerpedY > highest)
                        lerpedY = highest;
                }
            }
            else if (loweringCell)
            {
                targetY = overlayDrawer.minHeight;
                fracComplete = (float)((PhotonNetwork.Time - eventTime) / heightSpeed);
                lerpedY = Mathf.Lerp(startingScaleY, targetY, fracComplete);
            }


            playerInfo.currentCell.transform.localScale = new Vector3(1f, lerpedY, 1f);
            
        }



        //move player up - could prob work this out fine with maffs

        Vector3 shootFrom2 = transform.position;
        RaycastHit hit;
        if (Physics.SphereCast(shootFrom2 + Vector3.up * 50f, 3f, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells", "Wall")))
        {
            transform.position = hit.point;
        }


        if (!loweringCell && !raisingCell)
        {
            


            //stop shaking the camera!
            // Camera.main.GetComponent<CameraShake>().cellShake--;
        }
        else
        {
            //shake the camera!
            // Camera.main.GetComponent<CameraShake>().cellShake++;
        }



    }

    public void SetCellRaising()
    {

        //catch change - will let cell heights script on network know when this hapopened
        if (!raisingCell)
        {
            raisingCell = true;
            SetTimeAndTargets();
        }
        else
            raisingCell = true;
    }

    public void DisableCellRaising()
    {
        if (raisingCell)
        {
            raisingCell = false;
            SetTimeAndTargets();
        }
        else
            raisingCell = false;
    }

    public void SetCellLowering()
    {
        if (!loweringCell)
        {
            loweringCell = true;
            SetTimeAndTargets();
        }
        else
            loweringCell = true;
    }

    public void DisableCellLowering()
    {
        if (loweringCell)
        {
            loweringCell = false;
            SetTimeAndTargets();
        }
        else
            loweringCell = false;
    }

    void SetTimeAndTargets()
    {
        //we need to know start time
        eventTime = PhotonNetwork.Time;
        
        //and starting to scale
        startingScaleY = GetComponent<PlayerInfo>().currentCell.transform.localScale.y;
        //to be able to lerp properly - target is worked out as we are going on cell height script

        //update network
        SendCellHeightToNetwork();
    }
    void SendCellHeightToNetwork()
    {
        byte evCode = 50; // Custom Event 50 : cell height change

        int photonViewID = GetComponent<PhotonView>().ViewID;
        //send id and event start time to all others, as well as raisingCell -  need to know this to update inputs on networked player
        object[] content = new object[] { photonViewID, eventTime, raisingCell,loweringCell,startingScaleY };
        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        //keep resending until server receives
        SendOptions sendOptions = new SendOptions { Reliability = true };



        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
    }


}
