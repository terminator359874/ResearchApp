using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RecipeSearchApp.Services;
using System.Threading.Tasks;

namespace RecipeSearchApp.ViewModels
{
    public class DiagnosticsViewModel : INotifyPropertyChanged
    {
        private readonly DatabaseService _database;

        public ObservableCollection<BenchmarkResult> StringSearchResults { get; } = new();
        public ObservableCollection<BloomFilterBenchmarkResult> BloomFilterResults { get; } = new();
        public ObservableCollection<FTS5BenchmarkResult> Fts5Results { get; } = new();
        public ObservableCollection<TrieBenchmarkResult> TrieResults { get; } = new();

        public ISeries[] BloomSeries { get; set; } = new ISeries[0];
        public Axis[] BloomXAxes { get; set; } = new Axis[0];

        public ICommand RunStringSearchCommand { get; }
        public ICommand RunBloomCommand { get; }
        public ICommand RunFts5Command { get; }
        public ICommand RunTrieCommand { get; }

        public DiagnosticsViewModel(DatabaseService db)
        {
            _database = db;

            RunStringSearchCommand = new RelayCommand(async () => RunStringSearchBenchmarks());
            RunBloomCommand = new RelayCommand(async () => await RunBloomBenchmarks());
            RunFts5Command = new RelayCommand(async () => await RunFts5Benchmarks());
            RunTrieCommand = new RelayCommand(async () => RunTrieBenchmarks());
        }

        private void RunStringSearchBenchmarks()
        {
            var res = StringSearcher.BenchmarkSearchAlgorithms();
            StringSearchResults.Clear();
            foreach (var r in res) StringSearchResults.Add(r);
        }

        private async Task RunBloomBenchmarks()
        {
            var res = await BloomFilterBenchmarks.AnalyzeFalsePositivesAsync(_database);
            BloomFilterResults.Clear();
            
            var fpRates = new double[res.Count];
            var labels = new string[res.Count];

            for (int i=0; i<res.Count; i++)
            {
                BloomFilterResults.Add(res[i]);
                fpRates[i] = res[i].FalsePositiveRate;
                labels[i] = $"{res[i].FilterSize}b/{res[i].HashCount}h";
            }

            BloomSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Values = fpRates,
                    Fill = null,
                    Name = "False Positive Rate"
                }
            };

            BloomXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = labels,
                    Name = "Конфигурация фильтра"
                }
            };

            OnPropertyChanged(nameof(BloomSeries));
            OnPropertyChanged(nameof(BloomXAxes));
        }

        private async Task RunFts5Benchmarks()
        {
            var res = await FTS5Benchmarks.RunFTS5TestsAsync(_database);
            Fts5Results.Clear();
            foreach(var r in res) Fts5Results.Add(r);
        }

        private void RunTrieBenchmarks()
        {
            var res = TrieMemoryBenchmarks.BenchmarkTries();
            TrieResults.Clear();
            foreach(var r in res) TrieResults.Add(r);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
