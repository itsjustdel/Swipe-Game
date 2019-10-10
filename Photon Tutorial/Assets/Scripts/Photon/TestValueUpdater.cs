using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;

using ExitGames.Client.Photon;
public class TestValueUpdater : MonoBehaviour
{
    //this allows me tt change values on all clients for testing on the fly
    public bool updateValues;
    OverlayDrawer overlayDrawer;
    PlayerClassValues playerClassValues;
    // Start is called before the first frame update
    void Start()
    {
        overlayDrawer = GetComponent<OverlayDrawer>();
        playerClassValues = GetComponent<PlayerClassValues>(); 
    }


    void FixedUpdate()
    {        
        if(updateValues)
        {
            SendValuesToNetwork();
            updateValues = false;            
        }
    }

    void SendValuesToNetwork()
    {
        
        Debug.Log("[MASTER] - sending player class values to others");
        byte evCode = 80; // Custom Event : Update all players with new class values

        //what we want to send


        object[] content = new object[]
        {
            //player values
            new float[]{
            playerClassValues.respawnTime,
            playerClassValues.maxClimbHeight,
            playerClassValues.playerCooldownAfterOverheadHit,
            playerClassValues.playerCooldownAfterOverheadBlock,
            playerClassValues.playerCooldownAfterOverheadWhiff,
            playerClassValues.overheadSpeed,
            playerClassValues.armLength,
            playerClassValues.swordLength,
            playerClassValues.swordWidth,
            playerClassValues.blockRaise,
            playerClassValues.blockLower,
            playerClassValues.blockMinimum,
            playerClassValues.playerCooldownAfterBump
            },
            //overlay drawer
            new float[]
            {
                overlayDrawer.heightSpeed,
                overlayDrawer.heightSpeedSiege,
                overlayDrawer.heightMultiplier,
                overlayDrawer.minHeight,
            },
            new bool[]
            {
                overlayDrawer.doHeights,
                //overlayDrawer.automaticFrontlineHeightRaise,
                overlayDrawer.reduceFrontline,
                //overlayDrawer.doCapture
            }

        };
        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        //keep resending until server receives
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
        
    }
}
