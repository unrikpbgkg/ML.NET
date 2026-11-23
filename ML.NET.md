public class MainViewModel : INotifyPropertyChanged
{
    private string _logOutput = "";
    private bool _isTraining = false;
    private string _predictionResult = "";

    public string LogOutput 
    { 
        get => _logOutput; 
        set { _logOutput = value; OnPropertyChanged(); } 
    }
    public bool IsTraining 
    { 
        get => _isTraining; 
        set { _isTraining = value; OnPropertyChanged(); } 
    }
    public string PredictionResult
    {
        get => _predictionResult;
        set { _predictionResult = value; OnPropertyChanged(); }
    }

    // Команды (RelayCommand)
    public ICommand SelectFileCommand { get; }
    public ICommand TrainCommand { get; }
    public ICommand ClassifyCommand { get; }

    // ... реализация OnPropertyChanged и конструкторов
}

public class ProcessRunner
{
    public event EventHandler<string>? OnOutputReceived;
    public event EventHandler? OnProcessExited;

    public async Task<int> RunAsync(string fileName, string arguments, CancellationToken token)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8 // Важно для корректного отображения логов
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        
        process.OutputDataReceived += (s, e) => { if (e.Data != null) OnOutputReceived?.Invoke(this, e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) OnOutputReceived?.Invoke(this, $"[ERR] {e.Data}"); };
        
        process.Exited += (s, e) => OnProcessExited?.Invoke(this, EventArgs.Empty);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        while (!process.HasExited) 
        {
            await Task.Delay(100, token);
            token.ThrowIfCancellationRequested();
        }
        
        return process.ExitCode;
    }
}

public string BuildClassificationCommand(string datasetPath, int labelCol, bool hasHeader, 
    string modelName, int trainTime, string outputPath)
{
    var sb = new StringBuilder();
    sb.Append("classification ");
    sb.Append($"--dataset \"{datasetPath}\" ");
    sb.Append($"--label-col {labelCol} ");
    sb.Append($"--has-header {hasHeader.ToString().ToLower()} ");
    sb.Append($"--name {modelName} ");
    sb.Append($"--train-time {trainTime} ");
    sb.Append($"--output \"{outputPath}\" ");
    return sb.ToString();
}

// В обработчике события OnOutputReceived
if (line.Contains("Top accuracy"))
{
    var match = Regex.Match(line, @"Top accuracy: ([\d\.]+)");
    if (match.Success)
    {
        // Обновить свойство ViewModel.Metrics.Accuracy
    }
}

// Было:
// while (reader.ReadLine() is string line)
// {
//     var prediction = predictionEngine.Predict(new SentimentData { Text = line });
//     Console.WriteLine(...);
// }

// Стало:
if (args.Length > 0)
{
    var prediction = predictionEngine.Predict(new SentimentData { Text = args[0] });
    Console.WriteLine($"Prediction: {prediction.Prediction}, Probability: {prediction.Score.Max()}");
}
else
{
    Console.WriteLine("Error: No text provided.");
}

// Путь к папке с модифицированным проектом
var projectPath = @"C:\Results\SentimentModel";
var textToClassify = "Этот фильм был просто великолепен!";

var psi = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"run --project \"{projectPath}\" --configuration Release -- \"{textToClassify}\"",
    RedirectStandardOutput = true,
    UseShellExecute = false
};

// Чтение вывода и отображение в PredictionResult
