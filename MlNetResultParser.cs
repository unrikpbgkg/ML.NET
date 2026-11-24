public static class MlNetResultParser
{
    // Регулярное выражение для поиска метрик (пример: "Accuracy: 0.9234")
    private static readonly Regex MetricRegex = new(@"^(Accuracy|AUC|LogLoss|Training time): ([\d.]+)", 
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Поиск пути к сгенерированному проекту
    private static readonly Regex ProjectPathRegex = new(@"console app at:\s+(.+\.csproj)", 
        RegexOptions.Compiled);

    public static TrainingResult Parse(List<string> logLines)
    {
        var result = new TrainingResult();

        // 1. Ищем путь к проекту (ищем снизу вверх, так как это финальное сообщение)
        for (int i = logLines.Count - 1; i >= 0; i--)
        {
            var match = ProjectPathRegex.Match(logLines[i]);
            if (match.Success)
            {
                result.GeneratedProjectPath = match.Groups[1].Value.Trim();
                break;
            }
        }

        // 2. Ищем финальные метрики (они обычно выводятся в блоке "Top 1")
        for (int i = logLines.Count - 1; i >= 0; i--)
        {
            if (logLines[i].Contains("* Top 1:"))
            {
                // Проверяем следующие 5-10 строк после "Top 1"
                for (int j = i + 1; j < i + 10 && j < logLines.Count; j++)
                {
                    var metricMatch = MetricRegex.Match(logLines[j]);
                    if (metricMatch.Success)
                    {
                        var name = metricMatch.Groups[1].Value;
                        var value = double.Parse(metricMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                        result.Metrics[name] = value;
                    }
                }
                break;
            }
        }

        return result;
    }
}

public class TrainingResult
{
    public string? GeneratedProjectPath { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
}

// Внутри MainViewModel.cs

[ObservableProperty] private TrainingResult _trainingResult = new();
[ObservableProperty] private ObservableCollection<MetricViewModel> _metricsView = new();

private async void OnProcessExited(object? sender, EventArgs e)
{
    Application.Current.Dispatcher.Invoke(() => IsTraining = false);

    // Парсим результат
    var result = MlNetResultParser.Parse(LogLines.ToList());
    
    if (result.GeneratedProjectPath != null)
    {
        TrainingResult = result;
        
        // Отображаем метрики в UI
        Application.Current.Dispatcher.Invoke(() =>
        {
            MetricsView.Clear();
            foreach (var metric in result.Metrics)
            {
                MetricsView.Add(new MetricViewModel { Name = metric.Key, Value = metric.Value });
            }
        });

        // Запускаем модификацию Program.cs в фоне, чтобы не блокировать UI
        await Task.Run(() => PrepareGeneratedAppForPrediction(result));
    }
}

private void PrepareGeneratedAppForPrediction(TrainingResult result)
{
    var projectDir = Path.GetDirectoryName(result.GeneratedProjectPath);
    if (projectDir == null) return;

    var programCsPath = Path.Combine(projectDir, "Program.cs");
    
    // Вызов модуля из предыдущего плана (Sprint 3)
    if (GeneratedAppModifier.PatchProgramCs(programCsPath))
    {
        AppendLog("[INFO] Program.cs успешно модифицирован для чтения аргументов.");
        
        // Собираем проект, чтобы получить .exe
        var buildArgs = $"build --configuration Release --project \"{result.GeneratedProjectPath}\"";
        var buildRunner = new CliProcessRunner();
        
        // Подписка на вывод билда не обязательна, но полезна для отладки
        buildRunner.LineReceived += (s, line) => AppendLog($"[BUILD] {line}");
        
        int buildExit = buildRunner.RunAsync("dotnet", buildArgs, CancellationToken.None).GetAwaiter().GetResult();
        
        if (buildExit == 0)
        {
            AppendLog("[SUCCESS] Консольное приложение собрано и готово к предсказаниям.");
        }
        else
        {
            AppendLog("[ERROR] Не удалось собрать консольное приложение.");
        }
    }
    else
    {
        AppendLog("[WARN] Не удалось найти шаблон Program.cs для модификации. Будет использован запасной вариант.");
    }
}
