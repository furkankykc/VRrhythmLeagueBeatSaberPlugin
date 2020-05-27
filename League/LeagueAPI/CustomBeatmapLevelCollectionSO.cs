using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VRrhythmLeague.LeagueAPI
{
	public class CustomBeatmapLevelCollectionSO : PersistentScriptableObject, IBeatmapLevelCollection
	{
		public static CustomBeatmapLevelCollectionSO CreateInstance(IPreviewBeatmapLevel[] beatmapLevels)
		{
			CustomBeatmapLevelCollectionSO customBeatmapLevelCollectionSO = PersistentScriptableObject.CreateInstance<CustomBeatmapLevelCollectionSO>();
			customBeatmapLevelCollectionSO._beatmapLevels = beatmapLevels;
			return customBeatmapLevelCollectionSO;
		}

		public IPreviewBeatmapLevel[] beatmapLevels
		{
			get
			{
				return this._beatmapLevels;
			}
		}

		[SerializeField]
		protected IPreviewBeatmapLevel[] _beatmapLevels;
	}
}
