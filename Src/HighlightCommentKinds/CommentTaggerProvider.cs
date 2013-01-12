using System.ComponentModel.Composition;
using EditorUtils;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace HighlightCommentKinds
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(Constants.CSharpContentType)]
    [TagType(typeof(TextMarkerTag))]
    internal sealed class CommentTaggerProvider : IViewTaggerProvider
    {
        private readonly ITaggerFactory _taggerFactory;
        private readonly object _key = new object();

        [ImportingConstructor]
        internal CommentTaggerProvider([Import(EditorUtils.Constants.ContractName)] ITaggerFactory taggerFactory)
        {
            _taggerFactory = taggerFactory;
        }

        ITagger<T> IViewTaggerProvider.CreateTagger<T>(ITextView textView, ITextBuffer textBuffer)
        {
            if (textView.TextBuffer != textBuffer)
            {
                return null;
            }

            var tagger = _taggerFactory.CreateAsyncTagger(
                textBuffer.Properties,
                _key,
                () => new CommentTagger(textView));
            return (ITagger<T>)tagger;
        }
    }
}
