using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.AddressableAssets.Settings;

namespace SmartAddresser.Editor.Foundation.AddressableAdapter
{
    public sealed class AddressableAssetSettingsAdapter : IAddressableAssetSettingsAdapter
    {
        private readonly AddressableAssetSettings _settings;

        // Note: internal void RemoveAssetEntries(IEnumerable<AddressableAssetEntry> removeEntries, bool postEvent = true)
        private readonly MethodInfo _removeAssetEntriesMethod;
        // Note: internal void CreateOrMoveEntries(IEnumerable guids, AddressableAssetGroup targetParent, List<AddressableAssetEntry> createdEntries, List<AddressableAssetEntry> movedEntries, bool readOnly = false, bool postEvent = true)
        private readonly MethodInfo _createOrMoveEntriesMethod;

        public AddressableAssetSettingsAdapter(AddressableAssetSettings settings)
        {
            _settings = settings;
            
            _removeAssetEntriesMethod = typeof(AddressableAssetSettings).GetMethod("RemoveAssetEntries", BindingFlags.Instance | BindingFlags.NonPublic);
            _createOrMoveEntriesMethod = typeof(AddressableAssetSettings).GetMethod("CreateOrMoveEntries", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        /// <inheritdoc />
        public IAddressableAssetEntryAdapter FindAssetEntry(string guid)
        {
            var entry = _settings.FindAssetEntry(guid);
            return entry == null ? null : new AddressableAssetEntryAdapter(entry);
        }

        /// <inheritdoc />
        public IAddressableAssetEntryAdapter CreateOrMoveEntry(string groupName, string guid)
        {
            var group = _settings.FindGroup(groupName);
            var entry = _settings.CreateOrMoveEntry(guid, group);
            return entry == null ? null : new AddressableAssetEntryAdapter(entry);
        }

        public IEnumerable<IAddressableAssetEntryAdapter> CreateOrMoveEntries(string groupName, IEnumerable<string> guids)
        {
            var group = _settings.FindGroup(groupName);
            var createdEntries = new List<AddressableAssetEntry>();
            var movedEntries = new List<AddressableAssetEntry>();
            var parameters = new object[] {guids, group, createdEntries, movedEntries, };
            _createOrMoveEntriesMethod.Invoke(_settings, parameters);
            
            return createdEntries
                .Concat(movedEntries)
                .Where(entry=> entry != null)
                .Select(entry => new AddressableAssetEntryAdapter(entry));
        }

        /// <inheritdoc />
        public bool RemoveEntry(string guid)
        {
            return _settings.RemoveAssetEntry(guid);
        }

        /// <inheritdoc />
        public void RemoveAllEntries(string groupName)
        {
            var group = _settings.groups.FirstOrDefault(x => x.Name == groupName);
            if (group == null)
                throw new InvalidOperationException($"Specified group '{groupName}' was not found.");

            // Note: Bulk delete is internal, so call via Reflection
            var entries = group.entries.ToArray();
            var parameters = new object[] { entries, };
            _removeAssetEntriesMethod.Invoke(group, parameters);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetLabels()
        {
            return _settings.GetLabels();
        }

        /// <inheritdoc />
        public void AddLabel(string label)
        {
            _settings.AddLabel(label);
        }

        /// <inheritdoc />
        public void RemoveLabel(string label)
        {
            _settings.RemoveLabel(label);
        }
    }
}
