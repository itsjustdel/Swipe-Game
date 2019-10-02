using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerMovement : MonoBehaviourPun {

    
    public bool doBasicMove = true;
    public bool moveToAdjacent = false;
    public float deadzone = 0.4f;
    public float walkRotationSpeed = 0.115f;
    public float rotationSpeed = 0.15f;
    public float movementSpeed = 1f;
   // public float jumpSpeed = 15f;
   // public float jumpStepDistance = 15f;
  //  public float jumpBounceAmount = 15f;
    public float bumpSpeed = 20f;
    public float bumpBounceAmount = 10f;
    public float walkSpeed = 7f;
    public float walkStepDistance = 5f;
    public float walkBounceAmount = 5f;
    public float walkSpeedThisFrame = 10f;
    public float walkSpeedWhileAttacking = 1f;
    public float walkSpeedWhilePullBack = 3.5f;
    public float walkSpeedWhileBlocking = 3.5f;    
    public float walkSpeedWhileBlockingOverhead = 2f;
    public float sprintSpeed = 10f;
    public float sprintStepDistance = 10f;
    public float sprintBounce = 7f;

    public float inertiaSpeed = .33f;//used for accel and decel when attacking
    public float bumpInertia = .1f;
    public float currentWalkSpeed;
    


    public float shieldStepDistance = 3f;
    public float shieldStepDistanceOverhead = 1f;

    public bool moving = false;
    public bool walking = false;
    
    
    //bumped from other player
    public bool bumped = false;
    public bool bumpInProgress = false;
    public bool bumpedOther;
    
    public bool sprinting = false;

    public bool targetFound = false;
    public Vector3 lastTarget;//spawner needs access
    public Vector3 target;
    public GameObject targetCell;
    public bool leftStickReset = true;

    public float angle;
    public float stickAngle;
    public Vector3 lookDir;

    //animation
    public float startTime;
    public float journeyTime;

    public int playerNumber = 0;


    public List<GameObject> currentAdjacents = new List<GameObject>();
    public GameObject codeObject;

    public Vector3 rotStart;
    public bool rotationTargetSet = false;

    //jump stuff
    public float jumpStart;
    public Vector3 walkStartPos;
    public Vector3 jumpStartPos;
    public Vector3 jumpTarget;
    public Vector3 walkTarget;
    public double walkStart;

    public Vector3 bumpShootfrom;
    public Vector3 bumpTarget;
    public double bumpStart;
    public Vector3 bumpStartPos;

    //rotation stuff
    private Vector3 rotateTarget;

    public float x;
    public float y;
    public float leftStickMagnitude;
  //  float leftStickMagnitudeForStep;
  //  public bool buttonA;
  //  public bool buttonB;
  //  public bool buttonX;
  //  public bool buttonY;

    public float fracComplete = 0f;

    public PlayerGlobalInfo pgi;
    public PlayerClassValues playerClassValues;
    public OverlayDrawer overlayDrawer;
    PlayerAttacks pA;

    public GameObject head;
    bool freeWalk = false;

    Swipe swipe;
    //PlayerAttacks playerAttacks;
    Inputs inputs;

    PhotonView thisPhotonView;
    
    void Start()
    {
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
        overlayDrawer = GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>();
        pgi = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>();
        head = transform.Find("Head").gameObject;
        currentWalkSpeed = walkSpeed;
        swipe = GetComponent<Swipe>();
        pA = GetComponent<PlayerAttacks>();//??
        inputs = GetComponent<Inputs>();
       // playerAttacks = GetComponent<PlayerAttacks>();
        //only control our own player - the network will move the rest
        if (!GetComponent<PhotonView>().IsMine)// && PhotonNetwork.IsConnected == true)
        {

            return;
        }

        
        
       
        codeObject = GameObject.FindGameObjectWithTag("Code");
        playerNumber = GetComponent<PlayerInfo>().playerNumber;
      
        
        //enable if we are controlling this
      
        inputs.enabled = true;

       
        swipe.enabled = true;

        pA.enabled = true;
       // playerAttacks.enabled = true;
        
        lastTarget = transform.position;//using?>
        


       
    }
    
    // Update is called once per frame
    void FixedUpdate()
    {
       
       

     

        if (GetComponent<PhotonView>().IsMine)
        {
            GetInputs();//this should all be done in inputs script and then referenced from here

            //work out which player is looking compared to camera - 
            CalculateLookDir();
            //bool for stick reset
            CalculateStickReset();
            //send inputs to other clients for prediction            
            SendInputsToNetwork();//this is happening every fixed update - meaning the messaages per room will be high, perhaps lower the frequency of this
        }
        else
        {
            //lookdir already received from network
            CalculateStickReset();

        }




        //adjust speed slowly if between states (bumper etc)
        Inertias();


        BasicMove();


        //heights for head
        
        
        HeadHeight();
        

            //rotate player

            //rotations for head
        if (GetComponent<PlayerAttacks>().blocking)//test blocking- if blocking, should be stuck in animation -ok it seems)
        {
            //rotations are done in player attacks in Block()
        }
        else if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell)
        {
            //we are adjusting cell, ignore any right stick input
            LookToGround();
            
        }
        else if (swipe.overheadSwiping || swipe.planningPhaseOverheadSwipe || swipe.pulledBackForOverhead)
        {
            //Debug.Log("rotating for swipe");
            RotateForSwipe();
        }
        else if (!swipe.whiffed && !swipe.overheadSwiping) //using whiff? dont think so
        {
            //Debug.Log("Rotating to face right stick");            

            //rotate transform to either look at closest player or face the direciton of movement(left stick)
            RotateToFaceClosestPlayer();
            //rotate head back to neutral
            RotateHeadToFaceRightStick();
        }

      
    }

    private void Update()
    {
        //movement, targets worked out in fixed update
        if (bumped)
        {
            
             LerpBump(); 

        }

        if (!walking && !bumped && GetComponent<PhotonView>().IsMine)//only work out new target on local client - otherwise the target is sent over the network already worked out
        {
        


        }
        else if (walking)
        {
             LerpPlayer();
        }
    }

    void GetInputs()
    {
        
        x = inputs.x;
        y = inputs.y;
        

        leftStickMagnitude = new Vector3(x, 0f, y).magnitude;
    }

    void CalculateLookDir()
    {
        Vector3 right = x * Camera.main.transform.parent.right;
        Vector3 forward = y * Camera.main.transform.parent.forward;
        //public var so helper class can access
        lookDir = right - forward;
        
    }

    void CalculateStickReset()
    {
        
        if (lookDir.magnitude < deadzone)
        {
            leftStickReset = true;
        }
        else
            leftStickReset = false;
    }

    void Inertias()
    {
        if (bumpedOther)//confsued about bumped other. why not just bumped?
        {
            if (currentWalkSpeed < walkSpeedThisFrame)
                currentWalkSpeed += bumpInertia;
            if (currentWalkSpeed >= walkSpeedThisFrame)
            {
                currentWalkSpeed = walkSpeedThisFrame;
                bumpedOther = false;
            }
        }

        else
        {
            if (currentWalkSpeed < walkSpeedThisFrame)
                currentWalkSpeed += inertiaSpeed;
            if (currentWalkSpeed >= walkSpeedThisFrame)
                currentWalkSpeed = walkSpeedThisFrame;
        }
    }

    void RotateToFaceRightStick()//old
    {
        //get target cell, cell which stick is pointing closest to
        pA.targetCellRightStick = pA.NearestCellToStickAngle(pA.lookDirRightStick);

        //if outside dead zone
        //dont spin if stabbing, commit to the move
        if (!pA.rightStickReset)
        {
            //face the way way the stick is pushed
            //set new target
            Vector3 targetY0 = pA.targetCellRightStick.GetComponent<ExtrudeCell>().centroid - transform.position;

            targetY0.y = 0;
            rotateTarget = targetY0;

        }
        //always rotate towards target unles left stick is guiding movement and right stick is zero
        if (!pA.rightStickReset && GetComponent<PlayerMovement>().leftStickReset)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rotateTarget);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, GetComponent<PlayerMovement>().rotationSpeed);
        }
    }

    void RotateForSwipe()
    {

        if (swipe.overheadSwiping && swipe.currentSwipeObject != null)
        {
            Mesh mesh = swipe.currentSwipeObject.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            if (mesh.vertices.Length > 4)
            {
                Vector3 center = Vector3.zero;
                for (int i = vertices.Length - 4; i < vertices.Length; i++)
                {
                    center += vertices[i];

                }
                center /= 4;
                //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //c.transform.position = center + swipe.currentSwipeObject.transform.position;

                Quaternion targetRotation = Quaternion.LookRotation(center);
                head.transform.rotation = Quaternion.Lerp(head.transform.rotation, targetRotation, pA.headRotationSpeed);
            }
        }
        else if (swipe.planningPhaseOverheadSwipe || swipe.pulledBackForOverhead)
        {
             Quaternion targetRotation = Quaternion.LookRotation(swipe.firstPullBackLookDir);
             head.transform.rotation = Quaternion.Lerp(head.transform.rotation, targetRotation, pA.headRotationSpeed);
        }
        else if (swipe.currentSwipeObject == null)
        {
            RotateHeadToFaceRightStick(); 
        }


    }

    void RotateHeadToFaceRightStick()
    {

        float tempRotSpeed = pA.headRotationSpeed;
        if (bumpedOther)
            tempRotSpeed = pA.headRotationSpeedWhenBumping;

        //look out for overhead block, this is cosmetic


        //if blocking, we shouldnt be here

        if (!pA.rightStickReset)
        {
            Vector3 rotateTargetForHead = pA.lookDirRightStick.normalized;
            //angle for block
            if (inputs.blocking0 && inputs.blocking1)//?
                rotateTargetForHead += Vector3.up;

            rotateTargetForHead.Normalize();

            if (rotateTargetForHead == Vector3.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(rotateTargetForHead);
            //flatten
            // targetRotation.eulerAngles = new Vector3(0f, targetRotation.eulerAngles.y, 0f);
            head.transform.rotation = Quaternion.Lerp(head.transform.rotation, targetRotation, tempRotSpeed);
        }
        else if (pA.rightStickReset)
        {
            //stops look direction zero problem
            Quaternion targetRotation = Quaternion.identity;
            //angle for block
            if (inputs.blocking0 && inputs.blocking1)
                targetRotation = Quaternion.LookRotation(Vector3.up + Vector3.forward);

            targetRotation.Normalize();

            head.transform.localRotation = Quaternion.Lerp(head.transform.localRotation, targetRotation, tempRotSpeed);
        }

    }

    void RotateForWhiff()
    {

        //player looks towards ground if in whiff state
        Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
        if (swipe.whiffed)
        {

            targetRot = Quaternion.LookRotation(Vector3.forward - Vector3.up * .5f);

            head.transform.localRotation = Quaternion.Lerp(head.transform.localRotation, targetRot, pA.whiffDuckSpeed);

            //duck
            //head.transform.localPosition -= Vector3.up * whiffDuckSpeed;
            if (pA.headOriginalPos.y - head.transform.localPosition.y < head.transform.localScale.y)
                head.transform.localPosition -= Vector3.up * pA.whiffDuckSpeed;

        }
        else
        {
            
            //unduck
            head.transform.localPosition += Vector3.up * pA.whiffDuckSpeed;
            if (head.transform.localPosition.y > pA.headOriginalPos.y)
                head.transform.localPosition = pA.headOriginalPos;


            targetRot = Quaternion.LookRotation(Vector3.forward);

            head.transform.localRotation = Quaternion.Lerp(head.transform.localRotation, targetRot, pA.duckSpeed);
            
        }
    }

    void LookToGround()
    {
       // Debug.Break();
        //player looks towards ground if in whiff state
        Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
       // if (swipe.whiffed)
        {

            //should be lerped - need to add current head rotation variable -- leaving atm as will probably only be a visual aid and not involved in too many hit checks

            //double eventStart = GetComponent<CellHeights>().eventTime;
            //float fracComplete = (float)((PhotonNetwork.Time - eventStart) * 10f);

            targetRot = Quaternion.LookRotation(Vector3.forward - Vector3.up * .5f);

            //Quaternion lerpedRot = Quaternion.Slerp -- current head starting position needed to ad this

            
            head.transform.localRotation = Quaternion.Lerp(head.transform.localRotation, targetRot, pA.whiffDuckSpeed);

        }
    }

    void HeadHeight()
    {
        double eventStart = GetComponent<CellHeights>().eventTime;
        float headLowerSpeed = 10f;
        if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell)
        {
            //should be lerp from cell height event time
            
            
            
            float fracComplete = (float)((PhotonNetwork.Time - eventStart) * headLowerSpeed);
            Vector3 target = Vector3.Lerp(pA.headOriginalPos, pA.headOriginalPos - head.transform.localScale.y * Vector3.up, fracComplete);

            head.transform.localPosition = Vector3.Lerp(head.transform.localPosition, target, playerClassValues.blockRotationNetworkLerp);//using block rotation value - maybe should just be general network vale
            //duck
            //head.transform.localPosition -= Vector3.up * whiffDuckSpeed;
          
        }
        else
        {
            float fracComplete = (float)((PhotonNetwork.Time - eventStart) * headLowerSpeed);
            Vector3 target = Vector3.Lerp(pA.headOriginalPos - head.transform.localScale.y * Vector3.up, pA.headOriginalPos, fracComplete);
            head.transform.localPosition = Vector3.Lerp(head.transform.localPosition, target, playerClassValues.blockRotationNetworkLerp);//using block rotation value - maybe should just be general network vale
        }
    }

    void RotateToFaceClosestPlayer()
    {       

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        
        //find closest player to this player
        float distance = Mathf.Infinity;
        GameObject closestPlayer = players[0];
        for (int i = 0; i < players.Length; i++)
        {
            //don't check this player
            if (gameObject == players[i])
                continue;

            //dont look at dead people
            if (players[i].GetComponent<PlayerInfo>()!=null && players[i].GetComponent<PlayerInfo>().playerDespawned)
                continue;

            float temp = Vector3.Distance(transform.position, players[i].transform.position);
            if (temp < distance)
            {
                closestPlayer = players[i];
                distance = temp;
            }

        }

        if(players.Length ==1 || distance > 70f)
        {
           // Debug.Log("rotating to face look dir");
                
            freeWalk = true;
            RotateToMovementDirection();
        }
        else
        {
            //Debug.Log("rotating to face closest player");
            //rotate to face closest player

            //face the way way the stick is pushed
            //set new target
            Vector3 targetY0 = closestPlayer.transform.position - transform.position;
            targetY0.y = 0;// transform.position.y;
            rotateTarget = targetY0;

            //Debug.DrawLine(transform.position, closestPlayer.transform.position);

            Quaternion targetRotation = Quaternion.LookRotation(rotateTarget);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed); //**re using var

            freeWalk = false;
        }
        
    }

    void RotateToMovementDirection()
    {
        //rotation
        //using right stick for rotation

        //if (GetComponent<PlayerAttacks>().rightStickReset == true)// && (GetComponent<PlayerAttacks>().stabbing == false || GetComponent<PlayerAttacks>().pullBackStab))
        {
            if (lookDir.magnitude < deadzone)//was x ==0  and y == 0 - changed for network sending lookdir
            {
                //do nothing
            }
            else
            {
                if (moveToAdjacent)
                {
                    if (!leftStickReset && lastTarget != target)  //what happens if right stick is pushed too?
                    {
                        //this needs start and end target, not lerp from current transform

                        //face the way way the stick is pushed
                        Vector3 targetY = target - lastTarget;
                        Vector3 targetY0 = targetY;
                        targetY0.y = 0;
                        Quaternion targetRotation = Quaternion.LookRotation(targetY0);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, walkRotationSpeed);
                    }



                }
                else if (doBasicMove)
                {
                    if (!leftStickReset)
                    {

                        
                        //face the way way the stick is pushed
                        Vector3 targetY = lookDir;
                        Quaternion targetRotation = Quaternion.LookRotation(targetY);
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, walkRotationSpeed);
                    }
                }
            }
        }
        //else
        {
            //let the right stick have priority
        }

    }

    void BasicMove()
    {
        
        Vector3 thisLook = lookDir;

        bool blockNewStep = false;
        if (freeWalk)
        {
            //if transform is not close enough to facing target direction, move body in direction of thumbstick and let head rotation catch up
            float angleRot = Vector3.Angle(transform.forward, lookDir);
            //Debug.Log(angleRot);
            if (angleRot < 30)
            { 
                thisLook = (transform.forward);
                blockNewStep = false;
            }
        }
        //stop move if changing cell height
        if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell)
            blockNewStep = true;

        bool glideWalk = false; //keeping for idea/ice/slide attack..

        if (glideWalk)
        {
            Glide();
        }

        if (bumped)
        {
            //set bump target if we are in control of this player
          //  if(thisPhotonView.IsMine)
          if(!bumpInProgress)
                BumpTarget();

           // LerpBump(); //moved to update
            
        }
        
        if (!walking && !bumped && GetComponent<PhotonView>().IsMine)//only work out new target on local client - otherwise the target is sent over the network already worked out
        {
            WalkTarget(blockNewStep, thisLook);
           

        }
        else if (walking)
        {
           // LerpPlayer();//moved to update
        }
           
        
    }

    void SendWalkTargetToNetwork()
    {      

        byte evCode = 21; // Custom Event 21: Used to update player walk targets
        //enter the data we need in to an object array to send over the network
        int photonViewID = GetComponent<PhotonView>().ViewID;

        object[] content = new object[] {walkStartPos, walkStart, walkTarget,walkSpeedThisFrame, photonViewID };
        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        //keep resending until server receives
        SendOptions sendOptions = new SendOptions {Reliability = true};
        
        
        
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

    }

    void SendBumpTargetToNetwork()
    {

        //now, if we are master tell the clients where the actual bump target is.
        if (PhotonNetwork.IsMasterClient)//should be after bump target set in master client?
        {
            Debug.Log("[MASTER] - sending bump info to clients");
            byte evCode = 22; // Custom Event 21: Used to update player walk targets
                              //enter the data we need in to an object array to send over the network
            int thisPhotonViewID = GetComponent<PhotonView>().ViewID;

            
            object[] content = new object[] { thisPhotonViewID,bumpStart, bumpStartPos, bumpTarget};
            //send to everyone but this client
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

            //keep resending until server receives
            SendOptions sendOptions = new SendOptions { Reliability = true };

            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
        }
    }
   
    void SendInputsToNetwork()
    {
      //  if (!PhotonNetwork.IsMasterClient) ///**** testing. otherwise master wont get rotation info otherwise?
        {
            
            byte evCode = 30; // Custom Event 30: client inputs
                              
            int thisPhotonViewID = GetComponent<PhotonView>().ViewID;

            object[] content = new object[] { thisPhotonViewID, lookDir, pA.lookDirRightStick };
            //send to everyone but this client
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

            //send every fixed update
            SendOptions sendOptions = new SendOptions { Reliability = false };

            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
        }
        
    }

    void BumpTarget()
    {
        walking = false;
        Debug.Log("setting bump target " + GetComponent<PlayerInfo>().playerNumber);
        // Debug.Break();
        if (!bumpInProgress)
        {
            Debug.Log("if not bump in progress");

            /*  //applied from network or from bump collision script
            fracComplete = 0f;
         
            */
            RaycastHit hit;

            float maxBumpDistance = 10f;
            //what to use for radius? - review if gettin no hits
            float add = 0f;
            bool bumpTargetFound = false;
            while (bumpTargetFound == false)
            {
                Vector3 bumpDirection = (bumpTarget - transform.position).normalized;
                Vector3 shootFrom = bumpShootfrom + (bumpDirection * (add));
                if (Physics.SphereCast(shootFrom + Vector3.up * 50f, walkStepDistance * .5f, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells", "Wall")))
                {

                    //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    ////   c.transform.position = hit.point;
                    //  c.transform.parent = transform;

                    //stop at edge
                    if (hit.point.y - transform.position.y <= playerClassValues.maxClimbHeight * overlayDrawer.heightMultiplier)//testing, not checked
                    {
                        bumpTarget = hit.point;
                        bumpInProgress = true;
                        bumpTargetFound = true;

                       // Debug.Break();
                        Debug.DrawLine(transform.position + Vector3.up * 10, transform.position, Color.blue);
                        Debug.DrawLine(bumpTarget + Vector3.up * 10, bumpTarget, Color.red);


                        bumpStart = PhotonNetwork.Time;
                        bumpStartPos = transform.position;

                        //send results to everyone//only if master client
                        SendBumpTargetToNetwork();


                        //  Debug.Log("setting target for bump");
                    }
                    else
                    {
                        //already at a wall, cancel bump
                        bumped = false;
                    }
                }


                else if (add >= maxBumpDistance)
                {
                    //missed, will fall in to hole//??

                    //already at edge
                    bumped = false;

                    return;
                }


                add += 0.1f;//accuracy? opto


            }
        }
    }

    void LerpBump()
    {
        float bumpDistance = (bumpStartPos - bumpTarget).magnitude;
        //add for arc //half way for arc loop for jumping animation
        Vector3 bumpCenter = Vector3.Lerp(bumpStartPos, bumpTarget, 0.5f);// (transform.position + (transform.position + lookDir * walkAmount)) * 0.5F;///**    

        bumpCenter += new Vector3(0, -bumpDistance / bumpBounceAmount, 0);

        //from unity slerp docs
        Vector3 riseRelCenterBump = bumpStartPos - bumpCenter;
        Vector3 setRelCenterBump = bumpTarget - bumpCenter;

        //bumpspeed this step

        fracComplete = (float) ((PhotonNetwork.Time - bumpStart) / (bumpDistance / bumpSpeed));
        //Debug.Log(fracComplete);
        /*
        Debug.Log("risRelCenterBump = " + riseRelCenterBump);
        Debug.Log("setRelCenterBump = " + setRelCenterBump);
        Debug.Log("fracComplete = " + fracComplete);
        */
        transform.position = Vector3.Slerp(riseRelCenterBump, setRelCenterBump, fracComplete);
        transform.position += bumpCenter;
        //transform.position = Vector3.Slerp(bumpStartPos, bumpTarget, fracComplete);


        if (fracComplete >= 1f)
        {
            //force the player to flick the tick again
            //if (leftStickReset)
            {
                bumped = false;
                bumpInProgress = false;
                fracComplete = 0f;

            }
        }

        Debug.DrawLine(bumpStartPos, bumpTarget);
        return;
    }

    void WalkTarget(bool blockNewStep,Vector3 thisLook)
    {
        float maxJumpDistance = 10f;

        if (!blockNewStep)
        {
            if (leftStickMagnitude > 1f)
                leftStickMagnitude = 1f;//can be over by a small amount

            if (!leftStickReset)
            {
                walkSpeedThisFrame = walkSpeed * leftStickMagnitude;

                //remember what the magnitude was
                //  leftStickMagnitudeForStep = leftStickMagnitude;//instead of doing this we could set start pos and end pos for slerp here?


                //speed

                //blocking 

                //if (inputs.blocking0 && inputs.blocking1) //second block not in
                //{
                //    walkSpeedThisFrame = walkSpeedWhileBlockingOverhead * leftStickMagnitude;
                // }
                //else 
                if (pA.blocking)
                {
                    walkSpeedThisFrame = walkSpeedWhileBlocking * leftStickMagnitude;
                }
                else if (swipe.planningPhaseOverheadSwipe || swipe.pulledBackForOverhead)
                {
                    walkSpeedThisFrame = walkSpeedWhilePullBack * leftStickMagnitude;
                }
                

                //
                //else
                if (swipe.overheadSwiping)
                {
                    //set when swipe object is started
                }
                //sprint
                /*
                else if (inputs.state.Buttons.A == XInputDotNetPure.ButtonState.Pressed)
                {
                    //sprinting
                    walkSpeedThisFrame = sprintSpeed * leftStickMagnitude;
                    sprinting = true;
                }
                */
                
                //step size

                //apply stick amount 
                float walkStepDistanceThisFrame = walkStepDistance * leftStickMagnitude;


                //change step size if blocking
                // if (inputs.blocking0 && inputs.blocking1)
                //   walkStepDistanceThisFrame = shieldStepDistanceOverhead * leftStickMagnitude;

                //else 
                if (pA.blocking)
                    walkStepDistanceThisFrame = shieldStepDistance * leftStickMagnitude;

                
                //also if holding swing
                //else 
                if (swipe.planningPhaseOverheadSwipe || swipe.pulledBackForOverhead)
                    walkStepDistanceThisFrame = shieldStepDistance * leftStickMagnitude;  //need var?  using shield 

                /*
                else if (swipe.overheadSwiping)
                {
                    //by not setting anythin here we keep the large arc for the swipe, looks cool basically. Makes player float
                }
                else if (inputs.state.Buttons.A == XInputDotNetPure.ButtonState.Pressed)
                {
                    walkStepDistanceThisFrame = sprintStepDistance * leftStickMagnitude;
                }
                */

                //start timer to use with calculating how far we ahve travelled
                walkStart = PhotonNetwork.Time;// Time.time;
                //set flag
                walking = true;
                //remember where we started
                walkStartPos = transform.position;
                //add stick direction to player position

                RaycastHit hit2;
                bool targetFound = false;
                float add = 0;
                while (targetFound == false)
                {


                    Vector3 shootFrom = transform.position + (thisLook * (walkStepDistanceThisFrame + add));

                    if (Physics.SphereCast(shootFrom + Vector3.up * 50f, walkStepDistance * .5f, Vector3.down, out hit2, 100f, LayerMask.GetMask("Cells", "Wall")))//,"PlayerBody")))
                    {

                        //  Debug.Log("tr pos = " + transform.position.y);
                        //  Debug.Log("hp pos = " + hit.point.y);
                        // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //  c.transform.position = hit.point;
                        //  Debug.Log("sum = " + (hit.point.y - transform.position.y).ToString());
                        if (hit2.point.y - transform.position.y <= playerClassValues.maxClimbHeight * overlayDrawer.heightMultiplier)
                        {
                            walkTarget = hit2.point;
                        }
                        else
                        {
                            //cancel walk 
                            walking = false;
                            Debug.Log("no move");
                            walkTarget = transform.position;
                        }

                        // if ( transform.position.y - hit.point.y> 10)
                        //     debug = true;

                        targetFound = true;

                        //update other clients with this new walk target
                        SendWalkTargetToNetwork();
                    }

                    else if (add >= maxJumpDistance)
                    {
                        //missed, will fall in to hole//??
                        //walkTarget = transform.position;
                        walking = false;
                        break;
                    }


                    add += 0.1f;//accuracy? opto
                }
            }
        }

    }

    void LerpPlayer()
    {
            //now move player
            //this fraction makes it take longer for longer distances - gameplay option, make it take the same length of time for each jump?

            //how far we have come

            float walkDistance = (transform.position - walkTarget).magnitude;
            //add for arc //half way for arc loop for jumping animation
            Vector3 walkCenter = Vector3.Lerp(walkStartPos, walkTarget, 0.5f);// (transform.position + (transform.position + lookDir * walkAmount)) * 0.5F;///**    
            walkCenter += new Vector3(0, -walkDistance / walkBounceAmount, 0);

            //from unity slerp docs
            Vector3 riseRelCenterWalk = walkStartPos - walkCenter;
            Vector3 setRelCenterWalk = walkTarget - walkCenter;

            bool bigDrop = false;
            if (walkStartPos.y - walkTarget.y > playerClassValues.maxClimbHeight)
            {

                bigDrop = true;
            }

            float tempWalkSpeed = walkSpeedThisFrame;


            if (bigDrop)
            {
                tempWalkSpeed *= 2;
            }

            // float fracComplete = (Time.time - walkStart) / (1f / 4); //game play option *** same time for each cell jump

            fracComplete = (float) ((PhotonNetwork.Time - walkStart) / (walkDistance / tempWalkSpeed));

            float fracCompleteForLerp = fracComplete;

            //disgusting hack for big jumps            
            if (bigDrop)
            {
                fracCompleteForLerp *= 0.5f;

            }

            transform.position = Vector3.Slerp(riseRelCenterWalk, setRelCenterWalk, fracCompleteForLerp);
            transform.position += walkCenter;


            //finish drop
            if (bigDrop)
            {

                if (fracComplete > 0.5f)
                {
                    //last point of arc for this jump
                    Vector3 dropStartPos = Vector3.Slerp(riseRelCenterWalk, setRelCenterWalk, 0.5f) + walkCenter;

                    Vector3 shootFrom = transform.position;
                    RaycastHit hit;
                    if (Physics.SphereCast(shootFrom + Vector3.up * 50f, walkStepDistance * .5f, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells")))
                    {
                        transform.position = Vector3.Lerp(dropStartPos, hit.point, (fracComplete - .5f) * 2);
                    }
                }
            }


            if (fracComplete >= 1f)
            {
                //force the player to flick the tick again
                //if (leftStickReset)
                {
                    walking = false;

                    //tell sound script to make a noise now we have finished our step

                    GetComponent<PlayerSounds>().startWalk = true;
                }
            }
        }

    void Glide()
    {

        Vector3 right = x * Camera.main.transform.parent.right;
        Vector3 forward = y * Camera.main.transform.parent.forward;
        //public var so helper class can access
        lookDir = right - forward;
        if (!leftStickReset)
        {
            //check to see if in cell
            //*** can't slide along each of cell, seems like to much friction (obviously not this)- could add rigid body and inver cell walls? trans
            RaycastHit hit;
            float distanceFromEdge = 1f;
            Vector3 start = transform.position + Vector3.up * distanceFromEdge + lookDir * distanceFromEdge;
            if (Physics.Raycast(start, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells")))
            {
                //it can hit a cell across the gap, make sure it is nly hitting current cell
                if (hit.transform.gameObject == GetComponent<PlayerInfo>().currentCell)
                {
                    transform.position += right * movementSpeed;
                    transform.position -= forward * movementSpeed;
                }
            }
        }
    }
}

