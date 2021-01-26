using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SPLConqueror_Core;

namespace MachineLearning.Sampling.Heuristics
{
    public class FeatureWise
    {
        private List<List<BinaryOption>> configurations = new List<List<BinaryOption>>();

        /// <summary>
        /// Generates configurations based on the feature-wise heuristic: The method utilizes the structure of the variability model by first determining the set of options that are always selected.
        /// In each iteration, it starts from this set of options (partial configuration) and adds a single option to this partial configurations. 
        /// It then checks whether configuration is valid and if not calls a CSP solver to make this configuration valid with as few selected options as possible.
        /// </summary>
        /// <param name="vm">The variability model for which the feature-wise configurations should be generated.</param>
        /// <returns>A list of configurations, in which each configuration is a list of binary options that represent the SELECTED options.</returns>
        public List<List<BinaryOption>> generateFeatureWiseConfigurations(VariabilityModel vm)
        {
            configurations.Clear();
            List<BinaryOption> optionalFirstLevelElements = new List<BinaryOption>();
            List<BinaryOption> binOptions = vm.WithAbstractBinaryOptions;

            //First: Add options that are present in all configurations
            List<BinaryOption> firstLevelMandatoryFeatures = new List<BinaryOption>();
            foreach (BinaryOption binOpt in binOptions)
            {
                if (binOpt.Parent == null || binOpt.Parent == vm.Root)
                {
                    if (!binOpt.Optional)
                    {
                        if (!binOpt.hasAlternatives())
                        {
                            firstLevelMandatoryFeatures.Add(binOpt);
                        }
                    }
                    else
                        optionalFirstLevelElements.Add(binOpt);
                }
            }
            Solver.ICheckConfigSAT checkSAT = new Solver.CheckConfigSATZ3();
            //Generating new configurations: one per option
            if (checkSAT.checkConfigurationSAT(firstLevelMandatoryFeatures, vm, false))
                this.configurations.Add(firstLevelMandatoryFeatures);
            foreach (BinaryOption e in binOptions)
            {
                BinaryOption[] temp = new BinaryOption[firstLevelMandatoryFeatures.Count];
                firstLevelMandatoryFeatures.CopyTo(temp);
                List<BinaryOption> tme = temp.ToList<BinaryOption>();
                if (!tme.Contains(e))
                {
                    tme.Add(e);
                    if (checkSAT.checkConfigurationSAT(tme, vm, false))
                    {
                        if (!this.configurations.Contains(tme))
                            this.configurations.Add(tme);
                    }
                    else
                    {
                        tme = ConfigurationBuilder.vg.MinimizeConfig(tme, vm, true, null);
                        if (tme != null && Configuration.containsBinaryConfiguration(this.configurations, tme) == false)
                            this.configurations.Add(tme);
                    }
                }
                else
                    continue;
            }

            return this.configurations;
        }

        /// <summary>
        /// This algorithm calls for each binary option in the variability model the CSP solver to generate a valid, minimal configuration containing that option.
        /// </summary>
        /// <param name="vm">The variability model for which the feature-wise configurations should be generated.</param>
        /// <returns>A list of configurations, in which each configuration is a list of binary options that represent the SELECTED options.</returns>
        public List<List<BinaryOption>> generateFeatureWiseConfigsCSP(VariabilityModel vm)
        {
            this.configurations.Clear();
            foreach (var opt in vm.WithAbstractBinaryOptions)
            {
                if (opt == vm.Root)
                    continue;
                List<BinaryOption> temp = new List<BinaryOption>();
                temp.Add(opt);
                temp = ConfigurationBuilder.vg.MinimizeConfig(temp, vm, true, null);
                if (temp != null && Configuration.containsBinaryConfiguration(this.configurations, temp) == false)
                    this.configurations.Add(temp);

                //Now finding a configuration without the current option, but with all other options to be able to compute a delta
                List<BinaryOption> withoutOpt = new List<BinaryOption>();
                BinaryOption[] tempArray = temp.ToArray();
                withoutOpt = tempArray.ToList<BinaryOption>();
                withoutOpt.Remove(opt);
                List<BinaryOption> excluded = new List<BinaryOption>();
                excluded.Add(opt);
                withoutOpt = ConfigurationBuilder.vg.MinimizeConfig(withoutOpt, vm, true, excluded);
                if (withoutOpt != null && Configuration.containsBinaryConfiguration(this.configurations, withoutOpt) == false)
                    this.configurations.Add(withoutOpt);
            }

            return this.configurations;
        }
    }
}
