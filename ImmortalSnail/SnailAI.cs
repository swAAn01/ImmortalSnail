using GameNetcodeStuff;
using UnityEngine;
using Unity.Netcode;

namespace ImmortalSnail
{
    class SnailAI : EnemyAI
    {
        /*
         * Here's what we want the snail to do
         * 1. on start, target a player
         * 2. move toward the player
         * 3. if we touch the player, kill them and select a new target
         */

        public override void Start()
        {
            // configure script
            AIIntervalTime = 0.2f;
            updatePositionThreshold = 0.4f;
            moveTowardsDestination = true;
            syncMovementSpeed = 0.22f;
            exitVentAnimationTime = 1.45f;
            enemyHP = 1;

            base.Start();
            refreshTarget();
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (!other.gameObject.GetComponent<PlayerControllerB>())
            {
                return;
            }

            base.OnCollideWithPlayer(other);

            if (stunNormalizedTimer >= 0f)
                return;

            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();

            if (player != null)
            {
                if ((int) player.playerClientId == NetworkHandler.TargetPlayerClientId)
                {
                    player.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown);
                    refreshTarget();
                }
            }
        }

        public void refreshTarget()
        {
            if (!(NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer))
                return;

            PlayerControllerB[] allPlayers = StartOfRound.Instance.allPlayerScripts;

            PlayerControllerB tempPlayer = null;
            float dist = float.MaxValue;

            // find nearest player
            for (int i = 0; i < allPlayers.Length; i++)
            {
                if (allPlayers[i].isPlayerDead)
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
                Debug.Log("Snail was unable to find a player to target.");
                return;
            }

            SetMovingTowardsTargetPlayer(tempPlayer);
            NetworkHandler.Instance.SetTargetPlayerClientRpc((int) tempPlayer.playerClientId);
        }
    }
}