using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldSpawner : MonoBehaviour {

    public int roundTime = 180;
    
    public bool startWorld = true;
    public bool endWorld = false;
    public GameObject codeObject;
    public GameObject canvasObject;
    private GameObject worldInstance;
    private GameObject canvasInstance;

    CellMeter cellMeter;
	// Use this for initialization
	void Start ()
    {
       // cellMeter = GameObject.Find("Canvas").GetComponent<CellMeter>();
    }
	
	// Update is called once per frame
	void Update ()
    {



        if(startWorld)
        {

            canvasInstance = Instantiate(canvasObject);
            worldInstance = Instantiate(codeObject);
            
            //reset timer
            cellMeter = worldInstance.GetComponent<CellMeter>();            
            cellMeter.roundTime = roundTime;
            //re asign camera
            Camera.main.GetComponent<CameraControl>().Start();
            Camera.main.GetComponent<CameraControl>().enabled = true;

            startWorld = false;
        }

        if(endWorld)
        {
            //destroy all!
            //disable UI script

            Destroy(canvasInstance);
            
            //grab players first
            List<GameObject> players = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList;
            for (int i = 0; i < players.Count; i++)
            {
                Destroy(players[i]);
            }
            //now destroy object with code and cells and walls
            Destroy(worldInstance);

            //disabling cam script - prob need to write some transitiin code
            Camera.main.GetComponent<CameraControl>().enabled = false;

            endWorld = false;

            startWorld = true;
        }
		
	}
}
