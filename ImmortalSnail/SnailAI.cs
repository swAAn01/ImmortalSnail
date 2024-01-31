using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace ImmortalSnail
{
    class SnailAI : EnemyAI
    {

        public override void Start()
        {
            // EnemyAI attributes
            AIIntervalTime = 0.2f;
            updatePositionThreshold = 0.4f;
            moveTowardsDestination = true;
            syncMovementSpeed = 0.22f;
            exitVentAnimationTime = 1.45f;
            enemyHP = 1;

            base.Start();

            RefreshTargetServerRpc();
        }

        public override void Update()
        {
            base.Update();
            if (targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
                RefreshTargetServerRpc();
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();

            if (player == null)
            {
                Debug.LogWarning("Snail collided with a player, but player was null.");
                return;
            }

            base.OnCollideWithPlayer(other);

            if (stunNormalizedTimer >= 0f)
                return;

            if (player.playerClientId == targetPlayer.playerClientId)
                KillPlayerServerRpc(player.playerClientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RefreshTargetServerRpc()
        {
            PlayerControllerB[] allPlayers = StartOfRound.Instance.allPlayerScripts;

            PlayerControllerB tempPlayer = null;
            float dist = float.MaxValue;

            // find nearest player
            for (int i = 0; i < allPlayers.Length; i++)
            {
                if (allPlayers[i].isPlayerDead || !allPlayers[i].isPlayerControlled)
                    continue;

                float d2 = Vector3.Distance(this.transform.position, allPlayers[i].transform.position);

                if (d2 < dist)
                {
                    tempPlayer = allPlayers[i];
                    dist = d2;
                }
            }

            if (tempPlayer == null)
            {
                Debug.LogWarning("Snail was unable to find a player to target.");
                return;
            }

            SetMovingTowardsTargetPlayer(tempPlayer);
            gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target : " + tempPlayer.playerUsername;
            RefreshTargetClientRpc(tempPlayer.playerClientId);
        }

        [ClientRpc]
        public void RefreshTargetClientRpc(ulong playerId)
        {
            SetMovingTowardsTargetPlayer(StartOfRound.Instance.allPlayerScripts[playerId]);
            gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target: " + StartOfRound.Instance.allPlayerScripts[playerId].playerUsername;
        }

        [ServerRpc(RequireOwnership = false)]
        public void KillPlayerServerRpc(ulong playerId)
        {
            StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(Vector3.up, spawnBody: true, CauseOfDeath.Unknown);
            KillPlayerClientRpc(playerId);
            RefreshTargetServerRpc();
        }

        [ClientRpc]
        public void KillPlayerClientRpc(ulong playerId)
        {
            StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(Vector3.up, spawnBody: true, CauseOfDeath.Unknown);
        }
    }
}