// Пример устойчивого поиска через Regex
var mainMethodRegex = new Regex(@"static void Main$.*$\s*\{([\s\S]*?)\}", RegexOptions.Multiline);
// Ищем строку var input = ... внутри захваченной группы (тела метода)

// В Prediction Client
string fileName;
string arguments;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    fileName = exePath;
    arguments = $"\"{text}\"";
}
else
{
    fileName = "dotnet";
    arguments = $"\"{exePath}\" \"{text}\"";
}
await _processRunner.RunAsync(fileName, arguments, token);

public partial class MainViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, 100, ErrorMessage = "Индекс колонки должен быть в пределах датасета")]
    private int _labelCol;

    // Валидация пути к файлу
    partial void OnDatasetPathChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && !File.Exists(value))
        {
            SetErrorForProperty(nameof(DatasetPath), "Файл не существует.");
        }
        else
        {
            ClearErrors(nameof(DatasetPath));
        }
        UpdateCanStartTraining();
    }
}
