using System;
using System.Collections.Generic;
using SmartAddresser.Editor.Core.Models.LayoutRules.VersionRules;
using SmartAddresser.Editor.Core.Tools.Addresser.Shared;
using SmartAddresser.Editor.Foundation.CommandBasedUndo;
using SmartAddresser.Editor.Foundation.TinyRx;
using SmartAddresser.Editor.Foundation.TinyRx.ObservableCollection;

namespace SmartAddresser.Editor.Core.Tools.Addresser.LayoutRuleEditor.VersionRuleEditor
{
    /// <summary>
    ///     Presenter for <see cref="VersionRuleListView" />.
    /// </summary>
    internal sealed class VersionRuleListPresenter : IDisposable
    {
        private readonly AutoIncrementHistory _history;

        private readonly Dictionary<string, VersionRuleListTreeView.Item> _ruleIdToTreeViewItem =
            new Dictionary<string, VersionRuleListTreeView.Item>();

        private readonly IAssetSaveService _saveService;

        private readonly CompositeDisposable _setupViewDisposables = new CompositeDisposable();

        private readonly VersionRuleListView _view;
        private readonly CompositeDisposable _viewEventDisposables = new CompositeDisposable();

        private bool _didSetupView;
        private IObservableList<VersionRule> _rules;

        public VersionRuleListPresenter(VersionRuleListView view, AutoIncrementHistory history,
            IAssetSaveService saveService)
        {
            _view = view;
            _history = history;
            _saveService = saveService;

            SetupViewEventHandlers();
        }

        public void Dispose()
        {
            CleanupView();
            CleanupViewEventHandlers();
        }

        public void SetupView(IObservableList<VersionRule> rules)
        {
            CleanupView();

            _rules = rules;

            rules.ObservableAdd.Subscribe(x => AddRuleView(x.Value, x.Index)).DisposeWith(_setupViewDisposables);
            rules.ObservableRemove.Subscribe(x => RemoveRuleView(x.Value)).DisposeWith(_setupViewDisposables);
            rules.ObservableClear.Subscribe(_ => ClearViews()).DisposeWith(_setupViewDisposables);
            foreach (var rule in rules)
                AddRuleView(rule);
            _view.TreeView.Reload();

            _didSetupView = true;

            #region Local methods

            void AddRuleView(VersionRule rule, int index = -1, bool reload = true)
            {
                var item = _view.TreeView.AddItem(rule, index);
                _ruleIdToTreeViewItem.Add(rule.Id, item);
                if (reload)
                    _view.TreeView.Reload();
            }

            void RemoveRuleView(VersionRule rule)
            {
                var item = _ruleIdToTreeViewItem[rule.Id];
                _ruleIdToTreeViewItem.Remove(rule.Id);
                _view.TreeView.RemoveItem(item.id);
                _view.TreeView.Reload();
            }

            void ClearViews()
            {
                _view.TreeView.ClearItems();
                _view.TreeView.Reload();
                _ruleIdToTreeViewItem.Clear();
            }

            #endregion
        }

        public void CleanupView()
        {
            _setupViewDisposables.Clear();
            _view.TreeView.ClearItems();
            _view.TreeView.Reload();
            _ruleIdToTreeViewItem.Clear();

            _didSetupView = false;
        }

        private void SetupViewEventHandlers()
        {
            _view.AddButtonClickedAsObservable.Subscribe(x => AddRule()).DisposeWith(_viewEventDisposables);

            #region Local methods

            void AddRule()
            {
                if (!_didSetupView)
                    return;

                var rule = new VersionRule();
                _history.Register($"Add Version Rule {rule.Id}", () =>
                {
                    _rules.Add(rule);
                    _saveService.Save();
                }, () =>
                {
                    _rules.Remove(rule);
                    _saveService.Save();
                });
            }

            #endregion
        }

        private void CleanupViewEventHandlers()
        {
            _viewEventDisposables.Clear();
        }
    }
}
