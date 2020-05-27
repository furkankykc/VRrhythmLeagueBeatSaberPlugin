﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace VRrhythmLeague.Misc
{
    public class ScrappedData : MonoBehaviour
    {
        private static ScrappedData _instance = null;
        public static ScrappedData Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("ScrappedData").AddComponent<ScrappedData>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public static List<ScrappedSong> Songs = new List<ScrappedSong>();
        public static bool Downloaded;

        public static string scrappedDataURL = "https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/combinedScrappedData.json";

        public void DownloadScrappedData(Action<List<ScrappedSong>> callback)
        {
            StartCoroutine(DownloadScrappedDataCoroutine(callback));
        }

        public IEnumerator DownloadScrappedDataCoroutine(Action<List<ScrappedSong>> callback)
        {
            Plugin.log.Info("Downloading scrapped data...");

            UnityWebRequest www;
            bool timeout = false;
            float time = 0f;
            UnityWebRequestAsyncOperation asyncRequest;

            try
            {
                www = SongDownloader.GetRequestForUrl(scrappedDataURL);

                asyncRequest = www.SendWebRequest();
            }
            catch (Exception e)
            {
                Plugin.log.Error(e);
                yield break;
            }

            while (!asyncRequest.isDone)
            {
                yield return null;
                time += Time.deltaTime;
                if (time >= 5f && asyncRequest.progress <= float.Epsilon)
                {
                    www.Abort();
                    timeout = true;
                    Plugin.log.Error("Connection timed out!");
                }
            }


            if (www.isNetworkError || www.isHttpError || timeout)
            {
                Plugin.log.Error("Unable to download scrapped data! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                Plugin.log.Info("Received response from github.com!");

                Task parsing = new Task( () => { Songs = JsonConvert.DeserializeObject<List<ScrappedSong>>(www.downloadHandler.text); });
                parsing.ConfigureAwait(false);

                Plugin.log.Info("Parsing scrapped data...");
                Stopwatch timer = new Stopwatch();

                timer.Start();
                parsing.Start();

                yield return new WaitUntil(() => parsing.IsCompleted);

                timer.Stop();
                Downloaded = true;
                callback?.Invoke(Songs);
                Plugin.log.Info($"Scrapped data parsed! Time: {timer.Elapsed.TotalSeconds.ToString("0.00")}s");
            }
        }
        
    }
}
