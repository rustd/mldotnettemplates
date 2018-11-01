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

namespace MLNETConsoleApp
{
    class Program
    {
        static void Main()
        {
            // 1. Build an ML.NET pipeline for training a sentiment analysis model
            Console.WriteLine("Training a model for Sentiment Analysis using ML.NET");
            var env = new LocalEnvironment();
            // 1a. Load the training data using a TextLoader.
            var reader = new TextLoader(env,
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
            var pipeline = new TextTransform(env, "Text", "Features")  //Convert the text column to numeric vectors (Features column)   
                                           .Append(new LinearClassificationTrainer(env, new LinearClassificationTrainer.Arguments(),
                                                                                   "Features", "Label"));

            // 2b. Add AveragedPerceptron (a linear learner) to the pipeline.
            //new AveragedPerceptronBinaryClassifier() { NumIterations = 10 }

            // 3. Get a model by training the pipeline that was built.
            var model = pipeline.Fit(trainingDataView);

            // 4. Evaluate the model to see how well it performs on different data (output the percent of examples classified correctly).
            Console.WriteLine("Training of model is complete \nTesting the model with test data");
            IDataView testDataView = reader.Read(new MultiFileSource(@"Data\wikipedia-detox-250-line-test.tsv"));
            var predictions = model.Transform(testDataView);
            var binClassificationCtx = new BinaryClassificationContext(env);
            var metrics = binClassificationCtx.Evaluate(predictions, "Label");
            Console.WriteLine($"Accuracy: {metrics.Accuracy:P2}");

            // 5. Use the model for a single prediction.
            var predictionFunct = model.MakePredictionFunction<SentimentData, SentimentPrediction>(env);
            SentimentData testInput = new SentimentData { Text = "ML.NET is fun, more samples at https://github.com/dotnet/machinelearning-samples" };
            var resultprediction = predictionFunct.Predict(testInput);

            /* This template uses a minimal dataset to build a sentiment analysis model which leads to relatively low accuracy. 
             * Building good Machine Learning models require large volumes of data. This template comes with a minimal dataset (Data/wikipedia-detox) for sentiment analysis. 
             * In order to build a sentiment analysis model with higher accuracy please follow the walkthrough at https://aka.ms/mlnetsentimentanalysis/ */
            Console.WriteLine($"Predicted sentiment for \"{testInput.Text}\" is: { (Convert.ToBoolean(resultprediction.Prediction) ? "Positive" : "Negative")}");

            // 6. Save the model to file so it can be used in another app.
            Console.WriteLine("Saving the model");
            var fs = new FileStream("sentiment_model.zip", FileMode.Create, FileAccess.Write, FileShare.Write);
            model.SaveTo(env, fs);
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
