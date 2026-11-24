private async Task<string> ClassifyViaPredictionEngineAsync(string text)
{
    // Путь к ZIP-модели, которую создал mlnet
    var modelPath = Path.Combine(
        Path.GetDirectoryName(TrainingResult.GeneratedProjectPath)!, 
        ModelName, 
        $"{ModelName}.zip");

    if (!File.Exists(modelPath)) return "Файл модели не найден.";

    await Task.Run(() =>
    {
        var mlContext = new MLContext();
        var model = mlContext.Model.Load(modelPath, out _);
        var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
        
        var input = new ModelInput { Text = text };
        var result = predEngine.Predict(input);
        
        // Находим максимальную вероятность
        var maxProb = result.Score.Max();
        var labelIndex = Array.IndexOf(result.Score, maxProb);
        
        // Для получения строкового значения метки потребуется загрузить IDataView из обучения
        // или просто вернуть индекс, если метки были числовыми. 
        // Упрощенный вариант:
        PredictionResult = $"Prediction: Index_{labelIndex}, Probability: {maxProb:F4}";
    });

    return PredictionResult;
}

// Внутри блока try/catch метода ClassifyAsync
if (exitCode == 0)
{
    PredictionResult = ParsePredictionOutput(output.ToString());
}
else 
{
    AppendLog("[WARN] Запуск внешнего приложения не удался. Пробуем встроенный движок.");
    PredictionResult = await ClassifyViaPredictionEngineAsync(InputText);
}
