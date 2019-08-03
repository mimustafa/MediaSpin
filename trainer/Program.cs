﻿using System;
using System.IO;
using Microsoft.ML;
using SentimentAnalysisConsoleApp.DataStructures;
using static Microsoft.ML.DataOperationsCatalog;

namespace trainer
{
    internal static class Program
    {
        private static readonly string BaseDatasetsRelativePath = @"../../../../data";
        private static readonly string DataRelativePath = $"{BaseDatasetsRelativePath}/articles.tsv";

        private static readonly string DataPath = GetAbsolutePath(DataRelativePath);

        private static readonly string BaseModelsRelativePath = BaseDatasetsRelativePath;
        private static readonly string ModelRelativePath = $"{BaseModelsRelativePath}/SentimentModel.zip";

        private static readonly string ModelPath = GetAbsolutePath(ModelRelativePath);

        static void Main(string[] args)
        {
            #region try
            // Create MLContext to be shared across the model creation workflow objects 
            // Set a random seed for repeatable/deterministic results across multiple trainings.
            var mlContext = new MLContext(seed: 1);

            #region step1to3
            // STEP 1: Common data loading configuration
            IDataView dataView = mlContext.Data.LoadFromTextFile<SentimentIssue>(DataPath, hasHeader: true);

            TrainTestData trainTestSplit = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            IDataView trainingData = trainTestSplit.TrainSet;
            IDataView testData = trainTestSplit.TestSet;

            // STEP 2: Common data process configuration with pipeline data transformations          
            var dataProcessPipeline = mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(SentimentIssue.Text));

            // STEP 3: Set the training algorithm, then create and config the modelBuilder                            
            var trainer = mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: "Label", featureColumnName: "Features");
            var trainingPipeline = dataProcessPipeline.Append(trainer);
            #endregion

            #region step4
            // STEP 4: Train the model fitting to the DataSet
            ITransformer trainedModel = trainingPipeline.Fit(trainingData);
            #endregion

            #region step5
            // STEP 5: Evaluate the model and show accuracy stats
            var predictions = trainedModel.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(data: predictions, labelColumnName: "Label", scoreColumnName: "Score");
            #endregion

            ConsoleHelper.PrintBinaryClassificationMetrics(trainer.ToString(), metrics);

            // STEP 6: Save/persist the trained model to a .ZIP file
            mlContext.Model.Save(trainedModel, trainingData.Schema, ModelPath);

            Console.WriteLine("The model is saved to {0}", ModelPath);

            // TRY IT: Make a single test prediction loding the model from .ZIP file
            SentimentIssue sampleStatement = new SentimentIssue { Text = "Nu är Moderaterna näst största parti igen och Kristersson har väljarnas förtroende!" };
            SentimentIssue sampleStatement2 = new SentimentIssue { Text = "Socialdemokraterna sämsta parti i senaste opinionsmätningen" };

            #region consume
            // Create prediction engine related to the loaded trained model
            var predEngine = mlContext.Model.CreatePredictionEngine<SentimentIssue, SentimentPrediction>(trainedModel);

            // Score
            var resultprediction = predEngine.Predict(sampleStatement);
            var resultprediction2 = predEngine.Predict(sampleStatement2);
            #endregion

            Console.WriteLine($"=============== Single Prediction  ===============");
            Console.WriteLine($"Text: {sampleStatement.Text} | Förutsägelse: {(Convert.ToBoolean(resultprediction.Prediction) ? "Negativt" : "Positivt")} känsla | Sannolikheten att denna mening är negativt laddad: {resultprediction.Probability} ");
            Console.WriteLine($"Text: {sampleStatement2.Text} | Förutsägelse: {(Convert.ToBoolean(resultprediction2.Prediction) ? "Negativt" : "Positivt")} känsla | Sannolikheten att denna mening är negativt laddad: {resultprediction2.Probability} ");
            #endregion
        }

        public static string GetAbsolutePath(string relativePath)
        {
            FileInfo _dataRoot = new FileInfo(typeof(Program).Assembly.Location);
            string assemblyFolderPath = _dataRoot.Directory.FullName;

            string fullPath = Path.Combine(assemblyFolderPath , relativePath);

            return fullPath;
        }
    }
}