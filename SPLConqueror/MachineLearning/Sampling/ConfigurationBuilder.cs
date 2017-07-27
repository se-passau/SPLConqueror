﻿using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;
using MachineLearning.Solver;
using MachineLearning.Sampling.Heuristics;
using MachineLearning.Sampling.ExperimentalDesigns;
using MachineLearning.Sampling.Hybrid;

namespace MachineLearning.Sampling
{
    public class ConfigurationBuilder
    {
        public static int binaryThreshold = 0;
        public static int binaryModulu = 0;
        public static Dictionary<SamplingStrategies, List<NumericOption>> optionsToConsider = new Dictionary<SamplingStrategies, List<NumericOption>>();
        public static BinaryParameters binaryParams = new BinaryParameters();

        // Added by Ch.K.
        private static List<String> blacklisted;

        public static void setBlacklisted(List<String> blacklist)
        {
            ConfigurationBuilder.blacklisted = blacklist;
        }

        public static List<Configuration> buildConfigs(VariabilityModel vm, List<SamplingStrategies> binaryStrategies,
            List<ExperimentalDesign> experimentalDesigns, List<HybridStrategy> hybridStrategies)
        {
            List<Configuration> result = new List<Configuration>();
            VariantGenerator vg = new VariantGenerator();

            List<List<BinaryOption>> binaryConfigs = new List<List<BinaryOption>>();
            List<Dictionary<NumericOption, Double>> numericConfigs = new List<Dictionary<NumericOption, double>>();
            foreach (SamplingStrategies strat in binaryStrategies)
            {
                switch (strat)
                {
                    //Binary sampling heuristics
                    case SamplingStrategies.ALLBINARY:
                        binaryConfigs.AddRange(vg.generateAllVariantsFast(vm));
                        break;
                    case SamplingStrategies.BINARY_RANDOM:
                        RandomBinary rb = new RandomBinary(vm);
                        foreach (Dictionary<string, string> expDesignParamSet in binaryParams.randomBinaryParameters)
                        {
                            binaryConfigs.AddRange(rb.getRandomConfigs(expDesignParamSet));
                        }

                        break;
                    case SamplingStrategies.OPTIONWISE:
                        { 
                            FeatureWise fw = new FeatureWise();
                            binaryConfigs.AddRange(fw.generateFeatureWiseConfigurations(GlobalState.varModel));
                        }
                        break;

                    //case SamplingStrategies.MINMAX:
                    //    {
                    //        MinMax mm = new MinMax();
                    //        binaryConfigs.AddRange(mm.generateMinMaxConfigurations(GlobalState.varModel));

                    //    }
                    //    break;

                    case SamplingStrategies.PAIRWISE:
                        {
                            PairWise pw = new PairWise();
                            binaryConfigs.AddRange(pw.generatePairWiseVariants(GlobalState.varModel));
                        }
                        break;
                    case SamplingStrategies.NEGATIVE_OPTIONWISE:
                        {
                            NegFeatureWise neg = new NegFeatureWise();//2nd option: neg.generateNegativeFWAllCombinations(GlobalState.varModel));
                            binaryConfigs.AddRange(neg.generateNegativeFW(GlobalState.varModel));
                        }
                        break;

                    case SamplingStrategies.T_WISE:
                        foreach (Dictionary<string, string> ParamSet in binaryParams.tWiseParameters)
                        {
                            TWise tw = new TWise();
                            int t = 3;

                            foreach (KeyValuePair<String, String> param in ParamSet)
                            {
                                if (param.Key.Equals(TWise.PARAMETER_T_NAME))
                                {
                                    t = Convert.ToInt16(param.Value);
                                }
                                binaryConfigs.AddRange(tw.generateT_WiseVariants_new(vm, t));
                            }
                        }
                        break;
                }
            }

            //Experimental designs for numeric options
            if (experimentalDesigns.Count != 0)
            {
                handleDesigns(experimentalDesigns, numericConfigs, vm);
            }


            foreach (List<BinaryOption> binConfig in binaryConfigs)
            {
                if (numericConfigs.Count == 0)
                {
                    Configuration c = new Configuration(binConfig);
                    result.Add(c);
                }
                foreach (Dictionary<NumericOption, double> numConf in numericConfigs)
                {
                    Configuration c = new Configuration(binConfig, numConf);
                    result.Add(c);
                }
            }

            // Hybrid designs
            if (hybridStrategies.Count != 0)
            {
                List<Configuration> configurations = ExecuteHybridStrategy(hybridStrategies, vm);

                if (experimentalDesigns.Count == 0 && binaryStrategies.Count == 0)
                {
                    result = configurations;
                } else
                {
                    // Prepare the previous sample sets
                    if (result.Count == 0 && binaryConfigs.Count == 0)
                    {
                        foreach (Dictionary<NumericOption, double> numConf in numericConfigs)
                        {
                            Configuration c = new Configuration(new Dictionary<BinaryOption, BinaryOption.BinaryValue>(), numConf);
                            result.Add(c);
                        }
                    }


                    // Build the cartesian product
                    List<Configuration> newResult = new List<Configuration>();
                    foreach (Configuration config in result)
                    {
                        foreach (Configuration hybridConfiguration in configurations)
                        {
                            Dictionary<BinaryOption, BinaryOption.BinaryValue> binOpts = new Dictionary<BinaryOption, BinaryOption.BinaryValue>(config.BinaryOptions);
                            Dictionary<NumericOption, double> numOpts = new Dictionary<NumericOption, double>(config.NumericOptions);

                            Dictionary<BinaryOption, BinaryOption.BinaryValue> hybridBinOpts = hybridConfiguration.BinaryOptions;
                            foreach (BinaryOption binOpt in hybridConfiguration.BinaryOptions.Keys)
                            {
                                binOpts.Add(binOpt, hybridBinOpts[binOpt]);
                            }

                            Dictionary<NumericOption, double> hybridNumOpts = hybridConfiguration.NumericOptions;
                            foreach (NumericOption numOpt in hybridConfiguration.NumericOptions.Keys)
                            {
                                numOpts.Add(numOpt, hybridNumOpts[numOpt]);
                            }

                            newResult.Add(new Configuration(binOpts, numOpts));
                        }
                    }
                    result = newResult;
                }
            }


            if (vm.MixedConstraints.Count == 0)
            {
                if (binaryStrategies.Count == 1 && binaryStrategies.Last().Equals(SamplingStrategies.ALLBINARY) && experimentalDesigns.Count == 1 && experimentalDesigns.Last() is FullFactorialDesign)
                {
                    return result.ToList();
                } else
                {
                    return result.Distinct().ToList();
                }
            } else
            {
                List<Configuration> unfilteredList = result.Distinct().ToList();
                List<Configuration> filteredConfiguration = new List<Configuration>();
                foreach (Configuration toTest in unfilteredList)
                {
                    bool isValid = true;
                    foreach (MixedConstraint constr in vm.MixedConstraints)
                    {
                        if(!constr.requirementsFulfilled(toTest))
                        {
                            isValid = false;
                        }
                    }

                    if (isValid)
                    {
                        filteredConfiguration.Add(toTest);
                    }
                }
                return filteredConfiguration;
            }
        }

        private static List<Configuration> ExecuteHybridStrategy(List<HybridStrategy> hybridStrategies, VariabilityModel vm)
        {
            List<Configuration> allSampledConfigurations = new List<Configuration>();
            foreach (HybridStrategy hybrid in hybridStrategies)
            {
                hybrid.ComputeSamplingStrategy();
                allSampledConfigurations.AddRange(hybrid.selectedConfigurations);
            }
            return allSampledConfigurations;
        }

        private static void handleDesigns(List<ExperimentalDesign> samplingDesigns, List<Dictionary<NumericOption, Double>> numericOptions,
            VariabilityModel vm)
        {
            foreach (ExperimentalDesign samplingDesign in samplingDesigns)
            {
                SamplingStrategies currentSamplingStrategy = (SamplingStrategies)System.Enum.Parse(typeof(SamplingStrategies), samplingDesign.getName());
                if (optionsToConsider.ContainsKey(currentSamplingStrategy))
                    samplingDesign.setSamplingDomain(optionsToConsider[currentSamplingStrategy]);
                else
                    samplingDesign.setSamplingDomain(vm.getNonBlacklistedNumericOptions(blacklisted));
                samplingDesign.computeDesign();
                numericOptions.AddRange(samplingDesign.SelectedConfigurations);
            }
        } 

        public static void printSelectetedConfigurations_expDesign(List<Dictionary<NumericOption, double>> configurations)
        {
            GlobalState.varModel.NumericOptions.ForEach(x => GlobalState.logInfo.log(x.Name+" | "));
            GlobalState.logInfo.log("\n");
            foreach (Dictionary<NumericOption, double> configuration in configurations)
            {
                GlobalState.varModel.NumericOptions.ForEach(x =>
                {
                    if (configuration.ContainsKey(x))
                        GlobalState.logInfo.log(configuration[x] + " | ");
                    else
                        GlobalState.logInfo.log("\t | ");
                });
                GlobalState.logInfo.log("\n");
            }
        }
    }
}
