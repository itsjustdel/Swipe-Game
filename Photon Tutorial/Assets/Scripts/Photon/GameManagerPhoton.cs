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
            if(!PhotonNetwork.IsMasterClient )
                SpawnPlayer();
            else if(masterGetsPlayer)
                SpawnPlayer();


        }

        public static void SpawnPlayer()
        {
            Debug.Log("Spawning");
            GameObject playerPrefab = Resources.Load("PlayerPrefab") as GameObject;
            if (playerPrefab == null)
                Debug.Log("didnt find prefab");
            else
                Debug.Log("found prefab");

            PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
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

            //swipe object instantiation
            if(eventCode == 20)
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
                //get swipe start time
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
        }


        #endregion

    }
}