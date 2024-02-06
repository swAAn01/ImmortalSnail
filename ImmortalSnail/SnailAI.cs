using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace ImmortalSnail
{
    class SnailAI : EnemyAI
    {
        private bool allPlayersDead;
        private float timeAtLastUsingEntrance;
        public override void Start()
        {
            // EnemyAI attributes
            enemyHP = 1;
            isOutside = false;

            base.Start();

            allPlayersDead = false;

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
         * This may seem odd, but it actually makes sense here to just skip EnemyAI.DoAIInterval entirely. Why?
         * This method only does 2 things:
         *  1. sets destination
         *  2. synchronizes position
         *  
         * But this is essentially a waste of bandwidth and cycles, because we can handle this with:
         *  1. setting destination here
         *  2. the NetworkTransform attached to the snail prefab
         */
        public override void DoAIInterval()
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
                RefreshTargetServerRpc();

            if (movingTowardsTargetPlayer)
            {
                if ((targetPlayer.isInsideFactory && !isOutside) || (!targetPlayer.isInsideFactory && isOutside))
                    destination = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
                else if (getNearestExitTransform() != null)
                    destination = RoundManager.Instance.GetNavMeshPosition(getNearestExitTransform().position, RoundManager.Instance.navHit, 2.7f);
                else
                    Debug.LogWarning("Snail has no destination! Target Player is in a different area, and it couldn't find an exit.");
            }

            if (moveTowardsDestination)
                agent.SetDestination(destination);

            Transform nearbyExitTransform = getNearbyExitTransform();
            if (nearbyExitTransform != null && Time.realtimeSinceStartup - timeAtLastUsingEntrance > 3f && Plugin.configGoOutside.Value)
            {
                if (base.IsOwner) // no clue why this is necessary, but it is!
                {
                    agent.enabled = false;
                    base.transform.position = nearbyExitTransform.position;
                    agent.enabled = true;
                }
                else
                    base.transform.position = nearbyExitTransform.position;
                timeAtLastUsingEntrance = Time.realtimeSinceStartup;
                isOutside = !isOutside;
            }
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

                float d2 = Vector3.Distance(base.transform.position, allPlayers[i].transform.position);

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

        private Transform getNearbyExitTransform()
        {
            EntranceTeleport[] entrances = Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            foreach (EntranceTeleport entrance in entrances)
            {
                if (Vector3.Distance(base.transform.position, entrance.entrancePoint.position) < 1f) // found nearby entrance, get corresponding exit
                {
                    foreach (EntranceTeleport entrance2 in entrances)
                    {
                        if (entrance2.isEntranceToBuilding != entrance.isEntranceToBuilding && entrance2.entranceId == entrance.entranceId)
                            return entrance2.entrancePoint;
                    }
                }
            }
            return null;
        }

        private Transform getNearestExitTransform()
        {
            EntranceTeleport[] entrances = Object.FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            Transform t = null;
            float dist = float.MaxValue;
            foreach (EntranceTeleport entrance in entrances)
            {
                float d2 = Vector3.Distance(base.transform.position, entrance.entrancePoint.position);
                if (d2 < dist)
                {
                    t = entrance.entrancePoint;
                    dist = d2;
                }
            }
            return t;
        }
    }
}