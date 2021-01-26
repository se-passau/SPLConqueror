using System;
using System.Diagnostics;
using System.Collections.Generic;
using MachineLearning.Solver;
using SPLConqueror_Core;
using System.IO;
using System.Numerics;

namespace MachineLearning.Sampling.Heuristics.UniformHeuristics
{
    public class UniformGrammarSamplingBigInt : UniformSampling
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
        private List<BigInteger> Bases = new List<BigInteger>();

        private BigInteger NumberWords;
        private readonly int samples;
        private readonly bool wholePopulation;
        private RandomBigInteger rand;


        public UniformGrammarSamplingBigInt(Dictionary<string, string> parameters)
        {
            this.strategyParameter = new Dictionary<string, string>()
            {
                {SEED, "0" },
                {NUM_CONFIGS, "asTW2" },
                {WHOLE_POPULATION, "false"},
                {LANGUAGE_FILE, "language.csv"},
            };
            foreach (KeyValuePair<string, string> keyValue in parameters)
            {
                if (strategyParameter.ContainsKey(keyValue.Key))
                {
                    strategyParameter[keyValue.Key] = keyValue.Value;
                }
            }
            rand = new RandomBigInteger(int.Parse(this.strategyParameter[SEED]));

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            GenerateGrammar();
            NumberWords = computeNumberGrammarValidConfigurations(Grammar.Root);
            if (!int.TryParse(this.strategyParameter[NUM_CONFIGS], out samples))
            {
                samples = CountConfigurations();
            }
            ComputeSamplingStrategy();
            stopwatch.Stop();
            Console.WriteLine("ConfigurationSampling done in {0} ms", stopwatch.ElapsedMilliseconds);
            Console.WriteLine(NumberWords);
            Grammar.print();
            if (!bool.TryParse(this.strategyParameter[WHOLE_POPULATION], out wholePopulation))
            {
                wholePopulation = false;
            }
        }


        public override bool ComputeSamplingStrategy()
        {
            List<List<string>> featureLists = new List<List<string>>();
            CheckConfigSATZ3 configSAT = new CheckConfigSATZ3();
            BigInteger[] randomNumbers = new BigInteger[samples];
            for (int i = 0; i < samples; i++)
            {
                randomNumbers[i] = -1;
            }

            for (int i = 0; i < samples; i++)
            {
                BigInteger number = rand.NextBigInteger(0, NumberWords);
                while (Array.IndexOf(randomNumbers, number) > -1)
                {
                    number = rand.NextBigInteger(0, NumberWords);
                }
                List<string> featureList = ConvertIntegerToFeatureList(number, Grammar.Root);
                featureList.Add(Grammar.Root);
                List<BinaryOption> configuration = ConvertFeatureListToConfiguration(featureList);
                while (!configSAT.checkConfigurationSAT(configuration, GlobalState.varModel, false))
                {
                    Console.WriteLine("invalid number: " + number + " respective config: " + String.Join(", ", featureList));
                    number = rand.NextBigInteger(0, NumberWords);
                    while (Array.IndexOf(randomNumbers, number) > -1)
                    {
                        number = rand.NextBigInteger(0, NumberWords);
                    }
                    featureList = ConvertIntegerToFeatureList(number, Grammar.Root);
                    featureList.Add(Grammar.Root);
                    configuration = ConvertFeatureListToConfiguration(featureList);
                }
                randomNumbers[i] = number;
                featureLists.Add(featureList);
                // Console.WriteLine("drawn number: " + number + "; config: " + String.Join(", ", featureList));
                selectedConfigurations.Add(configuration);
            }
            return true;
        }

        private List<string> ConvertIntegerToFeatureList(BigInteger index, string key)
        {
            List<string> features = new List<string>();
            Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation> rule_set = Grammar.GetRule(key);
            List<Tuple<string, BigInteger, Boolean>> rule = rule_set.Item1;
            if (rule_set.Item3 == Operation.MULT)
            {
                for (int j = 0; j < rule.Count; j++)
                {
                    BigInteger basis = 1;
                    for (int i = rule.Count - 1; i > j; i--)
                    {
                        basis *= rule[i].Item2;
                    }
                    BigInteger number = index / basis;
                    index = index % basis;
                    // Console.WriteLine("Number: " + number + "; Index: " + index + "; Basis: " + basis + "; key: " + key + "; child: " + rule[j].Item1 + "; Symbolkind: " + rule_set.Item2);
                    List<string> childFeatures = ConvertIntegerToFeatureList(number, rule[j].Item1);
                    if (!rule[j].Item3 && childFeatures.Count > 0) features.Add(rule[j].Item1);
                    if (rule_set.Item2 == SymbolKind.NONTERMINAL) features.AddRange(childFeatures);
                }
            }
            else
            {
                for (int i = 0; i < rule.Count; i++)
                {
                    if (index >= rule[i].Item2) index = index - rule[i].Item2;
                    else if (rule_set.Item2 == SymbolKind.NONTERMINAL)
                    {
                        List<string> childFeatures = ConvertIntegerToFeatureList(index, rule[i].Item1);
                        if (!rule[i].Item3 && childFeatures.Count > 0) features.Add(rule[i].Item1);
                        features.AddRange(childFeatures);
                        break;
                    }
                    else
                    {
                        if (!rule[i].Item3) features.Add(rule[i].Item1);
                        break;
                    }
                }
            }
            return features;
        }

        private List<BinaryOption> ConvertFeatureListToConfiguration(List<string> features)
        { 
            List<BinaryOption> configuration = new List<BinaryOption>();
            foreach (string name in features)
            {
                configuration.Add(Tree.getNodeByName(name).ActualOption);
            }
            return configuration;
        }

        private void GenerateGrammar()
        {
            Tree = new FMTree();
            Grammar = new Grammar(Tree);
        }

        private BigInteger computeNumberGrammarValidConfigurations(string rule_key)
        {
            BigInteger number = 0;
            Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation> ruleInfo = Grammar.GetRule(rule_key);
            if (ruleInfo.Item3 == Operation.MULT) number = 1;
            List<Tuple<string, BigInteger, Boolean>> rule_set = ruleInfo.Item1;
            for (int i = 0; i < rule_set.Count; i++)
            {
                Tuple<string, BigInteger, Boolean> tuple = rule_set[i];
                if (tuple.Item2 == -1)
                {
                    rule_set[i] = new Tuple<string, BigInteger, Boolean> (tuple.Item1, computeNumberGrammarValidConfigurations(tuple.Item1), tuple.Item3);
                }
                tuple = rule_set[i];
                if (ruleInfo.Item3 == Operation.ADD)
                {
                    number += tuple.Item2;
                }
                else 
                {
                    number *= tuple.Item2; 
                }
            }
            return number;
        }

        private void printSamplingSet()
        {
            Console.WriteLine("Grammar-based Sample Set:");
            foreach (List<BinaryOption> conf in selectedConfigurations)
            {
                foreach (BinaryOption feature in conf)
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
