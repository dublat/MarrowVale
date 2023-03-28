using MarrowVale.Common.Contracts;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using OpenAI_API.Completions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarrowVale.Common.Providers
{
    public class OpenAiProvider : IOpenAiProvider
    {
        private readonly string[] stopOn = new string[] { "\n" };
        private readonly ILogger _logger;
        private readonly IAppSettingsProvider _appSettingsProvider;
        private readonly string apiKey;


        public OpenAiProvider(ILoggerFactory logger, IAppSettingsProvider appSettingsProvider)
        {
            _logger = logger.CreateLogger<OpenAiProvider>();
            _appSettingsProvider = appSettingsProvider;
            apiKey = _appSettingsProvider.OpenAiKey;
        }
        public Task<string> BestMatch(string item, string options)
        {
            throw new NotImplementedException();
        }

        public async Task<string> SemanticSearch(string query, string documents)
        {
            var api = new OpenAIAPI(apiKey);
            var result = await api.Embeddings.CreateEmbeddingAsync(documents);
            return result.ToString();
        }

        public async Task<string> SearchAsync(IEnumerable<string> documents, string searchTerm)
        {
            // Embed the search term
            var api = new OpenAIAPI(apiKey);
            float[] searchTermEmbedding = await api.Embeddings.GetEmbeddingsAsync(searchTerm);

            // Embed all documents
            List<float[]> documentEmbeddings = new List<float[]>();
            foreach (string document in documents)
            {
                float[] documentEmbedding = await api.Embeddings.GetEmbeddingsAsync(document);
                documentEmbeddings.Add(documentEmbedding);
            }

            // Calculate the cosine similarity between the search term and all documents
            List<double> similarities = new List<double>();
            for (int i = 0; i < documentEmbeddings.Count; i++)
            {
                double similarity = CosineSimilarity(searchTermEmbedding, documentEmbeddings[i]);
                similarities.Add(similarity);
            }

            // Find the index of the most similar document
            int mostSimilarIndex = similarities.IndexOf(similarities.Max());

            // Return the most similar document
            return documents.ElementAt(mostSimilarIndex);
        }

        private double CosineSimilarity(float[] vec1, float[] vec2)
        {
            double dotProduct = 0;
            double vec1Magnitude = 0;
            double vec2Magnitude = 0;

            for (int i = 0; i < vec1.Length; i++)
            {
                dotProduct += vec1[i] * vec2[i];
                vec1Magnitude += Math.Pow(vec1[i], 2);
                vec2Magnitude += Math.Pow(vec2[i], 2);
            }

            return dotProduct / (Math.Sqrt(vec1Magnitude) * Math.Sqrt(vec2Magnitude));
        }

        public async Task<CompletionResult> Complete(CompletionRequest completionRequest)
        {
            var api = new OpenAIAPI(apiKeys: apiKey);

            completionRequest.Model = string.IsNullOrEmpty(completionRequest.Model) ? OpenAI_API.Models.Model.BabbageText : completionRequest.Model;
            var result = await api.Completions.CreateCompletionAsync(completionRequest);
            return result;
        }

        public Task<string> SemanticSearch(IEnumerable<string> documents, string searchTermtring)
        {
            throw new NotImplementedException();
        }

    }
}
