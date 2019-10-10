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

        public bool startCanvas = false;
        public GameObject canvasPrefab;

        private bool masterGetsPlayer = true;
        

        public Vector3[] startData = new Vector3[0];
        bool mapLoaded = false;

        public void OnEnable()//add new?
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

                if(masterGetsPlayer)
                    SpawnPlayer();

                ActivatePlayers();

                if(startCanvas)
                    AddCanvas();

            }

             

            
        }

        public  void AddCanvas()
        {
            GameObject canvas =Instantiate(canvasPrefab);
            //GameObject canvas = GameObject.Find("Canvas(Clone)");
            canvas.GetComponent<Canvas>().enabled = false;
            //start timer too
            GameObject.FindGameObjectWithTag("Code").GetComponent<CellMeter>().EnableNextFrame();
            GameObject.FindGameObjectWithTag("Code").GetComponent<CellMeter>().currentRoundTime = 0;
        }
     

        public static void SpawnPlayer()
        {
            Debug.Log("Spawning");
            GameObject playerPrefab = Resources.Load("PlayerPrefab") as GameObject;
          
            Vector3 spawnPos = Vector3.zero;            
            PhotonNetwork.Instantiate(playerPrefab.name, spawnPos, Quaternion.identity, 0);
        }

        void ActivatePlayers()
        {
            //and enable all network players photon instantiated on join -  we kept them disabled
            List<GameObject> players = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList;
            
            Debug.Log("players count = " + players.Count);
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].GetComponent<PlayerStarter>() == null)
                    Debug.Log("no player start");

                players[i].GetComponent<PlayerStarter>().enabled = true;
            }
        }

        void CreateNewSwipeObject(string type, bool overhead, bool sideSwipe, bool buttonSwipe,Vector3 firstPullBackLookDir,List<Vector3> centralPoints,double swipeTimeStart,int photonViewID)
        {
          //  Debug.Log("creating new swipe object");

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
            sO.parentPlayer.GetComponent<Swipe>().currentSwipeObject = newSwipe;

            
            //find player by photon id

            
            sO.firstPullBackLookDir = firstPullBackLookDir;

            sO.local = false;

            //grabbing from swipe class attached to plyer - a bit jumbled
            PlayerClassValues playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();
            sO.playerClassValues = playerClassValues;
            //sO.activeTime = playerClassValues.overheadWhiffCooldown;
            sO.firstPullBackLookDir = firstPullBackLookDir;

            sO.swipeTimeStart = swipeTimeStart;

            if (overhead)
            {
                Swipe swipe = sO.parentPlayer.GetComponent<Swipe>();
                swipe.overheadSwiping = true;
                swipe.overheadAvailable = false;
                //add lunge/side swipe?
                sO.overheadSwipe = true;
                       
                //pass planned points
                sO.centralPoints = new List<Vector3>(centralPoints);
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

                        //now we can spawn player, we need the map to havew been created before we could do this
                        SpawnPlayer();

                        //turn all players on!
                        ActivatePlayers();

                        if(startCanvas)
                            //start UI
                            AddCanvas();



                    }
                    else
                        Debug.Log("Client already received map data - ignore");
                }
                else
                    Debug.Log("Master sent itself map data");

              
            }
            //game state request
            //request position of others //client asks master this //
            if (eventCode == 10)
            {
                Debug.Log("Event 10 -[MASTER] - client requesting game state");

                //get photon view id of who passed this call
                object[] customData = (object[])photonEvent.CustomData;                
                int initialPhotonViewID = (int)customData[0];

                //The first thing we will do is find the gameobject which matches the photon view and enable it, building the meshes and setting team
                PhotonView passedPhotonView = PhotonView.Find(initialPhotonViewID);
                //start script to build
                passedPhotonView.GetComponent<PlayerStarter>().enabled = true;


                List<GameObject> players = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList;// GameObject.FindGameObjectsWithTag("Player");
                int[] views = new int[players.Count];
                Vector3[] positions = new Vector3[players.Count];
                float[] healths = new float[players.Count];
                int[] teams = new int[players.Count];

                for (int i = 0; i < players.Count; i++)
                {
                    //get players photon view id (unique across network)                    
                    views[i] = players[i].GetComponent<PhotonView>().ViewID;
                    //get player position on master (this)
                    positions[i] = players[i].transform.position;
                  //  if (players[i].GetComponent<PlayerInfo>() == null)
                    {
                        //player needs new spawn, spawnerscript will take care of this
                    }
                   // else
                    {
                        //current health
                        healths[i] = players[i].GetComponent<PlayerInfo>().health;
                        //which team
                        teams[i] = players[i].GetComponent<PlayerInfo>().teamNumber;
                    }
                }

                //pass back a list of viewID and a list of positions, and the photonview id of who requested it
                //To create less traffic, i could sen only to who requested it but i dont know how to do that atm

                byte evCode = 11; // Custom Event 11: 
                object[] content = new object[] { initialPhotonViewID, views, positions, healths, teams};
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
                int viewID = (int)customData[0];
                

                PhotonView passedPhotonView = PhotonView.Find(viewID);
                //only update if it the client who requested data
                if (passedPhotonView.IsMine)
                {
                    Debug.Log("Event 11 [CLIENT]- Received game state from master");
                    //check whether it was this client who requested positions
                    //Debug.Log("passed ID = " + viewID);
                    //Debug.Log(passedPhotonView);                
                    //Debug.Log("My id");


                    //unpack more of what we need
                    int[] views = (int[])customData[1];
                    Vector3[] positions = (Vector3[])customData[2];
                    float[] healths = (float[])customData[3];
                    int[] teams = (int[])customData[4];

                    //find players with view ids in array and set positions, health, teams etc
                    for (int i = 0; i < views.Length; i++)
                    {
                        //don't update own
                        
                        PhotonView pV = PhotonView.Find(views[i]);

                        //use data passed from the network to update other clients
                        pV.transform.position = positions[i];
                        pV.GetComponent<PlayerInfo>().health = healths[i];
                        pV.GetComponent<PlayerInfo>().teamNumber = teams[i];

                        //start avator
                        pV.GetComponent<PrefabCreator>().enabled = true;


                    }

                }
            }

            //respawn
            if(eventCode == 12)
            {
                object[] customData = (object[])photonEvent.CustomData;
                int photonViewID = (int)customData[0];
                //get game object of the client that requested the spawn
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                //set respawn flag - this wont be exactly synced across servers unless i put in a delay - will do if i encounter desync
                viewOwner.GetComponent<PlayerInfo>().respawn = true;

            }

            //swipe object instantiation
            if (eventCode == 20)
            {
               // Debug.Log("Event 20 - instantiate swipe");

                //get swipe start time
                object[] customData = (object[])photonEvent.CustomData;
                double swipeTimeStart = (double)customData[0];
                //Debug.Log("swipe time start = " + swipeTimeStart);
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

            //clients receive info on a bump and overwrites any local prediction info
            if (eventCode == 22)
            {
                Debug.Log("[CLIENT] - Master overwriting bump target and bump start time");
                object[] customData = (object[])photonEvent.CustomData;

                
                int photonViewID = (int)customData[0];
                
                double bumpStartTime = (double)customData[1];
                Vector3 startPos = (Vector3)customData[2];
                Vector3 target = (Vector3)customData[3];

                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                
                PlayerMovement pM = viewOwner.GetComponent<PlayerMovement>();

                pM.bumpStartPos = startPos;
                pM.bumpTarget = target;
                pM.bumpStart = bumpStartTime;

                //should already be set unless desync?
                pM.bumped = true;
                pM.bumpInProgress = true;
                pM.walking = false;

            }

            //shield

            if (eventCode == 23)
            {

                Debug.Log("[CLIENT] - Getting shield UP data");
                object[] customData = (object[])photonEvent.CustomData;

                int photonViewID = (int)customData[0];

                //unpack
                bool blocking = (bool)customData[1];
                bool blockRaising = (bool)customData[2];
                bool blockLowering = (bool)customData[3];
                bool blockLowered = (bool)customData[4];
                Quaternion shieldStartingRotation = (Quaternion)customData[5];
                Vector3 shieldScaleOnButtonPress = (Vector3)customData[6];
                Quaternion headStartingRotationOnBlock = (Quaternion)customData[7];
                Quaternion headTargetRotationOnBlock = (Quaternion)customData[8];
                Vector3 headStartPos = (Vector3)customData[9];
                double blockStartTime = (double)customData[10];
                double blockStartTimeOnUpdate = (double)customData[11];
                
                //apply data from network master to this client
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                //enter the block button into the inputs class on client player

                PlayerAttacks pA = viewOwner.GetComponent<PlayerAttacks>();
                //apply
                pA.blocking = blocking;
                pA.blockRaising = blockRaising;
                pA.blockLowering = blockLowering;
                pA.blockLowered = blockLowered;
                pA.shieldStartingRotation = shieldStartingRotation;
                pA.shieldScaleOnButtonPress = shieldScaleOnButtonPress;
                pA.headStartingRotationOnBlock = headStartingRotationOnBlock;
                pA.headTargetRotationOnBlock = headTargetRotationOnBlock;
                pA.headStartingRotationOnBlock = headStartingRotationOnBlock;
                pA.headStartPos = headStartPos;
                pA.blockStartTime = blockStartTime;
                pA.blockRotationTime = blockStartTimeOnUpdate;

                /*
                //do we need, ? - just make sure other scripts are referencing pA.blocking and not any inputs
                Inputs inputs = viewOwner.GetComponent<Inputs>();
                if (blocking)
                    inputs.blocking0 = true;
                //blocking1 still to be done (if used in the end)
                else
                    inputs.blocking0 = false;
                */


            }
            //edge bump
            //if player gets bumped but can't go anywhere
            if(eventCode == 24)
            {
                Debug.Log("[CLIENT] - Getting edge bump");
                object[] customData = (object[])photonEvent.CustomData;

                int photonViewID = (int)customData[0];
                double bumpfinishTime = (double)customData[1];
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                PlayerMovement pM = viewOwner.GetComponent<PlayerMovement>();
                //set all flags to force a bump cool down
                pM.bumped = false;
                pM.bumpInProgress = false;
                pM.walking = false;
                pM.waitingForBumpReset = true;
                //let it know when to start counting for bump cooldown
                pM.bumpFinishTime = bumpfinishTime;
            }

            //30+ predictive
            if (eventCode == 30)
            {
                
                //receiving constant stream of unreliable client input
                object[] customData = (object[])photonEvent.CustomData;
                int photonViewID = (int)customData[0];
                Vector3 lookDir = (Vector3)customData[1];
                Vector3 rightStickLookDir = (Vector3)customData[2];
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                PlayerMovement pM = viewOwner.GetComponent<PlayerMovement>();
                if (pM != null)// && inputs !=null)// || !PhotonNetwork.IsMasterClient)//happens on connect // master doesnt need predcition?
                {
                    pM.lookDir = lookDir;//left
                }

               // Debug.Log("sending unreliable");
               if(viewOwner.GetComponent<PlayerAttacks>() != null)
                    viewOwner.GetComponent<PlayerAttacks>().lookDirRightStick = rightStickLookDir;

            }

            //resolutions 40+
            //swipe on swipe hit
            if (eventCode == 40)
            {
                
                object[] customData = (object[])photonEvent.CustomData;
                int photonViewID = (int)customData[0];
                
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                Swipe swipe = viewOwner.GetComponent<Swipe>();
             
                
                SwipeObject sO = swipe.currentSwipeObject.GetComponent<SwipeObject>();

                sO.impactDirection = (Vector3)customData[1];//need impact point? check when reworking
                //sO.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;//on destroy swipe function
                sO.DestroySwipe();
            }
            //swipe on self player
            if (eventCode == 41)
            {
                
                object[] customData = (object[])photonEvent.CustomData;
                int photonViewID = (int)customData[0];
                
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;

                if (viewOwner.GetComponent<Swipe>().currentSwipeObject == null)
                {
                    //double event send can cause this?
                }
                else
                {
                    SwipeObject thisSwipeObjectScript = viewOwner.GetComponent<Swipe>().currentSwipeObject.GetComponent<SwipeObject>();
                    thisSwipeObjectScript.impactDirection = (Vector3)customData[1];
                    thisSwipeObjectScript.impactPoint = (Vector3)customData[2];
                    thisSwipeObjectScript.hitSelf = true;
                    thisSwipeObjectScript.DestroySwipe();
                    thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                }

            }
            //swipe on other player
            if (eventCode == 42)
            {

                object[] customData = (object[])photonEvent.CustomData;

                //send hit info to clients
                //send resolution to network
                // Custom Event 41: send player hit to clients
                //i need to send, 
                //who got hit
                int photonViewIDVictim = (int)customData[0];
                //how powerful the hit was
                float healthReduction = (float)customData[1];
                //put destroy swipe if not null in event code
                //send bump update too
                Vector3 bumpShootFrom = (Vector3)customData[2];
                //we also need to update the player who's hit was successful
                int photonViewIDAttacker = (int)customData[3];
                double timeSwingFinished = (double)customData[4];
                double finishTimeStriking = (double)customData[5];
                //update waiting on reset overhead on event code
                //update hit bool on event code
                //update active time on event code
                //deactivate swipe (the swipe that hit)
                //tell swipe it hit opponent on event code
                //impact dir
                Vector3 impactDir = (Vector3)customData[6];
                //impact point
                Vector3 impactPoint = (Vector3)customData[7];
                //destroy swipe on event code


                GameObject viewOwnerVictim = PhotonView.Find(photonViewIDVictim).gameObject;
                //reduce health on victim
                PlayerInfo victimInfo = viewOwnerVictim.GetComponent<PlayerInfo>();
                victimInfo.health -= healthReduction;
               
                GameObject viewOwnerAttacker = PhotonView.Find(photonViewIDAttacker).gameObject;

                if (viewOwnerAttacker.GetComponent<Swipe>().currentSwipeObject != null)
                {
                    SwipeObject thisSwipeObjectScript = viewOwnerAttacker.GetComponent<Swipe>().currentSwipeObject.GetComponent<SwipeObject>();

                    thisSwipeObjectScript.hitOpponent = true;
                    thisSwipeObjectScript.timeSwingFinished = PhotonNetwork.Time;

                    //for bumping player

                    PlayerMovement pMother = viewOwnerVictim.GetComponent<PlayerMovement>();

                    //thisSwipeObjectScript.activeTime = thisSwipeObjectScript.playerClassValues.overheadHitCooldown;

                    if (victimInfo.health > 0)
                    {
                        //if player was attempting any attacks, cancel
                        bool cancelSwipeIfNonLethal = false;
                        if (cancelSwipeIfNonLethal)
                        {
                            Swipe otherSwipeScript = viewOwnerVictim.GetComponent<Swipe>();
                            otherSwipeScript.ResetFlags();
                            //check for swipe from victim, destroy this if any
                            if (otherSwipeScript.currentSwipeObject != null)//will be null when not swiping
                            {
                                //let player know it was cancelled with visual aid
                                otherSwipeScript.currentSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                                otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().impactDirection = -otherSwipeScript.transform.position;//not //wokring?
                                otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                            }
                        }

                        //let player object know when we finished this swing too
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().finishTimeSriking = PhotonNetwork.Time;//should be network time?
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().waitingOnResetOverhead = true;
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().buttonSwipeAvailable = false;
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().hit = true;


                        thisSwipeObjectScript.DeactivateSwipe();


                        //destroy this swipe, the victim is still alive
                        thisSwipeObjectScript.impactDirection = impactDir;

                        thisSwipeObjectScript.impactPoint = impactPoint;

                        viewOwnerAttacker.GetComponent<Swipe>().currentSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                        pMother.bumped = true;
                        //hit point is on the near side, so get dir to hit transform and extend it through. point will now be on the rear side of hit transform
                        // float hitBumpAmount = 1f;//*global var
                        pMother.bumpShootfrom = bumpShootFrom;

                        /*
                        //tell hit player to vibrate
                        PlayerVibration pV = parentOfHitHeadMesh.GetComponent<PlayerVibration>();
                        pV.shakeTimerHit += pV.nonLethatHitLength;
                        //tell player who successfully hit too - just use non lethal for hit confirm
                        pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                        pV.shakeTimerHit += pV.nonLethatHitLength;
                        */

                    }
                    else if (victimInfo.health <= 0f)
                    {
                        //if player was attempting any attacks, cancel
                        bool cancelSwipeIfLethal = true;
                        if (cancelSwipeIfLethal)
                        {
                            Swipe otherSwipeScript = viewOwnerVictim.GetComponent<Swipe>();
                            otherSwipeScript.ResetFlags();
                            //check for swipe from victim, destroy this if any
                            if (otherSwipeScript.currentSwipeObject != null)//will be null when not swiping
                            {
                                //let player know it was cancelled with visual aid
                                otherSwipeScript.currentSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;
                                otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().impactDirection = -otherSwipeScript.transform.position;//not //wokring?
                                otherSwipeScript.currentSwipeObject.GetComponent<SwipeObject>().DestroySwipe();

                            }
                        }

                        //flag for this player
                        Debug.Log("overhead hit opponent");


                        //reset other player (victim)
                        viewOwnerVictim.GetComponent<Swipe>().ResetFlags();

                        //impact dir

                        thisSwipeObjectScript.impactDirection = impactDir;

                        thisSwipeObjectScript.impactPoint = impactPoint;
                        //we need to break up the head mesh - they died
                        GameObject victimHeadMesh = viewOwnerVictim.GetComponent<PlayerMovement>().head.transform.GetChild(0).gameObject;
                        Swipe.BreakUpPlayer(victimHeadMesh, thisSwipeObjectScript);
                        Swipe.DeSpawnPlayer(viewOwnerVictim);

                        //reset this player too (attacker)
                        thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();

                        /*
                        //tell hit player to vibrate
                        PlayerVibration pV = parentOfHitHeadMesh.GetComponent<PlayerVibration>();
                        pV.shakeTimerHit += pV.lethatHitLength;

                        //tell player who successfully hit too - just use non lethal for hit confirm
                        pV = thisSwipeObjectScript.parentPlayer.GetComponent<PlayerVibration>();
                        pV.shakeTimerHit += pV.nonLethatHitLength;
                        */



                    }
                }
            }

            //swipe on shield
            if(eventCode == 43)
            {
                


                //tell client's palyer to destroy swipe and rest player - they hit a shield
                object[] customData = (object[])photonEvent.CustomData;
                int photonViewID = (int)customData[0];
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                Swipe swipe = viewOwner.GetComponent<Swipe>();

                GameObject thisSwipeObject = swipe.currentSwipeObject;

                thisSwipeObject.GetComponent<MeshRenderer>().sharedMaterial = Resources.Load("Materials/Grey0") as Material;

                SwipeObject thisSwipeObjectScript = thisSwipeObject.GetComponent<SwipeObject>();
                thisSwipeObjectScript.hitShield = true;
                thisSwipeObjectScript.DestroySwipe();

                if (thisSwipeObjectScript.activeSwipe)
                {
                    swipe.finishTimeSriking = PhotonNetwork.Time;
                    swipe.waitingOnResetOverhead = true;
                    swipe.buttonSwipeAvailable = false;
                    swipe.blocked = true;

                    //reset so clients avatar is looking where they are meant to be looking or not etc
                    thisSwipeObjectScript.parentPlayer.GetComponent<Swipe>().ResetFlags();
                }

            }

            //50+ map changes
            if(eventCode == 50)
            {
                //Debug.Log("Receiving event code 50 - cell heights");
                //cell height change
                //player is changing height, find gameobject matching the photon view sent and start altering the height
                object[] customData = (object[])photonEvent.CustomData;
                int photonViewID = (int)customData[0];
                double startTime = (double)customData[1];
                bool raising = (bool)customData[2];
                bool lowering = (bool)customData[3];
                float startingScaleY = (float)customData[4];
                GameObject viewOwner = PhotonView.Find(photonViewID).gameObject;
                //set flags on cell height scripts on owner
                
                viewOwner.GetComponent<CellHeights>().raisingCell = raising;                
                viewOwner.GetComponent<CellHeights>().loweringCell = lowering;

                //add start time
                viewOwner.GetComponent<CellHeights>().eventTime = startTime;
                //addd start scale 
                viewOwner.GetComponent<CellHeights>().startingScaleY = startingScaleY;

                //this will start the networked player adjusting cell height

            }

            //80+tests
            if(eventCode == 80)
            {
                Debug.Log("Receiving new test values");
                
                PlayerClassValues playerClassValues = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerClassValues>();

                object[] customData = (object[])photonEvent.CustomData;
                float[] playerClassFloats = (float[])customData[0];
                playerClassValues.respawnTime = playerClassFloats[0];
                playerClassValues.maxClimbHeight = playerClassFloats[1];
                playerClassValues.playerCooldownAfterOverheadHit = playerClassFloats[2];
                playerClassValues.playerCooldownAfterOverheadBlock = playerClassFloats[3];
                playerClassValues.playerCooldownAfterOverheadWhiff = playerClassFloats[4];
                playerClassValues.overheadSpeed = playerClassFloats[5];
                playerClassValues.armLength= playerClassFloats[6];
                playerClassValues.swordLength = playerClassFloats[7];
                playerClassValues.swordWidth = playerClassFloats[8];
                playerClassValues.blockRaise = playerClassFloats[9];
                playerClassValues.blockLower = playerClassFloats[10];
                playerClassValues.blockMinimum = playerClassFloats[11];
                playerClassValues.playerCooldownAfterBump = playerClassFloats[12];

                //overlay drawer
                float[] overlayFloats = (float[])customData[1];
            
                OverlayDrawer overlayDrawer = GameObject.FindGameObjectWithTag("Code").GetComponent<OverlayDrawer>();

                overlayDrawer.heightSpeed = overlayFloats[0];
                overlayDrawer.heightSpeedSiege = overlayFloats[1];
                overlayDrawer.heightMultiplier = overlayFloats[2];
                overlayDrawer.minHeight = overlayFloats[3];

                bool[] overlayBools = (bool[])customData[2];
                overlayDrawer.doHeights = overlayBools[0];
               // overlayDrawer.automaticFrontlineHeightRaise = overlayBools[1];
                overlayDrawer.reduceFrontline = overlayBools[1];
               // overlayDrawer.doCapture = overlayBools[3];

            }

        }


        #endregion

    }
}