using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.Sampling
{
    public enum SamplingStrategies
    {
        ALLBINARY, OPTIONWISE, PAIRWISE, NEGATIVE_OPTIONWISE, BINARY_RANDOM, T_WISE, DISTANCE_BASED, GRAMMAR_BASED,

        BOXBEHNKEN, CENTRALCOMPOSITE, FACTORIAL, FULLFACTORIAL, HYPERSAMPLING, ONEFACTORATATIME, KEXCHANGE, PLACKETTBURMAN, RANDOM, SAT
    }
}
