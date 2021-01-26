using System;
using System.Collections.Generic;
using System.Linq;
using SPLConqueror_Core;
using MachineLearning.Solver;
using MachineLearning.Sampling.Heuristics;
using MachineLearning.Sampling.ExperimentalDesigns;
using MachineLearning.Sampling.Hybrid;
using MachineLearning.Sampling.Heuristics.UniformHeuristics;
using System.Diagnostics;

namespace MachineLearning.Sampling
{
    public class ConfigurationBuilder
    {
        public static int binaryThreshold = 0;
        public static int binaryModulu = 0;
        public static Dictionary<SamplingStrategies, List<BinaryOption>> optionsToConsider = new Dictionary<SamplingStrategies, List<BinaryOption>>();
        public static BinaryParameters binaryParams = new BinaryParameters();

        // The default variant generator is the one using the CSP solver of the Microsoft solver foundation
        public static IVariantGenerator vg = new VariantGenerator();

        private static List<String> blacklisted;

        public static void setBlacklisted(List<String> blacklist)
        {
            ConfigurationBuilder.blacklisted = blacklist;
        }

        public static void clear()
        {
            binaryModulu = 0;
            binaryThreshold = 0;
            optionsToConsider = new Dictionary<SamplingStrategies, List<BinaryOption>>();
            binaryParams = new BinaryParameters();
        }

        public static List<Configuration> buildConfigs(VariabilityModel vm, List<SamplingStrategies> binaryStrategies,
            List<ExperimentalDesign> experimentalDesigns, List<HybridStrategy> hybridStrategies)
        {
            List<Configuration> result = new List<Configuration>();

            List<List<BinaryOption>> binaryConfigs = new List<List<BinaryOption>>();
            List<Dictionary<NumericOption, Double>> numericConfigs = new List<Dictionary<NumericOption, double>>();
            foreach (SamplingStrategies strat in binaryStrategies)
            {
                switch (strat)
                {
                    //Binary sampling heuristics
                    case SamplingStrategies.ALLBINARY:
                        if (optionsToConsider.ContainsKey(SamplingStrategies.ALLBINARY))
                        {
                            List<List<BinaryOption>> variants =
                                vg.GenerateAllVariantsFast(vm.reduce(optionsToConsider[SamplingStrategies.ALLBINARY]));
                            binaryConfigs.AddRange(changeModel(vm, variants));
                        }
                        else
                        {
                            binaryConfigs.AddRange(vg.GenerateAllVariantsFast(vm));
                        }
                        break;
                    case SamplingStrategies.SAT:
                        int numberSamples = 2;
                        foreach (Dictionary<string, string> parameters in binaryParams.satParameters)
                        {
                            if (parameters.ContainsKey("henard"))
                            {
                                try
                                {
                                    bool b = Boolean.Parse(parameters["henard"]);
                                    ((Z3VariantGenerator)vg).henard = b;
                                }
                                catch (FormatException e)
                                {
                                    Console.Error.WriteLine(e);
                                }
                            }
                            if (parameters.ContainsKey("numConfigs"))
                            {
                                try
                                {
                                    numberSamples = Int32.Parse(parameters["numConfigs"]);
                                }
                                catch (FormatException)
                                {
                                    TWise tw = new TWise();
                                    numberSamples = tw.generateT_WiseVariants_new(GlobalState.varModel, Int32.Parse(parameters["numConfigs"].Remove(0, 4))).Count;
                                }
                            }

                            if (parameters.ContainsKey("seed") && vg is Z3VariantGenerator)
                            {
                                uint seed = 0;
                                seed = UInt32.Parse(parameters["seed"]);
                                ((Z3VariantGenerator)vg).setSeed(seed);
                            }

                            Stopwatch stopwatch_sat = new Stopwatch();
                            stopwatch_sat.Start();

                            if (parameters.ContainsKey("measureTime")) {
                            }

                            if (optionsToConsider.ContainsKey(SamplingStrategies.SAT))
                            {
                                List<List<BinaryOption>> variants =
                                    vg.GenerateUpToNFast(vm.reduce(optionsToConsider[SamplingStrategies.SAT]), numberSamples);
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(vg.GenerateUpToNFast(vm, numberSamples));
                            }
                            stopwatch_sat.Stop();
                            Console.WriteLine("ConfigurationSampling done in {0} ms", stopwatch_sat.ElapsedMilliseconds);

                            numberSamples = 2;
                        }
                        break;
                    case SamplingStrategies.BINARY_RANDOM:
                        RandomBinary rb;
                        if (optionsToConsider.ContainsKey(SamplingStrategies.BINARY_RANDOM))
                        {
                            rb = new RandomBinary(vm.reduce(optionsToConsider[SamplingStrategies.BINARY_RANDOM]));
                        }
                        else
                        {
                            rb = new RandomBinary(vm);
                        }
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        foreach (Dictionary<string, string> expDesignParamSet in binaryParams.randomBinaryParameters)
                        {
                            binaryConfigs.AddRange(changeModel(vm, rb.getRandomConfigs(expDesignParamSet)));
                        }
                        stopwatch.Stop();
                        Console.WriteLine("ConfigurationSampling done in {0} ms", stopwatch.ElapsedMilliseconds);
                        
                        break;
                    case SamplingStrategies.OPTIONWISE:
                        {
                            FeatureWise fw = new FeatureWise();
                            if (optionsToConsider.ContainsKey(SamplingStrategies.OPTIONWISE))
                            {
                                List<List<BinaryOption>> variants = fw.generateFeatureWiseConfigurations(GlobalState.varModel
                                    .reduce(optionsToConsider[SamplingStrategies.OPTIONWISE]));
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(fw.generateFeatureWiseConfigurations(GlobalState.varModel));
                            }
                        }
                        break;
                    case SamplingStrategies.DISTANCE_BASED:
                        foreach (Dictionary<string, string> parameters in binaryParams.distanceMaxParameters)
                        {
                            DistanceBased distSampling = new DistanceBased(vm);
                            binaryConfigs.AddRange(distSampling.getSample(parameters));
                        }
                        break;

                    case SamplingStrategies.GRAMMAR_BASED:
                        foreach (Dictionary<string, string> parameters in binaryParams.grammarParameters)
                        {
                            UniformGrammarSamplingBigInt grammarSampling = new UniformGrammarSamplingBigInt(parameters);
                            binaryConfigs.AddRange(grammarSampling.selectedConfigurations);
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
                            if (optionsToConsider.ContainsKey(SamplingStrategies.PAIRWISE))
                            {
                                List<List<BinaryOption>> variants = pw.generatePairWiseVariants(GlobalState.varModel
                                    .reduce(optionsToConsider[SamplingStrategies.PAIRWISE]));
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(pw.generatePairWiseVariants(GlobalState.varModel));
                            }
                        }
                        break;
                    case SamplingStrategies.NEGATIVE_OPTIONWISE:
                        {
                            NegFeatureWise neg = new NegFeatureWise();//2nd option: neg.generateNegativeFWAllCombinations(GlobalState.varModel));
                            if (optionsToConsider.ContainsKey(SamplingStrategies.NEGATIVE_OPTIONWISE))
                            {
                                List<List<BinaryOption>> variants = neg.generateNegativeFW(GlobalState.varModel
                                    .reduce(optionsToConsider[SamplingStrategies.NEGATIVE_OPTIONWISE]));
                                binaryConfigs.AddRange(changeModel(vm, variants));
                            }
                            else
                            {
                                binaryConfigs.AddRange(neg.generateNegativeFW(GlobalState.varModel));
                            }
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

                                Stopwatch stopwatch_t = new Stopwatch();
                                stopwatch_t.Start();

                                if (optionsToConsider.ContainsKey(SamplingStrategies.T_WISE))
                                {
                                    List<List<BinaryOption>> variants = tw.generateT_WiseVariants_new(
                                        vm.reduce(optionsToConsider[SamplingStrategies.T_WISE]), t);
                                    binaryConfigs.AddRange(changeModel(vm, variants));
                                }
                                else
                                {
                                    binaryConfigs.AddRange(tw.generateT_WiseVariants_new(vm, t));
                                }
                                stopwatch_t.Stop();
                                Console.WriteLine("ConfigurationSampling done in {0} ms", stopwatch_t.ElapsedMilliseconds);
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

            if (vm.NumericOptions.Any(x => x.Optional))
            {
                createConfigurationsOptNumeric(vm, binaryConfigs, numericConfigs, result);
            } else
            {
                createConfigurations(binaryConfigs, numericConfigs, result);
            }
            
            result = filterNonBoolean(result, vm);

            // Hybrid designs
            if (hybridStrategies.Count != 0)
            {
                List<Configuration> configurations = ExecuteHybridStrategy(hybridStrategies, vm);

                if (experimentalDesigns.Count == 0 && binaryStrategies.Count == 0)
                {
                    result = configurations;
                }
                else
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
                    return replaceReference(result.ToList());
                }
                else
                {
                    return replaceReference(result.Distinct().ToList());
                }
            }
            else
            {
                List<Configuration> unfilteredList = result.Distinct().ToList();
                List<Configuration> filteredConfiguration = new List<Configuration>();
                foreach (Configuration toTest in unfilteredList)
                {
                    bool isValid = true;
                    foreach (MixedConstraint constr in vm.MixedConstraints)
                    {
                        if (!constr.requirementsFulfilled(toTest))
                        {
                            isValid = false;
                            break;
                        }
                    }

                    if (isValid)
                    {
                        filteredConfiguration.Add(toTest);
                    }
                }
                return replaceReference(filteredConfiguration);
            }
        }

        private static void createConfigurations(List<List<BinaryOption>> binaryParts, List<Dictionary<NumericOption, Double>> numericParts, List<Configuration> results)
        {
            foreach (List<BinaryOption> binConfig in binaryParts)
            {
                if (numericParts.Count == 0)
                {
                    Configuration c = new Configuration(binConfig);
                    results.Add(c);
                }
                foreach (Dictionary<NumericOption, double> numConf in numericParts)
                {
                    Configuration c = new Configuration(binConfig, numConf);
                    results.Add(c);
                }
            }
        }

        private static void createConfigurationsOptNumeric(VariabilityModel vm, List<List<BinaryOption>> binaryParts, List<Dictionary<NumericOption, Double>> numericParts, List<Configuration> results)
        {
            IEnumerable<NumericOption> optionalOptions = vm.NumericOptions.Where(x => x.Optional);
            foreach (List<BinaryOption> binConfig in binaryParts)
            {
                // Get all abstract binary options
                var abstractOptions = binConfig.Where(x => x.IsStrictlyAbstract);

                // get all currently deselected numeric options
                List<NumericOption> currentDeselected = optionalOptions
                    .Where(x => !abstractOptions.Contains(x.abstractEnabledConfigurationOption())).ToList();
                currentDeselected.ForEach(x => binConfig.Add(x.abstractDisabledConfigurationOption()));

                List<String> alreadyAdded = new List<String>();
                foreach (Dictionary<NumericOption, double> numConf in numericParts)
                {
                    Dictionary<NumericOption, double> buff = new Dictionary<NumericOption, double>();
                    numConf.ToList().ForEach(x => { buff[x.Key] = currentDeselected.Contains(x.Key) ? x.Key.OptionalFlag : x.Value; });
                    var selectedPart = String.Join(";", buff.Select(x => x.Value));
                    if (!alreadyAdded.Contains(selectedPart))
                    {
                        alreadyAdded.Add(selectedPart);
                    } else 
                    {
                        continue;
                    }
                    Configuration c = new Configuration(binConfig, buff);
                    results.Add(c);
                }
            }
        }

        private static List<Configuration> filterNonBoolean(List<Configuration> intermediateResults, VariabilityModel vm)
        {
            // Filter configurations based on the NonBooleanConstratins
            List<Configuration> filtered = new List<Configuration>();
            foreach (Configuration conf in intermediateResults)
            {
                bool isValid = true;
                foreach (NonBooleanConstraint nbc in vm.NonBooleanConstraints)
                {
                    if (!nbc.configIsValid(conf))
                    {
                        isValid = false;
                        continue;
                    }
                }

                if (isValid)
                    filtered.Add(conf);
            }
            return filtered;
        }

        private static List<Configuration> replaceReference(List<Configuration> sampled)
        {
            // Replaces the reference of the sampled configuration with the corresponding measured configurstion if it exists
            if (GlobalState.varModel.AbrstactOptions.Count > 0)
            {

            } else { }
            var measured = GlobalState.allMeasurements.Configurations.Intersect(sampled);
            var notMeasured = sampled.Except(measured);
            return measured.Concat(notMeasured).ToList();
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

        private static List<List<BinaryOption>> changeModel(VariabilityModel vm, List<List<BinaryOption>> variants)
        {
            List<List<BinaryOption>> toReturn = new List<List<BinaryOption>>();

            foreach (List<BinaryOption> variant in variants)
            {
                List<BinaryOption> variantInRightModel = new List<BinaryOption>();

                foreach (BinaryOption opt in variant)
                {
                    variantInRightModel.Add(vm.getBinaryOption(opt.Name));
                }

                toReturn.Add(variantInRightModel);
            }

            return toReturn;
        }

        private static void handleDesigns(List<ExperimentalDesign> samplingDesigns, List<Dictionary<NumericOption, Double>> numericOptions,
            VariabilityModel vm)
        {
            foreach (ExperimentalDesign samplingDesign in samplingDesigns)
            {
                if (samplingDesign.getSamplingDomain() == null ||
                    samplingDesign.getSamplingDomain().Count == 0)
                {
                    samplingDesign.setSamplingDomain(vm.getNonBlacklistedNumericOptions(blacklisted));
                }
                else
                {
                    samplingDesign.setSamplingDomain(vm.getNonBlacklistedNumericOptions(blacklisted)
                        .Intersect(samplingDesign.getSamplingDomain()).ToList());
                }

                samplingDesign.computeDesign();
                numericOptions.AddRange(samplingDesign.SelectedConfigurations);
            }
        }

        public static void printSelectetedConfigurations_expDesign(List<Dictionary<NumericOption, double>> configurations)
        {
            GlobalState.varModel.NumericOptions.ForEach(x => GlobalState.logInfo.log(x.Name + " | "));
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
