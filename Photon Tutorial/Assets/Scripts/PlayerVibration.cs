using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XInputDotNetPure; // Required in C#

public class PlayerVibration : MonoBehaviour
{

    public bool doWalk;
    public bool doCellHeights = true;

    public bool walkVibrate;
    public float walkTimer;
    public bool pullBack = false;    
    public bool playerHit = true;    
    public bool shieldHit = true;
    public bool swipeHit = true;
    public bool bump = true;
    

    public float cellHeightShakeAmount = 0.2f;
    public float walkShakeAmount = 1.5f;
    public float walkShakeLength = .1f;

    public float shakeTimerShield = 0f;
    public float shakeTimerHit = 0f;
    public float nonLethatHitLength = .33f;
    public float lethatHitLength = .66f;
    public float hitShakeAmount = 1f;

    public float shieldHitAmount = 1f;
    public float shieldHitLength = 0.2f;

    public float swipeHitAmount = 1f;
    public float swipeHitLength = .2f;
    public float swipeHitTimer = 0f;

    public float bumpAmount = .5f;
    public float bumpLength = .2f;//lowest it can really go?
    public float bumpTimer = 0f;

    public float pullBackShakeAmount=0.1f;
    int playerNumber;
    PlayerIndex playerIndex;
    Swipe swipe;

    float vibrateAmount;

    
    // Start is called before the first frame update
    void Start()
    {
        playerNumber = GetComponent<PlayerInfo>().playerNumber;
        playerIndex = (PlayerIndex)playerNumber;
        swipe = GetComponent<Swipe>();
    }

    // Update is called once per frame
    void Update()
    {
        vibrateAmount = 0f;

        if(doCellHeights)
            CellHeight();

        if(doWalk)
            Walk();

        if (pullBack)
            PullBack();

        if (playerHit)
            PlayerHit();

        if (shieldHit)
            Shield();

        if (swipe)
            Swipe();

        if (bump)
            Bump();

        GamePad.SetVibration(playerIndex, vibrateAmount, vibrateAmount);
    }

    void CellHeight()
    {
        
        GamePadState state = GamePad.GetState(playerIndex);
        if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell)
        {
            //don't do if cell has hit limits -  we are checking this in player sounds so let's just grab the bool from there
            if (GetComponent<PlayerSounds>().cellAtLimit)
            {
                return;
            }
            else
            {
                //shake controller for this player
                vibrateAmount += cellHeightShakeAmount;                
            }
        }
    }

    void Walk()
    {
        //hmm i tihnk vibrations aren't reactive enough to small pulses
        if(walkTimer > 0f)
        {
            //GamePad.SetVibration(playerIndex, walkShakeAmount*walkTimer, walkShakeAmount*walkTimer);
            vibrateAmount += walkShakeAmount * walkTimer;
        }

        if(walkTimer > 0)
            walkTimer -= Time.fixedDeltaTime;
    }

    void PullBack()
    {
        if(swipe.pulledBackForOverhead)
        {
            // GamePad.SetVibration(playerIndex, pullBackShakeAmount, pullBackShakeAmount);
            vibrateAmount += pullBackShakeAmount;
        }
    }

    void PlayerHit()
    {
        shakeTimerHit -= Time.deltaTime;
        if (shakeTimerHit < 0f)
            shakeTimerHit = 0f;

        if (shakeTimerHit > 0f)
        {
            //GamePad.SetVibration(playerIndex, hitShakeAmount, hitShakeAmount);
            vibrateAmount += hitShakeAmount;

            Camera.main.GetComponent<CameraShake>().ShakeForHit();
           // Camera.main.GetComponent<CameraShake>().shakeDuration += 0.2f;
        }
    }

    void Shield()
    {
        shakeTimerShield -= Time.deltaTime;
        if (shakeTimerShield < 0f)
            shakeTimerShield = 0f;

        if (shakeTimerShield > 0f)
            //GamePad.SetVibration(playerIndex, shieldHitAmount, shieldHitAmount);
            vibrateAmount += shieldHitAmount;
    }

    void Swipe()
    {
        swipeHitTimer -= Time.deltaTime;
        if (swipeHitTimer < 0f)
            swipeHitTimer = 0f;

        if (swipeHitTimer > 0f)
            //GamePad.SetVibration(playerIndex, shieldHitAmount, shieldHitAmount);
            vibrateAmount += swipeHitAmount;
    }

    void Bump()
    {
        bumpTimer -= Time.deltaTime;
        if (bumpTimer < 0f)
            bumpTimer = 0f;

        if (bumpTimer > 0f)            
            vibrateAmount += bumpAmount;
    }
}

