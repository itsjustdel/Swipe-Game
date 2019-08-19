using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerCollision : MonoBehaviour
{


    void OnTriggerEnter(Collider collision)
    {
        //alter bump target on hit player
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
        {

            PlayerMovement pMthis = transform.parent.parent.GetComponent<PlayerMovement>();
            PlayerMovement pMother = collision.transform.parent.parent.GetComponent<PlayerMovement>();

            bool alterOther = false;
            if (alterOther)
            {


                //consider if other player hasd a walk target
                Vector3 walkTargetOther = pMother.transform.position;
                if (pMother.walking)
                    walkTargetOther = pMother.walkTarget;

                Vector3 otherBumpTarget = pMother.transform.position + (pMother.transform.position - pMthis.transform.position);// * .5f + (pMother.transform.position - walkTargetOther); //how do we get this?

                //set vibration for our player only
                pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

                

                //tell other player its been bumped and set its targets

                byte evCode = 22; // Custom Event 21: Used to update player walk targets
                                  //enter the data we need in to an object array to send over the network
                int photonViewID = pMother.GetComponent<PhotonView>().ViewID;

                object[] content = new object[] { otherBumpTarget, photonViewID };
                //send to everyone but this client
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

                //keep resending until server receives
                SendOptions sendOptions = new SendOptions { Reliability = true };

                PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);



            }

            bool alterThis = true;//local
            if(alterThis)
            {

                
                //only move our player
                if (pMthis.GetComponent<PhotonView>().IsMine && pMthis.bumped == false)
                {

                    Debug.Log("reporting hit");

                    //consider if other player hasd a walk target
                    Vector3 walkTargetThis = pMthis.transform.position;
                    if (pMthis.walking)
                        walkTargetThis = pMthis.walkTarget;

                    Vector3 thisBumpTarget = pMthis.transform.position + (pMthis.transform.position - pMother.transform.position);// * .5f + (pMother.transform.position - walkTargetOther); //how do we get this?

                    //set vibration for our player only
                    pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

                    pMthis.bumped = true;

                    pMthis.bumpTarget = thisBumpTarget;


                    //tell others
                    byte evCode = 22; // Custom Event 21: Used to update player walk targets
                                      //enter the data we need in to an object array to send over the network
                    int photonViewID = pMthis.GetComponent<PhotonView>().ViewID;

                    object[] content = new object[] { thisBumpTarget, photonViewID };
                    //send to everyone but this client
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

                    //keep resending until server receives
                    SendOptions sendOptions = new SendOptions { Reliability = true };

                    PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);


                }
                //some small desyncs happening
                //tell everyone about my bump





                //tell other player its been bumped and set its targets
                /*
                byte evCode = 22; // Custom Event 21: Used to update player walk targets
                                  //enter the data we need in to an object array to send over the network
                int photonViewID = pMother.GetComponent<PhotonView>().ViewID;

                object[] content = new object[] { thisBumpTarget, photonViewID };
                //send to everyone but this client
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

                //keep resending until server receives
                SendOptions sendOptions = new SendOptions { Reliability = true };

                PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
                */

            }

        }

    }
}
