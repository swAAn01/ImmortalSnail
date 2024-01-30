using BepInEx;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace ImmortalSnail
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static AssetBundle bundle;
        private void Awake()
        {
            NetcodePatcher();

            // loading snail from bundle
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            bundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "immortalsnail"));

            if (bundle == null)
            {
                Logger.LogError("Failed to load asset bundle.");
                return;
            }

            EnemyType snail = bundle.LoadAsset<EnemyType>("ImmortalSnail.EnemyType");

            if (snail == null || snail.enemyPrefab == null)
            {
                Logger.LogError("Snail Failed to load properly.");
                return;
            }

            /*
             * TODO
             * find out what rarity to use
             * add TerminalNode
             * add TerminalKeyword
             */

            Logger.LogInfo("Configuring Snail.");
            snail.enemyPrefab.AddComponent<SnailAI>();
            snail.enemyPrefab.GetComponent<SnailAI>().enemyType = snail;
            snail.enemyPrefab.GetComponentInChildren<EnemyAICollisionDetect>().mainScript = snail.enemyPrefab.GetComponent<SnailAI>();
            snail.enemyPrefab.AddComponent<NetworkHandler>();

            Logger.LogInfo("Registering Snail as Enemy");
            Levels.LevelTypes levelFlags = Levels.LevelTypes.All;
            Enemies.SpawnType spawnType = Enemies.SpawnType.Default;

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(snail.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(snail, 100, levelFlags, spawnType, null, null);           

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static void NetcodePatcher()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
    }
}