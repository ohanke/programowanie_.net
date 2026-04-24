using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Windows;
using Contracts;
using Prism.Events;

namespace DashboardApp
{
    public partial class MainWindow : Window
    {
        // Kolekcja wtyczek z metadanymi (Zadanie pkt 1)
        [ImportMany(AllowRecomposition = true)]
        public IEnumerable<Lazy<IWidget, IWidgetMetadata>> Widgets { get; set; }

        [Import]
        private IEventAggregator _eventAggregator;

        private CompositionContainer _container;
        private DirectoryCatalog _catalog;

        public MainWindow()
        {
            InitializeComponent();
            StartMef();
        }

        private void StartMef()
        {
            // 1. Przygotowanie folderu wtyczek
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            // 2. Konfiguracja MEF i Agregatora (Shared)
            var aggregateCatalog = new AggregateCatalog();
            _catalog = new DirectoryCatalog(path);
            aggregateCatalog.Catalogs.Add(_catalog);

            _container = new CompositionContainer(aggregateCatalog);

            // Rejestrujemy Agregator Zdarzeń jako singleton dla wtyczek (Pkt 2)
            var batch = new CompositionBatch();
            batch.AddExportedValue<IEventAggregator>(new EventAggregator());

            _container.Compose(batch);
            _container.ComposeParts(this);

            // 3. Podpięcie pod GUI
            WidgetsTab.ItemsSource = Widgets;

            // 4. FileSystemWatcher - monitorowanie zmian (Pkt 1)
            var fsw = new FileSystemWatcher(path, "*.dll");
            fsw.Created += (s, e) => RefreshPlugins();
            fsw.Deleted += (s, e) => RefreshPlugins();
            fsw.EnableRaisingEvents = true;
        }

        private void RefreshPlugins()
        {
            // Odświeżanie interfejsu musi odbywać się w wątku UI (Pkt 3 - Wskazówki)
            Dispatcher.Invoke(() => {
                _catalog.Refresh();
                _container.ComposeParts(this);
                WidgetsTab.ItemsSource = null;
                WidgetsTab.ItemsSource = Widgets;
            });
        }

        private void SendData_Click(object sender, RoutedEventArgs e)
        {
            // Publikacja danych przez Agregator Zdarzeń (Pkt 1 i 3)
            if (_eventAggregator != null)
            {
                _eventAggregator.GetEvent<DataSubmittedEvent>().Publish(MyTextBox.Text);
            }
        }
    }
}