using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public bool shieldActive = false;
    public float shieldRotationSpeed = 20f;
    public float duckSpeed = 0.1f;

    public Swipe swipe;
    
    public bool previousRightStickReset = false;
    public List<GameObject> currentAdjacents = new List<GameObject>();

    public bool skipFrame = true;
    public List<GameObject> playerGlobalList;
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
   public  float shieldStartingScaleX;
    public float shieldStartingScaleY;
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
        
        playerGlobalList = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList;
        spawner= GameObject.FindGameObjectWithTag("Code").GetComponent<Spawner>();

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

        GetInputs();


        //check for any attacks and control them if they are attacking
        
        
        swipe.SwipeOrder();
        

        //can block if not attacking, or changing cell height
        if(!swipe.overheadSwiping && !swipe.buttonSwiping && !swipe.whiffed && !GetComponent<PlayerMovement>().adjustingCellHeight)            
            Block();
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

        RSMagnitude = new Vector2(x, y).magnitude;
        //Debug.Log("RS playerAttacks " + RSMagnitude);

        if (RSMagnitude < deadzone)
        {
            rightStickReset = true;
        }
        else
            rightStickReset = false;

        playerMoving = GetComponent<PlayerMovement>().moving;

        //create a magnitude which doesn't consider camera angle, more precise, tells us exactly what the user put in the stick
      
    }

   

    void Block()
    {
        //place shield in front of player if block button pushed
        //rotate shield high by pushin stick towards?

        float shieldScaleSpeed = shieldRotationSpeed*Time.deltaTime*Mathf.PI*2;//just using cos it is a very magic number :)

        Vector3 scale = (new Vector3(1f,1f,0f) * shieldScaleSpeed);//z value stays at shield thickness
        
      

        if (!autoShield)
        {
            
            if (inputs.state.Buttons.LeftShoulder == XInputDotNetPure.ButtonState.Pressed)// && shieldActive)
            {
                //if button pushed, raise shield and embiggen

                //rotate towards fwd if not at level
                Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
                   
                    if (inputs.state.Buttons.RightShoulder == XInputDotNetPure.ButtonState.Pressed)
                    {

                        targetRot = Quaternion.LookRotation(Vector3.forward + Vector3.up * .5f);

                        //duck
                        if (headOriginalPos.y - head.transform.localPosition.y < head.transform.localScale.y)
                            head.transform.localPosition -= Vector3.up * duckSpeed;

                    }
                    else
                    {
                        //unduck
                        head.transform.localPosition += Vector3.up * duckSpeed;
                        if (head.transform.localPosition.y > headOriginalPos.y)
                            head.transform.localPosition = headOriginalPos;


                    }
                    

                shieldPivot.transform.localRotation = Quaternion.RotateTowards(shieldPivot.transform.localRotation, targetRot, shieldRotationSpeed);


                //scale

                if (shield.transform.localScale.x < shieldStartingScaleX)
                    shield.transform.localScale += scale;

                if (shield.transform.localScale.x > shieldStartingScaleX)
                    shield.transform.localScale = new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ);

                shieldActive = true;

            }
            else if (inputs.state.Buttons.LeftShoulder == XInputDotNetPure.ButtonState.Released)
            {
                //if button is let go lower shield and ensmallen


                Quaternion targetRot = Quaternion.LookRotation(Vector3.down);
                //targetRot = 
                shieldPivot.transform.localRotation = Quaternion.RotateTowards(shieldPivot.transform.localRotation, targetRot, shieldRotationSpeed);

                if (shield.transform.localScale.x > 0f)
                    shield.transform.localScale -= scale;

                if (shield.transform.localScale.x < 0f)
                    shield.transform.localScale = new Vector3(0f, 0f, shieldStartingScaleZ);


                if (shieldPivot.transform.localRotation == targetRot)
                    shieldActive = false;

                //unduck
                head.transform.localPosition += Vector3.up * duckSpeed;
                if (head.transform.localPosition.y > headOriginalPos.y)
                    head.transform.localPosition = headOriginalPos;

            }
              

          
        }

        else if(autoShield)
        {
            shieldPivot.SetActive(true);
            shieldActive = true;

            Quaternion targetRot = Quaternion.LookRotation(Vector3.forward);
            shieldPivot.transform.localRotation = Quaternion.RotateTowards(shieldPivot.transform.localRotation, targetRot, shieldRotationSpeed);


            //scale

            if (shield.transform.localScale.x < shieldStartingScaleX)
                shield.transform.localScale += scale;

            if (shield.transform.localScale.x > shieldStartingScaleX)
                shield.transform.localScale = new Vector3(shieldStartingScaleX, shieldStartingScaleY, shieldStartingScaleZ);

            shieldActive = true;

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

