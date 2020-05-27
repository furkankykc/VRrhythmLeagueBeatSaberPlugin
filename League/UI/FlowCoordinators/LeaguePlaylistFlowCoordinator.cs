using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Components;
using VRrhythmLeague.Data;
using VRrhythmLeague.Interop;
using VRrhythmLeague.IPAUtilities;
using VRrhythmLeague.LeagueAPI;
using VRrhythmLeague.Misc;
using VRrhythmLeague.UI.ViewControllers.RoomScreen;
using BeatSaverDownloader;
using BS_Utils.Utilities;
using HMUI;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace VRrhythmLeague.UI.FlowCoordinators
{
    public enum SortMode { Default, Difficulty, Newest };

    class LeaguePlaylistFlowCoordinator : FlowCoordinator
    {
        public event Action didFinishEvent;

        private SongPreviewPlayer _songPreviewPlayer;

        public SongPreviewPlayer PreviewPlayer
        {
            get
            {
                if (_songPreviewPlayer == null)
                {
                    _songPreviewPlayer = Resources.FindObjectsOfTypeAll<SongPreviewPlayer>().FirstOrDefault();
                }

                return _songPreviewPlayer;
            }
            private set { _songPreviewPlayer = value; }
        }

        public IDifficultyBeatmap levelDifficultyBeatmap;
        public LevelCompletionResults levelResults;
        public int lastHighscoreForLevel;
        public bool lastHighscoreValid;
        public BeatmapDifficulty LastSelectedDifficulty;
        public String LastSelectedSeason;
        AdditionalContentModel _contentModelSO;
        BeatmapLevelsModel _beatmapLevelsModel;

        RoomNavigationController _roomNavigationController;

        SongSelectionViewControllerXcopy _songSelectionViewController;
        DifficultySelectionViewController _difficultySelectionViewController;
        MultiplayerResultsViewController _resultsViewController;
        PlayingNowViewController _playingNowViewController;
        LevelPacksUIViewController _levelPacksViewController;
        RequestsViewController _requestsViewController;
        SongInfo LastSelectedSongInfo;
        PlayerManagementViewController _playerManagementViewController;
        HighScoresViewController _highScoresViewController;
        SeasonInfoViewController _seasonInfoViewController;
        QuickSettingsViewController _quickSettingsViewController;

        private IAnnotatedBeatmapLevelCollection _lastSelectedCollection;
        IAnnotatedBeatmapLevelCollection LastSelectedCollection
        {
            get { return _lastSelectedCollection; }
            set
            {
                if (_lastSelectedCollection == value)
                    return;
                _lastSelectedCollection = value;
#if DEBUG
                if (value == null)
                    Plugin.log.Debug($"LastSelectedCollection set to <NULL>");
                else
                    Plugin.log.Debug($"LastSelectedCollection set to {value.collectionName}");
#endif
            }
        }
        private SortMode _lastSortMode;
        SortMode LastSortMode
        {
            get { return _lastSortMode; }
            set
            {
                if (_lastSortMode == value)
                    return;
                _lastSortMode = value;
#if DEBUG
                Plugin.log.Debug($"LastSortMode set to {value.ToString()}");
#endif
            }
        }
        private string _lastSearchRequest;
        string LastSearchRequest
        {
            get { return _lastSearchRequest; }
            set
            {
                if (_lastSearchRequest == value)
                    return;
                _lastSearchRequest = value;
#if DEBUG
                if (string.IsNullOrEmpty(value))
                    Plugin.log.Debug($"LastSearchRequest set to {(value == null ? "<NULL>" : "<Empty>")}");
                else
                    Plugin.log.Debug($"LastSearchRequest set to {value}");
#endif
            }
        }

        private string _lastSelectedSong;
        string LastSelectedSong
        {
            get { return _lastSelectedSong; }
            set
            {
                if (_lastSelectedSong == value)
                    return;
                _lastSelectedSong = value;
#if DEBUG
                if (string.IsNullOrEmpty(value))
                    Plugin.log.Debug($"LastSelectedSong set to {(value == null ? "<NULL>" : "<Empty>")}");
                else
                    Plugin.log.Debug($"LastSelectedSong set to {value}");
#endif
            }
        }

        private float _lastScrollPosition;
        public float LastScrollPosition
        {
            get { return _lastScrollPosition; }
            set
            {
                if (_lastScrollPosition == value)
                    return;
                _lastScrollPosition = value;
            }
        }


        RoomInfo roomInfo = new RoomInfo();


        bool joined = false;
        private SimpleDialogPromptViewController _passHostDialog;
        private SimpleDialogPromptViewController _roomLeaveDialog;

        private List<SongInfo> _requestedSongs = new List<SongInfo>();

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
           
            _beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().FirstOrDefault();
            _contentModelSO = Resources.FindObjectsOfTypeAll<AdditionalContentModel>().FirstOrDefault();
            if (firstActivation && activationType == ActivationType.AddedToHierarchy)
            {
                _playerManagementViewController = BeatSaberUI.CreateViewController<PlayerManagementViewController>();
                _playerManagementViewController.gameplayModifiersChanged += UpdateLevelOptions;
                _playerManagementViewController.transferHostButtonPressed += TransferHostConfirmation;  

                var dialogOrig = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(FindObjectOfType<MainFlowCoordinator>(), "_simpleDialogPromptViewController");
                _passHostDialog = Instantiate(dialogOrig.gameObject).GetComponent<SimpleDialogPromptViewController>();
                _roomLeaveDialog = Instantiate(dialogOrig.gameObject).GetComponent<SimpleDialogPromptViewController>();

                //_quickSettingsViewController = BeatSaberUI.CreateViewController<QuickSettingsViewController>();
                _quickSettingsViewController = BeatSaberUI.CreateViewController<QuickSettingsViewController>();
                _highScoresViewController = BeatSaberUI.CreateViewController<HighScoresViewController>();
                _seasonInfoViewController = BeatSaberUI.CreateViewController<SeasonInfoViewController>();
                _roomNavigationController = BeatSaberUI.CreateViewController<RoomNavigationController>();
                
            }
            LeagueRequestHandler.Instance.scoreUploaded += () =>
            {
                _highScoresViewController.UpdateLeaderboard(Season.getSeason(LastSelectedSeason));
                _seasonInfoViewController.UpdateLeaderboard(Season.getSeason(LastSelectedSeason));
            };
            showBackButton = true;


            //ProvideInitialViewControllers(_roomNavigationController, _playerManagementViewController, _quickSettingsViewController);
            ProvideInitialViewControllers(_roomNavigationController, _highScoresViewController, _seasonInfoViewController);

        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            Plugin.log.Debug("Back button was pressed");

            if (topViewController == _roomNavigationController)

                if (roomInfo.roomState == RoomState.Results)
                {
                    HideInGameLeaderboard();
                    Plugin.log.Debug("Change view results to difficulty");
                    ShowDifficultySelection(roomInfo.selectedSong);
                    
                    roomInfo.roomState = RoomState.Preparing;
                }
            else if (roomInfo.roomState == RoomState.Preparing)
                {
                    Plugin.log.Debug("Change view difficulty to songs menu");
                    HideInGameLeaderboard();
                    UpdateUI(RoomState.SelectingSong);
                    roomInfo.roomState = RoomState.SelectingSong;

                }
                else
                {
                    PopAllViewControllers();
                    LeaveRoom();
                }
        }




        public void LeaveRoom(bool force = false)
        {
            if (Client.Instance != null && Client.Instance.connected && !force)
            {
                string leaveMsg = (
                    true ?
                    "<size=120%><b>You're the host!</b></size>\nAre you sure you want to leave the room?" :
                    "Are you sure you want to leave this room?"
                );

                _roomLeaveDialog.Init("Leave room?", leaveMsg, "Leave", "Cancel", (selectedButton) =>
                {
                    DismissViewController(_roomLeaveDialog);
                    if (selectedButton == 0)
                    {
                        LeaveRoom(true);
                    }
                });
                PresentViewController(_roomLeaveDialog);
                return;
            }

            try
            {
                if (joined)
                {
                    InGameOnlineController.Instance.needToSendUpdates = false;
                    Client.Instance.LeaveRoom();
                    joined = false;
                }
                if (Client.Instance.connected)
                {
                    Client.Instance.Disconnect();
                }
            }
            catch
            {
                Plugin.log.Warn("Unable to disconnect from ServerHub properly!");
            }

            Client.Instance.ClearMessageQueue();



            InGameOnlineController.Instance.DestroyPlayerControllers();
            InGameOnlineController.Instance.VoiceChatStopRecording();
            PreviewPlayer.CrossfadeToDefault();
            LastSelectedSong = "";
            LastSortMode = SortMode.Default;
            LastSearchRequest = "";
            levelDifficultyBeatmap = null;
            levelResults = null;
            lastHighscoreForLevel = 0;
            lastHighscoreValid = false;
            Client.Instance.inRoom = false;
            _requestedSongs.Clear();
            PopAllViewControllers();
            SetLeftScreenViewController(_playerManagementViewController);
            if (SteamManager.Initialized)
                SteamRichPresence.ClearSteamRichPresence();
            didFinishEvent?.Invoke();
        }




        public void PacketReceived()
        {
            //0 join room


            if (!joined)
            {
                        joined = true;
                        levelDifficultyBeatmap = null;
                        levelResults = null;
                        lastHighscoreForLevel = 0;
                        lastHighscoreValid = false;
                Plugin.log.Debug($"Just joined");
                UpdateUI(RoomState.SelectingSong);

                Plugin.log.Debug($"ui after select");
            }
            else
            {
                Plugin.log.Debug($"Ui select song");
                
            }
  


        }

        public void UpdateUI(RoomState state)
        {
            switch (state)
            {
                case RoomState.SelectingSong:
                    {
                        PopAllViewControllers();
                        //if (roomInfo.songSelectionType == SongSelectionType.Manual)
                        //_highScoresViewController.UpdateLeaderboard();
                        ShowSongsList();
                        roomInfo.roomState = RoomState.SelectingSong;

                    }
                    break;
                case RoomState.Preparing:
                    {
                        PopAllViewControllers();

                        if (roomInfo.selectedSong != null)
                        {
                            LastSelectedSongInfo = roomInfo.selectedSong;
                            ShowDifficultySelection(roomInfo.selectedSong);
                            roomInfo.roomState = RoomState.Preparing;
                        }
                        else
                        {

                            ShowSongsList();
                            roomInfo.roomState = RoomState.SelectingSong;

                        }
                    }
                    break;
                case RoomState.InGame:
                    {
                        PopAllViewControllers();
                        ShowInGameLeaderboard(roomInfo.selectedSong);
                    }
                    break;
                case RoomState.Results:
                    {
                        PopAllViewControllers();

                        ShowResultsLeaderboard(roomInfo.selectedSong);
                        roomInfo.roomState = RoomState.Results;

                    }
                    break;
            }
            _playerManagementViewController.UpdateViewController(true, (int)state <= 1);

        }
        public void UpdateInGameLeaderboard(float currentTime, float totalTime)
        {
            if (_playingNowViewController != null)
            {
                _playingNowViewController.UpdateLeaderboard();
                _playingNowViewController.SetTimer(currentTime, totalTime);
            }
        }
        public void HideInGameLeaderboard()
        {
            _roomNavigationController.ClearChildViewControllers();

            PreviewPlayer.CrossfadeToDefault();
        }

        private void UpdateLevelOptions()
        {
            try
            {
                if (_playerManagementViewController != null)
                {
                    if (true)
                    {
                        if (_difficultySelectionViewController != null)
                        {
                            /*  LevelOptionsInfo info = new LevelOptionsInfo(_difficultySelectionViewController.selectedDifficulty, _playerManagementViewController.modifiers, _difficultySelectionViewController.selectedCharacteristic.serializedName); */
                            Plugin.log.Debug($"Difficulty is = {LastSelectedDifficulty}");
                            LevelOptionsInfo info = new LevelOptionsInfo(LastSelectedDifficulty, GameplayModifiers.defaultModifiers, _difficultySelectionViewController.selectedCharacteristic.serializedName);
                            //Client.Instance.SetLevelOptions(info);
                            roomInfo.startLevelInfo = info;
                            //Client.Instance.playerInfo.updateInfo.playerLevelOptions = info;

                        }
                        else
                        {

                            // todo burada sarkının diffini alcaz
                            //LevelOptionsInfo info = new LevelOptionsInfo(BeatmapDifficulty.Hard, _playerManagementViewController.modifiers, "Standard");
                            LevelOptionsInfo info = new LevelOptionsInfo(LastSelectedDifficulty, GameplayModifiers.defaultModifiers, "Standard");
                            //Client.Instance.SetLevelOptions(info);
                            roomInfo.startLevelInfo = info;
                            //Client.Instance.playerInfo.updateInfo.playerLevelOptions = info;
                        }
                        
                    }

                }
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to update level options! Exception: {e}");
            }
        }

        public void TransferHostConfirmation(PlayerInfo newHost)
        {
            _passHostDialog.Init("Pass host?", $"Are you sure you want to pass host to <b>{newHost.playerName}</b>?", "Pass host", "Cancel",
                (selectedButton) =>
                {
                    SetLeftScreenViewController(_playerManagementViewController);
                    if (selectedButton == 0)
                    {
                        Client.Instance.TransferHost(newHost);
                    }
                });
            SetLeftScreenViewController(_passHostDialog);
        }

        public void StartLevel(IBeatmapLevel level, BeatmapCharacteristicSO characteristic, BeatmapDifficulty difficulty, GameplayModifiers modifiers, float startTime = 0f)
        {
            Client.Instance.playerInfo.updateInfo.playerComboBlocks = 0;
            Client.Instance.playerInfo.updateInfo.playerCutBlocks = 0;
            Client.Instance.playerInfo.updateInfo.playerTotalBlocks = 0;
            Client.Instance.playerInfo.updateInfo.playerEnergy = 0f;
            Client.Instance.playerInfo.updateInfo.playerScore = 0;
            Client.Instance.playerInfo.updateInfo.playerLevelOptions = new LevelOptionsInfo(difficulty, modifiers, characteristic.serializedName);

            MenuTransitionsHelper menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuTransitionsHelper>().FirstOrDefault();

            //if (_playerManagementViewController != null)
            //{
            //    _playerManagementViewController.SetGameplayModifiers(modifiers);
            //}

            if (menuSceneSetupData != null)
            {
                //Client.Instance.playerInfo.updateInfo.playerState = Config.Instance.SpectatorMode ? PlayerState.Spectating : PlayerState.Game;

                PlayerData playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().FirstOrDefault().playerData;

                PlayerSpecificSettings playerSettings = playerData.playerSpecificSettings;
                OverrideEnvironmentSettings environmentOverrideSettings = playerData.overrideEnvironmentSettings;

                ColorScheme colorSchemesSettings = playerData.colorSchemesSettings.overrideDefaultColors ? playerData.colorSchemesSettings.GetColorSchemeForId(playerData.colorSchemesSettings.selectedColorSchemeId) : null;
                //belki sıkıntı buradadır
                //roomInfo.roomState = RoomState.InGame;

                IDifficultyBeatmap difficultyBeatmap = level.GetDifficultyBeatmap(characteristic, difficulty, false);

                Plugin.log.Debug($"Starting song: name={level.songName}, levelId={level.levelID}, difficulty={difficulty}");


                try
                {
                    BS_Utils.Gameplay.Gamemode.NextLevelIsIsolated("Beat Saber Multiplayer");
                }
                catch
                {

                }

                PracticeSettings practiceSettings = null;
                //if (Config.Instance.SpectatorMode || startTime > 1f)
                //{
                //    practiceSettings = new PracticeSettings(PracticeSettings.defaultPracticeSettings);
                //    if (startTime > 1f)
                //    {
                //        practiceSettings.startSongTime = startTime + 1.5f;
                //        practiceSettings.startInAdvanceAndClearNotes = true;
                //    }
                //    practiceSettings.songSpeedMul = modifiers.songSpeedMul;
                //}

                var scoreSaber = IPA.Loader.PluginManager.GetPluginFromId("ScoreSaber");

                if (scoreSaber != null)
                {
                    ScoreSaberInterop.InitAndSignIn();
                }

                menuSceneSetupData.StartStandardLevel(difficultyBeatmap, environmentOverrideSettings, colorSchemesSettings, modifiers, playerSettings, practiceSettings: practiceSettings, "Lobby", false, () => { }, InGameOnlineController.Instance.SongFinished);
                //    menuSceneSetupData.StartStandardLevel(difficultyBeatmap, environmentOverrideSettings, colorSchemesSettings, GameplayModifiers.defaultModifiers, PlayerSpecificSettings.defaultSettings, PracticeSettings.defaultPracticeSettings, "Lobby",false, () => { }, null);
                //    UpdateUI(RoomState.Results);
            }
            else
            {
                Plugin.log.Error("SceneSetupData is null!");
            }
        }

        public void PopAllViewControllers()
        {
            if (childFlowCoordinator != null)
            {
                if (childFlowCoordinator is IDismissable)
                    (childFlowCoordinator as IDismissable).Dismiss(true);
                else
                    DismissFlowCoordinator(childFlowCoordinator, null, true);
            }
            if (_roomLeaveDialog.isInViewControllerHierarchy && !_roomLeaveDialog.GetPrivateField<bool>("_isInTransition"))
                DismissViewController(_roomLeaveDialog, null, true);
            if (_passHostDialog.isInViewControllerHierarchy && !_passHostDialog.GetPrivateField<bool>("_isInTransition"))
                DismissViewController(_passHostDialog, null, true);

            if (_roomNavigationController.viewControllers.Contains(_resultsViewController))
                HideResultsLeaderboard();
            if (_roomNavigationController.viewControllers.Contains(_playingNowViewController))
                HideInGameLeaderboard();
            if (_roomNavigationController.viewControllers.Contains(_difficultySelectionViewController))
            {
                HideDifficultySelection();
            }
            if (_roomNavigationController.viewControllers.Contains(_requestsViewController))
                HideRequestsList();
            if (_roomNavigationController.viewControllers.Contains(_songSelectionViewController))
                HideSongsList();

        }



        public void ShowSongsList()
        {
            if (_songSelectionViewController == null)
            {
                _songSelectionViewController = BeatSaberUI.CreateViewController<SongSelectionViewControllerXcopy>();
                _songSelectionViewController.ParentFlowCoordinator = this;
                _songSelectionViewController.SongSelected += SongSelected;
                _songSelectionViewController.RequestModePressed += RequestModePressed;
                _songSelectionViewController.RequestSongPressed += RequestSongPressed;
                _songSelectionViewController.SortPressed += (sortMode) => { SetSongs(LastSelectedCollection, sortMode, LastSearchRequest); };
                _songSelectionViewController.SearchPressed += (value) => { SetSongs(LastSelectedCollection, LastSortMode, value); };
                _songSelectionViewController.PlayerRequestsPressed += () => { ShowRequestsList(); };

                Plugin.log.Debug($"song selection view controller initialized");
            }
            if (_levelPacksViewController == null)
            {

                _levelPacksViewController = BeatSaberUI.CreateViewController<LevelPacksUIViewController>();
                _levelPacksViewController.packSelected += (IAnnotatedBeatmapLevelCollection pack) =>
                {
                    _highScoresViewController.UpdateLeaderboard(Season.getSeason(pack.collectionName));
                    _seasonInfoViewController.UpdateLeaderboard(Season.getSeason(pack.collectionName));
                    LastSelectedSeason = pack.collectionName;
                };
                _levelPacksViewController.packSelected += (IAnnotatedBeatmapLevelCollection pack) =>
                {

                    float scrollPosition = LastScrollPosition;
                    if (LastSelectedCollection != pack)
                    {
                        LastSelectedCollection = pack;
                        LastSortMode = SortMode.Default;
                        LastSearchRequest = "";
                        LastSelectedDifficulty = Season.getDifficulty(pack.collectionName);

                    }
                    SetSongs(LastSelectedCollection, LastSortMode, LastSearchRequest);
                    if (_songSelectionViewController.ScrollToPosition(scrollPosition))
                    {
#if DEBUG
                        Plugin.log.Debug($"Scrolling to {scrollPosition} / {_songSelectionViewController.SongListScroller.scrollableSize}");
#endif
                    }
                    else
                    {
                        Plugin.log.Debug($"Couldn't scroll to {scrollPosition}, max is {_songSelectionViewController.SongListScroller.scrollableSize}");
                        _songSelectionViewController.ScrollToLevel(LastSelectedSong);
                    }
                };
                _levelPacksViewController.packSelected += (IAnnotatedBeatmapLevelCollection pack) =>
                {

                    _seasonInfoViewController.showSeason(Season.getSeason(pack.collectionName));
                    //System.Random random = new System.Random();
                    //if(random.Next(0,10)==0)
                    //    SetRightScreenViewController(_seasonInfoViewController);
                    //else
                    //    SetRightScreenViewController(_highScoresViewController);
                    //_seasonInfoViewController = BeatSaberUI.CreateViewController<SeasonInfoViewController>();
                    //SetRightScreenViewController(_seasonInfoViewController,true);
                    //    DismissViewController(_seasonInfoViewController,
                    //        () =>
                    //        {

                    //            //_seasonInfoViewController = BeatSaberUI.CreateViewController<SeasonInfoViewController>();
                    //            //_seasonInfoViewController.showSeason(_levelPacksViewController.getSeason(pack.collectionName));

                    //        }

                    //        , true);
                };
            }

            if (_roomNavigationController.viewControllers.IndexOf(_songSelectionViewController) < 0)
            {
                float scrollPosition = LastScrollPosition;
                SetViewControllerToNavigationController(_roomNavigationController, _songSelectionViewController);
                SetSongs(LastSelectedCollection, LastSortMode, LastSearchRequest);
                if (_songSelectionViewController.ScrollToPosition(scrollPosition))
                {
#if DEBUG
                    Plugin.log.Debug($"Scrolled to {scrollPosition}");
#endif
                }
                else
                {
                    Plugin.log.Debug($"Couldn't scroll to {scrollPosition}, max is {_songSelectionViewController.SongListScroller.scrollableSize}");
                    if (!string.IsNullOrEmpty(LastSelectedSong))
                        _songSelectionViewController.ScrollToLevel(LastSelectedSong);
                }
            }


            if (true || _songSelectionViewController.requestMode)
            {
                _levelPacksViewController.gameObject.SetActive(true);
                SetBottomScreenViewController(_levelPacksViewController);
            }
            else
            {
                _levelPacksViewController.gameObject.SetActive(false);
                SetBottomScreenViewController(null);
            }


            _songSelectionViewController.UpdateViewController();
        }

        private void RequestModePressed()
        {
            _levelPacksViewController.gameObject.SetActive(true);
            SetBottomScreenViewController(_levelPacksViewController);
        }

        public void HideSongsList()
        {
            _roomNavigationController.ClearChildViewControllers();
            SetBottomScreenViewController(null);
        }

        public void SetSongs(IAnnotatedBeatmapLevelCollection selectedCollection, SortMode sortMode, string searchRequest)
        {
            LastSortMode = sortMode;
            LastSearchRequest = searchRequest;
            LastSelectedCollection = selectedCollection;

            List<IPreviewBeatmapLevel> levels = new List<IPreviewBeatmapLevel>();

            if (LastSelectedCollection != null)
            {
                levels = LastSelectedCollection.beatmapLevelCollection.beatmapLevels.ToList();

                if (string.IsNullOrEmpty(searchRequest))
                {
                    switch (sortMode)
                    {
                        case SortMode.Newest: { levels = SortLevelsByCreationTime(levels); }; break;
                        case SortMode.Difficulty:
                            {
                                levels = levels.AsParallel().OrderByDescending(x =>
                                {
                                    var diffs = ScrappedData.Songs.FirstOrDefault(y => x.levelID.Contains(y.Hash)).Diffs;
                                    if (diffs != null && diffs.Count > 0)
                                        return diffs.Max(y => y.Stars);
                                    else
                                        return -1;
                                }).ToList();
                            }; break;
                    }
                }
                else
                {
                    levels = levels.Where(x => $"{x.songName} {x.songSubName} {x.levelAuthorName} {x.songAuthorName}".ToLower().Contains(searchRequest)).ToList();
                }
            }
            if (levels == null)
                Plugin.log.Debug($"_songSelectionViewController null");
            else
                Plugin.log.Debug($"_songSelectionViewController null degil");
            _songSelectionViewController.SetSongs(levels);
        }

        public List<IPreviewBeatmapLevel> SortLevelsByCreationTime(List<IPreviewBeatmapLevel> levels)
        {
            DirectoryInfo customSongsFolder = new DirectoryInfo(CustomLevelPathHelper.customLevelsDirectoryPath);

            List<string> sortedFolders = customSongsFolder.GetDirectories().OrderByDescending(x => x.CreationTime.Ticks).Select(x => x.FullName).ToList();

            List<string> sortedLevelPaths = new List<string>();

            for (int i = 0; i < sortedFolders.Count; i++)
            {
                if (SongCore.Loader.CustomLevels.TryGetValue(sortedFolders[i], out var song))
                {
                    sortedLevelPaths.Add(song.customLevelPath);
                }
            }
            List<IPreviewBeatmapLevel> notSorted = new List<IPreviewBeatmapLevel>(levels);

            List<IPreviewBeatmapLevel> sortedLevels = new List<IPreviewBeatmapLevel>();

            for (int i2 = 0; i2 < sortedLevelPaths.Count; i2++)
            {
                IPreviewBeatmapLevel data = notSorted.FirstOrDefault(x =>
                {
                    if (x is CustomPreviewBeatmapLevel)
                        return (x as CustomPreviewBeatmapLevel).customLevelPath == sortedLevelPaths[i2];
                    else
                        return false;
                });
                if (data != null)
                {
                    sortedLevels.Add(data);
                }

            }
            sortedLevels.AddRange(notSorted.Except(sortedLevels));

            return sortedLevels;
        }

        private void SongSelected(IPreviewBeatmapLevel song)
        {
            LastSelectedSong = song.levelID;
            //Client.Instance.SetSelectedSong(new SongInfo(song));
            roomInfo.selectedSong = new SongInfo(song);
            //UpdateLevelOptions();
            UpdateUI(RoomState.Preparing);
        }

        private void RequestSongPressed(IPreviewBeatmapLevel song)
        {
            LastSelectedSong = song.levelID;
            Client.Instance.RequestSong(new SongInfo(song));
        }

        public void ShowRequestsList()
        {
            if (_requestsViewController == null)
            {
                _requestsViewController = BeatSaberUI.CreateViewController<RequestsViewController>();
                _requestsViewController.BackPressed += () => { ShowSongsList(); };
                _requestsViewController.SongSelected += (SongInfo song) => {
                    LastSelectedSong = song.levelId;
                    Client.Instance.SetSelectedSong(song);
                    Client.Instance.RemoveRequestedSong(song);
                    UpdateLevelOptions();
                };
                _requestsViewController.RemovePressed += (SongInfo info) => { Client.Instance.RemoveRequestedSong(info); };
            }

            SetViewControllerToNavigationController(_roomNavigationController, _requestsViewController);
            _requestsViewController.SetSongs(_requestedSongs);

        }

        public void HideRequestsList()
        {
            _roomNavigationController.ClearChildViewControllers();
        }

        public void ShowDifficultySelection(SongInfo song)
        {
            if (song == null)
                return;

            if (_difficultySelectionViewController == null)
            {
                Plugin.log.Debug("Difficulty Section Init");

                _difficultySelectionViewController = BeatSaberUI.CreateViewController<DifficultySelectionViewController>();
                _difficultySelectionViewController.discardPressed += DiscardPressed;
                _difficultySelectionViewController.difficulty = LastSelectedDifficulty;
                _difficultySelectionViewController.playPressed += (level, characteristic, difficulty) => {
                    Plugin.log.Debug("Play pressed");
                    PlayPressed(level, characteristic, difficulty, GameplayModifiers.defaultModifiers);
            
                };
                _difficultySelectionViewController.levelOptionsChanged += UpdateLevelOptions;
            
            }

            Plugin.log.Debug("Difficulty Section Init");
            try
            {
                if (!_roomNavigationController.viewControllers.Contains(_difficultySelectionViewController) && childFlowCoordinator == null)
                {
                    SetViewControllerToNavigationController(_roomNavigationController, _difficultySelectionViewController);
                }

                _difficultySelectionViewController.UpdateViewController(true, roomInfo.perPlayerDifficulty);

                IPreviewBeatmapLevel selectedLevel = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID == song.levelId);
                if (selectedLevel != null)
                {
                    _difficultySelectionViewController.playButton.interactable = false;
                    _difficultySelectionViewController.SetLoadingState(true);

                    LoadBeatmapLevelAsync(selectedLevel,
                        (status, success, level) =>
                        {
                            if (status == AdditionalContentModel.EntitlementStatus.NotOwned)
                            {

                                _difficultySelectionViewController.SetSelectedSong(selectedLevel);

                                selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).ContinueWith(
                                     (res) =>
                                     {
                                         if (!res.IsFaulted)
                                         {
                                             PreviewPlayer.CrossfadeTo(res.Result, selectedLevel.previewStartTime, res.Result.length - selectedLevel.previewStartTime);
                                         }
                                     });
                                Plugin.log.Debug("Song not owned");

                            }
                            else if (success)
                            {

                                Plugin.log.Debug("Song owned");
                                _difficultySelectionViewController.SetSelectedSong(level);

                                if (level.beatmapLevelData.audioClip != null)
                                {

                                    Plugin.log.Debug("Song audio not null");
                                    PreviewPlayer.CrossfadeTo(level.beatmapLevelData.audioClip, selectedLevel.previewStartTime, level.beatmapLevelData.audioClip.length - selectedLevel.previewStartTime, 1f);
                                    _difficultySelectionViewController.playButton.interactable = true;
                                //Client.Instance.SendPlayerReady(true);
                            }
                                else
                                {

                                    Plugin.log.Debug("Song audio null");
                                    _difficultySelectionViewController.playButton.interactable = false;
                                //Client.Instance.SendPlayerReady(false);
                            }

                            //Client.Instance.playerInfo.updateInfo.playerState = PlayerState.Room;

                        }
                            else
                            {

                                Plugin.log.Debug("Song getted level ");
                                _difficultySelectionViewController.SetSelectedSong(song);
                                _difficultySelectionViewController.playButton.interactable = false;
                            //Client.Instance.SendPlayerReady(false);
                            //Client.Instance.playerInfo.updateInfo.playerState = PlayerState.Room;
                        }
                        });

                    Plugin.log.Debug("Difficulty Section Init");
                }
                else
                {

                    Plugin.log.Debug("Song null");
                    _difficultySelectionViewController.SetSelectedSong(song);

                    _difficultySelectionViewController.playButton.interactable = false;

                }
            }
            catch(Exception ex)
            {
                Plugin.log.Critical(ex.StackTrace);
        
                }
            Plugin.log.Debug("Difficulty Section Init");
        
        }

        public void HideDifficultySelection()
        {
            _roomNavigationController.ClearChildViewControllers();
        }

        private async void LoadBeatmapLevelAsync(IPreviewBeatmapLevel selectedLevel, Action<AdditionalContentModel.EntitlementStatus, bool, IBeatmapLevel> callback)
        {
            var token = new CancellationTokenSource();

            var entitlementStatus = await _contentModelSO.GetLevelEntitlementStatusAsync(selectedLevel.levelID, token.Token);

            if (entitlementStatus == AdditionalContentModel.EntitlementStatus.Owned)
            {
                BeatmapLevelsModel.GetBeatmapLevelResult getBeatmapLevelResult = await _beatmapLevelsModel.GetBeatmapLevelAsync(selectedLevel.levelID, token.Token);

                callback?.Invoke(entitlementStatus, !getBeatmapLevelResult.isError, getBeatmapLevelResult.beatmapLevel);
            }
            else
            {
                callback?.Invoke(entitlementStatus, false, null);
            }
        }

        private void PlayPressed(IPreviewBeatmapLevel song, BeatmapCharacteristicSO characteristic, BeatmapDifficulty difficulty, GameplayModifiers modifiers)
        {
            IPreviewBeatmapLevel level = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID.StartsWith(roomInfo.selectedSong.levelId));
            if (level == null)
            {
                Plugin.log.Error("Unable to start level! Level is null! LevelID: " + roomInfo.selectedSong.levelId);
            }
            else
            {
                LoadBeatmapLevelAsync(level,
                    (status, success, beatmapLevel) =>
                    {
                        StartLevel(beatmapLevel, characteristic, LastSelectedDifficulty, GameplayModifiers.defaultModifiers);

                        //if (roomInfo.perPlayerDifficulty && _difficultySelectionViewController != null)
                        //{

                        //    StartLevel(beatmapLevel, characteristic, LastSelectedDifficulty, GameplayModifiers.defaultModifiers);
                        //}
                        //else
                        //{
                        //    StartLevel(beatmapLevel, characteristic, LastSelectedDifficulty, GameplayModifiers.defaultModifiers);
                        //}
                    });
            }
        }

        private void DiscardPressed()
        {
            //Client.Instance.SetSelectedSong(null);
            Plugin.log.Debug("cancel invoked");
            PreviewPlayer.CrossfadeToDefault();
            UpdateUI(RoomState.SelectingSong);
        }

        public void ShowResultsLeaderboard(SongInfo song)
        {
            if (_resultsViewController == null)
            {
                _resultsViewController = BeatSaberUI.CreateViewController<MultiplayerResultsViewController>();
            }
            if (_roomNavigationController.viewControllers.IndexOf(_resultsViewController) < 0)
            {
                SetViewControllerToNavigationController(_roomNavigationController, _resultsViewController);
            }

            IPreviewBeatmapLevel level = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID.StartsWith(song.levelId));

            if (level != null)
            {
                LoadBeatmapLevelAsync(level,
                    (status, success, beatmapLevel) =>
                    {
                        PreviewPlayer.CrossfadeTo(beatmapLevel.beatmapLevelData.audioClip, beatmapLevel.previewStartTime, beatmapLevel.beatmapLevelData.audioClip.length - beatmapLevel.previewStartTime, 1f);
                    });
            }

            _resultsViewController.UpdateLeaderboard();
            _resultsViewController.SetSong(song);
        }

        public void HideResultsLeaderboard()
        {
            _roomNavigationController.ClearChildViewControllers();

            Plugin.log.Debug("Resetting last level results");
            levelDifficultyBeatmap = null;
            levelResults = null;
            lastHighscoreForLevel = 0;
            lastHighscoreValid = false;

            PreviewPlayer.CrossfadeToDefault();
        }

        public void UpdateResultsLeaderboard(float currentTime, float totalTime)
        {
            if (_resultsViewController != null)
            {
                _resultsViewController.UpdateLeaderboard();
                _resultsViewController.SetTimer((int)(totalTime - currentTime));
            }
        }

        public void ShowInGameLeaderboard(SongInfo song)
        {
            roomInfo.roomState = RoomState.Results;
            if (_playingNowViewController == null)
            {
                _playingNowViewController = BeatSaberUI.CreateViewController<PlayingNowViewController>();
                _playingNowViewController.playNowPressed += PlayNow_Pressed;
                
            }
            if (_roomNavigationController.viewControllers.IndexOf(_playingNowViewController) < 0)
            {
                SetViewControllerToNavigationController(_roomNavigationController, _playingNowViewController);
            }

            _playingNowViewController.perPlayerDifficulty = roomInfo.perPlayerDifficulty;

            IPreviewBeatmapLevel selectedLevel = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID.StartsWith(song.levelId));

            if (selectedLevel != null)
            {
                _playingNowViewController.playNowButton.interactable = false;

                LoadBeatmapLevelAsync(selectedLevel,
                    (status, success, level) =>
                    {
                        if (status == AdditionalContentModel.EntitlementStatus.NotOwned)
                        {
                            _playingNowViewController.SetSong(selectedLevel);
                            Client.Instance.SendPlayerReady(false);
                            Client.Instance.playerInfo.updateInfo.playerState = PlayerState.DownloadingSongs;
                            Client.Instance.playerInfo.updateInfo.playerProgress = 0f;
                            selectedLevel.GetPreviewAudioClipAsync(new CancellationToken()).ContinueWith(
                                 (res) =>
                                 {
                                     if (!res.IsFaulted)
                                     {
                                         PreviewPlayer.CrossfadeTo(res.Result, selectedLevel.previewStartTime, res.Result.length - selectedLevel.previewStartTime);
                                     }
                                 });

                        }
                        else if (success)
                        {
                            BeatmapCharacteristicSO characteristic = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>().FirstOrDefault(x => x.serializedName == roomInfo.startLevelInfo.characteristicName);
                            PlayerDataModel playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().First();

                            _playingNowViewController.SetSong(level, characteristic, (roomInfo.perPlayerDifficulty ? playerData.playerData.lastSelectedBeatmapDifficulty : roomInfo.startLevelInfo.difficulty));

                            if (level.beatmapLevelData.audioClip != null)
                            {
                                PreviewPlayer.CrossfadeTo(level.beatmapLevelData.audioClip, selectedLevel.previewStartTime, level.beatmapLevelData.audioClip.length - selectedLevel.previewStartTime, 1f);
                                _playingNowViewController.playNowButton.interactable = true;
                                Client.Instance.SendPlayerReady(true);
                            }
                            else
                            {
                                _playingNowViewController.playNowButton.interactable = false;
                                Client.Instance.SendPlayerReady(false);
                            }

                            Client.Instance.playerInfo.updateInfo.playerState = PlayerState.Room;

                        }
                        else
                        {
                            _playingNowViewController.SetSong(song);
                            _playingNowViewController.playNowButton.interactable = false;
                            Client.Instance.SendPlayerReady(false);
                            Client.Instance.playerInfo.updateInfo.playerState = PlayerState.Room;
                        }
                    });
            }
            else
            {
                _playingNowViewController.SetSong(song);
                _playingNowViewController.playNowButton.interactable = true;
            }

            _playingNowViewController.UpdateLeaderboard();
        }





        private void PlayNow_Pressed()
        {
            SongInfo info = roomInfo.selectedSong;
            IPreviewBeatmapLevel level = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID == info.levelId);
            if (level == null)
            {
                _playingNowViewController.playNowButton.interactable = false;
                SongDownloader.Instance.RequestSongByLevelID(info.hash,
                (song) =>
                {
                    SongDownloader.Instance.DownloadSong(song,
                    (success) =>
                    {
                        if (success)
                        {
                            SongCore.Loader.SongsLoadedEvent += PlayNow_SongsLoaded;
                            SongCore.Loader.Instance.RefreshSongs(false);
                        }
                        else
                        {
                            _playingNowViewController.SetProgressBarState(true, -1f);
                        }
                    },
                    (progress) =>
                    {
                        _playingNowViewController.SetProgressBarState(progress > 0f, progress);
                    });
                });
            }
            else
            {
                StartLevel(_playingNowViewController.selectedLevel, _playingNowViewController.selectedBeatmapCharacteristic, _playingNowViewController.selectedDifficulty, roomInfo.startLevelInfo.modifiers.ToGameplayModifiers(), 0);
            }
        }

        private void PlayNow_SongsLoaded(SongCore.Loader arg1, Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            SongCore.Loader.SongsLoadedEvent -= PlayNow_SongsLoaded;
            roomInfo.selectedSong.UpdateLevelId();

            IPreviewBeatmapLevel level = _beatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).FirstOrDefault(x => x.levelID == roomInfo.selectedSong.levelId);

            if (_playingNowViewController.isActivated && level != null)
            {
                _playingNowViewController.SetProgressBarState(true, 1f);

                LoadBeatmapLevelAsync(level,
                    (status, success, beatmapLevel) =>
                    {
                        if (success)
                        {
                            _playingNowViewController.playNowButton.interactable = true;
                            _playingNowViewController.SetProgressBarState(false, 0f);
                            BeatmapCharacteristicSO characteristic = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSO>().FirstOrDefault(x => x.serializedName == roomInfo.startLevelInfo.characteristicName);
                            PlayerDataModel playerData = Resources.FindObjectsOfTypeAll<PlayerDataModel>().First();

                            _playingNowViewController.SetSong(beatmapLevel, characteristic, (roomInfo.perPlayerDifficulty ? playerData.playerData.lastSelectedBeatmapDifficulty : roomInfo.startLevelInfo.difficulty));
                        }
                    });
            }
        }

        public void DisplayError(string error, bool hideSideScreens = true)
        {
            _roomNavigationController.DisplayError(error);
            if (hideSideScreens)
            {
                SetLeftScreenViewController(null);
                SetRightScreenViewController(null);
            }
        }

    
        private string GetActivityDetails(bool includeAuthorName)
        {
            if (roomInfo.roomState != RoomState.SelectingSong && roomInfo.songSelected)
            {
                string songSubName = string.Empty;
                if (!string.IsNullOrEmpty(roomInfo.selectedSong.songSubName))
                    songSubName = $" ({roomInfo.selectedSong.songSubName})";

                string songAuthorName = string.Empty;

                if (!string.IsNullOrEmpty(roomInfo.selectedSong.authorName))
                    songAuthorName = $"{roomInfo.selectedSong.authorName} - ";

                return $"{(includeAuthorName ? songAuthorName : "")}{roomInfo.selectedSong.songName}{songSubName} | {Client.Instance.playerInfo.updateInfo.playerLevelOptions.difficulty.ToString().Replace("Plus", "+")}";
            }
            else
                return "In room";
        }

        private string GetFancyCharacteristicName(string charName)
        {
            switch (charName)
            {
                case "360Degree": return "360 Degree";
                case "90Degree": return "90 Degree";
                case "Standard": return "Standard";
                case "OneSaber": return "One Saber";
                case "NoArrows": return "No Arrows";
            }
            return "Unknown";
        }

        private string GetCharacteristicIconID(string charName)
        {
            switch (charName)
            {
                case "360Degree": return "360degree";
                case "90Degree": return "90degree";
                case "Standard": return "multiplayer";
                case "OneSaber": return "one_saber";
                case "NoArrows": return "no_arrows";
            }
            return "empty";
        }

    }
}

