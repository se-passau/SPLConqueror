using System;
using System.Diagnostics;
using System.Collections.Generic;
using MachineLearning.Solver;
using SPLConqueror_Core;
using System.IO;

namespace MachineLearning.Sampling.Heuristics.UniformHeuristics
{
    public class UniformGrammarSampling : UniformSampling
    {
        private Grammar Grammar;
        private FMTree Tree;
        private List<List<string>> MergedTerminals = new List<List<string>>();
        private Dictionary<string, string> strategyParameter;

        #region constants
        public const string NUM_CONFIGS = "numConfigs";
        public const string AS_TW = "asTW";
        public const string SEED = "seed";
        public const string WHOLE_POPULATION = "wholePopulation";
        public const string LANGUAGE_FILE = "languageFile";
        public const string CONF_FAULTS_REPORT = "confFaultsReport";
        #endregion
        // <summary>
        // This is only to convert an integer to a list of active features! Each position has the product of all bases that are left.
        // </summary>
        private List<int> Bases = new List<int>();

        private int NumberWords;
        private readonly int samples;
        private readonly bool wholePopulation;


        public UniformGrammarSampling(Dictionary<string, string> parameters)
        {
            this.strategyParameter = new Dictionary<string, string>()
            {
                {SEED, "0" },
                {NUM_CONFIGS, "asTW2" },
                {WHOLE_POPULATION, "false"},
                {LANGUAGE_FILE, "language.csv"},
                {CONF_FAULTS_REPORT, "false" }
            };        
            foreach (KeyValuePair<string, string> keyValue in parameters)
            {
                if (strategyParameter.ContainsKey(keyValue.Key))
                {
                    strategyParameter[keyValue.Key] = keyValue.Value;
                }
            }

            GenerateGrammar();
            if (!int.TryParse(this.strategyParameter[NUM_CONFIGS], out samples))
            {
                samples = CountConfigurations();
            }
            if (this.strategyParameter[CONF_FAULTS_REPORT].Contains("false"))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                ComputeSamplingStrategy();
                stopwatch.Stop();
                Console.WriteLine("ConfigurationSampling done in {0} ms", stopwatch.ElapsedMilliseconds);
            }
            else 
            {
                ComputeSamplingStrategyWithReport(this.strategyParameter[CONF_FAULTS_REPORT]);
            }
            
            if (! bool.TryParse(this.strategyParameter[WHOLE_POPULATION], out wholePopulation))
            {
                wholePopulation = false;
            }


            // printSamplingSet();

        }

        public override bool ComputeSamplingStrategy()
        {
            Random random = new Random(int.Parse(this.strategyParameter[SEED]));
            List<List<string>> featureLists = new List<List<string>>();
            List<List<BinaryOption>> configurationList = new List<List<BinaryOption>>();
            CheckConfigSATZ3 configSAT = new CheckConfigSATZ3();
            int[] randomNumbers = new int[samples];
            for (int i = 0; i < samples; i++)
            {
                randomNumbers[i] = -1;                
            }

            for (int i = 0; i < samples; i++)
            {
                int number = random.Next(0, NumberWords);
                while (Array.IndexOf(randomNumbers, number) > -1)
                {                    
                    number = random.Next(0, NumberWords);
                }
                List<string> featureList = ConvertIntegerToFeatureList(number);
                List<BinaryOption> configuration = ConvertFeatureListToConfiguration(featureList);
                while (! configSAT.checkConfigurationSAT(configuration, GlobalState.varModel, false))
                {
                    number = random.Next(0, NumberWords);
                    while (Array.IndexOf(randomNumbers, number) > -1)
                    {                    
                        number = random.Next(0, NumberWords);
                    }
                    featureList = ConvertIntegerToFeatureList(number);
                    configuration = ConvertFeatureListToConfiguration(featureList);
                }
                randomNumbers[i] = number;
                featureLists.Add(featureList);
                configurationList.Add(configuration);
            }
            foreach (List<string> featureList in featureLists)
            {
                selectedConfigurations.Add(ConvertFeatureListToConfiguration(featureList));
            }
            return true;
        }

        //<summary>
        // This method reports each sampled configuration within the grammar that is not valid
        // considering all crosstree constraints
        //</summary>
        public bool ComputeSamplingStrategyWithReport(string Filename)
        {
            Random random = new Random(int.Parse(this.strategyParameter[SEED]));
            List<List<string>> featureLists = new List<List<string>>();
            List<List<BinaryOption>> configurationList = new List<List<BinaryOption>>();
            CheckConfigSATZ3 configSAT = new CheckConfigSATZ3();
            int[] randomNumbers = new int[samples];
            for (int i = 0; i < samples; i++)
            {
                randomNumbers[i] = -1;
            }
            int faults = 0;
            using (StreamWriter w = File.CreateText(Filename))
            {
                Console.WriteLine("Number to sample: " + samples.ToString() + "; Number of Configurations in Grammar: " + NumberWords.ToString());
                for (int i = 0; i < samples; i++)
                {
                    int number = random.Next(0, NumberWords);
                    while(Array.IndexOf(randomNumbers, number) > -1)
                    {
                        Console.WriteLine("Draw a new random number, cause " + number.ToString() + " is already in drawn.");
                        number = random.Next(0, NumberWords);
                    }
                    List<string> featureList = ConvertIntegerToFeatureList(number);
                    List<BinaryOption> configuration = ConvertFeatureListToConfiguration(featureList);
                    while (!configSAT.checkConfigurationSAT(configuration, GlobalState.varModel, false))
                    {
                        w.WriteLine(number.ToString() + " - " + String.Join(", ", featureList));
                        
                        faults = faults + 1;
                        number = random.Next(0, NumberWords);
                        while(Array.IndexOf(randomNumbers, number) > -1)
                        {
                            Console.WriteLine("Draw a new random number, cause " + number.ToString() + " is already in drawn.");
                            number = random.Next(0, NumberWords);
                        }
                        featureList = ConvertIntegerToFeatureList(number);
                        configuration = ConvertFeatureListToConfiguration(featureList);
                    }
                    randomNumbers[i] = number;
                    featureLists.Add(featureList);
                    configurationList.Add(configuration);
                }
            }
            Console.WriteLine(faults.ToString() + " drawn configurations were invalid");
            foreach (List<string> featureList in featureLists)
            {
                selectedConfigurations.Add(ConvertFeatureListToConfiguration(featureList));
            }
            return true;
        }

        private List<string> ConvertIntegerToFeatureList(int index)
        {
            List<string> features = new List<string>();
            int remainder = index;
            for (int i = 0; i < Bases.Count; i++)
            {
                int res = remainder / Bases[i];
                remainder %= Bases[i];
                string feature = MergedTerminals[i][res];
                if (feature != "\u03B5") features.Add(feature);

            }
            return features;
        }

        private List<BinaryOption> ConvertFeatureListToConfiguration (List<string> featureList)
        {
            List<string> configNames = new List<string>();
            foreach (string name in featureList)
            {
                FMNode node = Tree.getNodeByName(name);
                while (node.Parent != null)
                {
                    if (configNames.Contains(node.Name)) break;
                    configNames.Add(node.Name);
                    node = node.Parent;
                }
            }
            List<BinaryOption> configuration = new List<BinaryOption>();
            foreach (string name in configNames)
            {
                configuration.Add(Tree.getNodeByName(name).ActualOption);
            }
            configuration.Add(Tree.Root.ActualOption);
            return configuration;
        }

        private void GenerateGrammar()
        {
            Tree = new FMTree();
            Grammar = new Grammar(Tree);
        }

        private void printSamplingSet()
        {
            Console.WriteLine("Grammar-based Sample Set:");
            foreach(List<BinaryOption> conf in selectedConfigurations)
            {
                foreach(BinaryOption feature in conf)
                {
                    Console.Write(feature.Name + " ");
                }
                Console.WriteLine(";");
            }
        }

        private int CountConfigurations()
        {
            int numberConfigs;

            string numConfigsValue = this.strategyParameter[NUM_CONFIGS];
            // Only "asTW" is currently supported
            if (numConfigsValue.Contains(AS_TW))
            {
                numConfigsValue = numConfigsValue.Replace(AS_TW, "").Trim();
                int t;
                int.TryParse(numConfigsValue, out t);
                TWise tw = new TWise();
                numberConfigs = tw.generateT_WiseVariants_new(GlobalState.varModel, t).Count;
            }
            else
            {
                throw new ArgumentException("Only asTW is currently supported. But was: " + numConfigsValue);
            }

            return numberConfigs;
        }
        public override string GetName()
        {
            return "UNIFORM-GRAMMAR-SAMPLING";
        }

        public override string GetTag()
        {
            return "UNIFORMGR";
        }

    }
}
