using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace sapra.InfiniteLands{
    public abstract class UpdateableSO : ScriptableObject
    {
        public static bool ToCall = false;
        [HideInInspector] public Action OnValuesUpdated;
        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (!ToCall)
            {
                EditorApplication.delayCall += TriggerCallBack;
                ToCall = true;
            }
#endif
        }
        public void TriggerCallBack()
        {
            OnValuesUpdated?.Invoke();
            ToCall = false;
        }
    }
}