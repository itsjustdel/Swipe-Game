using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerCollision : MonoBehaviour
{
    PlayerClassValues playerClassValues;

    private void Start()
    {
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
    }


    void OnTriggerEnter(Collider collision)
    {

        //simplified at the moment, once stable, consider putting momentum back in - can use walk target or fracComplete for walk as a multiplier for distance
        if (!PhotonNetwork.IsMasterClient)
        {
          //  Debug.Log("not master, returning");//usign predictive which will then be overwritten by master (if client)
          //  return;
        }
        //only workign out bumps on master
        

        //alter bump target on hit player
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
        {
            Debug.Log("player collision on master/client");
            //each collider will report a hit, but we work out both collisions on first report
            //we can return if collisions already reported  - note if a new palyer collides, we rework collisions            

            PlayerMovement pMthis = transform.parent.parent.GetComponent<PlayerMovement>();
            
            PlayerMovement pMother = collision.transform.parent.parent.GetComponent<PlayerMovement>();

            if(pMthis.lastPLayerIdCollision == pMother.GetComponent<PhotonView>().ViewID)
            {
                Debug.Log("Already worked out collisions, returning");
                return;
            }
            
            pMother.bumped = true;
            pMthis.bumped = true;
            

            pMother.walking = false;
            pMthis.walking = false;

            //remember who we bumped os we don't work out two bumps from same player
            pMthis.lastPLayerIdCollision = pMother.GetComponent<PhotonView>().ViewID;
            pMother.lastPLayerIdCollision = pMthis.GetComponent<PhotonView>().ViewID;

            //simplfying bump penalties - not using walk target- use transfor.forward * size of player who bumped them
            Vector3 otherBumpTarget = pMother.transform.position - pMother.transform.forward * pMthis.GetComponent<Swipe>().head.transform.localScale.x*playerClassValues.bumpMulitplier;

            //set vibration for our player only
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

            //Debug.DrawLine(otherBumpTarget, pMother.transform.position, Color.red);

            //local - set the client to start making this bump            
            pMother.bumpShootfrom = otherBumpTarget;
            
            //Debug.Log("reporting hit");

            //consider if other player hasd a walk target
            Vector3 walkTargetThis = pMthis.transform.position;
            if (pMthis.walking)
                walkTargetThis = pMthis.walkTarget;

            //reset this flag in case we ahve way through another bump - will force to calculate new target
            pMthis.bumpInProgress = false;
            pMother.bumpInProgress = false;

            //simplifying
            //.Vector3 thisBumpTarget = pMthis.transform.position + (pMthis.transform.position - pMother.transform.position);// * .5f + (pMthis.transform.position - walkTargetThis); //how do we get this?
            Vector3 thisBumpTarget = pMthis.transform.position - pMthis.transform.forward * pMother.GetComponent<Swipe>().head.transform.localScale.x * playerClassValues.bumpMulitplier;
            //set vibration for our player only
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

            // pMthis.bumped = true;

            //pMthis.bumpStartPos = transform.position;
            pMthis.bumpShootfrom = thisBumpTarget;



            
        }
    }
}
