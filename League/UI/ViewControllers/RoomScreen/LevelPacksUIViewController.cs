using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components;
using System;
using System.Linq;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using UnityEngine;
using System.Collections.Generic;
using VRrhythmLeague.LeagueAPI;

namespace VRrhythmLeague.UI.ViewControllers.RoomScreen
{
    class LevelPacksUIViewController : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action<IAnnotatedBeatmapLevelCollection> packSelected;

		[UIComponent("packs-list-table")]
        CustomListTableData levelPacksTableData;

		//[UIComponent("packs-collections-control")]
		//TextSegmentedControl packsCollectionsControl;
		private BeatmapDifficulty difficulty;
		private bool _initialized;
		private BeatmapLevelsModel _beatmapLevelsModel;
		private IAnnotatedBeatmapLevelCollection[] _visiblePacks;

		protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            base.DidActivate(firstActivation, type);

			//packsCollectionsControl.SetTexts(new string[] { "OST & EXTRAS", "MUSIC PACKS", "PLAYLISTS", "CUSTOM LEVELS" });
			Plugin.log.Debug($"Pack onloaded Init ");

			LeagueRequestHandler.Instance.playlistsChanged += this.onLoaded;

			Initialize();

		}

		public void Initialize()
        {
			if (_initialized)
			{
				return;
			}

			if (_beatmapLevelsModel == null)
				_beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();

			_visiblePacks = LeagueAPI.LeagueRequestHandler.Instance.getPlaylist();
			SetPacks(_visiblePacks);
			SongCore.Loader.SongsLoadedEvent += onLoaded;

			if (_visiblePacks.Length > 0)
				packSelected?.Invoke(_visiblePacks[0]);

			_initialized = true;
		}
		void onLoaded(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> songs)
		{
			_visiblePacks = LeagueRequestHandler.Instance.getPlaylist();
			SetPacks(_visiblePacks);
			if (_visiblePacks.Length > 0)
				packSelected?.Invoke(_visiblePacks[0]);
		}
		void onLoaded()
		{
			Plugin.log.Debug($"Pack onloaded called on working ");

			_visiblePacks = LeagueAPI.LeagueRequestHandler.Instance.getPlaylist();
			SetPacks(_visiblePacks);
			if (_visiblePacks.Length > 0)
				packSelected?.Invoke(_visiblePacks[0]);
		}
		public void SetPacks(IAnnotatedBeatmapLevelCollection[] packs)
		{
			levelPacksTableData.data.Clear();

			foreach (var pack in packs)
			{
				levelPacksTableData.data.Add(new CustomListTableData.CustomCellInfo(pack.collectionName, $"{pack.beatmapLevelCollection.beatmapLevels.Length} levels", pack.coverImage.texture));
			}

			levelPacksTableData.tableView.ReloadData();
		}

		[UIAction("pack-selected")]
		public void PackSelected(TableView sender, int index)
		{
			packSelected?.Invoke(_visiblePacks[index]);
		}
	}
}
