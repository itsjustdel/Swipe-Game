﻿using System.Collections;
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
    public int playerAmount = 3;//set this where ?
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


        List<GameObject> spawnCells = SpawnCells(cells, playerAmount);
        SpawnPlayers(spawnCells);


    }

    public static List<GameObject> SpawnCells(List<GameObject> cells, int teams)//if teams = 2, breaks?
    {
        Debug.Log("Cells count = " + cells.Count);
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
        float spin = (360f / teams) * rValue;
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
                if (found == teams)
                    allFound = true;

                i += 360f / teams;
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
        float yPos = GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>().minHeight;// cellToSpawnTo.GetComponent<ExtrudeCell>().depth;
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