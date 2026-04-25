using System;
using System.Collections.Generic;
using Windsmoon.UIStateController.Properties;
using UnityEngine;

namespace Windsmoon.UIStateController
{
    [Serializable]
    public class UIControllerTargetStateData
    {
        #region fields
        [SerializeField]
        private string _name;
        [SerializeReference]
        private List<UIControllerProperty> _propertyList = new List<UIControllerProperty>();

        private Dictionary<string, UIControllerProperty> _propertyDict;
        #endregion

        #region properties
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public List<UIControllerProperty> PropertyList => _propertyList;
        public IReadOnlyDictionary<string, UIControllerProperty> PropertyDict
        {
            get
            {
                EnsurePropertyDict();
                return _propertyDict;
            }
        }
        #endregion

        #region methods
        public void RebuildCache()
        {
            EnsurePropertyList();
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

            EnsurePropertyList();
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

            EnsurePropertyList();
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

        private void EnsurePropertyList()
        {
            if (_propertyList == null)
            {
                _propertyList = new List<UIControllerProperty>();
            }
        }
        #endregion
    }
}
