/* This template shows the building blocks for training a machine learning model with ML.NET (https://aka.ms/mlnet).
 * This model predicts whether a sentence has a positive or negative sentiment. It is based on a sample that can be 
 * found at https://aka.ms/mlnetsentimentanalysis, which provides a more detailed introduction to ML.NET and the scenario.*/

using System;
using System.IO;
using Microsoft.ML.Runtime.Learners;
using Microsoft.ML.Runtime.Data;
using Microsoft.ML.Core.Data;
using Microsoft.ML;
using Microsoft.ML.Runtime.Api;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Categorical;
using Microsoft.ML.Transforms.Conversions;
using Microsoft.ML.Transforms.Text;
using static Microsoft.ML.Runtime.TransformsCatalog;

namespace MLNETConsoleApp
{
    class Program
    {
        static void Main()
        {
            // 1. Build an ML.NET pipeline for training a sentiment analysis model
            Console.WriteLine("Training a model for Sentiment Analysis using ML.NET");
            MLContext mlContext = new MLContext();
            // 1a. Load the training data using a TextLoader.
            TextLoader reader = new TextLoader(mlContext,
                                            new TextLoader.Arguments()
                                            {
                                                Separator = "tab",
                                                HasHeader = true,
                                                Column = new[]
                                                {
                                                    new TextLoader.Column("Label", DataKind.Bool, 0),
                                                    new TextLoader.Column("Text", DataKind.Text, 1)
                                                }
                                            });

            IDataView trainingDataView = reader.Read(new MultiFileSource(@"Data\wikipedia-detox-250-line-data.tsv"));
            // 2. Create a pipeline to prepare your data, pick your features and apply a machine learning algorithm 
            // 2a. Featurize the text into a numeric vector that can be used by the machine learning algorithm.
            var pipeline = new TextFeaturizingEstimator(mlContext, "Text", "Features")
                                           .Append(mlContext.BinaryClassification.Trainers.StochasticDualCoordinateAscent( "Label", "Features"));

            // 3. Get a model by training the pipeline that was built.
            var model = pipeline.Fit(trainingDataView);

            // 4. Evaluate the model to see how well it performs on different data (output the percent of examples classified correctly).
            Console.WriteLine("Training of model is complete \nTesting the model with test data");
            IDataView testDataView = reader.Read(new MultiFileSource(@"Data\wikipedia-detox-250-line-test.tsv"));
            var predictions = model.Transform(testDataView);
            BinaryClassificationContext binClassificationCtx = new BinaryClassificationContext(mlContext);
            BinaryClassifierEvaluator.CalibratedResult metrics = binClassificationCtx.Evaluate(predictions, "Label");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");

            // 5. Use the model for a single prediction.
            PredictionFunction<SentimentData, SentimentPrediction> predictionFunct = model.MakePredictionFunction<SentimentData, SentimentPrediction>(mlContext);
            SentimentData testInput = new SentimentData { Text = "ML.NET is fun, more samples at https://github.com/dotnet/machinelearning-samples" };
            SentimentPrediction resultprediction = predictionFunct.Predict(testInput);

            /* This template uses a minimal dataset to build a sentiment analysis model which leads to relatively low accuracy. 
             * Building good Machine Learning models require large volumes of data. This template comes with a minimal dataset (Data/wikipedia-detox) for sentiment analysis. 
             * In order to build a sentiment analysis model with higher accuracy please follow the walkthrough at https://aka.ms/mlnetsentimentanalysis/ */
            Console.WriteLine($"Predicted sentiment for \"{testInput.Text}\" is: { (Convert.ToBoolean(resultprediction.Prediction) ? "Positive" : "Negative")}");

            // 6. Save the model to file so it can be used in another app.
            Console.WriteLine("Saving the model");
            FileStream fs = new FileStream("sentiment_model.zip", FileMode.Create, FileAccess.Write, FileShare.Write);
            model.SaveTo(mlContext, fs);
        }

        /// <summary>
        /// Input class that tells ML.NET how to read the dataset (which columns are included)
        /// </summary>
        public class SentimentData
        {
            public bool Label { get; set; }
            public string Text { get; set; }
        }

        /// <summary>
        /// Output class for the prediction, in this case including only the predicted sentiment.
        /// </summary>
        public class SentimentPrediction
        {
            [ColumnName("PredictedLabel")]
            public bool Prediction { get; set; }
        }
    }
}
