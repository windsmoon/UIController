using System;
using System.Collections.Generic;
using UnityEngine;

namespace Windsmoon.UIController
{
    [Serializable]
    public class UIControllerStateData
    {
        #region fields
#if UNITY_EDITOR
        [SerializeField]
        private string _comment;
        [SerializeField, Obsolete("Legacy state index for old serialized data only.")]
        private int _index;
#endif
        [SerializeField]
        private List<UIControllerTargetStateData> _targetStateList = new List<UIControllerTargetStateData>();
        #endregion

        #region properties
#if UNITY_EDITOR
#pragma warning disable CS0618
        public int Index
        {
            get => _index;
            set => _index = value;
        }
#pragma warning restore CS0618
#endif

        public List<UIControllerTargetStateData> TargetStateList => _targetStateList;
#if UNITY_EDITOR
        public string Comment
        {
            get => _comment;
            set => _comment = value;
        }
#endif
        #endregion

        #region methods
        public void RebuildCache()
        {
            for (int i = 0; i < _targetStateList.Count; i++)
            {
                _targetStateList[i]?.RebuildCache();
            }
        }
        #endregion
    }
}
