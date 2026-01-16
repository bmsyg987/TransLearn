
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using TransLearn.Data; // For LearnDataEntry

namespace TransLearn.Services.Analysis
{
    // This service manages the interaction with the external Python analysis script.
    public class PythonAnalysisService
    {
        private readonly string _pythonPath;
        private readonly string _scriptPath;

        // The paths should be configurable in a real application.
        public PythonAnalysisService(string pythonPath, string scriptPath)
        {
            _pythonPath = pythonPath; // e.g., "C:\Python39\python.exe"
            _scriptPath = scriptPath; // e.g., "path\to\analyzer.py"
        }

        public async Task<List<LearnDataEntry>> AnalyzeTextAsync(string text)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _pythonPath,
                Arguments = $"\"{_scriptPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();

                    // Asynchronously write text to the Python script's stdin
                    await process.StandardInput.WriteAsync(text);
                    process.StandardInput.Close(); // Signal end of input

                    // Asynchronously read the results from stdout and any errors from stderr
                    string jsonResult = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(); // Modern way to wait for process exit

                    if (!string.IsNullOrEmpty(error))
                    {
                        // 에러 내용을 팝업으로 띄워줍니다!
                        System.Windows.MessageBox.Show($"파이썬 오류 발생:\n{error}");
                        return new List<LearnDataEntry>();
                    }
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"Python script error: {error}");
                        return new List<LearnDataEntry>();
                    }

                    if (string.IsNullOrEmpty(jsonResult))
                    {
                        return new List<LearnDataEntry>();
                    }

                    // Deserialize the JSON output from the script into C# objects
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<LearnDataEntry>>(jsonResult, options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to run Python analysis script: {ex.Message}");
                    return new List<LearnDataEntry>();
                }
            }
        }
    }
}
