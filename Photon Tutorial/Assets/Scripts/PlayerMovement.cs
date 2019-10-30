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

   // public float bumpSpeed = 3f;
   // public float bumpBounceAmount = 10f;
    public float walkSpeed = 5f;
    public float walkStepDistance = 5f;
    public float walkBounceAmount = 5f;
    public float walkSpeedThisFrame = 0f;
    public float walkSpeedWhileAttacking = 5f;
    public float walkSpeedWhilePullBack = 5f;
    public float pullBackStepDistance = 5f;
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


    //bump
    public Vector3 bumpShootfrom;
    public Vector3 bumpTarget;
    public double bumpStart;
    public Vector3 bumpStartPos;
    public bool waitingForBumpReset = false;
    public double bumpFinishTime;
    public int lastPLayerIdCollision = -1;//-1 is no team, teams start at 0

    //wall
    public bool onWall = false;

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
    public float attackLerp = 0f;

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

        // needed for swipe height if swipe is used instantly
        walkTarget = transform.position;

       // playerAttacks = GetComponent<PlayerAttacks>();
        //only control our own player - the network will move the rest
        if (!GetComponent<PhotonView>().IsMine)// && PhotonNetwork.IsConnected == true)
        {

            return;
        }

        
        
       
        codeObject = GameObject.FindGameObjectWithTag("Code");
        
      
        
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
        Inertias();//look over this again?


        //player movement
        Movement();


        //section name needed
        //

        //heights for head
        HeadHeight();
        

        //rotations
        //

    

        //rotations for head
        if (GetComponent<PlayerAttacks>().blocking)//test blocking- if blocking, should be stuck in animation -ok it seems)
        {
            //rotations are done in player attacks in Block()
        }
        else if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell || swipe.attackedTooClose || waitingForBumpReset)
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
        return;
        //movement, targets worked out in fixed update
        if (bumpInProgress)
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

        if(walking && bumped)
        {
            Debug.Log("walking and bumped");
            Debug.Break();
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

    void Movement()
    {
        //check if we have been bumped and waitin for cooldown to finish- cant moveif this is happening
        if (waitingForBumpReset)
        {
            //  Debug.Log("waiting for bump reset");
            //we are in bump recovery mode, player cant move for a set amount of time, check if we have completed this wait
            if (PhotonNetwork.Time - bumpFinishTime > playerClassValues.playerCooldownAfterBump)
            {
                // Debug.Log("bump reset");

                Debug.Log("bump reset, walking = " + walking);
                waitingForBumpReset = false;

            }
            else
            {
                //just checkin this if bumped and some one else is adjusting cell height - will keep player at correct height
                RaycastForHeight();

                return;
            }
        }
        

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
        //stop move if changing cell height or attacking
        if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell || swipe.overheadSwiping) //block new step is only used sometimes to block new step, other times just if statements.. two methods, bad
            blockNewStep = true;

        bool glideWalk = false; //keeping for idea/ice/slide attack..

        if (glideWalk)
        {
            Glide();
        }

        if (bumped)
        {
            //Debug.Break();
            //set bump target if we are in control of this player
            //  if(thisPhotonView.IsMine)
            //if(PhotonNetwork.IsMasterClient)//only work out targets on master/predictive now
                if (!bumpInProgress)
                {
                   // Debug.Log("Bump in progress");
                    BumpTarget();
                }

            LerpBump(); //moved to update
            
        }        
        else if (!walking && !bumped && !waitingForBumpReset) // if not walking or bumped
        {
            //check cells ahvent been moved up or down
            RaycastForHeight();
            //Debug.Log("here");

            if (GetComponent<PhotonView>().IsMine)//only work out new target on local client - otherwise the target is sent over the network already worked out
            {
                //check for input
                if (lookDir.magnitude >= deadzone)
                {
                    if (photonView.IsMine)
                        Debug.Log("setting walk target, walking = " + walking);


                    WalkTarget(blockNewStep, thisLook);
                }
            }
            
            
        }
        else if (walking)
        {
            LerpPlayer();//moved to update
        }
           
        
    }

    void RaycastForHeight()
    {
        //move player up - could prob work this out fine with maffs

        Vector3 shootFrom2 = transform.position;
        RaycastHit hit;

        if (Physics.SphereCast(shootFrom2 + Vector3.up * 50f, pA.head.transform.localScale.x * 0.5f, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells", "Wall")))
        {
           // Debug.Log("hitting");
            Debug.DrawLine(hit.point, hit.point - Vector3.up * 10);
            transform.position = hit.point;

            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Wall"))
                onWall = true;
            else
                onWall = false;

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

    void SendEdgeBumpToNetwork()
    {

        //now, if we are master tell the clients where the actual bump target is.
        if (PhotonNetwork.IsMasterClient)//should be after bump target set in master client?
        {
            Debug.Log("[MASTER] - sending edge bump info to clients");
            byte evCode = 24; // Custom Event 24:

            int thisPhotonViewID = GetComponent<PhotonView>().ViewID;

            object[] content = new object[] { thisPhotonViewID, bumpFinishTime };
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
        //Debug.Log("setting bump target " + GetComponent<PlayerInfo>().teamNumber);
        // Debug.Break();
        if (!bumpInProgress)
        {
            Debug.Log("if not bump in progress");

            /*  //applied from network or from bump collision script
            fracComplete = 0f;
         
            */
            RaycastHit hit;

            
            //what to use for radius? - review if gettin no hits
            float add = 0f;
            bool bumpTargetFound = false;
            while (bumpTargetFound == false)
            {
                Vector3 bumpDirection = (transform.position - bumpShootfrom).normalized;
                Vector3 shootFrom = bumpShootfrom + (bumpDirection * (add));
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = shootFrom;
                c.name = "shoot from";
                Destroy(c, 3);
                if (Physics.SphereCast(shootFrom + Vector3.up * 50f, head.transform.localScale.x, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells", "Wall")))
                {

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = hit.point;                    
                    Destroy(c, 3);
                    c.name = "hit.point";

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

                        return;

                        //  Debug.Log("setting target for bump");
                    }
                }
                //if target for bump is close enough to player position, consider this to be at an edge
                else if (add >= 10)//??
                {
                    //this shouldn't get here? at the very least a hit should be registered underneath the feet of the player
                    //missed, will fall in to hole//??
                    Debug.Log("Bump at edge - PROBLEM?");
                    Debug.Break();
                    return;
                }


                add += 0.1f;//accuracy? opto


            }
        }
    }

    void LerpBump()
    {
        walking = false;//worried about master overwriting client with predictive walk

        float bumpDistance = (bumpStartPos - bumpTarget).magnitude;
        if(bumpDistance == 0)
        {
            //if we are already at abump target, happens when at edge, cancel bump
            bumped = false;
            bumpInProgress = false;
            return;
        }

        //add for arc //half way for arc loop for jumping animation
        Vector3 bumpCenter = Vector3.Lerp(bumpStartPos, bumpTarget, 0.5f);// (transform.position + (transform.position + lookDir * walkAmount)) * 0.5F;///**    

        bumpCenter += new Vector3(0, -bumpDistance /playerClassValues.bumpBounceAmount, 0);

        //from unity slerp docs
        Vector3 riseRelCenterBump = bumpStartPos - bumpCenter;
        Vector3 setRelCenterBump = bumpTarget - bumpCenter;

        //bumpspeed this step
       // Debug.Log("bump distance = " + bumpDistance);
        float fracCompleteBump = (float) ((PhotonNetwork.Time - bumpStart) / (bumpDistance /playerClassValues.bumpSpeed));
        
        //Debug.Log(fracComplete);
        /*
        Debug.Log("risRelCenterBump = " + riseRelCenterBump);
        Debug.Log("setRelCenterBump = " + setRelCenterBump);
        Debug.Log("fracComplete = " + fracComplete);
        */
        Vector3 target = Vector3.Slerp(riseRelCenterBump, setRelCenterBump, fracCompleteBump) + bumpCenter;
        
        

        //smooth if client //testing
        
        if (PhotonNetwork.IsMasterClient)
            transform.position = target;
        else
            transform.position = Vector3.Lerp(transform.position, target, playerClassValues.clientMovementLerp);


        if (fracCompleteBump >= 1f)
        {
            //force the player to flick the tick again
            //if (leftStickReset)
            {
                bumped = false;
                bumpInProgress = false;
                fracCompleteBump = 0f;
                waitingForBumpReset = true;
                bumpFinishTime = PhotonNetwork.Time;

                lastPLayerIdCollision = -1;

                //snap if client
                transform.position = target;

            }
        }

        Debug.DrawLine(bumpStartPos, bumpTarget);
        return;
    }

    void WalkTarget(bool blockNewStep,Vector3 thisLook)
    {
        Debug.Log("setting walk target");
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
                    walkStepDistanceThisFrame = pullBackStepDistance * leftStickMagnitude;  //need var?  using shield 

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
                        Debug.Log("max jump distance > ");
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
            if (walkStartPos.y - walkTarget.y > playerClassValues.maxClimbHeight*20)//**wasnt *20 //check
            {

                bigDrop = true;
            }

            float tempWalkSpeed = walkSpeedThisFrame;


            if (bigDrop)
            {
                tempWalkSpeed *= 2;
            }

        // float fracComplete = (Time.time - walkStart) / (1f / 4); //game play option *** same time for each cell jump

        //use a seperate lerp when attacking - this is worked out from how far along the swipe has made it  -we work this out on swipeobject and pass it to attackLerp
        //swipe object can take a few frames to update as it is calculated on a fixed update and we lerping here on render Update, so we need to do another check to see if swipe object has started yet
        bool useAttackLerp = false;
        //only need to do this if attacking 
        
        if(swipe.currentSwipeObject != null)
            if (swipe.overheadSwiping && swipe.currentSwipeObject.GetComponent<SwipeObject>().arrayRenderCount != 0)
                useAttackLerp = true;
        
        if(!useAttackLerp)
        {
            //otherwise use normal walk speed lerp
            fracComplete = (float)((PhotonNetwork.Time - walkStart) / (walkDistance / tempWalkSpeed));
            //Debug.Log("frac complete (normal walk) = " + fracComplete);
        }
        

        float fracCompleteForLerp = fracComplete;
        
       

        if (useAttackLerp)
        {
            //work out how much of the jump we still ahve to do            
            //starting amount (frac complete + the percentage of what we have left to jump)
            float whatsLeft = 1f - fracComplete;
            fracCompleteForLerp = fracComplete + (whatsLeft * attackLerp); 
            Debug.Log("frac complete swiping B = " + fracComplete);
            Debug.Log("for lerp = " + fracCompleteForLerp);

           // Debug.Break();
        }

        //disgusting hack for big jumps            
        if (bigDrop)
        {
            fracCompleteForLerp *= 0.5f;

        }

        //smooth if client //testing
        Vector3 target = Vector3.Slerp(riseRelCenterWalk, setRelCenterWalk, fracCompleteForLerp) + walkCenter;
        if (PhotonNetwork.IsMasterClient)
            transform.position = target;
        else
            transform.position = Vector3.Lerp(transform.position, target, playerClassValues.clientMovementLerp);
        


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
               // Debug.Log("fraccomplete > 1");
                walking = false;

                //tell sound script to make a noise now we have finished our step

                GetComponent<PlayerSounds>().startWalk = true;

                //reset fraccomplete too
                fracComplete = 0f;

                //snap if client
                transform.position = target;
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

