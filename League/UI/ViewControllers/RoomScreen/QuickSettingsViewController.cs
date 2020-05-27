using BeatSaberMarkupLanguage;
using HMUI;
using System.Reflection;
using UnityEngine;

namespace VRrhythmLeague.UI.ViewControllers.RoomScreen
{
    class QuickSettingsViewController : ViewController
    {
        private Settings _settings;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                _settings = new GameObject("Multiplayer Quick Settings").AddComponent<Settings>();

                BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetAssembly(this.GetType()), "VRrhythmLeague.UI.ViewControllers.RoomScreen.QuickSettingsViewController"), gameObject, _settings);
            }
        }
    }
}
