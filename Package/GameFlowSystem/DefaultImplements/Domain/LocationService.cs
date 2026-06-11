using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.GameData.Implemented;
using KahaGameCore.GameEvent;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.Package.GameFlowSystem.DefaultImplements.Events;
using UnityEngine;

namespace KahaGameCore.Package.GameFlowSystem.DefaultImplements
{
    public class LocationService : ILocationService
    {
        public int CurrentLocationID => gameState.Get(GameValueTags.CurrentLocation);
        public LocationData CurrentLocation => FindLocation(CurrentLocationID);

        private readonly IGameState gameState;
        private readonly IConditionEvaluator conditionEvaluator;
        private readonly List<LocationData> locations;

        public LocationService(GameStaticDataManager staticDataManager, IGameState gameState, IConditionEvaluator conditionEvaluator)
        {
            this.gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            this.conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));

            LocationData[] loadedLocations = staticDataManager.GetAllGameData<LocationData>();
            locations = loadedLocations == null
                ? new List<LocationData>()
                : loadedLocations.OrderBy(location => location.SortOrder).ToList();
        }

        public void MoveTo(int locationId)
        {
            LocationData location = FindLocation(locationId);
            if (location == null)
            {
                Debug.LogError($"[LocationService] 找不到地點 ID={locationId}。");
                return;
            }

            if (CurrentLocationID == locationId)
            {
                return;
            }

            gameState.Set(GameValueTags.CurrentLocation, locationId);
            EventBus.Publish(new LocationChangedEvent(location));
        }

        public void Unlock(int locationId)
        {
            gameState.Set(GameValueTags.LocationUnlocked(locationId), 1);
        }

        public IReadOnlyList<LocationData> GetSelectableLocations()
        {
            return locations
                .Where(location => location.ShowInMenu == 1)
                .Where(location => location.ID != CurrentLocationID)
                .Where(location => conditionEvaluator.Evaluate(location.VisibleCondition))
                .ToList();
        }

        private LocationData FindLocation(int locationId)
        {
            return locations.Find(location => location.ID == locationId);
        }
    }
}
