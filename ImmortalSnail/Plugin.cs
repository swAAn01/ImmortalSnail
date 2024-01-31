using BepInEx;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
using UnityEngine.AI;

namespace ImmortalSnail
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public static AssetBundle bundle;
        public static ConfigEntry<float> configSize;
        public static ConfigEntry<float> configSpeed;
        public static ConfigEntry<int> configMaxSnails;
        public static ConfigEntry<int> configRarity;

        private void Awake()
        {
            // setup for Unity Netcode Patcher
            NetcodePatcher();

            // configuration setup
            configSize = Config.Bind("General", "Scale", 100.0f, "The scale of the snail. Defaults to 100.");
            configSpeed = Config.Bind("General", "Speed", 0.5f, "The speed of the snail. Defaults to 0.5.");
            configMaxSnails = Config.Bind("General", "Max Snails", 1, "The maximum number of snails that can spawn in a round. Defaults to 1.");
            configRarity = Config.Bind("General", "Rarity", 100, "Honestly not sure exactly how this works, but a higher \"Rarity\" will make the snail more likely to spawn.");

            // check if using LethalConfig
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig"))
                ConfigManager.setupLethalConfig();

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

            Logger.LogInfo("Configuring Snail.");
            snail.enemyPrefab.AddComponent<SnailAI>();
            snail.enemyPrefab.GetComponent<SnailAI>().enemyType = snail;
            snail.enemyPrefab.GetComponentInChildren<EnemyAICollisionDetect>().mainScript = snail.enemyPrefab.GetComponent<SnailAI>();

            Logger.LogInfo("Applying User-Defined Configuration Settings");
            float scale = 25f * configSize.Value / 100f;
            snail.enemyPrefab.transform.localScale = new Vector3(scale, scale, scale);
            snail.enemyPrefab.GetComponent<NavMeshAgent>().speed = configSpeed.Value;
            snail.MaxCount = configMaxSnails.Value;

            Logger.LogInfo("Registering Snail as Enemy");
            Levels.LevelTypes levelFlags = Levels.LevelTypes.All;
            Enemies.SpawnType spawnType = Enemies.SpawnType.Default;
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(snail.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(snail, configRarity.Value, levelFlags, spawnType, null, null);           

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