using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldCollision : MonoBehaviour {

    void OnTriggerEnter(Collider collision)
    {
        //alter bump target on hit player
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
        {
            //check for self hit, ignore
            if (collision.transform.gameObject == transform.parent.parent.gameObject)
                return;


            PlayerMovement pMthis = transform.parent.parent.parent.GetComponent<PlayerMovement>();
            PlayerMovement pMother = collision.transform.parent.parent.GetComponent<PlayerMovement>();
            pMother.bumped = true;

            //consider if other player hasd a walk target
            Vector3 walkTargetOther = pMother.transform.position;
            if (pMother.walking)
                walkTargetOther = pMother.walkTarget;

            pMother.bumpTarget = pMother.transform.position + (pMother.transform.position - pMthis.transform.position) + (pMother.transform.position - walkTargetOther);

            //alter shield beares move speed to infer hit
            pMthis.currentWalkSpeed = 0f;
            //reset walk so it makes a new start and end point with slowed walk speed
            pMthis.walking = false;
            pMthis.bumpedOther = true;

            //set vibration  - not just re using bump instead of making shield bump var
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;
            pMother.GetComponent<PlayerVibration>().bumpTimer += pMother.GetComponent<PlayerVibration>().bumpLength;

            
        }

        //alter bump target on hit player
        else if (collision.transform.gameObject.layer == LayerMask.NameToLayer("Shield"))
        {
            //bump both playes slightly, if walking have, slight advantage

            PlayerMovement pMthis = transform.parent.parent.parent.GetComponent<PlayerMovement>();
            PlayerMovement pMother = collision.transform.parent.parent.parent.GetComponent<PlayerMovement>();
            pMother.bumped = true;
            //consider if other player hasd a walk target
            Vector3 walkTargetOther = pMother.transform.position;
            if (pMother.walking)
                walkTargetOther = pMother.walkTarget;

            pMother.bumpTarget = pMother.transform.position + (pMother.transform.position - pMthis.transform.position) * .5f + (pMother.transform.position - walkTargetOther); ; //could half this? (non shield bump is halved - let;s leave it like this and see how it plays

            //alter shield beares move speed to infer hit
            pMthis.currentWalkSpeed = 0f;
            //reset walk so it makes a new start and end point with slowed walk speed
            pMthis.walking = false;
            pMthis.bumpedOther = true;

            //set vibration for our player only ? noo - not just re using bump instead of making shield bump var
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;
            pMother.GetComponent<PlayerVibration>().bumpTimer += pMother.GetComponent<PlayerVibration>().bumpLength;

        }

    }
}
