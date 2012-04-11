using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using EditorUtils;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace HighlightCommentKinds
{
    internal sealed class CommentTagger : AsyncTaggerSource<string, TextMarkerTag>
    {
        static readonly Regex s_commentRegex = new Regex(@"\/\/\s*(?<word>\w+)");
        static readonly TextMarkerTag s_tag = new TextMarkerTag(Constants.HighlightCommentKindsTagName);

        internal CommentTagger(ITextView textView)
            : base(textView)
        {

        }

        /// <summary>
        /// No data really needed here.  Just pass the empty string
        /// </summary>
        protected override string GetDataForSpan(SnapshotSpan span)
        {
            return String.Empty;
        }

        protected override ReadOnlyCollection<ITagSpan<TextMarkerTag>> GetTagsInBackground(string data, SnapshotSpan span, CancellationToken cancellationToken)
        {
            var lineRange = SnapshotLineRange.CreateForSpan(span);
            var list = new List<ITagSpan<TextMarkerTag>>();
            foreach (var snapshotLine in lineRange.Lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var commentSpan = MatchLine(snapshotLine);
                if (commentSpan.HasValue)
                {
                    var tagSpan = new TagSpan<TextMarkerTag>(commentSpan.Value, s_tag);
                    list.Add(tagSpan);
                }
            }

            return list.ToReadOnlyCollectionShallow();
        }

        private SnapshotSpan? MatchLine(ITextSnapshotLine snapshotLine)
        {
            var text = snapshotLine.GetText();
            var match = s_commentRegex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            var group = match.Groups["word"];
            if (!Constants.Words.Contains(group.Value))
            {
                return null;
            }

            var startPoint = snapshotLine.Start.Add(group.Index);
            return new SnapshotSpan(startPoint, snapshotLine.End);
        }
    }
}
