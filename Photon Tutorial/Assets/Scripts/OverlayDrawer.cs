using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayDrawer : MonoBehaviour
{

    //receives positions from each player and draws correct overlay for each cell
   // public int playerAmount = 2;
    List<GameObject> cells;
    PlayerGlobalInfo pgi;
    PlayerClassValues playerClassValues;
    List<List<int>> coloursForEachCell = new List<List<int>>();

    public bool resetCells = false;
    // Use this for initialization

    bool skipFrame = true;
    public bool doHeights;
    public bool doFrontline;
    public bool doHomeCellHeightsToStart;
    public bool reduceFrontline;
    // public float targetY;
    public float heightSpeed = .1f;
    public float heightSpeedSiege = .01f;
    public float heightMultiplier = 2f;
    public float minHeight = 3f;
    public int totalCellsTransparent = 0;
    public List<int> opponents;
    //holds all meshes off voronoi generation before we altered them
    public List<Mesh> originalMeshes = new List<Mesh>();
    void Start()
    {
        
        cells = GetComponent<MeshGenerator>().cells;
        pgi = GetComponent<PlayerGlobalInfo>();
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();

        //  targetY = minHeight;
    }

    // Update is called once per frame
    void Update()
    {




    }

    private void FixedUpdate()
    {

        UpdateCurrentCells();


        Height2();

        if (resetCells)
        {
            ResetCells();

            resetCells = false;
        }

        if (doHeights)
        {


            for (int i = 0; i < cells.Count; i++)
            {
                float tempHeightSpeed = heightSpeed;

                //if frontline cell, raise height if standing on it - stop if two plyers re on it
                for (int j = 0; j < pgi.playerGlobalList.Count; j++)
                {
                    //find out if this is a current cell
                    if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == cells[i] && cells[i].GetComponent<AdjacentCells>().controlledBy == -1)
                    {
                        //find highest adjacent cell
                        float highest = 0f;

                        for (int k = 0; k < cells[i].GetComponent<AdjacentCells>().adjacentCells.Count; k++)
                        {
                            if (cells[i].GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y > highest)
                            {
                                highest = cells[i].GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y;
                            }
                        }
                        cells[i].GetComponent<AdjacentCells>().targetY = highest; ;//highest opponent adjacent
                        tempHeightSpeed = heightSpeedSiege;
                    }
                }


                float y = cells[i].transform.localScale.y + tempHeightSpeed;

                if (Mathf.Abs(cells[i].transform.localScale.y - cells[i].GetComponent<AdjacentCells>().targetY) <= tempHeightSpeed)
                {
                    //if close to target,
                    cells[i].transform.localScale = new Vector3(1f, cells[i].GetComponent<AdjacentCells>().targetY, 1f);
                }
                else
                {

                    //grow it if smaller than target
                    if (cells[i].transform.localScale.y < cells[i].GetComponent<AdjacentCells>().targetY)
                    {
                        cells[i].transform.localScale = new Vector3(1f, cells[i].transform.localScale.y + tempHeightSpeed, 1f);

                        if (cells[i].transform.localScale.y > cells[i].GetComponent<AdjacentCells>().targetY)
                            cells[i].transform.localScale = new Vector3(1f, cells[i].GetComponent<AdjacentCells>().targetY + tempHeightSpeed, 1f);
                    }
                    else if (cells[i].transform.localScale.y > cells[i].GetComponent<AdjacentCells>().targetY)
                    {
                        cells[i].transform.localScale = new Vector3(1f, cells[i].transform.localScale.y - tempHeightSpeed, 1f);

                        if (cells[i].transform.localScale.y < cells[i].GetComponent<AdjacentCells>().targetY)
                            cells[i].transform.localScale = new Vector3(1f, cells[i].GetComponent<AdjacentCells>().targetY + tempHeightSpeed, 1f);
                    }


                }

            }
        }


        if (reduceFrontline)
        {
            ReduceFrontline();
        }

       // TransparentCells(); 
    }

    void UpdateCurrentCells()
    {

        //for each player, raycast for them to find out which cell they are over

        RaycastHit hit;
        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            PlayerInfo pI = pgi.playerGlobalList[i].GetComponent<PlayerInfo>();
            if (pI.playerDespawned)
                continue;

            GameObject currentCell = pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell;
            Vector3 shootFrom = pgi.playerGlobalList[i].transform.position + Vector3.up * 50;
            if (Physics.Raycast(shootFrom, Vector3.down, out hit, 100f, LayerMask.GetMask("Cells"))) //was using last target as shootFrom here
            {
                if (currentCell != hit.transform.gameObject)
                {


                    GameObject lastCell = currentCell;
                    //if a change in cell, update
                    pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell = hit.transform.gameObject;

                    //what happens to cell we just left
                    AbandonedCell(lastCell);
                    //and work out new cells for this player
                    Capture(i);

                    Ensnared(pI.teamNumber);
                }
            }
        }
    }

    void ReduceFrontline()
    {
        //if no played is on the cell, make sure it is being reduced to lowest amount
        for (int i = 0; i < cells.Count; i++)
        {
            //look for frontliners
            if (cells[i].GetComponent<AdjacentCells>().controlledBy > -1)
                continue;

            //look to see if any palyer's current cell
            bool current = false;
            for (int j = 0; j < pgi.playerGlobalList.Count; j++)
            {
                CellHeights cellHeights = pgi.playerGlobalList[j].GetComponent<CellHeights>();
                if (cellHeights.loweringCell || cellHeights.raisingCell)
                {

                    if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == cells[i])
                    {
                        current = true;
                        break;
                    }
                }
            }

            if (current)
                continue;

            float targetY = cells[i].transform.localScale.y - heightSpeedSiege;
            if (targetY < 1f)//min height?
                targetY = 1f;

            cells[i].transform.localScale = new Vector3(1f, targetY, 1f);

        }
    }

    

    void TransparentCells()
    {



        //first reset all bools for transparency
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].GetComponent<AdjacentCells>().beingMadeTransparent = false;
        }
        //rest counter for cam
        totalCellsTransparent = 0;

        //find out if a cell is in the way of a player and the camera



        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {

            Vector3 shootFrom = pgi.playerGlobalList[i].transform.position + Vector3.up * 0.5f;//was causing glitch, still?
            Vector3 shotDir = Camera.main.transform.position - shootFrom;

            // Debug.DrawLine(shootFrom, Camera.main.transform.position);

            RaycastHit[] hits = Physics.RaycastAll(shootFrom, shotDir, shotDir.magnitude, LayerMask.GetMask("Cells"));

            for (int j = 0; j < hits.Length; j++)
            {
                Material mat = null;

                //frontline?
                if (hits[j].transform.GetComponent<AdjacentCells>().controlledBy == -1)
                {
                    mat = Resources.Load("ClearMaterials/DisputedClear") as Material;
                }
                else
                {
                    //then who

                    int controlledBy = hits[j].transform.GetComponent<AdjacentCells>().controlledBy;
                    if (controlledBy == 0)
                        mat = Resources.Load("ClearMaterials/CyanClear2") as Material;
                    else if (controlledBy == 1)
                        mat = Resources.Load("ClearMaterials/OrangeClear2") as Material;
                    else if (controlledBy == 2)
                        mat = Resources.Load("ClearMaterials/GreenClear2") as Material;
                    else if (controlledBy == 3)
                        mat = Resources.Load("ClearMaterials/DeepBlueClear2") as Material;
                }

                hits[j].transform.GetComponent<AdjacentCells>().beingMadeTransparent = true;
                hits[j].transform.GetComponent<MeshRenderer>().sharedMaterial = mat;

                totalCellsTransparent++;
            }
        }
    }

    private void ResetCells()
    {
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
        }
    }

    void AbandonedCell(GameObject lastCell)
    {
        if (lastCell == null)
            return;

        //work out if anythin should happen to the cell that was just left by a player

        //if another team is on it, the cell becomes a frontline cell, unless they own the cell they are on
        //check for other team
        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {

            PlayerInfo pI = pgi.playerGlobalList[i].GetComponent<PlayerInfo>();
            //make disputed cell if the team now on it is different to that who owned it before
            //keeps up the advantage of pushing opponent back
            if (pI.currentCell == lastCell && pI.currentCell.GetComponent<AdjacentCells>().controlledBy != pI.teamNumber)
            {
                DisputedCell(lastCell);
                return;
            }
        }
    }

    void Ensnared(int teamNumber)
    {
        //check all cells to see if they have been surrounded by the same team
        List<GameObject> cells = GetComponent<MeshGenerator>().cells;
        //for each cell
        for (int i = 0; i < cells.Count; i++)
        {
            //only do a check for cells which are frontline
            AdjacentCells thisAdjacentCells = cells[i].GetComponent<AdjacentCells>();
            if (thisAdjacentCells.controlledBy != -1)
                continue;

            //skip if any player is this cell
            if (PlayerOnCell(cells[i]))
                continue;

            //for each adjacent cell on cell
            List<GameObject> adjacents = thisAdjacentCells.adjacentCells;
            int ownedByThisPlayer = 0;
            int disputed = 0;

            for (int j = 0; j < adjacents.Count; j++)
            {
                //who controls it?
                //if anyone controls it
                int controlledBy = adjacents[j].GetComponent<AdjacentCells>().controlledBy;
                if (controlledBy == teamNumber)
                {
                    //count who controls it
                    ownedByThisPlayer++;
                }
                else if (adjacents[j].GetComponent<AdjacentCells>().controlledBy == -1)
                {
                    //keep a count of how many were frontline cells too
                    disputed++;
                }
            }

            if (ownedByThisPlayer + disputed == adjacents.Count)
            {
                //take central ensnared cell for player with cells ensnaring 
                CaptureCell(teamNumber, cells[i]);
                //Debug.Log("captured");
            }

        }
    }


    bool PlayerOnCell(GameObject cell)
    {
        bool playerOnCell = false;
        for (int j = 0; j < pgi.playerGlobalList.Count; j++)
        {
            if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == cell)
            {
                playerOnCell = true;
                break;
            }
        }

        return playerOnCell;
    }

    void Capture(int thisPlayer)
    {

        //work out who owns the cell just landed on by the passed player

        GameObject currentCell = pgi.playerGlobalList[thisPlayer].GetComponent<PlayerInfo>().currentCell;

        //if player already owns this cell, it shouldn't change when we land on it - we can return from here
        if (PlayerOwnsCell(currentCell, thisPlayer))
            return;

        //if another team is already on this cell, we can't take it - must kill or push them off
        for (int j = 0; j < pgi.playerGlobalList.Count; j++)
        {
            if (thisPlayer == j)
                continue;

            if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == pgi.playerGlobalList[thisPlayer].GetComponent<PlayerInfo>().currentCell)
            {
                return;
            }
        }

        bool onlyPlayerOnCell = true;
        bool otherPlayerOnAdjacentCell = false;
        bool otherPlayerControlsAdjacent = false;

        for (int j = 0; j < pgi.playerGlobalList.Count; j++)
        {
            //skip our own player
            if (thisPlayer == j)
                continue;

            GameObject otherCurrentCell = pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell;



            //look for another player on this cell
            if (currentCell != null)//if not dead or floating in mid air between cells
            {
                if (currentCell == otherCurrentCell)
                {
                    //some one else is here
                    onlyPlayerOnCell = false;
                }
            }

            //look for another player on this cell's adjacent cells
            List<GameObject> adjacents = currentCell.GetComponent<AdjacentCells>().adjacentCells;
            for (int a = 0; a < adjacents.Count; a++)
            {
                if (adjacents[a] == otherCurrentCell)
                {
                    //another player is on this adjacent cell
                    otherPlayerOnAdjacentCell = true;

                }

                //look for the adjacent cell being controlled by an opponent
                if (adjacents[a].GetComponent<AdjacentCells>().controlledBy == j)
                {
                    otherPlayerControlsAdjacent = true;
                }
            }
        }

        //if we are the only one on the cell and there is no opponent on an adjacent 
        if (onlyPlayerOnCell && !otherPlayerOnAdjacentCell && !otherPlayerControlsAdjacent)
        {
            CaptureCell(thisPlayer, currentCell);
        }
        else if (otherPlayerOnAdjacentCell || otherPlayerControlsAdjacent)
        {
            //we can make this a disputed cell
            DisputedCell(currentCell);
        }
    }

    bool PlayerOwnsCell(GameObject currentCell, int thisPlayer)
    {
        bool alreadyOwnedByThisPlayer = false;
        if (currentCell.GetComponent<AdjacentCells>().controlledBy == thisPlayer)
            alreadyOwnedByThisPlayer = true;

        return alreadyOwnedByThisPlayer;
    }

    void DisputedCell(GameObject disputedCell)
    {
        //remove from all players if they have it and make current cell controlled by -1 (disputed index)
        for (int a = 0; a < pgi.playerGlobalList.Count; a++)
        {
            List<GameObject> cellsOwnedByOther = pgi.playerGlobalList[a].GetComponent<PlayerInfo>().cellsUnderControl;

            cellsOwnedByOther.Remove(disputedCell);
        }

        disputedCell.GetComponent<AdjacentCells>().controlledBy = -1;
    }

    void CaptureCell(int i, GameObject cell)
    {
        cell.GetComponent<AdjacentCells>().controlledBy = pgi.playerGlobalList[i].GetComponent<PlayerInfo>().teamNumber;// i;

        //add to list on player if not already
        if (!pgi.playerGlobalList[i].GetComponent<PlayerInfo>().cellsUnderControl.Contains(cell))
        {
            pgi.playerGlobalList[i].GetComponent<PlayerInfo>().cellsUnderControl.Add(cell);
        }

        //remove from other players
        for (int a = 0; a < pgi.playerGlobalList.Count; a++)
        {
            if (a == i)
                continue;

            List<GameObject> cellsOwnedByOther = pgi.playerGlobalList[a].GetComponent<PlayerInfo>().cellsUnderControl;

            cellsOwnedByOther.Remove(cell);
        }
    }

    void Spread(GameObject player)
    {
        //if player makes a line of cells, gather all to his side
        //find any other edges
        List<GameObject> cellsOwned = player.GetComponent<PlayerInfo>().cellsUnderControl;
        List<GameObject> edgeCells = new List<GameObject>();
        for (int i = 0; i < cellsOwned.Count; i++)
        {
            if (cellsOwned[i].GetComponent<AdjacentCells>().edgeCell)
                edgeCells.Add(cellsOwned[i]);
        }


        //still to figure out // W.I.P
    }

    void FrontLine()
    {
        //find front line cells

        //it is a frontline cell if there are at least two neighbouring/adjacent cells which belong to at leat two players

        //conditions : Can't be made in to a frontline cell if cell is fully built up. This cell is considered safe

        //special case: if cell belongs to plater 1 and player 2 has the only claim on it, it becomes frontline(if not built up)

        for (int i = 0; i < cells.Count; i++)
        {

            List<GameObject> frontlineCells = new List<GameObject>();

            List<GameObject> thisCellAdjacents = cells[i].GetComponent<AdjacentCells>().adjacentCells;
            List<int> otherOwners = new List<int>();
            int thisOwned = 0;
            int thisControlledBy = cells[i].GetComponent<AdjacentCells>().controlledBy;
            for (int j = 0; j < thisCellAdjacents.Count; j++)
            {
                int otherControlledBy = thisCellAdjacents[j].GetComponent<AdjacentCells>().controlledBy;

                if (thisControlledBy != otherControlledBy && thisCellAdjacents[j].GetComponent<AdjacentCells>().controlledBy != -1)
                {
                    otherOwners.Add(otherControlledBy);
                }
                else if (thisControlledBy == otherControlledBy && thisCellAdjacents[j].GetComponent<AdjacentCells>().controlledBy != -1)
                {
                    thisOwned++;
                }
            }
            /*
            //if cell has at least one pal but also doesnt have enemy at the gates, it is not a frontline cell
            if (thisOwned >0 && otherOwners.Count ==0)
                cells[i].GetComponent<AdjacentCells>().frontlineCell = false;
            else
                cells[i].GetComponent<AdjacentCells>().frontlineCell = true;
                */

        }
    }


    void Height2()
    {
        //for each cell, check how many of its own adjacents are controlled by the same team
        for (int i = 0; i < cells.Count; i++)
        {
            int thisControlledBy = cells[i].GetComponent<AdjacentCells>().controlledBy;

            if (thisControlledBy < -1)
                continue;
            //frontline is capped when raising so we can just set this number to amount of adjacents in total
            List<GameObject> adjacents = cells[i].GetComponent<AdjacentCells>().adjacentCells;

            if (thisControlledBy == -1)
            {
               // cells[i].GetComponent<AdjacentCells>().targetY = (adjacents.Count);// * heightMultiplier) + minHeight;
               // continue;
            }

            float adjacentControlledBySame = 0;            

            for (int j = 0; j < adjacents.Count; j++)
            {
                if (thisControlledBy == adjacents[j].GetComponent<AdjacentCells>().controlledBy)
                {
                    adjacentControlledBySame++;
                }
            }

            //set target Y to this
            //  if (adjacentControlledBySame < minHeight) // when might this happen?
            //      adjacentControlledBySame = minHeight;
            //  else
            adjacentControlledBySame = minHeight + (adjacentControlledBySame * heightMultiplier);

            cells[i].GetComponent<AdjacentCells>().targetY = adjacentControlledBySame;
        }
    }

    void Adjacents()
    {
        if (skipFrame)
        {
            skipFrame = false;
            return;
        }

        coloursForEachCell.Clear();

        for (int i = 0; i < cells.Count; i++)
        {
            //first of all blank it
            cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
            //list to add to global colour list
            List<int> colours = new List<int>();

            //go through each cell and check if it exist in any of the player lists for cell info, target adjacent etc
            for (int j = 0; j < pgi.playerGlobalList.Count; j++)
            {
                List<GameObject> thisAdjacents = pgi.playerGlobalList[j].GetComponent<PlayerMovement>().currentAdjacents;
                for (int a = 0; a < thisAdjacents.Count; a++)
                {
                    if (thisAdjacents[a] == cells[i])
                    {
                        //draw blue
                        //

                        //remember which player this was
                        colours.Add(j);
                    }
                }
            }

            coloursForEachCell.Add(colours);
        }

        //now go through and render for each colour

        for (int i = 0; i < coloursForEachCell.Count; i++)
        {
            //if only one colour in list, render whole cell for the player's colour

            for (int j = 0; j < coloursForEachCell[i].Count; j++)
            {
                //index will match cell number


                if (coloursForEachCell[i].Count == 1)
                {
                    if (coloursForEachCell[i][j] == 0)
                        cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue1") as Material;
                    else if (coloursForEachCell[i][j] == 1)
                        cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;
                }
                else
                {
                    cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red1") as Material;
                }
            }
        }

        //now paint over current cells
        for (int i = 0; i < pgi.playerGlobalList.Count; i++)
        {
            if (i == 0)
                pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;
            else if (i == 1)
                pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red2") as Material;
        }
    }
}
