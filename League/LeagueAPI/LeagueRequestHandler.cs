
using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Steamworks;
using VRrhythmLeague.Misc;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using VRrhythmLeague.UI.ViewControllers.RoomScreen;
using BeatSaberMarkupLanguage;
using VRrhythmLeague.SimpleJSON;
using Newtonsoft.Json.Linq;
using VRrhythmLeague.UI;

namespace VRrhythmLeague.LeagueAPI
{
    public enum PluginState
    {
        UpdateRequired=0,
        LatestVersion =1
    }
    public enum UserState
    {

        NotConnected = 0,
        NotRegistered = 1,
        DownloadingSongs = 2,
        Registered = 3,

    }
    class LeagueRequestHandler : MonoBehaviour
    {
        private static Token token;
        public event Action downloadCompleted;
        public event Action playlistsChanged;
        public Dictionary<string, string> songDict = new Dictionary<string, string>();
        public List<Season> seasons;
        public PluginState pluginState = PluginState.LatestVersion;
        public UserState userState = 0;
        List<String> downloading_songs = new List<String>();
        public int loadedSongCount = 0;
        List<CustomPlaylistSO> playlists;
        public bool isSeasonSongsReady=false;
        private static LeagueRequestHandler _instance = null;
        public Action scoreUploaded;
        public static LeagueRequestHandler Instance
        {
            get
            {
                if (!_instance)
                { _instance = new GameObject("LeagueRequestHandler").AddComponent<LeagueRequestHandler>();

                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public static UnityWebRequest GetRequestForUrl(string url)
        {


            Plugin.log.Debug($"Steam id  = {SteamUser.GetSteamID()}");
            Plugin.log.Debug($"Authorization TOKEN {token.token}");

            UnityWebRequest www = UnityWebRequest.Get(url);
            www.SetRequestHeader("User-Agent", UserAgent);
            www.SetRequestHeader("Authorization", "TOKEN " + token.token);
            www.certificateHandler = new CustomCertificateHandler();

            return www;

        }
        public void CheckPluginVersion()
        {

        }
        public static string UserAgent { get; } = $"{Assembly.GetExecutingAssembly().GetName().Name}/{Assembly.GetExecutingAssembly().GetName().Version}";

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

        }
        public void RequestLeagueData(string key, Action callback)
        {
           
            if (token==null)
            {
              
                Plugin.log.Debug(@"Retrieving league data");
                //{ SteamUser.GetSteamID()}
                string logindata = $"{{\"steamid\":\"{SteamUser.GetSteamID()}\"}}";
                StartCoroutine(CallLogin($"{leagueURL}/api/api-token-auth/", logindata, ()=>StartCoroutine(RequestDataByKeyCoroutine(key, callback))));
            }
            else
            {
                Plugin.log.Debug(@"https://vrrhythmleague.com/api/api-token-auth/");

                StartCoroutine(RequestDataByKeyCoroutine(key, callback));
            }
        }
        public void RequestApply(string key, Action<bool> callback)
        {
            if (token == null)
            {

                //{ SteamUser.GetSteamID()}
                string logindata = $"{{\"steamid\":\"76561198103998111\"}}";
                StartCoroutine(CallLogin($"{leagueURL}/api/api-token-auth/", logindata, ()=> StartCoroutine(RequestApplySeasonCoroutine(key, callback))));
            }
            else
            {

                StartCoroutine(RequestApplySeasonCoroutine(key, callback));
            }
        }
        public static String leagueURL = "https://vrrhythmleague.com";
        public static String debugURL = "http://192.168.1.12:8000";
        public IEnumerator CheckVersion(Action callback)
        {

            Plugin.log.Info("Checking for updates...");

            UnityWebRequest www = SongDownloader.GetRequestForUrl($"https://api.github.com/repos/andruzzzhka/VRrhythmLeague/releases");
            www.timeout = 10;

            yield return www.SendWebRequest();

            if (!www.isNetworkError && !www.isHttpError)
            {
                JSONNode releases = JSON.Parse(www.downloadHandler.text);

                JSONNode latestRelease = releases[0];

                SemVer.Version currentVer = IPA.Loader.PluginManager.GetPlugin("Beat Saber Multiplayer").Version;
                SemVer.Version githubVer = new SemVer.Version(latestRelease["tag_name"], true);

                bool newTag = new SemVer.Range($">{currentVer}").IsSatisfied(githubVer);

                if (newTag)
                {
                    Plugin.log.Info($"An update for the mod is available!\nNew mod version: {(string)latestRelease["tag_name"]}\nCurrent mod version: {currentVer}");
                    pluginState = PluginState.LatestVersion;
                    callback?.Invoke();
                    //_newVersionText.gameObject.SetActive(true);
                    //_newVersionText.text = $"Version {(string)latestRelease["tag_name"]}\n of the mod is available!\nCurrent mod version: {currentVer}";
                    //_newVersionText.alignment = TextAlignmentOptions.Center;
                }
                else
                {
                    pluginState = PluginState.UpdateRequired;
                    callback?.Invoke();
                }
            }
            else
            {
                pluginState = PluginState.UpdateRequired;
                callback?.Invoke();
            }
        }
        public IEnumerator RequestDataByKeyCoroutine(string key, Action callback)
        {
            UnityWebRequest wwwId = GetRequestForUrl($"{leagueURL}/api/seasons/");
            Plugin.log.Debug($"{leagueURL}/api/seasons/");
            wwwId.timeout = 10;

            yield return wwwId.SendWebRequest();

            Plugin.log.Debug("Requesting data by key coroutine");
            if (wwwId.isNetworkError || wwwId.isHttpError)
            {
                Plugin.log.Error(wwwId.error);
                userState = UserState.NotRegistered;
            }
            else
            {
                Plugin.log.Debug($"There are no reason to start league {!isSeasonSongsReady}");

                seasons = LeagueAPI.getSeasons(wwwId.downloadHandler.text);
                callback?.Invoke();
                if (!isSeasonSongsReady)
                {
                    //download season songs
                    Plugin.log.Debug("Creating league songs");
                    createLeaguePacks(this.seasons);
                }
                else
                {

                    userState = UserState.Registered;
                }
            }
        }
        public IEnumerator RequestApplySeasonCoroutine(string applyLink, Action<bool> callback)
        {
            UnityWebRequest wwwId = GetRequestForUrl($"{leagueURL}/api{applyLink}");
            wwwId.timeout = 10;

            yield return wwwId.SendWebRequest();


            if (wwwId.isNetworkError || wwwId.isHttpError)
            {
                Plugin.log.Error(wwwId.error);
                Plugin.log.Debug($"{leagueURL}{applyLink}");

                callback?.Invoke(false);
            }
            else
            {

                bool result= (bool) JObject.Parse(wwwId.downloadHandler.text)["result"];/// json response result

                Plugin.log.Debug((string) JObject.Parse(wwwId.downloadHandler.text)["result"]+" result burada "+ result);
                callback?.Invoke(result);

            }
        }
        private void loadSeasons()
        {
            RequestLeagueData("",null);
        }
        private void loadSeasonsStatic()
        {
            List<Season> seasonsList = new List<Season>();
            Season season = new Season();
            Week week = new Week();
            List<Song> songs = new List<Song>();

            Song song = new Song();
            song.hash = "483c7bc03133c6e215f3018e5033b0913821126f";
            songs.Add(song);
            week.songs = songs;
           
            season.name = "Title";
            season.current_week = week;
            seasonsList.Add(season);
            seasons = seasonsList;
        }

        private void loadSeasonsFromJson()
        {
            String json = "[{\"name\":\"test\",\"is_season_started\":true,\"current_week\":{\"name\":\"9 week of 12 weeks\",\"songs\":[],\"highscores\":[]},\"is_applied\":\" / apply / 49\",\"get_difficulty\":\"Expert Plus\",\"cover_url\":\" /static/ league / img / vrom.jpg\"},{\"name\":\"asdfg\",\"is_season_started\":true,\"current_week\":{\"name\":\"9 week of 12 weeks\",\"songs\":[{\"key\":\"30fc\",\"hash\":\"9f4c2bde9629dd004337588ca83b8a295207d3f5\",\"name\":\"Salsa Tequila -Anders Nilsen\",\"bpm\":120.0},{\"key\":\"5dc8\",\"hash\":\"b4dd5117d977a7ca06f2d4ab58cf396a6ae7c3e2\",\"name\":\"Mary Had A Little Lamb\",\"bpm\":122.25},{\"key\":\"6478\",\"hash\":\"285b7fc4016d0eb45541091510d3548540faf692\",\"name\":\"I didn't get no sleep cause of y'all\",\"bpm\":100.0},{\"key\":\"6e8f\",\"hash\":\"dd2fd0b1ddf5424fb01f16e51ffe4a250398950a\",\"name\":\"GTTPIC\",\"bpm\":150.0}],\"highscores\":[]},\"is_applied\":\" / apply / 50\",\"get_difficulty\":\"Hard\",\"cover_url\":\" /static/ league / img / vrom.jpg\"},{\"name\":\"sdfghn\",\"is_season_started\":true,\"current_week\":{\"name\":\"9 week of 12 weeks\",\"songs\":[],\"highscores\":[]},\"is_applied\":\" / apply / 51\",\"get_difficulty\":\"Easy\",\"cover_url\":\" / media / season_pics / 2020 / 03 / 14 / Nvidia - Geforce - GTX - Logo.jpg\"},{\"name\":\"BeatSaber Alpha\",\"is_season_started\":true,\"current_week\":{\"name\":\"23 day of 23 days\",\"songs\":[],\"highscores\":[]},\"is_applied\":\" / apply / 52\",\"get_difficulty\":\"Expert Plus\",\"cover_url\":\" / media / season_pics / 2020 / 03 / 14 / 112314.jpg\"}]";
            seasons = LeagueAPI.getSeasons(json);
        }
        private void loadPacks()
        {
            if (seasons == null)
                loadSeasons();
                //loadSeasonsStatic();
                //loadSeasonsFromJson();
            createLeaguePacks(this.seasons);
        }
        public CustomPlaylistSO[] getPlaylist()
        {
            if(playlists == null)
            {
                loadPacks();
            }

            return returnPacks();
        }



        public void applyUILeaguePacks(IAnnotatedBeatmapLevelCollection[] leaguePacks)
        {
            LevelPacksUIViewController _levelPacksViewController = BeatSaberUI.CreateViewController<LevelPacksUIViewController>();
            _levelPacksViewController.SetPacks(leaguePacks);

        }

        public IEnumerator CallLogin(string url, string logindataJsonString,Action callback)
        {
            Plugin.log.Debug(@"Called Function name CallLogin");

            var request = UnityWebRequest.Post(url, logindataJsonString);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(logindataJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new CustomCertificateHandler();
            yield return request.SendWebRequest();
            Plugin.log.Debug($"Login error Error: { request.isNetworkError || request.isHttpError}");
            if (request.isNetworkError || request.isHttpError)
            {
                Plugin.log.Debug(@"CallLogin: Network error");

                userState = UserState.NotConnected;
                
            }
            else
            {

                Plugin.log.Debug("All OK");
                Plugin.log.Debug("Status Code: " + request.responseCode);
                Plugin.log.Debug("Token Auth: " + request.downloadHandler.text);
                if (request.downloadHandler.text == "UserDoesNotExists")
                    userState = UserState.NotRegistered;
                else
                {
                    
                    token = new Token(request.downloadHandler.text);
                    //StartCoroutine(RequestDataByKeyCoroutine("", null));
                    callback?.Invoke();
                }
            }

        }
        public IEnumerator UploadDataCoroutine(string url, string scoreDataJsonString, Action callback)
        {
            Plugin.log.Debug(@"Called Function name Upload Data");

            var request = UnityWebRequest.Post(url, scoreDataJsonString);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(scoreDataJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new CustomCertificateHandler();

            request.SetRequestHeader("User-Agent", UserAgent);
            request.SetRequestHeader("Authorization", "TOKEN " + token.token);
            yield return request.SendWebRequest();
            Plugin.log.Debug($"Login error Error: { request.isNetworkError || request.isHttpError}");
            if (request.isNetworkError || request.isHttpError)
            {
                Plugin.log.Debug(@"Upload Data: Network error");




            }
            else
            {

                Plugin.log.Debug("All OK");
                Plugin.log.Debug("Status Code: " + request.responseCode);
                Plugin.log.Debug("Response" + request.downloadHandler.text);
                //token = new Token(request.downloadHandler.text);
                callback?.Invoke();
            }

        }
        private static IPreviewBeatmapLevel MatchSong(String hash)
        {
            if (!SongCore.Loader.AreSongsLoaded || SongCore.Loader.AreSongsLoading)
            {
                Plugin.log.Info("Songs not loaded. Not Matching songs for playlist.");
                return null;
            }
            IPreviewBeatmapLevel x = null;
            try
            {
                if (!string.IsNullOrEmpty(hash))
                    x = SongCore.Loader.CustomLevels.Values.FirstOrDefault(y => string.Equals(y.levelID.Split('_')[2], hash, StringComparison.OrdinalIgnoreCase));

            }
            catch (Exception)
            {
                Plugin.log.Warn($"Unable to match song with {(string.IsNullOrEmpty(hash) ? " unknown hash!" : ("hash " + hash + " !"))}");
            }
            return x;
        }

        public CustomPlaylistSO[] returnPacks()
        {
            Plugin.log.Debug($"Playlist count : {playlists.Count()}");
            return playlists.ToArray();
        }

        public void downloadFinishedForSong(String hash) {
            //todo sıkıntı var
            if (!String.IsNullOrEmpty(hash))
            {

                downloading_songs.Remove(hash);
                loadedSongCount++;
            }
            Plugin.log.Debug($"{downloading_songs.Count} songs count");
            if (downloading_songs.Count == 0)
            {
                //burda yeniden olustur playlistleri
                userState = UserState.Registered;
                Plugin.log.Debug("Download finished for song invokin downloaded");

                downloadCompleted?.Invoke();
            }

        }
        public  void createLeaguePacks(List<Season> seasons)
        {

            bool thisHasSongs = false;
            //levels = LastSelectedCollection.beatmapLevelCollection.beatmapLevels.ToList();
            playlists = new List<CustomPlaylistSO>();
            userState = UserState.DownloadingSongs;
            foreach (Season season in seasons)
            {
                List<IPreviewBeatmapLevel> beatmapLevels = new List<IPreviewBeatmapLevel>();
                foreach (Song song in season.current_week.songs)
                {
                    if (!songDict.ContainsKey(song.hash.ToUpper()))
                        songDict.Add(song.hash.ToUpper(), song.key);
                    thisHasSongs = true;
                    IPreviewBeatmapLevel beatmapLevel = null;
                    String hash = song.hash;
                    beatmapLevel = MatchSong(hash);
                    if (beatmapLevel != null)
                    {

                        beatmapLevels.Add(beatmapLevel);
                        loadedSongCount++;
                    }
                    else
                    {

                        userState = UserState.DownloadingSongs;
                        downloading_songs.Add(hash);
                        Misc.Song songToDownload;
                        SongDownloader.Instance.RequestSongByLevelID(hash, (info) =>
                        {

                            songToDownload = info;

                            SongDownloader.Instance.DownloadSong(songToDownload,
                                (success) =>
                                {
                                    if (success)
                                    {
                                        void onLoaded(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> songs)
                                        {
                                            SongCore.Loader.SongsLoadedEvent -= onLoaded;
                                            Plugin.log.Debug($"Sarkı indirildi");
                                            beatmapLevel = MatchSong(hash);
                                            beatmapLevels.Add(beatmapLevel);
                                            downloadFinishedForSong(hash);
                                        }

                                        SongCore.Loader.SongsLoadedEvent += onLoaded;

                                        SongCore.Loader.Instance.RefreshSongs(true);
                                    }
                                    else
                                    {
                                        Plugin.log.Error($"Unable to download song! An error occurred");

                                    }
                                    songToDownload = null;
                                },
                            (progress) =>
                            {
                                float clampedProgress = Math.Min(progress, 0.99f);
                                //Client.Instance.playerInfo.updateInfo.playerProgress = 100f * clampedProgress;
                            });
                        });

                    }
                }

                CustomBeatmapLevelCollectionSO customBeatmapLevelCollection = CustomBeatmapLevelCollectionSO.CreateInstance(beatmapLevels.ToArray());
                String playlistTitle = season.name;
                String playlistImage = CustomPlaylistSO.DEFAULT_IMAGE;
                Texture2D coverImage = null;
                Sprite online = Sprites.onlineIcon;
                Plugin.log.Debug(songDict.ToString());
   
                    Plugin.log.Debug($"Else for {season.name} : {leagueURL}{season.coverURL}");
               //if (customBeatmapLevelCollection.beatmapLevels.Length != 0)
                StartCoroutine(LoadScripts.LoadSpriteCoroutine($"{leagueURL}{season.coverURL}", (cover) =>
                {


                    Plugin.log.Debug($"Playlist changed count:{playlists.Count()}");
                    Sprite spriteCover = Sprite.Create(cover, new Rect(0.0f, 0.0f, cover.width, cover.height), new Vector2(1f, 1f), 100.0f);
                    playlists.Add(CustomPlaylistSO.CreateInstance(playlistTitle, spriteCover, customBeatmapLevelCollection));
                    playlistsChanged?.Invoke();

                }));

            }
            downloadFinishedForSong("");
            isSeasonSongsReady = true;
        }

        internal void UploadScore(IDifficultyBeatmap difficultyBeatmap, int rawScore, int modifiedScore, bool fullCombo, int goodCutsCount, int badCutsCount, int missedCount, int maxCombo, GameplayModifiers gameplayModifiers)
        {
            if(songDict.ContainsKey(difficultyBeatmap.level.levelID.Split('_')[2].ToUpper()))
                Plugin.log.Debug($"songDict:{songDict[difficultyBeatmap.level.levelID.Split('_')[2].ToUpper()]}");
            Score score = new Score(modifiedScore, songDict[difficultyBeatmap.level.levelID.Split('_')[2].ToUpper()], fullCombo);
            string scoredata = score.toJson()["Score"].ToString();
            Plugin.log.Debug($"score json: {scoredata}");
            if (token == null)
            {

                Plugin.log.Debug(@"Retrieving league data");
                //{ SteamUser.GetSteamID()}

                StartCoroutine(UploadDataCoroutine($"{leagueURL}/api/scores/", scoredata, () => StartCoroutine(RequestDataByKeyCoroutine("", null))));
            }
            else
            {
                Plugin.log.Debug(@"https://vrrhythmleague.com/api/api-token-auth/");

                StartCoroutine(UploadDataCoroutine($"{leagueURL}/api/scores/", scoredata, () => StartCoroutine(RequestDataByKeyCoroutine("", scoreUploaded))));
            }
        }
    }
}
