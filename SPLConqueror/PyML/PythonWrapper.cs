using System.Diagnostics;
using SPLConqueror_Core;
using System.Collections.Generic;
using System;
using System.Text;
using System.Threading;
using System.Linq;

namespace ProcessWrapper
{
    public class PythonWrapper
    {
        private Process pythonProcess;

        protected string SCRIPT_LOCATION = System.IO.Path.DirectorySeparatorChar + "PyML" + System.IO.Path.DirectorySeparatorChar + "pyScripts";

        public const string COMMUNICATION_SCRIPT = "Communication.py";

        public static string PYTHON_PATH = "python";

        // Messages sent to the python process.

        /* These messages are used indicate the start and end of the sequenz, where 
         * the configuration data of the learner will be sent.
         * Used in initialize Learning.
        */
        private const string LEARNING_SETTINGS_STREAM_START = "settings_start";

        private const string LEARNING_SETTINGS_STREAM_END = "settings_end";

        /* Message to send the task that should be performed by the application.
         * Only one task can be performed by the process before terminating.
        */
        public const string START_LEARN = "start_learn";

        public const string START_PARAM_TUNING = "start_param_tuning";

        // Message to request of the results of the process.
        private const string REQUESTING_LEARNING_RESULTS = "req_results";


        // Messages received by the python process

        // Message to indicate that settings can be sent.
        private const string AWAITING_SETTINGS = "req_settings";

        // Message to indicate that configurations can be sent.
        private const string AWAITING_CONFIGS = "req_configs";

        // ACK.
        private const string PASS_OK = "pass_ok";

        // Message to indicate that the process has performed the task.
        private const string FINISHED_LEARNING = "learn_finished";

        private string[] mlProperties = null;

        /// <summary>
        /// Create a new wrapper that contains a running Python Process.
        /// </summary>
        /// <param name="path">The path of the source file called to start Python.</param>
        /// <param name="mlProperties">Configurations for the machine learning algorithm.</param>
        public PythonWrapper(string path, string[] mlProperties)
        {
            bool debugTrace = false;
            if((mlProperties.Length > 1) && mlProperties[1] == "Debug") {
                debugTrace = true;
                mlProperties = mlProperties.Except(new string[] { mlProperties[1] }).ToArray();
            }
            this.mlProperties = mlProperties;
            ProcessStartInfo pythonSetup = new ProcessStartInfo(PYTHON_PATH, path + " " + debugTrace.ToString());
            pythonSetup.UseShellExecute = false;
            pythonSetup.RedirectStandardInput = true;
            pythonSetup.RedirectStandardOutput = true;
            pythonSetup.RedirectStandardError = true;
            pythonProcess = Process.Start(pythonSetup);
            Thread errorRedirect = new Thread(() => redirectOutputThread(pythonProcess));
            errorRedirect.Start();
        }

        public bool isRunning()
        {
            return pythonProcess != null && !pythonProcess.HasExited;
        }


        private void redirectOutputThread(Process python)
        {
            while (!python.HasExited)
            {
                if (!python.StandardError.EndOfStream)
                {
                    //GlobalState.logError.logLine("Python error/warning:");
                    //GlobalState.logError.logLine(python.StandardError.ReadToEnd());
                }
            }
        }

        private string waitForNextReceivedLine()
        {
            string processOutput = "";
            while (processOutput == null || processOutput.Length == 0)
            {
                if (pythonProcess.HasExited)
                {
                    GlobalState.logError.logLine("Python process crashed. For more information check std error.");
                    throw new InvalidOperationException();
                }
                processOutput = pythonProcess.StandardOutput.ReadLine();
            }
            return processOutput;
        }

        private void passLineToApplication(string toPass)
        {
            pythonProcess.StandardInput.WriteLine(toPass);
        }

        /// <summary>
        /// Kill the process.
        /// </summary>
        public void endProcess()
        {
            pythonProcess.Close();
        }

        private void initializeLearning(string[] mlSettings)
        {
            passLineToApplication(LEARNING_SETTINGS_STREAM_START);
            passLineToApplication(mlSettings[0].Trim().ToLower());
            if (mlSettings.Length > 1)
            {
                for (int i = 1; i < mlSettings.Length; ++i)
                {
                    passLineToApplication(mlSettings[i]);
                }
            }
            passLineToApplication(LEARNING_SETTINGS_STREAM_END);
        }

        /// <summary>
        /// This method prints the predictions in the specified file and returns the error rate.
        /// </summary>
        /// <param name="pythonList">The predictions from python.</param>
        /// <param name="predictedConfigurations">The configurations that were predicted.</param>
        /// <param name="writer">The writer object for the file.</param>
        /// <returns></returns>
        private double printNfpPredictionsPython(string pythonList, List<Configuration> predictedConfigurations, PythonPredictionWriter writer, out  List<Configuration> predictedByPython)
        {
            predictedByPython = new List<Configuration>();
            string[] separators = new String[] { "," };
            string[] predictions = pythonList.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            if (predictedConfigurations.Count != predictions.Length)
                GlobalState.logError.log("number of predictions using a python learner does not match with number of configurations");

            if ((predictions[0].ToLower()).StartsWith("error"))
            {
                GlobalState.logError.log("There was a error running the Python script.");
                StringBuilder errMessage = new StringBuilder();
                foreach (string errInfo in predictions)
                {
                    errMessage.Append(errInfo);
                }
                GlobalState.logError.log("Error message: " + errMessage.ToString());
                return Double.NaN;
            }
            else
            {
                double error = 0;
                writer.writePredictions("Configuration;MeasuredValue;PredictedValue\n");
                for (int i = 0; i < predictedConfigurations.Count; i++)
                {
                    writer.writePredictions(predictedConfigurations[i].ToString().Replace(";", "_") + ";" + Math.Round(predictedConfigurations[i].GetNFPValue(), 4) + ";" + Math.Round(Convert.ToDouble(predictions[i]), 4) + "\n");

                    error += Math.Abs(predictedConfigurations[i].GetNFPValue() - Convert.ToDouble(predictions[i])) / predictedConfigurations[i].GetNFPValue() ;
                    var copy = predictedConfigurations[i].Copy();
                    copy.setMeasuredValue(GlobalState.currentNFP, predictedConfigurations[i].GetNFPValue());
                    predictedByPython.Add(copy);
                }

                error /= predictedConfigurations.Count;
                return error;
            }
        }

        /// <summary>
        /// Starts the python process by sending the learner configurations.
        /// Then sends the configurations that are used to train the learner and the configurations that should be used for prediction by
        /// the learner.
        /// At last sends the task that should be performed(learning or parameter tuning).
        /// This has to be performed before requesting results and can only be done once per lifetime of the process.
        /// </summary>
        /// <param name="configsLearn">Path to the file that constains the configurations used for learning.</param>
        /// <param name="configsPredict">Path to the file that constains the configurations used for prediction.</param>
        /// <param name="nfpLearn">Path to the file that contains the nfp values that belong to the learning set.</param>
        /// <param name="nfpPredict">Path to the file that contains the nfp vlaues that belong to the prediction set.</param>
        /// <param name="task">Task that should be performed by the learner. Can either be parameter tuning
        /// or learning.</param>
        /// <param name="model">Model that contains all the configuration options.</param>
        public void setupApplication(string configsLearn, string nfpLearn, string configsPredict, string nfpPredict,
            string task, VariabilityModel model, string treePath = " ")
        {
            if (AWAITING_SETTINGS.Equals(waitForNextReceivedLine()))
            {
                initializeLearning(this.mlProperties);
                if (AWAITING_CONFIGS.Equals(waitForNextReceivedLine()))
                {
                    passLineToApplication(configsLearn + " " + nfpLearn);
                    while (!waitForNextReceivedLine().Equals(PASS_OK)) ;
                    passLineToApplication(configsPredict + " " + nfpPredict);
                    while (!waitForNextReceivedLine().Equals(PASS_OK)) ;
                    passLineToApplication(treePath);
                    while (!waitForNextReceivedLine().Equals(PASS_OK)) ;
                        List<string> opts = new List<string>();
                    model.BinaryOptions.ForEach(opt => opts.Add(opt.Name));
                    model.NumericOptions.ForEach(opt => opts.Add(opt.Name));
                    passLineToApplication(string.Join(",", opts));
                    while (!waitForNextReceivedLine().Equals(PASS_OK)) ;
                    passLineToApplication(task);
                }
            }
        }

        /// <summary>
        /// Send a request to the process to get the learning/prediction results.
        /// The process has to be set up before performing this method.
        /// The process will automatically terminate after this method was performed.
        /// </summary>
        /// <param name="predictedConfigurations">The configurations that were used to predict the nfp values by the learner.</param>
        /// <param name="writer">Writer to write the prediction results into a csv File.</param>
        /// <returns>A double indicating the error rate (NaN if there is none)</returns>
        public double getLearningResult(List<Configuration> predictedConfigurations, PythonPredictionWriter writer, out List<Configuration> predictedByPython)
        {

            while (!waitForNextReceivedLine().Equals(FINISHED_LEARNING))
            {

            }

            passLineToApplication(REQUESTING_LEARNING_RESULTS);
            return printNfpPredictionsPython(waitForNextReceivedLine(), predictedConfigurations, writer, out predictedByPython);
        }

        public string getTimeToLearning()
        {
            return waitForNextReceivedLine();
        }

        /// <summary>
        /// Send a request to the process to get the parameter tuning results(optimal configuration of the learner for the current
        /// scenario).
        /// The process has to be set up before performing this method.
        /// The process will automatically terminate after this method was performed.
        /// </summary>
        /// <param name="predictedConfigurations">The configurations that were used to predict the nfp values by the learner.</param>
        /// <param name="targetPath">Target path to write intermediate results into a file.</param>
        /// <returns>Optimal configuration</returns>
        public string getOptimizationResult(List<Configuration> predictedConfigurations, string targetPath)
        {
            passLineToApplication(targetPath);

            while (!waitForNextReceivedLine().Equals(FINISHED_LEARNING))
            {

            }

            passLineToApplication(REQUESTING_LEARNING_RESULTS);
            return waitForNextReceivedLine();
        }


    }
}
