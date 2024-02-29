using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;
using LethalConfig;

namespace ImmortalSnail
{
    class ConfigManager
    {
        public static void setupLethalConfig()
        {
            var sizeSlider = new FloatSliderConfigItem(Plugin.configSize, new FloatSliderOptions
            {
                Min = 75f,
                Max = 225f
            });

            var speedSlider = new FloatSliderConfigItem(Plugin.configSpeed, new FloatSliderOptions
            {
                Min = 0.1f,
                Max = 2.0f
            });

            var maxSnailsSlider = new IntSliderConfigItem(Plugin.configMaxSnails, new IntSliderOptions
            {
                Min = 0,
                Max = 10
            });

            var raritySlider = new IntSliderConfigItem(Plugin.configRarity, new IntSliderOptions
            {
                Min = 0,
                Max = 100
            });

            var goOutsideBox = new BoolCheckBoxConfigItem(Plugin.configGoOutside, requiresRestart: false);
            var enterShipBox = new BoolCheckBoxConfigItem(Plugin.configEnterShip, requiresRestart: false);
            var garyBox = new BoolCheckBoxConfigItem(Plugin.configGary, requiresRestart: true);
            var canExplodeBox = new BoolCheckBoxConfigItem(Plugin.configCanExplode, requiresRestart: false);
            var explosionKillOthersBox = new BoolCheckBoxConfigItem(Plugin.configExplosionKillOthers, requiresRestart: false);
            var showTargetBox = new BoolCheckBoxConfigItem(Plugin.configShowTarget, requiresRestart: false);

            LethalConfigManager.AddConfigItem(sizeSlider);
            LethalConfigManager.AddConfigItem(speedSlider);
            LethalConfigManager.AddConfigItem(maxSnailsSlider);
            LethalConfigManager.AddConfigItem(raritySlider);
            LethalConfigManager.AddConfigItem(goOutsideBox);
            LethalConfigManager.AddConfigItem(enterShipBox);
            LethalConfigManager.AddConfigItem(garyBox);
            LethalConfigManager.AddConfigItem(canExplodeBox);
            LethalConfigManager.AddConfigItem(explosionKillOthersBox);
            LethalConfigManager.AddConfigItem(showTargetBox);
        }
    }
}
