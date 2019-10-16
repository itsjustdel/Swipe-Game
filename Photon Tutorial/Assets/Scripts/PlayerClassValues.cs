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

    public float playerCooldownAfterAttackedTooClose = 0.5f;
    public float playerCooldownAfterBump = 0.5f;    

    public float overheadSpeed = 1f; //how fast it steps through the arc
  
    public float armLength = 10f;
    public float swordLength = 10f;
    public float swordWidth = 2f;


    //bump speeds //same as movement?
     public float bumpSpeed = 10f;
     public float bumpBounceAmount = 10f;
    //bump distances
    //uses player size multiplied by below cariables for bump distance

    public float bumpMulitplier = 3f;
    public float shieldBumpForBumpedMulitplier = 5f;
    public float shieldBumpForBumperMulitplier = 5f;

    //time to lean in and pull shield up
    public float blockRaise = .5f;
    public float blockLower = .5f;
    //smallest time block will be locked and activated
    public float blockMinimum = 2f;
    //
    public float blockRotation = 1f;
    public float blockRotationNetworkLerp = 1f;
    public float clientMovementLerp = 0.9f;

}
