
using System;
using System.Threading.Tasks;
using TransLearn.Data;
using TransLearn.Services.Analysis;

namespace TransLearn.Services
{
    // Orchestrates the "Slow Track" learning analysis process.
    // This should be run in a background thread to not block the UI.
    public class LearningManager
    {
        private readonly PythonAnalysisService _analysisService;
        private readonly TransLearnDao _dao;

        public LearningManager(PythonAnalysisService analysisService, TransLearnDao dao)
        {
            _analysisService = analysisService;
            _dao = dao;
        }

        // This method is called after a translation occurs.
        // It takes the source text, analyzes it, and updates the learning database.
        public async Task ProcessTextForLearningAsync(string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return;
            }

            try
            {
                // 1. Get the list of learnable words/phrases from the Python service.
                var learnableEntries = await _analysisService.AnalyzeTextAsync(sourceText);

                if (learnableEntries == null || learnableEntries.Count == 0)
                {
                    return;
                }

                // 2. Iterate through the results and save each one to the database.
                // The DAO's "AddOrUpdate" method handles incrementing the frequency for existing words.
                foreach (var entry in learnableEntries)
                {
                    await _dao.AddOrUpdateLearnDataAsync(entry);
                }

                Console.WriteLine($"Processed and updated {learnableEntries.Count} entries in the LearnTable.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in learning manager: {ex.Message}");
            }
        }
    }
}
