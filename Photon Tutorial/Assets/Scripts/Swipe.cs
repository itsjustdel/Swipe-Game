using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
public class Swipe : MonoBehaviour {


    //debug bool
    public bool record = false;
    public bool playback = false;
    public bool playbackTimeSet = false;
    public List<Vector3> recordedInput = new List<Vector3>();
    //classes we need access to
    public PlayerAttacks pA;
    public Inputs inputs;
    public GameObject head;
    public GameObject swiper;

    //adjustable variables
    //[Range(0.2f, 2f)]
    // public float weaponSpeed = .5f;
    //  public float weaponSpeedStab = .1f;
    public float debugSwipeLength = 20f;
    public float angleAllowedForLetGoMax = 135f;
    public float angleAllowedForLetGoMin = 90f;
    //  public float maxTimeAllowedInDeadzoneThroughStrike = 0.4f;
    //  public float maxSwipeTime = 5f;
    public float overheadWaitBeforeReset = .0f;//min at fixed update? // time to wait before allowing next strike, do we need different for every type of strike, do we need at all? *off atm
    public float wiggleRoom = .5f;
  //  public float distanceForStillStickDetection = 0.01f;//matching fixed update
 //   public float distanceForStillStickDetectionCentral = 0.1f;//alow more space. tiny amounts needed for quick detection for strike end
 //   public int framesForCancel = 1;
 //   public int stillStickFramesCurrent = 1;
    public float maxThumbMagnitude = 0.85f;

    // float swordStart = 10f;
    //public float swordWidth = 2f;
    public float overheadActivationAngle = 0f;// 30f;//145;//     changed how this works - availabel from any angle now
    public float sideSwipeActivationAngle = 0f;// 90f;
    public float lungeActivationAngle = 0f;//30f;
    //flags
    public bool swiping;
    public bool overheadAvailable = true;
    public bool sideSwipeAvailable = true;
    public bool lungeAvailable = true;
    
    public bool buttonSwipeAvailable;
    public bool rightStickPulledBack;
    public bool waitingOnResetPlanning;
    public bool waitingOnResetStriking;

    public bool lowAttack = false;

    public bool overheadSwiping = false;
    public bool overheadLow = false;
    public bool overheadHigh = false;
    public bool sideSwiping = false;
    public bool lunging = false;
    public bool buttonSwiping;

    public bool sideCircle = false;
    //  public bool allowingFinishForLunge;

    //public bool rightStickStill;//detects if user is holding stick in one position or is moving it
    public bool rightStickLetGo;//flagged when stick is travelling back to center position
    public bool rSForward;
    public bool rSBackward;
    public bool pulledBackForSideSwipe;
    public bool pulledBackForOverhead;
    public bool planningPhaseSideSwipe;
    public bool planningPhaseOverheadSwipe;

  
    public bool buttonSwipeFirstLookDirSet = false;


    public bool waitingOnResetOverhead;
    public bool waitingOnResetSideSwipe;
   // public bool waitingOnResetLunge;
    public bool waitingOnResetButtonSwipe;
    public bool whiffed;
    public bool blocked;
    public bool hit;

    //counters
    // public float finishTimePlanning;
    public double finishTimeSriking;
    //public int deadzoneTimesInSwipe;
    public double pullBackTimeStart;
    public double swipeTimeStart;
    public float firstPullBackAngle;
    public float angleTravelled = 0;

    // public float timeToFinishSwipe = 0.01f;
    //public float SwingToFinishStartTime;
    //relative to player lookDirection
    public float rightStickAngle;
    public float yAdd;
    // float previousSideSwipeMagnitude = 0f;
    float previousDot = 0f;
    public float currentDot = 0f;

   // public bool overheadHit;
    public bool overheadBlock;
    public bool overheadWhiff;

    public bool sideSwipeHit;
    public bool sideSwipeBlock;
    public bool sideSwipeWhiff;

    public bool lungeHit;
    public bool lungeBlock;
    public bool lungeWhiff;

    public bool selfPlayerHit;

    //positions we need to save
    public Vector3 firstPullBackLookDir;
    Vector3 swipeStartLookDir;
    Vector3 previousRightStickPos;//last frame
    Vector3 palyerPositionAtStartOfSwipePlan;
    // Vector3 lastSwipePoint;
    public Vector3 swipePoint;
    public Vector3 previousSwipePoint;
    private Vector3 previousPreviousSwipePoint;
    Vector3 previousRelativeRightStickDir;
    Vector3 previousRsPos;

    //debug helpers
    public float previousAngle;
    public float startAngle;
    public float startAngleRelative;
    public float swingMagnitude;

    public BezierSpline spline;

    //GameObject debugCube;

    // public List<Vector3> swipePoints = new List<Vector3>();
    public List<Vector3> centralPoints = new List<Vector3>();
    public List<Vector3> sideSwipePoints = new List<Vector3>();
    public List<Vector3> lungePoints = new List<Vector3>();
    public List<Vector3> lungePointsFinal = new List<Vector3>();

    public System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    public GameObject currentSwipeObject;
    //public List<GameObject> currentSwipes = new List<GameObject>();

    public PlayerClassValues playerClassValues;


    // public bool fullCircle = false;


    public List<Vector3> lungeOriginalVertices;//save to give to swipe object for simple raycasting
    public float angleForLetGo;
    public float prevAngleForLetGo;

    public float curveSmoothing = 1f;
    public float arcDetail = .05f;//changing this affects overhead speed (great!) perhaps multiply overhead speeed by this so stays consistent?
    public float dragSize = .5f;//divides array for curve points, so 1f wil let the full curve come out before starting the exit animation, and 0.1f will start exit animation quickly

    public GameObject guide;

    public ProceduralAudioController proceduralAudioController;



    private void Start()
    {



        //tell sound script where we are at
        //GetComponent<SwipeSound>().swipe = this;
        //GetComponent<SwipeSound>().enabled = true;
        // GetComponent<SineWaveExample>().swipe = this;
        inputs = GetComponent<Inputs>();

        head = transform.Find("Head").gameObject;

        UpdateValues();

        stopwatch = new System.Diagnostics.Stopwatch();


        // debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //debugCube.transform.localScale *= 3;
        pA = transform.GetComponent<PlayerAttacks>();

        guide = Guide.GenerateGuide(this);





        // swiper = transform.Find("Swiper").gameObject;

        // spline = swiper.AddComponent<BezierSpline>();
    }


    void UpdateValues()
    {
        //grab variables from Code object
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();

    }

    //called from fixed update in PlayerAttacks
    public void SwipeOrder()
    {


        if (playback)
        {
            //play recorded input
            if (playbackTimeSet == false)
            {

                //  CreateNewSwipeObject("Side Swipe Recorded");
                //  swipeTimeStart= Time.time;
                //  playbackTimeSet = true;
            }

            // RenderSideSwipe(recordedInput);
        }




        DetectForwardMovementRightStick();//also updates current dot

        GetSwipePoint();

        if (!pA.blocking && !GetComponent<PlayerMovement>().adjustingCellHeight)
        {
            //look for user input and determine which swing to start

            SwipePlanning();
        }
        else if (pA.blocking || !GetComponent<PlayerMovement>().adjustingCellHeight)
        {
            ResetFlags();
        }


        previousRightStickPos = pA.lookDirRightStick;



    }



    void DetectForwardMovementRightStick()
    {
        //set flag if stick is being pushed ahead of character#s forward direction

        Vector3 lookDirPlusPlayerPosition = -pA.lookDirRightStick;// - transform.position;
        float dot = Vector3.Dot(lookDirPlusPlayerPosition, transform.forward);
        //  Debug.Log(dot);


        previousDot = currentDot;
        currentDot = dot;

        //do we need a tolerance/wiggle room setting?
        if (previousDot < currentDot)
        {
            //moving back
            rSBackward = true;
            rSForward = false;
            //  rightStickStill = false;


        }
        else if (previousDot > currentDot)
        {
            //moving foreard
            rSBackward = false;
            rSForward = true;
            // rightStickStill = false;
        }
        else if (previousDot == currentDot)
        {
            //right stick still
            rSBackward = false;
            rSForward = false;
            // rightStickStill = true;
        }

      

        //keep track of how long stick has been still for -- not working
       // if (rightStickStill)
      //      stillStickFramesCurrent++;
        
        


        //detect whether stick is on its way to center
        Vector3 swipe0y = new Vector3(swipePoint.x, 0f, swipePoint.z);
        Vector3 prevSwipe0y = new Vector3(previousSwipePoint.x, 0f, previousSwipePoint.z);
        Vector3 prevPrevSwipe0y = new Vector3(previousPreviousSwipePoint.x, 0f, previousPreviousSwipePoint.z);
        Vector3 a = (prevSwipe0y - prevPrevSwipe0y).normalized;
        Vector3 b = (prevPrevSwipe0y - swipe0y).normalized;

        // Debug.DrawLine(previousSwipePoint + head.transform.position, previousPreviousSwipePoint + head.transform.position);
        //  Debug.DrawLine(previousSwipePoint + head.transform.position, swipePoint+ head.transform.position);
        angleForLetGo = Vector3.Angle(a, b);
        //angleForLetGo = MovementHelper.SignedAngle(toCenterFromCurrent, toCurrentFromPreviousSwipePoint, Vector3.up);

        if (prevAngleForLetGo - angleForLetGo > angleAllowedForLetGoMax)// && angleForLetGo < angleAllowedForLetGoMin)
            rightStickLetGo = true;
        else
            rightStickLetGo = false;

        // if (planningPhaseOverheadSwipe)
        //    Debug.Log(angleForLetGo);

    }


    void GetSwipePoint()
    {
        previousAngle = startAngle;
        startAngle = MovementHelper.SignedAngle(pA.lookDirRightStick, transform.forward, Vector3.up);
        //Vector3 rotatedLookDir = transform.rotation * pA.lookDirRightStick;
        //startAngleRelative = MovementHelper.SignedAngle(rotatedLookDir, transform.transform.forward, Vector3.up);

        //get magnitude and clamp it
        swingMagnitude = pA.RSMagnitude ;
        if (swingMagnitude > 1f)
            swingMagnitude = 1f;
        //this rounds up any magnitude at 0.99
        //  swingMagnitude = (float)(System.Math.Round(swingMagnitude, 1));//just using magnitude >.95 atm ? no
        //doing this means transitions wont be smooth, so we will need to lerp animations - but it's workth it to make sure all data is tight and accurate (lol, changed)




        //save previous position
        previousPreviousSwipePoint = previousSwipePoint;
        previousSwipePoint = swipePoint;
        prevAngleForLetGo = angleForLetGo;
        previousRsPos = pA.lookDirRightStick;
        //still need to remove debug length?
        //when swiper is active, add any left over magnitude to height, this will create overhead strikes if user strikes through center of analog stick
        yAdd = .85f - swingMagnitude;//makes sure it goes to 0 on y 
        if (yAdd < 0f)
            yAdd = 0f;
        //create cureve from this linear paramater
        yAdd = Easings.CubicEaseOut(yAdd);
        
        swipePoint = pA.lookDirRightStick.normalized * swingMagnitude + Vector3.up * (yAdd);


        //debugCube.transform.position = head.transform.position + swipePoint;
    }

    /// <summary>
    /// Uses Right Analog stick to create swipe pattern
    /// </summary>    
    void SwipePlanning()
    {

        /////////////////
        //check to see if any timer have finished, so we can reset any available strikes - player cooldown after swiping and whiffing

        //overhead
        if (waitingOnResetOverhead)
        {
            if (whiffed)
            {
                if (PhotonNetwork.Time- finishTimeSriking > playerClassValues.playerCooldownAfterOverheadWhiff)
                {
                    Debug.Log("making overhead available after cooldown wait");
                    waitingOnResetOverhead = false;
                    buttonSwipeAvailable = true;
                    overheadAvailable = true;

                    ResetFlags();//everything reset? or just this swipe option..
                }
            }
            else if (blocked)
            {
                
                if (PhotonNetwork.Time - finishTimeSriking > playerClassValues.playerCooldownAfterOverheadBlock)
                {
                    Debug.Log("making overhead available after cooldown wait - block");
                    waitingOnResetOverhead = false;
                    buttonSwipeAvailable = true;
                    overheadAvailable = true;

                    ResetFlags();//everything reset? or just this swipe option..
                }
            }
            else if (hit)
            {

                if (PhotonNetwork.Time - finishTimeSriking > playerClassValues.playerCooldownAfterOverheadHit)
                {
                    Debug.Log("making overhead available after cooldown wait - hit");
                    waitingOnResetOverhead = false;
                    buttonSwipeAvailable = true;
                    overheadAvailable = true;

                    ResetFlags();//everything reset? or just this swipe option..
                }
            }    //no penalty for hitting - reset instantly

        }
        //button swipe
        if (waitingOnResetButtonSwipe)
        {
            if (whiffed)
            {
                if (Time.time - finishTimeSriking > playerClassValues.playerCooldownAfterLungeWhiff)
                {
                    Debug.Log("making lunge available after cooldown wait - whiff");

                    buttonSwipeAvailable = true;
                    overheadAvailable = true;
                    waitingOnResetButtonSwipe = false;
                    ResetFlags();//everything reset? or just this swipe option..
                }
            }
            else if (blocked)
            {
                if (Time.time - finishTimeSriking > playerClassValues.playerCooldownAfterLungeBlock)
                {
                    Debug.Log("making lunge available after cooldown wait - block");

                    buttonSwipeAvailable = true;
                    overheadAvailable = true;
                    waitingOnResetButtonSwipe = false;
                    ResetFlags();//everything reset? or just this swipe option..
                }
            }
            else if (hit)
            {
                if (Time.time - finishTimeSriking > playerClassValues.playerCooldownAfterLungeHit)
                {
                    Debug.Log("making lunge available after cooldown wait - block");

                    buttonSwipeAvailable = true;
                    overheadAvailable = true;
                    waitingOnResetButtonSwipe = false;
                    ResetFlags();//everything reset? or just this swipe option..
                }
            }
        }



        /*lunge stuff
        //check to see if payer has centred stick, required to reset lunge availability
        // if (overheadAvailable && sideSwipeAvailable)
        {
            if (waitingOnResetLunge && !lunging && !lungeAvailable)// && (overheadSwiping || sideSwiping))
            {
                if (Time.time - finishTimeSriking > playerClassValues.playerCooldownAfterSideSwipe)
                {
                    //if (rightStickStill && pA.lookDirRightStick.magnitude < 0maxThumbMagnitude)
                    if (pA.lookDirRightStick.magnitude < 0.25f)
                    {
                        waitingOnResetLunge = false;
                       // Debug.Log("reset lunge");

                        lungeAvailable = true;

                        //  overheadAvailable = true;
                        //  sideSwipeAvailable = true;

                        lungePoints.Clear();

                    }
                }
            }
        }
        */
        lungeAvailable = false;
        sideSwipeAvailable = false;
        buttonSwipeAvailable = false;

        ///////
        /////populate list for lunge points constantly
       // LungePoints();


        //check to see where stick is and se if we can start to save path data for any swipes

      //  if (inputs.state.Buttons.RightShoulder == XInputDotNetPure.ButtonState.Released)//not using button swipe..
        {
            if (overheadAvailable)
            {
                //look for overhead             
                CheckForOverheadPullBack();
            }
        }

      //  ButtonSwipe();


        /*
        else if (pA.rbHeld)
        {
            if (lungeAvailable)
            {
                //look for forward attack
                CheckForLungeStart();
            }

            if (sideSwipeAvailable)//will never get here atm
            {
                //look for sideAttack            
                CheckForSideSwipePullBack();
            }
        }
        */

        //if stick is in a swipe start position, check to see if player has moved the stick enough to start loking for a path (allows some wobbly finger movements(small))
        if (pulledBackForOverhead)
        {
            //not using button swipe

           // if (inputs.state.Buttons.RightShoulder == XInputDotNetPure.ButtonState.Pressed)
          //  {
                //user pressed button. over ride witrh side swipe
          //      ResetFlags();
          //      return;
          //  }

            float distanceFromInitialPullBack = Vector3.Distance(firstPullBackLookDir, swipePoint);

            if (distanceFromInitialPullBack > wiggleRoom)
            {
                //centralPoints.Clear();
                // sideSwipePoints.Clear();
                //reset still count for this swipe
               // stillStickFramesCurrent = 0;
                pulledBackForOverhead = false;
                planningPhaseOverheadSwipe = true;
            }
        }

        /*
        //this allows the player to alter side swipe position slightly if pulling back too
        if (pulledBackForSideSwipe)
        {
            //check to see if user has  launched strike
            float distanceFromInitialPullBack = Vector3.Distance(firstPullBackLookDir, swipePoint);
            
            if (distanceFromInitialPullBack > wiggleRoom && pA.lookDirRightStick.magnitude > maxThumbMagnitude)
            {
                //also make sure user is pushing forward if for side swipe
                /*
                if (rSForward)
                {
                    //player reset before swiping
                    centralPoints.Clear();
                    sideSwipePoints.Clear();

                    pulledBackForSideSwipe = false;
                    //sideSwipe = true;
                    //start following stick movement
                    planningPhaseSideSwipe = true;

                }
                else if (rSBackward) //if we wantt o put side wipe backwards in, remove this, also will need to stop overhead overwriting this
                {
                    //reset the pullback start dir
                    firstPullBackLookDir = swipePoint;
                }
                ///*
                if (rightStickStill)// && pA.lookDirRightStick.magnitude < maxThumbMagnitude)
                {
                    //  Debug.Log("reset from cancel - side swipe");
                    //   ResetFlags();
                    //player reset before swiping
                    centralPoints.Clear();
                    sideSwipePoints.Clear();

                    pulledBackForSideSwipe = false;
                    //sideSwipe = true;
                    //start following stick movement
                    planningPhaseSideSwipe = true;
                }


            }
            //detect if user cancels strike

            if (rightStickStill)// && pA.lookDirRightStick.magnitude < maxThumbMagnitude)
            {
              //  Debug.Log("reset from cancel - side swipe");
             //   ResetFlags();
            }

        }
        
       

        if (planningPhaseSideSwipe)
        {
            //add position to list
            StickPathSideSwipe();
            //check if we have finished a swipe
            ChecksSideSwipe();
        }
    */

        //gather path and check for end of swipe
        if (planningPhaseOverheadSwipe)
        {
            //populate list with points
            StickPathOverhead();
            //detect if user has finished input
            ChecksOverhead();
        }
    }

    void CreateNewSwipeObject(string type, bool overhead, bool sideSwipe, bool buttonSwipe)
    {
        Debug.Log("creating new swipe object");

        GameObject newSwipe = new GameObject();
        newSwipe.name = "swipe Current " + type;
        newSwipe.AddComponent<MeshFilter>();
        newSwipe.AddComponent<MeshRenderer>();
        newSwipe.transform.position = head.transform.position;
        newSwipe.layer = LayerMask.NameToLayer("Swipe");
        newSwipe.AddComponent<MeshCollider>();
        currentSwipeObject = newSwipe;

        SwipeObject sO = newSwipe.AddComponent<SwipeObject>();
        sO.parentPlayer = gameObject;
        sO.firstPullBackLookDir = firstPullBackLookDir;

        sO.playerClassValues = playerClassValues;
        sO.activeTime = playerClassValues.overheadWhiffCooldown;
        sO.firstPullBackLookDir = firstPullBackLookDir;
        sO.swipeTimeStart = Photon.Pun.PhotonNetwork.Time;

        if (overhead)
        {

            sO.overheadSwipe = true;
            //note time of the the user finishing their swipe plan            
            //pass planned points
            sO.centralPoints = new List<Vector3>(centralPoints);
        }
        if (sideSwipe)
        {
            sO.sideSwipe = true;
        }
        if (!overhead && !sideSwipe && !buttonSwipe)
        {
            //lunge
            sO.lunge = true;
        }
        if (buttonSwipe)
        {
            sO.buttonSwipe = true;
        }

        //only have two current swipes
        // if (currentSwipes.Count > 1 && currentSwipes.Count > 0)
        {
            //Destroy(currentSwipes[0]);
            //currentSwipes.RemoveAt(0);


        }
        //audio
        ProceduralAudioController pAC = newSwipe.AddComponent<ProceduralAudioController>();
        pAC.swipeObject = true;
        pAC.useSinusAudioWave = true;

        /// currentSwipes.Add(newSwipe);
    }

    void LungePoints()
    {
        /*
        //this functin constantly popultaes lunge points with stick movements. 
        //it will reset and clear its list if any other attack happesn or user stops moving stick
        if (!rightStickStill)// && currentDot < 0f)//does allow for backwards travelling in front half
            lungePoints.Add(swipePoint);
        else
            lungePoints.Clear();
            */
    }

    void CheckForLungeStart()
    {
        //  Debug.Log("checking for lunge start");
        //  //lunge enabled when user starts from neutral and pushes forward within angle allowance
        //if (!lunging)
        {
            if (pA.lookDirRightStick.magnitude >= maxThumbMagnitude && startAngle > -lungeActivationAngle && startAngle < lungeActivationAngle)
            {

                Debug.Log("found lunge start");

                firstPullBackLookDir = swipePoint;
                pullBackTimeStart = Time.time;
                //finishTimePlanning = Time.time;
                swipeTimeStart = Time.time;//review

                //lungeWaitingForReset = true;

                overheadAvailable = false;
                lungeAvailable = false;
                sideSwipeAvailable = false;

                planningPhaseOverheadSwipe = false;
                planningPhaseSideSwipe = false;

                lunging = true;

                //make a swipe object
                CreateNewSwipeObject("Lunge", false, false, false);



                //zero last lunge y 
                Vector3 zeroYLast = new Vector3(lungePoints[lungePoints.Count - 1].x, 0f, lungePoints[lungePoints.Count - 1].z);
                //save this list, lungepoints will be reset
                lungePoints[lungePoints.Count - 1] = zeroYLast;
                lungePointsFinal = new List<Vector3>(lungePoints);

                currentSwipeObject.GetComponent<SwipeObject>().lungePointsFinal = lungePointsFinal;

                for (int i = 0; i < lungePointsFinal.Count; i++)
                {
                    //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c.transform.position = lungePointsFinal[i] + head.transform.position;
                    // Destroy(c, 3);
                }
            }
        }
    }

    void CheckForLungeEnd()
    {
        /* 
        Debug.Log("checking for lunge end");
        if (rightStickStill)
        {
            //considering fixed update step to allow one more frame to finish rendereing
            if (Time.time - swipeTimeStart - Time.fixedDeltaTime > playerClassValues.lungeSpeed + overheadWaitBeforeReset)//overheadWaitBeforeReset
            {
                Debug.Log("found lunge end - timer");
                lunging = false;
                lungeWhiff = true;
                ResetFlags();

                // Debug.Log("ended lunge on timer");
            }
        }
        */
    }

    void CheckForOverheadPullBack()
    {
        //Debug.Log(pA.RSMagnitude);
        //look for overhead start
        //if ((startAngle < -overheadActivationAngle || startAngle >= overheadActivationAngle) && 
         if(pA.RSMagnitude >= maxThumbMagnitude)//.95 means you need to slam the stcik off the rim

        {
          //  Debug.Log("1");
            //set flag
            pulledBackForOverhead = true;
            pulledBackForSideSwipe = false;

            lungeAvailable = false;
            overheadAvailable = false;
            sideSwipeAvailable = false;

            swipeTimeStart = PhotonNetwork.Time;
            firstPullBackLookDir = swipePoint;

            firstPullBackAngle = MovementHelper.SignedAngle(firstPullBackLookDir, transform.forward, Vector3.up);
            angleTravelled = 0f;

            pullBackTimeStart = PhotonNetwork.Time;
            stopwatch.Start();

            //clear mesh
            // swiper.GetComponent<MeshFilter>().mesh.Clear();

            // debugCube.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;

            //

            palyerPositionAtStartOfSwipePlan = transform.position;

           
        }
    }

 

  

    void CalculateAngleTravelled()
    {

        //detect a 360
        //add to angle travelled counter
        float smallAngle = Vector3.Angle(pA.lookDirRightStick, transform.forward);
        float smallAnglePrevious = Vector3.Angle(previousRightStickPos, transform.forward);
        angleTravelled += Mathf.Abs(smallAngle - smallAnglePrevious);
    }

    void ChecksOverhead()
    {
        if (overheadSwiping)
            return;

        bool sendToRender = false;



        CalculateAngleTravelled();
        //use a minumum strike amount
       // if (angleTravelled < 45 && rightStickStill) //45?, refine?
        {
            //ResetFlags();
           // return;
        }

    

        //detect if player stops moving the stick
        if (swipePoint == previousPreviousSwipePoint)// && pA.lookDirRightStick.magnitude>0.95f)
        {

            if (pA.RSMagnitude < pA.deadzone)
            {
                ResetFlags();
              //  Debug.Log("Reset From Cancel, magnitude = " + pA.lookDirRightStick.magnitude);
            }
            if (angleTravelled > 360 + 180)
            {
                //,deadzone? work in progress) 
              //  ResetFlags();
              //  Debug.Log("Reset From Cancel - angle travelled = " + angleTravelled);
            }
            else if (centralPoints.Count > 3)
            {


                sendToRender = true;

            }
            else
            {

                //add imaginary mid point - 
                if (centralPoints.Count == 3)
                {
                    //simulate movement, if angle is shallower, make middle points higher, if lower angle, make arc flatter
                    Vector3 firstArm = centralPoints[0];
                    Vector3 secondArm = centralPoints[centralPoints.Count - 1];

                    Vector3 middleArm = Vector3.Lerp(firstArm, secondArm, 0.5f);
                    //fully flatten if below quarter circle strike
                    if (angleTravelled < 90)
                        middleArm.y = 0f;
                    //start to raise the more straight the swipe is(back to front)
                    else
                        middleArm.y = Mathf.Abs(45f - angleTravelled) / angleTravelled;




                    //centralPoints.Insert(2, new Vector3(0, 1f, 0f));
                    centralPoints.Insert(2, middleArm);

                    // for (int i = 0; i < centralPoints.Count; i++)
                    // {
                    //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //    c.transform.position = centralPoints[i] * 10 + head.transform.position;
                    //    Destroy(c, 2);
                    // }


                    sendToRender = true;
                }
                else
                {
                    //reset, never travelled far enough
                    centralPoints.Clear();
                    sendToRender = false;
                    return;
                }

            }
        }

        bool stop360 = false;
        if (stop360)
        {
            if (angleTravelled >= 320)
            {
                Debug.Log("360 detected");

                centralPoints.RemoveAt(centralPoints.Count - 1);
                centralPoints.Add(centralPoints[0]);
                centralPoints.Add(centralPoints[0]);

                sendToRender = true;
            }
        }

        if (sendToRender)
        {
            centralPoints.Add(swipePoint);//makes sure it alwys goes to end
            
            Debug.Log("Over head - finished planning");
            planningPhaseOverheadSwipe = false;
            //start swipe being rendered and being checked for any hits
            overheadSwiping = true;

            //set walk speed from here
            PlayerMovement pM = GetComponent<PlayerMovement>();
            pM.walkSpeedThisFrame = pM.walkSpeedWhileAttacking;
                
            CreateNewSwipeObject("Overhead", true, false, false);
            currentSwipeObject.GetComponent<SwipeObject>().activeSwipe = true;
            currentSwipeObject.GetComponent<SwipeObject>().overheadSwipe = true;
            //note time of the the user finishing their swipe plan
            //swipeTimeStart = Time.time;





            // List<Vector3> returnedPoints = new List<Vector3>();
            //one final update on the mesh

            // centralPoints.Add(swipePoint);
            //currentSwipeObject.GetComponent<MeshFilter>().mesh = RenderCurve(out returnedPoints, centralPoints);//?

            //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // c.transform.position = centralPoints[centralPoints.Count - 1] + currentSwipeObject.transform.position;
            //Destroy(c, 3);

          //  ProceduralAudioController proceduralAudioControllerForNewObject = currentSwipeObject.GetComponent<ProceduralAudioController>();
            //the frequency which this palyer's guide is buily from
          //  double baseFrequency = GetComponent<ProceduralAudioController>().mainFrequency;
            //create harmony
            //baseFrequency =baseFrequency/2 + ((baseFrequency / 8) * 12);
            //proceduralAudioControllerForNewObject.mainFrequency = baseFrequency;
          //  proceduralAudioControllerForNewObject.mainFrequencyBase = (float)baseFrequency;

        }

    }

   

    void StickPathOverhead()
    {
        


        //gather stick movement
        Vector3 tPY0 = new Vector3(transform.position.x, 0f, transform.position.z);
        palyerPositionAtStartOfSwipePlan = new Vector3(palyerPositionAtStartOfSwipePlan.x, 0f, palyerPositionAtStartOfSwipePlan.z);
        //trying toget swipe to mvoe with player - will need to keep adding to stick path whilst swiping
        Vector3 playerDifference = (tPY0 - palyerPositionAtStartOfSwipePlan);//.normalized*50;//GetComponent<PlayerMovement>().lookDir ;// 
                                                                             // Debug.DrawLine(transform.position, palyerPositionAtStartOfSwipePlan,Color.magenta);
                                                                             //add curve points
        Vector3 modSwipePoint = swipePoint + playerDifference; //enough difference? render isnt using acutaly swipe point length, it normalizes and used length variable - coudl work out length here and change in render? -*cant work out here- need to know when swipe starts and finishes
        //in order to work out length - unless i remove length change?

        /*  shows difference between modified swipe
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = swipePoint + tPY0; 
        Destroy(c, 3);
        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = modSwipePoint + tPY0;
        c.name = "mod";
        Destroy(c, 3);
        */
        if (centralPoints.Count > 0)
        {
            //first of all remove last point, this will be a half way house between points at a regular distance from each other
            //centralPoints.RemoveAt(centralPoints.Count - 1);
            //centralPoints.RemoveAt(centralPoints.Count - 1);

            float d = Vector3.Distance(swipePoint, centralPoints[centralPoints.Count - 1]);
            // Debug.Log(d);
            //larger numbers mean more smoothing for the curve (basically it gives less points to the curve
            if (d >= curveSmoothing) //** still working out what's best for this
            {
                centralPoints.Add(swipePoint); //** revise?
            }
        }
        else
        {
            //working ok?
            centralPoints.Add(firstPullBackLookDir);
            centralPoints.Add(firstPullBackLookDir);
        }

        //some stuff in here from tracking real time strike - if i take it out, curve goes sharp at end, leaving for the moment


        //centralPoints.Add(swipePoint);//makes sure it alwys goes to end

    }

    void StickPathSideSwipe()
    {
        swipePoint = new Vector3(swipePoint.x, 0f, swipePoint.z);
        //debugCube.transform.position = head.transform.position + swipePoint;
        //sometimes, first pull back isn't quite zero height
        firstPullBackLookDir = new Vector3(firstPullBackLookDir.x, 0f, firstPullBackLookDir.z);
        //outside start

        //float distanceOfSwipe = Vector3.Distance(swipePoint, firstPullBackLookDir);
        Vector3 outsideStart = firstPullBackLookDir.normalized * (playerClassValues.armLength + playerClassValues.sideSwipeLength + playerClassValues.swordLength);
        //Vector3 outsideEnd = outsideStart + ((swipePoint - outsideStart).normalized * distanceOfSwipe * 1.33f);
        Vector3 outsideEnd = swipePoint.normalized * (playerClassValues.armLength + playerClassValues.sideSwipeLength + playerClassValues.swordLength);

        //        Vector3 insideStart = firstPullBackLookDir.normalized * (playerClassValues.armLength);//closeto player
        Vector3 insideStart = firstPullBackLookDir.normalized * (playerClassValues.armLength);// - playerClassValues.swordLength);
        Vector3 insideEnd = swipePoint.normalized * (playerClassValues.armLength);// + playerClassValues.swordLength - playerClassValues.swordLength);


        //if swipe is going in reverse - cancel

        float endDot = Vector3.Dot(outsideEnd, transform.forward);
        float startDot = Vector3.Dot(outsideStart, transform.forward);
        //  Debug.Log(dot);
        // Debug.Log("Start dot = " + startDot);
        //  Debug.Log("End dot = " + endDot);
        if (endDot < startDot)
        {
            // Debug.Log("swipe going backwards, reseting");
            // planningPhaseOverheadSwipe = false;

            //  ResetFlags();
            // Debug.Break();
        }

        //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //  c.transform.position = outsideStart + head.transform.position;

        // Debug.DrawLine(outsideStart + head.transform.position, insideStart + head.transform.position);
        // Debug.DrawLine(insideStart + head.transform.position, insideEnd + head.transform.position);
        //Debug.DrawLine(insideEnd + head.transform.position, outsideEnd + head.transform.position);
        //Debug.DrawLine(outsideEnd + head.transform.position, outsideStart + head.transform.position);


        List<Vector3> vertices = new List<Vector3>() { outsideStart, outsideEnd, insideEnd, insideStart };
        sideSwipePoints = vertices;

        if (record)
            recordedInput = new List<Vector3>(vertices);
    }

    public void CurveHitCheck2(List<Vector3> pointsFromCurve, bool activeSwipe, GameObject thisSwipeObject, bool forOverhead, bool forLunge)
    {
        if (pointsFromCurve.Count <= 24)
            return;

        for (int i = 0; i < pointsFromCurve.Count; i++)
        {
            // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // c.transform.localScale *= 0.3f;
            //  c.transform.position = pointsFromCurve[i] + thisSwipeObject.transform.position;
            //  Destroy(c, 2);
        }

        int rays = 0;
        //vertical side edge / length
        List<RaycastHit[]> totalRayList = new List<RaycastHit[]>();
        RaycastHit[] hits;
        List<Vector3> impactDirections = new List<Vector3>();

        SwipeObject thisSwipeObjectScript = thisSwipeObject.transform.GetComponent<SwipeObject>();

        //if swipe is active, activestrike = true, then look for player, shields and other swipes
        //else if it is on cooldown phase(static), don't look for other swipes, it can not destroy them
        LayerMask mask = LayerMask.GetMask("PlayerBody", "Shield", "Swipe");
        if (activeSwipe == false)
            mask = LayerMask.GetMask("PlayerBody", "Shield");


        //first do self test at fron of swipe, if user does a crossover strike, cancel swipe (or advacned idea, snap off rear of strike and continue)
        // Vector3 l0Self = pointsFromCurve[pointsFromCurve.Count - 3];
        // Vector3 l1Self = pointsFromCurve[pointsFromCurve.Count - 2];





        // GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //c0.transform.position = lastCentre;




        //if (pointsFromCurve.Count > 24 * 3)

        bool doSelfCheck = false;
        if (doSelfCheck)
        //ggoing round mesh and moving points slightly inside so they dont coddile with outer mesh. Not perfect, how to check self hit ?
        {

            for (int j = 0; j < pointsFromCurve.Count - 16 - 8; j += 16)
            {
                Vector3 secondLastCentre = Vector3.zero;
                Vector3 lastCentre = Vector3.zero;

                for (int i = j; i < j + 8; i += 2)
                {
                    // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    // c.transform.position = pointsFromCurve[i] + thisSwipeObjectScript.transform.position;
                    //  c.name = "first";

                    secondLastCentre += pointsFromCurve[i] + thisSwipeObjectScript.transform.position;
                }
                secondLastCentre /= 4;



                for (int i = j + 8; i < j + 16; i += 2)
                {
                    //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //   c.transform.position = pointsFromCurve[i] + thisSwipeObjectScript.transform.position;
                    //  c.name = "second";

                    lastCentre += pointsFromCurve[i] + thisSwipeObjectScript.transform.position;
                }
                lastCentre /= 4;


                for (int i = j; i < j + 16; i += 2)
                {
                    //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //   c.transform.position = thisSwipeObject.transform.position + pointsFromCurve[i];
                    //    c.name = i.ToString();
                    //    c.name = "p from C" + i.ToString();
                    //shoot back the way


                    Vector3 startingPos = pointsFromCurve[i] + thisSwipeObject.transform.position;
                    startingPos = Vector3.Lerp(startingPos, secondLastCentre, 0.1f);

                    Vector3 endPos = pointsFromCurve[i + 8] + thisSwipeObject.transform.position;
                    endPos = Vector3.Lerp(endPos, lastCentre, 0.1f);
                    startingPos += (endPos - startingPos).normalized * 0.1f;
                    endPos -= (endPos - startingPos).normalized * 0.1f;
                    //float distance = Vector3.Distance(startingPos, endPos) * .9f;

                    //  GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c0.transform.position = startingPos;

                    Debug.DrawLine(startingPos, endPos);
                    RaycastHit hit;
                    if (Physics.Linecast(startingPos, endPos, out hit, mask))

                    //for (int a = 0; a < hits.Length; a++)
                    {
                        //if we hit our own object, destroy!
                        if (hit.transform.gameObject == thisSwipeObject)
                        {
                            //if (Vector3.Distance(startingPos, hit.point) > 0.1f)
                            {
                                thisSwipeObjectScript.DestroySwipe();
                                thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                                /*
                                GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c0.transform.position = startingPos;
                                c0.name = "start";
                                Destroy(c0, 3);

                                GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                c1.transform.position = hit.point;
                                c1.name = "hit";
                                Destroy(c1, 3);
                                */
                            }
                        }
                    }
                }

            }
        }



        //checking for objects other than itself
        List<Vector3> startingPositionsForRays = new List<Vector3>();
        for (int i = 0; i < pointsFromCurve.Count - 8; i += 4)
        {

            rays++;

            Vector3 p0 = pointsFromCurve[i];
            // startingPositionsForRays.Add(p0);
            Vector3 p1 = pointsFromCurve[i + 1];
            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);



            hits = Physics.RaycastAll(p0 + thisSwipeObject.transform.position, dir, distance, mask);
            totalRayList.Add(hits);
            //add ipact direction to list too - used in explosion effect
            //use the forward direction of the last panel on the sipe object, the normal of the last triangle //**opto shouldn't this just be worked outa boce and there is only 1 impact direction to use?
            Vector3 l0 = pointsFromCurve[pointsFromCurve.Count - 3];
            Vector3 l1 = pointsFromCurve[pointsFromCurve.Count - 2];
            Vector3 l2 = pointsFromCurve[pointsFromCurve.Count - 1];
            Vector3 impactDir = Vector3.Cross(l0, l1).normalized;
            impactDirections.Add(impactDir);

            //Debug.DrawLine(p0 + thisSwipeObject.transform.position, p0 + dir*distance + thisSwipeObject.transform.position, Color.cyan);
            //  Debug.DrawLine(p0 + thisSwipeObject.transform.position, p0 + dir * distance + thisSwipeObject.transform.position, Color.yellow);
        }

        //horizontal edge / width
        for (int i = 2; i < pointsFromCurve.Count - 8; i += 4)
        {
            rays++;
            // Debug.DrawLine(pointsFromCurve[i] + thisSwipeObject.transform.position, pointsFromCurve[i + 1] + head.transform.position, Color.cyan);

            Vector3 p0 = pointsFromCurve[i];
            //  startingPositionsForRays.Add(p0);
            Vector3 p1 = pointsFromCurve[i + 1];
            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);

            hits = Physics.RaycastAll(p0 + thisSwipeObject.transform.position, dir, distance, mask);
            //totalRayList.Add(hits);
            //add ipact direction to list too - used in explosion effect
            //use the forward direction of the last panel on the sipe object, the normal of the last triangle
            Vector3 l0 = pointsFromCurve[pointsFromCurve.Count - 3];
            Vector3 l1 = pointsFromCurve[pointsFromCurve.Count - 2];
            Vector3 l2 = pointsFromCurve[pointsFromCurve.Count - 1];
            Vector3 impactDir = Vector3.Cross(l0, l1).normalized;
            impactDirections.Add(impactDir);

            //  Debug.DrawLine(p0 + thisSwipeObject.transform.position, p0 + dir * distance + thisSwipeObject.transform.position, Color.cyan);
        }

        //outside edge running along curve
        for (int i = 4; i < pointsFromCurve.Count - 16; i += 2)
        {
            rays++;
            //      Debug.DrawLine(pointsFromCurve[i] + thisSwipeObject.transform.position, pointsFromCurve[i + 8] + head.transform.position, Color.red);
            Vector3 p0 = pointsFromCurve[i];
            //  startingPositionsForRays.Add(p0);
            Vector3 p1 = pointsFromCurve[i + 8];
            Vector3 dir = (p1 - p0).normalized;
            //  p0 += dir * 0.1f;
            //  p1 -= dir * 0.1f;
            float distance = Vector3.Distance(p0, p1);

            hits = Physics.RaycastAll(p0 + thisSwipeObject.transform.position, dir, distance, mask);
            /*
            RaycastHit hit;
            if(Physics.Raycast(p0+thisSwipeObject.transform.position,dir,out hit,distance,mask))
            //if(Physics.Linecast(p0 + thisSwipeObject.transform.position,p1 + thisSwipeObject.transform.position ,out hit))
            {
                Debug.Break();

                 GameObject c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  c2.transform.position = hit.point;
                  c2.name = "hit of single ";
                  Destroy(c2, 3);
                //Debug.Log(LayerMask.LayerToName(hit.transform.gameObject.layer));

                Debug.DrawLine(p0 + thisSwipeObject.transform.position, p0 + thisSwipeObject.transform.position + dir*distance);

            }

            */
            totalRayList.Add(hits);
            //add ipact direction to list too - used in explosion effect
            //use the forward direction of the last panel on the sipe object, the normal of the last triangle
            Vector3 l0 = pointsFromCurve[pointsFromCurve.Count - 3];
            Vector3 l1 = pointsFromCurve[pointsFromCurve.Count - 2];
            Vector3 l2 = pointsFromCurve[pointsFromCurve.Count - 1];
            Vector3 impactDir = Vector3.Cross(l0, l1).normalized;
            impactDirections.Add(impactDir);

            // Debug.DrawLine(p0 + thisSwipeObject.transform.position, p0 + dir * distance + thisSwipeObject.transform.position, Color.red);
        }

        //Debug.DrawLine(endPoint0 + head.transform.position, endPoint0 + (dirToNext*distance) + head.transform.position);

        //shield check
        for (int i = 0; i < totalRayList.Count; i++)
        {
            for (int j = 0; j < totalRayList[i].Length; j++)
            {
                //dont do anythin if it detects a hit on its own swipe object
                if (totalRayList[i][j].transform.gameObject == thisSwipeObject)
                    continue;

                //check for shield hit first
                if (totalRayList[i][j].transform.gameObject.layer == LayerMask.NameToLayer("Shield"))
                {
                    Debug.Log("Hit Shield ( overhead )");

                    thisSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;

                    thisSwipeObjectScript.hitShield = true;
                    thisSwipeObjectScript.DestroySwipe();

                    if (thisSwipeObjectScript.activeSwipe)
                    {
                        finishTimeSriking = PhotonNetwork.Time;
                        waitingOnResetOverhead = true;
                        buttonSwipeAvailable = false;
                        blocked = true;
                        
                        //start timer for reset
                        //thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                    }

                    //tell hit player to vibrate
                    GameObject parentOfHitHeadMesh = totalRayList[i][j].transform.parent.parent.parent.gameObject;
                    PlayerVibration pV = parentOfHitHeadMesh.GetComponent<PlayerVibration>();
                    pV.shakeTimerShield+= pV.shieldHitLength;
                    //tell player who successfully hit too - just use non lethal for hit confirm
                    pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                    pV.shakeTimerShield += pV.shieldHitLength;

                    //send resolution to network
                    byte evCode = 43; // Custom Event 43: send shield hit to clients

                    int photonViewID = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;

                    object[] content = new object[] { photonViewID };
                    //send to everyone but this client
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

                    //don't look any further, shield trumps all
                    return;
                }
            }
        }

        //swipe check




        for (int i = 0; i < totalRayList.Count; i++)
        {
            for (int j = 0; j < totalRayList[i].Length; j++)
            {

                // GameObject c2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //  c2.transform.position = totalRayList[i][j].point;
                //  c2.name = "hitzzz ";
                //  Destroy(c2, 3);

                //dont do anythin if it detects a hit on its own swipe object
                if (totalRayList[i][j].transform.gameObject == thisSwipeObject)
                {
                    //mmmmmmmmmmmm
                    /*
                    float d = Vector3.Distance(totalRayList[i][j].point, startingPositionsForRays[i]+ thisSwipeObject.transform.position);
                    if (d > 1f)
                    {
                        Debug.Break();

                        GameObject c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c1.transform.position = startingPositionsForRays[i] + thisSwipeObject.transform.position;
                        c1.name = "start ";
                        Destroy(c1, 3);

                        c1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        c1.transform.position = totalRayList[i][j].point;
                        c1.name = "hit ";
                        Destroy(c1, 3);

                        Debug.DrawLine(startingPositionsForRays[i] + thisSwipeObject.transform.position, totalRayList[i][j].point);
                    }
                    */
                    continue;
                }

                if (totalRayList[i][j].transform.gameObject.layer == LayerMask.NameToLayer("Swipe"))
                {
                    //only check for other overhead swipes - the side swipe straight tests cover everything else

                    //if it is this player's swipe, just knock through it
                    SwipeObject otherSwipeObjectScript = totalRayList[i][j].transform.GetComponent<SwipeObject>();
                    if (otherSwipeObjectScript.parentPlayer == thisSwipeObjectScript.parentPlayer)
                    {
                        Debug.Log("Knock through own swipe");
                        otherSwipeObjectScript.GetComponent<SwipeObject>().impactDirection = impactDirections[i];
                        otherSwipeObjectScript.GetComponent<SwipeObject>().impactPoint = totalRayList[i][j].point;
                        otherSwipeObjectScript.hitByOverhead = true;
                        otherSwipeObjectScript.DestroySwipe();

                        //no reset - other swipe will be on cooldown if it is the same player and not the active swipe

                    }
                    //else if another player's overhead , smash it,smash our own and reset players, only if we swung first
                    else if (totalRayList[i][j].transform.gameObject.GetComponent<SwipeObject>().overheadSwipe)
                    {
                        Debug.Log("Hit another swipe, player numer = " + GetComponent<PlayerInfo>().playerNumber);
                       // Debug.Log("this time start = " + thisSwipeObjectScript.swipeTimeStart + " , player numer = " + GetComponent<PlayerInfo>().playerNumber);
                        //Debug.Log("other time start = " + otherSwipeObjectScript.swipeTimeStart + " , player numer = " +otherSwipeObjectScript.parentPlayer.GetComponent<PlayerInfo>().playerNumber);
                        //smash the weaker of the two strikes ( the one that started last is weaker)
                        if (thisSwipeObjectScript.swipeTimeStart < otherSwipeObjectScript.swipeTimeStart)
                        {

                            Debug.Log("this swipe is greater than other, player numer = " + GetComponent<PlayerInfo>().playerNumber);
                         
                          
                            
                            //tell hit player to vibrate

                            PlayerVibration pV = otherSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                            pV.swipeHitTimer += pV.swipeHitLength;

                            //commenting out player who won the swipe battle, only having vibrations on hits or destroys?
                            //pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                            //pV.swipeHitTimer += pV.swipeHitLength;

                            //send resolution to network
                            byte evCode = 40; // Custom Event 40: send hit to clients
                                              
                            int photonViewID =otherSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;

                            object[] content = new object[] { photonViewID, impactDirections[i] };
                            //send to everyone but this client
                            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                            SendOptions sendOptions = new SendOptions { Reliability = true };
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);


                            otherSwipeObjectScript.DestroySwipe();
                            //reset player if it was an active strike //always is atm sipwes dont hang when this was written
                            //if (otherSwipeObjectScript.activeSwipe)
                              //  otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();


                        }
                        else if(thisSwipeObjectScript.swipeTimeStart > otherSwipeObjectScript.swipeTimeStart)
                        {
                            Debug.Log("this swipe is less than other, player numer = " + GetComponent<PlayerInfo>().playerNumber);

                            //break this swipe
                            
                           
                            //reset player if it was an active strike //always is atm sipwes dont hang when this was written
                            
                            //thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            //send resolution to network
                            byte evCode = 40; // Custom Event 40: send hit to clients
                            int photonViewID = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;
                            object[] content = new object[] { photonViewID, impactDirections[i] };
                            //send to everyone but this client
                            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                            SendOptions sendOptions = new SendOptions { Reliability = true };
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);


                           // thisSwipeObjectScript.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                            thisSwipeObjectScript.DestroySwipe();

                            //add vibrate

                        }
                        else
                        {
                            Debug.Log("similiar starts- destroy both");//this will likely never hppen because of double accuracy - force for gameplay?

                            otherSwipeObjectScript.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                            otherSwipeObjectScript.DestroySwipe();
                            otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            thisSwipeObjectScript.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                            thisSwipeObjectScript.DestroySwipe();
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            byte evCode = 40; // Custom Event 40: send hit to clients
                            int photonViewID = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;
                            object[] content = new object[] { photonViewID, Vector3.down };
                            //send to everyone but this client
                            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                            SendOptions sendOptions = new SendOptions { Reliability = true };
                            //this
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
                            //other
                            content = new object[] { photonViewID, Vector3.down };
                            photonViewID = otherSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
                            
                        }

                        //below commented code smashes both swipes if they hit each other
                        /*
                        Debug.Log("Smashing oberhead swipes, this and other");
                        thisSwipeObjectScript.DestroySwipe();
                        otherSwipeObjectScript.DestroySwipe();
                        //no directions, we want both swipes just to fall

                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                        //destroy other swipe too - we have a matched swipe - cancels each other out
                        //reset player if it was an active strike
                        if (otherSwipeObjectScript.activeSwipe)
                            otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                        //stop looking for hits, swipe got cancelled     
                        */




                        return;
                    }
                }
            }
        }


        //player check
        for (int i = 0; i < totalRayList.Count; i++)
        {
            for (int j = 0; j < totalRayList[i].Length; j++)
            {
                //dont do anythin if it detects a hit on its own swipe object
                if (totalRayList[i][j].transform.gameObject == thisSwipeObject)
                    continue;


                //side swipes checks for all swipe on swipe hits, so jump to players
                else if (totalRayList[i][j].transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
                {

                    // self hit
                    if (totalRayList[i][j].transform.gameObject == head.transform.GetChild(0).gameObject)
                    {

                        Debug.Log("overhead hit self");
                        //hitSelf = true;
                        thisSwipeObjectScript.impactDirection = (totalRayList[i][j].point - head.transform.position).normalized;
                        thisSwipeObjectScript.impactPoint = totalRayList[i][j].point;
                        thisSwipeObjectScript.hitSelf = true;
                        thisSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                        if (thisSwipeObjectScript.activeSwipe)
                        {
                            Debug.Log("Swipe self hit active - reseting");
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                        }

                        //tell network what happened

                        //send resolution to network
                        byte evCode = 41; // Custom Event 41: player hit
                        int photonViewID = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;

                        object[] content = new object[] { photonViewID, thisSwipeObjectScript.impactDirection, thisSwipeObjectScript.impactPoint };
                        //send to everyone but this client
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                        SendOptions sendOptions = new SendOptions { Reliability = true };
                        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);


                        //stop looking, we canceled the swipe with oue own body, this takes priority over an opponent hit
                        return;
                    }
                    else
                    //must be opponent - team mates? - for the future...
                    {

                        PlayerInfo playerInfo = totalRayList[i][j].transform.parent.parent.GetComponent<PlayerInfo>();
                        float p = thisSwipeObjectScript.per;
                        //if (p > 1f)
                        //    p = 1f;

                        playerInfo.health -= Mathf.RoundToInt(p);// playerClassValues.overheadHitHealthReduce; ///put back mathf round
                       //network needs to know?
                        Debug.Log("hit power = " + p);

                        //if hit player is swiping, interrupt swipe and reset him // should this only be when killed?, or allow strike to continue if only been popped
                        Swipe otherSwipeScript = totalRayList[i][j].transform.parent.parent.GetComponent<Swipe>();
                        //player got hit, cancel anything they were doing
                        otherSwipeScript.ResetFlags();
                        //network**

                        
                        if (otherSwipeScript.currentSwipeObject != null)//will be null when not swiping
                        {
                            //let player know it was cancelled with visual aid
                            otherSwipeScript.currentSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                            otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().impactDirection = -otherSwipeScript.transform.position;//not //wokring
                            otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                            //network ** 
                        }


                       
                        thisSwipeObjectScript.hitOpponent = true;
                        thisSwipeObjectScript.timeSwingFinished = PhotonNetwork.Time;

                        //for bumping player
                        GameObject parentOfHitHeadMesh = totalRayList[i][j].transform.parent.parent.gameObject;
                        PlayerMovement pMother = parentOfHitHeadMesh.GetComponent<PlayerMovement>();

                        Vector3 impactDir = Vector3.zero;

                        thisSwipeObjectScript.activeTime = thisSwipeObjectScript.playerClassValues.overheadHitCooldown;

                        if (playerInfo.health > 0 )
                        {
                            
                            //stop swipe
                            //thisSwipeObjectScript.swipeFinishedBuilding = true;
                          
                            //let player object know when we finished this swing too
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().finishTimeSriking = PhotonNetwork.Time;//should be network time?
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().waitingOnResetOverhead = true;
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().buttonSwipeAvailable = false;                            
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().hit = true;                            

                          
                            thisSwipeObjectScript.DeactivateSwipe();
                            
                            Debug.Log("health > 0 overhead");

                            //destroy this swipe
                            thisSwipeObjectScript.impactDirection = (totalRayList[i][j].point - head.transform.position).normalized;
                            //overwrite impact dir for network that we worked out above
                            impactDir = thisSwipeObjectScript.impactDirection;
                            thisSwipeObjectScript.impactPoint = totalRayList[i][j].point;                            

                            thisSwipeObject.GetComponent<SwipeObject>().DestroySwipe();
                            
                            pMother.bumped = true;
                            //hit point is on the near side, so get dir to hit transform and extend it through. point will now be on the rear side of hit transform
                            float hitBumpAmount = 1f;//*global var
                            pMother.bumpShootfrom = (parentOfHitHeadMesh.transform.position - totalRayList[i][j].point) * hitBumpAmount + parentOfHitHeadMesh.transform.position;

                            //tell hit player to vibrate
                            PlayerVibration pV = parentOfHitHeadMesh.GetComponent<PlayerVibration>();
                            pV.shakeTimerHit += pV.nonLethatHitLength;
                            //tell player who successfully hit too - just use non lethal for hit confirm
                            pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                            pV.shakeTimerHit += pV.nonLethatHitLength;
                            
                        }
                        else if (playerInfo.health <= 0f)
                        {

                            //flag for this player
                            Debug.Log("overhead hit opponent");
                            
                            
                            //reset other player
                            parentOfHitHeadMesh.GetComponent<Swipe>().ResetFlags();


                            //impact dir
                            Vector3 l0 = pointsFromCurve[pointsFromCurve.Count - 3];
                            Vector3 l1 = pointsFromCurve[pointsFromCurve.Count - 2];
                            impactDir = Vector3.Cross(l0, l1).normalized;

                            thisSwipeObjectScript.impactDirection = impactDir;

                            thisSwipeObjectScript.impactPoint = totalRayList[i][j].point;

                            BreakUpPlayer(totalRayList[i][j].transform.gameObject, thisSwipeObjectScript);
                            DeSpawnPlayer(parentOfHitHeadMesh);
                            

                            //reset this player
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            //tell hit player to vibrate
                            PlayerVibration pV = parentOfHitHeadMesh.GetComponent<PlayerVibration>();
                            pV.shakeTimerHit += pV.lethatHitLength;

                            //tell player who successfully hit too - just use non lethal for hit confirm
                            pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                            pV.shakeTimerHit += pV.nonLethatHitLength;


                            
                           
                        }

                        //gather network info
                        //send hit info to clients
                        //send resolution to network
                        // Custom Event 41: send player hit to clients
                        //i need to send, 
                        //who got hit
                        int photonViewIDVictim = otherSwipeScript.GetComponent<PhotonView>().ViewID;
                        //how powerful the hit was
                        float healthReduction = Mathf.RoundToInt(p);
                        //put destroy swipe if not null in event code
                        //send bump update too
                        Vector3 bumpShootFrom = pMother.bumpShootfrom;
                        //we also need to update the player who's hit was successful
                        int photonViewIDAttacker = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;
                        double timeSwingFinished = PhotonNetwork.Time;
                        double finishTimeStriking = PhotonNetwork.Time;
                        //update waiting on reset overhead on event code
                        //update hit bool on event code
                        //update active time on event code
                        //deactivate swipe (the swipe that hit)
                        //tell swipe it hit opponent on event code
                        //impact dir
                        
                        //impact point
                        Vector3 impactPoint = thisSwipeObjectScript.impactPoint;
                        //destroy swipe on event code

                        byte evCode = 42;//send hit player

                        object[] content = new object[] { photonViewIDVictim, healthReduction, bumpShootFrom, photonViewIDAttacker, timeSwingFinished, finishTimeStriking, impactDir, impactPoint };
                        //send to everyone but this client
                        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                        SendOptions sendOptions = new SendOptions { Reliability = true };
                        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

                        return;
                    }
                }
            }
        }

    }

    public static void BreakUpPlayer(GameObject headMesh, SwipeObject swipeObject)//just using swipeobject for its data truct (should have seperate struct within swipeobject yes
    {
        //stop calling twice
        if (headMesh.transform.parent.parent.GetComponent<PlayerInfo>().playerDespawned)
            return;

        //set flag to stop instant respawn
        headMesh.transform.parent.parent.GetComponent<PlayerInfo>().playerCanRespawn = false;
        //start timed function to enable respawn - this flag also helps camera
        PlayerClassValues playerClassValues = GameObject.FindWithTag("Code").GetComponent<PlayerClassValues>();
        headMesh.transform.parent.parent.GetComponent<PlayerInfo>().Invoke("SetSpawnAvailable", playerClassValues.respawnTime);


        //Debug.Log("breaking player");

        //remove all theplayer'sswipes


        List<GameObject> voxels = new List<GameObject>();
        //head mesh is a box, split this box in to many
        float scale = headMesh.transform.localScale.x;
        int detail = 4;//errors on 1
        float step = scale / detail;
        for (float i = -scale * .5f; i < scale * .5f; i += step)
        {
            for (float j = -scale * 0.5f; j < scale * .5f; j += step)
            {
                for (float k = -scale * 0.5f; k < scale * .5f; k += step)
                {
                    Vector3 pos = new Vector3(i + step * .5f, j + step * .5f, k + step * .5f);
                    // pos = headMesh.transform.rotation * pos;
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.parent = headMesh.transform;
                    c.transform.localPosition = Vector3.zero;
                    c.transform.position += headMesh.transform.rotation * pos;

                    c.transform.rotation = headMesh.transform.rotation;
                    c.transform.localScale *= scale / detail;
                    c.name = "Player Voxel";
                    c.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Red0") as Material;
                    //layer?
                    //Destroy(c, 5);

                    //Rigidbody rb = c.AddComponent<Rigidbody>();//adding add force
                    //add physics weights etc
                    //c.layer = LayerMask.NameToLayer("Voxel");

                    //rb.mass = 10f;


                    //now we ahve cheated by using the head mesh's transform for position and rotation, unparent voxel
                    c.transform.parent = null;

                    voxels.Add(c);
                }
            }
        }


        SwipeObject.AddForceToVoxels(voxels, swipeObject);

        //doing in despawnPlayer()
        //headMesh.GetComponent<BoxCollider>().enabled = false;
        //headMesh.GetComponent<MeshRenderer>().enabled = false;
    }
    public static void DeSpawnPlayer(GameObject parent)
    {

        //takes player off screen
        //disable all children of player
        //up two steps        
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            parent.transform.GetChild(i).gameObject.SetActive(false);
            //disable movement and attack scripts
            parent.GetComponent<PlayerMovement>().enabled = false;
            parent.GetComponent<PlayerAttacks>().enabled = false;
            parent.GetComponent<Swipe>().enabled = false;

            //set flag so we know it is disabled
            parent.GetComponent<PlayerInfo>().playerDespawned = true;

        }

        //remove current cell from player
        parent.GetComponent<PlayerInfo>().currentCell = null;
    }

    private void StartCoolDownReset()
    {
        //add current last point to central points for the swipe
       // centralPoints.Add(swipePoint);
       // centralPoints.Add(swipePoint);
       
       
       // finishTimePlanning = Time.time;

       // previousSideSwipeMagnitude = 0f;


        ResetFlags();
        /*
        //tell current swipe stuiff it needs to know
        SwipeObject sO = currentSwipeObject.AddComponent<SwipeObject>();
        sO.startTime = Time.time;
        sO.activeTime = 1f;//******VAR
        sO.parentPlayer = gameObject;
        sO.sideSwipePoints = new List<Vector3>(sideSwipePoints);
        sO.centralPoints = new List<Vector3>(centralPoints);
        */

    }

    //public so other playesrs can reset swipes
    public void ResetFlags()
    {
      //  Debug.Log("resetting");
        //tell current swipe stuiff it needs to know
   

        //finishTimePlanning = Time.time;

        overheadAvailable = true;
        sideSwipeAvailable = true;
        
       // waitingOnResetLunge = true;
        waitingOnResetButtonSwipe = false;

        pulledBackForSideSwipe = false;
        pulledBackForOverhead = false;
        waitingOnResetPlanning = false;
        planningPhaseOverheadSwipe = false;
        //need to put a flag to wait for stick reset before allowing another swipe?        
        overheadSwiping = false;
        overheadLow = false;
        overheadHigh = false;
        sideSwiping = false;
        buttonSwiping = false;
        lunging = false;
        //lowAttack = false;
        //allowingFinish = false;

        //result flags reset
        sideSwipeHit = false;
      //  overheadHit = false;
        lungeHit = false;

        sideSwipeBlock = false;
        overheadBlock = false;
        lungeBlock = false;

        sideSwipeWhiff = false;
        overheadWhiff = false;
        lungeWhiff = false;

        blocked = false;
        whiffed = false;
        hit = false;
        //fullCircle = false;


        selfPlayerHit = false;

       // debugCube.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
        
        stopwatch.Stop();

        //clear mesh
        //swiper.GetComponent<MeshFilter>().mesh.Clear();

       // if(stopwatch.ElapsedMilliseconds >0)
        //    Debug.Log("Time taken: " + (stopwatch.Elapsed));

        stopwatch.Reset();

        centralPoints.Clear();
        sideSwipePoints.Clear();

        record = false;
        playback = false;
        playbackTimeSet = false;
    }
}



