using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerAttacks : MonoBehaviour {

    private Spawner spawner;
    public Inputs inputs;

    public bool autoShield = false;

    
    public float deadzone = 0.4f;//make global for other stick too?
    public bool rightStickReset = false;

    public float x;
    public float y;
    public bool playerMoving = false;
    public Vector3 lookDirRightStick;
    public Vector3 previousLookDirectionRightStick;
    //head (shield)
    public float headRotationSpeed = 0.25f;
    public float headRotationSpeedWhenBumping = 0.25f;//keeping same speed atm. confusing - loook at if shield is op?
    public float whiffDuckSpeed = 0.5f;


    //shield
    public float shieldActiveLength = 0.5f;
    public float shieldActiveStart = 0f;
    
    public float shieldRotationSpeed = 0.2f;
    public float shieldScaleSpeed = 0.2f;
    public float duckSpeed = 0.1f;

    public Swipe swipe;
    PlayerClassValues playerClassValues;
    public bool previousRightStickReset = false;
    public List<GameObject> currentAdjacents = new List<GameObject>();

    public bool skipFrame = true;
   // public List<GameObject> playerGlobalList;
    public GameObject targetCellRightStick;
    //public Spawner spawner;
    public Vector3 stabberStartLocalPos;
    public Vector3 currentTargetCentroid;
    
    public bool stabTargetSet = false;
    
    public GameObject head;
    public Vector3 headOriginalPos;
    GameObject stabber;
   // GameObject shield;

   //block/shield
    GameObject shieldPivot;
    GameObject shield;
    public bool blocking;
    public bool blockRaising = false;
    public bool blockLowering = true;//starts true so shield gets minimised
    public bool blockLowered = false;
 
 //   public bool blockStartTimeSetRaise = false;
  //  public bool blockStartTimeSetLower = false;//we can have this set at the start, the prefab will always start with shield down
    public bool blocking1UpdatedPressed = false;
    public bool blocking1UpdatedReleased = false;
    public double blockStartTime;
    public double blockRotationTime;
   
    public Quaternion shieldStartingRotation;
    public Quaternion headStartingRotationOnBlock;
    public Quaternion headTargetRotationOnBlock;
    

    public Vector3 shieldScaleOnButtonPress;
    public Vector3 headStartPos;
    float shieldStartingScaleX;
    float shieldStartingScaleY;
    float shieldStartingScaleZ;

    // GameObject swiper;
    // Use this for initialization
    public Vector3[] startStabberVertices;
    public Vector3[] endStabberVertices = new Vector3[0];

    // private List<Vector3> swipePoints = new List<Vector3>();

    public float RSMagnitude;

    private void Awake()
    {
       // enabled = false;
    }
    void Start ()
    {

        
        inputs = GetComponent<Inputs>();
        swipe = gameObject.GetComponent<Swipe>();
        
        
      //  playerGlobalList = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList;
        spawner= GameObject.FindGameObjectWithTag("Code").GetComponent<Spawner>();
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();



        head = transform.Find("Head").gameObject;
        headOriginalPos = head.transform.localPosition;
        //stabber = head.transform.GetChild(0).gameObject;
        shieldPivot = head.transform.Find("ShieldPivot").gameObject;
        shield = shieldPivot.transform.GetChild(0).gameObject;
        shieldStartingScaleX = shield.transform.localScale.x;
        shieldStartingScaleY = shield.transform.localScale.y;
        shieldStartingScaleZ = shield.transform.localScale.z;

        
        //swiper = transform.Find("Swiper").gameObject;
        //stabberStartLocalPos = stabber.transform.localPosition;

        //startStabberVertices = stabber.GetComponent<MeshFilter>().mesh.vertices;
    }


    // Update is called once per frame
    private void FixedUpdate()
    {
        if (skipFrame)
        {
            skipFrame = false;
            return;
        }
        if (GetComponent<PhotonView>().IsMine)
        {
            GetInputs();

            CalculateRightStickReset();
            //check for any attack with the right thumbstick
            swipe.SwipeOrder();
        }
        else
        {
            //we will wait on network event codes to update shield use
            CalculateRightStickReset();
        }


        //check for any attacks and control them if they are attacking
        
        
        

        //can block if not attacking, or changing cell height
        if(!swipe.overheadSwiping && (!GetComponent<CellHeights>().loweringCell || !GetComponent<CellHeights>().raisingCell))
            Block();
    }

    private void Update()
    {
        //move shield etc if values allow
        BlockLerp();
    }



    void SendShieldToNetwork()
    {

        
        //only send updates on shield if we are controlling this player
        if (!GetComponent<PhotonView>().IsMine)
            return;

        byte evCode = 23; // Custom Event 23 - shield up
        //enter the data we need in to an object array to send over the network
        int photonViewID = GetComponent<PhotonView>().ViewID;
        //sending ID, and starting time, rotations ,scale of head and shield
        object[] content = new object[] { photonViewID, blocking, blockRaising, blockLowering,blockLowered, shieldStartingRotation, shieldScaleOnButtonPress, headStartingRotationOnBlock, headTargetRotationOnBlock, headStartPos,blockStartTime,blockRotationTime };
        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        //keep resending until server receives
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

    }   

    void GetInputs()
    {

        //remember last frame
        previousLookDirectionRightStick = lookDirRightStick;


        x = inputs.state.ThumbSticks.Right.X;
        y = -inputs.state.ThumbSticks.Right.Y;//inverted

        //my pad is broken so giving option to use triggers instead -- will need to fix for xinput


        //xInput test
        // x = inputs.rightStickAxisX;
        // y = inputs.rightStickAxisY;
        //  y = -y;


        //using xInput 

        Vector3 right = x * Camera.main.transform.parent.right;
        Vector3 forward = y * Camera.main.transform.parent.forward;

        lookDirRightStick = right - forward;

        //use for comparing states
        previousRightStickReset = rightStickReset;

        //RSMagnitude = new Vector2(x, y).magnitude;
        //Debug.Log("RS playerAttacks " + RSMagnitude);

        

        playerMoving = GetComponent<PlayerMovement>().moving;




        //block

        //create a magnitude which doesn't consider camera angle, more precise, tells us exactly what the user put in the stick

    }

    void CalculateRightStickReset()
    {
        RSMagnitude = lookDirRightStick.magnitude;
        if (RSMagnitude < deadzone)
        {
            rightStickReset = true;
        }
        else
            rightStickReset = false;
    }
    
    void Block()
    {
        //place shield in front of player if block button pushed
        //rotate shield high by pushin stick towards?

        Vector3 scale = (new Vector3(1f,1f,0f) * shieldScaleSpeed);//z value stays at shield thickness//also need to put this from time start?

        if (autoShield)
            inputs.blocking0 = true;

        //if we control this player, we can update any block events
        if (GetComponent<PhotonView>().IsMine)
        {
            //update locally
            BlockTargets();
           
        }

        


    }

    void BlockTargets()
    {
        //set locally or updated from network

        // only update if client is ours, else we will update from event code
        //player needs to input block button and direction to start a block action, can't set new block until old block is finished
        //if (inputs.blocking0  && !blocking)
        if (inputs.blocking0)
        {
            
            bool changedThisFrame = false;
            if (!blockRaising)
            {
                //start timer for rotation of shield to forward position
                blocking = true;                
                blockRaising = true;
                blockLowered = false;
                blockLowering = false;

                //flag for sending to network
                changedThisFrame = true;

                blockStartTime = PhotonNetwork.Time;                
                //not starting shield scale- current shield scale
                shieldScaleOnButtonPress = shield.transform.localScale;
                //lift rotation start/current
                shieldStartingRotation = shieldPivot.transform.localRotation;

               
            }
            
            //set time for this rotation update
            blockRotationTime = PhotonNetwork.Time;
            
            

            //set start and end rotation targets
            headStartingRotationOnBlock = head.transform.rotation;

            //if no right stick input, point shield in the direction the player is facing
            //else face towards the stick direction
            //lerp now so instant snap doesnt happen

            ////**** send final destination to network, don't limit it. Only limit on player client - use same lerp value for client 


            if (lookDirRightStick.magnitude > deadzone)
                headTargetRotationOnBlock = Quaternion.LookRotation(lookDirRightStick);// Quaternion.Lerp(headStartingRotationOnBlock, Quaternion.LookRotation(lookDirRightStick), playerClassValues.blockRotation);
            else
                headTargetRotationOnBlock = Quaternion.LookRotation(transform.forward);// Quaternion.Lerp(headStartingRotationOnBlock, Quaternion.LookRotation(transform.forward), playerClassValues.blockRotation);

            

            headStartPos = head.transform.localPosition;






            //send targets to network
            //only send on change of right stick or button press
            if (lookDirRightStick != previousLookDirectionRightStick || changedThisFrame)
            {
                Debug.Log("[CLIENT] - Setting shield targets and sending to network");
                SendShieldToNetwork();
            }
            

        }
        //if timer has run its course we can reset the block, we can keep block input down if we wish to hold on for longer
        else if (!inputs.blocking0 )//)//(lookDirRightStick.magnitude < deadzone ||
        {   
            //start unwind animation
            blockRaising = false;
            //reset any raise flags
            //blockStartTimeSetRaise = false;

            //targets, only set if we control this player
            if (GetComponent<PhotonView>().IsMine)
            {
                //reset raise flag
             //   blockStartTimeSetRaise = false;

                if (!blockLowering && !blockLowered)//resewt when lerp is 1
                {
                    Debug.Log("unblocking");
                    blockLowering = true;
                    blockStartTime = PhotonNetwork.Time;
                    //blockStartTimeSetLower = true;
                    shieldStartingRotation = shieldPivot.transform.localRotation;
                    shieldScaleOnButtonPress = shield.transform.localScale;
                    headStartingRotationOnBlock = head.transform.rotation;
                    headStartPos = head.transform.localPosition;

                    Debug.Log("[CLIENT] - lowering shield and sending to network");

                    SendShieldToNetwork();

                }
            }
        }
    }

    void BlockLerp()
    {

        //moves depending on values set in BlockTargets

        if (blockRaising)
        {
            //player rotation
            //shield rotation //lifting shield up
            float lerpFromStart = (float)((PhotonNetwork.Time - blockStartTime) / playerClassValues.blockRaise);


            //scale - use same lerp as lift rotation, the higher the rotation, the bigger the scale
            shield.transform.localScale = Vector3.Lerp(shieldScaleOnButtonPress, new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ), lerpFromStart);
            //rotae shield to face forward

            Vector3 lookUpAndFwd = Vector3.forward + Vector3.up * (.33f / 2);
            Quaternion targetRotShield = Quaternion.LookRotation(lookUpAndFwd);

            shieldPivot.transform.localRotation = Quaternion.Lerp(shieldStartingRotation, targetRotShield, lerpFromStart);

            //can put easings function here?
            //lerpT = Easings.CubicEaseIn(lerpT);//slow start and straight end
            //new
            //target rotation was set when player initiated block

            //add upwards look to stick direction
            

            //consider if blocking1 target rot change?
            //shield rotation - facing right stick
            //different if local player
            if(GetComponent<PhotonView>().IsMine)
            {
                
                head.transform.rotation =  Quaternion.Slerp(headStartingRotationOnBlock, headTargetRotationOnBlock, playerClassValues.blockRotation);
            }
            else
            {
                float lerpUpdate = (float)((PhotonNetwork.Time - blockRotationTime) / playerClassValues.blockRotation);
                //work out where the head should be - we will lerp to this
                Quaternion headRotateClientTarget = Quaternion.Slerp(headStartingRotationOnBlock, headTargetRotationOnBlock, lerpUpdate);
                //now slerp to target, do this to stop any teleporting - so the first part of the rotation will probably swing faster to catch up with rotation that was happening when the message was being sent over the net- but its better than a jump/teleport
                head.transform.rotation = Quaternion.Slerp(head.transform.rotation, headRotateClientTarget, playerClassValues.blockRotationNetworkLerp);


            }
            //testing - 
            
            //if(!PhotonNetwork.IsMasterClient)
              //  Debug.Log("lerp update = " + lerpUpdate);

            //head.transform.rotation = Quaternion.Lerp(headStartingRotationOnBlock, headTargetRotationOnBlock, lerpUpdate);
            

            // headTargetRotationOnBlock = Quaternion.Lerp(head.transform.rotation, Quaternion.LookRotation(lookDirRightStick), playerClassValues.blockRotation);



            //head position
            //move cube forward and down making it look like it is bracing itself or leaning with a knee
            // Vector3 targetForHead = headOriginalPos + head.transform.localScale.x * .66f * head.transform.forward - head.transform.localScale.x * .66f * Vector3.up;
            // head.transform.localPosition = Vector3.Lerp(headStartPos, targetForHead, lerpT);

            //shield rotation - facing right stick
            // lerpT = (float)((PhotonNetwork.Time - blockRotationTime) / playerClassValues.blockRaise);
            //can put easings function here?
            //  lerpT = Easings.CubicEaseIn(lerpT);//slow start and straight end


            //add look up to rotation //lerped?
            //   Vector3 lookUpAndFwd = Vector3.forward + Vector3.up * (.33f / 2);
            //   targetRot = Quaternion.LookRotation(lookUpAndFwd);

            //consider if blocking1 target rot change





        }
        else if (blockLowering && !blockLowered)
        {
            
            //player rotation

            float lerpFromStart = (float)((PhotonNetwork.Time - blockStartTime) / playerClassValues.blockLower);
         //   if(!PhotonNetwork.IsMasterClient)
         //       Debug.Log(lerpFromStart);
            //can put easings function here?
            //lerpT = Easings.CubicEaseOut(lerpT);//slow start and straight end
            //new
            //target rotation - face forward to match transform forward
            //start where we finished blcoking towards
           // Quaternion startingRot = Quaternion.LookRotation(headTargetDirectionOnBlock + (Vector3.up * .33f / 2));
            //Quaternion targetRot = Quaternion.LookRotation(transform.forward);
            //consider if blocking1 target rot change?
            //head.transform.rotation = Quaternion.Lerp(startingRot, targetRot, lerpT);

            Vector3 targetForHead = headOriginalPos;
          //  head.transform.localPosition = Vector3.Lerp(headStartPos, targetForHead, lerpT);

            //shield rotation
            // lerpT = (float)((PhotonNetwork.Time - blockStartTimeLower) / playerClassValues.blockLower);
            //can put easings function here?
            ///// lerpT = Easings.CubicEaseIn(lerpT);//slow start and straight end

            
            //scale

            shield.transform.localScale = Vector3.Lerp(new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ), Vector3.zero, lerpFromStart);

            Quaternion targetRot = Quaternion.LookRotation(Vector3.down);
            //consider if blocking1 target rot change
            shieldPivot.transform.localRotation = Quaternion.Lerp(shieldStartingRotation, targetRot, lerpFromStart);

           if(lerpFromStart >= 1f)
            {
                //once animation has completed, reset flags
                blockLowering = false;
                blockLowered = true;
                blocking = false;
                //blockStartTimeSetLower = false;
               
            }
        }
    }


    public GameObject NearestCellToStickAngle(Vector3 lookDir)
    {
        GameObject target = null;
        //find which adjacent cell's centroid is closest to angle pushed on stick
        
        //check adjacent cells on our current cell, we work current cell out every frame in PlayerInfo

        currentAdjacents = GetComponent<PlayerInfo>().currentCell.GetComponent<AdjacentCells>().adjacentCells;

        //find which cell is closest to stick direction
      //  List<float> anglesMinus = new List<float>();
      //  List<float> anglesPositive = new List<float>();

        List<MovementHelper.CellAndAngle> cellsAndAngles = new List<MovementHelper.CellAndAngle>();

        for (int i = 0; i < currentAdjacents.Count; i++)
        {

            //get angle of each cell relative to player position
            Vector3 centroid = currentAdjacents[i].GetComponent<ExtrudeCell>().centroid;
            Vector3 dirToCellFromPlayer = (centroid - transform.position).normalized;

            float angle = MovementHelper.SignedAngle(dirToCellFromPlayer, Vector3.right, Vector3.up);

            MovementHelper.CellAndAngle cAndA = new MovementHelper.CellAndAngle();
            cAndA.cell = currentAdjacents[i];
            cAndA.angle = angle + 180;//gets rid of minus numbers
            cellsAndAngles.Add(cAndA);
            // currentAdjacents[i].name = angle.ToString() + "";
        }

        //use comparer (at bottom of script) to sort list by angle
        cellsAndAngles.Sort(MovementHelper.SortByAngle);
        //we are going to create a pie chart adn the user will choose which slice to move the character to
        //now get mid points between each section

        for (int i = 0; i < cellsAndAngles.Count; i++)
        {
            //start of the slice is half between this angle and last angle

            float thisMid = cellsAndAngles[i].angle;

            if (i > 0)
            {
                float prevMid = cellsAndAngles[i - 1].angle;
                float start = (prevMid + thisMid) / 2;
                cellsAndAngles[i].sliceStart = start;
            }
            if (i < cellsAndAngles.Count - 1)
            {
                float nextMid = cellsAndAngles[i + 1].angle;
                float end = (nextMid + thisMid) / 2;
                cellsAndAngles[i].sliceEnd = end;
            }
        }

        //put in start and end for bookending array indexes
        float firstMid = cellsAndAngles[0].angle;
        float lastMid = cellsAndAngles[cellsAndAngles.Count - 1].angle;
        float firstStart = (firstMid + lastMid) / 2 - 180;
        cellsAndAngles[0].sliceStart = firstStart;
        float lastEnd = (lastMid + firstMid) / 2 + 180;

        //grab first slice and add 180 to make a complete circle
        cellsAndAngles[cellsAndAngles.Count - 1].sliceEnd = lastEnd;

        //now find which slice "stick angle" is within - this is the slice we want!
        float stickAngle = MovementHelper.SignedAngle(lookDir, Vector3.right, Vector3.up) + 180;

        //catch stick angle if before first slice, this will be pointing at last slice
        //need to catch because last slice will be ober 360, but first slice won't startfor a nit, 23.3 e.g


        if (stickAngle < cellsAndAngles[0].sliceStart)
        {
            //point it to last cell, slice starts after 0, there is a gap
            target = cellsAndAngles[cellsAndAngles.Count - 1].cell;
        }
        else if (stickAngle > cellsAndAngles[cellsAndAngles.Count - 1].sliceEnd)
        {
            //this means it should point to first cell
            target = cellsAndAngles[0].cell;
        }
        else
        {
            for (int i = 0; i < cellsAndAngles.Count; i++)
            {
                float start = cellsAndAngles[i].sliceStart;
                float end = cellsAndAngles[i].sliceEnd;
                //catch last to first out of 360 range problem

                if (stickAngle > start && stickAngle < end)
                {
                    target = cellsAndAngles[i].cell;
                }                
            }
        }

        return target;
    }
}

