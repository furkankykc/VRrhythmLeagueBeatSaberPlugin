using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine.Networking;
using System.Reflection;
using System.Collections;
using System.Linq;

namespace VRrhythmLeague.LeagueAPI
{
    public class Season
    {
        public static BeatmapDifficulty getDifficulty(String seasonName)
        {
            String difficulty = LeagueRequestHandler.Instance.seasons.Where(x => x.name == seasonName).Select(x => x.get_difficulty).First().ToString();
            Plugin.log.Debug($"Selected pack's({seasonName}) difficulty is {difficulty}");
            switch (difficulty)
            {
                case "Easy": return BeatmapDifficulty.Easy;
                case "Hard": return BeatmapDifficulty.Hard;
                case "Normal": return BeatmapDifficulty.Normal;
                case "Expert": return BeatmapDifficulty.Expert;
                case "Expert Plus": return BeatmapDifficulty.ExpertPlus;
                default: return BeatmapDifficulty.Normal;
            }
        }
        public static Season getSeason(String seasonName)
        {
            Season season = LeagueRequestHandler.Instance.seasons.Where(x => x.name == seasonName).Select(x => x).First();
            return season;
        }
        //[{"name":"seasonal-experiment","is_season_started":true,"current_week":{"name":"9 week of 12 weeks","songs":[],"highscores":[]},"is_applied":true,"get_difficulty":"Hard"}]
        public Season(JObject jsonNode)
        {
            List<HighScore> highscores = new List<HighScore>();

            name = (string)jsonNode["name"];
            is_season_started = (bool)jsonNode["is_season_started"];
            current_week = new Week((JObject)jsonNode["current_week"]);
            is_applied = (string)jsonNode["is_applied"];
            get_difficulty = (string)jsonNode["get_difficulty"];
            coverURL = (string)jsonNode["cover_url"];
            description = (string)jsonNode["description"];
            finishingDate = (string)jsonNode["finishing_at"];
            season_rank = (string)jsonNode["season_rank"];
            season_score = (int)jsonNode["season_score"];
            foreach (JObject highscore in jsonNode["highscores"])
            {
                highscores.Add(new HighScore(highscore));
            }

            this.highscores = highscores;
        }
        public Season()
        {

        }

        public int season_score { get; }
        public string season_rank { get; }
        public List<HighScore> highscores { get; set; }

        public string name { get; set; }
        public string description { get; set; }
        public string finishingDate { get; set; }
        public Week current_week { get; set; }
        public bool is_season_started { get; set; }
        public string is_applied { get; set; }
        public string get_difficulty { get; set; }
        public string coverURL { get; set; }


    }
    public class Week
    {
        public Week(JObject jsonNode)
        {

            List<Song> songs = new List<Song>();
            List<HighScore> highscores = new List<HighScore>();
            songsPlayed = (int)jsonNode["songs_played"];
            name = (string)jsonNode["name"];
            user_rank = (int)jsonNode["user_rank"];
            user_score = (int)jsonNode["user_score"];
            foreach (JObject song in jsonNode["songs"])
            {
                songs.Add(new Song(song));
            }
            foreach (JObject highscore in jsonNode["highscores"])
            {
                highscores.Add(new HighScore(highscore));
            }

            this.songs = songs;
            this.highscores = highscores;

        }
        public Week()
        {

        }
        public int user_rank { get; }
        public int user_score{ get; }
        public string name { get; set; }
        public int songsPlayed {get;}

        public List<Song> songs { get; set; }
        public List<HighScore> highscores { get; set; }

    }
    public class Song
    {
        public Song()
        {

        }
        public Song(JObject jsonNode)
        {
            name = (string)jsonNode["name"];
            key = (string)jsonNode["key"];
            hash = (string)jsonNode["hash"];
            bpm = (string)jsonNode["bpm"];
        }
        public string key { get; set; }
        public string hash {get; set; }
        public string name { get; set; }
        public string bpm { get; set; }


    }
    public class HighScore
    {
        public HighScore(JObject jsonNode)
        {
            user = (string)jsonNode["user"];
            score = (int)jsonNode["score"];
        }
        public string user { get; set; }
        public int score { get; set; }
    }
    public class Score
    {
        public JObject toJson()
        {
            JObject o = JObject.FromObject(new
            {
                Score = new
                {
                    game = this.game,
                    score = this.score,
                    full_combo = this.full_combo,
                    song = this.song,
                }
            }); ; ;
            return o;
        }
        public int game { get; set; }
        public int score { get; set; }
        public bool full_combo { get; set; }
        public string song { get; set; }

        public Score(int score, string song, bool full_combo)
        {
            this.game = 1;
            this.score = score;
            this.full_combo = full_combo;
            this.song = song;

        }
    }
    public class Token
    {

        public Token(JObject jsonNode)
        {
            token = (string)jsonNode["token"];

        }
        public Token(String text)
        {
            JObject jsonNode = JObject.Parse(text);
            token = (string)jsonNode["token"];

        }
        public string token { get; set; }
    }
    public class Auth
    {
        public string steamid { get; set; }

        public Auth(string steamid)
        {
            this.steamid = steamid;
        }
    }

    public class LeagueAPI
    {
        public static List<Season> getSeasons(string text)
        {
            Plugin.log.Debug($"Raw json text : {text}");
            List<Season> seasons = new List<Season>();
            JArray jArray = JArray.Parse(text);
            foreach (JObject season in jArray)
            {
                seasons.Add(new Season(season));
            }
            return seasons;
        }

        public static void showSeasons(List<Season> list)
        {
            foreach (Season season in list)
                Plugin.log.Debug(@"name : " + season.name
                                + "is_season_started : " + season.is_season_started
                                + "is_applied : " + season.is_applied
                                + "songs :" + season

                    );
        }

        /*[{"name":"asdfg","is_season_started":true,"current_week":{"name":"9 week of 12 weeks","songs":[{"key":"30fc","name":"Salsa Tequila - Anders Nilsen","bpm":120.0},{"key":"5dc8","name":"Mary Had A Little Lamb","bpm":122.25},{"key":"6478","name":"I didn't get no sleep cause of y'all","bpm":100.0},{"key":"6e8f","name":"GTTPIC","bpm":150.0}],"highscores":[]},"is_applied":"/apply/50","get_difficulty":"Hard"}]*/

    }




}