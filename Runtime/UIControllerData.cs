using System;
using System.Collections.Generic;
using UnityEngine;

namespace Windsmoon.UIController
{
    [Serializable]
    public class UIControllerData
    {
        #region fields
        [SerializeField]
        private string _name;
        [SerializeField]
        private List<UIControllerTargetData> _targetList = new List<UIControllerTargetData>();
        [SerializeField]
        private List<UIControllerStateData> _stateList = new List<UIControllerStateData>();

        private Dictionary<string, int> _stateNameIndexDict;
        #endregion

        #region properties
        public string Name => _name;
        public List<UIControllerTargetData> TargetList => _targetList;
        public List<UIControllerStateData> StateList => _stateList;
        #endregion

        #region methods
        public void RebuildCache()
        {
            RebuildStateNameCache();

            for (int i = 0; i < _stateList.Count; i++)
            {
                _stateList[i]?.RebuildCache();
            }
        }

        public bool TryGetStateIndex(string stateName, out int stateIndex)
        {
            stateIndex = -1;
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            EnsureStateNameIndexDict();
            if (_stateNameIndexDict.TryGetValue(stateName, out stateIndex))
            {
                return true;
            }

            stateIndex = -1;
            return false;
        }

        private void RebuildStateNameCache()
        {
            _stateNameIndexDict = new Dictionary<string, int>(_stateList.Count);

            for (int i = 0; i < _stateList.Count; i++)
            {
                UIControllerStateData stateData = _stateList[i];
                if (stateData == null || string.IsNullOrWhiteSpace(stateData.Name))
                {
                    continue;
                }

                if (_stateNameIndexDict.ContainsKey(stateData.Name))
                {
                    string controllerName = string.IsNullOrWhiteSpace(Name) ? "<unnamed>" : Name;
                    Debug.LogError($"controller {controllerName} has duplicate state name {stateData.Name} at state index {i}. The first state name entry was kept.");
                    continue;
                }

                _stateNameIndexDict.Add(stateData.Name, i);
            }
        }

        private void EnsureStateNameIndexDict()
        {
            if (_stateNameIndexDict != null)
            {
                return;
            }

            RebuildStateNameCache();
        }
        #endregion
    }
}
