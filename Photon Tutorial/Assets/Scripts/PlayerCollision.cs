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

            pMother.bumped = true;
            pMthis.bumped = true;
            

            pMother.walking = false;
            pMthis.walking = false;

            //reset this flag in case we ahve way through another bump - will force to calculate new target
            pMthis.bumpInProgress = false;
            pMother.bumpInProgress = false;

          //  pMthis.bumpStart = PhotonNetwork.Time;//set after bump target spherecasted on master
           // pMother.bumpStart = PhotonNetwork.Time;

          //  pMthis.bumpStartPos = pMthis.transform.position;
          //  pMother.bumpStartPos = pMother.transform.position;

            //consider if other players had a walk target
            Vector3 walkTargetOther = pMother.transform.position;
            if (pMother.walking)
                walkTargetOther = pMother.walkTarget;

            Vector3 otherBumpTarget = pMother.transform.position + (pMother.transform.position - pMthis.transform.position);// * .5f + (pMother.transform.position - walkTargetOther); //how do we get this?

            //set vibration for our player only
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

            Debug.DrawLine(otherBumpTarget, pMother.transform.position, Color.red);

            //Debug.Break();

            //local - set the client to start making this bump
            //pMother.bumpStartPos = pMother.transform.position;
            pMother.bumpShootfrom = otherBumpTarget;

            //only move our player
          //  if (pMthis.GetComponent<PhotonView>().IsMine && pMthis.bumped == false)
            {
                Debug.Log("reporting hit");

                //consider if other player hasd a walk target
                Vector3 walkTargetThis = pMthis.transform.position;
                if (pMthis.walking)
                    walkTargetThis = pMthis.walkTarget;

                Vector3 thisBumpTarget = pMthis.transform.position + (pMthis.transform.position - pMother.transform.position);// * .5f + (pMthis.transform.position - walkTargetThis); //how do we get this?

                //set vibration for our player only
                pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

                // pMthis.bumped = true;

                //pMthis.bumpStartPos = transform.position;
                pMthis.bumpShootfrom = thisBumpTarget;



            }
        }
    }
}
