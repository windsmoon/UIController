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
        private List<UIControllerStateData> _stateList = new List<UIControllerStateData>();
        #endregion

        #region properties
        public string Name => _name;
        public List<UIControllerStateData> StateList => _stateList;
        #endregion
    }
}
