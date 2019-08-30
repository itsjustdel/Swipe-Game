using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerClassValues : MonoBehaviour {

    //Holds info for attacks/movement etc. Editable from inspector in "Code" Object
    public float respawnTime = 2f;

    public float maxClimbHeight = 3f;

    public float playerCooldownAfterOverheadHit = 0.5f;        
    public float playerCooldownAfterOverheadBlock = 0.5f;
    public float playerCooldownAfterOverheadWhiff = 0.5f;

    public float playerCooldownAfterLungeHit = 0.5f;
    public float playerCooldownAfterLungeBlock = 0.5f;
    public float playerCooldownAfterLungeWhiff = 0.5f;

    //spublic float playerCooldownAfterSideSwipe = 0.5f;
    //public float playerCooldownAfterLunge = 0.5f;

    public float overheadHitCooldown = 0.5f;
    public float overheadBlockCooldown = 0.5f;
    public float overheadWhiffCooldown = 0.5f;

    public float sideSwipeHitCooldown = 0.5f;
    public float sideSwipeBlockCooldown = 0.5f;
    public float sideSwipeWhiffCooldown = 0.5f;

    public float lungeHitCooldown = 0.5f;
    public float lungeBlockCooldown = 0.5f;
    public float lungeWhiffCooldown = 0.5f;

    public float overheadSpeed = 1f; //how fast it steps through the arc
    public float sideSwipeSpeed = 1f;//so, if using this and wanting to match overhead speed, do, arc detail * overhead speed?
    public float lungeSpeed = 1f;

    public float overheadLength= 10f;
    public float sideSwipeLength = 15f;
    public float lungeLength = 20f;

    public float armLength = 10f;
    public float swordLength = 10f;
    public float swordWidth = 2f;

    public float overheadHitHealthReduce = 33.4f;
    public float lungeHitHealthReduce = 15f;

    //time to lean in and pull shield up
    public float blockRaise = .5f;
    public float blockLower = .5f;
    //smallest time block will be locked and activated
    public float blockMinimum = 2f;
    //


}
