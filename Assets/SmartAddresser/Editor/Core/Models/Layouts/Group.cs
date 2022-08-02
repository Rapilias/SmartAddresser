using System;
using System.Collections.Generic;
using SmartAddresser.Editor.Core.Models.Shared;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SmartAddresser.Editor.Core.Models.Layouts
{
    [Serializable]
    public sealed class Group
    {
        [SerializeField] private string _id;
        [SerializeField] private AddressableAssetGroup _addressableGroup;
        [SerializeField] private LayoutErrorType _errorType;
        [SerializeField] private List<Entry> _entries = new List<Entry>();

        public Group(AddressableAssetGroup addressableGroup)
        {
            _id = IdentifierFactory.Create();
            _addressableGroup = addressableGroup;
        }

        public string Id => _id;
        public AddressableAssetGroup AddressableGroup => _addressableGroup;

        /// <summary>
        ///     Error type of the group.
        ///     This matches the most critical error type of all entries in the group.
        /// </summary>
        public LayoutErrorType ErrorType => _errorType;

        public List<Entry> Entries => _entries;

        public string DisplayName => _addressableGroup == null ? "[Missing Reference]" : _addressableGroup.name;

        internal void RefreshErrorType()
        {
            _errorType = LayoutErrorType.None;
            for (int i = 0, entryCount = _entries.Count; i < entryCount; i++)
            {
                var entry = _entries[i];
                entry.RefreshErrorTypeAndMessages();
                if (entry.ErrorType.IsMoreCriticalThan(_errorType))
                    _errorType = entry.ErrorType;
            }
        }
    }
}