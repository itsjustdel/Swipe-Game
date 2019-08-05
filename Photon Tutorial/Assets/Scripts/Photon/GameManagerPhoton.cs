using System;
using System.Collections;
using System.Collections.Generic;


using UnityEngine;
using UnityEngine.SceneManagement;


using Photon.Pun;
using Photon.Realtime;

using ExitGames.Client.Photon;


namespace DellyWellyWelly
{
    public class GameManagerPhoton : MonoBehaviourPunCallbacks
    {

        public Vector3[] startData = new Vector3[0];
        bool mapLoaded = false;

        public void OnEnable()
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }

        public void OnDisable()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }
        #region Methods

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        public void LoadArena()
        {
            //if we are here it is because there is no map to play on,
            //if we are the client, request data to generate the map from the master client
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Client requesting map data from master");

                byte evCode = 0; // Custom Event 0: Used as "MoveUnitsToTargetPosition" event             

                //ask master client to send this client map data
                
                SendOptions sendOptions = new SendOptions { Reliability = true };
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };
                PhotonNetwork.RaiseEvent(evCode, null,raiseEventOptions,sendOptions);
            }
            //if we are the master client, we need to generate a map
            else
            {
                
                //create arena
                Debug.Log("Master Creating Map");
                //data for map generation
                MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
                //start generation
                mg.Lloyds();
                //grab randomly generated vector3 list
                List<Vector3> centroids = mg.previousCentroids;
                startData = new Vector3[centroids.Count];
                for (int i = 0; i < centroids.Count; i++)
                {
                    startData[i] = centroids[i];
                }
            

                mapLoaded = true;

                
            }

            //now we have our map, spawn a player!
            SpawnPlayer();


        }

        public void SpawnPlayer()
        {
            Debug.Log("Spawning");
            GameObject playerPrefab = Resources.Load("PlayerPrefab") as GameObject;
            if (playerPrefab == null)
                Debug.Log("didnt find prefab");
            else
                Debug.Log("found prefab");

            PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
        }

        #endregion

        #region Photon Callbacks

        public void OnEvent(EventData photonEvent)
        {

            byte eventCode = photonEvent.Code;


            if (eventCode == 0)//0 is map data request event code// client asking master
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("Master responding to Event 0 - sending map data to client");

                    byte evCode = 1;
                    Vector3[] content = startData;// new int[] { 0, 44, 3 };
                    RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
                }

                else
                {
                    Debug.Log("Event 0 is for master client, something has went wrong");
                }
            }

            if (eventCode == 1)
            {
                if (!PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("Client Receiving Map Data -  Event Code 1");

                    if (!mapLoaded)
                    {
                        Debug.Log("Client map not loaded, updating map data");

                        startData = (Vector3[])photonEvent.CustomData;

                        //feed mesh gen script these positions
                        MeshGenerator mg = GameObject.FindGameObjectWithTag("Code").GetComponent<MeshGenerator>();
                        mg.masterPoints = new List<Vector3>(startData);
                        mg.fillWithMasterPoints = true;
                        mg.fillWithPoints = false;
                        mg.fillWithRandom = false;
                        //dont relax anymore, points passed have been relaxed already
                        mg.lloydIterations = 1;

                        //start generation
                        mg.Lloyds();

                    }
                    else
                        Debug.Log("Client already received map data - ignore");
                }
                else
                    Debug.Log("Master sent itself map data");

              
            }
        }


        #endregion

    }
}