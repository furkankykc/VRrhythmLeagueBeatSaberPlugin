using VRrhythmLeague.Data;
using VRrhythmLeague.Misc;
using VRrhythmLeague.OverriddenClasses;
using VRrhythmLeague.VOIP;
using BS_Utils.Utilities;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Xft;

namespace VRrhythmLeague
{
    public class OnlinePlayerController : PlayerController
    {
        private const int _voipDelay = 1;

        public PlayerInfo playerInfo;
        public AudioSource voipSource;

        public OnlineBeatmapCallbackController beatmapCallbackController;
        public OnlineBeatmapSpawnController beatmapSpawnController;
        public OnlineBeatmapObjectManager beatmapObjectManager;
        public OnlineAudioTimeController audioTimeController;

        public float avatarOffset;
        public bool noInterpolation = false;
        public bool destroyed = false;

        private PlayerUpdate _syncStartInfo;
        private PlayerUpdate _syncEndInfo;

        private bool _voipPlaying;
        private FifoFloatStream _voipFragQueue = new FifoFloatStream();
        private float[] _voipBuffer;
        private int _lastVoipFragIndex;
        private int _silentFrames;
        private int _voipDelayCounter;

        public double syncDelay = 0f;
        private double lastSynchronizationTime = 0f;
        private double syncTime = 0f;
        private float lerpProgress = 0f;

        public void Start()
        {
            Plugin.log.Debug($"Player controller created!");

            voipSource = gameObject.AddComponent<AudioSource>();

            voipSource.clip = null;
            voipSource.spatialize = Config.Instance.SpatialAudio;
            voipSource.loop = true;
            voipSource.Play();

            if (playerInfo != null)
            {
                Plugin.log.Debug($"Starting player controller for {playerInfo.playerName}:{playerInfo.playerId}...");

                _syncStartInfo = playerInfo.updateInfo;
                _syncEndInfo = playerInfo.updateInfo;
            }
        }

        void SpawnBeatmapControllers()
        {
            Plugin.log.Debug("Creating beatmap controllers...");

            audioTimeController = new GameObject("OnlineAudioTimeController").AddComponent<OnlineAudioTimeController>();
            Plugin.log.Debug("Created audio time controller!");
            audioTimeController.Init(this);
            Plugin.log.Debug("Initialized audio time controller!");

            beatmapCallbackController = new GameObject("OnlineBeatmapCallbackController").AddComponent<OnlineBeatmapCallbackController>();
            Plugin.log.Debug("Created beatmap callback controller!");
            beatmapCallbackController.Init(this, audioTimeController);
            Plugin.log.Debug("Initialized beatmap callback controller!");

            beatmapObjectManager = new GameObject("OnlineBeatmapObjectManager").AddComponent<OnlineBeatmapObjectManager>();
            Plugin.log.Debug("Created beatmap object manager!");
            beatmapObjectManager.Init(this, audioTimeController);
            Plugin.log.Debug("Initialized beatmap object manager!");

            beatmapSpawnController = new GameObject("OnlineBeatmapSpawnController").AddComponent<OnlineBeatmapSpawnController>();
            Plugin.log.Debug("Created beatmap spawn controller!");
            beatmapSpawnController.Init(this, beatmapCallbackController, beatmapObjectManager, audioTimeController);
            Plugin.log.Debug("Initialized beatmap spawn controller!");
        }

        void SpawnSabers()
        {
            Plugin.log.Debug("Spawning left saber...");
            try
            {
                _leftSaber = Instantiate(Resources.FindObjectsOfTypeAll<Saber>().First(x => x.name == "LeftSaber"), transform, false);
            }
            catch (Exception e)
            {
                if (!(e is NullReferenceException) || !e.StackTrace.Contains("VRController.UpdatePositionAndRotation"))
                    Plugin.log.Critical("Unable to spawn saber (Left)! Exception: "+e);
            }
            _leftSaber.gameObject.name = "CustomLeftSaber";
            var leftController = _leftSaber.gameObject.AddComponent<OnlineVRController>();
            leftController.owner = this;
            _leftSaber.SetPrivateField("_vrController", leftController);

            //TODO: Check trails later

            /*
            var leftTrail = leftController.GetComponentInChildren<XWeaponTrail>();
            var colorManager = Resources.FindObjectsOfTypeAll<ColorManager>().First();
            leftTrail.SetPrivateField("_colorManager", colorManager);
            leftTrail.SetPrivateField("_saberTypeObject", leftController.GetComponentInChildren<SaberTypeObject>());
            */

            Plugin.log.Debug("Spawning right saber...");
            try
            { 
                _rightSaber = Instantiate(Resources.FindObjectsOfTypeAll<Saber>().First(x => x.name == "RightSaber"), transform, false);
            }
            catch (Exception e)
            {
                if (!(e is NullReferenceException) || !e.StackTrace.Contains("VRController.UpdatePositionAndRotation"))
                    Plugin.log.Critical("Unable to spawn saber (Right)! Exception: " + e);
            }
            _rightSaber.gameObject.name = "CustomRightSaber";
            var rightController = _rightSaber.gameObject.AddComponent<OnlineVRController>();
            rightController.owner = this;
            _rightSaber.SetPrivateField("_vrController", rightController);

            /*
            var rightTrail = rightController.GetComponentInChildren<XWeaponTrail>();
            rightTrail.SetPrivateField("_colorManager", colorManager);
            rightTrail.SetPrivateField("_saberTypeObject", rightController.GetComponentInChildren<SaberTypeObject>());
            */

            Plugin.log.Debug("Sabers spawned!");
        }

        public void SetSabers(Saber leftSaber, Saber rightSaber)
        {
            _leftSaber = leftSaber;
            _rightSaber = rightSaber;
        }

        public void SetBlocksState(bool active)
        {
            if(active && !playerInfo.updateInfo.playerLevelOptions.characteristicName.ToLower().Contains("degree") && beatmapCallbackController == null && audioTimeController == null && beatmapSpawnController == null && beatmapObjectManager == null && _leftSaber == null && _rightSaber == null)
            {
                SpawnBeatmapControllers();
                SpawnSabers();
            }
            else if (!active && beatmapCallbackController != null && audioTimeController != null && beatmapSpawnController != null && _leftSaber != null && _rightSaber != null)
            {
                Destroy(beatmapCallbackController.gameObject);
                Destroy(audioTimeController.gameObject);
                Destroy(beatmapSpawnController);
                beatmapObjectManager.PrepareForDestroy();
                Destroy(beatmapObjectManager.gameObject, 1.4f);
                Destroy(_leftSaber.gameObject);
                Destroy(_rightSaber.gameObject);
            }
        }

        public override void Update()
        {


            if (voipSource != null)
            {
                if(_voipFragQueue.Length <= 0)
                {
                    _voipPlaying = false;
                }

                if (_voipPlaying)
                {
                    _silentFrames = 0;
                }
            }
            else
            {
                _silentFrames = 999;
            }

            if (_rightSaber != null)
            {
                _rightSaber.ManualUpdate();
            }

            if (_leftSaber != null)
            {
                _leftSaber.ManualUpdate();
            }
        }

        public void FixedUpdate()
        {
            
            if (playerInfo != null && playerInfo.updateInfo != default)
            {
                if (!noInterpolation)
                {
                    syncTime += Time.fixedDeltaTime;

                    lerpProgress = (float)(syncTime / syncDelay);

                    playerInfo.updateInfo.headPos = Vector3.Lerp(_syncStartInfo.headPos, _syncEndInfo.headPos, lerpProgress);
                    playerInfo.updateInfo.leftHandPos = Vector3.Lerp(_syncStartInfo.leftHandPos, _syncEndInfo.leftHandPos, lerpProgress);
                    playerInfo.updateInfo.rightHandPos = Vector3.Lerp(_syncStartInfo.rightHandPos, _syncEndInfo.rightHandPos, lerpProgress);

                    playerInfo.updateInfo.headRot = Quaternion.Lerp(_syncStartInfo.headRot, _syncEndInfo.headRot, lerpProgress);
                    playerInfo.updateInfo.leftHandRot = Quaternion.Lerp(_syncStartInfo.leftHandRot, _syncEndInfo.leftHandRot, lerpProgress);
                    playerInfo.updateInfo.rightHandRot = Quaternion.Lerp(_syncStartInfo.rightHandRot, _syncEndInfo.rightHandRot, lerpProgress);

                    if (_syncStartInfo.fullBodyTracking)
                    {
                        playerInfo.updateInfo.leftLegPos = Vector3.Lerp(_syncStartInfo.leftLegPos, _syncEndInfo.leftLegPos, lerpProgress);
                        playerInfo.updateInfo.rightLegPos = Vector3.Lerp(_syncStartInfo.rightLegPos, _syncEndInfo.rightLegPos, lerpProgress);
                        playerInfo.updateInfo.pelvisPos = Vector3.Lerp(_syncStartInfo.pelvisPos, _syncEndInfo.pelvisPos, lerpProgress);

                        playerInfo.updateInfo.leftLegRot = Quaternion.Lerp(_syncStartInfo.leftLegRot, _syncEndInfo.leftLegRot, lerpProgress);
                        playerInfo.updateInfo.rightLegRot = Quaternion.Lerp(_syncStartInfo.rightLegRot, _syncEndInfo.rightLegRot, lerpProgress);
                        playerInfo.updateInfo.pelvisRot = Quaternion.Lerp(_syncStartInfo.pelvisRot, _syncEndInfo.pelvisRot, lerpProgress);
                    }

                    float lerpedPlayerProgress = Mathf.Lerp(_syncStartInfo.playerProgress, _syncEndInfo.playerProgress, lerpProgress);

                    playerInfo.updateInfo.playerProgress = lerpedPlayerProgress;
                }

                _overrideHeadPos = true;
                _overriddenHeadPos = playerInfo.updateInfo.headPos;
                _headPos = playerInfo.updateInfo.headPos + Vector3.right * avatarOffset;
                transform.position = _headPos;
            }
            
        }

        public void OnDestroy()
        {
            if(playerInfo == null)
                Plugin.log.Debug("Destroying player controller!");
            else
                Plugin.log.Debug($"Destroying player controller! Name: {playerInfo.playerName}, ID: {playerInfo.playerId}");

            destroyed = true;
            


            if (beatmapCallbackController != null && beatmapSpawnController != null && audioTimeController != null)
            {
                Destroy(beatmapCallbackController.gameObject);
                Destroy(audioTimeController.gameObject);
                Destroy(beatmapSpawnController.gameObject);
                beatmapObjectManager.PrepareForDestroy();
                Destroy(beatmapObjectManager.gameObject, 1.4f);
                Destroy(_leftSaber.gameObject);
                Destroy(_rightSaber.gameObject);
            }
        }

        public void UpdateInfo(PlayerUpdate newInfo)
        {
            if (playerInfo == null)
                return;

            syncTime = 0;
            syncDelay = Time.time - lastSynchronizationTime;

            if (syncDelay > 0.5f)
            {
                syncDelay = 0.5f;
            }

            lastSynchronizationTime = Time.time;

            if (noInterpolation)
            {
                playerInfo.updateInfo = newInfo;
                return;
            }
            else
            {
                playerInfo.updateInfo.playerNameColor = newInfo.playerNameColor;
                playerInfo.updateInfo.playerState = newInfo.playerState;

                playerInfo.updateInfo.fullBodyTracking = newInfo.fullBodyTracking;
                playerInfo.updateInfo.playerScore = newInfo.playerScore;
                playerInfo.updateInfo.playerCutBlocks = newInfo.playerCutBlocks;
                playerInfo.updateInfo.playerComboBlocks = newInfo.playerComboBlocks;
                playerInfo.updateInfo.playerTotalBlocks = newInfo.playerTotalBlocks;
                playerInfo.updateInfo.playerEnergy = newInfo.playerEnergy;
                playerInfo.updateInfo.playerLevelOptions = newInfo.playerLevelOptions;
                playerInfo.updateInfo.playerFlags = newInfo.playerFlags;
            }

            _syncStartInfo = _syncEndInfo;
            if (_syncStartInfo.IsRotNaN())
            {
                _syncStartInfo.headRot = Quaternion.identity;
                _syncStartInfo.leftHandRot = Quaternion.identity;
                _syncStartInfo.rightHandRot = Quaternion.identity;
                _syncStartInfo.leftLegRot = Quaternion.identity;
                _syncStartInfo.rightLegRot = Quaternion.identity;
                _syncStartInfo.pelvisRot = Quaternion.identity;
                Plugin.log.Warn("Start rotation is NaN!");
            }

            if (Mathf.Abs(playerInfo.updateInfo.playerProgress - newInfo.playerProgress) > 0.1f)
            {
                playerInfo.updateInfo.playerProgress = newInfo.playerProgress;
            }

            _syncEndInfo = newInfo;
            if (_syncEndInfo.IsRotNaN())
            {
                _syncEndInfo.headRot = Quaternion.identity;
                _syncEndInfo.leftHandRot = Quaternion.identity;
                _syncEndInfo.rightHandRot = Quaternion.identity;
                _syncEndInfo.leftLegRot = Quaternion.identity;
                _syncEndInfo.rightLegRot = Quaternion.identity;
                _syncEndInfo.pelvisRot = Quaternion.identity;
                Plugin.log.Warn("Target rotation is NaN!");
            }
        }



        public void VoIPUpdate()
        {
            _silentFrames++;
        }

#if DEBUG && VERBOSE
        int lastPlayPos;
        int lastFrame;
#endif

        public void PlayVoIPFragment(float[] data, int fragIndex)
        {
            if(voipSource != null && !InGameOnlineController.Instance.mutedPlayers.Contains(playerInfo.playerId))
            {
                if ((_lastVoipFragIndex + 1) != fragIndex || _silentFrames > 15)
                {
#if DEBUG && VERBOSE
                    Plugin.log.Info($"Starting from scratch! ((_lastVoipFragIndex + 1) != fragIndex): {(_lastVoipFragIndex + 1) != fragIndex}, (_silentFrames > 20): {_silentFrames > 20}, _lastVoipFragIndex: {_lastVoipFragIndex}, fragIndex: {fragIndex}");
#endif
                    _voipFragQueue.Flush();
                    _voipDelayCounter = 0;
                }
                else
                {
#if DEBUG && VERBOSE
                    Plugin.log.Info($"New data ({data.Length}) at pos {_voipFrames} while playing at {voipSource.timeSamples}, Overlap: {voipSource.timeSamples > _voipFrames && !(voipSource.timeSamples > _voipClip.samples/2 && _voipFrames < _voipClip.samples / 2)}, Delay: {_voipFrames - voipSource.timeSamples}, Speed: {voipSource.timeSamples - lastPlayPos}, Frames: {Time.frameCount - lastFrame}");

                    lastPlayPos = voipSource.timeSamples;
                    lastFrame = Time.frameCount;
#endif
                    _voipDelayCounter++;

                    if (!_voipPlaying && _voipDelayCounter >= _voipDelay)
                    {
                        _voipPlaying = true;
                    }
                }

                _lastVoipFragIndex = fragIndex;
                _silentFrames = 0;

                _voipFragQueue.Write(data, 0, data.Length);
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_voipPlaying)
                return;

            int sampleRate = AudioSettings.outputSampleRate;

            int dataLen = data.Length / channels;

            if (_voipBuffer == null || _voipBuffer.Length < Mathf.CeilToInt(dataLen / (sampleRate / 16000f)))
            {
                _voipBuffer = new float[Mathf.CeilToInt(dataLen / (AudioSettings.outputSampleRate / 16000f))];
                Plugin.log.Debug($"Created new VoIP player buffer! Size: {dataLen}, Channels: {channels}, Resampling rate: {sampleRate / 16000f}x");
            }

            int read = _voipFragQueue.Read(_voipBuffer, 0, Mathf.CeilToInt(dataLen / (sampleRate / 16000f)));

            AudioUtils.Resample(_voipBuffer, data, read, data.Length, 16000, sampleRate, channels);
        }

        public void SetVoIPVolume(float newVolume)
        {
            if(voipSource != null)
            {
                voipSource.volume = newVolume;
            }
        }

        public void SetSpatialAudioState(bool spatialAudio)
        {
            if (voipSource != null)
            {
                voipSource.spatialize = spatialAudio;
            }
        }

        public bool IsTalking()
        {
            return _silentFrames < 20;
        }
    }
}
