using System;
using System.Collections.Generic;
using Windsmoon.UIController.Properties;
using UnityEngine;

namespace Windsmoon.UIController
{
    [Serializable]
    public class UIControllerTargetStateData
    {
        #region fields
#if UNITY_EDITOR
        [SerializeField, Obsolete("Legacy target name for manual migration only.")]
        private string _name;
#endif
        [SerializeReference]
        private List<UIControllerProperty> _propertyList = new List<UIControllerProperty>();

        private Dictionary<string, UIControllerProperty> _propertyDict;
        #endregion

        #region properties
#if UNITY_EDITOR
#pragma warning disable CS0618
        public string Name
        {
            get => _name;
            set => _name = value;
        }
#pragma warning restore CS0618
#endif

        public List<UIControllerProperty> PropertyList => _propertyList;
        #endregion

        #region methods
        public void RebuildCache()
        {
            _propertyDict = new Dictionary<string, UIControllerProperty>(_propertyList.Count);

            for (int i = 0; i < _propertyList.Count; i++)
            {
                UIControllerProperty property = _propertyList[i];
                if (property == null || string.IsNullOrWhiteSpace(property.Name) || _propertyDict.ContainsKey(property.Name))
                {
                    continue;
                }

                _propertyDict.Add(property.Name, property);
            }
        }

        public UIControllerProperty GetProperty(string propertyName)
        {
            EnsurePropertyDict();
            _propertyDict.TryGetValue(propertyName, out UIControllerProperty property);
            return property;
        }

        public void SetProperty(UIControllerProperty property)
        {
            if (property == null || string.IsNullOrWhiteSpace(property.Name))
            {
                return;
            }

            RemoveProperty(property.Name);
            _propertyList.Add(property);
            EnsurePropertyDict();
            _propertyDict[property.Name] = property;
        }

        public void RemoveProperty(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            for (int i = _propertyList.Count - 1; i >= 0; i--)
            {
                UIControllerProperty property = _propertyList[i];
                if (property != null && property.Name == propertyName)
                {
                    _propertyList.RemoveAt(i);
                }
            }

            if (_propertyDict != null)
            {
                _propertyDict.Remove(propertyName);
            }
        }
        #endregion

        #region private methods
        private void EnsurePropertyDict()
        {
            if (_propertyDict != null)
            {
                return;
            }

            RebuildCache();
        }
        #endregion
    }
}
