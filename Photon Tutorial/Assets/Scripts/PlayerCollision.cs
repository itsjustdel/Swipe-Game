using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour {


    void OnTriggerEnter(Collider collision)
    {        
        //alter bump target on hit player
        if (collision.transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
        {

            PlayerMovement pMthis = transform.parent.parent.GetComponent<PlayerMovement>();
            PlayerMovement pMother = collision.transform.parent.parent.GetComponent<PlayerMovement>();
            pMother.bumped = true;

            //consider if other player hasd a walk target
            Vector3 walkTargetOther = pMother.transform.position;
            if (pMother.walking)
                walkTargetOther = pMother.walkTarget;

            pMother.bumpTarget = pMother.transform.position + (pMother.transform.position - pMthis.transform.position) * .5f + (pMother.transform.position - walkTargetOther);

            //set vibration for our player only
            pMthis.GetComponent<PlayerVibration>().bumpTimer += pMthis.GetComponent<PlayerVibration>().bumpLength;

        }   
        
    }
}
