using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.Text;

namespace GithubIssues
{
    public static class TopicModeling
    {
        public static List<TransformedDocument> PerformTopicModeling(List<Document> documents)
        {
            var mlContext = new MLContext();

            var data = mlContext.Data.LoadFromEnumerable(documents);

            var srpskeStopReci = new[] { "i", "a", "ali", "pa", "u", "na", "je", "da", "se", "za", "sa", "su", "koji", "koja", "koje", "od", "do", "s", "bez", "iz", "pre", "posle", "kroz", "ili", "o", "kao", "veoma", "mnogo" };

            var textPipeline = mlContext.Transforms.Text
                .NormalizeText("NormalizedText", "Text")
                .Append(mlContext.Transforms.Text.TokenizeIntoWords("Tokens", "NormalizedText"))
                .Append(mlContext.Transforms.Text.RemoveDefaultStopWords("Tokens"))
                .Append(mlContext.Transforms.Text.RemoveStopWords("Tokens", stopwords: srpskeStopReci))
                .Append(mlContext.Transforms.Text.ProduceWordBags("BagOfWords", "Tokens"))
                .Append(mlContext.Transforms.Text.LatentDirichletAllocation("Topics", "BagOfWords", numberOfTopics: 2));

            var model = textPipeline.Fit(data);

            var transformedData = model.Transform(data);

            var topics = mlContext.Data.CreateEnumerable<TransformedDocument>(transformedData, reuseRowObject: false).ToList();

            return topics;
        }
    }
}