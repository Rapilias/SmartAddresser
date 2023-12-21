using System.Collections.Generic;
using System.Linq;
using SmartAddresser.Editor.Core.Models.LayoutRules;
using SmartAddresser.Editor.Foundation.AddressableAdapter;
using SmartAddresser.Editor.Foundation.AssetDatabaseAdapter;
using SmartAddresser.Editor.Foundation.SemanticVersioning;

namespace SmartAddresser.Editor.Core.Models.Services
{
    public sealed class ApplyLayoutRuleService
    {
        private readonly IAddressableAssetSettingsAdapter _addressableSettingsAdapter;
        private readonly IAssetDatabaseAdapter _assetDatabaseAdapter;
        private readonly LayoutRule _layoutRule;
        private readonly IVersionExpressionParser _versionExpressionParser;

        public ApplyLayoutRuleService(LayoutRule layoutRule,
            IVersionExpressionParser versionExpressionParser,
            IAddressableAssetSettingsAdapter addressableSettingsAdapter,
            IAssetDatabaseAdapter assetDatabaseAdapter)
        {
            _layoutRule = layoutRule;
            _addressableSettingsAdapter = addressableSettingsAdapter;
            _versionExpressionParser = versionExpressionParser;
            _assetDatabaseAdapter = assetDatabaseAdapter;
        }

        /// <summary>
        ///     If you want to process multiple asset by this instance, you should call this method before you call
        ///     <see cref="TryAddEntry" /> and set <c>false</c> to the <c>doSetup</c> argument of the <see cref="TryAddEntry" />.
        ///     If you want to process single asset by this instance, you should not call this method and set <c>true</c> to the
        ///     <c>doSetup</c> argument of the <see cref="TryAddEntry" />.
        /// </summary>
        public void Setup()
        {
            _layoutRule.SetupForAddress();
            _layoutRule.SetupForLabels();
            _layoutRule.SetupForVersion();
        }

        /// <summary>
        ///     Apply the layout rule to the addressable settings for all assets.
        /// </summary>
        public void UpdateAllEntries()
        {
            Setup();

            // Remove all entries in the addressable groups that are under control of this layout rule.
            var groupNames = _layoutRule
                .AddressRules
                .Where(x => x.Control.Value)
                .Select(x => x.AddressableGroup.Name);
            foreach (var groupName in groupNames)
                _addressableSettingsAdapter.RemoveAllEntries(groupName);

            // Add all entries to the addressable asset system.
            var versionExpression = _layoutRule.Settings.VersionExpression;
            TryAddEntries(_assetDatabaseAdapter.GetAllAssetPaths(), false, versionExpression.Value);
        }

        /// <summary>
        ///     Apply the layout rule to the addressable settings.
        /// </summary>
        /// <param name="pathes"></param>
        /// <param name="doSetup">
        ///     If true, setup rules before providing.
        ///     When you call this method multiple times, set this false and call <see cref="Setup" /> before.
        ///     If you call this method only once, set this true and don't call <see cref="Setup" />.
        /// </param>
        /// <param name="versionExpression"></param>
        /// <returns>
        ///     If the layout rule was applied to the addressable asset system, return true.
        ///     Returns false if no suitable layout rule was found.
        /// </returns>
        public void TryAddEntries(string[] pathes, bool doSetup, string versionExpression = null)
        {
            var groupedGuids = new Dictionary<string, List<string>>();
            foreach (var assetPath in pathes)
            {
                var assetGuid = _assetDatabaseAdapter.AssetPathToGUID(assetPath);
                var assetType = _assetDatabaseAdapter.GetMainAssetTypeAtPath(assetPath);
                var isFolder = _assetDatabaseAdapter.IsValidFolder(assetPath);

                // If the layout rule was not found, return false.
                if (!_layoutRule.TryProvideAddressAndAddressableGroup(assetPath, assetType, isFolder, doSetup,
                        out _,
                        out var addressableGroup))
                    continue;

                // If the layout rule is found but the addressable asset group has already been destroyed, return false.
                if (addressableGroup == null)
                    continue;

                // Check the version if it is specified.
                if (!string.IsNullOrEmpty(versionExpression))
                {
                    var comparator = _versionExpressionParser.CreateComparator(versionExpression);
                    var versionText = _layoutRule.ProvideVersion(assetPath, assetType, isFolder, doSetup);

                    if (string.IsNullOrEmpty(versionText) && _layoutRule.Settings.ExcludeUnversioned.Value)
                        continue;

                    // If the version is not satisfied, return false.
                    if (!string.IsNullOrEmpty(versionText) && Version.TryCreate(versionText, out var version) && !comparator.IsSatisfied(version))
                        continue;
                }

                if (!groupedGuids.TryGetValue(addressableGroup.name, out var assetGuids))
                {
                    assetGuids = new List<string>();
                    groupedGuids.Add(addressableGroup.name, assetGuids);
                }
                assetGuids.Add(assetGuid);
            }

            // Update entries 
            foreach (var kvp in groupedGuids)
            {
                var entryAdapters = _addressableSettingsAdapter.CreateOrMoveEntries(kvp.Key, kvp.Value);
                foreach (var entryAdapter in entryAdapters)
                {
                    var assetPath = entryAdapter.Address;
                    var assetType = _assetDatabaseAdapter.GetMainAssetTypeAtPath(assetPath);
                    var isFolder = _assetDatabaseAdapter.IsValidFolder(assetPath);
                
                    UpdateAssetLabel(entryAdapter, assetPath, assetType, isFolder, doSetup);
                }   
            }
        }

        /// <summary>
        ///     Apply the layout rule to the addressable settings.
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="doSetup">
        ///     If true, setup rules before providing.
        ///     When you call this method multiple times, set this false and call <see cref="Setup" /> before.
        ///     If you call this method only once, set this true and don't call <see cref="Setup" />.
        /// </param>
        /// <param name="versionExpression"></param>
        /// <returns>
        ///     If the layout rule was applied to the addressable asset system, return true.
        ///     Returns false if no suitable layout rule was found.
        /// </returns>
        public bool TryAddEntry(string assetPath, bool doSetup, string versionExpression = null)
        {
            var assetGuid = _assetDatabaseAdapter.AssetPathToGUID(assetPath);
            var assetType = _assetDatabaseAdapter.GetMainAssetTypeAtPath(assetPath);
            var isFolder = _assetDatabaseAdapter.IsValidFolder(assetPath);

            // If the layout rule was not found, return false.
            if (!_layoutRule.TryProvideAddressAndAddressableGroup(assetPath, assetType, isFolder, doSetup,
                    out var address,
                    out var addressableGroup))
                return false;

            // If the layout rule is found but the addressable asset group has already been destroyed, return false.
            if (addressableGroup == null)
                return false;

            var addressableGroupName = addressableGroup.Name;

            // Check the version if it is specified.
            if (!string.IsNullOrEmpty(versionExpression))
            {
                var comparator = _versionExpressionParser.CreateComparator(versionExpression);
                var versionText = _layoutRule.ProvideVersion(assetPath, assetType, isFolder, doSetup);

                if (string.IsNullOrEmpty(versionText) && _layoutRule.Settings.ExcludeUnversioned.Value)
                    return false;

                // If the version is not satisfied, return false.
                if (!string.IsNullOrEmpty(versionText)
                    && Version.TryCreate(versionText, out var version)
                    && !comparator.IsSatisfied(version))
                    return false;
            }

            // Set group and address.
            var entryAdapter = _addressableSettingsAdapter.CreateOrMoveEntry(addressableGroupName, assetGuid);
            entryAdapter.SetAddress(address);

            UpdateAssetLabel(entryAdapter, assetPath, assetType, isFolder, doSetup);

            return true;
        }

        internal void UpdateAssetLabel(IAddressableAssetEntryAdapter entryAdapter, string assetPath, System.Type assetType, bool isFolder, bool doSetup)
        {
            // Add labels to addressable settings if not exists.
            var labels = _layoutRule.ProvideLabels(assetPath, assetType, isFolder, doSetup);
            var addressableLabels = _addressableSettingsAdapter.GetLabels();
            foreach (var label in labels)
                if (!addressableLabels.Contains(label))
                    _addressableSettingsAdapter.AddLabel(label);

            // Remove old labels.
            var oldLabels = entryAdapter.Labels.ToArray();
            foreach (var label in oldLabels)
                entryAdapter.SetLabel(label, false);

            // Add new labels.
            foreach (var label in labels)
                entryAdapter.SetLabel(label, true);
        }
    }
}
