using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayDrawer : MonoBehaviour {

    //receives positions from each player and draws correct overlay for each cell
    int playerAmount = 0;
    List<GameObject> cells;
    PlayerGlobalInfo pgi;
    PlayerClassValues playerClassValues;
    List<List<int>> coloursForEachCell = new List<List<int>>();

    public bool resetCells = false;
    // Use this for initialization

    bool skipFrame = true;
    public bool doHeights;
    public bool doHomeCellHeightsToStart;
    public bool reduceFrontline;
    public bool doCapture;
    // public float targetY;
    public float heightSpeed = .1f;
    public float heightSpeedSiege = .01f;
    public float heightMultiplier = 1f;//i think if this gets changed, maybe need to alter maxclimb height?
    public float minHeight = 3f;
    public int totalCellsTransparent = 0;
    
    //holds all meshes off voronoi generation before we altered them
    public List<Mesh> originalMeshes = new List<Mesh>();
    void Start ()
    { 
       // playerAmount = GetComponent<Spawner>().playerAmount;
        cells = GetComponent<MeshGenerator>().cells;
        pgi = GetComponent<PlayerGlobalInfo>();
        playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();

      //  targetY = minHeight;
	}
	
	// Update is called once per frame
	void Update ()
    {

        

        
	}

    private void FixedUpdate()
    {
        playerAmount = pgi.playerGlobalList.Count;

        FrontLine();

        if(doCapture)
            Capture();

        

        if (resetCells)
        {
            ResetCells();

            resetCells = false;
        }

        if (doHeights)
        {

            Height();//


            for (int i = 0; i < cells.Count; i++)
            {
                float tempHeightSpeed = heightSpeed;

                //if frontline cell, raise height if standing on it - stop if two plyers re on it
                for (int j = 0; j < playerAmount; j++)
                {
                    //find out if this is a current cell
                    if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == cells[i] && cells[i].GetComponent<AdjacentCells>().frontlineCell)
                    {
                        //find highest adjacent cell
                        float highest = 0f;
                       
                        for (int k = 0; k < cells[i].GetComponent<AdjacentCells>().adjacentCells.Count; k++)
                        {
                            if(cells[i].GetComponent<AdjacentCells>().adjacentCells[k].transform.localScale.y > highest)
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

        if(doHomeCellHeightsToStart)
        {
            HomeCellsHeights();
        }

        if(reduceFrontline)
        {
            ReduceFrontline();
        }

    TransparentCells();
    }

    void ReduceFrontline()
    {
        //if no played is on the cell, make sure it is being reduced to lowest amount
        for (int i = 0; i < cells.Count; i++)
        {
            //look for frontliners
            if (!cells[i].GetComponent<AdjacentCells>().frontlineCell)
                continue;

            //look to see if any palyer's current cell
            bool current = false;
            for (int j = 0; j < playerAmount; j++)
            {
                if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == cells[i])
                {
                    current = true;
                    break;
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

    void HomeCellsHeights()
    {
        for (int i = 0; i < playerAmount; i++)
        {
            //to do
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



        for (int i = 0; i < playerAmount; i++)
        {
            
            Vector3 shootFrom = pgi.playerGlobalList[i].transform.position + Vector3.up * 0.5f;//was causing glitch, still?
            Vector3 shotDir = Camera.main.transform.position - shootFrom;

           // Debug.DrawLine(shootFrom, Camera.main.transform.position);

            RaycastHit[] hits = Physics.RaycastAll(shootFrom, shotDir, shotDir.magnitude, LayerMask.GetMask("Cells"));

            for (int j = 0; j < hits.Length; j++)
            {   
                Material mat= null;

                //frontline?
                if (hits[j].transform.GetComponent<AdjacentCells>().frontlineCell)
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

  

    void Capture()
    {
        //turns cells to player colour

        //for each player check what cell they are on
        //compare against other player's cell
        for (int i = 0; i < playerAmount; i++)
        {
            if (pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell == null)
                continue;

            bool onlyPlayerOnCell = true;
           //s bool colourForPlayer = false;
            for (int j = 0; j < playerAmount; j++)
            {
                //skip our own player
                if (i == j)
                    continue;

              

                    //if both have current cells
                if (pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell != null)//if not dead
                {
                    
                    if (pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell == null)//if other player is dead
                    {
                        //colourForPlayer = true;
                    }
                    else if (pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell == pgi.playerGlobalList[j].GetComponent<PlayerInfo>().currentCell)
                    {
                        //some one else is here
                        onlyPlayerOnCell = false;
                      //  pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Disputed") as Material;

                      //  pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<AdjacentCells>().controlledBy = -2;//-2 disputed, -1 neutral

                    //don't change anything here, the first to control the cell gets to hold unitl defeated or retreated
                    }                    
                 
                }
            }

            if (onlyPlayerOnCell)
            {
                //  if (colourForPlayer)
                {
                    //can't grab a frontline cell, need to break the line to get control
                    // if(!pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<AdjacentCells>().frontlineCell)
                    //if (pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell != null)//if not dead
                    pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<AdjacentCells>().controlledBy = i;

                    //add to list on player if not already
                    if (!pgi.playerGlobalList[i].GetComponent<PlayerInfo>().cellsUnderControl.Contains(pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell))
                    {
                        pgi.playerGlobalList[i].GetComponent<PlayerInfo>().cellsUnderControl.Add(pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell);
                    }

                    //remove from other players
                    for (int a = 0; a < playerAmount; a++)
                    {
                        if (a == i)
                            continue;

                        List<GameObject> cellsOwnedByOther = pgi.playerGlobalList[a].GetComponent<PlayerInfo>().cellsUnderControl;

                        cellsOwnedByOther.Remove(pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell);
                    }

                    //check for edge cell
                    bool edgeCell = pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<AdjacentCells>().edgeCell;

                    if (edgeCell)
                    {
                        Spread(pgi.playerGlobalList[i]);
                    }
                }
            }
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

        // for each cell, find if it has two different player's cells adjacent to it

        for (int i = 0; i < cells.Count; i++)
        {
            
            List<GameObject> frontlineCells = new List<GameObject>();

            List<GameObject> thisCellAdjacents = cells[i].GetComponent<AdjacentCells>().adjacentCells;

            List<int> opponents = new List<int>();

            for (int j = 0; j < thisCellAdjacents.Count; j++)
            {
                int controlledBy = thisCellAdjacents[j].GetComponent<AdjacentCells>().controlledBy;

                //neutral, no frontline , or disputed
                if (controlledBy == -1 || thisCellAdjacents[j].GetComponent<AdjacentCells>().frontlineCell)
                    continue;

                else
                if (!opponents.Contains(controlledBy))
                    opponents.Add(controlledBy);

            }

            if (opponents.Count > 1)
            {
                //cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
                cells[i].GetComponent<AdjacentCells>().frontlineCell = true;
             //   cells[i].GetComponent<AdjacentCells>().controlledBy = -2;
            }
            else if(opponents.Count == 0)
            {
                cells[i].GetComponent<AdjacentCells>().frontlineCell = false;
                //cells[i].GetComponent<AdjacentCells>().controlledBy = opponents[0];
            }
            else if (opponents.Count == 1)
            {
                cells[i].GetComponent<AdjacentCells>().frontlineCell = false;
                //cells[i].GetComponent<AdjacentCells>().controlledBy = opponents[0];
            }

        }
    }

    void Height()
    {
        //works out cell's max height allowed. Height capped by how many adjacent cells the player owns and adjusted by the player


       // List<GameObject> frontlineCells = new List<GameObject>();

        //check player
        for (int i = 0; i < playerAmount; i++)
        {
            //check each player's owned adjacent cells
            List<GameObject> thisCells = pgi.playerGlobalList[i].GetComponent<PlayerInfo>().cellsUnderControl;
              
           // for (int j = 0; j < playerAmount; j++)
            {
                //against all other players
            //    if (i == j)
            //        continue;

                //List<GameObject> otherCells = pgi.playerGlobalList[j].GetComponent<PlayerInfo>().cellsUnderControl;

                for (int a = 0; a < thisCells.Count; a++)
                {

                    List<GameObject> thisAdjacents = thisCells[a].GetComponent<AdjacentCells>().adjacentCells;
                    List<GameObject> adjacentCellsOwned = new List<GameObject>();
                  //  List<GameObject> enemyCellsOwned = new List<GameObject>();

                    //check how many cells that we own are adjacent to each other
                    for (int b = 0; b < thisAdjacents.Count; b++)
                    {

                        //if frontline, and owned by other player, skip
                        //if (frontlineCells.Contains(thisAdjacents[b]) && otherCells.Contains(thisAdjacents[b]))
                        //if(thisAdjacents[b].GetComponent<AdjacentCells>().frontlineCell && otherCells.Contains)
                          //  continue;

                        if (thisCells.Contains(thisAdjacents[b]))
                        {
                            if(!thisAdjacents[b].GetComponent<AdjacentCells>().frontlineCell)
                                adjacentCellsOwned.Add(thisAdjacents[b]);
                        }
                    }

                    float tY = adjacentCellsOwned.Count * heightMultiplier;// - enemyCellsOwned.Count * 3;
                    if (tY < minHeight )
                        tY = minHeight;

                    if(thisCells[a].GetComponent<AdjacentCells>().frontlineCell == false)
                       thisCells[a].GetComponent<AdjacentCells>().targetY = minHeight + tY;
                    else
                        thisCells[a].GetComponent<AdjacentCells>().targetY = minHeight*heightMultiplier;
                }
            }
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
            for (int j = 0; j < playerAmount; j++)
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
                        cells[i].GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0b") as Material;
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
        for (int i = 0; i < playerAmount; i++)
        {
            if (i == 0)
                pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0b") as Material;
            else if (i == 1)
                pgi.playerGlobalList[i].GetComponent<PlayerInfo>().currentCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red2") as Material;
        }
    }
}
