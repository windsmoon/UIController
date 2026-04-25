using System;
using System.Collections.Generic;
using Windsmoon.UIStateController.Properties;

namespace Windsmoon.UIStateController
{
    public sealed class UIControllerPropertyDefinition
    {
        #region fields
        private readonly Func<UIControllerProperty> _createFunc;
        #endregion

        #region properties
        public string Name { get; }
        #endregion

        #region methods
        public UIControllerPropertyDefinition(string name, Func<UIControllerProperty> createFunc)
        {
            Name = name;
            _createFunc = createFunc;
        }

        public UIControllerProperty Create()
        {
            return _createFunc();
        }
        #endregion
    }

    public static class UIControllerPropertyFactory
    {
        #region fields
        private static readonly UIControllerPropertyDefinition[] s_definitionArray =
        {
            new UIControllerPropertyDefinition(UIControllerActiveProperty.PropertyName, () => new UIControllerActiveProperty()),
            new UIControllerPropertyDefinition(UIControllerAnchoredPositionProperty.PropertyName, () => new UIControllerAnchoredPositionProperty()),
            new UIControllerPropertyDefinition(UIControllerCanvasGroupAlphaProperty.PropertyName, () => new UIControllerCanvasGroupAlphaProperty()),
            new UIControllerPropertyDefinition(UIControllerImageColorProperty.PropertyName, () => new UIControllerImageColorProperty()),
            new UIControllerPropertyDefinition(UIControllerLocalScaleProperty.PropertyName, () => new UIControllerLocalScaleProperty()),
            new UIControllerPropertyDefinition(UIControllerSizeDeltaProperty.PropertyName, () => new UIControllerSizeDeltaProperty()),
            new UIControllerPropertyDefinition(UIControllerTextForTextMeshProperty.PropertyName, () => new UIControllerTextForTextMeshProperty()),
            new UIControllerPropertyDefinition(UIControllerTextMeshColorProperty.PropertyName, () => new UIControllerTextMeshColorProperty()),
        };

        private static readonly Dictionary<string, UIControllerPropertyDefinition> s_definitionDict = BuildDefinitionDict();
        #endregion

        #region properties
        public static IReadOnlyList<UIControllerPropertyDefinition> Definitions => s_definitionArray;
        #endregion

        #region methods
        public static UIControllerProperty Create(string propertyName)
        {
            if (s_definitionDict.TryGetValue(propertyName, out UIControllerPropertyDefinition definition))
            {
                return definition.Create();
            }

            return null;
        }

        private static Dictionary<string, UIControllerPropertyDefinition> BuildDefinitionDict()
        {
            Dictionary<string, UIControllerPropertyDefinition> definitionDict = new Dictionary<string, UIControllerPropertyDefinition>(s_definitionArray.Length);
            for (int i = 0; i < s_definitionArray.Length; i++)
            {
                UIControllerPropertyDefinition definition = s_definitionArray[i];
                definitionDict[definition.Name] = definition;
            }

            return definitionDict;
        }
        #endregion
    }
}
