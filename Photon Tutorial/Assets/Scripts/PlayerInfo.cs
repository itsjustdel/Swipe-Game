using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerInfo : MonoBehaviour {

    //holds info about player, player number, health, points etc
    public bool respawn = true;
    public double lastDeathTime;
    public bool playerDespawned = true;
    public bool playerCanRespawn = true;
    public int controllerNumber = 1;//controller
    public int teamNumber = -1;
    public GameObject currentCell;
    public GameObject homeCell;
    public GameObject lastCell;
    public bool beenHit = false;
    public bool updateAdjacentCells = false;
    public bool updateCurrentCell = false;//enable after spawn position
    public float health = 100f;
    public bool healthRegen = false;
    private float targetHealth = 100f;
    public float healthAnimationSpeed = 1f;
    public float healthRegenSpeed = 0.05f;
    
    //top for primitive cylinder Unity
    //public int [] topVertices = new int[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 41, 43, 45, 46, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87 };
    public PlayerClassValues playerClassValues;
    public PlayerGlobalInfo pgi;
    public List<GameObject> cellsUnderControl = new List<GameObject>();
    private Inputs inputs;

    private void Start()
    {
     
        //force a respawn if this is our network player

        if(GetComponent<PhotonView>().IsMine)
        {
            playerDespawned = true;
            respawn = true;
        }

        inputs = GetComponent<Inputs>();

        pgi = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>();
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();

        controllerNumber = pgi.playerGlobalList.Count - 1;//tests
    }



    private void FixedUpdate()
    {
        if(playerDespawned)
        {
           
            currentCell = null;
            //need respawn code, auto respawn atm
            if (PhotonNetwork.Time - lastDeathTime > playerClassValues.respawnTime)
                respawn = true;
        }

        if(respawn)
        {

            RespawnPlayer();
            respawn = false;

        }

        //Health

        //update current cell
        if (updateCurrentCell && !playerDespawned)
            CurrentCell();

    
        //if this gets any more complicated, put in own script
        if(healthRegen)
            HealthRegen();

        
    }
    private void Update()
    {
        HealthVisualisation();
    }

    void HealthRegen()
    {
        if (currentCell == null)
            return;

        if(currentCell.GetComponent<AdjacentCells>().controlledBy == controllerNumber)
        {
            health += healthRegenSpeed;

           if (health > 100f)
                health = 100f;
        }
    }
    void HealthVisualisation()
    {
        //turns health number in to scaled cubes with different colours to show player visually

        //move target health towards health
        if (health > targetHealth)
            targetHealth += healthAnimationSpeed;
        else if (health < targetHealth)
            targetHealth -= healthAnimationSpeed;

        if (targetHealth > 100f)
            targetHealth = 100f;

        for (int i = 0; i < 2; i++)
        {
            GameObject cube = transform.GetChild(2).GetChild(0).GetChild(i).gameObject;
            //first cube is blood cube
            if(i == 1)
            {
                float p = targetHealth / 100f;

                cube.transform.localScale = new Vector3(cube.transform.localScale.x, p/cube.transform.parent.transform.localScale.y, cube.transform.localScale.z);

                if (targetHealth <= 0)//will be y scael 0 causing gfx probs
                {
                    cube.gameObject.SetActive(false);
                }
                else
                {
                    cube.gameObject.SetActive(true);
                }
            }
            else
            {
              
                float p = (100f - targetHealth) / 100f;

                cube.transform.localScale = new Vector3(cube.transform.localScale.x, p / cube.transform.parent.transform.localScale.y, cube.transform.localScale.z);
            }
            
        }
    }
    //perhaps should be in its own script
    void RespawnPlayer()
    {
        //has function fired for respawn - called from BreakUpPlayer in Swipe
        if (!playerCanRespawn)
            return;
        
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            //disable movement and attack scripts
            GetComponent<PlayerMovement>().enabled = true;
            GetComponent<PlayerMovement>().walking = false;
            GetComponent<PlayerAttacks>().enabled = true;
            GetComponent<Swipe>().enabled = true;

            //reset flag
            playerDespawned = false;
            
        }

        //move player to a random space
        Vector3 spawnPos = homeCell.GetComponent<ExtrudeCell>().centroid;// Spawner.RandomPosition();
        transform.position = spawnPos;

        health = 100f;
        targetHealth = 100f;
    }

    void SendRespawnToNetwork()
    {
        //let the network know this plaayer has respawned so they can mirror the action

        byte evCode = 12; // Custom Event 12:spawn
        int photonViewID = GetComponent<PhotonView>().ViewID;

        object[] content = new object[] { photonViewID };


        //send to everyone but this client
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        //keep resending until server receives
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
    }

    public void SetSpawnAvailable()
    {
        playerCanRespawn = true;
    }

    
    void AdjacentCells()
    {
        PlayerMovement pM = GetComponent<PlayerMovement>();
        //find which adjacent cell's centroid is closest to angle pushed on stick
        pM.currentAdjacents = new List<GameObject>();
        //check adjacent cells on our current cell, we work current cell out every frame in PlayerInfo

        pM.currentAdjacents = pM.GetComponent<PlayerInfo>().currentCell.GetComponent<AdjacentCells>().adjacentCells;
    }

    void CurrentCell()
    {

        RaycastHit hit;

        if (Physics.Raycast(transform.position + Vector3.up * 50f, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells"))) //was using last target as shootFrom here
        {
            //check for a change in cell
            if (currentCell != hit.transform.gameObject)
            {

                lastCell = currentCell;

                UpdateAbondonedCell();

                currentCell = hit.transform.gameObject;

                UpdateCells();
            }
        }

      
    }

    void UpdateCells()
    {

        //add to player's list and remove from opponents (list which holds which cells they own) // if conditions allow
        AdjustLists();
        //check to see if any adjacent cell is surrounded by enemy cells
        //Snared();
        //work out the frontline cells for this new cell
        Frontline();
    }

    public void UpdateAbondonedCell()
    {
        if (lastCell == null)
            return;

        //check if another player/team owns this now
        //check for other player
        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            PlayerInfo pI = pgi.playerGlobalList[i].GetComponent<PlayerInfo>();

            //don't check own player
            if (controllerNumber == pI.controllerNumber)
                continue;

            //if last cell is another player's currenct cell (and they are alone) - give them the cell
            if (lastCell == pI.currentCell)
            {
                //Debug.Log("found");
                //give lsat cell to this play
                lastCell.GetComponent<AdjacentCells>().controlledBy = pI.controllerNumber;
            }
        }
    }

    public void AdjustLists()
    {
        //remove this cell if in any other player's list
        if (currentCell.GetComponent<AdjacentCells>().controlledBy != controllerNumber)
        {
            //Debug.Log("not player number");


            //grab cell if other player is not on it already
            //if another player is already on this cell, it can't be grabbed --**using this segment of code (with slight changes) a few times now - method/function it?
            bool otherTeamOnCell = false;
            bool otherTeamOnAdjacentCell = false;
            bool wallOnAdjacentCell = false;
            for (int j = 0; j < pgi.playerGlobalList.Count; j++)
            {
                PlayerInfo pI = pgi.playerGlobalList[j].GetComponent<PlayerInfo>();

                //don't check our own player
                if (pI.controllerNumber == controllerNumber)
                    continue;

                //if other player's currenct cell matches this current cell
                if (pI.currentCell == currentCell)
                {
                    //stop searching - somebody else is on the cell we were thinking about making a frontline cell
                    otherTeamOnCell = true;
                    //Debug.Log("other player on cell");
                    continue;
                }

                //or if other player/team is on an adjacent cell
                //chck for walls too

                AdjacentCells aJ = currentCell.GetComponent<AdjacentCells>();
                for (int a = 0; a < aJ.adjacentCells.Count; a++)
                {
                    if (pI.currentCell == aJ.adjacentCells[a])
                        otherTeamOnAdjacentCell = true;

                    //we don't need to do this check if the adjacent cell is owned by us, we can claim it
                    if (aJ.adjacentCells[a].GetComponent<AdjacentCells>().controlledBy == controllerNumber)
                    {
                        //don't need to check
                    }
                    else
                    {
                        if (aJ.adjacentCells[a].transform.localScale.y - currentCell.transform.localScale.y >= playerClassValues.maxClimbHeight)
                            wallOnAdjacentCell = true;
                    }
                }
            }
            Debug.Log(otherTeamOnCell);
            Debug.Log(otherTeamOnAdjacentCell);
            Debug.Log(wallOnAdjacentCell);

            if (!otherTeamOnCell && !otherTeamOnAdjacentCell && !wallOnAdjacentCell)
            {
                //find list and remove cell from other player (if in list) and if cell has been controlled at all yet (-1)
                AdjacentCells aJ = currentCell.GetComponent<AdjacentCells>();
                int controlledBy = aJ.controlledBy;
                if (controlledBy > -1)
                {
                    List<GameObject> otherCells = pgi.playerGlobalList[controlledBy].GetComponent<PlayerInfo>().cellsUnderControl;
                    //if (otherCells.Contains(currentCell))
                    otherCells.Remove(currentCell);
                }

                aJ.controlledBy = controllerNumber;
                //no longer frontline
                //aJ.frontlineCell = false;

                //update this player's list
                if (!cellsUnderControl.Contains(currentCell))
                    cellsUnderControl.Add(currentCell);
            }
            else
            {
                //we are sharing a cell with another player, fight to the end!
            }
        }
    }

    public void Frontline()
    {
        //Debug.Log("doing frontline");
        //use current cell and determine which cells around it should be considered "frontline"

        //firstly let's check the cell we just landed on
        //as we landed on it, CurrentCell() already asigned it to this player
        //1. it should be a frontline cell if another team's player is standing on an adjacent cell

        // currentCell.GetComponent<AdjacentCells>().frontlineCell = false;

        bool opponentOnAdjacent = OpponentOnAdjacent();
        //secondly, check to see if opponent is on current cell - opponent can hold cell if they were there first
        bool opponentOnCurrent = OpponentOnCurrent();


        if (opponentOnAdjacent && !opponentOnCurrent)
        {
            //  currentCell.GetComponent<AdjacentCells>().frontlineCell = true;
        }



        //now we have decdied about our current cell, let's look at adjacent cells and see if we need to 
        //make them frontline cells

        //an adjacent cell should be a frontline cell when//
        //1. if it has an adjacent cell controlled (but not a frontline) by another player
        //2. has not been built too high


        List<GameObject> currentAdjacents = currentCell.GetComponent<AdjacentCells>().adjacentCells;
        for (int i = 0; i < currentAdjacents.Count; i++)
        {
            AdjacentCells aJ = currentAdjacents[i].GetComponent<AdjacentCells>();

            //if adjacent is controlled by another but NOT too high to get to (walls up)
            bool opponentOnThisAdjacent = false;

            for (int j = 0; j < pgi.playerGlobalList.Count; j++)
            {
                if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == currentAdjacents[i])
                    opponentOnThisAdjacent = true;
            }

            bool tooHigh = false;
            //check ajacent height against current cell height
            Debug.Log(currentAdjacents[i].transform.localScale.y - currentCell.transform.localScale.y);
            if (currentAdjacents[i].transform.localScale.y - currentCell.transform.localScale.y >= playerClassValues.maxClimbHeight)
            {

                tooHigh = true;
            }

            //Debug.Log("aj.controlled by =" + (aJ.controlledBy != controllerNumber));
            //  Debug.Log("opponnet on adj = " + opponentOnAdjacent);            
            //  Debug.Log("Too High = " + tooHigh);
            //  Debug.Log("oppnonent on current = " + opponentOnCurrent);
            if (aJ.controlledBy != controllerNumber && !opponentOnThisAdjacent && !tooHigh && !opponentOnCurrent)
            {
                //   currentAdjacents[i].GetComponent<AdjacentCells>().frontlineCell = true;
            }
            //if adjacent is too high to be turned in to be turned in to a frontline cell, the current cell becomes a frontline instead unless is it that own the high cell
            else if (tooHigh && aJ.controlledBy != controllerNumber)
            {
                //  currentCell.GetComponent<AdjacentCells>().frontlineCell = true;
            }
        }
    }

    bool OpponentOnAdjacent()
    {

        bool opponentOnAdjacent = false;
        //These are the cells we need to check
        List<GameObject> currentAdjacents = currentCell.GetComponent<AdjacentCells>().adjacentCells;
        //against these players's current cells
        List<GameObject> opponentsCurrentCells = new List<GameObject>();

        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            //don't check this player
            if (i == controllerNumber)
                continue;
            //populate list with all current cells - note, we will not know who owns what from this list, but we do not need this
            opponentsCurrentCells.Add(pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell);
        }

        //now check if any of our current adjacents are occupied
        for (int i = 0; i < currentAdjacents.Count; i++)
        {
            //if in opponents current cell list
            if (opponentsCurrentCells.Contains(currentAdjacents[i]))
                opponentOnAdjacent = true;

        }
        return opponentOnAdjacent;
    }

    bool OpponentOnCurrent()
    {
        bool opponentOnCurrent = false;

        //other players's current cells
        List<GameObject> opponentsCurrentCells = new List<GameObject>();

        for (int i = 0; i < pgi.playerGlobalList.Count; i++)//**work this loop out in opponentOnAdjacent too (small opto)
        {
            //don't check this player
            if (i == controllerNumber)
                continue;
            //populate list with all current cells - note, we will not know who owns what from this list, but we do not need this
            opponentsCurrentCells.Add(pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell);
        }

        if (opponentsCurrentCells.Contains(currentCell))
            opponentOnCurrent = true;

        return opponentOnCurrent;
    }
}
