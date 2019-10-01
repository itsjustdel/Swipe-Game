using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdjacentCells : MonoBehaviour {

    public List<GameObject> adjacentCells = new List<GameObject>();
    public List<GameObject> controlledAdjacents = new List<GameObject>();

    public bool edgeCell;
    public float targetY = 1f;
    public int controlledBy = -2;
   // public bool frontlineCell = false;

    public bool beingMadeTransparent = false;
    //attach to gameobject to store adjacent cells 

    private void Start()
    {
        

    }

    private void Update()
    {
        if (beingMadeTransparent)
            return;        
       
        if (controlledBy == -1)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Disputed") as Material;
        else if (controlledBy == 0)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team0b") as Material;
        else if (controlledBy == 1)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team1b") as Material;
        else if (controlledBy == 2)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team2b") as Material;
        else if (controlledBy == 3)
            GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Team3b") as Material;
        
    }
}
