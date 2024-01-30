using BepInEx;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LethalConfig;
using RuntimeNetcodeRPCValidator;
using UnityEngine.AI;

namespace ImmortalSnail
{

    

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("ainavt.lc.lethalconfig")]
    [BepInDependency(RuntimeNetcodeRPCValidator.MyPluginInfo.PLUGIN_GUID, RuntimeNetcodeRPCValidator.MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public static AssetBundle bundle;
        private ConfigEntry<float> configSize;
        private ConfigEntry<float> configSpeed;
        private ConfigEntry<int> configMaxSnails;

        private void Awake()
        {
            // setup for Unity Netcode Patcher
            NetcodePatcher();

            // configuration setup
            configSize = Config.Bind("General", "Scale", 100.0f, "The scale of the snail. Defaults to 100.");
            var sizeSlider = new FloatSliderConfigItem(configSize, new FloatSliderOptions
            {
                Min = 25f,
                Max = 225f
            });

            configSpeed = Config.Bind("General", "Speed", 0.5f, "The speed of the snail. Defaults to 0.5.");
            var speedSlider = new FloatSliderConfigItem(configSpeed, new FloatSliderOptions
            {
                Min = 0.2f,
                Max = 1.0f
            });
            

            configMaxSnails = Config.Bind("General", "Max Snails", 1, "The maximum number of snails that can spawn in a round. Defaults to 1.");
            var maxSnailsSlider = new IntSliderConfigItem(configMaxSnails, new IntSliderOptions
            {
                Min = 0,
                Max = 4
            });

            LethalConfigManager.AddConfigItem(sizeSlider);
            LethalConfigManager.AddConfigItem(speedSlider);
            LethalConfigManager.AddConfigItem(maxSnailsSlider);

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
            snail.enemyPrefab.AddComponent<NetworkHandler>();

            Logger.LogInfo("Applying User-Defined Configuration Settings");
            float scale = 25f * configSize.Value / 100f;
            snail.enemyPrefab.transform.localScale = new Vector3(scale, scale, scale);
            snail.enemyPrefab.GetComponent<NavMeshAgent>().speed = configSpeed.Value;
            snail.MaxCount = configMaxSnails.Value;

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