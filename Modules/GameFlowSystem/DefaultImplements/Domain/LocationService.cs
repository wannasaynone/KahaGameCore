using System;
using System.Collections.Generic;
using System.Linq;
using KahaGameCore.StaticData;
using KahaGameCore.Foundation.Messaging;
using KahaGameCore.GameFlowSystem.DefaultImplements.Data;
using KahaGameCore.GameFlowSystem.DefaultImplements.Events;
using UnityEngine;

namespace KahaGameCore.GameFlowSystem.DefaultImplements
{
    public class LocationService : ILocationService
    {
        public int CurrentLocationID => currentLocationId;
        public LocationData CurrentLocation => FindLocation(CurrentLocationID);

        private readonly IConditionEvaluator conditionEvaluator;
        private readonly List<LocationData> locations;
        private readonly int initialLocationId;
        private int currentLocationId;

        public LocationService(GameStaticDataManager staticDataManager, IConditionEvaluator conditionEvaluator)
        {
            this.conditionEvaluator = conditionEvaluator ?? throw new ArgumentNullException(nameof(conditionEvaluator));

            locations = LoadLocations(staticDataManager);
            initialLocationId = locations.Count == 0 ? 0 : locations[0].ID;
            currentLocationId = initialLocationId;
        }

        public void ResetToInitial()
        {
            SetCurrentLocation(initialLocationId);
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

            SetCurrentLocation(locationId);
            MessageBus.Publish(new LocationChangedEvent(location));
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

        private static List<LocationData> LoadLocations(GameStaticDataManager staticDataManager)
        {
            LocationData[] loadedLocations = staticDataManager.GetAllGameData<LocationData>();
            return loadedLocations == null
                ? new List<LocationData>()
                : loadedLocations.OrderBy(location => location.SortOrder).ToList();
        }

        private void SetCurrentLocation(int locationId)
        {
            currentLocationId = locationId;
        }
    }
}
