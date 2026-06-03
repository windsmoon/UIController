using System;
using System.Collections.Generic;
using UnityEngine;

namespace Windsmoon.UIController
{
    [Serializable]
    public class UIControllerTargetData
    {
        #region fields
#if UNITY_EDITOR
        [SerializeField]
        private string _name;
#endif
        [SerializeField]
        private RectTransform _rectTransform;
        [SerializeField]
        private List<string> _propertyNameList = new List<string>();
        #endregion

        #region properties
#if UNITY_EDITOR
        public string Name
        {
            get => _name;
            set => _name = value;
        }
#endif
        public RectTransform RectTransform
        {
            get => _rectTransform;
            set => _rectTransform = value;
        }

        public List<string> PropertyNameList => _propertyNameList;
        #endregion
    }
}
