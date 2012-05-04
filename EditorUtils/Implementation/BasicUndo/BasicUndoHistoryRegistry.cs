using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text.Operations;

namespace EditorUtils
{
    /// <summary>
    /// This class intentionally doesn't Export ITextUndoHistoryRegistry.  Doing that would conflict
    /// with any hosted environment which provided an ITextUndoHistoryRegistry.  This class is 
    /// intended to be a simple + non-default solution for hosts that don't
    /// </summary>
    [Export(Constants.ContractName, typeof(IBasicUndoHistoryRegistry))]
    internal sealed class BasicTextUndoHistoryRegistry : ITextUndoHistoryRegistry, IBasicUndoHistoryRegistry
    {
        private readonly ConditionalWeakTable<object, ITextUndoHistory> _map = new ConditionalWeakTable<object, ITextUndoHistory>();

        #region ITextUndoHistoryRegistry

        void ITextUndoHistoryRegistry.AttachHistory(object context, ITextUndoHistory history)
        {
            _map.Add(context, history);
        }

        ITextUndoHistory ITextUndoHistoryRegistry.GetHistory(object context)
        {
            ITextUndoHistory history;
            _map.TryGetValue(context, out history);
            return history;
        }

        ITextUndoHistory ITextUndoHistoryRegistry.RegisterHistory(object context)
        {
            ITextUndoHistory history;
            if (!_map.TryGetValue(context, out history))
            {
                history = new BasicUndoHistory();
                _map.Add(context, history);
            }
            return history;
        }

        void ITextUndoHistoryRegistry.RemoveHistory(ITextUndoHistory history)
        {
            throw new NotImplementedException();
        }

        bool ITextUndoHistoryRegistry.TryGetHistory(object context, out ITextUndoHistory history)
        {
            return _map.TryGetValue(context, out history);
        }

        #endregion

        #region IBasciUndoHistoryRegistry

        ITextUndoHistoryRegistry IBasicUndoHistoryRegistry.TextUndoHistoryRegistry
        {
            get { return this; }
        }

        #endregion
    }
}
