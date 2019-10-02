using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class CameraControl : MonoBehaviour {
    // How many units should we keep from the players
    public bool onlyFollowPlayer = true;
    public float zoomFactor = 1.5f;
    public float zoomDampener = 1f;
    public float followTimeDelta = 0.8f;
    public float followTimeDeltaHeight = 0.01f;
    public float nearBumpStop = 10f;
    public GameObject p0;
    public GameObject p1;
    private PlayerGlobalInfo pgi;
    public  List<GameObject> players;
    public bool bumped;
    OverlayDrawer overlayDrawer;
    public GameObject winner;
    public bool showWinner = false;
    
   public  float startingRotX;
    public float rotateForTransparentCells = 10;
    public float extraSpace = 5f;

    // Use this for initialization
    public void Start ()
    {
        overlayDrawer = GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>();
        pgi = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>();
        players = pgi.playerGlobalList;


        
        showWinner = false;

        
        
      //  GetComponent<CameraShake>().enabled = true;
       
	}
	
	// Update is called once per frame
	void Update ()
    {
        //find our locally controlled player
        if(p0 == null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].GetComponent<PhotonView>().IsMine)
                {
                    p0 = players[i];
                    break;
                }
            }
            //if still not found, wait til next frame and try again, we haven't spawned yet
            if (p0 == null)
                return;
            else
            {
                //set starting rotation so we know where to swing back to                
                startingRotX = transform.localRotation.eulerAngles.x;
                //adjust camera height base on player size
                if(p0.GetComponent<PlayerMovement>()!=null)
                    transform.localPosition += p0.GetComponent<PlayerMovement>().head.transform.localPosition.y * Vector3.up;
            }


        }
        if (!showWinner)
        {
            if (onlyFollowPlayer)
            {
                FollowPlayerOnly(GetComponent<Camera>(), p0);
            }
            else
            {
                //finds middle point of all players - good for local co-op etc
                FixedCameraFollowSmooth(GetComponent<Camera>(), players);
            }
        }
        else if (showWinner)
        {
            followTimeDelta = 0.05f;
            FollowWinner(GetComponent<Camera>());
        }

        //swing camera up
        RotateForTransparentCells();
	}

    void RotateForTransparentCells()
    {
        //for every cell rotate up a bit and zoom out a bit

        //swing up if we have transparent cells
        if (overlayDrawer.totalCellsTransparent > 0)
            transform.localRotation = Quaternion.Euler(new Vector3(transform.localRotation.eulerAngles.x + rotateForTransparentCells, transform.localRotation.y, transform.localRotation.z));
        //swing back down
        else
        {
            float x = transform.localEulerAngles.x - rotateForTransparentCells;
            if (x < startingRotX)
                x = startingRotX;

            transform.localRotation = Quaternion.Euler(new Vector3(x, transform.localRotation.y, transform.localRotation.z));
        }


        
        
    }

    public void FollowWinner(Camera cam)
    {
        if (players.Count == 0)
            return;
        //will need to add 3rd player etc
        float distance = winner.transform.position.magnitude;
        // Distance between objects
        //float distance = (t1.position - t2.position).magnitude;
        float mod = (distance / zoomDampener) * zoomFactor;
        Vector3 cameraDestination = winner.transform.position - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
        if (distance < nearBumpStop)///zoomDampener) * zoomFactor)
        {
            bumped = true;
            mod = (nearBumpStop / zoomDampener) * zoomFactor;
            cameraDestination = winner.transform.position - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
            cam.transform.parent.position = Vector3.Lerp(cam.transform.parent.position, cameraDestination, followTimeDelta);

            return;
        }
        else bumped = false;
        // Move camera a certain distance


        // Adjust ortho size if we're using one of those
        if (cam.orthographic)
        {
            // The camera's forward vector is irrelevant, only this size will matter
            cam.orthographicSize = distance;
        }
        // You specified to use MoveTowards instead of Slerp/if no too close

        cam.transform.parent.position = Vector3.Lerp(cam.transform.parent.position, cameraDestination, followTimeDelta);

        // Snap when close enough to prevent annoying slerp behavior
        if ((cameraDestination - cam.transform.position).magnitude <= 0.05f)
            cam.transform.parent.position = cameraDestination;
    }

    public void FixedCameraFollowSmooth(Camera cam, List<GameObject> players)
    {
        if (players.Count < 2)
            return;
        
        float distance = 0f;

        Vector3 avg = Vector3.zero;
        int playersUsed = 0;
        for (int i = 0; i < players.Count; i++)
        {
            //ony not do if both true
            if(players[i].GetComponent<PlayerInfo>().playerDespawned && players[i].GetComponent<PlayerInfo>().playerCanRespawn)
            {
                //don't count
            }
            else
            {
                avg += players[i].transform.position;
                playersUsed++;

                distance += players[i].transform.position.magnitude;
            }
        }
        //anchor to center by adding a player at vector.zero - obz dont need to add zero
        avg /= playersUsed;// + 1;
        // Midpoint we're after
        Vector3 midpoint = avg;// (t1.position + t2.position) / 2f;

        // Distance between objects
        //float distance = (t1.position - t2.position).magnitude;
        float mod = (distance/zoomDampener) * zoomFactor;
        Vector3 cameraDestination = midpoint - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
        if (distance < nearBumpStop)///zoomDampener) * zoomFactor)
        {
            bumped = true;
            mod = (nearBumpStop / zoomDampener) * zoomFactor;
            cameraDestination = midpoint - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
            Vector3 targetPos = Vector3.Slerp(cam.transform.parent.position, cameraDestination, followTimeDelta);
            if(float.IsNaN(targetPos.x))
            {
                return;
            }
            else
                cam.transform.parent.position = targetPos;

            return;
        }
        else bumped = false;
        // Move camera a certain distance
        

        // Adjust ortho size if we're using one of those
        if (cam.orthographic)
        {
            // The camera's forward vector is irrelevant, only this size will matter
            cam.orthographicSize = distance;
        }
        // You specified to use MoveTowards instead of Slerp/if no too close
        
            cam.transform.parent.position = Vector3.Slerp(cam.transform.parent.position, cameraDestination, followTimeDelta);

        // Snap when close enough to prevent annoying slerp behavior
        if ((cameraDestination - cam.transform.position).magnitude <= 0.05f)
            cam.transform.parent.position = cameraDestination;
    }

    void FollowPlayerOnly(Camera cam, GameObject player)
    {
        
        float distance = player.transform.position.magnitude;        
        float mod = (distance / zoomDampener) * zoomFactor;
        Vector3 cameraDestination = player.transform.position - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
        if (distance < nearBumpStop)///zoomDampener) * zoomFactor)
        {
            bumped = true;
            mod = (nearBumpStop / zoomDampener) * zoomFactor;
            cameraDestination = player.transform.position - cam.transform.forward * mod;// (distance/ zoomDampener) * zoomFactor;
            cam.transform.parent.position = Vector3.Lerp(cam.transform.parent.position, cameraDestination, followTimeDelta);

            return;
        }
        else bumped = false;
        // Move camera a certain distance


        // Adjust ortho size if we're using one of those
        if (cam.orthographic)
        {
            // The camera's forward vector is irrelevant, only this size will matter
            cam.orthographicSize = distance;
        }
        // You specified to use MoveTowards instead of Slerp/if no too close
        float targetX = Mathf.Lerp(cam.transform.parent.position.x, cameraDestination.x, followTimeDelta); Vector3.Lerp(cam.transform.parent.position, cameraDestination, followTimeDelta);
        float targetY = Mathf.Lerp(cam.transform.parent.position.y, cameraDestination.y, followTimeDeltaHeight);//height has different lerp value to stop sickengin bouncing        
        float targetZ = Mathf.Lerp(cam.transform.parent.position.z, cameraDestination.z, followTimeDelta);

        cam.transform.parent.position = new Vector3(targetX, targetY, targetZ);

        // Snap when close enough to prevent annoying slerp behavior
        if ((cameraDestination - cam.transform.position).magnitude <= 0.05f)
            cam.transform.parent.position = cameraDestination;

    }
}

