using GameNetcodeStuff;
using System;
using Unity.Netcode;
using UnityEngine;

namespace ImmortalSnail
{
    class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; }
        public static int TargetPlayerClientId { get; private set; }

        public override void OnNetworkSpawn()
        {
            TargetPlayerClientId = -1;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void SetTargetPlayerClientRpc(int playerId)
        {
            TargetPlayerClientId = playerId;
        }
    }
}
