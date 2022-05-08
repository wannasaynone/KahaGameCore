using UnityEngine;
using System.Collections;
using System;

namespace KahaGameCore.Common
{
    public static class GameUtility
    {
        public static void RunNunber(float from, float to, float time, Action<float> onAdd, Action onDone)
        {
            float _each = (to - from) / (time / Time.deltaTime);
            GeneralCoroutineRunner.Instance.StartCoroutine(IENumberRunner(from, to, _each, time, onAdd, onDone));
        }

        private static IEnumerator IENumberRunner(float current, float target, float each, float time, Action<float> onAdd, Action onDone)
        {
            float _delta = Time.deltaTime;

            current += each;

            if(current >= target)
            {
                current = target;
            }

            onAdd?.Invoke(current);

            time -= _delta;
            if(time <= 0f)
            {
                onDone?.Invoke();
                yield break;
            }
            yield return new WaitForSeconds(_delta);

            GeneralCoroutineRunner.Instance.StartCoroutine(IENumberRunner(current, target, each, time, onAdd, onDone));
        }

        public static void CheckConnection(Action<bool> onChecked)
        {
            Debug.Log("CheckConnection");
            Debug.Log("Application.internetReachability=" + Application.internetReachability);
            onChecked?.Invoke(Application.internetReachability != NetworkReachability.NotReachable);
        }

        public static Color GetColor(string colorString)
        {
            Color _color = Color.white;
            if (ColorUtility.TryParseHtmlString(colorString, out _color))
            {
                return _color;
            }
            else
            {
                return Color.black;
            }
        }
    }
}
