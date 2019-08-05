using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CellMeter : MonoBehaviour {

    public int roundTime = 180;
    public int  currentRoundTime = 0;

    public float overallX = 0.8f;
    public float xScale = 1f;
    public float yPos = 1f;
    public float meterHeight = 50f;//screen zie percentage needed
    PlayerGlobalInfo playerGlobalInfo;
    public int playerAmount;
    GameObject cameraObject;
    MeshGenerator meshGenerator;
    public List<GameObject> anchors = new List<GameObject>();
    public List<float> ratios = new List<float>();
    GameObject timer;
    public bool roundEnded = false;
    public GameObject centralCell = null;
    bool finishedScaling = false;
    bool scalePhaseOneComplete = false;

    public float scaleSpeed = 0.02f;
    public float scaleSizeForCentralPhaseOne = 50f;
    public float scaleSizeForCentralPhaseTwo = 4f;
    private Coroutine timerCoroutine;
    // Use this for initialization
   public  GameObject canvas;
    private void Awake()
    {
        enabled = false;
    }
    void Start ()
    {
        currentRoundTime = 0;
        anchors = new List<GameObject>();
        ratios = new List<float>();
        centralCell = null;
        roundEnded = false;
        finishedScaling = false;
        scalePhaseOneComplete = false;

        playerGlobalInfo = GetComponent<PlayerGlobalInfo>();
        meshGenerator= GetComponent<MeshGenerator>();
        playerAmount = playerGlobalInfo.playerGlobalList.Count;
        cameraObject = Camera.main.gameObject;
        canvas = GameObject.Find("Canvas(Clone)");
        canvas.GetComponent<Canvas>().enabled = true;
        timer = canvas.transform.Find("Timer").gameObject;


        AddRects();

        EnableNeeded();
        //SetStartSizes();

        //make sure to stop any timers from last round
        if(timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        //now start timer
        timerCoroutine = StartCoroutine("Timer");
	}

    private void Update()
    {
        if (!roundEnded)
        {
            TimerSize();//count
            SetSizes();//teams
        }
    }

    private void FixedUpdate()
    {
        
        if(roundEnded)
        {
            //scale cell

            if(roundEnded && centralCell !=  null && finishedScaling == false)            
            {
                for(int i = 0; i < meshGenerator.cells.Count;i++)
                {
                    if(meshGenerator.cells[i] != centralCell)
                    {
                        //scale
                        meshGenerator.cells[i].transform.localScale -= scaleSpeed * Vector3.one;
                        if (meshGenerator.cells[i].transform.localScale.x <= 0f)
                            meshGenerator.cells[i].SetActive(false);
                    }
                    else
                    {
                        if(scalePhaseOneComplete == false)
                        {
                            centralCell.transform.localScale += scaleSpeed * Vector3.one;
                            if (centralCell.transform.localScale.x >= scaleSizeForCentralPhaseOne)
                            {
                                scalePhaseOneComplete = true;
                                centralCell.transform.localScale = scaleSizeForCentralPhaseOne * Vector3.one;
                            }
                        }
                        else if (scalePhaseOneComplete == true)
                        {
                            centralCell.transform.localScale -= scaleSpeed * Vector3.one;
                            if (centralCell.transform.localScale.x <= scaleSizeForCentralPhaseTwo)
                            {
                                finishedScaling = true;
                                centralCell.transform.localScale = scaleSizeForCentralPhaseTwo * Vector3.one;
                            }
                        }

                    }
                }
            }
        }
    }

    void EnableNeeded()
    {
        for (int i = 0; i < playerAmount; i++)
        {
            //they start disabled, so enable how amny we need
            canvas.transform.GetChild(i).gameObject.SetActive(true);
        }
    }

    void AddRects()
    {
        for (int i = 0; i < playerAmount; i++)
        {            
            anchors.Add(canvas.transform.GetChild(i).gameObject);
        }
    }

    IEnumerator Timer()
    {
        if( currentRoundTime >= roundTime)
        {
            currentRoundTime = roundTime;
            //disable and check last time
            //enabled = false;
            roundEnded = true;
            SetSizes();
            TimerSize();
            EndRound();
        }

        if (roundEnded)
            yield break;
        else
        {
            yield return new WaitForSecondsRealtime(1);
            currentRoundTime++;
            StartCoroutine("Timer");
        }
    }
    public void EnableNextFrame()
    {
        Invoke("Enable",0.01f);
    }

    private void Enable()
    {
        Start();
        enabled = true;
        
        GameObject canvas = GameObject.Find("Canvas(Clone)");
        canvas.GetComponent<Canvas>().enabled = true;
    }

    void ResetWorld()
    {
        GameObject.FindGameObjectWithTag("WorldSpawner").GetComponent<WorldSpawner>().endWorld = true;
    }
   
    void EndRound()
    { 
      
        //Determine Winner!
        List<GameObject> sortedPlayers = new List<GameObject>();
        for (int i = 0; i < playerAmount; i++)
        {
            sortedPlayers.Add(playerGlobalInfo.playerGlobalList[i]);
        }

        sortedPlayers.Sort(delegate (GameObject a, GameObject b) {
            
            return (a.GetComponent<PlayerInfo>().cellsUnderControl.Count).CompareTo(b.GetComponent<PlayerInfo>().cellsUnderControl.Count);
        });
        //most at first
        sortedPlayers.Reverse();

        //find if any draw
        int draws = 1;
        for (int i = 1; i < playerAmount; i++)
        {
            if (sortedPlayers[i - 1].GetComponent<PlayerInfo>().cellsUnderControl.Count == sortedPlayers[i].GetComponent<PlayerInfo>().cellsUnderControl.Count)
            {
                draws++;
            }
            else
                break;
        }

        Debug.Log("Draws = " + draws);
        if (draws == 1)
        {
            //Debug.Break();
            Camera.main.GetComponent<CameraControl>().showWinner = true;
            Camera.main.GetComponent<CameraControl>().winner = sortedPlayers[0];

            //...
            Invoke("ResetWorld", 5);
            
        }
        
        if (draws > 1)
        {

            
            //create final standoff!
            //get central cell
            
            List<GameObject> sortedCells = new List<GameObject>(meshGenerator.cells);
            
            sortedCells.Sort(delegate (GameObject a, GameObject b)
            {
                return (a.transform.position.sqrMagnitude.CompareTo(b.transform.position.sqrMagnitude));
            });
            
            centralCell = sortedCells[0];
            centralCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Disputed") as Material;

            //turn off and scripts controlling arena (cell heights etc)
            //possibly could run from overlay drawer?
            //disable for now, and run from here
            GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>().enabled = false;

            

        }

        GameObject.Find("Walls").SetActive(false);
    }

    void TimerSize()
    {
        //fraction
        float fraction = 1f / roundTime;
        //current
        float current = fraction * currentRoundTime * (float)Camera.main.pixelWidth * xScale;
        float y = Camera.main.pixelHeight * (yPos) - meterHeight;
        timer.GetComponent<RectTransform>().transform.position = new Vector3(Camera.main.pixelWidth/2, y, 0f);
        timer.GetComponent<RectTransform>().sizeDelta = new Vector2(current, meterHeight);

    }

    void SetSizes()
    {
        

        float tempWidth = (float)Camera.main.pixelWidth * xScale;

        int totalCells =meshGenerator.cells.Count;
        float totalCellsControlled = 0;
        float fraction = (float)tempWidth / totalCells;

        List<int> cellsUnderControlNoFrontline = new List<int>() { 0, 0, 0, 0 };

        //frac = 10
        for (int i = 0; i < playerAmount; i++)
        {
            PlayerInfo pI = playerGlobalInfo.playerGlobalList[i].GetComponent<PlayerInfo>();

            for (int j = 0; j < pI.cellsUnderControl.Count; j++)
            {
                //work out ratio 
                //don't include frontline cells, these aren't consided "controlled", it saves it as owned by a player so we know who to give it to
                //..when?
                if (pI.cellsUnderControl[j].GetComponent<AdjacentCells>().frontlineCell)
                    continue;

                //all cells
                totalCellsControlled ++;

                //cells for each player
                cellsUnderControlNoFrontline[i]++;
            }
            

        }

        //total cells 
       

        ratios.Clear();
        for (int i = 0; i < playerAmount; i++)
        {
           // PlayerInfo pI = playerGlobalInfo.playerGlobalList[i].GetComponent<PlayerInfo>();

            //work out ratio
            ratios.Add( cellsUnderControlNoFrontline[i] / totalCellsControlled);

        }

        float last = 0f;
        List<float> midPoints = new List<float>();
        for (int i = 0; i < anchors.Count ; i++)
        {
            PlayerInfo pI = playerGlobalInfo.playerGlobalList[i].GetComponent<PlayerInfo>();
                        
            float x = ratios[i] * tempWidth * .5f + last;

            //add x scale 
            x += ((1f-xScale) * Camera.main.pixelWidth*.5f); //keeps in the middle regardless of x scale
            midPoints.Add(x);

            last = ratios[i] * tempWidth + last; 
        }

        float y = Camera.main.pixelHeight  * yPos;
        for (int i = 0; i < anchors.Count; i++)
        {
            RectTransform rect = anchors[i].transform.GetComponent<RectTransform>();
           // float x = Camera.main.pixelWidth - (Camera.main.pixelWidth/(playerAmount))* (i);
            //x *= playerAmount;
          //  rect.sizeDelta = new Vector2(x * (ratios[i]), meterHeight);
          //  x = x;// * 1f/playerAmount + (x * i);//will need more when 3 players
            rect.transform.position = new Vector3(midPoints[i], y, 0f);
           // float xWidth = (Camera.main.pixelWidth * ratios[i] );
            rect.sizeDelta = new Vector2(ratios[i] * tempWidth, meterHeight);
        }

        for (int i = 0; i < totalCells; i++)
        {
           // GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
           // c.transform.position = new Vector3(fraction*i, 0f, 0f);
           // Destroy(c, Time.deltaTime);

        }
    }
}
