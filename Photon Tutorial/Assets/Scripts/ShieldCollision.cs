using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class ShieldCollision : MonoBehaviour {

    PlayerClassValues playerClassValues;

    private void Start()
    {
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
    }

    void OnTriggerEnter(Collider collision)
    {
        //alter bump target on hit player
        //if shield hits player. this = shield, other = player body
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
        {
            //check for self hit, ignore
            if (collision.transform.gameObject == transform.parent.parent.gameObject)
                return;


            PlayerMovement pMthis = transform.parent.parent.parent.GetComponent<PlayerMovement>();
            PlayerMovement pMother = collision.transform.parent.parent.GetComponent<PlayerMovement>();

            if (pMthis.lastPLayerIdCollision == pMother.GetComponent<PhotonView>().ViewID)
            {
                Debug.Log("Already worked out collisions SHIELD, returning");
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
            Vector3 otherBumpTarget = pMother.transform.position - pMother.transform.forward * pMthis.GetComponent<Swipe>().head.transform.localScale.x * playerClassValues.shieldBumpForBumpedMulitplier;

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
            
            Vector3 thisBumpTarget = pMthis.transform.position - pMthis.transform.forward * pMother.GetComponent<Swipe>().head.transform.localScale.x * playerClassValues.shieldBumpForBumperMulitplier;
            //set vibration for our player only
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

            // pMthis.bumped = true;

            //pMthis.bumpStartPos = transform.position;
            pMthis.bumpShootfrom = thisBumpTarget;

        }

        //alter bump target on hit player
        //shield on shield
        else if (collision.transform.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            //bump both playes slightly

            PlayerMovement pMthis = transform.parent.parent.parent.GetComponent<PlayerMovement>();
            PlayerMovement pMother = collision.transform.parent.parent.parent.GetComponent<PlayerMovement>();
            if (pMthis.lastPLayerIdCollision == pMother.GetComponent<PhotonView>().ViewID)
            {
                Debug.Log("Already worked out collisions SHIELD, returning");
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
            Vector3 otherBumpTarget = pMother.transform.position - pMother.transform.forward * pMthis.GetComponent<Swipe>().head.transform.localScale.x * playerClassValues.shieldBumpForBumperMulitplier;//both bumper var, just a small penalty for both

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

            Vector3 thisBumpTarget = pMthis.transform.position - pMthis.transform.forward * pMother.GetComponent<Swipe>().head.transform.localScale.x * playerClassValues.shieldBumpForBumperMulitplier;
            //set vibration for our player only
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

            // pMthis.bumped = true;

            //pMthis.bumpStartPos = transform.position;
            pMthis.bumpShootfrom = thisBumpTarget;

        }

    }
}
