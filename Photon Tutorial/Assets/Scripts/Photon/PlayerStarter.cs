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

        // Start is called before the first frame update
        void Start()
        {
            if (GetComponent<PhotonView>().IsMine)
            {
                SetPositionsOfOthers();
            }

        }

        void SetPositionsOfOthers()
        {
            GetPlayerPositions();
        }

        public void GetPlayerPositions()//called from game manager photon
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

            //set to these positions in event code 10
        }
    }
}
