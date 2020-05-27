using VRrhythmLeague.LeagueAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace VRrhythmLeague.Misc
{
    class LoadScripts
    {
        static public Dictionary<string, Texture2D> _cachedTextures = new Dictionary<string, Texture2D>();

        static public IEnumerator LoadSpriteCoroutine(string spritePath, Action<Texture2D> done)
        {
            Plugin.log.Debug($"Cover loading : {spritePath}");
            if (_cachedTextures.ContainsKey(spritePath))
            {
                Plugin.log.Debug($"Cover already loaded : {_cachedTextures[spritePath]}");
                Plugin.log.Debug($"Cover after action : {done}");
                done?.Invoke(_cachedTextures[spritePath]);
                yield break;
            }
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(spritePath))
                {

                    www.SetRequestHeader("User-Agent", SongDownloader.UserAgent);
                    www.certificateHandler = new CustomCertificateHandler();

                    yield return www.SendWebRequest();

                    if (www.isHttpError || www.isNetworkError)
                    {
                    byte[] imageBytes;

                    imageBytes = Convert.FromBase64String(CustomPlaylistSO.DEFAULT_IMAGE.Substring(CustomPlaylistSO.DEFAULT_IMAGE.IndexOf(",") + 1));
                    
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadRawTextureData(imageBytes);


                    Plugin.log.Warn("Unable to download sprite! Exception: " + www.error);
                    done?.Invoke(tex);
                }
                    else
                    {
                        Texture2D tex = DownloadHandlerTexture.GetContent(www);
                        if (!_cachedTextures.ContainsKey(spritePath))
                            _cachedTextures.Add(spritePath, tex);
                        Plugin.log.Debug($"Cover val : {_cachedTextures[spritePath]}");

                        Plugin.log.Debug($"Cover loaded : {spritePath}");
                        Plugin.log.Debug($"Cover after action : {done}");

                        done?.Invoke(_cachedTextures[spritePath]);
                    }
                }
            //}
        }

    }
}
