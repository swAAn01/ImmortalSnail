using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace ImmortalSnail
{
    class SnailAI : EnemyAI
    {
        private bool allPlayersDead;
        public override void Start()
        {
            // EnemyAI attributes
            // Huh. I guess there's just one now.
            enemyHP = 1;

            base.Start();

            allPlayersDead = false;

            RefreshTargetServerRpc();
        }

        public override void Update()
        {
            base.Update();

            if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
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
                KillPlayerServerRpc((int) player.playerClientId);
        }

        /*
         * This may seem odd, but it actually makes sense here to just skip this entirely. Why?
         * This method only does 2 things:
         *  1. sets destination
         *  2. synchronizes position
         *  
         * But this is essentially a waste of bandwidth and cycles, because we are already
         * handling this with 1. RefreshTarget and 2. the NetworkTransform attached to the prefab.
         */ 
        public override void DoAIInterval()
        {
            if (movingTowardsTargetPlayer)
                destination = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);

            if (moveTowardsDestination)
                agent.SetDestination(destination);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RefreshTargetServerRpc()
        {
            if (StartOfRound.Instance.allPlayersDead)
            {
                if (allPlayersDead)
                    return;

                Debug.Log("All players dead.");
                targetPlayer = null;
                movingTowardsTargetPlayer = false;
                RefreshTargetClientRpc(-1);
                allPlayersDead = true;
                return;
            }

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
                targetPlayer = null;
                movingTowardsTargetPlayer = false;
                RefreshTargetClientRpc(-1);
                Debug.LogWarning("Snail was unable to find a player to target.");
                return;
            }

            SetMovingTowardsTargetPlayer(tempPlayer);
            gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target : " + tempPlayer.playerUsername;
            RefreshTargetClientRpc((int) tempPlayer.playerClientId);
        }

        [ClientRpc]
        public void RefreshTargetClientRpc(int playerId)
        {
            if (playerId == -1)
            {
                targetPlayer = null;
                movingTowardsTargetPlayer = false;
                return;
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];
            SetMovingTowardsTargetPlayer(player);
            gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target: " + player.playerUsername;
        }

        [ServerRpc(RequireOwnership = false)]
        public void KillPlayerServerRpc(int playerId)
        {
            StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(Vector3.up, spawnBody: true, CauseOfDeath.Unknown);
            KillPlayerClientRpc((ulong) playerId);
            RefreshTargetServerRpc();
        }

        [ClientRpc]
        public void KillPlayerClientRpc(ulong playerId)
        {
            StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(Vector3.up, spawnBody: true, CauseOfDeath.Unknown);
        }
    }
}