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
                Min = 25f,
                Max = 225f
            });

            var speedSlider = new FloatSliderConfigItem(Plugin.configSpeed, new FloatSliderOptions
            {
                Min = 0.2f,
                Max = 1.0f
            });

            var maxSnailsSlider = new IntSliderConfigItem(Plugin.configMaxSnails, new IntSliderOptions
            {
                Min = 0,
                Max = 4
            });

            var raritySlider = new IntSliderConfigItem(Plugin.configRarity, new IntSliderOptions
            {
                Min = 0,
                Max = 100
            });

            LethalConfigManager.AddConfigItem(sizeSlider);
            LethalConfigManager.AddConfigItem(speedSlider);
            LethalConfigManager.AddConfigItem(maxSnailsSlider);
            LethalConfigManager.AddConfigItem(raritySlider);
        }
    }
}
