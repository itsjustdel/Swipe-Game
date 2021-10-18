using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class Swipe : MonoBehaviour {


    private PlayerGlobalInfo playerGlobalInfo;

    //classes we need access to
    public PlayerClassValues playerClassValues;    
    

    public ProceduralAudioController proceduralAudioController;
    public PlayerAttacks pA;
    public Inputs inputs;

    //game objects
    public GameObject guide;
    public GameObject head;
    public GameObject currentSwipeObject;

    //varaiables
    public float overheadWaitBeforeReset = .0f;//min at fixed update? // time to wait before allowing next strike, do we need different for every type of strike, do we need at all? *off atm
    //how long can a player move the thumbstick before attack
    public float maxPlanningTime = 1f;
   
    //flags
    public bool swiping;
    public bool overheadAvailable = true;
    public bool overheadSwiping = false;    
    public bool planningPhaseOverheadSwipe;
    public bool waitingOnResetOverhead;//still in use? review
    public bool blocked;
    public bool hit;
    public bool selfPlayerHit;

    //counters

    public double finishTimeSriking;// asigning to this but actually using it?    
    public double planningStartTime;//used for max planning time of user input for swipe


    public System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

    //positions    
    public Vector3 firstPullBackLookDir;//can all occurences be changed to centralpoints[0]?    //no, using firstPullBack to send over network so we don't need to send a full list
    public Vector3 swipePoint;
    public int previousSwipePointsAmount = 1;
    public List<Vector3> previousSwipePoints = new List<Vector3>();
    
    //debug helpers
    public float previousAngle;
    public float startAngle;
    public float startAngleRelative;
    

    //curve
    public BezierSpline spline;    
    public List<Vector3> centralPoints = new List<Vector3>();

    //floats    
    
    private float yAdd;//add height to 2d thumbstick input
    public float curveSmoothing = 1f;
    public float arcDetail = .05f;//changing this affects overhead speed (great!) perhaps multiply overhead speeed by this so stays consistent? //needs looked at, unsure of effect
    public float dragSize = 20;//how many points pass before we recede swing  linked with arc detail //perhaps needing review along with above

    //test stuff
    //private List<Vector3> testPoints = new List<Vector3>() { new Vector3(1, 0, 1), new Vector3(1, 0, 1, new Vector3(1, 0, 0, new Vector3(-1, 0, 0, new Vector3(-1, 0, 1, new Vector3(-1, 0, 1};

    
    private void Start()
    {
        playerGlobalInfo = GameObject.FindWithTag("Code").GetComponent<PlayerGlobalInfo>();
        
        inputs = GetComponent<Inputs>();

      //  head = transform.Find("Head").gameObject; //asigned on spawner

        UpdateValues();

        stopwatch = new System.Diagnostics.Stopwatch();

        pA = transform.GetComponent<PlayerAttacks>();

        //instantiate guide objectif we are local player
        if (GetComponent<PhotonView>().IsMine)
        {
            guide = Guide.GenerateGuide(this);
        }
        //disable this script if not for our player - we don not need to check for swipes from other players
        else
            enabled = false;


    }


    void UpdateValues()
    {
        //grab variables from Code object
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();

    }

    //called from fixed update in PlayerAttacks
    public void SwipeOrder()
    {
        GetSwipePoint();

        //check for overhead reset
        ButtonOverHeadReset();
        
        bool adjustingCellHeight = false;            
        if (GetComponent<CellHeights>().loweringCell || GetComponent<CellHeights>().raisingCell)
            adjustingCellHeight = true;

        //cant swipe if blocking or adjusting or in cooldown after trying to start a swipe to close to another player
        if (!pA.blocking && !adjustingCellHeight)
        {
            //look for user input and determine which swing to start

            //SwipePlanning();
            ButttonOverhead();
        }
        else if (pA.blocking || !adjustingCellHeight)
        {
            //Debug.Log("Resetting from blocking or not adjusting cell height");
            ResetFlags();                         
        }
    }
    
    void GetSwipePoint()
    {
        //save for sound script to know how far swipe point has moved
        previousSwipePoints.Add(swipePoint);
        if (previousSwipePoints.Count > previousSwipePointsAmount)
            //remove first element, only keeping the last x positions. x = stillStickFrameLimit
            previousSwipePoints.RemoveAt(0);

        previousAngle = startAngle;
        startAngle = MovementHelper.SignedAngle(pA.lookDirRightStick, transform.forward, Vector3.up);
        
        //get magnitude and clamp it
        float swingMagnitude = pA.lookDirRightStick.magnitude ;
        if (swingMagnitude > 1f)
            swingMagnitude = 1f;

        //when swiper is active, add any left over magnitude to height, this will create overhead strikes if user strikes through center of analog stick
        yAdd = .85f - swingMagnitude;//makes sure it goes to 0 on y 
        if (yAdd < 0f)
            yAdd = 0f;
        //create cureve from this linear paramater
        yAdd = Easings.CubicEaseOut(yAdd);
        
        swipePoint = pA.lookDirRightStick * swingMagnitude + Vector3.up * (yAdd);
    }

    void ButttonOverhead()
    {
        //check user as released attack button after attacking

        //start swipe planning if attack button is pressed, stop planning when button is released
        if(inputs.attack0 && overheadAvailable)
        {
            //gather points
            if (!planningPhaseOverheadSwipe)
            {
                //start time to control how long a player can plan for - otherwise they can make extreme curves
                planningPhaseOverheadSwipe = true;
                planningStartTime = PhotonNetwork.Time;

                //reset list
                centralPoints.Clear();
                
            }
            //populate list with joystick input
            firstPullBackLookDir = swipePoint;
            StickPathOverhead();
        }
        //stop strike if

        bool stopPlanning = false;

        if (planningPhaseOverheadSwipe)
        {
            //if we have started a swipe
            if (centralPoints.Count > 2)
            {
                //if attack button released or we get to max input time or if right stick was still for too long
                if (!inputs.attack0 || PhotonNetwork.Time - planningStartTime >= maxPlanningTime )
                    //send to render
                    stopPlanning = true;
            }
            //if button was pressed but no input from thumbstick, look to cancel
            else
            {
                if(!inputs.attack0)
                {
                    centralPoints.Clear();
                    planningPhaseOverheadSwipe = false;
                    //player held attack
                }
            }
        }
        
        if(stopPlanning)
        {
            //we have started swinging
            overheadAvailable = false;

            //if strike is coming down at the end, extend points to floor (overheadSmash!)
            bool overheadSmash = false;
            //check for overhead smash
            // GameObject c = null;
            /*
            for (int i = centralPoints.Count - 2; i < centralPoints.Count; i++)
            {
                //   c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //   c.transform.position = centralPoints[i]*5 + head.transform.position;
                //   Destroy(c, 3);
            }
            */
             
            //check angle of last two points            
            //if second last point is high enough - consider it an overhead smash
            if (centralPoints[centralPoints.Count - 2].y - centralPoints[centralPoints.Count - 1].y > 0.5f)
            {
                overheadSmash = true;
                //c.name = "Smash!";
                //move the last point down to the ground
                centralPoints[centralPoints.Count - 1] -= Vector3.up * .5f;
                //and also add right underneath player- this will make it swing over walls - we will check for walls/cells on swipe hit checks to stop it going through

                centralPoints[centralPoints.Count - 1].Normalize();
                centralPoints.Add(-Vector3.up);
            }

            centralPoints.Add(centralPoints[centralPoints.Count - 1]);//makes sure it alwys goes to end
            

            planningPhaseOverheadSwipe = false;
            //start swipe being rendered and being checked for any hits
            overheadSwiping = true;

            //set walk speed from here
            PlayerMovement pM = GetComponent<PlayerMovement>();
            pM.walkSpeedThisFrame = pM.walkSpeedWhileAttacking;
            pM.walkStart = PhotonNetwork.Time;
            pM.walkStartPos = transform.position;


            CreateNewSwipeObject("Overhead", true, false, false, overheadSmash);
            currentSwipeObject.GetComponent<SwipeObject>().activeSwipe = true;
            currentSwipeObject.GetComponent<SwipeObject>().overheadSwipe = true;

        }
    }

    void ButtonOverHeadReset()
    {
        //check user has released attack button after swiping
        if(!overheadSwiping && waitingOnResetOverhead)
        {
            if(!inputs.attack0)
            {
                waitingOnResetOverhead = false;
                overheadAvailable = true;
            } 
        }
    }

    void CreateNewSwipeObject(string type, bool overhead, bool sideSwipe, bool buttonSwipe,bool straightFinish)
    {
        Debug.Log("creating new swipe object - own player");
        //Debug.Log("frac complete after swipe obj = " + GetComponent<PlayerMovement>().fracComplete);

        GameObject newSwipe = new GameObject();
        newSwipe.name = "swipe Current " + type;
        newSwipe.AddComponent<MeshFilter>();
        newSwipe.AddComponent<MeshRenderer>();
        newSwipe.transform.position = head.transform.position;// swipePosition;
        newSwipe.layer = LayerMask.NameToLayer("Swipe");
        newSwipe.AddComponent<MeshCollider>();

        //keep track of this swipe
        currentSwipeObject = newSwipe;

        SwipeObject sO = newSwipe.AddComponent<SwipeObject>();
        sO.parentPlayer = gameObject;
        sO.firstPullBackLookDir = firstPullBackLookDir;

        sO.playerClassValues = playerClassValues;
        //sO.activeTime = playerClassValues.overheadWhiffCooldown;
        sO.firstPullBackLookDir = firstPullBackLookDir;
        sO.swipeTimeStart = Photon.Pun.PhotonNetwork.Time;

        if (overhead) //always
        {
            sO.overheadSwipe = true;            
            sO.centralPoints = new List<Vector3>(centralPoints);
        }

        //audio
        ProceduralAudioController pAC = newSwipe.AddComponent<ProceduralAudioController>();
        pAC.swipeObject = true;
        pAC.useSinusAudioWave = true;
      //  pAC.useSawAudioWave = true;
        pAC.useSquareAudioWave = true;
        pAC.sinusAudioWaveIntensity = 1f;
        //pAC.squareAudioWaveIntensity = 0.186f;
        pAC.sawAudioWaveIntensity = 0.1f;
        pAC.useAmplitudeModulation = true;
        pAC.amplitudeModulationOscillatorFrequency = 8f;

        AudioSource aS = newSwipe.AddComponent<AudioSource>();
        UnityEngine.Audio.AudioMixer mixer = Resources.Load("Sound/SwipeMixer") as UnityEngine.Audio.AudioMixer;
        aS.outputAudioMixerGroup = mixer.FindMatchingGroups("Master/SwipeObjects")[0];
        
    }

    void StickPathOverhead()
    {
        if (centralPoints.Count > 0)
        {
            float d = Vector3.Distance(swipePoint, centralPoints[centralPoints.Count - 1]);
            // Debug.Log(d);
            //larger numbers mean more smoothing for the curve (basically it gives less points to the curve
            if (d >= curveSmoothing) //** still working out what's best for this
            {
                centralPoints.Add(swipePoint.normalized); //** revise?
            }
        }
        else
        {
            //working ok?
            centralPoints.Add(firstPullBackLookDir.normalized);
            centralPoints.Add(firstPullBackLookDir.normalized);
        }
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
        LayerMask mask = LayerMask.GetMask("PlayerBody", "Shield", "Swipe","Cells","Wall");
        if (activeSwipe == false)
            mask = LayerMask.GetMask("PlayerBody", "Shield");


        //first do self test at fron of swipe, if user does a crossover strike, cancel swipe (or advacned idea, snap off rear of strike and continue)
        // Vector3 l0Self = pointsFromCurve[pointsFromCurve.Count - 3];
        // Vector3 l1Self = pointsFromCurve[pointsFromCurve.Count - 2];





        // GameObject c0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //c0.transform.position = lastCentre;




        //if (pointsFromCurve.Count > 24 * 3)

        bool doSelfCheck = false; //if set to true, overhead available needs reset if self hit occurs
        if (doSelfCheck)

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
                        
                        blocked = true;

                        //start timer for reset
                        //thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                    }

                    //tell hit player to vibrate
                    GameObject parentOfHitHeadMesh = totalRayList[i][j].transform.parent.parent.parent.gameObject;
                    PlayerVibration pV = parentOfHitHeadMesh.GetComponent<PlayerVibration>();
                    pV.shakeTimerShield += pV.shieldHitLength;
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


                    //if it is this player's swipe, just knock through it
                    SwipeObject otherSwipeObjectScript = totalRayList[i][j].transform.GetComponent<SwipeObject>();

                    if (otherSwipeObjectScript.parentPlayer == thisSwipeObjectScript.parentPlayer)
                    {
                        /*
                        Debug.Log("Knock through own swipe");
                        otherSwipeObjectScript.GetComponent<SwipeObject>().impactDirection = impactDirections[i];
                        otherSwipeObjectScript.GetComponent<SwipeObject>().impactPoint = totalRayList[i][j].point;
                        otherSwipeObjectScript.hitByOverhead = true;
                        otherSwipeObjectScript.DestroySwipe();

                        //no reset - other swipe will be on cooldown if it is the same player and not the active swipe
                        */

                        //not doing anything - currently in a state where only the very end and the very start of a swipe will touch - im allowing this overlap to chain together swipes

                    }
                    //else if another player's overhead , smash it,smash our own and reset players, only if we swung first
                    else if (totalRayList[i][j].transform.gameObject.GetComponent<SwipeObject>().overheadSwipe)
                    {
                        Debug.Log("Hit another swipe, player numer = " + GetComponent<PlayerInfo>().teamNumber);
                        // Debug.Log("this time start = " + thisSwipeObjectScript.swipeTimeStart + " , player numer = " + GetComponent<PlayerInfo>().playerNumber);
                        //Debug.Log("other time start = " + otherSwipeObjectScript.swipeTimeStart + " , player numer = " +otherSwipeObjectScript.parentPlayer.GetComponent<PlayerInfo>().playerNumber);
                        //smash the weaker of the two strikes ( the one that started last is weaker)
                        if (thisSwipeObjectScript.swipeTimeStart < otherSwipeObjectScript.swipeTimeStart)
                        {

                            Debug.Log("this swipe is greater than other, player numer = " + GetComponent<PlayerInfo>().teamNumber);



                            //tell hit player to vibrate

                            PlayerVibration pV = otherSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                            pV.swipeHitTimer += pV.swipeHitLength;

                            //commenting out player who won the swipe battle, only having vibrations on hits or destroys?
                            //pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                            //pV.swipeHitTimer += pV.swipeHitLength;

                            //send resolution to network
                            byte evCode = 40; // Custom Event 40: send hit to clients

                            int photonViewID = otherSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;

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
                        else if (thisSwipeObjectScript.swipeTimeStart > otherSwipeObjectScript.swipeTimeStart)
                        {
                            Debug.Log("this swipe is less than other, player numer = " + GetComponent<PlayerInfo>().teamNumber);

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
                            Debug.Log("similiar starts- destroy both");//this will likely never hppen because of double accuracy - force for gameplay? - happens with testing mastr and client with same pad

                            //Debug.Break();
                            // otherSwipeObjectScript.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;//on destroy swipe
                            otherSwipeObjectScript.DestroySwipe();
                            otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            // thisSwipeObjectScript.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                            thisSwipeObjectScript.DestroySwipe();
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            byte evCode = 40; // Custom Event 40: send hit to clients
                            SendOptions sendOptions = new SendOptions { Reliability = true };
                            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                            //this
                            int photonViewID = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;
                            object[] content = new object[] { photonViewID, Vector3.down };
                            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

                            //other                            
                            photonViewID = otherSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;
                            content = new object[] { photonViewID, Vector3.down };
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

                        PlayerInfo hitPlayerInfo = totalRayList[i][j].transform.parent.parent.GetComponent<PlayerInfo>();
                        float p = thisSwipeObjectScript.per;
                        //if (p > 1f)
                        //    p = 1f;

                        hitPlayerInfo.health -= Mathf.RoundToInt(p);// playerClassValues.overheadHitHealthReduce; ///put back mathf round
                                                                 //network needs to know?
                        Debug.Log("hit power = " + p);

                        Swipe otherSwipeScript = totalRayList[i][j].transform.parent.parent.GetComponent<Swipe>();




                        thisSwipeObjectScript.hitOpponent = true;
                        thisSwipeObjectScript.timeSwingFinished = PhotonNetwork.Time;

                        //for bumping player
                        GameObject parentOfHitHeadMesh = totalRayList[i][j].transform.parent.parent.gameObject;
                        PlayerMovement pMother = parentOfHitHeadMesh.GetComponent<PlayerMovement>();

                        Vector3 impactDir = Vector3.zero;

                        //thisSwipeObjectScript.activeTime = thisSwipeObjectScript.playerClassValues.overheadHitCooldown;

                        if (hitPlayerInfo.health > 0)
                        {
                            bool interruptSwipeOnNonLethal = false;
                            if (interruptSwipeOnNonLethal)
                            {
                                InterruptSwipe(otherSwipeScript);
                            }


                            //stop swipe
                            //thisSwipeObjectScript.swipeFinishedBuilding = true;

                            //let player object know when we finished this swing too
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().finishTimeSriking = PhotonNetwork.Time;//should be network time?
                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().waitingOnResetOverhead = true;
                            
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
                        else if (hitPlayerInfo.health <= 0f)
                        {
                            //stop other player's swipe if he is swiping
                            bool interruptSwipeOnLethal = true;
                            if (interruptSwipeOnLethal)
                            {
                                InterruptSwipe(otherSwipeScript);
                            }

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

                            //strip cell from player we just killed 
                            parentOfHitHeadMesh.GetComponent<PlayerInfo>().cellsUnderControl.Remove(GetComponent<PlayerInfo>().currentCell);
                            //force a recheck on cells by making this null- will upate when this happens
                            GetComponent<PlayerInfo>().currentCell = null;

                            //reset this player// yeah? - shouldnt swipe do this when finsihed?
                            //thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

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

        //cell/walls check
        for (int i = 0; i < totalRayList.Count; i++)
        {
            for (int j = 0; j < totalRayList[i].Length; j++)
            {
                
                if(totalRayList[i][j].transform.gameObject.layer == LayerMask.NameToLayer("Cells") || totalRayList[i][j].transform.gameObject.layer == LayerMask.NameToLayer("Wall"))
                {
                    Debug.Log("Hit Cerll or wall ( overhead )");

                    thisSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;

                    thisSwipeObjectScript.hitShield = true;
                    thisSwipeObjectScript.DestroySwipe();

                    if (thisSwipeObjectScript.activeSwipe)
                    {
                        finishTimeSriking = PhotonNetwork.Time;
                        waitingOnResetOverhead = true;
                        
                        

                        //start timer for reset
                        //thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                    }

                    //tell player to vibrate
                    
                    
                    //tell player who hit to vibrate - just use non lethal for hit confirm
                    PlayerVibration pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                    pV.shakeTimerShield += pV.shieldHitLength;

                    //send resolution to network
                    byte evCode = 43; // Custom Event 43: send shield hit to clients //REUSING SHIELD HIT EVENT

                    int photonViewID = thisSwipeObjectScript.parentPlayer.GetComponent<PhotonView>().ViewID;

                    object[] content = new object[] { photonViewID };
                    //send to everyone but this client
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

                    return;
                }
            }
        }
    }


    void InterruptSwipe(Swipe otherSwipeScript)
    {
        //if hit player is swiping, interrupt swipe and reset him // should this only be when killed? - YES, or allow strike to continue if only been popped

       
        //player got hit, cancel anything they were doing
        otherSwipeScript.ResetFlags();




        if (otherSwipeScript.currentSwipeObject != null)//will be null when not swiping
        {
            //let player know it was cancelled with visual aid
           // otherSwipeScript.currentSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material; //on destroy swipe
            otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().impactDirection = -otherSwipeScript.transform.position;//not //wokring
            otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

            //network ** 
        }
    }

    public static void BreakUpPlayer(GameObject headMesh, SwipeObject swipeObject)//just using swipeobject for its data truct (should have seperate struct within swipeobject yes
    {
        //stop calling twice
        if (headMesh.transform.parent.parent.GetComponent<PlayerInfo>().playerDespawned)
            return;

        //set flag to stop instant respawn
       // headMesh.transform.parent.parent.GetComponent<PlayerInfo>().playerCanRespawn = false;
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

            //reset player current cell to force a recheck of cells captured
            parent.GetComponent<PlayerInfo>().currentCell = null;

            //set time of death
            parent.GetComponent<PlayerInfo>().lastDeathTime = PhotonNetwork.Time;

        }

        //remove current cell from player
        parent.GetComponent<PlayerInfo>().currentCell = null;


    }
    
    //public so other playesrs can reset swipes
    public void ResetFlags()
    {
        
        planningPhaseOverheadSwipe = false;
        //need to put a flag to wait for stick reset before allowing another swipe?        
        overheadSwiping = false;        

        blocked = false;
        
        hit = false;        
        selfPlayerHit = false;
        

        stopwatch.Stop();
        stopwatch.Reset();

        centralPoints.Clear();
        
    }
}



