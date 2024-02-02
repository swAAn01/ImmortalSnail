using BepInEx;
using LethalLib.Modules;
using System.IO;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
using UnityEngine.AI;
using System.Collections;

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
            configSize = Config.Bind("General", "Scale", 100.0f, "The scale of the snail.");
            configSpeed = Config.Bind("General", "Speed", 0.5f, "The speed of the snail.");
            configMaxSnails = Config.Bind("General", "Max Snails", 1, "The maximum number of snails that can spawn in a round.");
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

            /*
             * TODO terminal node is not working right now, so we'll just comment it out for now.
             * the keywords are working, but it just says we need to scan the creature to get the data
             * scanning the creature did not change this
             * I think there's some setting I'm missing. must investigate.
             * 
            TerminalNode snailNode = ScriptableObject.CreateInstance<TerminalNode>();
            snailNode.displayText = "The Immortal Snail\n\nDanger level: 50%\n\n" +
                "When I first saw this thing I didn't know what to think. I mean, it's a snail. And it's HUGE. Not as big as some of the other things I've seen in here," +
                " but still enough to take a guy by surprise. It's resilient, though. Once it locks on to you, it won't give up until you're gone one way or another." +
                " We've tried everything to stop it too, but nothing seems to penetrate its shell.";
            snailNode.clearPreviousText = true;
            snailNode.maxCharactersToType = 500;
            snailNode.creatureName = "The Immortal Snail";
            snailNode.creatureFileID = 1738;

            CompatibleNoun[] snailWords = new CompatibleNoun[11];
            for (int i = 0; i < snailWords.Length; i++)
            {
                snailWords[i] = new CompatibleNoun();
                snailWords[i].result = snailNode;
            }
            
            snailWords[0].noun = TerminalUtils.CreateTerminalKeyword("snail", false);
            snailWords[1].noun = TerminalUtils.CreateTerminalKeyword("Snail", false);
            snailWords[2].noun = TerminalUtils.CreateTerminalKeyword("immortalsnail", false);
            snailWords[3].noun = TerminalUtils.CreateTerminalKeyword("immortal snail", false);
            snailWords[4].noun = TerminalUtils.CreateTerminalKeyword("Immortal snail", false);
            snailWords[5].noun = TerminalUtils.CreateTerminalKeyword("immortal Snail", false);
            snailWords[6].noun = TerminalUtils.CreateTerminalKeyword("Immortal Snail", false);
            snailWords[7].noun = TerminalUtils.CreateTerminalKeyword("immortal-snail", false);
            snailWords[8].noun = TerminalUtils.CreateTerminalKeyword("Immortal-snail", false);
            snailWords[9].noun = TerminalUtils.CreateTerminalKeyword("immortal-Snail", false);
            snailWords[10].noun = TerminalUtils.CreateTerminalKeyword("Immortal-Snail", false);

            TerminalKeyword snailKeyword = TerminalUtils.CreateTerminalKeyword("The-Immortal-Snail", false, snailWords);
            */

            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(snail.enemyPrefab);
            LethalLib.Modules.Enemies.RegisterEnemy(snail, configRarity.Value, levelFlags, spawnType, /*snailNode, snailKeyword*/ null, null);           

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