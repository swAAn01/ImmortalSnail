using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace ImmortalSnail
{
    class SnailAI : EnemyAI
    {
        private float timeAtLastUsingEntrance;

        public override void Start()
        {
            // EnemyAI attributes
            enemyHP = 1;

            base.Start();

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

            if (targetPlayer && player.playerClientId == targetPlayer.playerClientId) KillPlayerServerRpc((int)player.playerClientId);
        }

        /*
         * This may seem odd, but it actually makes sense here to just skip EnemyAI.DoAIInterval entirely. Why?
         * This method only does 2 things:
         *  1. sets destination
         *  2. synchronizes position
         *  
         * But this is essentially a waste of bandwidth and cycles, because we can handle this with:
         *  1. setting destination here and in Update()
         *  2. the NetworkTransform attached to the snail prefab
         */
        public override void DoAIInterval()
        {
            if (targetPlayer == null || targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled
                || (targetPlayer.isInHangarShipRoom && !Plugin.configEnterShip.Value) // player is in ship and snail can't go to ship
                || (!targetPlayer.isInsideFactory && !Plugin.configGoOutside.Value)) // player is outside and snail can't go outside
                RefreshTargetServerRpc();

            if (movingTowardsTargetPlayer)
            {
                if (isInTargetPlayerArea()) destination = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
                else SetDestinationToOtherArea();

                agent.SetDestination(destination);
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
            }
        }

        private void SetDestinationToOtherArea()
        {
            Transform nearbyExitTransform = getNearbyExitTransform();

            if (nearbyExitTransform && Time.realtimeSinceStartup - timeAtLastUsingEntrance > 3f) // we are at an entrance => use it
            {
                if (base.IsOwner) // no clue why this is necessary, but it is!
                {
                    agent.enabled = false;
                    base.transform.position = nearbyExitTransform.position;
                    agent.enabled = true;
                }
                else
                {
                    base.transform.position = nearbyExitTransform.position;
                }

                timeAtLastUsingEntrance = Time.realtimeSinceStartup;
                isOutside = !isOutside;
                destination = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
            }
            else // we are not at an entrance => go to an entrance
            {
                destination = RoundManager.Instance.GetNavMeshPosition(getNearestExitTransform().position, RoundManager.Instance.navHit, 2.7f);
            }
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
                if (allPlayers[i] == null || allPlayers[i].isPlayerDead || !allPlayers[i].isPlayerControlled
                || (allPlayers[i].isInHangarShipRoom && !Plugin.configEnterShip.Value) // player is in ship room and snail can't go to ship
                || (!allPlayers[i].isInsideFactory && !Plugin.configGoOutside.Value)) // player is outside and snail can't go outside
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
                return;
            }

            SetMovingTowardsTargetPlayer(tempPlayer);
            if (Plugin.configShowTarget.Value) gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target : " + tempPlayer.playerUsername;

            RefreshTargetClientRpc((int)tempPlayer.playerClientId);
        }

        [ClientRpc]
        public void RefreshTargetClientRpc(int playerId) // possibly unecessary
        {
            if (playerId == -1)
            {
                targetPlayer = null;
                movingTowardsTargetPlayer = false;
                return;
            }

            PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerId];

            SetMovingTowardsTargetPlayer(player);
            if (Plugin.configShowTarget.Value) gameObject.GetComponentInChildren<ScanNodeProperties>().subText = "Current Target: " + player.playerUsername;
        }

        [ServerRpc(RequireOwnership = false)]
        public void KillPlayerServerRpc(int playerId)
        {
            if (Plugin.configCanExplode.Value) Explode(playerId, Plugin.configExplosionKillOthers.Value);
            else StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown);

            KillPlayerClientRpc(playerId, Plugin.configCanExplode.Value, Plugin.configExplosionKillOthers.Value);
            RefreshTargetServerRpc();
        }

        [ClientRpc]
        public void KillPlayerClientRpc(int playerId, bool explode, bool killOthers)
        {
            if (explode) Explode(playerId, killOthers);
            else StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown);
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

        // area meaning inside or outside
        private bool isInTargetPlayerArea()
        {
            return (targetPlayer.isInsideFactory && !isOutside) || (!targetPlayer.isInsideFactory && isOutside);
        }

        private void Explode(int playerId, bool killOthers)
        {
            if (killOthers)
            {
                Landmine.SpawnExplosion(base.transform.position, true, 5.7f, 6.4f);
            }
            else
            {
                Landmine.SpawnExplosion(base.transform.position, true, 0f, 0f);
                StartOfRound.Instance.allPlayerScripts[playerId].KillPlayer(GetBodyVelocity(playerId), spawnBody: true, CauseOfDeath.Blast);
            }
        }

        private Vector3 GetBodyVelocity(int playerId)
        {
            Vector3 result = StartOfRound.Instance.allPlayerScripts[playerId].gameplayCamera.transform.position;
            result -= base.transform.position;
            result *= 80f;
            result /= Vector3.Distance(StartOfRound.Instance.allPlayerScripts[playerId].gameplayCamera.transform.position, base.transform.position);
            return result;
        }
    }
}