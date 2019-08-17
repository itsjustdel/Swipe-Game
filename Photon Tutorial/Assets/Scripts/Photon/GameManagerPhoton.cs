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

        private bool masterGetsPlayer = true;
        

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
            //check if master gets a player

            /*
            if(!PhotonNetwork.IsMasterClient )
                SpawnPlayer();
            else if(masterGetsPlayer)
                SpawnPlayer();

            */
        }
     

        public static void SpawnPlayer()
        {
            Debug.Log("Spawning");
            GameObject playerPrefab = Resources.Load("PlayerPrefab") as GameObject;
          
            Vector3 spawnPos = Vector3.zero;            
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity, 0);
        }

        void CreateNewSwipeObject(string type, bool overhead, bool sideSwipe, bool buttonSwipe,Vector3 firstPullBackLookDir,List<Vector3> centralPoints,float swipeTimeStart,int photonViewID)
        {
            Debug.Log("creating new swipe object");

            GameObject newSwipe = new GameObject();
            newSwipe.name = "swipe Current " + type;
            newSwipe.AddComponent<MeshFilter>();
            newSwipe.AddComponent<MeshRenderer>();
            SwipeObject sO = newSwipe.AddComponent<SwipeObject>();


            sO.parentPlayer = PhotonView.Find(photonViewID).gameObject;
            sO.parentPlayer.GetComponent<Swipe>().head =  sO.parentPlayer.transform.Find("Head").gameObject;
            newSwipe.transform.position = sO.parentPlayer.GetComponent<Swipe>().head.transform.position;
            newSwipe.layer = LayerMask.NameToLayer("Swipe");
            newSwipe.AddComponent<MeshCollider>();
            //currentSwipeObject = newSwipe;

            
            //find player by photon id

            
            sO.firstPullBackLookDir = firstPullBackLookDir;

            sO.local = false;

            //grabbing from swipe class attached to plyer - a bit jumbled
            PlayerClassValues playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
            sO.playerClassValues = playerClassValues;
            sO.activeTime = playerClassValues.overheadWhiffCooldown;
            sO.firstPullBackLookDir = firstPullBackLookDir;
            sO.swipeTimeStart = swipeTimeStart;

            if (overhead)
            {

                sO.overheadSwipe = true;
                //note time of the the user finishing their swipe plan            
                //pass planned points
                sO.centralPoints = new List<Vector3>(centralPoints);
            }
            if (sideSwipe)
            {
                sO.sideSwipe = true;
            }
            if (!overhead && !sideSwipe && !buttonSwipe)
            {
                //lunge
                sO.lunge = true;
            }
            if (buttonSwipe)
            {
                sO.buttonSwipe = true;
            }

            //only have two current swipes
            // if (currentSwipes.Count > 1 && currentSwipes.Count > 0)
            {
                //Destroy(currentSwipes[0]);
                //currentSwipes.RemoveAt(0);


            }
            //audio
            ProceduralAudioController pAC = newSwipe.AddComponent<ProceduralAudioController>();
            pAC.swipeObject = true;
            pAC.useSinusAudioWave = true;

            /// currentSwipes.Add(newSwipe);
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
            //request position of others //client asks master this
            if (eventCode == 10)
            {
                Debug.Log("Event 10 -[MASTER] - client requesting other positions on join");

                //get photon view id of who passed this call
                object[] customData = (object[])photonEvent.CustomData;                
                int initialPhotonViewID = (int)customData[0];


                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                int[] views = new int[players.Length];
                Vector3[] positions = new Vector3[players.Length];

                for (int i = 0; i < players.Length; i++)
                {
                    //get players photon view id (unique across network)                    
                    views[i] = players[i].GetComponent<PhotonView>().ViewID;
                    //get player position on master (this)
                    positions[i] = players[i].transform.position;
                }

                //pass back a list of viewID and a list of positions, and the photonview id of who requested it
                //To create less traffic, i could sen only to who requested it but i dont know how to do that atm

                byte evCode = 11; // Custom Event 11: 
                object[] content = new object[] { views, positions, initialPhotonViewID };
                //send to everyone but this client
                RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

                //keep resending until server receives
                SendOptions sendOptions = new SendOptions { Reliability = true };

                PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);
            }

            //positions sent from master            
            if(eventCode == 11)
            {
                object[] customData = (object[])photonEvent.CustomData;
                int viewID = (int)customData[2];
                PhotonView myPhotonView = PhotonView.Find(viewID);
                if (myPhotonView.IsMine)
                {
                    Debug.Log("Event 11 [CLIENT]- Received positions from master");

                
                    //check whether it was this client who requested positions
                    
                    Debug.Log("passed ID = " + viewID);

                



                    Debug.Log(myPhotonView);
                
                    Debug.Log("My id");
                    
                    int[] views = (int[])customData[0];
                    Vector3[] positions = (Vector3[])customData[1];

                    //find players with view ids in array and set positions
                    for (int i = 0; i < views.Length; i++)
                    {
                        PhotonView pV = PhotonView.Find(views[i]);
                        pV.transform.position = positions[i];
                    }

                }
            }

            //swipe object instantiation
            if (eventCode == 20)
            {
                Debug.Log("Event 20 - instantiate swipe");

                //get swipe start time
                object[] customData = (object[])photonEvent.CustomData;
                float swipeTimeStart = 0f;// (float)customData[0];
                Debug.Log("swipe time start = " + swipeTimeStart);
                Vector3 firstPullBackLookDir = (Vector3)customData[1];
                Vector3[] centralPointsArray = (Vector3[])customData[2];
                

                List<Vector3> centralPoints = new List<Vector3>(centralPointsArray);
                int photonViewID = (int)customData[3];
                CreateNewSwipeObject("Networked", true, false, false, firstPullBackLookDir, centralPoints, swipeTimeStart,photonViewID);

            }

            //updating walk targets
            if (eventCode == 21)
            {
               // Debug.Log("Event 21 - walk target update");
                //walkStartPos, walkStart, walkTarget,walkSpeedThisFrame, photonViewID 
                
                object[] customData = (object[])photonEvent.CustomData;
                Vector3 walkStartPos = (Vector3)customData[0];
                double walkStart = (double)customData[1];
                Vector3 walkTarget = (Vector3)customData[2];
                float walkSpeedThisFrame = (float)customData[3];
                int photonViewID = (int)customData[4];

                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                PlayerMovement pM = viewOwner.GetComponent<PlayerMovement>();
                pM.walkStartPos = walkStartPos;
                pM.walkStart = walkStart;
                pM.walkTarget = walkTarget;
                pM.walkSpeedThisFrame = walkSpeedThisFrame;
                pM.walking = true;
                

            }

            //updating bump targets//movement script does the rest
            if (eventCode == 22)
            {                
                object[] customData = (object[])photonEvent.CustomData;                
                
                Vector3 bumpTarget = (Vector3)customData[0];
                
                int photonViewID = (int)customData[1];

                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;

                PlayerMovement pM = viewOwner.GetComponent<PlayerMovement>();
                
                pM.bumpTarget = bumpTarget;

                pM.bumped = true;
                pM.walking = false;

            }
        }


        #endregion

    }
}