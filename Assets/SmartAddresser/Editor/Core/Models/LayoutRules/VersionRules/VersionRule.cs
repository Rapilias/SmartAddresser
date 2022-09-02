using System;
using SmartAddresser.Editor.Core.Models.Shared;
using SmartAddresser.Editor.Core.Models.Shared.AssetGroups;
using SmartAddresser.Editor.Foundation.TinyRx.ObservableCollection;
using SmartAddresser.Editor.Foundation.TinyRx.ObservableProperty;
using UnityEngine;

namespace SmartAddresser.Editor.Core.Models.LayoutRules.VersionRules
{
    /// <summary>
    ///     Provide rules for setting versions.
    /// </summary>
    [Serializable]
    public sealed class VersionRule : ISerializationCallbackReceiver
    {
        [SerializeField] private string _id;
        [SerializeField] private AssetGroupObservableList _assetGroups = new AssetGroupObservableList();
        private readonly ObservableProperty<string> _assetGroupDescription = new ObservableProperty<string>();
        private readonly ObservableProperty<string> _versionProviderDescription = new ObservableProperty<string>();

        [SerializeReference] private IVersionProvider _versionProviderInternal;

        public VersionRule()
        {
            _id = IdentifierFactory.Create();
            var defaultAssetGroup = new AssetGroup
            {
                Name =
                {
                    Value = "Default Asset Group"
                }
            };
            _assetGroups.Add(defaultAssetGroup);
            VersionProvider.Value = new ConstantVersionProvider();
        }

        public string Id => _id;

        public IObservableList<AssetGroup> AssetGroups => _assetGroups;
        public IReadOnlyObservableProperty<string> AssetGroupDescription => _assetGroupDescription;
        public IReadOnlyObservableProperty<string> VersionProviderDescription => _versionProviderDescription;

        public ObservableProperty<IVersionProvider> VersionProvider { get; } =
            new ObservableProperty<IVersionProvider>();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            _versionProviderInternal = VersionProvider.Value;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            VersionProvider.Value = _versionProviderInternal;
        }

        /// <summary>
        ///     Setup to generate versions.
        ///     This method must be called before calling <see cref="TryProvideVersion" />.
        /// </summary>
        public void Setup()
        {
            _assetGroups.Setup();
            _versionProviderInternal.Setup();
        }

        /// <summary>
        ///     Create a version from asset information.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="assetType"></param>
        /// <param name="isFolder"></param>
        /// <param name="version">If successful, assign the address. If not, null.</param>
        /// <returns>Return true if successful.</returns>
        public bool TryProvideVersion(string assetPath, Type assetType, bool isFolder, out string version)
        {
            if (!_assetGroups.Contains(assetPath, assetType, isFolder))
            {
                version = null;
                return false;
            }

            version = _versionProviderInternal.Provide(assetPath, assetType, isFolder);
            return true;
        }

        internal void RefreshAssetGroupDescription()
        {
            var description = _assetGroups.GetDescription();
            if (string.IsNullOrEmpty(description))
                description = "(None)";
            _assetGroupDescription.Value = description;
        }

        internal void RefreshVersionProviderDescription()
        {
            _versionProviderDescription.Value = _versionProviderInternal == null
                ? "(None)"
                : _versionProviderInternal.GetDescription();
        }
    }
}