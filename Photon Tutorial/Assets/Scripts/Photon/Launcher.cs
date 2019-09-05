using UnityEngine;

using System.Collections;

using Photon.Pun;
using Photon.Realtime;

namespace DellyWellyWelly
{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        #region Private Serializable Fields


        #endregion


        #region Private Fields


        /// <summary>
        /// This client's version number. Users are separated from each other by gameVersion (which allows you to make breaking changes).
        /// </summary>
        string gameVersion = "1";

        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField]
        private byte maxPlayersPerRoom = 4;


        #endregion


        #region MonoBehaviour CallBacks


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            // #Critical
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;

            //make fast rate
           // PhotonNetwork.SendRate = 10;
            //PhotonNetwork.SerializationRate = 10;
        }


        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            Connect();
        }


        #endregion


        #region Public Methods


        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                // PhotonNetwork.SendRate = 10;
                // PhotonNetwork.SerializationRate = 10;

                

                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                // #Critical, we must first and foremost connect to Photon Online Server.
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();

                
            }
        }


        #endregion

        #region MonoBehaviourPunCallbacks Callbacks

        int tries = 0;
        bool pauseForSync = false;
        public void SyncClientAndNetwork()
        {
            /*
            Debug.Log("PreSync -Photon Network time is =" + PhotonNetwork.Time);
            Debug.Log("PreSync - Time = " + Time.time);
            Debug.Log("PreSync - TimeScale = " + Time.timeScale);
            float diff = ((float)PhotonNetwork.Time % 1) -(Time.time % 1);
            Debug.Log("PreSync - Time difference = " + diff);
            */

            //we need to pause unity on a fixed update step
            //to do this we need to be ona fixed update step, so set a flag for fixed update to grab
            pauseForSync = true;
           

            //now we need to restart unity now we are on same pulse as network time
        }

        private void FixedUpdate()
        {
            if(pauseForSync)
            {
                //stop unity time
                Time.timeScale = 0;

                //start recursive function which waits for network time to be a whole number
                WaitForNetworkTime();

                //flag to false, we started the search on a fixed update step
                pauseForSync = false;

                /*
                Debug.Log("PauseSync -Photon Network time is =" + PhotonNetwork.Time);
                Debug.Log("PauseSync- Time = " + Time.time);
                Debug.Log("PauseSync - TimeScale = " + Time.timeScale);
                float diff = ((float)PhotonNetwork.Time % 1) - (Time.time % 1);
                Debug.Log("PauseSync - Time difference = " + diff);
                */
            }
        }

        void WaitForNetworkTime()
        {
            //makes fixed update on unity tick at the same time as network time
            //add time difference to unity timescale

            double difference = PhotonNetwork.Time % 1;
           // Debug.Log("fixed update step = " + Time.fixedDeltaTime);

            float startTime = Time.time;
            tries++;
            //wait for network time to be whole second
            if (PhotonNetwork.Time % 1 < 0.01f)//still to test what this number should be - the smaller, the mroe accurate but sync time longer?
            {

               // Debug.Log("FOUND");
                //start unity time again
                Time.timeScale = 1f;



                //we can now load our map
                GetComponent<GameManagerPhoton>().LoadArena();
                //spawn with out map atm
              //  if(PhotonNetwork.IsMasterClient == false)
              //    GameManagerPhoton.SpawnPlayer();
                

                /*
                Debug.Log("PostSync -Photon Network time is =" + PhotonNetwork.Time);
                Debug.Log("PostSync- Time = " + Time.time);
                Debug.Log("PostSync- TimeScale = " + Time.timeScale);
                float diff = ((float)PhotonNetwork.Time % 1) -(Time.time % 1);
                Debug.Log("PostSync - Time difference = " + diff);


                Debug.Break();
                */

            }
            else
            {
                //wait and try again
                //using custom class which isnt affected by Unity's time - Probably just a coroutine class but it was easy to use so there we go
                Invoker.InvokeDelayed(WaitForNetworkTime, 0.001f);
            }

            if (tries > 5000)
            {
                Debug.Log("prob");
                Debug.Break();

            }
        }
       

        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");

            PhotonNetwork.JoinRandomRoom();

           
        }


        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

            // #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = maxPlayersPerRoom });
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");

            //start sync
           
            SyncClientAndNetwork();

        }


        #endregion

    }

}