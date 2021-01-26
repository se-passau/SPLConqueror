using System;
using System.Collections.Generic;

namespace SPLConqueror_Core
{
    public class FMTree
    {

        public FMNode Root { get; }

        private Dictionary<string, FMNode> nodes = new Dictionary<string, FMNode>() ;

        public FMTree()
        {
            VariabilityModel vm = GlobalState.varModel;
            Root = new FMNode(vm.Root.Name, false, vm.Root);
            nodes.Add(vm.Root.Name, Root);
            List<BinaryOption> configurations = vm.WithAbstractBinaryOptions;
            configurations.ForEach(createFMNode);

        }

        private void createFMNode(BinaryOption option) {
            if (option.Name == Root.Name) return;
            FMNode fmNode = new FMNode(option.Name, option.Optional, option);
            nodes.Add(option.Name, fmNode);

            string parent = option.ParentName;
            FMNode parentNode;
            if (parent == "")
                parentNode = Root;
            else
                parentNode = nodes[parent];
            fmNode.Parent = parentNode;
            foreach (List<ConfigurationOption> list in option.Excluded_Options)
            {
                // TODO is it valid to throw an exception here?
                if (list.Count > 1)
                {
                    throw new NotSupportedException("The configuration " + option.Name + " has combinations of excluded options");
                }
                else if (list.Count == 0)
                {
                    continue;
                }
                fmNode.addExcludedOption(list[0].Name);
            }
            parentNode.addChild(fmNode);
        }

        public FMNode getNodeByName (string name)
        {
            return nodes[name];
        }

    }
}
