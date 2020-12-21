using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementHelper : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	public static void MoveToAdjacent(PlayerMovement pM,Transform transform)
    {
        //grab list
        List<GameObject> playerInfos = pM.codeObject.GetComponent<PlayerGlobalInfo>().playerGlobalList;
        //find what cell we are on

        for (int i = 0; i < pM.currentAdjacents.Count; i++)
        {
            //pM.currentAdjacents[i].transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
           // pM.GetComponent<PlayerInfo>().currentCell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("White") as Material;
        }

        
        List<CellAndAngle> cellsAndAngles = new List<CellAndAngle>();
        //checks here could perhaps be up one level for easie changes later
        if (!pM.moving && !pM.leftStickReset)
        {
           

            //find which cell is closest to stick direction
           // List<float> anglesMinus = new List<float>();
           // List<float> anglesPositive = new List<float>();

            
            
            for (int i = 0; i < pM.currentAdjacents.Count; i++)
            {
                
                //get angle of each cell relative to player position
                Vector3 centroid = pM.currentAdjacents[i].GetComponent<ExtrudeCell>().centroid;
                Vector3 dirToCellFromPlayer = (centroid - transform.position).normalized;

                float angle = SignedAngle(dirToCellFromPlayer, Vector3.right, Vector3.up);

                CellAndAngle cAndA = new CellAndAngle();
                cAndA.cell = pM.currentAdjacents[i];
                cAndA.angle = angle + 180;//gets rid of minus numbers
                cellsAndAngles.Add(cAndA);
                // currentAdjacents[i].name = angle.ToString() + "";
            }

            //use comparer (at bottom of script) to sort list by angle
            cellsAndAngles.Sort(SortByAngle);
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
            pM.stickAngle = SignedAngle(pM.lookDir, Vector3.right, Vector3.up) + 180;

            //catch stick angle if before first slice, this will be pointing at last slice
            //need to catch because last slice will be ober 360, but first slice won't startfor a nit, 23.3 e.g
           

            for (int i = 0; i < cellsAndAngles.Count - 1; i++)
            {
                //cellsAndAngles[i].cell.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;
            }

            if (pM.stickAngle < cellsAndAngles[0].sliceStart)
            {
                //point it to last cell, slice starts after 0, there is a gap
                //cellsAndAngles[cellsAndAngles.Count - 1].cell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;

                pM.target = cellsAndAngles[cellsAndAngles.Count - 1].cell.GetComponent<ExtrudeCell>().centroid;
                pM.targetCell = cellsAndAngles[cellsAndAngles.Count - 1].cell;

                for (int i = 0; i < cellsAndAngles.Count - 1; i++)
                {
                  //  cellsAndAngles[i].cell.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;
                }


               // Debug.Log("changed to last");
            }
            else if (pM.stickAngle > cellsAndAngles[cellsAndAngles.Count - 1].sliceEnd)
            {
                //this means it should point to first cell
                //cellsAndAngles[0].cell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;
                
                pM.target = cellsAndAngles[0].cell.GetComponent<ExtrudeCell>().centroid;
                pM.targetCell = cellsAndAngles[0].cell;

                for (int i = 1; i < cellsAndAngles.Count; i++)
                {
                  //  cellsAndAngles[i].cell.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;
                }

              //  Debug.Log("changed to first");
            }
            else
            {
                for (int i = 0; i < cellsAndAngles.Count; i++)
                {
                    float start = cellsAndAngles[i].sliceStart;
                    float end = cellsAndAngles[i].sliceEnd;
                    //catch last to first out of 360 range problem

                    if (pM.stickAngle > start && pM.stickAngle < end)
                    {
                        //cellsAndAngles[i].cell.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;

                        pM.target = cellsAndAngles[i].cell.GetComponent<ExtrudeCell>().centroid;
                        pM.targetCell = cellsAndAngles[i].cell;

                        //break?

                    }
                   // else
                       // cellsAndAngles[i].cell.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;




                 //   cellsAndAngles[i].cell.name = i.ToString() + " start is " + cellsAndAngles[i].sliceStart.ToString() + " angle is " + cellsAndAngles[i].angle.ToString() + " end is " + cellsAndAngles[i].sliceEnd.ToString();
                }

              
            }



            //if (targetCell != null)
            //     targetCell.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red") as Material;
            PlayerInfo pI = pM.GetComponent<PlayerInfo>();

            bool buttonLBPressed = false;
            if (pI.controllerNumber == 0)
            {
                if (Input.GetButton("LB_1"))///***needs changed to inputs directx
                    buttonLBPressed = true;
            }
            else if (pI.controllerNumber == 1)
            {
                if (Input.GetButton("LB_2"))
                    buttonLBPressed = true;
            }
            
            if (!buttonLBPressed) //don't move on block
            {
                pM.startTime = Time.time;
                pM.moving = true;
            }
            //targetFound = true;
            pM.lastTarget = transform.position;


            //is target available to move to?
            //check global list of player positions
            for (int i = 0; i < playerInfos.Count; i++)
            {
                //don't check this player
                if (pM.gameObject == playerInfos[i])
                    continue;

                //could be snappier lol
                if(playerInfos[i].GetComponent<PlayerInfo>().currentCell.GetComponent<ExtrudeCell>().centroid == pM.target)
                {
                    //don't allow a move
                    pM.moving = false;
                    
                    
                }
            }

            

        }

        for (int i = 0; i < cellsAndAngles.Count; i++)
        {
            //cellsAndAngles[i].cell.transform.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;

        }


        if (pM.moving)
        {
            // The center of the arc
            Vector3 center = (pM.lastTarget + pM.target) * 0.5F;


            //jump is same as distance
            float distance = (pM.lastTarget - pM.target).magnitude;
            //distance /= pM.jumpAmount;
            center += new Vector3(0, -distance/pM.walkBounceAmount, 0);//walkbounce amount changed..

            // Interpolate over the arc relative to center
            Vector3 riseRelCenter = pM.lastTarget - center;
            Vector3 setRelCenter = pM.target - center;

            //this fraction makes it take longer for longer distances - gameplay option, make it take the same length of time for each jump?
            //float fracComplete = (Time.time - pM.startTime) / (1f / pM.movementSpeed); //game play option *** same time for each cell jump
            float fracComplete = (Time.time - pM.startTime) / (distance/pM.movementSpeed);
            
            transform.position = Vector3.Slerp(riseRelCenter, setRelCenter, fracComplete);
            transform.position += center;


       
          //  Vector3 dirToTarget = (pM.target - transform.position).normalized;
            //we have found our target, lerp towards
           // transform.position += dirToTarget * pM.movementSpeed;

            if (fracComplete >= 1f)
            {
                //force the player to flick the tick again
                //if (leftStickReset)
                {
                    pM.moving = false;
                    pM.lastTarget = transform.position;
                    
                  
                }
            }
        }
    }

    public class CellAndAngle
    {
        public GameObject cell;
        public float angle;
        public float sliceStart;
        public float sliceEnd;
    }

    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 normal)
    {
        // angle in [0,180]
        float angle = Vector3.Angle(from, to);
        float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(from, to)));
        return angle * sign;
    }


    public static int SortByAngle(CellAndAngle c1, CellAndAngle c2)
    {
        return c1.angle.CompareTo(c2.angle);
    }
}
