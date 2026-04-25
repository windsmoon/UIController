using System;
using UnityEngine;

namespace Framework.UI.Controller
{
    [Serializable]
    public struct UIControllerTargetBinding
    {
        #region fields
        public string Name;
        public RectTransform RectTransform;
        #endregion
    }
}
