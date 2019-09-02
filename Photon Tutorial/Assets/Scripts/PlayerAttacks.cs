using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerAttacks : MonoBehaviour {

    public Inputs inputs;

    public bool autoShield = false;

    public int playerNumber = 0;
    public float deadzone = 0.4f;//make global for other stick too?
    public bool rightStickReset = false;

    public float x;
    public float y;
    public bool playerMoving = false;
    public Vector3 lookDirRightStick;

  //  public bool lbPushed = false;
  //  public bool lbHeld = false;
 //   public bool rbPushed = false;
  //  public bool rbHeld = false;

    //trigger
   // public float leftTrigger;
  //  public float rightTrigger;

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
    public Spawner spawner;
    public Vector3 stabberStartLocalPos;
    public Vector3 currentTargetCentroid;
    public Vector3 rotateTarget;
    public bool stabTargetSet = false;
    
    public GameObject head;
    public Vector3 headOriginalPos;
    GameObject stabber;
   // GameObject shield;
    GameObject shieldPivot;
    GameObject shield;
    public bool blocking;
    public bool blockRaising = false;
    public bool blockLowering = true;//starts true so shield gets minimised
  //  public bool blockStartTimeSetRaise = false;
    //public bool blockStartTimeSetLower = true;//we can have this set at the start, the prefab will always start with shield down
    public bool blocking1UpdatedPressed = false;
    public bool blocking1UpdatedReleased = true;
    public double blockStartTime;
    //public double blockStartTimeLower;
    public Quaternion shieldStartingRotation;
    public Quaternion headStartingRotationOnBlock;
    public Vector3 headTargetDirectionOnBlock;
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
        playerNumber = GetComponent<PlayerInfo>().playerNumber;


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

        //do all of these get updated on network player? -will block ever be called?
        if(!swipe.overheadSwiping && !swipe.buttonSwiping && !swipe.whiffed && !GetComponent<PlayerMovement>().adjustingCellHeight)            
            Block();
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
        object[] content = new object[] { photonViewID, blocking, blockRaising, blockLowering, shieldStartingRotation, shieldScaleOnButtonPress, headStartingRotationOnBlock, headTargetDirectionOnBlock, headStartPos,blockStartTime };
        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        //keep resending until server receives
        SendOptions sendOptions = new SendOptions { Reliability = true };

        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

    }
    


    void GetInputs()
    {

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
        
        if (!autoShield)
        {
            //if we control this player, we can update any block events
            if (GetComponent<PhotonView>().IsMine)
            {
                //update locally
                BlockTargets();
                //update network
                SendBlockTargetsToNetwork();
            }

            //move shield etc if values allow
            BlockLerp();

        }

        else if(autoShield)
        {
            shieldPivot.SetActive(true);
           

            Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
            shieldPivot.transform.localRotation = Quaternion.RotateTowards(shieldPivot.transform.localRotation, targetRot, shieldRotationSpeed);


            //scale

            if (shield.transform.localScale.x < shieldStartingScaleX)
                shield.transform.localScale += scale;

            if (shield.transform.localScale.x > shieldStartingScaleX)
                shield.transform.localScale = new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ);

        }
    }

    void BlockTargets()
    {
        //set locally or updated from network

        //need to change this to blocking bool. only update if client is ours, else we will update from event code
        //player needs to input block button and direction to start a block action, can't set new block until old block is finished
        if (inputs.blocking0 && lookDirRightStick.magnitude > deadzone && !blocking)
        {
            //set flag to keep track, this will be set to false after timer runs out
            blocking = true;
            blockRaising = true;

            blockStartTime = PhotonNetwork.Time;
            //blockStartTimeSetRaise = true;
            shieldStartingRotation = shieldPivot.transform.localRotation;
            shieldScaleOnButtonPress = shield.transform.localScale;

            //set start and end rotation targets
            headStartingRotationOnBlock = head.transform.rotation;
            headTargetDirectionOnBlock = lookDirRightStick;

            headStartPos = head.transform.localPosition;

         

            Debug.Log("[CLIENT] - Setting shield targets and sending to network");
                

            //send targets to network
            SendShieldToNetwork();
            

        }
        //if timer has run its course we can reset the block, we can keep block input down if we wish to hold on for longer
        else if (PhotonNetwork.Time - blockStartTime > swipe.playerClassValues.blockMinimum && blockRaising == true && (lookDirRightStick.magnitude < deadzone || !inputs.blocking0))
        {
            Debug.Log("unblocking");
            //start unwind animation
            blockRaising = false;
            blockLowering = true;
            //targets, only set if we control this player
            if (GetComponent<PhotonView>().IsMine)
            {
                //reset raise flag
             //   blockStartTimeSetRaise = false;

                ////  if (!blockStartTimeSetLower)
                {
                    blockStartTime = PhotonNetwork.Time;
                   // blockStartTimeSetLower = true;
                    shieldStartingRotation = shieldPivot.transform.localRotation;
                    shieldScaleOnButtonPress = shield.transform.localScale;
                    headStartingRotationOnBlock = head.transform.rotation;
                    headStartPos = head.transform.localPosition;

                    Debug.Log("[CLIENT] - lowering shield and sending to network");


                    //send targets to network
                    SendShieldToNetwork();
                    //   SendShieldDownToNetwork();
                }
            }


            //if button is let go lower shield and ensmallen



            //unduck
            ////    head.transform.localPosition += Vector3.up * duckSpeed;
            //   if (head.transform.localPosition.y > headOriginalPos.y)
            //       head.transform.localPosition = headOriginalPos;

        }

    }

    void BlockLerp()
    {
        //moves depending on values set in BlockTargets

        if (blockRaising)
        {
            //player rotation
            //shield rotation
            float lerpT = (float)((PhotonNetwork.Time - blockStartTime) / playerClassValues.blockRaise);
            //can put easings function here?
             //lerpT = Easings.CubicEaseIn(lerpT);//slow start and straight end
            //new
            //target rotation was set when player initiated block

            //add upwards look to stick direction
            Quaternion targetRot = Quaternion.LookRotation(headTargetDirectionOnBlock + (Vector3.up * .33f / 2));

            //consider if blocking1 target rot change?
            head.transform.rotation = Quaternion.Lerp(headStartingRotationOnBlock, targetRot, lerpT);

            //head position
            //move cube forward and down making it look like it is bracing itself or leaning with a knee
            Vector3 targetForHead = headOriginalPos + head.transform.localScale.x * .66f * head.transform.forward - head.transform.localScale.x * .66f * Vector3.up;
            head.transform.localPosition = Vector3.Lerp(headStartPos, targetForHead, lerpT);

            //shield rotation
            //lerpT = (float)((PhotonNetwork.Time - blockStartTimeRaise) / playerClassValues.blockRaise);
            //can put easings function here?
            //lerpT = Easings.CubicEaseIn(lerpT);//slow start and straight end


            //add look up to rotation
            Vector3 lookUpAndFwd = Vector3.forward + Vector3.up * (.33f / 2);
            targetRot = Quaternion.LookRotation(lookUpAndFwd);

            //consider if blocking1 target rot change
            shieldPivot.transform.localRotation = Quaternion.Lerp(shieldStartingRotation, targetRot, lerpT);


            //scale

            shield.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ), lerpT);

        }
        else if (blockLowering)
        {

            //player rotation

            float lerpT = (float)((PhotonNetwork.Time - blockStartTime) / playerClassValues.blockLower);
           // Debug.Log(lerpT);
            //can put easings function here?
            //lerpT = Easings.CubicEaseOut(lerpT);//slow start and straight end
            //new
            //target rotation - face forward to match transform forward
            //start where we finished blcoking towards
            Quaternion startingRot = Quaternion.LookRotation(headTargetDirectionOnBlock + (Vector3.up * .33f / 2));
            Quaternion targetRot = Quaternion.LookRotation(transform.forward);
            //consider if blocking1 target rot change?
            head.transform.rotation = Quaternion.Lerp(startingRot, targetRot, lerpT);

            Vector3 targetForHead = headOriginalPos;
            head.transform.localPosition = Vector3.Lerp(headStartPos, targetForHead, lerpT);

            //shield rotation
            // lerpT = (float)((PhotonNetwork.Time - blockStartTimeLower) / playerClassValues.blockLower);
            //can put easings function here?
            ///// lerpT = Easings.CubicEaseIn(lerpT);//slow start and straight end

            targetRot = Quaternion.LookRotation(Vector3.down);
            //consider if blocking1 target rot change
            shieldPivot.transform.localRotation = Quaternion.Lerp(shieldStartingRotation, targetRot, lerpT);
            //scale

            shield.transform.localScale = Vector3.Lerp(new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ), Vector3.zero, lerpT);

            //once animation has got to 1f lerp (copmplete) reset flag to allow another block
            if (lerpT >= 1f)
            {
                blocking = false;
                blockLowering = false;
            }
        }
    }

    void SendBlockTargetsToNetwork()
    {

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

