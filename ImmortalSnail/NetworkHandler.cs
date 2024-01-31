using Unity.Netcode;

namespace ImmortalSnail
{
    class NetworkHandler : NetworkBehaviour
    {
        public static NetworkHandler Instance { get; private set; }
        public SnailAI localSnailAI { get; set; }

        public override void OnNetworkSpawn()
        {
            localSnailAI = null;

            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
                Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
            Instance = this;

            base.OnNetworkSpawn();
        }

        [ClientRpc]
        public void RefreshTargetClientRpc(ulong playerId)
        {
            localSnailAI.SetMovingTowardsTargetPlayer(StartOfRound.Instance.allPlayerScripts[playerId]);
            localSnailAI.gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target: " + localSnailAI.targetPlayer.playerUsername;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RefreshTargetServerRpc()
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                localSnailAI.refreshTarget();
                RefreshTargetClientRpc(localSnailAI.targetPlayer.playerClientId);
            }
        }
    }
}
