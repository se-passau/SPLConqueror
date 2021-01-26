using MachineLearning.Sampling.Heuristics;
using MachineLearning.Sampling.Hybrid.Distributive;
using MachineLearning.Sampling.Hybrid.Distributive.SelectionHeuristic;
using MachineLearning.Solver;
using System.Reflection;
using SPLConqueror_Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace MachineLearning.Sampling.Hybrid.Distributive
{
    /// <summary>
    /// This class summarizes the methods of distribution-aware and distribution-preserving sampling and is realized by using
    /// the strategy pattern. 
    /// In these sampling strategies, the configurations are divided in buckets and the sampled configurations are selected 
    /// from this buckets according to a given distribution (e.g., uniform, normal distribution).
    /// </summary>
    public abstract class DistributionSensitive : HybridStrategy
    {
        #region variables
        #region constants
        public const string NUM_CONFIGS = "numConfigs";
        public const string DISTRIBUTION = "distribution";
        public const string DISTANCE_METRIC = "distance-metric";
        public const string AS_TW = "asTW";
        public const string ONLY_NUMERIC = "onlyNumeric";
        public const string ONLY_BINARY = "onlyBinary";
        public const string SEED = "seed";
        public const string SELECTION_HEURISTIC = "selection";
        public const string OPTIONS_FOR_WEIGHTOPTIMIZATION = "number-weight-optimization";
        public const string USED_OPTIMIZATION = "optimization";
        public const string USE_WHOLE_POPULATION = "use-whole-population";
        public const int ROUND_FACTOR = 4;
        public static DistanceMetric[] metrics = { new ManhattanDistance() };
        public static IDistribution[] distributions = { new UniformDistribution(), new BinomialDistribution(),
            new NormalDistribution(), new GeometricDistribution()};
        #endregion

        protected DistanceMetric metric = null;
        protected IDistribution distribution = null;
        protected ISelectionHeuristic selection = null;
        #endregion

        /// <summary>
        /// The constructor initializes the parameters needed for this class and its default values.
        /// These may be overwritten by <see cref="HybridStrategy.SetSamplingParameters(Dictionary{string, string})"/>.
        /// </summary>
        public DistributionSensitive() : base()
        {
            this.strategyParameter = new Dictionary<string, string>()
            {
                {DISTANCE_METRIC, "manhattan" },
                {DISTRIBUTION, "uniform" },
                {NUM_CONFIGS, "asTW2" },
                {ONLY_NUMERIC, "false" },
                {ONLY_BINARY, "false" },
                {SEED, "0" },
                {SELECTION_HEURISTIC, "RandomSelection" },
                {OPTIONS_FOR_WEIGHTOPTIMIZATION, "0" },
                {USED_OPTIMIZATION, Optimization.NONE.ToString().ToUpper ()},
                {USE_WHOLE_POPULATION, "false"}
            };
        }

        /// <summary>
        /// Computes a distribution-aware sample set.
        /// </summary>
        /// <returns><code>True</code> iff the process was successfull;<code>False</code> otherwise</returns>
        public override bool ComputeSamplingStrategy()
        {
            // Check configuration and set the according variables
            CheckConfiguration();

            // Before beginning with the computation, the number of configurations of another sampling strategy is computed if no exact number is provided
            int numberConfigs;
            if (!int.TryParse(this.strategyParameter[NUM_CONFIGS], out numberConfigs))
            {
                numberConfigs = CountConfigurations();
            }

            // Now, compute all buckets according to the given features
            List<double> allBuckets = ComputeBuckets();

            Dictionary<double, List<Configuration>> wholeDistribution = null;
            if (this.selection is RandomSelection ||
                (this.distribution is NormalDistribution && this.strategyParameter.ContainsKey(DistributionAware.USE_WHOLE_POPULATION)))
            {
                // Compute the whole population needed for randomly sampling from the buckets
                wholeDistribution = ComputeDistribution(allBuckets);
            }

            // Then, sample from all buckets according to the given distribution
            SampleFromDistribution(wholeDistribution, allBuckets, numberConfigs);

            return true;
        }

        /// <summary>
        /// This method checks the configuration and sets the according variables.
        /// </summary>
        protected virtual void CheckConfiguration()
        {
            // Check the used metric
            string metricToUse = this.strategyParameter[DISTANCE_METRIC];
            foreach (DistanceMetric m in DistributionSensitive.metrics)
            {
                if (m.GetName().ToUpper().Equals(metricToUse.ToUpper()))
                {
                    this.metric = m;
                    break;
                }
            }

            if (this.metric == null)
            {
                throw new ArgumentException("The metric " + metricToUse + " is not supported.");
            }

            // Check the used distribution
            string distributionToUse = this.strategyParameter[DISTRIBUTION];
            foreach (IDistribution d in DistributionSensitive.distributions)
            {
                if (d.GetName().ToUpper().Equals(distributionToUse.ToUpper()))
                {
                    this.distribution = d;
                    break;
                }
            }

            if (this.distribution == null)
            {
                throw new ArgumentException("The metric " + distributionToUse + " is not supported.");
            }

            string selectionMechanism = this.strategyParameter[SELECTION_HEURISTIC];
            Type selectionType = Type.GetType("MachineLearning.Sampling.Hybrid.Distributive.SelectionHeuristic."
                + selectionMechanism);
            if (typeof(ISelectionHeuristic).IsAssignableFrom(selectionType))
            {
                this.selection = (ISelectionHeuristic)Activator.CreateInstance(selectionType);

                if (this.selection is RandomSelection)
                {
                    int seed = 0;
                    Int32.TryParse(this.strategyParameter[SEED], out seed);
                    ((RandomSelection)selection).setSeed(seed);
                }
                else if (this.selection is SolverSelection)
                {
                    int seed = 0;
                    Int32.TryParse(this.strategyParameter[SEED], out seed);
                    ((SolverSelection)selection).setSeed(seed);
                    Tuple<int, int> numberFeatureRange = new Tuple<int, int>(1, 1);
                    if (this.strategyParameter[OPTIONS_FOR_WEIGHTOPTIMIZATION].Contains("-"))
                    {
                        string[] range = this.strategyParameter[OPTIONS_FOR_WEIGHTOPTIMIZATION].Split('-');
                        if (range.Length > 2)
                        {
                            throw new ArgumentException("The argument " + OPTIONS_FOR_WEIGHTOPTIMIZATION +
                                " has to consist of at most two numbers separated by a minus sign ('-').");
                        }
                        int first = 1;
                        int second = 1;
                        Int32.TryParse(range[0], out first);
                        Int32.TryParse(range[1], out second);
                        numberFeatureRange = new Tuple<int, int>(first, second);
                    }
                    else
                    {
                        int numberFeatures = 1;
                        Int32.TryParse(this.strategyParameter[OPTIONS_FOR_WEIGHTOPTIMIZATION], out numberFeatures);
                        numberFeatureRange = new Tuple<int, int>(numberFeatures, numberFeatures);
                    }
                  ((SolverSelection)selection).setNumberFeatures(numberFeatureRange);
                }
            }
            else
            {
                throw new ArgumentException("The selection mechanism " + selectionMechanism + "is not supported");
            }


            string onlyNumeric = this.strategyParameter[ONLY_NUMERIC];
            if (onlyNumeric.ToLower().Equals("true"))
            {
                this.optionsToConsider = new List<ConfigurationOption>();
                this.optionsToConsider.AddRange(GlobalState.varModel.NumericOptions);
            }

            string onlyBinary = this.strategyParameter[ONLY_BINARY];
            if (onlyBinary.ToLower().Equals("true"))
            {
                this.optionsToConsider = new List<ConfigurationOption>();
                this.optionsToConsider.AddRange(GlobalState.varModel.BinaryOptions);
            }

            // Both can't be set to true at the same time
            if (onlyBinary.ToLower().Equals("true") && onlyNumeric.ToLower().Equals("true"))
            {
                throw new ArgumentException("The options " + ONLY_BINARY + " and " + ONLY_NUMERIC + " can not be active at the same time.");
            }

            // If no options are given, select all options
            if (this.optionsToConsider == null)
            {
                this.optionsToConsider = new List<ConfigurationOption>();
                this.optionsToConsider.AddRange(GlobalState.varModel.BinaryOptions);
                this.optionsToConsider.AddRange(GlobalState.varModel.NumericOptions);
            }

            string optimization = this.strategyParameter[USED_OPTIMIZATION];
            bool found = false;
            foreach (Optimization opt in Enum.GetValues(typeof(Optimization)))
            {
                if (opt.ToString().ToUpper().Equals(optimization.ToUpper()))
                {
                    found = true;
                    this.strategyParameter[USED_OPTIMIZATION] = optimization.ToUpper();
                    break;
                }
            }
            if (!found)
            {
                throw new ArgumentException("The optimization " + optimization + " is invalid.");
            }

        }

        /// <summary>
        /// Creates the wanted distribution.
        /// This method has to be implemented by the different sampling strategies (e.g., distribution-aware, distribution-preserving).
        /// </summary>
        /// <param name="wholeDistribution">a <see cref="Dictionary{TKey, TValue}"/> containing all configurations and their corresponding distance</param>
        /// <param name="allBuckets">all buckets of the distribution</param>
        public abstract Dictionary<double, double> CreateDistribution(Dictionary<double, List<Configuration>> wholeDistribution, List<double> allBuckets);

        /// <summary>
        /// Selects configurations of the given distribution by using the specified distribution (e.g., uniform).
        /// </summary>
        /// <param name="wholeDistribution">the distribution of all configurations</param>
        /// <param name="allBuckets">all buckets of the distribution</param>
        /// <param name="count">the number of configurations to sample</param>
        public void SampleFromDistribution(Dictionary<double, List<Configuration>> wholeDistribution, List<double> allBuckets, int count)
        {
            Dictionary<double, double> wantedDistribution = CreateDistribution(wholeDistribution, allBuckets);

            if (this.selection is RandomSelection)
            {
                ((RandomSelection)selection).setDistribution(wholeDistribution);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            this.selectedConfigurations = selection
                .SampleFromDistribution(wantedDistribution, allBuckets, count, GetOptimization());
            stopwatch.Stop();
            Console.WriteLine("ConfigurationSampling done in {0} ms", stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Returns the optimization.
		/// This implementation returns 'None' if no optimization matches.
        /// </summary>
        /// <returns>The optimization to use.</returns>
		protected Optimization GetOptimization()
        {
            string optimization = this.strategyParameter[USED_OPTIMIZATION];
            foreach (Optimization opt in Enum.GetValues(typeof(Optimization)))
            {
                if (opt.ToString().ToUpper().Equals(optimization.ToUpper()))
                {
                    return opt;
                }
            }

            // Defaults to 'None'
            return Optimization.NONE;
        }

        /// <summary>
        /// Returns whether there are any more samples or not.
        /// </summary>
        /// <param name="wholeDistribution">the distribution of the configurations</param>
        /// <returns><code>True</code> iff there are any configurations left</returns>
        protected bool HasSamples(Dictionary<double, List<Configuration>> wholeDistribution)
        {
            foreach (double d in wholeDistribution.Keys)
            {
                if (wholeDistribution[d].Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This method computes the distribution of the whole population using the <see cref="HybridStrategy.optionsToConsider"/>.
        /// </summary>
        /// <param name="allBuckets">all buckets of the distribution</param>
        /// <returns>a <see cref="Dictionary{TKey, TValue}"/> containing the bucket as key and a <see cref="List"/> of different configurations in these buckets</returns>
        private Dictionary<double, List<Configuration>> ComputeDistribution(List<double> allBuckets)
        {
            Dictionary<double, List<Configuration>> result = new Dictionary<double, List<Configuration>>();

            // Fill the dictionary
            foreach (double d in allBuckets)
            {
                result[d] = new List<Configuration>();
            }

            List<Configuration> allConfigurations = ConfigurationBuilder.vg.GenerateAllVariants(GlobalState.varModel, this.optionsToConsider);

            foreach (Configuration c in allConfigurations)
            {
                double distance = Math.Round(this.metric.ComputeDistance(c), ROUND_FACTOR);
                result[distance].Add(c);
            }

            // Remove empty buckets
            List<double> toRemove = new List<double>();
            foreach (double d in result.Keys)
            {
                if (result[d].Count == 0)
                {
                    toRemove.Add(d);
                }
            }
            foreach (double d in toRemove)
            {
                allBuckets.Remove(d);
                result.Remove(d);
            }

            return result;
        }

        /// <summary>
        /// Returns all possible buckets using the <see cref="HybridStrategy.optionsToConsider"/>.
        /// </summary>
        /// <returns>a <see cref="List"/> containing the sum of all value combinations of the features</returns>
        private List<double> ComputeBuckets()
        {
            List<List<double>> allValueSets = new List<List<double>>();
            foreach (ConfigurationOption o in this.optionsToConsider)
            {
                if (o is NumericOption)
                {
                    NumericOption numOpt = (NumericOption)o;
                    List<double> distances = new List<double>();
                    List<double> valuesOfNumericOption = numOpt.getAllValues();

                    foreach (double numOptValue in valuesOfNumericOption)
                    {
                        distances.Add(this.metric.ComputeDistanceOfNumericFeature(numOptValue, numOpt.Min_value, numOpt.Max_value));
                    }
                    allValueSets.Add(distances);
                }
                else
                {
                    BinaryOption binOpt = (BinaryOption)o;
                    if (!binOpt.Optional && CountChildren(binOpt, GlobalState.varModel) > 0)
                    {
                        allValueSets.Add(new List<double> { this.metric.ComputeDistanceOfBinaryFeature(1) });
                    }
                    else
                    {
                        allValueSets.Add(new List<double> { this.metric.ComputeDistanceOfBinaryFeature(0), this.metric.ComputeDistanceOfBinaryFeature(1) });
                    }
                }
            }

            List<double> result = ComputeSumOfCartesianProduct(allValueSets);

            // Sort the list
            result.Sort(delegate (double x, double y)
            {
                return x.CompareTo(y);
            });

            return result;
        }

        /// <summary>
        /// Returns the number of all children of the given feature.
        /// </summary>
        /// <param name="o">the configuration option to search children for</param>
        /// <param name="var">the variability model</param>
        /// <returns>the number of all children of the given feature</returns>
        private int CountChildren(ConfigurationOption o, VariabilityModel var)
        {
            int count = 0;
            foreach (ConfigurationOption conf in var.getOptions())
            {
                if (conf == o || conf.Parent == null)
                {
                    continue;
                }

                if (conf.Parent.Equals(o))
                {
                    count++;
                }
            }
            return count;
        }


        /// <summary>
        /// Computes the sum of the cartesion product of the given lists.
        /// Note that every bucket is only included once.
        /// </summary>
        /// <param name="remaining">the remaining lists</param>
        /// <returns>the sum of the different combinations</returns>
        private List<double> ComputeSumOfCartesianProduct(List<List<double>> remaining)
        {
            if (remaining.Count == 0)
            {
                return new List<double> { 0 };
            }

            List<double> currentList = remaining[0];

            remaining.RemoveAt(0);

            if (remaining.Count == 0)
            {
                return currentList;
            }
            else
            {
                List<double> sumOfRest = ComputeSumOfCartesianProduct(remaining);
                List<double> newList = new List<double>();
                foreach (double value in sumOfRest)
                {
                    foreach (double ownValue in currentList)
                    {
                        double newSum = Math.Round(value + ownValue, ROUND_FACTOR);
                        // Reduce the size of the new list by ignoring values that are already included
                        if (!newList.Contains(newSum))
                        {
                            newList.Add(newSum);
                        }
                    }
                }

                return newList;
            }
        }

        /// <summary>
        /// Counts the configurations from another sampling strategy.
        /// </summary>
        /// <returns>the number of configurations from the other sampling strategy</returns>
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
                throw new ArgumentException("Only asTW is currently supported.");
            }

            return numberConfigs;
        }
    }
}
