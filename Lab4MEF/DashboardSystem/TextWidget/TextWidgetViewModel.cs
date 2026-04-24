using System.ComponentModel.Composition;
using System.Windows;
using Contracts;
using Prism.Events;

namespace TextWidget
{
    [Export(typeof(IWidget))]
    [ExportMetadata("Name", "Analizator Tekstu")]
    public class TextWidgetViewModel : IWidget
    {
        public string Name => "Analizator Tekstu";
        public object View { get; }

        private readonly TextWidgetView _view;

        [ImportingConstructor]
        public TextWidgetViewModel(IEventAggregator eventAggregator)
        {
            _view = new TextWidgetView();
            View = _view;

            eventAggregator.GetEvent<DataSubmittedEvent>().Subscribe(OnDataReceived);
        }

        private void OnDataReceived(string payload)
        {
            int charCount = payload?.Length ?? 0;
            int wordCount = string.IsNullOrWhiteSpace(payload)
                ? 0
                : payload.Split(new[] { ' ', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries).Length;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _view.WordCountTxt.Text = wordCount.ToString();
                _view.CharCountTxt.Text = charCount.ToString();
            });
        }
    }
}