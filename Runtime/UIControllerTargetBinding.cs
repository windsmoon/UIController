using System;
using UnityEngine;

#if UNITY_EDITOR
namespace Windsmoon.UIController
{
    [Serializable]
    [Obsolete("Legacy data for manual migration only.")]
    public struct UIControllerTargetBinding
    {
        #region fields
        public string Name;
        public RectTransform RectTransform;
        #endregion
    }
}
#endif
