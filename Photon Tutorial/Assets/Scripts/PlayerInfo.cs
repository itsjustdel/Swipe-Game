using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class PlayerInfo : MonoBehaviour {

    //holds info about player, player number, health, points etc
    public bool respawn = true;
    public bool playerDespawned = true;
    public bool playerCanRespawn = true;
    public int playerNumber;//controller
    public int teamNumber = -1;
    public GameObject currentCell;
    public GameObject homeCell;
    public bool beenHit = false;
    public bool updateAdjacentCells = false;
    public bool updateCurrentCell = false;
    public float health = 100f;
    public bool healthRegen = false;
    private float targetHealth = 100f;
    public float healthAnimationSpeed = 1f;
    public float healthRegenSpeed = 0.05f;
    
    //top for primitive cylinder Unity
    //public int [] topVertices = new int[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 41, 43, 45, 46, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87 };
    public PlayerClassValues pgi;
    public List<GameObject> cellsUnderControl = new List<GameObject>();
    private void Start()
    {
        //force a respawn if this is our network player

        if(GetComponent<PhotonView>().IsMine)
        {
            playerDespawned = true;
            respawn = true;
        }


        pgi = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
    }



    private void Update()
    {
        if(playerDespawned)
        {
           //need respawn code, auto respawn atm
        }

        if(respawn)
        {

            RespawnPlayer();
            respawn = false;

        }

        //Health
        
        //if this gets any more complicated, put in own script
        if(healthRegen)
            HealthRegen();

        HealthVisualisation();
    }
    void HealthRegen()
    {
        if (currentCell == null)
            return;

        if(currentCell.GetComponent<AdjacentCells>().controlledBy == playerNumber)
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

    public void SetSpawnAvailable()
    {
        playerCanRespawn = true;
    }

    private void FixedUpdate()
    {
        //update current cell
        if (updateCurrentCell)        
            CurrentCell();        

        //call adjacent cells?
        if (updateAdjacentCells)
            AdjacentCells();
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

            //hit.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Grass") as Material;
            currentCell = hit.transform.gameObject;

        }
    }

}
