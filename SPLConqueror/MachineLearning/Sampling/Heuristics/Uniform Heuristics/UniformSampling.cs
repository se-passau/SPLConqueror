using System;
using System.Collections.Generic;
using SPLConqueror_Core;

namespace MachineLearning.Sampling.Heuristics.UniformHeuristics
{
    public abstract class UniformSampling
    {
        /// <summary>
        /// This list contains the configurations that were selected by the sampling strategy.
        /// </summary>
        public List<List<BinaryOption>> selectedConfigurations = new List<List<BinaryOption>>();

        /// <summary>
        /// This list contains the options, which have to be considered.
        /// </summary>
        protected List<ConfigurationOption> optionsToConsider = null;

        public UniformSampling()
        {
        }

        /// <summary>
        /// Sets the sampling domain to the given one.
        /// </summary>
        /// <param name="samplingDomain">a <see cref="List"/> of <see cref="ConfigurationOption"/> containing the configuration options that have to be considered by the sampling strategy</param>
        public void SetSamplingDomain(List<ConfigurationOption> samplingDomain)
        {
            this.optionsToConsider = samplingDomain;
        }

        /// <summary>
        /// Computes the sampling set using the provided parameters. 
        /// </summary>
        /// <returns><code>True</code> if the computation was successful.</returns>
        public abstract bool ComputeSamplingStrategy();

        /// <summary>
        /// The Methods returns the name of the experimental design.
        /// </summary>
        /// <returns>Name of the experimental design.</returns>
        public abstract string GetName();

        /// <summary>
        /// Return the tag that identifies the hybrid design.
        /// </summary>
        /// <returns>Tag of the hybrid design as string.</returns>
        public abstract string GetTag();
    }
}
