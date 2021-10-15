using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace DellyWellyWelly
{
    public class PlayerStarter : MonoBehaviour
    {
        //Attached to player prefab. Syncs game state before allowing control to player


        private void Awake()
        {
            //add this object to a list - will need to remove on disconnect
            GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList.Add(gameObject);
            //add info script
            PlayerInfo pI = gameObject.AddComponent<PlayerInfo>();
            pI.enabled = false;

            //work out team
            int teamNumber = -1;
            if (PhotonNetwork.IsMasterClient)
            {
                //work out team number
                teamNumber = TeamNumber();
                //set
                pI.teamNumber = teamNumber;
            }
            else
            {
                //we will request from master client when enabled
            }
            


            //we will enable all network players once the map has generated
            if (!GetComponent<PhotonView>().IsMine)
                enabled = false;
        }

        // Start is called before the first frame update
        public void Start()
        {
            //Debug.Log("Start called on player starter");
            //if client is spawning, get own info on healths, score etc //updates all players' avatars for this client
            if (!PhotonNetwork.IsMasterClient)
            {
                //and is a local player - we only need to do this once
                if(GetComponent<PhotonView>().IsMine)
                    GetGameState();
               // gameObject.GetComponent<PrefabCreator>().enabled = true;
            }
            else
            {
                //if master is spawning a player it means they are the first person to spawn in a room, they don't need to worry
                //about any other players other than their own avatar
                //this also gets called by the master when another player joins the room
                gameObject.GetComponent<PrefabCreator>().enabled = true;
            }
        }

        int TeamNumber()
        {
            //team number
            int teamNumber = 0;
            Debug.Log("player list length =" + PhotonNetwork.PlayerList.Length);

            //find player's position in player list
            int position = 0;
            List<GameObject> players = GameObject.FindGameObjectWithTag("Code").GetComponent<PlayerGlobalInfo>().playerGlobalList;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] == gameObject)
                {
                    position = i;
                    break;
                }
            }
            Debug.Log("position" + position);
            if (position % 2 == 0)
            {
                Debug.Log("setting team 0");
                teamNumber = 0;
            }
            else
            {
                Debug.Log("setting team 1");
                teamNumber = 1;
            }

            return teamNumber;

        }

        public void GetGameState()//called from game manager photon
        {
            Debug.Log("Get Player Positions - Client");

            //send request for player position
            byte evCode = 10; // Custom Event 10: Request player positions

            int photonViewID = gameObject.GetComponent<PhotonView>().ViewID;///need view id of player, not of launcher (there is none) //move all code to player prefab?

            object[] content = new object[] { photonViewID };
            //ask master for positions
            RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient };

            //keep resending until server receives
            SendOptions sendOptions = new SendOptions { Reliability = true };

            PhotonNetwork.RaiseEvent(evCode, content, raiseEventOptions, sendOptions);

        }
    }
}
