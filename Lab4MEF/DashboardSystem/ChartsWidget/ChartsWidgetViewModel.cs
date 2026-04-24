using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using Contracts;
using Prism.Events;

namespace ChartsWidget
{
    [Export(typeof(IWidget))]
    [ExportMetadata("Name", "Wykres Słupkowy")]
    public class ChartsWidgetViewModel : IWidget
    {
        public string Name => "Wykres Słupkowy";
        public object View { get; }
        private readonly ChartsWidgetView _view;

        [ImportingConstructor]
        public ChartsWidgetViewModel(IEventAggregator eventAggregator)
        {
            _view = new ChartsWidgetView();
            View = _view;

            
            eventAggregator.GetEvent<DataSubmittedEvent>().Subscribe(data =>
            {
                
                Application.Current.Dispatcher.Invoke(() => UpdateChart(data));
            });
        }

        private void UpdateChart(string rawData)
        {
            _view.ChartCanvas.Children.Clear();

           
            var values = rawData.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => double.TryParse(s, out var v) ? v : 0)
                               .Where(v => v > 0) 
                               .ToList();

            double xOffset = 10;
            foreach (var val in values)
            {
                var bar = new Rectangle
                {
                    Fill = Brushes.Orange,
                    Width = 25,
                    Height = val * 3, 
                    Stroke = Brushes.DarkOrange,
                    StrokeThickness = 1
                };

               
                Canvas.SetLeft(bar, xOffset);
                Canvas.SetBottom(bar, 0);

                _view.ChartCanvas.Children.Add(bar);
                xOffset += 35; 
            }
        }
    }
}