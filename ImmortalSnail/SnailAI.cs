using GameNetcodeStuff;
using UnityEngine;

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

            // NetworkHandler setup
            NetworkHandler.Instance.localSnailAI = this;
            NetworkHandler.Instance.RefreshTargetServerRpc();
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (!other.gameObject.GetComponent<PlayerControllerB>())
            {
                Debug.LogWarning("Snail collided with a player, but player was null.");
                return;
            }

            base.OnCollideWithPlayer(other);

            if (stunNormalizedTimer >= 0f)
                return;

            PlayerControllerB player = other.gameObject.GetComponent<PlayerControllerB>();

            if (player.playerClientId == targetPlayer.playerClientId)
            {
                player.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown);
                NetworkHandler.Instance.RefreshTargetServerRpc();
            }
        }

        public void refreshTarget()
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
        }

        public override void Update()
        {
            base.Update();
            if (targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled)
                NetworkHandler.Instance.RefreshTargetServerRpc();
        }
    }
}