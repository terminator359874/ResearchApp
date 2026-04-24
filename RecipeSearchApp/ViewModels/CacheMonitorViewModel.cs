using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RecipeSearchApp.Services;
using System.Threading.Tasks;
using System;
using Microsoft.UI.Dispatching;

namespace RecipeSearchApp.ViewModels
{
    public class CacheMonitorViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _database;
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly System.Timers.Timer _timer;

        public ObservableCollection<ISeries> HitMissSeries { get; set; } = new();

        private string _statsText = string.Empty;
        public string StatsText
        {
            get => _statsText;
            set { _statsText = value; OnPropertyChanged(); }
        }

        public CacheMonitorViewModel(DatabaseService db)
        {
            _database = db;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            HitMissSeries.Add(new PieSeries<double> { Values = new double[] { 0 }, Name = "Hits" });
            HitMissSeries.Add(new PieSeries<double> { Values = new double[] { 1 }, Name = "Misses" });

            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) => UpdateStats();
            _timer.Start();

            UpdateStats();
        }

        private void UpdateStats()
        {
            var stats = _database.GetCacheStatistics();
            
            var hits = Convert.ToInt32(stats["TotalHits"]);
            var misses = Convert.ToInt32(stats["TotalMisses"]);

            // Ensure there points in the pie chart
            if (hits == 0 && misses == 0) misses = 1;

            _dispatcherQueue.TryEnqueue(() =>
            {
                ((PieSeries<double>)HitMissSeries[0]).Values = new double[] { hits };
                ((PieSeries<double>)HitMissSeries[1]).Values = new double[] { misses };

                StatsText = $"Записей в кеше: {stats["CachedItems"]} / {stats["MaxSize"]}\n" +
                            $"Попадания (Hits): {stats["TotalHits"]}\n" +
                            $"Промахи (Misses): {stats["TotalMisses"]}\n" +
                            $"Вытеснения (Evictions): {stats["TotalEvictions"]}\n" +
                            $"Hit Rate: {Convert.ToDouble(stats["HitRate"]):P2}\n" +
                            $"Ср. время доступа: {Convert.ToDouble(stats["AverageAccessTimeMs"]):F4} мс\n\n" +
                            $"Топ рецептов:\n{stats["TopItems"]}";
            });
        }

        public void Stop() => _timer.Stop();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
