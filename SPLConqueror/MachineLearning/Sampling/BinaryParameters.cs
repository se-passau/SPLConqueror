using System.Collections.Generic;

namespace MachineLearning.Sampling
{
    public class BinaryParameters
    {
        public List<Dictionary<string, string>> tWiseParameters { get; set; }
        public List<Dictionary<string, string>> randomBinaryParameters { get; set; }
        public List<Dictionary<string, string>> satParameters { get; set; }
        public List<Dictionary<string, string>> distanceMaxParameters { get; set; }
        public List<Dictionary<string, string>> grammarParameters { get; set; }

        public BinaryParameters()
        {
            tWiseParameters = new List<Dictionary<string, string>>();
            randomBinaryParameters = new List<Dictionary<string, string>>();
            satParameters = new List<Dictionary<string, string>>();
            distanceMaxParameters = new List<Dictionary<string, string>>();
            grammarParameters = new List<Dictionary<string, string>>();
        }

    }
}
