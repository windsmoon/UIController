using System;
using System.Collections.Generic;
using UnityEngine;

namespace Windsmoon.UIController
{
    [Serializable]
    public class UIControllerStateData
    {
        #region fields
        [SerializeField]
        private int _index;
        [SerializeField]
        private List<UIControllerTargetStateData> _targetStateList = new List<UIControllerTargetStateData>();
#if UNITY_EDITOR
        [SerializeField]
        private string _comment;
#endif

        private Dictionary<string, UIControllerTargetStateData> _targetStateDict;
        #endregion

        #region properties
        public int Index
        {
            get => _index;
            set => _index = value;
        }

        public List<UIControllerTargetStateData> TargetStateList => _targetStateList;
#if UNITY_EDITOR
        public string Comment
        {
            get => _comment;
            set => _comment = value;
        }
#endif
        public IReadOnlyDictionary<string, UIControllerTargetStateData> TargetStateDict
        {
            get
            {
                EnsureTargetStateDict();
                return _targetStateDict;
            }
        }
        #endregion

        #region methods
        public void RebuildCache()
        {
            EnsureTargetStateList();
            _targetStateDict = new Dictionary<string, UIControllerTargetStateData>(_targetStateList.Count);

            for (int i = 0; i < _targetStateList.Count; i++)
            {
                UIControllerTargetStateData targetStateData = _targetStateList[i];
                if (targetStateData == null)
                {
                    continue;
                }

                targetStateData.RebuildCache();
                if (string.IsNullOrWhiteSpace(targetStateData.Name) || _targetStateDict.ContainsKey(targetStateData.Name))
                {
                    continue;
                }

                _targetStateDict.Add(targetStateData.Name, targetStateData);
            }
        }
        #endregion

        #region private methods
        private void EnsureTargetStateDict()
        {
            if (_targetStateDict != null)
            {
                return;
            }

            RebuildCache();
        }

        private void EnsureTargetStateList()
        {
            if (_targetStateList == null)
            {
                _targetStateList = new List<UIControllerTargetStateData>();
            }
        }
        #endregion
    }
}
