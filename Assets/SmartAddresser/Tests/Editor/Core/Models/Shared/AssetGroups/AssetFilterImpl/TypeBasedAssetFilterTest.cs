using NUnit.Framework;
using SmartAddresser.Editor.Core.Models.Shared.AssetGroups;
using SmartAddresser.Editor.Core.Models.Shared.AssetGroups.AssetFilterImpl;
using UnityEngine;

namespace SmartAddresser.Tests.Editor.Core.Models.Shared.AssetGroups.AssetFilterImpl
{
    internal sealed class TypeBasedAssetFilterTest
    {
        [TestCase(arg: false, ExpectedResult = true)]
        [TestCase(arg: true, ExpectedResult = false)]
        public bool IsMatch_SetMatchedType(bool ignoreFilter)
        {
            var filter = new TypeBasedAssetFilter();
            filter.Type.Value = TypeReference.Create(typeof(Texture2D));
            filter.IgnoreFilter = ignoreFilter;
            filter.SetupForMatching();
            return filter.IsMatch("Assets/Test.png", typeof(Texture2D), false);
        }
        
        [TestCase(arg: false, ExpectedResult = true)]
        [TestCase(arg: true, ExpectedResult = false)]
        public bool IsMatch_SetDerivedType(bool ignoreFilter)
        {
            var filter = new TypeBasedAssetFilter();
            filter.Type.Value = TypeReference.Create(typeof(Texture));
            filter.IgnoreFilter = ignoreFilter;
            filter.SetupForMatching();
            return filter.IsMatch("Assets/Test.png", typeof(Texture2D), false);
        }
        
        [TestCase(arg: false, ExpectedResult = false)]
        [TestCase(arg: true, ExpectedResult = true)]
        public bool IsMatch_SetNotMatchedType(bool ignoreFilter)
        {
            var filter = new TypeBasedAssetFilter();
            filter.Type.Value = TypeReference.Create(typeof(Texture3D));
            filter.IgnoreFilter = ignoreFilter;
            filter.SetupForMatching();
            return filter.IsMatch("Assets/Test.png", typeof(Texture2D), false);
        }

        [TestCase(arg: false, ExpectedResult = true)]
        [TestCase(arg: true, ExpectedResult = false)]
        public bool IsMatch_ContainsMatched(bool ignoreFilter)
        {
            var filter = new TypeBasedAssetFilter();
            filter.Type.IsListMode = true;
            filter.Type.AddValue(TypeReference.Create(typeof(Texture3D)));
            filter.Type.AddValue(TypeReference.Create(typeof(Texture2D)));
            filter.IgnoreFilter = ignoreFilter;
            filter.SetupForMatching();
            return filter.IsMatch("Assets/Test.png", typeof(Texture2D), false);
        }

        [TestCase(arg: false, ExpectedResult = false)]
        [TestCase(arg: true, ExpectedResult = true)]
        public bool IsMatch_NotContainsMatched(bool ignoreFilter)
        {
            var filter = new TypeBasedAssetFilter();
            filter.Type.IsListMode = true;
            filter.Type.AddValue(TypeReference.Create(typeof(Texture3D)));
            filter.Type.AddValue(TypeReference.Create(typeof(Texture2D)));
            filter.IgnoreFilter = ignoreFilter;
            filter.SetupForMatching();
            return filter.IsMatch("Assets/Test.png", typeof(Texture2DArray), false);
        }
    }
}
