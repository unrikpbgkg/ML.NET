using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;

public partial class MainViewModel : ObservableObject
{
    private readonly CliProcessRunner _processRunner = new();

    [ObservableProperty] private string _datasetPath = string.Empty;
    [ObservableProperty] private int _labelCol = 1;
    [ObservableProperty] private bool _hasHeader = false;
    [ObservableProperty] private string _modelName = "SentimentModel";
    [ObservableProperty] private int _trainTime = 60;
    [ObservableProperty] private string _outputDir = string.Empty;
    [ObservableProperty] private string _verbosity = "minimal";

    [ObservableProperty] private bool _isTraining;
    [ObservableProperty] private bool _canStartTraining;

    [ObservableProperty] private ObservableCollection<string> _logLines = new();

    public MainViewModel()
    {
        // Подписка на вывод процесса
        _processRunner.LineReceived += OnLineReceived;
        _processRunner.ProcessExited += OnProcessExited;
        
        // Первичная проверка возможности запуска
        UpdateCanStartTraining();
    }

    [RelayCommand]
    private async Task TrainAsync(CancellationToken token)
    {
        IsTraining = true;
        LogLines.Clear();
        
        var args = MlNetCommandBuilder.BuildClassificationArgs(this);
        AppendLog($"[CMD] mlnet {args}");

        try
        {
            // Запуск процесса. Путь к mlnet берется из PATH
            int exitCode = await _processRunner.RunAsync("mlnet", args, token);
            
            if (exitCode != 0)
            {
                AppendLog($"[FATAL] Процесс mlnet завершился с кодом {exitCode}. Проверьте параметры и лог выше.");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"[EXCEPTION] {ex.Message}");
        }
        finally 
        {
            // Состояние сбросится также в событии OnProcessExited, 
            // но это страховка на случай синхронных исключений.
            IsTraining = false; 
        }
    }

    [RelayCommand]
    private void StopTraining()
    {
        _processRunner.RequestStop();
    }

    private void OnLineReceived(object? sender, string line)
    {
        // Переключение в поток UI для обновления коллекции
        Application.Current.Dispatcher.Invoke(() => AppendLog(line));
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() => IsTraining = false);
    }

    private void AppendLog(string line)
    {
        LogLines.Add(line);
        // Логика автоскролла реализуется в коде-беhind View или через Behavior
    }

    partial void OnDatasetPathChanged(string value)
    {
        // Если папка вывода не задана, ставим ее рядом с датасетом
        if (string.IsNullOrWhiteSpace(OutputDir) && !string.IsNullOrWhiteSpace(value))
        {
            OutputDir = Path.Combine(Path.GetDirectoryName(value)!, "output");
        }
        UpdateCanStartTraining();
    }

    partial void OnLabelColChanged(int value) => UpdateCanStartTraining();

    private void UpdateCanStartTraining()
    {
        CanStartTraining = !IsTraining && 
                           !string.IsNullOrWhiteSpace(DatasetPath) && 
                           LabelCol >= 0;
    }
}

public static class MlNetCommandBuilder
{
    public static string BuildClassificationArgs(MainViewModel vm)
    {
        // Используем StringBuilder для эффективности
        var sb = new StringBuilder("classification");
        
        sb.Append($" --dataset \"{vm.DatasetPath}\"");
        sb.Append($" --label-col {vm.LabelCol}");
        sb.Append($" --has-header {(vm.HasHeader ? "true" : "false")}");
        sb.Append($" --name {vm.ModelName}");
        sb.Append($" --train-time {vm.TrainTime}");
        
        if (!string.IsNullOrWhiteSpace(vm.OutputDir))
        {
            sb.Append($" --output \"{vm.OutputDir}\"");
        }
        
        if (!string.IsNullOrWhiteSpace(vm.Verbosity))
        {
            sb.Append($" --verbosity {vm.Verbosity}");
        }

        return sb.ToString();
    }
}

<Window.DataContext>
    <local:MainViewModel/>
</Window.DataContext>

<Grid Margin="10">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <!-- Панель параметров -->
    <StackPanel Grid.Row="0" Orientation="Horizontal" Margin="0,0,0,10" VerticalAlignment="Top">
        <Button Content="Выбрать файл..." Command="{Binding SelectFileCommand}" Margin="0,0,10,0"/>
        <TextBox Text="{Binding DatasetPath, UpdateSourceTrigger=PropertyChanged}" Width="400" IsReadOnly="True"/>
        
        <Label Content="Колонка метки:" Margin="10,0,5,0"/>
        <ComboBox Width="60" 
                  ItemsSource="{Binding AvailableLabelCols}" 
                  SelectedValue="{Binding LabelCol}"/>
        
        <CheckBox Content="Есть заголовок" IsChecked="{Binding HasHeader}" Margin="10,0,0,0"/>
        
        <Button Content="Обучить" 
                Command="{Binding TrainCommand}" 
                Margin="10,0,5,0" 
                IsEnabled="{Binding CanStartTraining}"/>
        <Button Content="Стоп" 
                Command="{Binding StopCommand}" 
                IsEnabled="{Binding IsTraining}"/>
    </StackPanel>

    <!-- Лог выполнения -->
    <ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto" Height="200">
        <TextBox IsReadOnly="True" 
                 TextWrapping="Wrap" 
                 VerticalScrollBarVisibility="Disabled"
                 FontFamily="Consolas" 
                 FontSize="12">
            <TextBox.Text>
                <MultiBinding StringFormat="{}{0}{1}">
                    <Binding Path="Text"/>
                    <MultiBinding Converter="{local:LogLinesConverter}">
                        <Binding Path="LogLines"/>
                    </MultiBinding>
                </MultiBinding>
            </TextBox.Text>
        </TextBox>
    </ScrollViewer>

    <!-- Панель результатов (заглушка для следующих шагов) -->
    <Border Grid.Row="2" BorderBrush="LightGray" BorderThickness="1" Margin="0,10,0,0" Visibility="Collapsed">
        <!-- Здесь будут метрики и форма классификации -->
    </Border>
</Grid>
