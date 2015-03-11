﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SPLConqueror_Core
{
    /// <summary>
    /// This class stores all information that are needed to perform multiple experiments with one case study. 
    /// </summary>
    public class GlobalState
    {
        public static InfoLogger logInfo = new InfoLogger(null);

        public static ErrorLogger logError = new ErrorLogger(null);

        /// <summary>
        /// The variability model of the case study. 
        /// </summary>
        public static VariabilityModel varModel = null;

        /// <summary>
        /// The property being considered. 
        /// </summary>
        public static NFProperty currentNFP = null;
        public static ResultDB allMeasurements = new ResultDB();

        public static ResultDB evalutionSet = new ResultDB();

        public static InfluenceModel infModel = null;

        /// <summary>
        /// If we require a configuration for learning, but haven't measured it, shall we use a similar one instead?
        /// </summary>
        public static bool takeSimilarConfig = true;

        /// <summary>
        /// All properties of the current case study. 
        /// </summary>
        public static Dictionary<string, NFProperty> nfProperties = new Dictionary<string,NFProperty>();

        private GlobalState(){ }

        /// <summary>
        /// Clears the global state. This mehtod should be used after performing all experiments of one case study. 
        /// </summary>
        public static void clear()
        {
            varModel = null;
            currentNFP = null;
            allMeasurements = new ResultDB();
            evalutionSet = new ResultDB();
            infModel = null;
            nfProperties = new Dictionary<string,NFProperty>();
        }


        /// <summary>
        /// The mehtod returns non function property with the given name. If there is no property with the name, a new property is created. 
        /// </summary>
        /// <param name="name">Name of the property</param>
        /// <returns>A non functional property with the specified name.</returns>
        public static NFProperty getOrCreateProperty(string name)
        {
            if(nfProperties.Keys.Contains(name))
                return nfProperties[name];
            else{
                NFProperty newProp = new NFProperty(name);
                nfProperties.Add(name, newProp);
                return newProp;
            }
        }

        /// <summary>
        /// Adds a configration to the global state. 
        /// </summary>
        /// <param name="config">An configuration of the variability model.</param>
        public static void addConfiguration(Configuration config)
        {
            GlobalState.allMeasurements.add(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public static void setDefaultProperty(String propertyName)
        {
            GlobalState.currentNFP = GlobalState.nfProperties[propertyName];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        public static void setDefaultProperty(NFProperty property)
        {
            GlobalState.currentNFP = property;
        }

        // TODO: switch from non-normalized configurations to normalized configurations



        /// <summary>
        /// This function gets a list of configurations and checks in the global state whether this configuration has a measured value and returns it if so.
        /// </summary>
        /// <param name="list">The list of configurations for which we want a measured value.</param>
        /// <returns>A list of configurations containinga measured value. Might be empty.</returns>
        public static List<Configuration> getMeasuredConfigs(List<Configuration> list)
        {
            List<Configuration> configsWithValues = new List<Configuration>();
            foreach(var config in list) {
                List<Configuration> similarOnes = new List<Configuration>();
                int nbCount = config.BinaryOptions.Count;
                if (config.BinaryOptions.Keys.Contains(varModel.Root))
                    nbCount--;
                bool found = false;
                foreach (var configInGS in GlobalState.allMeasurements.Configurations)
                {
                    if (config.Equals(configInGS))
                    {
                        configsWithValues.Add(configInGS);
                        found = true;
                        break;
                    }
                    else if (takeSimilarConfig)
                    {
                        var conf = findSimilarConfigBinary(config, configInGS, nbCount);
                        if(conf != null)
                            similarOnes.Add(conf);
                    }
                }
                if (!found) {
                    if (takeSimilarConfig && similarOnes.Count > 0)
                    {
                        configsWithValues.Add(findSimilarConfigNumeric(config, similarOnes));
                        logError.log("Substituted a not found configuration with a similar one.");
                    }
                    else
                    {
                        if (similarOnes.Count == 0)
                            logInfo.log(config.ToString());
                        logError.log("Did not find a measured value for the configuration: " + config.ToString());
                    }
                        
                }
            }
            return configsWithValues;
        }

        private static Configuration findSimilarConfigNumeric(Configuration config, List<Configuration> similarOnes)
        {
            Dictionary<NumericOption, int> stepInValueRange = new Dictionary<NumericOption, int>();
            foreach (var numOpt in config.NumericOptions.Keys)
            {
                stepInValueRange.Add(numOpt, numOpt.getStep(config.NumericOptions[numOpt]));
            }
            int minDistance = Int32.MaxValue;
            Configuration best = null;
            foreach (var conf in similarOnes)
            {
                int distance = 0;
                foreach (var numOpt in conf.NumericOptions.Keys)
                {
                    distance += Math.Abs(stepInValueRange[numOpt] - numOpt.getStep(conf.NumericOptions[numOpt]));
                }
                if (distance < minDistance)
                {
                    minDistance = distance;
                    best = conf;
                }
            }
            return best;
        }

        private static Configuration findSimilarConfigBinary(Configuration config, Configuration configInGS, int nbCount)
        {
            int nbCount2 = configInGS.BinaryOptions.Count;
            if (configInGS.BinaryOptions.Keys.Contains(varModel.Root))
                nbCount2--;
            if (nbCount != nbCount2)
                return null;
            foreach (var binOpt in config.BinaryOptions.Keys)
            {
                if (binOpt == varModel.Root)
                    continue;
                if (!configInGS.BinaryOptions.Keys.Contains(binOpt))
                {
                    return null;
                }
            }
            return configInGS;
        }
    }
}
