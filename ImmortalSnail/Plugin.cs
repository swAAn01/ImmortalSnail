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
    [BepInDependency(LethalLib.Plugin.ModGUID)]
    [BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {

        public static AssetBundle bundle;

        public static ConfigEntry<float> configSize;
        public static ConfigEntry<float> configSpeed;
        public static ConfigEntry<int> configMaxSnails;
        public static ConfigEntry<int> configRarity;
        public static ConfigEntry<bool> configGoOutside;
        public static ConfigEntry<bool> configEnterShip;

        private void Awake()
        {
            Logger.LogInfo("Loading a mod by swAAn\n\n" +
                "                                    _\n" +
                "                                ,-\"\" \"\".\n" +
                "                              ,'  ____  `.\n" +
                "                            ,'  ,'    `.  `._\n" +
                "   (`.         _..--.._   ,'  ,'        \\    \\\n" +
                "  (`-.\\    .-\"\"        \"\"'   /          (  d _b\n" +
                " (`._  `-\"\" ,._             (            `-(   \\\n" +
                " <_  `     (  <`<            \\              `-._\\\n" +
                "  <`-       (__< <           :\n" +
                "   (__        (_<_<          ;\n" +
                "    `~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~"
                );
            Logger.LogInfo
               ("\n         __,._" + "\n" +
                "        /  _  \\" + "\n" +
                "       |  6 \\  \\  oo" + "\n" +
                "        \\___/ .|__||" + "\n" +
                " __,..=\" ^  . , \"  ,\\" + "\n" +
                "<.__________________/");

            // setup for Unity Netcode Patcher
            NetcodePatcher();

            // configuration setup
            configSize = Config.Bind("General", "Scale", 100.0f, "The scale of the snail.");
            configSpeed = Config.Bind("General", "Speed", 0.5f, "The speed of the snail.");
            configMaxSnails = Config.Bind("General", "Max Snails", 4, "The maximum number of snails that can spawn in a round.");
            configRarity = Config.Bind("General", "Rarity", 100, "Honestly not sure exactly how this works, but a higher \"Rarity\" will make the snail more likely to spawn.");
            configGoOutside = Config.Bind("Pathing", "Can Go Outside", true, "If enabled, allows the snail to exit the factory and chase players outside.");
            configEnterShip = Config.Bind("Pathing", "Can Enter Ship", true, "If enabled, allows the snail to target players that are in the ship room.");

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

            TerminalNode snailNode = ScriptableObject.CreateInstance<TerminalNode>();
            snailNode.displayText = "The Immortal Snail\n\nDanger level: 50%\n\n" +
                "The following is a recitation of the events that transpired on [REDACTED] to the best of my memory. This is my experience with the entity and I swear to do right " +
            "by the company and describe it as best I can. There were four of us out on a routine scrap-job, absolutely nothing was out of the ordinary before [REDACTED] spotted it. " +
            "We laughed our asses off. Half the things in here will make you soil your hazmat suit on the spot, and then there's this thing, moving at, well you know. We had our fun, but ultimately " +
            "moved on and forgot about the thing. Me and the boys went our separate ways and decided to rendezvous at the ship. Three of us returned, but [REDACTED] was missing. We all decided to go back in and investigate. " +
            "The place was silent. We thought things might have taken a turn for the worse, so we decided to split up and either guide [REDACTED] back to ship, or at least recover his remains. " +
            "As often happens, I found myself lost. Turning the corners of this elaborate labyrinth, I finally found what was left of [REDACTED]. It was unusual, I'd never seen a coworker left in such a state. " +
            "Reality struck as I turned a corner and found another comrade waiting on the ground for me, and then another. I made the heartbreaking decision to leave my comrades behind. I was almost free, when I find staring at " +
            "me from the front entrance the same snail I'd seen before. I saw red. I beat that thing as ruthlessly as I could (with my shovel), entirely imprinting my rage on its presumably fragile shell. " +
            "I expected to find a small puddle where the snail once stood, but the it was unaffected. It moved towards me at the same agitating pace it did before. The aftermath is a blur. Sometimes I wonder if I even made it back to the ship at all." +
            "\n\n";

            snailNode.clearPreviousText = true;
            snailNode.maxCharactersToType = 2000;
            snailNode.creatureName = "The Immortal Snail";
            snailNode.creatureFileID = 1738;

            TerminalKeyword snailKeyword = TerminalUtils.CreateTerminalKeyword("snail", specialKeywordResult: snailNode);

            NetworkPrefabs.RegisterNetworkPrefab(snail.enemyPrefab);
            Enemies.RegisterEnemy(snail, configRarity.Value, levelFlags, spawnType, snailNode, snailKeyword);

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} " + $"{PluginInfo.PLUGIN_VERSION} is loaded!");
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