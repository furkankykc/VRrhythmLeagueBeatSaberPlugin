using VRrhythmLeague.Interop;
using VRrhythmLeague.LeagueAPI;
using VRrhythmLeague.Misc;
using VRrhythmLeague.OverriddenClasses;
using VRrhythmLeague.UI;
using BS_Utils.Gameplay;
using HarmonyLib;
using IPA;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRrhythmLeague
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static Plugin instance;
        public static IPA.Logging.Logger log;

        private static bool joinAfterRestart;
        private static string joinSecret;
        public static bool DownloaderExists { get; private set; }

        [Init]
        public void Init(IPA.Logging.Logger logger)
        {
            log = logger;
        }

        [OnEnable]
        public void Enable()
        {
            instance = this;

            BS_Utils.Utilities.BSEvents.OnLoad();
            BS_Utils.Utilities.BSEvents.menuSceneLoadedFresh += MenuSceneLoadedFresh;
            BS_Utils.Utilities.BSEvents.menuSceneLoaded += MenuSceneLoaded;
            BS_Utils.Utilities.BSEvents.gameSceneLoaded += GameSceneLoaded;

            if (Config.Load())
                log.Info("Loaded config!");
            else
                Config.Create();

            try
            {
                PresetsCollection.ReloadPresets();
            }
            catch (Exception e)
            {
                log.Warn("Unable to load presets! Exception: " + e);
            }



            Sprites.ConvertSprites();

            ScrappedData.Instance.DownloadScrappedData(null);
            //LeagueRequestHandler.Instance.RequestLeagueData("", null);

            try
            {
                HarmonyPatcher.Patch();
            }
            catch (Exception e)
            {
                Plugin.log.Error("Unable to patch assembly! Exception: " + e);
            }

            if (IPA.Loader.PluginManager.GetPluginFromId("BeatSaverDownloader") != null)
                DownloaderExists = true;


            if(SteamManager.Initialized)
                SteamRichPresence.Init();
        }



        public void OnActivityJoin(string secret)
        {
            if (SceneManager.GetActiveScene().name.Contains("Menu") && !Client.Instance.inRoom && !Client.Instance.inRadioMode)
            {
                joinAfterRestart = true;
                joinSecret = secret;
                Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().First().RestartGame();
            }
        }

        private void MenuSceneLoadedFresh()
        {
            PluginUI.OnLoad();
            InGameOnlineController.OnLoad();
            SpectatingController.OnLoad();
            GetUserInfo.UpdateUserInfo();

            if (joinAfterRestart)
            {
                joinAfterRestart = false;
             //   SharedCoroutineStarter.instance.StartCoroutine(PluginUI.instance.JoinGameWithSecret(joinSecret));
                joinSecret = string.Empty;
            }
        }

        private void MenuSceneLoaded()
        {
            InGameOnlineController.Instance?.MenuSceneLoaded();
            SpectatingController.Instance?.MenuSceneLoaded();
        }

        private void GameSceneLoaded()
        {
            InGameOnlineController.Instance?.GameSceneLoaded();
            SpectatingController.Instance?.GameSceneLoaded();
        }


    }
}
