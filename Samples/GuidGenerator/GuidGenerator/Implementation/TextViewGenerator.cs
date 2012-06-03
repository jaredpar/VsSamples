using System;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace VsSamples.GuidGenerator.Implementation
{
    internal sealed class TextViewGenerator
    {
        private const string GenerateKey = "#new-guid";
        private readonly ITextView _textView;
        private readonly ITextBuffer _textBuffer;
        private Span? _changeSpan;
        private StringComparer _comparer = StringComparer.OrdinalIgnoreCase;

        internal TextViewGenerator(ITextView textView)
        {
            _textView = textView;
            _textBuffer = textView.TextBuffer;
            _textBuffer.Changed += OnChanged;
            _textBuffer.PostChanged += OnPostChanged;
        }

        internal void Close()
        {
            _textBuffer.Changed -= OnChanged;
            _textBuffer.PostChanged -= OnPostChanged;
        }

        private void OnChanged(object sender, TextContentChangedEventArgs e)
        {
            // This extension doesn't handle undo / redo.  Only supports the user typing
            // forwards for the generate key
            if (e.AfterVersion.VersionNumber != e.AfterVersion.ReiteratedVersionNumber)
            {
                _changeSpan = null;
                return;
            }

            // Multiple changes can occur in a extension generated edit or when a projection
            // buffer edit crosses ITextBuffer boundaries 
            if (e.Changes.Count != 1)
            {
                _changeSpan = null;
                return;
            }

            var change = e.Changes[0];

            // Only support typing forward.  
            if (change.Delta != 1)
            {
                _changeSpan = null;
                return;
            }

            if (_changeSpan.HasValue)
            {
                // If the user is typing forward then the change should begin at the end of the 
                // Span value that we are tracking
                var changeSpan = _changeSpan.Value;
                if (change.NewPosition != changeSpan.End)
                {
                    _changeSpan = null;
                }
                else
                {
                    _changeSpan = new Span(changeSpan.Start, changeSpan.Length + 1);
                }
            }
            else
            {
                // It's a new edit so start tracking it
                _changeSpan = new Span(change.NewPosition, 1);
            }
        }

        private void OnPostChanged(object sender, EventArgs e)
        {
            if (!_changeSpan.HasValue)
            {
                return;
            }

            var changeSpan = _changeSpan.Value;
            var snapshot = _textBuffer.CurrentSnapshot;
            var text = snapshot.GetText(changeSpan);
            if (text.Length == GenerateKey.Length && _comparer.Equals(text, GenerateKey))
            {
                InsertGuid(changeSpan);
                _changeSpan = null;
            }
            else if (text.Length >= GenerateKey.Length)
            {
                _changeSpan = null;
            }
            else if (!GenerateKey.StartsWith(text, StringComparison.OrdinalIgnoreCase))
            {
                _changeSpan = null;
            }
        }

        private void InsertGuid(Span changeSpan)
        {
            // TODO: Need to properly handle undo / redo 
            var replace = Guid.NewGuid().ToString();
            var snapshot = _textBuffer.Replace(changeSpan, replace);

            var caretPoint = new SnapshotPoint(snapshot, changeSpan.Start + replace.Length);
            _textView.Caret.MoveTo(caretPoint);
        }
    }
}
