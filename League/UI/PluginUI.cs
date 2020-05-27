using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.Settings;
using VRrhythmLeague.Data;
using VRrhythmLeague.Misc;
using VRrhythmLeague.UI.FlowCoordinators;
using VRrhythmLeague.SimpleJSON;
using BS_Utils.Gameplay;
using BS_Utils.Utilities;
using HMUI;
using Polyglot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VRrhythmLeague.Interop;
using VRrhythmLeague.LeagueAPI;
using BeatSaberMarkupLanguage.MenuButtons;
using System.Reflection;
using VRrhythmLeague.UI.ViewControllers.RoomScreen;
using static HMUI.ViewController;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;

namespace VRrhythmLeague.UI
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;

        internal MainMenuViewController _mainMenuViewController;
        private RectTransform _mainMenuRectTransform;
        private SimpleDialogPromptViewController _noUserInfoWarning;

        public ServerHubFlowCoordinator serverHubFlowCoordinator;
        public RoomCreationFlowCoordinator roomCreationFlowCoordinator;
        public RoomFlowCoordinator roomFlowCoordinator;
        public LeaguePlaylistFlowCoordinator leaguePlaylistFlowCoordinator;
        //public ChannelSelectionFlowCoordinator channelSelectionFlowCoordinator;
        //public RadioFlowCoordinator radioFlowCoordinator;

        private TextMeshProUGUI _newVersionText;
        private Button _multiplayerButton;

        private Settings _settings;

        public static void OnLoad()
        {
            if (instance == null)
            {
                new GameObject("Multiplayer Plugin").AddComponent<PluginUI>().Setup();
            }
        }

        public void Setup()
        {
            instance = this;
            GetUserInfo.UpdateUserInfo();

            CreateUI();

            if (SongCore.Loader.AreSongsLoading)
                SongCore.Loader.SongsLoadedEvent += SongsLoaded;
            else
                SongsLoaded(null, null);
        }

        public void SongsLoaded(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            if (_multiplayerButton != null)
            {
                LeagueRequestHandler.Instance.RequestLeagueData("",
                () =>
                {

                });
                _multiplayerButton.interactable = true;
            }

            SongInfo.GetOriginalLevelHashes();
        }

        public void CreateUI()
        {
            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                if (serverHubFlowCoordinator == null)
                {
                    serverHubFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ServerHubFlowCoordinator>();
                }
                if (roomCreationFlowCoordinator == null)
                {
                    roomCreationFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomCreationFlowCoordinator>();
                }
                if (roomFlowCoordinator == null)
                {
                    roomFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomFlowCoordinator>();
                }
                if (leaguePlaylistFlowCoordinator == null)
                {
                    leaguePlaylistFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<LeaguePlaylistFlowCoordinator>();
                    leaguePlaylistFlowCoordinator.didFinishEvent += () =>
                    {
                        Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First().InvokeMethod("DismissFlowCoordinator", leaguePlaylistFlowCoordinator, null, false);

                        if (SteamManager.Initialized)
                            SteamRichPresence.ClearSteamRichPresence();
                    };

                }
                /*
                if (channelSelectionFlowCoordinator == null)
                {
                    channelSelectionFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ChannelSelectionFlowCoordinator>();
                }
                if (radioFlowCoordinator == null)
                {
                    radioFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RadioFlowCoordinator>();
                }*/

                CreateOnlineButton();
                //StartCoroutine(LeagueRequestHandler.Instance.CheckVersion(
                //    () =>
                //    {
                //        Plugin.log.Debug("current ver checked");
                //        SemVer.Version currentVer = IPA.Loader.PluginManager.GetPlugin("Beat Saber Multiplayer").Version;
                //        //LeagueRequestHandler.Instance.state == PluginState.UpdateRequired
                //        if (true)
                //        {
                //            _newVersionText.gameObject.SetActive(true);
                //            _newVersionText.text = $"Version \n of the mod is available!\nCurrent mod version: {currentVer}";
                //            _newVersionText.alignment = TextAlignmentOptions.Center;
                //        }
                //    }
                //    ));



                //_settings = new GameObject("Multiplayer Settings").AddComponent<Settings>();
                //BSMLSettings.instance.AddSettingsMenu("Multiplayer", "VRrhythmLeague.UI.Settings", _settings);
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to create UI! Exception: {e}");
            }
        }
        private ReleaseInfoViewController releaseInfoViewController;
        private void OnDeactivate(DeactivationType deactivationType)
        {
            parserParams.EmitEvent("close-modals");
        }
        [UIParams]
        private BSMLParserParams parserParams;
        private void CreateLeftScreen()
        {
            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            MainMenuViewController mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
            SeasonInfoViewController _seasonInfoViewController = BeatSaberUI.CreateViewController<SeasonInfoViewController>();
            releaseInfoViewController = Resources.FindObjectsOfTypeAll<ReleaseInfoViewController>().First();
            //releaseInfoViewController.didDeactivateEvent -= OnDeactivate;
            //releaseInfoViewController.didDeactivateEvent += OnDeactivate;
            //BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "VRrhythmLeague.UI.ViewControllers.main-left-screen.bsml"), releaseInfoViewController.gameObject, this);
            //Destroy(releaseInfoViewController.gameObject.GetComponent<Canvas>());
            //BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "VRrhythmLeague.UI.ViewControllers.RoomScreen.HighScoresViewController.bsml"), _seasonInfoViewController.gameObject, this);

            //releaseInfoViewController.ReplaceViewControllerCoroutine(_seasonInfoViewController,null,true,0);
            //releaseInfoViewController.PresentViewControllerCoroutine(_seasonInfoViewController, null, true);
            //releaseInfoViewController.__Deactivate(DeactivationType.RemovedFromHierarchy,true);
            //mainFlow.InvokeMethod("DismissViewController", releaseInfoViewController, null, true);
            //mainFlow.InvokeMethod("SetLeftScreenViewController", _seasonInfoViewController, null, true);
            mainFlow.InvokeMethod("PresentViewController", releaseInfoViewController, null, false);




        }
        private void CreateOnlineButton()
        {

            _newVersionText = BeatSaberUI.CreateText(_mainMenuRectTransform, "A new version of the mod\nis available!", new Vector2(55.5f, 33f));
            _newVersionText.fontSize = 5f;
            _newVersionText.lineSpacing = -52;
            _newVersionText.gameObject.SetActive(false);

            Button[] mainButtons = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "MainButtons" && x.parent.name == "MainMenuViewController").GetComponentsInChildren<Button>();

            foreach (var item in mainButtons)
            {
                (item.transform as RectTransform).sizeDelta = new Vector2(35f, 30f);
            }

            _multiplayerButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "SoloFreePlayButton")), _mainMenuRectTransform, false);
            _multiplayerButton.name = "BSMultiplayerButton";
            Destroy(_multiplayerButton.GetComponentInChildren<LocalizedTextMeshProUGUI>());
            Destroy(_multiplayerButton.GetComponentInChildren<HoverHint>());
            _multiplayerButton.transform.SetParent(mainButtons.First(x => x.name == "SoloFreePlayButton").transform.parent);
            _multiplayerButton.transform.SetAsLastSibling();

            _multiplayerButton.SetButtonText("The League");
            //_multiplayerButton.SetButtonTextSize(0);
            _multiplayerButton.SetButtonIcon(Sprites.leagueIcon);
            //_multiplayerButton.SetButtonBackground(Sprites.leagueIcon);

            _multiplayerButton.interactable = !SongCore.Loader.AreSongsLoading;

            _multiplayerButton.onClick = new Button.ButtonClickedEvent();
            _multiplayerButton.onClick.AddListener(delegate ()
            {
                try
                {

                    MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

                    if (_noUserInfoWarning == null)
                    {
                        var dialogOrig = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(mainFlow, "_simpleDialogPromptViewController");
                        _noUserInfoWarning = Instantiate(dialogOrig.gameObject).GetComponent<SimpleDialogPromptViewController>();
                    }

                    if (false)
                    {
                        _noUserInfoWarning.Init("No access to multiplayer", $"You need \"Multiplayer Pro Elite Pack\" to continue\nWould you like to buy it now?", "Buy access", "Go back",
                           (selectedButton) =>
                           {
                               mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 0);
                               if (selectedButton == 0)
                               {
                                   mainFlow.InvokeMethod("PresentFlowCoordinator", leaguePlaylistFlowCoordinator, null, true, false);
                               }
                           });
                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);
                    }
                    else if (GetUserInfo.GetUserID() == 0)
                    {
                        _noUserInfoWarning.Init("Invalid username and ID", $"Your username and ID are invalid\nMake sure you are logged in", "Go back", "Continue anyway",
                              (selectedButton) =>
                              {
                                  mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 1);
                                  if (selectedButton == 1)
                                  {
                                      mainFlow.InvokeMethod("PresentFlowCoordinator", leaguePlaylistFlowCoordinator, null, true, false);
                                  }
                              });
                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);
                    }
                    else if (LeagueRequestHandler.Instance.userState == UserState.NotRegistered)
                    {
                        LeagueRequestHandler.Instance.RequestLeagueData("",
   () =>
   {

   });
                        _noUserInfoWarning.Init("Account not REGISTERED", $"You have to register on www.vrrhythmleague.com\nMake sure login as registered steam account", "Go back",
                              (selectedButton) =>
                              {
                                  mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 1);
                                  if (selectedButton == 1)
                                  {
                                      mainFlow.InvokeMethod("PresentFlowCoordinator", leaguePlaylistFlowCoordinator, null, true, false);
                                      leaguePlaylistFlowCoordinator.PacketReceived();

                                  }
                              });
                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);
                    }
                    else if (LeagueRequestHandler.Instance.userState == UserState.DownloadingSongs)
                    {
                        int downloadedSongs = LeagueRequestHandler.Instance.loadedSongCount;
                        int totalSongs = LeagueRequestHandler.Instance.seasons.SelectMany(season => season.current_week.songs).Distinct().Count();
                        _noUserInfoWarning.Init("DOWNLOADING Season Songs", $"Loaded Songs = {downloadedSongs} / {totalSongs}\n Please Wait ", "Go back",
                            
                              (selectedButton) =>
                              {
                                  mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 1);
                              }
                                );
                        LeagueRequestHandler.Instance.downloadCompleted += () =>
                         {
                             mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, true);
                             _noUserInfoWarning.Init("Downloading Season Songs", $"DownloadFinished", "Go back",
                                         "Continue to League",
                                         (ss) =>
                                         {
                                             mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, ss == 1);
                                             if (ss == 1)
                                             {


                                                 mainFlow.InvokeMethod("PresentFlowCoordinator", leaguePlaylistFlowCoordinator, null, false, false);
                                                 leaguePlaylistFlowCoordinator.PacketReceived();
                                                 LeagueRequestHandler.Instance.createLeaguePacks(LeagueRequestHandler.Instance.seasons);

                                             }

                                         });
                             mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);


                         };
                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);
                    }
                    else if (LeagueRequestHandler.Instance.userState == UserState.NotConnected)
                    {
                        _noUserInfoWarning.Init("CONNECTION Error", $"We cant connect to League", "Go back",
                              (selectedButton) =>
                              {
                                  mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 1);
                                  if (selectedButton == 1)
                                  {
                                      mainFlow.InvokeMethod("PresentFlowCoordinator", leaguePlaylistFlowCoordinator, null, true, false);
                                  }
                              });
                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);
                    }
                    else
                    {
                        mainFlow.InvokeMethod("PresentFlowCoordinator", leaguePlaylistFlowCoordinator, null, false, false);
                        leaguePlaylistFlowCoordinator.PacketReceived();
                    }
                }
                catch (Exception e)
                {
                    Plugin.log.Critical($"Unable to present flow coordinator! Exception: {e}");
                }
            });
        }






    }
}
