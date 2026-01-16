using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows; // MessageBox 사용을 위해 필요
using TransLearn.Data;

namespace TransLearn.Services.Analysis
{
    // 외부 Python 스크립트와 통신을 담당하는 서비스
    public class PythonAnalysisService
    {
        private readonly string _pythonPath;
        private readonly string _scriptPath;

        public PythonAnalysisService(string pythonPath, string scriptPath)
        {
            _pythonPath = pythonPath;
            _scriptPath = scriptPath;
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

                // [핵심 수정] 한글 윈도우에서 인코딩 오류(UnicodeDecodeError) 방지
                // 파이썬과 C#이 서로 UTF-8로만 대화하도록 강제합니다.
                StandardInputEncoding = System.Text.Encoding.UTF8,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();

                    // 1. 파이썬에게 텍스트 보내기 (UTF-8)
                    await process.StandardInput.WriteAsync(text);
                    process.StandardInput.Close(); // 입력 끝!

                    // 2. 결과 및 에러 읽기
                    string jsonResult = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    // 3. 치명적인 에러 체크
                    // (파이썬 쪽에서 경고를 무시하도록 했지만, 만약 심각한 에러가 나서 결과(json)가 없다면 경고창 띄움)
                    if (!string.IsNullOrEmpty(error) && string.IsNullOrEmpty(jsonResult))
                    {
                        MessageBox.Show($"파이썬 스크립트 오류:\n{error}");
                        return new List<LearnDataEntry>();
                    }

                    // 4. 결과가 비어있으면 빈 리스트 반환
                    if (string.IsNullOrWhiteSpace(jsonResult))
                    {
                        return new List<LearnDataEntry>();
                    }

                    // 5. JSON 변환 (대소문자 무시)
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<List<LearnDataEntry>>(jsonResult, options) ?? new List<LearnDataEntry>();
                }
                catch (Exception ex)
                {
                    // C# 내부에서 프로세스 실행 자체가 실패했을 때
                    MessageBox.Show($"파이썬 실행 실패: {ex.Message}");
                    return new List<LearnDataEntry>();
                }
            }
        }
    }
}