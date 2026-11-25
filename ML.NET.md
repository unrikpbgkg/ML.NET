public class CliProcessRunner
{
    public event EventHandler<string>? LineReceived;
    public event EventHandler? ProcessExited;

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
            StandardOutputEncoding = Encoding.UTF8 // Решение проблемы кодировки
        };

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (s, e) => 
        {
            if (e.Data != null) 
            {
                // Очистка от управляющих символов \r
                var cleanLine = e.Data.Replace("\r", "").Trim();
                LineReceived?.Invoke(this, cleanLine);
            }
        };
        process.ErrorDataReceived += (s, e) => 
        {
            if (e.Data != null) LineReceived?.Invoke(this, $"[ERR] {e.Data}");
        };

        process.Exited += (s, e) => ProcessExited?.Invoke(this, EventArgs.Empty);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(token);
        return process.ExitCode;
    }
}
