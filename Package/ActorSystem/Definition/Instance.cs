using System;
using System.Collections.Generic;
using KahaGameCore.ValueContainer;
using UnityEngine;

namespace KahaGameCore.Package.ActorSystem.Definition
{
    public class Instance : MonoBehaviour, IValueContainer
    {
        public Animator Animator => animator;
        [SerializeField] private Animator animator;
        [SerializeField] private float rotationSpeed = 10f;

        private Dictionary<string, int> baseValues = new Dictionary<string, int>();
        private Dictionary<string, int> tempValues = new Dictionary<string, int>();
        private Dictionary<string, string> stringKeyValues = new Dictionary<string, string>();

        public void UpdateFacingDirection(Vector3 direction, float? rotationSpeedOverride = null)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                Vector3 flatDirection = new Vector3(direction.x, 0, direction.z).normalized;
                if (flatDirection.sqrMagnitude > 0.001f)
                {
                    // Calculate target rotation
                    Quaternion targetRotation = Quaternion.LookRotation(flatDirection);

                    // Use provided rotation speed or default to the serialized field
                    float currentRotationSpeed = rotationSpeedOverride ?? rotationSpeed;

                    // Smoothly rotate towards the target rotation
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        currentRotationSpeed * Time.deltaTime
                    );
                }
            }
        }

        public Guid Add(string tag, int value)
        {
            string newGuid = Guid.NewGuid().ToString();
            if (!tempValues.ContainsKey(newGuid))
            {
                tempValues[newGuid] = 0;
            }
            tempValues[newGuid] += value;
            return new Guid(newGuid);
        }

        public void AddBase(string tag, int value)
        {
            if (!baseValues.ContainsKey(tag))
            {
                baseValues[tag] = 0;
            }
            baseValues[tag] += value;
        }

        public void AddToTemp(Guid guid, int value)
        {
            string guidStr = guid.ToString();
            if (!tempValues.ContainsKey(guidStr))
            {
                tempValues[guidStr] = 0;
            }
            tempValues[guidStr] += value;
        }

        public Dictionary<string, string> GetAllStringKeyValuePairs()
        {
            return new Dictionary<string, string>(stringKeyValues);
        }

        public string GetStringKeyValue(string key)
        {
            if (stringKeyValues.TryGetValue(key, out string value))
            {
                return value;
            }
            return string.Empty;
        }

        public int GetTotal(string tag, bool baseOnly)
        {
            int baseValue = 0;
            if (baseValues.TryGetValue(tag, out int bValue))
            {
                baseValue = bValue;
            }

            if (baseOnly)
            {
                return baseValue;
            }

            int tempValue = 0;
            foreach (var kvp in tempValues)
            {
                tempValue += kvp.Value;
            }

            return baseValue + tempValue;
        }

        public void Remove(Guid guid)
        {
            string guidStr = guid.ToString();
            if (tempValues.ContainsKey(guidStr))
            {
                tempValues.Remove(guidStr);
            }
        }

        public void RemoveStringKeyValue(string key)
        {
            if (stringKeyValues.ContainsKey(key))
            {
                stringKeyValues.Remove(key);
            }
        }

        public void SetBase(string tag, int value)
        {
            baseValues[tag] = value;
        }

        public void SetStringKeyValue(string key, string value)
        {
            stringKeyValues[key] = value;
        }

        public void SetTemp(Guid guid, int value)
        {
            string guidStr = guid.ToString();
            tempValues[guidStr] = value;
        }
    }
}
