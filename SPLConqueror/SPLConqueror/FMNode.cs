using System;
using System.Collections.Generic;

namespace SPLConqueror_Core
{
    public class FMNode
    {
        private string name;

        public string Name { get { return name; }  set { name = value; } }

        public void SetName(string value) => name = value;

        public FMNode Parent { get; set; }

        public bool Optional { get; set; }

        public BinaryOption ActualOption { get; set; }

        private List<FMNode> children = new List<FMNode>();

        private List<string> excludedOptions = new List<string>();

        public List<FMNode> Children
        {
            get { return children; }
        }

        public List<ConfigurationOption> AltGroup { get; set; }
        public List<string> ExcludedOptions { get { return excludedOptions; } }

        public FMNode(string name, bool optional, BinaryOption binOption)
        {
            SetName(name);
            Optional = optional;
            ActualOption = binOption;
            AltGroup = new List<ConfigurationOption>();
        }

        public void addChild(FMNode child)
        {
            Children.Add(child);
        }

        public void addExcludedOption(string optionName)
        {
            ExcludedOptions.Add(optionName);
        }
    }
}
