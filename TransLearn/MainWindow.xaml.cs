using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows;
using TransLearn.Data;
using TransLearn.Services;
using TransLearn.Services.Analysis;
using TransLearn.Services.Stt;

namespace TransLearn
{
    public partial class MainWindow : Window
    {
        private readonly TransLearnDao _dao;
        private readonly ScreenTranslationManager _screenManager;
        private readonly AudioTranslationManager _audioManager;
        private readonly LearningManager _learningManager;

        // 캡처 영역을 표시하는 투명 창
        private OverlayWindow _overlayWindow;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // 1. DB 초기화
                DatabaseHelper.InitializeDatabase();
                _dao = new TransLearnDao();

                // 2. 서비스 매니저들 조립
                _screenManager = new ScreenTranslationManager(_dao);

                // (STT 서비스는 현재 Mock 상태, 나중에 Azure 키 연결 필요)
                _audioManager = new AudioTranslationManager(new MockSttService(), _dao);
                _audioManager.OnNewTranslationAvailable += AudioManager_OnTranslationReceived;

                // 3. 파이썬 분석기 연결
                // [주의] 본인의 파이썬 경로가 맞는지 꼭 확인하세요!
                string myPythonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Python", "python.exe");
                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "analyzer.py");

                var analysisService = new PythonAnalysisService(myPythonPath, scriptPath);
                _learningManager = new LearningManager(analysisService, _dao);

                // 4. 투명 캡처 창 띄우기 (이제 프로그램 켜면 초록색 창도 같이 뜹니다)
                _overlayWindow = new OverlayWindow();
                _overlayWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 오류: {ex.Message}");
            }
        }

        // 프로그램이 꺼질 때 투명 창도 같이 끄기
        protected override void OnClosed(EventArgs e)
        {
            _overlayWindow?.Close();
            base.OnClosed(e);
        }

        // ==========================================
        // 탭 1: 화면 번역 (OCR) - 투명 창 영역 캡처 버전
        // ==========================================
        private async void BtnOcrStart_Click(object sender, RoutedEventArgs e)
        {
            BtnOcrStart.IsEnabled = false;
            BtnOcrStart.Content = "⏳ 분석 중...";
            TxtOcrSource.Text = "캡처 중...";
            TxtOcrTranslated.Text = "";

            try
            {
                // 1. 투명 창(OverlayWindow)의 현재 위치와 크기를 가져옵니다.
                var captureArea = _overlayWindow.GetCaptureArea();

                // 2. 백그라운드에서 캡처 및 번역 수행 (멈춤 방지)
                var result = await Task.Run(async () =>
                {
                    return await _screenManager.ProcessScreenAreaAsync(captureArea);
                });

                // 3. UI 업데이트
                if (string.IsNullOrWhiteSpace(result.sourceText))
                {
                    TxtOcrSource.Text = "⚠️ 텍스트를 발견하지 못했습니다. 초록색 창 안에 글자가 있는지 확인해주세요!";
                }
                else
                {
                    TxtOcrSource.Text = result.sourceText;
                    TxtOcrTranslated.Text = result.translatedText;

                    // 학습 데이터 분석 요청
                    _ = _learningManager.ProcessTextForLearningAsync(result.sourceText);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"OCR 오류: {ex.Message}");
            }
            finally
            {
                BtnOcrStart.IsEnabled = true;
                BtnOcrStart.Content = "▶ 화면 번역 시작 (선택 영역)";
            }
        }

        // ==========================================
        // 탭 2: 오디오 번역
        // ==========================================
        private void BtnAudioStart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _audioManager.Start();
                BtnAudioStart.IsEnabled = false;
                BtnAudioStop.IsEnabled = true;
                TxtAudioStatus.Text = "🔴 듣는 중...";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오디오 시작 오류: {ex.Message}");
            }
        }

        private void BtnAudioStop_Click(object sender, RoutedEventArgs e)
        {
            _audioManager.Stop();
            BtnAudioStart.IsEnabled = true;
            BtnAudioStop.IsEnabled = false;
            TxtAudioStatus.Text = "대기 중...";
        }

        private void AudioManager_OnTranslationReceived(string source, string translated)
        {
            Dispatcher.Invoke(() =>
            {
                string log = $"[듣기] {source}\n[번역] {translated}\n----------------\n";
                TxtAudioLog.AppendText(log);
                TxtAudioLog.ScrollToEnd();

                _ = _learningManager.ProcessTextForLearningAsync(source);
            });
        }

        // ==========================================
        // 탭 3: 학습 모드 (DB 조회)
        // ==========================================
        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 클릭된 버튼이 속한 행(Row)의 데이터를 가져옵니다.
                Button btn = sender as Button;
                System.Data.DataRowView row = btn.DataContext as System.Data.DataRowView;

                if (row != null)
                {
                    string wordToDelete = row["WordOrPhrase"].ToString();

                    if (MessageBox.Show($"'{wordToDelete}' 단어를 삭제하시겠습니까?", "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        // DB에서 삭제
                        string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TransLearn", "translearn.sqlite");
                        string connString = $"Data Source={dbPath};Version=3;";

                        using (var conn = new System.Data.SQLite.SQLiteConnection(connString))
                        {
                            conn.Open();
                            string sql = "DELETE FROM LearnTable WHERE WordOrPhrase = @word";
                            using (var cmd = new System.Data.SQLite.SQLiteCommand(sql, conn))
                            {
                                cmd.Parameters.AddWithValue("@word", wordToDelete);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // 삭제 후 목록 새로고침
                        BtnRefreshLearning_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"삭제 실패: {ex.Message}");
            }
        }

        // [2] 목록 불러오기 함수 (기존 함수 내용을 이걸로 교체 확인)
        private void BtnRefreshLearning_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TransLearn", "translearn.sqlite");

                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("아직 저장된 학습 데이터가 없습니다. 화면 번역을 먼저 진행해주세요!");
                    return;
                }

                string connString = $"Data Source={dbPath};Version=3;";

                using (var conn = new System.Data.SQLite.SQLiteConnection(connString))
                {
                    conn.Open();

                    // [정렬 수정] ORDER BY Id DESC 
                    // Id는 데이터가 들어온 순서대로 1, 2, 3... 번호가 붙습니다.
                    // DESC(내림차순)로 정렬하면 큰 숫자(최신 데이터)가 맨 위로 옵니다!
                    string sql = @"
                SELECT 
                    WordOrPhrase, 
                    Frequency, 
                    ContextSentence 
                FROM LearnTable 
                ORDER BY Id DESC";

                    var adapter = new System.Data.SQLite.SQLiteDataAdapter(sql, conn);
                    var dt = new System.Data.DataTable();
                    adapter.Fill(dt);

                    GridLearning.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 로드 실패: {ex.Message}");
            }
        }
    }
}