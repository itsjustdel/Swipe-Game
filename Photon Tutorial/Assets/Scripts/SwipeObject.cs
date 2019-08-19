using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class SwipeObject : MonoBehaviourPunCallbacks { 


    public GameObject parentPlayer;
    public List<Vector3> centralPoints = new List<Vector3>();
    public List<Vector3> sideSwipePoints = new List<Vector3>();
    public List<Vector3> lungePointsFinal = new List<Vector3>();
    public BezierSpline spline;
    public float finishTimePlanning;
    public float swordStart;
    public float swordLength;
    //public float swordWidth;
    public float weaponSpeed;

    public bool overheadSwipe;
    public bool sideSwipe;
    public bool lunge;
    public bool buttonSwipe;

    public bool hitByLunge = false;
    public bool hitBySideSwipe = false;
    public bool hitByOverhead = false;
    public bool hitSelf = false;
    public bool sideSwipeFlipped = false;
    public bool hitOpponent = false;
    public bool hitShield = false;


    public Mesh dataMesh;// simple mesh used for raycasting
    public Vector3 firstPullBackLookDir;

    public bool swiping;
    public bool waitingOnResetPlanning;

    //public GameObject head;

    public float timeSwingFinished;
    public float activeTime = 100;
    //attach this script to a swipe patter, it will render it and check for hits
    public List<Vector3> originalVertices;

    public bool destroySwipe;

    public Vector3 impactPoint = Vector3.zero;
    public Vector3 impactDirection = Vector3.zero;

    public bool flip;
    // Use this for initialization

    public bool saveMesh;
    public bool subdivide;
    public bool testSplit;
    public bool dontkill;
    public bool destroyingInProgress = false;

    public bool activeSwipe = false;

    public List<GameObject> voxels = new List<GameObject>();

    //for render
    public double swipeTimeStart;

    public PlayerClassValues playerClassValues;
    public bool swipeFinishedBuilding = false;
    private Vector3 playerOriginalPosition;

    //how far we have went around swipe - master client can change this value to catch up on time
    public float arrayRenderCount = 0;
    int startRenderCount = 0;

    public float per;//how far the swipe has made it through its animation

    PhotonView thisPhotonView;
    public bool local = true;
    private void Awake()
    {
       // enabled = false;
    }

    void Start ()
    {
        //lets network client catch up to when the local client started swipe
        CalculateStartArrayRenderCount();

        //find network photon view
        thisPhotonView = parentPlayer.GetComponent<PhotonView>();
        //only control our own player - the network will move the rest
     

        playerOriginalPosition = parentPlayer.transform.position;

        spline = gameObject.AddComponent<BezierSpline>();
        //head = parentPlayer.GetComponent<Swipe>().head;

        //detect if we need to flip some directions - if starting point is the irght hand side of th eplayer we do(relatively)
        
        if (sideSwipe || overheadSwipe)
        {
            //script needs to know if it should side direction, used to split in to voxels
            float flipAngle = MovementHelper.SignedAngle( firstPullBackLookDir, parentPlayer.transform.forward, Vector3.up);
         //   Debug.Log("Flip angle = " + flipAngle);
            bool flipTemp = true;
            if (flipAngle < 0)
                flipTemp = false;

            flip = flipTemp;
            
        } 
        
        if (lunge)
        {
            //all lunges need flipped
            flip = true;
        }


        // local call to instantiate this wipe across network
        if(local)
            SendToNetwork();

    }

    void CalculateStartArrayRenderCount()
    {
        //use swipe time start to figure out how many fixed updates have passed
        double timeNow = PhotonNetwork.Time;

        //how much time has passed? //** network time can switch from positive to negative, how to check? so, time start can be positive, and time now can be negative//how often can this happen?
        if(timeNow < 0 && swipeTimeStart > 0)
        {
            Debug.Break();
            Debug.Log("Photon network time flipped - will have to wait for authorative interjection ");
            swipeTimeStart = timeNow;
        }
        double timePassed = timeNow - swipeTimeStart;
        //how many steps have passed in this time
        double stepsPassed = timePassed / Time.fixedDeltaTime;
        //add
        arrayRenderCount = (float)stepsPassed;
    }

    void SendToNetwork()
    {
        // when this object is created, we need to tell every player (and the master client) about it
        // we will instantiate it on every machine
        // to do this will send all machines the data we will use to create the object
        // what data do we need?
        // swipes are made from a set of points which are used to create a curve "centralPoints" 
        // we need to what time the swipe was started "swipeTimeStart"

        byte evCode = 20; // Custom Event 20: Used as "Instantiate Swipe Object" event
        //enter the data we need in to an object array to send over the network
        int photonViewID = parentPlayer.GetComponent<PhotonView>().ViewID;        

        object[] content = new object[] { swipeTimeStart, firstPullBackLookDir, centralPoints.ToArray(), photonViewID}; 
        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others }; 
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

    }

    // Update is called once per frame
    void FixedUpdate ()
    {
        if (swipeFinishedBuilding)
        {
            if (Time.time - activeTime > timeSwingFinished)
            {

                //find this object in swipe list and remove it
                // List<GameObject> currentSwipes = parentPlayer.GetComponent<Swipe>().currentSwipes;
                //  for (int i = 0; i < currentSwipes.Count; i++)
                {
                    // if (currentSwipes[i] == gameObject)
                    //     currentSwipes.RemoveAt(i);
                }
                Debug.Log("destroying on timeout");
                Destroy(this.gameObject);

            }
        }
       
        if (overheadSwipe)
        {
            if (!swipeFinishedBuilding)
                RenderOverHead();

            if (!hitOpponent)
            {
                Mesh mesh = GetComponent<MeshFilter>().mesh;
                List<Vector3> verticesToCheck = new List<Vector3>(mesh.vertices);
                parentPlayer.GetComponent<Swipe>().CurveHitCheck2(verticesToCheck, activeSwipe, gameObject, true, false);
            }
        }

        if(buttonSwipe)
        {

            if (!swipeFinishedBuilding)
                RenderButtonSwipe();

            if (!hitOpponent)
            {
                Mesh mesh = GetComponent<MeshFilter>().mesh;
                List<Vector3> verticesToCheck = new List<Vector3>(mesh.vertices);
                StraightSwipeHitCheck2(verticesToCheck, activeSwipe, this.gameObject, false, true);
            }
        }

        if (destroySwipe)
        {
            // DestroySwipeStraightEdge();
            // destroySwipe = false;
            // GetComponent<MeshRenderer>().enabled = false;
            //Debug.Break();

        }

        if (saveMesh)
        {
            #if UNITY_EDITOR
            saveMesh = false;
            UnityEditor.AssetDatabase.CreateAsset(GetComponent<MeshFilter>().mesh, "Assets/saveMesh.mesh");
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
        }

        if (subdivide)
        {
            Mesh newMesh = new Mesh();
            newMesh = MeshHelper.SubdivideStatic(GetComponent<MeshFilter>().mesh, 2);
            GetComponent<MeshFilter>().mesh = newMesh;

            subdivide = false;
        }

        if (testSplit)
        {
            TestSplit();
            //   testSplit = false;
        }
	}

    public void RenderOverHead()
    {

        List<Vector3> pointsFromCurve = new List<Vector3>();

        //create mesh and return the points created by the curve. We will use these points to hit check the strike
        Mesh mesh = RenderCurve(out pointsFromCurve, centralPoints);


        GetComponent<MeshFilter>().mesh = mesh;
        //last sliver will break physics- 2d slice
        if(mesh.vertexCount > 16)//check this number?
            GetComponent<MeshCollider>().sharedMesh = mesh;

        List<Vector3> verticesToCheck = new List<Vector3>(mesh.vertices);
        GetComponent<SwipeObject>().originalVertices = verticesToCheck;
        // CurveHitCheck2(verticesToCheck, true, currentSwipeObject,true,false);//done on object
    }

    Mesh RenderCurve(out List<Vector3> pointsFromCurveReturning, List<Vector3> passedCurvePoints)
    {
        //do we need to work out curve points every frame?(unless i inted to influence last curve point with movement, no)

        List<Vector3> pointsFromCurve = new List<Vector3>();
        List<Vector3> directions = new List<Vector3>();

        // lastSwipePoint = swipePoint;
        //debugCube.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;



        //extend cruve in diretion of walk

        if (passedCurvePoints.Count < 3)
        {
            //   Debug.Log("Less than three");
            //   pointsFromCurveReturning = null;
            //   return null;

        }

        List<Vector3> curvePoints = new List<Vector3>();
        //curvePoints.Add(passedCurvePoints[0]);
        // curvePoints.Add(passedCurvePoints[0]);//booken
        //curvePoints.Add(passedCurvePoints[0]);

        //we now havea  uniform set of points  -we can create a smooth curve between them now

      


            //we now havea  uniform set of points  -we can create a smooth curve between them now
            //start at 1 to jump the bookends used to define the curve shape at the start
            for (int i = 1; i < passedCurvePoints.Count - 1; i++)
        {

            Vector3 p0 = Vector3.Lerp(passedCurvePoints[i - 1], passedCurvePoints[i], 0.5f);
            Vector3 cp0 = Vector3.Lerp(passedCurvePoints[i - 1], passedCurvePoints[i], .5f);
            Vector3 cp1 = Vector3.Lerp(passedCurvePoints[i], passedCurvePoints[i + 1], 0f);

            curvePoints.Add(p0);

            if (curvePoints.Count == 1)
            {
                //  curvePoints.Add(passedCurvePoints[0]);
                //  curvePoints.Add(passedCurvePoints[0]);//booken

            }
            curvePoints.Add(cp0);
            curvePoints.Add(cp1);
        }

        //add last, extra to control shape
        curvePoints.Add(passedCurvePoints[passedCurvePoints.Count - 1]);
        curvePoints.Add(passedCurvePoints[passedCurvePoints.Count - 1]);
        curvePoints.Add(passedCurvePoints[passedCurvePoints.Count - 1]);

        for (int a = 0; a < curvePoints.Count; a++)
        {
            //   GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //    c.transform.position = curvePoints[a] + head.transform.position;
            //  c.name = "Curve point";
            // Destroy(c, 3);

        }


        if (GetComponent<BezierSpline>() == null)
        {
            spline = gameObject.AddComponent<BezierSpline>();
        }
        else
            spline = GetComponent<BezierSpline>();


        spline.points = curvePoints.ToArray();

        float freq = 500f;//the higher this is the more chance of even voxels
        float step = 1f / freq;

        for (int i = 0; i < freq; i++)
        {
            Vector3 p0 = spline.GetPoint(i * step);
            //Vector3 p1 = spline.GetPoint((i + 1) * step);

            //Debug.DrawLine(p0, p1);

            //1. solutions to this, make phyiscs and visual layer different
            //2. instantiate break of swipe in coroutine prior to break up and just switch physics layer at time of hit - mesh collider add to the world is the slow down
            //always add first
            float arcDetail = parentPlayer.GetComponent<Swipe>().arcDetail;
            if (pointsFromCurve.Count == 0)
            {
                pointsFromCurve.Add(p0 - transform.position); //bezierspline class automatically adds transform.position
                directions.Add(spline.GetDirection((i + 1) * step));//first step's direction is zero... just use next step, seems to work ok
                /*
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = p0 ;

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = p0 + spline.GetDirection((i+1) * step).normalized;
                c.transform.localScale *= 0.5f;
                */
            }
            else if (Vector3.Distance(p0 - transform.position, pointsFromCurve[pointsFromCurve.Count - 1]) > arcDetail)
            {




                //bug in how i made the curve. points can sit on top of each other? creating zero direcions //just skip these. arc detail should be high enough to handle the odd skip
                if (spline.GetDirection(i * step).magnitude != 0f)
                {
                    // Debug.DrawLine(p0, pointsFromCurve[pointsFromCurve.Count - 1] + currentSwipeObject.transform.position);

                    //add player movement



                    //  float x = (parentPlayer.transform.position.x - playerOriginalPosition.x);
                    //float z = (parentPlayer.transform.position.z - playerOriginalPosition.z);
                    Vector3 pos = p0;// + new Vector3(x, 0f, z);
                    


                    pointsFromCurve.Add(pos - transform.position); //bezierspline class automatically adds transform.position
                    directions.Add(spline.GetDirection(i * step));

                    // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);                    
                    /// c.transform.position = p0 + spline.GetDirection(i * step).normalized;
                    //// c.transform.localScale *= 0.5f;
                    //  c.name = "points from curve 2nd loop";
                    //  Destroy(c, 3);
                    //  Debug.Break();
                }
            }
        }

        //make sure it goes to end of curve - not sure of necessary
        pointsFromCurve.Add(pointsFromCurve[pointsFromCurve.Count - 1]);
        directions.Add(directions[directions.Count - 1]);

        //keep track of how far we have made it round the mesh. we will update it every frame
       // int p = arrayRenderCount;

        //give audio an indication how far we are in creting swipe///***needs to be percentage
        GetComponent<ProceduralAudioController>().swipeObjectDistance = 1f/arrayRenderCount;//??? not testesd

        //create box/mesh points from central spine of points
        List<Vector3> swipePoints = new List<Vector3>();

        //vertices
        //Debug.Log("points total = " + pointsFromCurve.Count + ",p = " + p);


        for (int a = startRenderCount; a < arrayRenderCount ;a++)
        {
            int i = a;
            if (i > pointsFromCurve.Count - 1)
                i = pointsFromCurve.Count - 1;

            // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            // c.transform.position = pointsFromCurve[i] + head.transform.position;
            // c.name = "Curve point";
            // Destroy(c, 3);


           // Debug.Log(i.ToString() + " count =" + pointsFromCurve.Count);

            //get perpendiular vector using .cross
            Vector3 spunDir = Vector3.Cross(-pointsFromCurve[i], directions[i]).normalized * playerClassValues.swordWidth * 0.5f;

            Vector3 dirToEnd = (pointsFromCurve[i]).normalized;
            Vector3 closePoint = dirToEnd * (playerClassValues.armLength);

            //make longer as strike goes on - using some cobbled together math stuff to make strike get longer as it goes on - 
            // p is worked out from percentage of strike completed, 
            //combine with how far we have got through this loop, makes it smooth
            //float percentageOfStrikeCompleted = p * (float)(i) / pointsFromCurve.Count;//removing, test

            Vector3 endPoint = dirToEnd * (playerClassValues.overheadLength + playerClassValues.armLength + playerClassValues.swordLength);



            //first side
            swipePoints.Add(closePoint + spunDir);
            swipePoints.Add(endPoint + spunDir);

            //top/outer
            swipePoints.Add(endPoint + spunDir);
            swipePoints.Add(endPoint - spunDir);

            //other side
            swipePoints.Add(endPoint - spunDir);
            swipePoints.Add(closePoint - spunDir);

            //inside/bottom
            swipePoints.Add(closePoint - spunDir);
            swipePoints.Add(closePoint + spunDir);

            


        }
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices = swipePoints;

        //triangles
        for (int i = 0; i < vertices.Count - 8; i += 8)
        {

            triangles.Add(i);
            triangles.Add(i + 8);
            triangles.Add(i + 1);


            triangles.Add(i + 1);
            triangles.Add(i + 8 + 0);//is ring size
            triangles.Add(i + 8 + 1);

            //top

            triangles.Add(i + 2);
            triangles.Add(i + 8 + 2);
            triangles.Add(i + 3);

            triangles.Add(i + 3);
            triangles.Add(i + 8 + 2);
            triangles.Add(i + 8 + 3);

            //back side

            triangles.Add(i + 4);
            triangles.Add(i + 8 + 5);
            triangles.Add(i + 5);

            triangles.Add(i + 8 + 5);
            triangles.Add(i + 4);
            triangles.Add(i + 8 + 4);

            //bottom
            triangles.Add(i + 6);
            triangles.Add(i + 8 + 7);
            triangles.Add(i + 7);

            triangles.Add(i + 8 + 7);
            triangles.Add(i + 6);
            triangles.Add(i + 8 + 6);

        }

        if (vertices.Count > 24)
        {
            //add front and back panels
            //front
            vertices.Add(vertices[0]);//-4
            vertices.Add(vertices[1]);//-3
            vertices.Add(vertices[3]);//-2
            vertices.Add(vertices[5]);//-1

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);

            //back

            //a bit mental, adding and counting backwards too, afterthought much
            vertices.Add(vertices[vertices.Count - 1 - 4]);//-4
            vertices.Add(vertices[vertices.Count - 2 - 4 - 1]);//-2
            vertices.Add(vertices[vertices.Count - 3 - 4 - 3]);//-1
            vertices.Add(vertices[vertices.Count - 4 - 4 - 5]);//-1

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);

            //underside
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //  c.name = i.ToString();
            //  c.transform.position = head.transform.position + vertices[i];
            //   c.transform.localScale *= 0.33f;
            //  Destroy(c, 2);
        }
        // Debug.Break();
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 1;


        mesh.SetTriangles(triangles.ToArray(), 0);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        if(parentPlayer.GetComponent<PlayerInfo>().playerNumber == 0)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan0") as Material;
        else if (parentPlayer.GetComponent<PlayerInfo>().playerNumber == 1)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
        else if (parentPlayer.GetComponent<PlayerInfo>().playerNumber == 2)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;

        pointsFromCurveReturning = pointsFromCurve;

        //used for animating swipe finishing/receding
        if (arrayRenderCount >=  pointsFromCurve.Count * parentPlayer.GetComponent<Swipe>().dragSize)
            startRenderCount++;
        //check for whiff, if strike has made it all the way to the end and not hit anything
        //if (Time.time - swipeTimeStart - Time.fixedDeltaTime > playerClassValues.overheadSpeed)// + overheadWaitBeforeReset)//overheadWaitBeforeReset

        if (arrayRenderCount < pointsFromCurve.Count)
            arrayRenderCount += playerClassValues.overheadSpeed;
        else
            arrayRenderCount = pointsFromCurve.Count;

        if (startRenderCount >= pointsFromCurve.Count-1)
        {
            // overheadWhiff = true;
            //   ResetFlags();
            //   Debug.Log("Resetting within render function");

            //we have finished rendering, start cooldown timer
            swipeFinishedBuilding = true;
            timeSwingFinished = Time.time;
            //let player object know when we finished this swing too
            parentPlayer.GetComponent<Swipe>().finishTimeSriking = Time.time;
            parentPlayer.GetComponent<Swipe>().waitingOnResetOverhead = true;
            parentPlayer.GetComponent<Swipe>().buttonSwipeAvailable = false;
            
            parentPlayer.GetComponent<Swipe>().whiffed = true;

            activeTime = playerClassValues.overheadWhiffCooldown;
            //Invoke("DeactivateSwipe", Time.fixedDeltaTime);
            
            DeactivateSwipe();
            Destroy(this.gameObject);

            // parentPlayer.GetComponent<Swipe>().ResetFlags();

            //  Debug.Log("swipe time taken = " + (Time.time - swipeTimeStart));

            
        }
        //array is how far we have travelled
        //arrayrendercount is total
        Color red = (Resources.Load("Materials/Red0") as Material).color;
       // Debug.Log("array count = " + arrayRenderCount);
       // Debug.Log("points from curve count = " + pointsFromCurve.Count);
        // per = (float)arrayRenderCount / pointsFromCurve.Count;//gives a chanve for a mega hit even on short swipes
        //per = (float)arrayRenderCount/33f;//whats this number?
        per = arrayRenderCount*1.66f;
        //make intermediate colours for each player?
        //Debug.Log(per);
        float forLerp = per/100;// (float)arrayRenderCount/100;// (float)arrayRenderCount / pointsFromCurve.Count;// per * 0.1f;
        //forLerp *= 1.66f;
        Color orange = new Color(255/255, .66f, 0);
       // if(forLerp <0.5f)
            GetComponent<MeshRenderer>().material.color = Color.Lerp(Color.yellow, red, forLerp);
       // else
       //     GetComponent<MeshRenderer>().material.color = Color.Lerp(orange, red, forLerp);


        return mesh;
    }

    public void DeactivateSwipe()
    {
        activeSwipe = false;
    }

    void RenderButtonSwipe()
    {
        Debug.Log("Render Button Swipe");
        //points

        // swipePoint = new Vector3(swipePoint.x, 0f, swipePoint.z);
        //debugCube.transform.position = head.transform.position + swipePoint;

        //outside start

        //float distanceOfSwipe = lungePointsFinal[lungePointsFinal.Count-1].magnitude * playerClassValues.lungeLength;
        Vector3 outsideStart = lungePointsFinal[0].normalized * (playerClassValues.swordLength + playerClassValues.armLength + playerClassValues.lungeLength);
        //add a stretch
        //Vector3 outsideEnd = lungePointsFinal[lungePointsFinal.Count - 1];
        Vector3 outsideEnd = lungePointsFinal[1].normalized * (playerClassValues.lungeLength + playerClassValues.armLength + playerClassValues.swordLength);
        Vector3 insideStart = lungePointsFinal[lungePointsFinal.Count-1].normalized * (playerClassValues.armLength);
        Vector3 insideEnd = lungePointsFinal[2].normalized * (playerClassValues.armLength);// + playerClassValues.armLength);


      //  Debug.DrawLine(outsideStart + transform.position, insideStart + transform.position);    
       // Debug.DrawLine(insideStart + transform.position, insideEnd + transform.position);
      //  Debug.DrawLine(insideEnd + transform.position, outsideEnd + transform.position);
      //  Debug.DrawLine(outsideEnd + transform.position, outsideStart + transform.position);

        /*
        Debug.Break();

        
        GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = insideStart + transform.position;

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = insideEnd + transform.position;

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = outsideStart+ transform.position;

        c = GameObject.CreatePrimitive(PrimitiveType.Cube);
        c.transform.position = outsideEnd + transform.position;
        */

        List<Vector3> vertices = new List<Vector3>() { outsideStart, outsideEnd, insideEnd, insideStart };

        Mesh mesh = new Mesh();

        float per = (float)((Time.time - swipeTimeStart) / playerClassValues.lungeSpeed);
        per = Easings.CubicEaseIn(per);

         //Debug.Log(per);

        
        //lerp to make animation
        vertices[1] = Vector3.Lerp(vertices[0], vertices[1], per);
        vertices[2] = Vector3.Lerp(vertices[3], vertices[2], per);


        //make so vertices surround central point, we will hit check on centre of blade (above)
        //move upwards
        Vector3 p0 = vertices[0];
        Vector3 p1 = vertices[1];
        Vector3 dir = (p1 - p0).normalized;
        Vector3 fwdDir = (vertices[1] - vertices[2]).normalized;
        Vector3 sideDir = -Vector3.Cross(dir, fwdDir).normalized;


        for (int i = 0; i < 4; i++)
        {
            Vector3 p = vertices[i] + sideDir * playerClassValues.swordWidth * 0.5f;////**** do we need to do the fancy rotate thing we did for extrusion of curve?
            vertices[i] = p;
        }
        //extrude downwards
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = vertices[i] - sideDir * playerClassValues.swordWidth;////**** do we need to do the fancy rotate thing we did for extrusion of curve?
            vertices.Add(p);
        }

        List<int> triangles = new List<int>();

        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(2);
        triangles.Add(3);
        triangles.Add(0);

        //bottom
        triangles.Add(4);
        triangles.Add(6);
        triangles.Add(5);

        triangles.Add(4);
        triangles.Add(7);
        triangles.Add(6);

        //sides
        triangles.Add(0);
        triangles.Add(4);
        triangles.Add(1);

        triangles.Add(1);
        triangles.Add(4);
        triangles.Add(5);

        triangles.Add(1);
        triangles.Add(5);
        triangles.Add(2);

        triangles.Add(2);
        triangles.Add(5);
        triangles.Add(6);

        triangles.Add(2);
        triangles.Add(6);
        triangles.Add(3);

        triangles.Add(6);
        triangles.Add(7);
        triangles.Add(3);

        triangles.Add(3);
        triangles.Add(7);
        triangles.Add(0);

        triangles.Add(7);
        triangles.Add(4);
        triangles.Add(0);


        //reverse triangles if cominf from right side
        //work out angle from which we started //** coiuld save this and rest instead of working out every frame
        float firstPullBackAngle = MovementHelper.SignedAngle(firstPullBackLookDir, transform.forward, Vector3.up);
        if (firstPullBackAngle < 0)
        {

            for (int i = 0; i < triangles.Count; i += 3)
            {
                //   int temp = triangles[i + 2];
                //   triangles[i + 2] = triangles[i + 1];
                //   triangles[i + 1] = temp;
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 1;
        mesh.triangles = triangles.ToArray();

        //save original mesh before we make unique
        // lungeOriginalVertices = vertices;//using?        
        //currentSwipeObject.GetComponent<SwipeObject>().originalVertices = vertices;

        //now make unique vertices so we get nice edges
        mesh = MeshTools.UniqueVertices(mesh);
        //mesh.RecalculateBounds();
        //mesh.RecalculateNormals();

        //add to swipe object
        GetComponent<MeshFilter>().mesh = mesh;
        //if per is zero, mesh is absolutely flatm this will throw an error when trying to create the mesh collider
        if (per > 0)///happens?
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                // Debug.Log("vertices of swipe = " + vertices[i]);
            }

            if (vertices[0] == vertices[4])
                Debug.Log("SHOULD IGNORE");

            if (vertices[0] != vertices[4])
                GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        if (parentPlayer.GetComponent<PlayerInfo>().playerNumber == 0)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan1") as Material;
        else if (parentPlayer.GetComponent<PlayerInfo>().playerNumber == 1)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange2") as Material;
        else if (parentPlayer.GetComponent<PlayerInfo>().playerNumber == 2)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green2") as Material;
        else if (parentPlayer.GetComponent<PlayerInfo>().playerNumber == 3)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue2") as Material;

        //check for hits and report back - doing on object now
        //StraightSwipeHitCheck2(vertices, true, currentSwipeObject,false,true);


        if (per >= 1f)
        {
            // overheadWhiff = true;
            //   ResetFlags();
            //   Debug.Log("Resetting within render function");

            //we have finished rendering, start cooldown timer
            StartTimerForLunge();
            

        }
    }

    void StartTimerForLunge()
    {
        swipeFinishedBuilding = true;
        timeSwingFinished = Time.time;

        //let player object know when we finished this swing too
        parentPlayer.GetComponent<Swipe>().finishTimeSriking = Time.time;
        parentPlayer.GetComponent<Swipe>().waitingOnResetButtonSwipe = true;
        parentPlayer.GetComponent<Swipe>().overheadAvailable = false;
        parentPlayer.GetComponent<Swipe>().buttonSwiping = false;
        parentPlayer.GetComponent<Swipe>().whiffed = true;

        //time for this swipe to stay alive
        activeTime = playerClassValues.lungeWhiffCooldown;
        //activeSwipe = false;
        Invoke("DeactivateSwipe", Time.fixedDeltaTime);//why did i do this?
    }

    public void StraightSwipeHitCheck2(List<Vector3> vertices, bool activeSwipe, GameObject thisSwipeObject, bool forSideSwipe, bool forLunge)///need to do back edge and inside
    {
        //Debug.Log("hit check (straight swipe)");
        // Debug.Log("her");
        //runs rays between all vertices, only doing one direction, is it necessary to do it with reverse direction too?
        Vector3 impactPoint = Vector3.zero;
        //check for whiff, if strike has made it all the way to the end and not hit anything
        float speed = playerClassValues.sideSwipeSpeed;
        if (forLunge)
            speed = playerClassValues.lungeSpeed;



        //check for hit -- asigingn to swipe object now
        //  bool hitOpponent = false;
        // bool hitShield = false;
        //    bool hitSelf = false;
        //this could all be done down the middle of the strike by lerping between points(or just use side swipe points)
        List<RaycastHit[]> hitsTotalList = new List<RaycastHit[]>();
        RaycastHit[] hits = new RaycastHit[0];
       // List<Vector3> impactDirections = new List<Vector3>();
        //if swipe is active, activestrike = true, then look for player, shields and other swipes
        //else if it is on cooldown phase(static), don't look for other swipes, it can not destroy them
        LayerMask mask = LayerMask.GetMask("PlayerBody", "Shield", "Swipe");
        // if (activeSwipe == false)
        //    mask = LayerMask.GetMask("PlayerBody", "Shield","Swipe"); //checking for curves always mea

        //add ipact direction to list too - used in explosion effect
        //use the forward direction of the last panel on the sipe object, the normal of the last triangle
        Vector3 l0 = vertices[3];
        Vector3 l1 = vertices[1];//front edge

        Vector3 impactDir = Vector3.Cross(l0, l1).normalized;

        Vector3 centre = Vector3.Lerp(l0, l1, 0.5f);
        Debug.DrawLine(centre + transform.position, centre + transform.position + impactDir * 10);

        //check top edge
        List<int[]> sortedEdges = new List<int[]>() { new int[2] { 0, 1 }, new int[2] { 1, 2 }, new int[2] { 3, 4 }, new int[2] { 4, 0 } };
        for (int i = 0; i < 4; i++)
        {


            
            Vector3 p0 = vertices[sortedEdges[ i][0]];
            Vector3 p1 = vertices[sortedEdges[i][1]];

           

            //            Debug.DrawLine(p0 + transform.position, p1 + transform.position, Color.red);
            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);
            hits = Physics.RaycastAll(p0 + thisSwipeObject.transform.position, dir, distance, mask);
            hitsTotalList.Add(hits);

          

         //   impactDirections.Add(impactDir);
        }



        //check bottom
        sortedEdges = new List<int[]>() { new int[2] { 7, 8 }, new int[2] {8, 9 }, new int[2] { 10, 11 }, new int[2] { 10,6 } }; // :/
        for (int i = 0; i < 4; i++)
        {

            Vector3 p0 = vertices[sortedEdges[i][0] ];
            Vector3 p1 = vertices[sortedEdges[i][1] ];
            Vector3 dir = (p1 - p0).normalized;
            Debug.DrawLine(p0 + transform.position, p1 + transform.position, Color.red);
            float distance = Vector3.Distance(p0, p1);
            /*
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = p0 + transform.position;
            c.name = "0";

            c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = p1 + transform.position;
            c.name = "1";

        */
            hits = Physics.RaycastAll(p0 + thisSwipeObject.transform.position, dir, distance, mask);
            hitsTotalList.Add(hits);
           

         
        }
        //edges which make thickness ( needed?) //** first thing to chekfor optimsation
        for (int i = 0; i < 4; i++)
        {
            // Debug.DrawLine(vertices[i] + transform.position, vertices[i + 4] + transform.position);
            Vector3 p0 = vertices[i];
            Vector3 p1 = vertices[i + 4];
            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);

            hits = Physics.RaycastAll(p0 + thisSwipeObject.transform.position, dir, distance, mask);
            hitsTotalList.Add(hits);

        }



        for (int i = 0; i < hitsTotalList.Count; i++)
        {
            RaycastHit[] thisHits = hitsTotalList[i];

            for (int j = 0; j < thisHits.Length; j++)
            {
                //ignore self
                if (thisHits[j].transform.gameObject == thisSwipeObject)
                    continue;

                //check for shield first, then fir a swipe object, then finally for a player body - 

                if (thisHits[j].transform.gameObject.layer == LayerMask.NameToLayer("Shield"))
                {
                    // hitShield = true;
                    thisSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;

                    SwipeObject thisSwipeObjectScript = thisSwipeObject.GetComponent<SwipeObject>();//
                    thisSwipeObjectScript.impactDirection = impactDir;// (thisHits[j].point - transform.position).normalized;
                    thisSwipeObjectScript.impactPoint = thisHits[j].point;
                    thisSwipeObjectScript.hitShield = true;
                    //sideSwipeBlock = true;

                    thisSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                    if (activeSwipe)
                    {
                        
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().finishTimeSriking = Time.time;
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().blocked = true;
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().overheadAvailable = false;
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().waitingOnResetButtonSwipe = true;

                        //thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                    }

                    //shield beats all other checks
                    return;
                }
            }
        }

        for (int i = 0; i < hitsTotalList.Count; i++)
        {
            RaycastHit[] thisHits = hitsTotalList[i];

            for (int j = 0; j < thisHits.Length; j++)
            {
                //ignore self
                if (thisHits[j].transform.gameObject == thisSwipeObject)
                    continue;

                else if (thisHits[j].transform.gameObject.layer == LayerMask.NameToLayer("Swipe"))
                {
                    //we hit a swipe, check what type it was

                    //just destroy this swipe and continue looking for shields or bodies, 
                    //thisHits[j].transform.gameObject.GetComponent<SwipeObject>().impactPoint = thisHits[j].point;
                    // thisHits[j].transform.gameObject.GetComponent<SwipeObject>().impactDirection = impactDirections[i];
                    // Debug.Break();
                    //Debug.DrawLine(thisHits[j].point, thisHits[j].point + impactDirections[i], Color.red);
                    // Debug.Log("destroying this swipe");

                    SwipeObject thisSwipeObjectScript = thisSwipeObject.GetComponent<SwipeObject>();//
                    //tell swipe where we hit it - will be sued for explodo force
                    thisSwipeObject.GetComponent<SwipeObject>().impactDirection = impactDir;// impactDirections[i];
                    thisSwipeObject.GetComponent<SwipeObject>().impactPoint = thisHits[j].point;

                    SwipeObject otherSwipeObjectScript = thisHits[j].transform.gameObject.GetComponent<SwipeObject>();
                    //tell other too (impact)
                    otherSwipeObjectScript.GetComponent<SwipeObject>().impactDirection = impactDir;// impactDirections[i];


                    otherSwipeObjectScript.GetComponent<SwipeObject>().impactPoint = thisHits[j].point;

                    //check this player never owned the other swipe
                    if (otherSwipeObjectScript.parentPlayer != thisSwipeObjectScript.parentPlayer)
                    {
                        Debug.Log("different parent player");
                        //if same type of swipe, cancel both swipes and reset players, if they are both active
                        //if both swipes active - not using, how wdoes player know if strike has went to cooldown or not?

                        #region sideSwipes
                        //if both of the same type
                        if (thisSwipeObjectScript.sideSwipe && otherSwipeObjectScript.sideSwipe)
                        {
                            Debug.Log("same swipe type, destroying both");
                            //destroy both swipes and reset both players
                            // thisSwipeObjectScript.originalVertices = sideSwipeOriginalVertices;

                            thisSwipeObjectScript.DestroySwipe();

                            // otherSwipeObjectScript.originalVertices = otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().sideSwipeOriginalVertices;

                            otherSwipeObjectScript.DestroySwipe();

                            //impact directions? --add none, so they boyj just fall

                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            //we need to reset the other player if it was an active swipe
                            if (otherSwipeObjectScript.activeSwipe)
                            {
                                Debug.Log("--other player active, reseting other player");
                                otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                            }

                            //don't check any further, the swipe was cancelled by the other player
                            return;
                        }
                        //if side swipe (this vs other lunge)
                        else if (thisSwipeObjectScript.sideSwipe && otherSwipeObjectScript.lunge)
                        {
                            //lunge beats side swipe
                            Debug.Log("Other lunge beat this side swipe - destroying this only and reseting");

                            // thisSwipeObjectScript.originalVertices = sideSwipeOriginalVertices;
                            thisSwipeObjectScript.firstPullBackLookDir = firstPullBackLookDir;
                            thisSwipeObjectScript.hitByLunge = true;
                            thisSwipeObjectScript.impactDirection = Vector3.down;

                            thisSwipeObjectScript.DestroySwipe();

                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                        }
                        //if side swipe (this vs other overhead)
                        else if (thisSwipeObjectScript.sideSwipe && otherSwipeObjectScript.overheadSwipe)
                        {
                            //this beats overhead, destroy other and reset other player
                            Debug.Log("this side swipe beats other overhead, reseting other overhead");
                            //    otherSwipeObjectScript.originalVertices = otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().sideSwipeOriginalVertices;
                            otherSwipeObjectScript.hitBySideSwipe = true;
                            //let the other script know if direction should be flipped too
                            if (thisSwipeObjectScript.flip)
                                otherSwipeObjectScript.sideSwipeFlipped = true;

                            otherSwipeObjectScript.DestroySwipe();
                            //we need to reset the other player if it was an active swipe
                            if (otherSwipeObjectScript.activeSwipe)
                            {
                                Debug.Log("--other player active, reseting other player");
                                otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                            }

                            //we beat the other swipe, don't reset, we can keep looking for other hits
                        }
                        else if (thisSwipeObjectScript.buttonSwipe && otherSwipeObjectScript.overheadSwipe)
                        {
                            //this beats overhead, destroy other and reset other player
                            Debug.Log("this button swipe is beaten by other overhead, reseting this");
                            //    otherSwipeObjectScript.originalVertices = otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().sideSwipeOriginalVertices;
                            thisSwipeObjectScript.hitByOverhead = true;

                            thisSwipeObjectScript.GetComponent<SwipeObject>().impactDirection = impactDir ;// impactDirections[i];
                            thisSwipeObjectScript.GetComponent<SwipeObject>().impactPoint = thisHits[j].point;

                            //let the other script know if direction should be flipped too

                            //if (thisSwipeObjectScript.flip)
                            //otherSwipeObjectScript.sideSwipeFlipped = true;

                            thisSwipeObjectScript.DestroySwipe();
                            //we need to reset the other player if it was an active swipe
                            if (thisSwipeObjectScript.activeSwipe)
                            {
                                Debug.Log("--other player active, reseting other player");
                                thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                            }

                            //we beat the other swipe, don't reset, we can keep looking for other hits
                        }
                        else if (thisSwipeObjectScript.buttonSwipe && otherSwipeObjectScript.buttonSwipe)
                        {
                            Debug.Log("same swipe (button swipe) type, destroying both");
                            //destroy both swipes and reset both players
                            // thisSwipeObjectScript.originalVertices = sideSwipeOriginalVertices;

                            thisSwipeObjectScript.DestroySwipe();

                            // otherSwipeObjectScript.originalVertices = otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().sideSwipeOriginalVertices;

                            otherSwipeObjectScript.DestroySwipe();

                            //impact directions? --add none, so they boyj just fall

                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            //we need to reset the other player if it was an active swipe
                            if (otherSwipeObjectScript.activeSwipe)
                            {
                                Debug.Log("--other player active, reseting other player");
                                otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                            }

                            //don't check any further, the swipe was cancelled by the other player
                        }

                        #endregion
                        #region lunges

                        //if both lunges
                        if (thisSwipeObjectScript.lunge && otherSwipeObjectScript.lunge)
                        {
                            Debug.Log("both lunges, destroying both");
                            //destroy both swipes and reset both players
                            //thisSwipeObjectScript.originalVertices = lungeOriginalVertices;
                            //manipulate directions cause this function covers lunge and side swipe

                            thisSwipeObjectScript.DestroySwipe();

                            otherSwipeObjectScript.originalVertices = otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().lungeOriginalVertices;

                            //impact directions?  -- no direction on same swipe type hit- just let them fall                          

                            otherSwipeObjectScript.DestroySwipe();

                            thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                            //we need to reset the other player if it was an active swipe
                            if (otherSwipeObjectScript.activeSwipe)
                            {
                                Debug.Log("--other player active, reseting other player");
                                otherSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                            }

                            //don't check any further, the swipe was cancelled by the other player
                            return;
                        }
                    }
                    #endregion
                    else if (thisHits[j].transform.gameObject.GetComponent<SwipeObject>().parentPlayer == thisSwipeObjectScript.parentPlayer)
                    {
                        Debug.Log("same player");

                        //tell swipe where we hit it - will be sued for explodo force
                        thisSwipeObject.GetComponent<SwipeObject>().impactDirection = impactDir;// impactDirections[i];

                        //tell other too (impact)
                        otherSwipeObjectScript.GetComponent<SwipeObject>().impactDirection = impactDir;// impactDirections[i];

                        if (forLunge)
                            otherSwipeObjectScript.GetComponent<SwipeObject>().hitByLunge = true;

                        if (forSideSwipe)
                        {
                            otherSwipeObjectScript.GetComponent<SwipeObject>().hitBySideSwipe = true;

                            //let the other script know if direction should be flipped too
                            if (thisSwipeObjectScript.flip)
                                otherSwipeObjectScript.sideSwipeFlipped = true;
                        }


                        //if same player that made this swipe, blast threough it                       
                        if (thisSwipeObjectScript.activeSwipe)
                        {
                            Debug.Log("DSedning to destroy other swipe");
                            otherSwipeObjectScript.DestroySwipe();
                        }
                        if (otherSwipeObjectScript.activeSwipe && !thisSwipeObjectScript.activeSwipe)
                        {
                            Debug.Log("Sending to destroy this swipe - other active, this not active");
                            thisSwipeObjectScript.DestroySwipe();
                        }

                        //if this is a lunge and is being hit by an active overhead
                        //or if this is a side swipe and is being hit by an active overhead
                        if (forLunge && otherSwipeObjectScript.overheadSwipe && otherSwipeObjectScript.activeSwipe ||
                                forSideSwipe && otherSwipeObjectScript.overheadSwipe && otherSwipeObjectScript.activeSwipe
                                || thisSwipeObjectScript.buttonSwipe && otherSwipeObjectScript.buttonSwipe)
                            {
                                //tell script it is being hit by an overhead

                                //thisSwipeObjectScript.hitByOverhead = true; //gettin used for anything? yeah, forces for voxel

                                //find fwd facing curve direction
                                /*
                                List<Vector3> overheadVertices = new List<Vector3>(otherSwipeObjectScript.GetComponent<MeshFilter>().mesh.vertices);
                                Vector3 l0 = overheadVertices[overheadVertices.Count - 3];
                                Vector3 l1 = overheadVertices[overheadVertices.Count - 2];
                                //right angle to last vertices gives us fwd direction
                                Vector3 impactDir = Vector3.Cross(l0, l1).normalized;
                                thisSwipeObjectScript.impactDirection = impactDir;

                            */

                                // otherSwipeObjectScript.DestroySwipe();

                            }

                            //always keep looking if the swipe is our own, nothing should stop these swipes
                        }
                    }
                }
            }
            //players
            for (int i = 0; i < hitsTotalList.Count; i++)
            {
                RaycastHit[] thisHits = hitsTotalList[i];

                for (int j = 0; j < thisHits.Length; j++)
                {
                    //ignore self
                    if (thisHits[j].transform.gameObject == thisSwipeObject)
                        continue;

                    else if (thisHits[j].transform.gameObject.layer == LayerMask.NameToLayer("PlayerBody"))
                    {
                         Debug.Log("Hit Player Layer");
                    
                        SwipeObject thisSwipeObjectScript = thisSwipeObject.GetComponent<SwipeObject>();//
                                                                                                        // self hit
                        GameObject head = parentPlayer.transform.GetChild(1).GetChild(0).gameObject;
                        if (thisHits[j].transform.gameObject == head)//...
                        {

                           // Debug.Break();
                           // Debug.Log("Hit Self");
                            // hitSelf = true;

                            thisSwipeObjectScript.impactDirection = (thisHits[j].point - head.transform.position).normalized;
                            thisSwipeObjectScript.impactPoint = thisHits[j].point;
                            thisSwipeObjectScript.hitSelf = true;
                            thisSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                        if (activeSwipe)
                        {
                            //   thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                            StartTimerForLunge();
                        }

                            


                        //cancel checks if we hit ourselves, this takes priority over hit

                     //   Debug.Break();
                            return;
                        }
                        else//hit opponent or team mate 
                        {

                        //    hitOpponent = true;
                        //  sideSwipeHit = true;
                        // thisSwipeObjectScript.hitOpponent = true;
                        // thisSwipeObjectScript.timeSwingFinished = Time.time;
                        //  thisSwipeObjectScript.activeTime = playerClassValues.sideSwipeHitCooldown;
                        //thisSwipeObjectScript.hitBySideSwipe = true;


                        //reset opponent
                        //find parent of head mesh and reset it
                        PlayerInfo playerInfo = thisHits[j].transform.parent.parent.GetComponent<PlayerInfo>();
                        playerInfo.health -= playerClassValues.lungeHitHealthReduce;
                        //interrupt if swinging? 


                            GameObject parentOfHitHeadMesh = thisHits[j].transform.parent.parent.gameObject;
                            parentOfHitHeadMesh.GetComponent<Swipe>().ResetFlags();

                        thisSwipeObjectScript.impactDirection = (thisHits[j].point - head.transform.position).normalized;
                        thisSwipeObjectScript.impactPoint = thisHits[j].point;
                        thisSwipeObjectScript.hitOpponent = true;
                        //  thisSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Red0") as Material;//unsure
                        thisSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                        //bump player
                        PlayerMovement pMother = parentOfHitHeadMesh.GetComponent<PlayerMovement>();
                        pMother.bumped = true;
                        //hit point is on the near side, so get dir to hit transform and extend it through. point will now be on the rear side of hit transform
                        float hitBumpAmount = 1f;//*global var
                        pMother.bumpTarget = (parentOfHitHeadMesh.transform.position - thisHits[j].point)*hitBumpAmount + parentOfHitHeadMesh.transform.position;

                        //Debug.Break();

                        if (playerInfo.health <= 0f)
                        {

                            Swipe.BreakUpPlayer(thisHits[j].transform.gameObject, thisSwipeObjectScript);
                            Swipe.DeSpawnPlayer(parentOfHitHeadMesh);
                           
                            //
                        }
                        else
                        {
                            
                            //stop swipe
                            swipeFinishedBuilding = true;
                            timeSwingFinished = Time.time;
                            //let player object know when we finished this swing too
                            parentPlayer.GetComponent<Swipe>().finishTimeSriking = Time.time;
                            parentPlayer.GetComponent<Swipe>().waitingOnResetButtonSwipe = true;
                            //disable overhead while tiemr resets, dont want player just to switch strikes
                            parentPlayer.GetComponent<Swipe>().overheadAvailable = false;
                            parentPlayer.GetComponent<Swipe>().hit = true;

                            activeTime =  playerClassValues.lungeHitCooldown;
                            //Invoke("DeactivateSwipe", Time.fixedDeltaTime);
                            DeactivateSwipe();
                            hitOpponent = true;//set this to stop double hits
                            Debug.Log("health > 0 lunge");
                            return;
                        }
                            //we have a hit!, no need to keep looking
                            

                    }


                        // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        // c.transform.position = thisHits[j].point;


                }
            }
        }
    }

    void TestSplit()
    {
        if (!overheadSwipe)
            DestroyStraightSwipeCubes();
        else
            DestroyCurve();

        GetComponent<MeshRenderer>().enabled = false;
    }

    public void DestroySwipe()
    {
        enabled = false;
        //stop raycasting calling multiple times
        if (destroyingInProgress)
            return;

        destroyingInProgress = true;

        // Debug.Log("destroying swipe");
        //DestroySideSwipeDiamnonds(); //this is actually quite nice (keep just now)
        Debug.Log("side swipe = " + sideSwipe + ", lunge = " + lunge);
        if (!overheadSwipe)
            DestroyStraightSwipeCubes();
        else
            DestroyCurve();

        
        //add physics forces once all voels have been built
        AddForceToVoxels(voxels,this);

        if (!dontkill)
        {

            //find this object in swipe list and remove it
            //List<GameObject> currentSwipes = parentPlayer.GetComponent<Swipe>().currentSwipes;
            //for (int i = 0; i < currentSwipes.Count; i++)
            {
              //  if (currentSwipes[i] == gameObject)
                //    currentSwipes.RemoveAt(i);
            }

            Destroy(this.gameObject);
        }
        
    }
    
    public static Vector3 GetPerpendicularAtAngle(Vector3 v, float angle)
    {
        //slighlty altered from https://gamedev.stackexchange.com/questions/120980/get-perpendicular-vector-from-another-vector

        // Generate a uniformly-distributed unit vector in the XY plane.
        Vector3 inPlane = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

        // Rotate the vector into the plane perpendicular to v and return it.
        return Quaternion.LookRotation(v) * inPlane;
    }
    
    void DestroyCurve()
    {
        Debug.Log("Destroy Curve Function");
        int destroyTime = 5; //var

        //split curve using mesh vertices - not sure how to approach splitting up with afixed grid (worth the time to figure it out?)(cubes would be at differnet z positions?
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        List<Vector3> pointsFromCurve = new List<Vector3>(mesh.vertices);
        //float swordWidth = parentPlayer.GetComponent<Swipe>().swordWidth;
        // 
        //8 is ring size(amount of verts in one slice) and ignore last 8 vertices, they are the front and back plate
        int voxelSize = 2;//linked with curve detail, how many section to do together
        List<Vector3> quadPoints = new List<Vector3>();
        int step = 8 * voxelSize;
        for (int i = 0; i < pointsFromCurve.Count - step - 8; i += step)
        {

            //            Debug.DrawLine(pointsFromCurve[i + 0] + transform.position, pointsFromCurve[i + 1] + transform.position,Color.red);
            //          Debug.DrawLine(pointsFromCurve[i + 4 + 8] + transform.position, pointsFromCurve[i + 5 + 8] + transform.position, Color.blue);

            //work towards outer edge, sending for voxels as we go
            //inside distance for how long to make each voxel

            float verticalDistance = Vector3.Distance(pointsFromCurve[i], pointsFromCurve[i + 1]);
            float lastStep = Vector3.Distance(pointsFromCurve[i], pointsFromCurve[i + step]);
            Vector3 dir1 = -(pointsFromCurve[i] - pointsFromCurve[i + 1]).normalized;
            Vector3 dir2 = (pointsFromCurve[i + step + 1] - pointsFromCurve[i + step]).normalized;
            Vector3 sideDir2 = (pointsFromCurve[i + step + 1] - pointsFromCurve[i + step]).normalized;


          //  Debug.Log("inside");
          //  Debug.Log("vertical Dist = " + verticalDistance);
          //  Debug.Log("last step = " + lastStep);
            for (float j = 0; j < verticalDistance - lastStep * 1.1f; j += lastStep)
            {

               

                Vector3 p0 = pointsFromCurve[i] + (j * dir1);
                Vector3 next0 = pointsFromCurve[i + step] + sideDir2 * j;
                //make length the same as width
                float thisStep = Vector3.Distance(p0, next0);
                //if tiny, forget about it
                if(thisStep < 0.1f)
                {
                    
                    Debug.Log("tiny sliver for voxel");
                    return;
                    
                }


                Vector3 p1 = pointsFromCurve[i] + ((j + thisStep) * dir1);
                Vector3 p2 = pointsFromCurve[i + 5] + (j * dir1);
                Vector3 p3 = pointsFromCurve[i + 5] + ((j + thisStep) * dir1);

                Vector3 next1 = pointsFromCurve[i + step] + sideDir2 * (j + thisStep);
                Vector3 next2 = pointsFromCurve[i + step + 5] + sideDir2 * j;
                Vector3 next3 = pointsFromCurve[i + step + 5] + sideDir2 * (j + thisStep);

                //debug boxes
                if (i == 0)
                {
                    //inside horizontal
                    Debug.DrawLine(p0 + transform.position, p3 + transform.position, Color.green);
                    //outside horizontal
                     Debug.DrawLine(p1 + transform.position, p2 + transform.position, Color.blue);
                    //other vertical
                      Debug.DrawLine(p2 + transform.position, p3 + transform.position, Color.yellow);

                    //to next inside
                      Debug.DrawLine(pointsFromCurve[i] + transform.position, pointsFromCurve[i + 8] + transform.position, Color.white);
                    //to next outside
                      Debug.DrawLine(pointsFromCurve[i + 1] + transform.position, pointsFromCurve[i + 8 + 1] + transform.position, Color.magenta);
                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p0 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p1 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "1";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p2 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "2";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = p3 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "3";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = next0 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "n0";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = next1 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "n1";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = next2 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "n2";
                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = next3 + transform.position;
                    c.transform.localScale *= 0.1f;
                    c.name = "n3";
                    */
                }

                quadPoints = new List<Vector3>() { p0, p1, p2, p3, next0, next1, next2, next3 };
                
                MakeVoxel(quadPoints, destroyTime);
                //slowly make voxels larger as they get to the outside of the curve. This wil keep the voxels as square as possible
                lastStep = Vector3.Distance(p0, p1);
            }

            if(quadPoints.Count == 0)
            {
               // Debug.Break();
                
                Debug.Log("SKIP DESTROY CURVE");
              //  return;
            }

            //now add last piece of the pie
            //use last 4 points used in quadPoints from build loop, and add end points 
            Vector3 last0 = quadPoints[1];
            Vector3 last1 = quadPoints[3];
            Vector3 last2 = quadPoints[5];
            Vector3 last3 = quadPoints[7];

            Vector3 end0 = pointsFromCurve[i + 1];
            Vector3 end1 = pointsFromCurve[i + 1 + 3];
            Vector3 end2 = pointsFromCurve[i + 1 + step];
            Vector3 end3 = pointsFromCurve[i + 1 + step + 2];
            if (Vector3.Distance(last0, end0) < 0.01f)
            {
                
                Debug.Log("Distance small for end slice, returning");// ends can still be inside on occasion
                return;
            }

            quadPoints = new List<Vector3>() { last0, last1, last2, last3, end0, end1, end2, end3 };
            MakeVoxel(quadPoints, destroyTime);


            bool lastDebug = false;
            if (lastDebug)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = quadPoints[1] + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "1";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = quadPoints[3] + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "3";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = quadPoints[5] + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "5";
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = quadPoints[7] + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "7";

                
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = end0 + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "end 0";

                
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = end1 + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "end 1";

                
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = end2 + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "end 2";

                
                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = end3 + transform.position;
                c.transform.localScale *= 0.1f;
                c.name = "end 3";
            }

        }

    }

    void DestroyStraightSwipeCubes()
    {

        //create a mesh with many parts
        //use the original mesh to do this
        float detail = playerClassValues.swordWidth;

        //use furthest edge
        Vector3[] vertices = GetComponent<MeshFilter>().mesh.vertices;
        
      ///  Debug.DrawLine(vertices[0] + transform.position, vertices[1] + transform.position,Color.green);
      //  Debug.DrawLine(vertices[1] + transform.position, vertices[2] + transform.position, Color.blue);
      //  Debug.DrawLine(vertices[3] + transform.position, vertices[4] + transform.position, Color.red);
      //  Debug.DrawLine(vertices[4] + transform.position, vertices[0] + transform.position, Color.white);

       // Debug.Break();
        
        Vector3 p0 = vertices[0];
        Vector3 p1 = vertices[1];
        Vector3 dir = (p1 - p0).normalized;
        float distance = Vector3.Distance(p0, p1);

        Vector3 centreOfLong = Vector3.Lerp(p0, p1, 0.5f);
        Vector3 centreOfShort = Vector3.Lerp(vertices[3], vertices[4], 0.5f);
        Vector3 rotatedForLunge = (centreOfShort - centreOfLong).normalized;
        Vector3 fwdDir = (vertices[1] - vertices[2]).normalized;
        Vector3 upDir = (vertices[0] - vertices[4]).normalized;

      
        //Vector3 sideDir = Vector3.down;
        //if (lunge)
        Vector3 sideDir = Vector3.Cross(dir, fwdDir).normalized;//coudl use mesh sideway vertice? [5]- [4]
        //if (flip)
        //    sideDir = -sideDir;
        //work our way along edge at detail interval and project at a  90 degree angle looking for an intersection with the other edges
        for (float j = 0; j < distance - detail; j += detail)
        {            
            Vector3 start = p0 + (dir * j);

            //choose perpendicular vector dependin on what strike and whether weneed to flip the direction or not. We need to flip when swipe is on left side (or right, can't remember)
            Vector3 rotatedDir = Quaternion.Euler(0, -90, 0) * dir;
            //if (sideSwipe)
            {
              //  if(flip)
             //       rotatedDir = Quaternion.Euler(0, 90, 0) * dir;
            }
           // if (lunge)
            {
                //W.I.P

                //can we use this for side swipe too? (scared) if we do and hit it half way through we have non square voxels, 
                //it's fine if we want to keep non uniform mesh colliders - hoping to get as many box colliders as possiblein future



                //combines the forward and up dir to create a mid vector. To see enable debug lines above and combine blue and white
                rotatedDir = -((fwdDir + upDir) / 2).normalized; 

                //Debug.DrawLine(transform.position + centreOfShort, transform.position + centreOfShort + rotatedDir * 10, Color.magenta);
              

               // Debug.Break();
            }
            
            
            Vector3 closestInteresect = start;
            float closestDistance = Mathf.Infinity;
            Vector3 lastBuiltAt = start;//will get overwritten if we build any
                                        //each edge

            //look for intersection from first line on this edge

            
            //this is weird because of the way i constructed the mesh
            List<int[]> sortedEdges = new List<int[]>() { new int[2] { 0, 1 }, new int[2] { 1, 2 }, new int[2] { 3, 4 }, new int[2] { 4, 0 } };
            //start at 1 because we don't want to check the long edge. The long edge is the first edge in the array
            for (int i = 1; i < sortedEdges.Count; i++)
            {
                Vector3 intersectPoint;
                Vector3 intersectPoint2;
                Vector3 otherEdge0 = vertices[sortedEdges[i][0]];
                Vector3 otherEdge1 = vertices[sortedEdges[i][1]];

                //edges
                Debug.DrawLine(otherEdge0 + transform.position, otherEdge1 + transform.position,Color.magenta);
                Debug.DrawLine(start + transform.position, start + rotatedDir * 10f + transform.position, Color.cyan);
                /*
                 GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                 c.transform.position = transform.position + otherEdge0;
                 c.name = "0";
                 c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                 c.transform.position = transform.position + otherEdge1;
                 c.name = "1";
                 */

                if (ClosestPointsOnTwoLines(out intersectPoint,out intersectPoint2, start, rotatedDir, otherEdge0, (otherEdge1 - otherEdge0).normalized))
                {
                   // Debug.Log("intersect");

                    //will return multiple intersect points, this weeds out the wrong ones
                    float tempDistance = Vector3.Distance(intersectPoint2, start);
                    if (tempDistance < closestDistance)
                    {
                        closestDistance = tempDistance;
                        closestInteresect = intersectPoint2;
                    }

                   
                }
                else
                    Debug.Log("intersect not found");

                
            }

           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
         //   c.transform.position = transform.position + closestInteresect;
         //   c.name = "ip";

            //if we have found intersects, make voxels
            if (closestDistance != Mathf.Infinity)
            {
                // Color color = Color.red;
                // Debug.DrawLine(closestInteresect + transform.position, start + transform.position, color);

                //now we have our interesect point. Work our way towards it dropping points making sure we don't overshoot
                for (float i = 0; i < closestDistance - detail*1.5f; i += detail)
                {
                    //we need four points to make a "voxel"
                    List<Vector3> quadPoints = new List<Vector3>();

                    //create the quad

                    //first
                    Vector3 stepped = start + rotatedDir * i;
                    //to the right
                    Vector3 sideStepped = stepped + dir * (detail);
                    //to the right and forward
                    Vector3 nextSideStepped = stepped + rotatedDir * (detail) + (dir * detail);

                    Vector3 nextStepped = stepped + rotatedDir * (detail);

                   // Debug.DrawLine(stepped + transform.position, stepped + dir + transform.position, Color.cyan);

                    quadPoints.Add(stepped);
                    quadPoints.Add(nextStepped);
                    quadPoints.Add(sideStepped);
                    quadPoints.Add(nextSideStepped);

                    lastBuiltAt = nextStepped;

                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = stepped + transform.position;
                    c.name = "start";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = sideStepped + transform.position;
                    c.name = "sidestepped";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = nextSideStepped + transform.position;

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = nextStepped + transform.position;
                    */

                    

                    for (int x = 0;x < 4; x++)
                    {
                        Vector3 p = quadPoints[x] + sideDir * playerClassValues.swordWidth;////**** do we need to do the fancy rotate thing we did for extrusion of curve?
                        quadPoints.Add(p);
                    }

                    if (!flip)
                    {
                        //move down - this is a bit crap -- using?
                        for (int a = 0; a < quadPoints.Count; a++)
                        {
                            //quadPoints[a] += Vector3.down * parentPlayer.GetComponent<Swipe>().swordWidth;
                        }
                    }


                    MakeVoxel(quadPoints,5);
                }
            }
            //edge, make irregular shape
            bool doEdges = true;
            if(doEdges)
            {             
                //points we need, start + step, intersect point, start + step + sidestep, next intersect point
                //first
                Vector3 stepped = lastBuiltAt; //could be start or the end of the column depending on whether we built any voxels in the loop above
                Vector3 sideStepped = stepped + dir * (detail);
                Vector3 nextIntersect = Vector3.one * Mathf.Infinity;

                bool intersectFound = false;
                closestDistance = Mathf.Infinity;
                for (int i = 1; i < 4; i++)
                {
                    Vector3 intersectPoint;
                    Vector3 intersectPoint2;
                    Vector3 otherEdge0 = vertices[sortedEdges[i][0]];
                    Vector3 otherEdge1 = vertices[sortedEdges[i][1]];
                    

                    //edges
                   // Debug.DrawLine(originalVertices[i] + transform.position, originalVertices[nextIndex] + transform.position);


                    if (ClosestPointsOnTwoLines(out intersectPoint,out intersectPoint2, sideStepped, rotatedDir, otherEdge0, (otherEdge1 - otherEdge0).normalized))
                    {
                        //Debug.Log("intersect");


                        float tempDistance = Vector3.Distance(intersectPoint, start);
                        if (tempDistance < closestDistance)
                        {
                            closestDistance = tempDistance;
                            nextIntersect = intersectPoint;
                        }

                        intersectFound = true;
                    }


                    /*
                    GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = stepped + transform.position;
                    c.transform.name = "stepped";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = sideStepped + transform.position;
                    c.transform.name = "sideStepped";

                    c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    c.transform.position = closestInteresect + transform.position;
                    c.transform.name = "closest interesect";
                    */

                }

                if (!intersectFound)
                {
                    //make triangle by making intersect point the same as the starting point
                    nextIntersect = sideStepped;
                    Debug.Log("no intersect");///happening on lunge only?
                    //dontkill = true;
                    //return;//hack
                   

                    
                }
                
                //we need four points to make a "voxel"
                List<Vector3> quadPoints = new List<Vector3>();

                quadPoints.Add(stepped);
                quadPoints.Add(closestInteresect);
                quadPoints.Add(sideStepped);
                quadPoints.Add(nextIntersect);

                //some empty paramters
                // MakeVoxel(quadPoints,5,sideDir,Vector3.zero, parentPlayer.GetComponent<Swipe>().swordWidth,0f);

                for (int x = 0; x < 4; x++)
                {
                    Vector3 p = quadPoints[x] + sideDir * playerClassValues.swordWidth;////**** do we need to do the fancy rotate thing we did for extrusion of curve?
                    quadPoints.Add(p);
                }

                if(!flip)
                {
                    //move down - this is a bit crap
                    for (int i = 0; i < quadPoints.Count; i++)
                    {
                       // quadPoints[i] += Vector3.down * playerClassValues.swordWidth;
                    }
                }

                GameObject v =MakeVoxel(quadPoints, 5);
                v.name = "Edge";

            }
            //if(quadPoints.Count == 0)
            //{
                //we are at an edge
                
                //need next intersect point

                /*
                quadPoints.Add(start);
                quadPoints.Add(sideStepped);
                quadPoints.Add(closestInteresect);
                quadPoints.Add(start);

                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = start + transform.position;
                c.name = "start " + quadPoints.Count.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = sideStepped+ transform.position;
               // c.name = "start " + quadPoints.Count.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = closestInteresect+ transform.position;
               // c.name = "start " + quadPoints.Count.ToString();

                c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.transform.position = start+ transform.position;
               // c.name = "start " + quadPoints.Count.ToString();
               */
           // }

            //use these quad points to make a voxel



            
        }
    }

    GameObject MakeVoxel(List<Vector3> quadPoints,int destroyInSeconds)
    {
        
            //create a game object
            GameObject voxel = new GameObject();
            voxel.transform.position = transform.position;

            MeshRenderer mr = voxel.AddComponent<MeshRenderer>();
            MeshFilter mf = voxel.AddComponent<MeshFilter>();
            mr.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;


            Mesh mesh = new Mesh();
            List<Vector3> vertices = new List<Vector3>(quadPoints);

            List<int> triangles = new List<int>();

            triangles.Add(0);
            triangles.Add(1);
            triangles.Add(2);

            triangles.Add(3);
            triangles.Add(2);
            triangles.Add(1);

            //bottom
            triangles.Add(4);
            triangles.Add(6);
            triangles.Add(5);

            triangles.Add(5);
            triangles.Add(6);
            triangles.Add(7);


            //sides
            triangles.Add(0);
            triangles.Add(4);
            triangles.Add(1);

            //triangles.Add(0);
            //triangles.Add(4);
            //triangles.Add(1);

            triangles.Add(5);
            triangles.Add(1);
            triangles.Add(4);


            triangles.Add(2);
            triangles.Add(3);
            triangles.Add(6);

            triangles.Add(7);
            triangles.Add(6);
            triangles.Add(3);

            //back and front

            triangles.Add(0);
            triangles.Add(2);
            triangles.Add(4);

            triangles.Add(2);
            triangles.Add(6);
            triangles.Add(4);

            triangles.Add(1);
            triangles.Add(5);
            triangles.Add(3);

            triangles.Add(3);
            triangles.Add(5);
            triangles.Add(7);

            //change side dir and now we always need to flip - optpmisation, change above two flip 2 and 3rd tris.meh
            if (!overheadSwipe) //flip
            {
                //change 2 and 3 triangle, will flip faces
                for (int j = 0; j <= triangles.Count - 3; j += 3)
                {
                    int temp = triangles[j];
                    triangles[j] = triangles[j + 1];
                    triangles[j + 1] = temp;
                }

                voxel.name = "Flipped";
            }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();

        bool reAlign = true;//possible opto? needed? think so, distance calculations for force uses voxel position(could use mesh.bounds i gues..) I like it like this
        if (reAlign)
        {
            //move transform
            voxel.transform.position = mesh.bounds.center + transform.position;

            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] -= voxel.transform.position - transform.position; ;
            }

            mesh.vertices = vertices.ToArray();
        }

        mesh = MeshTools.UniqueVertices(mesh);

        mf.mesh = mesh;
        //add collider, box might be enough        

        bool makeBoxCollider = false; //box collider not rotated properly? works okay i guess
        bool skipCollider = false;
        if (!skipCollider)
        {
            if (makeBoxCollider)
            {
                BoxCollider bc = voxel.AddComponent<BoxCollider>();
                // bc.size = parentPlayer.GetComponent<Swipe>().swordWidth * Vector3.one;            
                bc.size = 0.1f * Vector3.one;
                bc.material = Resources.Load("Physics/SwipeShard") as PhysicMaterial;
            }
            else
            {

                MeshCollider mc = voxel.AddComponent<MeshCollider>();
                mc.convex = true;
                mc.material = Resources.Load("Physics/SwipeShard") as PhysicMaterial;
                voxel.layer = LayerMask.NameToLayer("Voxel");
            }
        }
        //this list will be used to add forces to the voxels
        voxels.Add(voxel);

        return voxel;

    }

    public static void AddForceToVoxels(List<GameObject> voxels, SwipeObject swipeObject)
    {
        //find out voxles distance to impact point, remmeber largest
        List<float> distances = new List<float>();
        float largestDistance = 0f;
        float smallestDistance = Mathf.Infinity;
        for (int i = 0; i < voxels.Count; i++)
        {
            float d = Vector3.Distance(voxels[i].transform.position, swipeObject.impactPoint);
            distances.Add(d);
            if (d > largestDistance)
                largestDistance = d;

            if (d < smallestDistance)
                smallestDistance = d;
        }

        //removing smallest distance from largest so when we create a percentage for force, we get a full 100% scale
        largestDistance -= smallestDistance;
        for (int i = 0; i < voxels.Count; i++)
        {
            GameObject voxel = voxels[i];

            //add rigidbody to use physics
            Rigidbody rb = voxel.AddComponent<Rigidbody>();

            //rb.AddForceAtPosition(Vector3.down * 1, impactPoint, ForceMode.Impulse);
            //float swordWidth = parentPlayer.GetComponent<Swipe>().swordWidth;
            //float radius = swordWidth*.5f;
          //  Vector3 blastPos = impactPoint + Vector3.up * radius;
            
            //worked out above, wil have same index as voxel
            float distanceToImpactPoint = distances[i];
            //remove smallest distance from both
            distanceToImpactPoint -= smallestDistance;
            //invert distance calculation so the closest point gets the most power and the furthest gets 0
            //get percentage between 0 and 1
            float p= distanceToImpactPoint / largestDistance;
            //use easing function to create cool shapes
            //1f -  inverst so furthest away gets the least force added

            bool smoothSlope = false;
            bool steepSlope = true;
            bool wave = false;
            bool elastic = false;//more of a step//maybe good for a big hammer?
            
            if(smoothSlope)
                p = 1f-  Easings.ExponentialEaseOut(p);
            if (steepSlope)
                p = 1f - Easings.ExponentialEaseOut(p * p);//more dramatic slope on curve if we square input
            if (wave)
                p = 1f - Easings.BackEaseInOut(p);//further way hangs in the air more
            if (elastic)
                p = 1f - Easings.ElasticEaseInOut(p);//oooh
            


            float overallForce = 1000f;//large cos it gets multiplied by 0.1f..etc

            //change direction if flipped 0 don't know why i cant just change impactDireciton to -impactdirection on (if(!flip))..maybe just tired


            if (swipeObject.hitBySideSwipe)
            {
                if (swipeObject.overheadSwipe)
                {
                    
                    //this flagged asigned to side swipe hits overhead in side swipes checks
                    if(swipeObject.sideSwipeFlipped)
                    {
                        rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                    }
                    else
                    {
                        rb.AddForce((-swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                    }
                    
                }
                if(swipeObject.sideSwipe)
                {
                    if(swipeObject.sideSwipeFlipped)
                        rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                    else
                        rb.AddForce((-swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                }
                if (swipeObject.lunge)
                {                   
                    
                    if (swipeObject.sideSwipeFlipped)
                        rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                    else
                        rb.AddForce((-swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                    
                }
            }
            else if (swipeObject.hitByLunge)
            {
                if (swipeObject.overheadSwipe)
                {

                    rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                }
                else
                {
                    
                    rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                }
            }
            else if(swipeObject.hitByOverhead)
            {
                //if (swipeObject.overheadSwipe) 
                if (float.IsNaN(swipeObject.impactDirection.x) || float.IsNaN(swipeObject.impactDirection.y) || float.IsNaN(swipeObject.impactDirection.z))
                {
                    Debug.Log(" swipe object hit by overhead is NaN");
                    Debug.Break();
                }
                else
                {
                    rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);
                }
                /*
                if (swipeObject.sideSwipe)
                {
                    rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);                    
                }
                if (swipeObject.lunge)
                    rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);                    
                    */
            }
            else if (swipeObject.hitSelf)
            {
                rb.AddForce((swipeObject.impactDirection * overallForce * p));// + impactPoint + voxel.transform.position);                    
            }

            else if(swipeObject.hitOpponent)
            {
                Vector3 force = (swipeObject.impactDirection * overallForce * p);
                if (force.x == float.NaN)
                {
                    Debug.Break();
                    Debug.Log("Force is Nan for voxel");
                    continue;
                }
                    
                else
                    rb.AddForce(force);// + impactPoint + voxel.transform.position);                    
            }


            if (swipeObject.impactDirection == Vector3.zero || swipeObject.impactPoint == Vector3.zero)
            {
               // Debug.Break();
                //Debug.Log("zero impact dir"); //this is okay, happns when same tpyes of swipe hit each other

            } 
            rb.mass = 1;//was more, seems ok atm
            rb.drag = 0;
           //rb.useGravity = false;

            Destroy(voxel, 3);
            // Destroy(s, 1);
        }
    }

    void DestroySideSwipeDiamnonds()
    {
        //create a mesh with many parts
        //use the original mesh to do this
        int detail = 10;
        //RaycastHit[] hits = new RaycastHit[0];
        //check top edge
        List<Vector3> edgePoints = new List<Vector3>();
        for (int i = 0; i < 3; i++)
        {
           // Debug.DrawLine(originalVertices[i] + transform.position, originalVertices[i + 1] + transform.position);
            Vector3 p0 = originalVertices[i];
            Vector3 p1 = originalVertices[i + 1];
            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);
            float m = 1f;
            if(i== 0 || i ==2)
                m=.5f;

            for (float j = 0; j < distance - (distance / detail) * 0.5f; j+= (distance/detail)*m) 
            {
                Vector3 p = p0 + dir * j;
                edgePoints.Add(p);
              //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
              // c.transform.position = p + transform.position;
            }

            if (i == 2)
            {
                //connecing loop
               // Debug.DrawLine(originalVertices[3] + transform.position, originalVertices[0] + transform.position, Color.cyan);
                p0 = originalVertices[3];
                p1 = originalVertices[0];
                dir = (p1 - p0).normalized;
                distance = Vector3.Distance(p0, p1);

                for (float j = 0; j < distance - (distance/detail)*0.5f; j += (distance / detail) *m)
                {
                    Vector3 p = p0 + dir * j;
                    edgePoints.Add(p);

                  //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  //  c.transform.position = p + transform.position;

                    edgePoints.Add(p);
                }
            }
        }
        List<Vector3> gridPoints = new List<Vector3>();

        for (int i = 0; i <= detail; i++)
        {
            Vector3 p0 = edgePoints[i];
            int reversedOpposite = (detail-i) + detail * 2 ;
            Vector3 p1 = edgePoints[reversedOpposite];

            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);

            for (float j = 0; j < distance + (distance / detail) * 0.5f; j += distance / detail)
            {
                Vector3 p = p0 + dir * j;
                gridPoints.Add(p);

                //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // c.transform.position = p + transform.position;
            }

            //Debug.DrawLine(p0 + transform.position, p1 + transform.position, Color.yellow);
            
        }


        //split in to four
        List<List<Vector3>> topPointsMaster = new List<List<Vector3>>();
        for (int i = 0; i < gridPoints.Count-detail-2; i++)
        {
            

            Vector3 p0 = gridPoints[i];
            Vector3 p1 = gridPoints[i+1];
            Vector3 p2 = gridPoints[i + detail +1];
            Vector3 p3 = gridPoints[i + 1 + detail +1];

            List<Vector3> topPoints = new List<Vector3>() { p0, p1, p2, p3 };

            topPointsMaster.Add(topPoints);

        }

        for (int i = 0; i < topPointsMaster.Count; i++)
        {

            bool skip = false;

            //check to see if these points overhang edge
            for (int j = 0; j < topPointsMaster[i].Count; j++)
            {
              //  string name = i.ToString() + " " + j.ToString();
                if (i > 0 && (i+1) % (detail + 1) == 0)
                {
                    name = "passed";
                    skip = true;

                    continue;   
                }

                
               // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
               // c.transform.position = topPointsMaster[i][j] + transform.position;
               // c.name = name;
            }

            if (!skip)
            {
                //create a game object
                GameObject voxel = new GameObject();
                voxel.transform.position = transform.position;

                MeshRenderer mr = voxel.AddComponent<MeshRenderer>();
                MeshFilter mf = voxel.AddComponent<MeshFilter>();
                mr.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;


                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>(topPointsMaster[i]);
                //extrude downwards
                for (int j = 0; j < 4; j++)
                {
                    Vector3 p = topPointsMaster[i][j] + Vector3.down * playerClassValues.swordWidth;////**** do we need to do the fancy rotate thing we did for extrusion of curve?
                    vertices.Add(p);
                }

                List<int> triangles = new List<int>();

                triangles.Add(0);
                triangles.Add(1);
                triangles.Add(2);

                triangles.Add(3);
                triangles.Add(2);
                triangles.Add(1);

                //bottom
                triangles.Add(4);                
                triangles.Add(6);
                triangles.Add(5);

                triangles.Add(5);                
                triangles.Add(6);
                triangles.Add(7);

                
                //sides
                triangles.Add(0);
                triangles.Add(4);
                triangles.Add(1);

                triangles.Add(0);
                triangles.Add(4);
                triangles.Add(1);

                triangles.Add(5);
                triangles.Add(1);
                triangles.Add(4);


                triangles.Add(2);                
                triangles.Add(3);
                triangles.Add(6);

                triangles.Add(7);
                triangles.Add(6);
                triangles.Add(3);

                //back and front

                triangles.Add(0);
                triangles.Add(2);
                triangles.Add(4);

                triangles.Add(2);
                triangles.Add(6);
                triangles.Add(4);

                triangles.Add(1);
                triangles.Add(5);
                triangles.Add(3);

                triangles.Add(3);
                triangles.Add(5);
                triangles.Add(7);

                if(flip)
                {
                    //change 2 and 3 triangle, will flip faces
                    for (int j = 0; j <= triangles.Count-3; j += 3)
                    {
                        int temp = triangles[j];
                        triangles[j] = triangles[j + 1];
                        triangles[j + 1] = temp;
                    }

                    voxel.name = "Flipped";
                }

                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();

                mesh = MeshTools.UniqueVertices(mesh);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                


                mf.mesh = mesh;

                //add collider, box might be enough
                MeshCollider mc = voxel.AddComponent<MeshCollider>();
                //  BoxCollider bc = voxel.AddComponent < BoxCollider > ();
                mc.convex = true;
                mc.material = Resources.Load("Physics/SwipeShard") as PhysicMaterial;
                //add rigidbody to use physics
                Rigidbody rb = voxel.AddComponent<Rigidbody>();

                //rb.velocity = -impactPoint.normalized*.0001f;
                rb.mass = 10;
                rb.drag = 1;
                rb.useGravity = true;
            }

        }

    }

    void DestroySwipeStraightEdgeOld()
    {
        //create a mesh with many parts
        //use the original mesh to do this
        int detail = 10;
        
        //check top edge
        List<Vector3> edgePoints = new List<Vector3>();
        for (int i = 0; i < 3; i++)
        {
            // Debug.DrawLine(originalVertices[i] + transform.position, originalVertices[i + 1] + transform.position);
            Vector3 p0 = originalVertices[i];
            Vector3 p1 = originalVertices[i + 1];
            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);

            for (float j = 0; j < distance - (distance / detail) * 0.5f; j += distance / detail)
            {
                Vector3 p = p0 + dir * j;
                edgePoints.Add(p);
                //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // c.transform.position = p + transform.position;
            }

            if (i == 2)
            {
                //connecing loop
                // Debug.DrawLine(originalVertices[3] + transform.position, originalVertices[0] + transform.position, Color.cyan);
                p0 = originalVertices[3];
                p1 = originalVertices[0];
                dir = (p1 - p0).normalized;
                distance = Vector3.Distance(p0, p1);

                for (float j = 0; j < distance - (distance / detail) * 0.5f; j += distance / detail)
                {
                    Vector3 p = p0 + dir * j;
                    edgePoints.Add(p);

                    //  GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //  c.transform.position = p + transform.position;

                    edgePoints.Add(p);
                }
            }
        }
        List<Vector3> gridPoints = new List<Vector3>();

        for (int i = 0; i <= detail; i++)
        {
            Vector3 p0 = edgePoints[i];
            int reversedOpposite = (detail - i) + detail * 2;
            Vector3 p1 = edgePoints[reversedOpposite];

            Vector3 dir = (p1 - p0).normalized;
            float distance = Vector3.Distance(p0, p1);

            for (float j = 0; j < distance + (distance / detail) * 0.5f; j += distance / detail)
            {
                Vector3 p = p0 + dir * j;
                gridPoints.Add(p);

                //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // c.transform.position = p + transform.position;
            }

            //Debug.DrawLine(p0 + transform.position, p1 + transform.position, Color.yellow);

        }


        //split in to four
        List<List<Vector3>> topPointsMaster = new List<List<Vector3>>();
        for (int i = 0; i < gridPoints.Count - detail - 2; i++)
        {


            Vector3 p0 = gridPoints[i];
            Vector3 p1 = gridPoints[i + 1];
            Vector3 p2 = gridPoints[i + detail + 1];
            Vector3 p3 = gridPoints[i + 1 + detail + 1];

            List<Vector3> topPoints = new List<Vector3>() { p0, p1, p2, p3 };

            topPointsMaster.Add(topPoints);

        }

        for (int i = 0; i < topPointsMaster.Count; i++)
        {

            bool skip = false;

            //check to see if these points overhang edge
            for (int j = 0; j < topPointsMaster[i].Count; j++)
            {
               // string name = i.ToString() + " " + j.ToString();
                if (i > 0 && (i + 1) % (detail + 1) == 0)
                {
                    name = "passed";
                    skip = true;

                    continue;
                }


                // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // c.transform.position = topPointsMaster[i][j] + transform.position;
                // c.name = name;
            }

            if (!skip)
            {
                //create a game object
                GameObject voxel = new GameObject();
                voxel.transform.position = transform.position;

                MeshRenderer mr = voxel.AddComponent<MeshRenderer>();
                MeshFilter mf = voxel.AddComponent<MeshFilter>();
                mr.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;


                Mesh mesh = new Mesh();
                List<Vector3> vertices = new List<Vector3>(topPointsMaster[i]);
                //extrude downwards
                for (int j = 0; j < 4; j++)
                {
                    Vector3 p = topPointsMaster[i][j] + Vector3.down * playerClassValues.swordWidth;////**** do we need to do the fancy rotate thing we did for extrusion of curve?
                    vertices.Add(p);
                }

                List<int> triangles = new List<int>();

                triangles.Add(0);
                triangles.Add(1);
                triangles.Add(2);

                triangles.Add(3);
                triangles.Add(2);
                triangles.Add(1);

                //bottom
                triangles.Add(4);
                triangles.Add(6);
                triangles.Add(5);

                triangles.Add(5);
                triangles.Add(6);
                triangles.Add(7);


                //sides
                triangles.Add(0);
                triangles.Add(4);
                triangles.Add(1);

                triangles.Add(0);
                triangles.Add(4);
                triangles.Add(1);

                triangles.Add(5);
                triangles.Add(1);
                triangles.Add(4);


                triangles.Add(2);
                triangles.Add(3);
                triangles.Add(6);

                triangles.Add(7);
                triangles.Add(6);
                triangles.Add(3);

                //back and front

                triangles.Add(0);
                triangles.Add(2);
                triangles.Add(4);

                triangles.Add(2);
                triangles.Add(6);
                triangles.Add(4);

                triangles.Add(1);
                triangles.Add(5);
                triangles.Add(3);

                triangles.Add(3);
                triangles.Add(5);
                triangles.Add(7);

                if (flip)
                {
                    //change 2 and 3 triangle, will flip faces
                    for (int j = 0; j <= triangles.Count - 3; j += 3)
                    {
                        int temp = triangles[j];
                        triangles[j] = triangles[j + 1];
                        triangles[j + 1] = temp;
                    }

                    voxel.name = "Flipped";
                }

                mesh.vertices = vertices.ToArray();
                mesh.triangles = triangles.ToArray();

                mesh = MeshTools.UniqueVertices(mesh);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();



                mf.mesh = mesh;

                //add collider, box might be enough
                MeshCollider mc = voxel.AddComponent<MeshCollider>();
                //  BoxCollider bc = voxel.AddComponent < BoxCollider > ();
                mc.convex = true;
                mc.material = Resources.Load("Physics/SwipeShard") as PhysicMaterial;
                //add rigidbody to use physics
                Rigidbody rb = voxel.AddComponent<Rigidbody>();

                //rb.velocity = -impactPoint.normalized*.0001f;
                rb.mass = 10;
                rb.drag = 1;
                rb.useGravity = true;
            }

        }


    } //non voexl shape (diamonds usually)

    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float planarFactor = Vector3.Dot(lineVec3, crossVec1and2);

        //is coplanar, and not parrallel
        if (Mathf.Abs(planarFactor) < 0.0001f && crossVec1and2.sqrMagnitude > 0.0001f)
        {
            float s = Vector3.Dot(crossVec3and2, crossVec1and2) / crossVec1and2.sqrMagnitude;
            intersection = linePoint1 + (lineVec1 * s);
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

    //Two non-parallel lines which may or may not touch each other have a point on each line which are closest
    //to each other. This function finds those two points. If the lines are not parallel, the function 
    //outputs true, otherwise false.
    public static bool ClosestPointsOnTwoLines(out Vector3 closestPointLine1, out Vector3 closestPointLine2, Vector3 linePoint1, Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2)
    {

        closestPointLine1 = Vector3.zero;
        closestPointLine2 = Vector3.zero;

        float a = Vector3.Dot(lineVec1, lineVec1);
        float b = Vector3.Dot(lineVec1, lineVec2);
        float e = Vector3.Dot(lineVec2, lineVec2);

        float d = a * e - b * b;

        //lines are not parallel
        if (d != 0.0f)
        {

            Vector3 r = linePoint1 - linePoint2;
            float c = Vector3.Dot(lineVec1, r);
            float f = Vector3.Dot(lineVec2, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = linePoint1 + lineVec1 * s;
            closestPointLine2 = linePoint2 + lineVec2 * t;

            return true;
        }

        else
        {
            return false;
        }
    }

}



