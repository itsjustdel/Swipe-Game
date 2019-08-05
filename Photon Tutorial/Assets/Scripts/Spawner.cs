using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;





using Photon.Pun;
using Photon.Realtime;

using ExitGames.Client.Photon;

public class Spawner : MonoBehaviour {

    public bool spawnNoCells;

    public float baseSize = 3f;
    public float headSize = 6f;
    public float headHeight = 1f;//ALL IN INSPECTOR
    public float stabberStartLength = 1f;
    public float stabberStartWidth = 1f;
    public float shieldX = .66f;
    public float shieldY= .66f;
    public float shieldZ = 0.2f;
    public int playerAmount = 1;
    public List<GameObject> spawnedCells = new List<GameObject>();
     List<GameObject> cells;
    
    // Use this for initialization
    private void Awake()
    {
       // enabled = false;
    }
    public void Start ()
    {
        cells = GetComponent<MeshGenerator>().cells;


        List<GameObject> spawnCells = SpawnCells();
        SpawnPlayers(spawnCells);


    }

    List<GameObject> SpawnCells()
    {

        List<GameObject> spawnCells = new List<GameObject>();
        //gather edge cells

        List<GameObject> toSort = new List<GameObject>();

        //only grab edge cells
        for (int j = 0; j < cells.Count; j++)
        {
            if (cells[j].GetComponent<AdjacentCells>().edgeCell)
                toSort.Add(cells[j]);
        }

        toSort.Sort(delegate (GameObject a, GameObject b)
        {
            return  Vector3.SignedAngle(Vector3.right, a.transform.position, Vector3.up)
            .CompareTo(Vector3.SignedAngle(Vector3.right, b.transform.position, Vector3.up));
        });



        //now find the closest to 0,90,180,270 degrees (if 4 players)
        //move round an amount randomly to keep any advantage random (only applicable to 3 player i think)
        float rValue = 0.1026664f;// Random.Range(0f, 0.5f);// 0.9758801f;//  Random.value;
       // Debug.Log(rValue);
        float spin = (360f / playerAmount) * rValue;
        bool allFound = false;
        float i = -180;// + spin;
        int found = 0;
        int safety =0;
        while (!allFound)
        {
         //   Debug.Log(i);

            safety++;
            if (safety > 10)
            {
                allFound = true;
                Debug.Log("Broke, i = " + i.ToString());
                Debug.Log("Random value = " + rValue.ToString());
            }

            for (int j = 0; j < toSort.Count; j++)
            {
                //Debug.Log("j = " + j.ToString());
                float check = i + spin;
                if (check >= 180)
                    check -= 360;
                if (Vector3.SignedAngle(Vector3.right, toSort[j].transform.position, Vector3.up) < check)
                    continue;

                //GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                //c.transform.position = toSort[j].transform.position;
                //c.transform.localScale *= 10;
                //c.name = Vector3.SignedAngle(Vector3.right, toSort[j].transform.position, Vector3.up).ToString();

                spawnCells.Add(toSort[j]);

                found++;
                if (found == playerAmount)
                    allFound = true;

                i += 360f / playerAmount;
                //restart loop
                if (i >= 180)
                {
                 //   Debug.Log("Resetting i = " + i.ToString());
                    i -= 360;
                //    Debug.Log("Reset i = " + i.ToString());
                }
                    

                
                break;
            }
        }

        return spawnCells;

    }

    //from https://forum.unity.com/threads/how-to-get-a-360-degree-vector3-angle.42145/
    float Angle360(Vector3 from, Vector3 to, Vector3 right)
    {
        float angle = Vector3.Angle(from, to);
        return (Vector3.Angle(right, to) > 90f) ? 360f - angle : angle;
    }

    void SpawnPlayers(List<GameObject> spawnCells)
    {
        for (int i = 0; i < playerAmount; i++)
        {
            SpawnPlayer(i, spawnCells[i]);
        }        
    }

    Vector3 ChooseCell()
    {
        //choose cell
        List<GameObject> cells = GetComponent<MeshGenerator>().cells;
        //dont place on a cell already with a player
        int r = Random.Range(0, cells.Count - 1);
        while (spawnedCells.Contains(cells[r]))
            r = Random.Range(0, cells.Count - 1);
        GameObject cellToSpawnTo = cells[r];
        spawnedCells.Add(cells[r]);

        //add centroid to extrude height for spanw point
        float yPos = cellToSpawnTo.GetComponent<ExtrudeCell>().depth;
        Vector3 centroid = GetCentroid(cellToSpawnTo, transform);
        Vector3 spawnPos = new Vector3(centroid.x, yPos, centroid.z) + cellToSpawnTo.transform.position;

        return spawnPos;
    }

    public static Vector3 RandomPosition()
    {
        Vector3 randomPosition = new Vector3(Random.Range(-50,50), 0f, Random.Range(-50, 50));

        return randomPosition;
    }

    void SpawnPlayer(int playerNumber,GameObject homeCell)
    {
       // GameObject player = CreatePrefab(playerNumber,homeCell,baseSize,headSize)
        //SpawnOnNetwork(player);

        ///probs need to fetch player list
        //add to global list of players
  //      GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList.Add(player);


//        player.transform.position = homeCell.GetComponent<ExtrudeCell>().centroid + Vector3.up * 10;//adding height to starting cell

    }

    public static GameObject CreatePrefab(int playerNumber,float baseSize,float headSize,float headHeight,float shieldX,float shieldY,float shieldZ)
    {

        //spawn
        GameObject player = new GameObject();
        player.name = "Player " + playerNumber.ToString();
        //add child for mesh
        GameObject playerMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/White") as Material;
        playerMesh.transform.position = player.transform.position;
        playerMesh.transform.localScale *= baseSize;
        playerMesh.name = "BaseMesh";

        playerMesh.transform.parent = player.transform;

        //add info script
        PlayerInfo pI = player.AddComponent<PlayerInfo>();
        pI.playerNumber = playerNumber;
       // pI.homeCell = homeCell;



        //add movementy script
        player.AddComponent<PlayerMovement>();
        //add attack script
        player.AddComponent<PlayerAttacks>();
        //add swipe attack script
        player.AddComponent<Swipe>();
        //add cell height controller
        player.AddComponent<CellHeights>();
        //controller input
        player.AddComponent<Inputs>();
        //vibration info
        player.AddComponent<PlayerVibration>();
        //sound
        player.AddComponent<PlayerSounds>();
        //tree for diufferent sound types in player
        GameObject soundParent = new GameObject();
        soundParent.name = "Sounds";
        soundParent.transform.parent = player.transform;
        //walk noise
        GameObject walkObject = new GameObject();
        walkObject.name = "Walk";
        walkObject.transform.parent = soundParent.transform;
        //noise
        walkObject.AddComponent<NoiseMaker>();
        // walkObject.AddComponent<ProceduralAudioController>();
        walkObject.AddComponent<AudioSource>();


        //head
        GameObject child = new GameObject();
        child.transform.position = Vector3.up * headHeight;
        child.name = "Head";
        //add mesh object
        GameObject headMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headMesh.name = "HeadMesh";
        headMesh.transform.parent = child.transform;
        headMesh.transform.localScale *= headSize;
        headMesh.transform.position = child.transform.position;
        headMesh.layer = LayerMask.NameToLayer("PlayerBody");
        //add collider stuff to do bumps from player
        headMesh.AddComponent<Rigidbody>().isKinematic = true;
        //
        headMesh.AddComponent<PlayerCollision>();
        headMesh.GetComponent<BoxCollider>().isTrigger = true;
        //disable mesh renderer
        headMesh.GetComponent<MeshRenderer>().enabled = false;
        //add children which will visualise player health

        for (int i = 0; i < 2; i++)
        {
            //anchor so scales easy
            GameObject headMesh0 = new GameObject();
            headMesh0.transform.parent = headMesh.transform;
            if (i == 0)
                headMesh0.transform.position = headMesh.transform.position + Vector3.down * headSize * 0.5f;
            else
                headMesh0.transform.position = headMesh.transform.position + Vector3.up * headSize * 0.5f;


            //actual cube we will see
            GameObject headMesh0a = GameObject.CreatePrimitive(PrimitiveType.Cube);


            if (i == 0)
                headMesh0a.transform.position = headMesh0.transform.position + (headSize * 0.5f) * Vector3.up;
            else
                headMesh0a.transform.position = headMesh0.transform.position - (headSize * 0.5f) * Vector3.up;

            headMesh0a.transform.parent = headMesh0.transform;

            headMesh0a.transform.localScale = Vector3.one * headSize;
            //first cube is blood cube - no blood to start
            if (i == 0)
                headMesh0.transform.localScale = new Vector3(headMesh0.transform.localScale.x, 0f, headMesh0.transform.localScale.z);

            if (i == 0)
            {
                headMesh0a.name = "Blood";
                headMesh0a.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Red0") as Material;
            }
            else
                headMesh0a.name = "Health";

            headMesh0a.GetComponent<BoxCollider>().enabled = false;
        }

        GameObject noseMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        noseMesh.name = "NoseMesh";
        noseMesh.transform.parent = child.transform;
        noseMesh.transform.localScale *= headSize * .5f;
        noseMesh.transform.position = child.transform.position + Vector3.forward * headSize * .5f;
        noseMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/White") as Material;
        Destroy(noseMesh.GetComponent<BoxCollider>());

        //stabber/sword
        /*
        GameObject stabber = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stabber.transform.rotation *= Quaternion.Euler(90, 0, 0);
        stabber.transform.position = child.transform.position;
        stabber.transform.position += Vector3.forward * ((headSize * .5f));// +(stabberStartLength));        
                                                                           //now alter vertices
        Vector3[] vertices = stabber.GetComponent<MeshFilter>().mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            //vertices[i] += Vector3.up * 1f;// ( stabberStartLength + headSize*.5f);
            //scale mesh
            vertices[i].x *= stabberStartWidth;
            vertices[i].y *= stabberStartLength;
            vertices[i].z *= stabberStartWidth;

            //put bottom vertices at zero, flush agaisnt cube
            if (vertices[i].y < 0f)
                vertices[i].y = 0f;

        }

        stabber.GetComponent<MeshFilter>().mesh.vertices = vertices;

        //stabber.transform.localScale = new Vector3(stabberStartWidth, stabberStartLength, stabberStartWidth);
        stabber.name = "Stabber";
        */
        //shield

        GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Cube);

        shield.transform.position = child.transform.position;
        shield.transform.position += Vector3.forward * ((headSize * .5f) + (shieldZ * 2));
        shield.name = "Shield";
        shield.layer = LayerMask.NameToLayer("Shield");
        shield.transform.localScale = new Vector3(shieldX, shieldY, shieldZ);
        shield.AddComponent<ShieldCollision>();
        shield.GetComponent<BoxCollider>().isTrigger = true;
        shield.AddComponent<Rigidbody>().isKinematic = true;
        //we will use a shield pivot
        GameObject shieldPivot = new GameObject();
        shieldPivot.transform.position = child.transform.position;
        shieldPivot.name = "ShieldPivot";



        /*
        GameObject shieldMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shieldMesh.name = "SheildMesh";
        shieldMesh.transform.parent = shield.transform;
        
        */

        /*
        //swiper 
        GameObject swiper = new GameObject();
        swiper.AddComponent<MeshRenderer>();
        swiper.AddComponent<MeshFilter>();
        swiper.transform.position = child.transform.position;
        swiper.name = "Swiper";
        */

        child.transform.parent = player.transform;
        //stabber.transform.parent = child.transform;
        shield.transform.parent = shieldPivot.transform;
        shieldPivot.transform.parent = child.transform;
        //swiper.transform.parent = player.transform;

        //start shield pointing to the ground now we ahve parent collider to it
        shieldPivot.transform.localRotation = Quaternion.Euler(90, 0, 0);





        //homeCell.transform.localScale = new Vector3(1f, 50f, 1f);/droppping atm, cool idea tho/////*********

        if (playerNumber == 0)
        {
            // playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan0") as Material;
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan0") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan0") as Material;
            // stabber.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Blue") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Cyan1") as Material;
            // swiper.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { Resources.Load("Red1") as Material };//,Resources.Load("Blue1") as Material};
        }
        else if (playerNumber == 1)
        {
            //  playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
            //  stabber.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red2") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange1") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Orange0") as Material;
            //   swiper.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { Resources.Load("Blue1") as Material };//, Resources.Load("Red1") as Material };
        }
        else if (playerNumber == 2)
        {
            //   playerMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;
            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green0") as Material;
            //  stabber.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Red2") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Green1") as Material;

            //   swiper.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { Resources.Load("Blue1") as Material };//, Resources.Load("Red1") as Material };
        }
        else if (playerNumber == 3)
        {

            headMesh.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue0") as Material;
            headMesh.transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue0") as Material;
            shield.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/DeepBlue2") as Material;

        }




        //audio
        ProceduralAudioController pAC = player.AddComponent<ProceduralAudioController>();
        // pAC.guide = true;
        pAC.useSinusAudioWave = true;

        //player.AddComponent<SwipeSound>();
        //player.AddComponent<SineWaveExample>();

        PhotonView photonView = player.AddComponent<PhotonView>();
        
        return player;

    }

    void SpawnOnNetwork(GameObject player)
    {
        PhotonView photonView = player.GetComponent<PhotonView>();

        if (PhotonNetwork.AllocateViewID(photonView))
        {
            object[] data = new object[]
            {
            player.transform.position, player.transform.rotation, photonView.ViewID
            };

            RaiseEventOptions raiseEventOptions = new RaiseEventOptions
            {
                Receivers = ReceiverGroup.Others,
                CachingOption = EventCaching.AddToRoomCache
            };

            SendOptions sendOptions = new SendOptions
            {
                Reliability = true
            };

            byte spawnEventCode = 10;
            PhotonNetwork.RaiseEvent(spawnEventCode, data, raiseEventOptions, sendOptions);
        }
        else
        {
            Debug.LogError("Failed to allocate a ViewId.");

            Destroy(player);
        }
    }

    public static Vector3 GetCentroid(GameObject cell, Transform transform)
    {
        Vector3 centroid = new Vector3();

        Vector3[] vertices = cell.GetComponent<MeshFilter>().mesh.vertices;
        List<Vector2> v2List = new List<Vector2>();
        for (int i = 0; i < vertices.Length; i++)
        {
            v2List.Add(new Vector2(vertices[i].x, vertices[i].z));
        }

        Vector2 centroidV2 = GetCentroid(v2List);
        centroid = new Vector3(centroidV2.x, 0f, centroidV2.y) + transform.position;// + (7 * Vector3.up);
        
        return centroid;
    }

    public static Vector2 GetCentroid(List<Vector2> poly)
    {
        //https://stackoverflow.com/questions/9815699/how-to-calculate-centroid - converted to vector2 by me

        float accumulatedArea = 0.0f;
        float centerX = 0.0f;
        float centerY = 0.0f;

        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            float temp = poly[i].x * poly[j].y - poly[j].x * poly[i].y;
            accumulatedArea += temp;
            centerX += (poly[i].x + poly[j].x) * temp;
            centerY += (poly[i].y + poly[j].y) * temp;
        }

        if (Mathf.Abs(accumulatedArea) < 1E-7f)
            return Vector2.zero;  // Avoid division by zero

        accumulatedArea *= 3f;
        return new Vector2(centerX / accumulatedArea, centerY / accumulatedArea);
    }

}
