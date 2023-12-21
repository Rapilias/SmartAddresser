using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace SmartAddresser.Editor.Foundation.AddressableAdapter
{
    public sealed class AddressableAssetEntryAdapter : IAddressableAssetEntryAdapter
    {
        private readonly AddressableAssetEntry _entry;

        public AddressableAssetEntryAdapter(AddressableAssetEntry entry)
        {
            _entry = entry;
        }
        
        /// <inheritdoc />
        public string Address => _entry.address;

        /// <inheritdoc />
        public HashSet<string> Labels => _entry.labels;
        
        /// <inheritdoc />
        public string GroupName => _entry.parentGroup.Name;

        /// <inheritdoc />
        public void SetAddress(string address)
        {
            if(string.Compare(_entry.address, address, StringComparison.Ordinal) == 0)
                return;
            _entry.SetAddress(address);
        }
        
        /// <inheritdoc />
        public bool SetLabel(string label, bool enable)
        {
            if(_entry.labels.Contains(label) == enable)
                return true;
            return _entry.SetLabel(label, enable);
        }
    }
}
