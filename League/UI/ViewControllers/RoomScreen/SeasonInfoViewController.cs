using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using VRrhythmLeague.Data;
using VRrhythmLeague.LeagueAPI;
using VRrhythmLeague.Misc;
using BS_Utils.Utilities;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace VRrhythmLeague.UI.ViewControllers.RoomScreen
{
    public enum SeasonStatus
    {
        NotApplied,Applied, Started
    }

    class SeasonInfoViewController : BSMLResourceViewController
    {
        public override string ResourceName => "VRrhythmLeague.UI.ViewControllers.RoomScreen.SeasonInfoViewController.bsml";

        public event Action applyPressed;
        public event Action<Season> seasonChanged;

        SeasonStatus status;

        [UIComponent("apply-button")]
        public Button applyButton;

        [UIComponent("season-name")]
        public TextMeshProUGUI seasonName;

        [UIComponent("current-week")]
        public TextMeshProUGUI seasonCurrentWeek;

        [UIComponent("finishing-date")]
        public TextMeshProUGUI seasonFinishingDate;

        [UIComponent("season-desc")]
        public TextMeshProUGUI seasonDescription;

        [UIComponent("season-status")]
        public TextMeshProUGUI seasonStatus;

        [UIComponent("season-cover")]
        public RawImage seasonCover;

        [UIComponent("leaderboard-table")]
        public LeaderboardTableView leaderboardTableView;

        [UIComponent("league-bg-banner")]
        public Image leagueBanner;

        [UIComponent("apply-blocker")]
        public RawImage applyBlocker;


        [UIValue("players")]
        List<object> players = new List<object>();
        public string seasonIsAplied;

        private List<LeaderboardTableView.ScoreData> _scoreData = new List<LeaderboardTableView.ScoreData>();

        public void applyPost(SeasonStatus status)
        {
            this.status = status;
            switch (status)
            {
                case SeasonStatus.Applied:
                    { 
                    applyBlocker.gameObject.SetActive(true);
                    applyButton.SetButtonText("APPLIED");
                    }
                    break;
                case SeasonStatus.Started:
                    {
                        applyBlocker.gameObject.SetActive(true);
                        applyButton.SetButtonText("ALREADY STARTED");
                    }
                    break;
                case SeasonStatus.NotApplied:
                    {
                        applyBlocker.gameObject.SetActive(false);
                        applyButton.SetButtonText("APPLY");
                    }
                    break;

            }

        }
        [UIAction("#post-parse")]
        protected void SetupViewController()
        {
            leagueBanner.type = Image.Type.Sliced;
            leagueBanner.color = new Color(0f, 0f, 0f, 0.75f);
            applyBlocker.color = new Color(0f, 0f, 0f, 0.75f);
            applyBlocker.gameObject.SetActive(false);
            applyPressed += () =>
            {
                    LeagueRequestHandler.Instance.RequestApply(seasonIsAplied, (result)=>
                    {
                        if (result)
                            applyPost(SeasonStatus.Applied);
                        
                    });
            };
            
            //seasonChanged += showSeason;


            HoverHintController hoverHintController = Resources.FindObjectsOfTypeAll<HoverHintController>().First();



            leaderboardTableView.GetComponent<TableView>().RemoveReusableCells("Cell");

            //foreach (var item in modifierToggles)
            //{
            //    item.toggle.onValueChanged.AddListener((enabled) => { gameplayModifiersChanged?.Invoke(); });
            //}

        }

        public void showSeason(Season season)
        {
         
            try
            {
                seasonName.text = season.name;
                seasonDescription.text = season.description;
                seasonFinishingDate.text = season.finishingDate;
                seasonCurrentWeek.text = season.current_week.name;
                seasonIsAplied = season.is_applied;
                if (season.is_season_started)
                {
                    status = SeasonStatus.Started;
                }
                else
                {
                    status = SeasonStatus.NotApplied;
                }
                if (seasonIsAplied == "True")
                {
                    status = SeasonStatus.Applied;
                    applyPost(status);
                }
                else
                {
                    applyPost(status);
                }
                StartCoroutine(LoadScripts.LoadSpriteCoroutine($"{LeagueAPI.LeagueRequestHandler.leagueURL}{season.coverURL}", (cover) =>
                {
                    seasonCover.texture = cover;
                }));
            }
            catch (Exception ex) 
            {
                Plugin.log.Debug($" showing on season info  {season.name} exception: {ex.Message}");
            }
          
            
        }

        //public void Update()
        //{
        //    if (Time.frameCount % 45 == 0 && seasonName != null && true && true)
        //        seasonName.text = "PING: " + 0.ToString();
        //}

        public void UpdateViewController(bool isHost, bool modifiersInteractable)
        {
            if(leagueBanner != null)
                leagueBanner.gameObject.SetActive(true);
        }

        public void UpdateLeaderboard(Season season)
        {
            //var scores = InGameOnlineController.Instance.playerScores;
            List<PlayerScore> scores = new List<PlayerScore>();

            foreach (HighScore highscore in season.highscores)
            {
                PlayerScore playerScore = new PlayerScore();

                playerScore.score = (uint)highscore.score;
                playerScore.name = highscore.user;
                scores.Add(playerScore);
            }

            if (scores == null)
                return;

            scores.Sort();

            if (scores.Count < _scoreData.Count)
            {
                _scoreData.RemoveRange(scores.Count, _scoreData.Count - scores.Count);
            }

            for (int i = 0; i < scores.Count; i++)
            {
                if (_scoreData.Count <= i)
                {
                    _scoreData.Add(new LeaderboardTableView.ScoreData((int)scores[i].score, scores[i].name, i + 1, false));
                    Plugin.log.Debug("Added new ScoreData!");
                }
                else
                {
                    _scoreData[i].SetProperty("playerName", scores[i].name);
                    _scoreData[i].SetProperty("score", (int)scores[i].score);
                    _scoreData[i].SetProperty("rank", i + 1);
                }
            }

            leaderboardTableView.SetScores(_scoreData, -1);

        }



      

        [UIAction("apply-pressed")]
        public void ApplyPressed()
        {
            applyPressed?.Invoke();
        }


        public class PlayerListObject
        {
            [UIComponent("speaker-icon")]
            public Image speakerIcon;

            [UIComponent("control-buttons")]
            public RectTransform controlButtonsRect;

            [UIComponent("pass-host-button")]
            public Button passHostButton;

            [UIComponent("mute-button")]
            public Button muteButton;

            [UIComponent("progress-text")]
            public TextMeshProUGUI progressText;

            [UIComponent("player-name")]
            public TextMeshProUGUI playerName;

            public PlayerInfo playerInfo;

            private IPlayerManagementButtons _buttonsInterface;
            private bool _isMuted;

            private bool _isInitialized;

            //public PlayerListObject(PlayerInfo info, IPlayerManagementButtons buttons)
            //{
            //    playerInfo = info;
            //    _buttonsInterface = buttons;
            //}


            [UIAction("refresh-visuals")]
            public void Refresh(bool selected, bool highlighted)
            {
                if (playerInfo != null)
                {
                    playerName.text = playerInfo.playerName;
                    playerName.color = playerInfo.updateInfo.playerNameColor;

                    passHostButton.onClick.RemoveAllListeners();
                    passHostButton.onClick.AddListener(() => _buttonsInterface.TransferHostButtonWasPressed(playerInfo));
                    muteButton.onClick.RemoveAllListeners();
                    muteButton.onClick.AddListener(() => _buttonsInterface.MuteButtonWasPressed(playerInfo));
                }
                _isInitialized = true;
            }


            public void Update(PlayerInfo info, RoomState state)
            {
                if (!_isInitialized)
                    return;

                if (info.playerId != playerInfo?.playerId)
                {
                    playerInfo = info;
                    Refresh(false, false);
                }
                else
                {
                    playerInfo.updateInfo = info.updateInfo;
                }

                playerName.color = playerInfo.updateInfo.playerNameColor;

                speakerIcon.enabled = InGameOnlineController.Instance.VoiceChatIsTalking(playerInfo.playerId);

                controlButtonsRect.gameObject.SetActive(( state == RoomState.SelectingSong || state == RoomState.Results));
                passHostButton.interactable = true;

                if (_isMuted && !InGameOnlineController.Instance.mutedPlayers.Contains(playerInfo.playerId))
                {
                    _isMuted = false;
                    muteButton.SetButtonText("MUTE");
                }
                else if (!_isMuted && InGameOnlineController.Instance.mutedPlayers.Contains(playerInfo.playerId))
                {
                    _isMuted = true;
                    muteButton.SetButtonText("UNMUTE");
                }

                progressText.gameObject.SetActive(state == RoomState.Preparing);

                if(playerInfo.updateInfo.playerProgress < 0f)
                {
                    progressText.text = "ERROR";
                }
                else if (playerInfo.updateInfo.playerState == PlayerState.DownloadingSongs && playerInfo.updateInfo.playerProgress < 100f)
                {
                    progressText.text = (playerInfo.updateInfo.playerProgress / 100f).ToString("P");
                }
                else
                {
                    progressText.text = "DOWNLOADED";
                }
            }
        }
    }


}
