using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace SPLConqueror_Core
{
    /// <summary>
    /// Central model to store all configuration options and their constraints.
    /// </summary>
    public class VariabilityModel
    {
        private List<NumericOption> numericOptions = new List<NumericOption>();

        /// <summary>
        /// The set of numeric configuration options of the variability model.
        /// </summary>
        public List<NumericOption> NumericOptions
        {
            get { return numericOptions; }
        }

        private List<BinaryOption> binaryOptions = new List<BinaryOption>();

        /// <summary>
        /// The set of all binary configuration options of the system.
        /// </summary>
        public List<BinaryOption> BinaryOptions
        {
            get { return binaryOptions; }
        }

        /// <summary>
        /// The set of all binary configuration options of the system including abstract options.
        /// </summary>
        public List<BinaryOption> WithAbstractBinaryOptions
        {
            get { return binaryOptions.Concat(abstractOptions).ToList(); }
        }

        List<BinaryOption> abstractOptions = new List<BinaryOption>();

        /// <summary>
        /// The set of internal abstract configuration options.
        /// </summary>
        public List<BinaryOption> AbrstactOptions
        {
            get { return abstractOptions; }
        }

        /// <summary>
        /// A mapping from the index of an option to the object providing all information of the configuratio option.
        /// </summary>
        public Dictionary<int, ConfigurationOption> optionToIndex = new Dictionary<int, ConfigurationOption>();

        /// <summary>
        /// A mapping from a configuration option to its index.
        /// </summary>
        public Dictionary<ConfigurationOption, int> indexToOption = new Dictionary<ConfigurationOption, int>();

        String name = "empty";

        /// <summary>
        /// Name of the variability model or configurable system.
        /// </summary>
        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        String path = "";

        /// <summary>
        /// Local path, in which the model is located
        /// </summary>
        public String Path
        {
            get { return path; }
            set { path = value; }
        }

        private BinaryOption root = null;

        /// <summary>
        /// The root binary configuration option.
        /// </summary>
        public BinaryOption Root
        {
            get { return root; }
        }

        private List<String> binaryConstraints = new List<string>();

        /// <summary>
        /// The set of all constraints among the binary configuration options.
        /// </summary>
        public List<String> BinaryConstraints
        {
            get { return binaryConstraints; }
            set { binaryConstraints = value; }
        }

        private List<NonBooleanConstraint> nonBooleanConstraints = new List<NonBooleanConstraint>();

        /// <summary>
        /// The list of all non-boolean constraints of the variability model. Non-boolean constraints are constraints among different numeric 
        /// options or binary and numeric options.
        /// </summary>
        public List<NonBooleanConstraint> NonBooleanConstraints
        {
            get { return nonBooleanConstraints; }
            set { nonBooleanConstraints = value; }
        }

        private List<MixedConstraint> mixedConstraints = new List<MixedConstraint>();

        /// <summary>
        /// The list of all constraints where binary and numeric options participate in.
        /// </summary>
        public List<MixedConstraint> MixedConstraints
        {
            get { return mixedConstraints; }
            set { mixedConstraints = value; }
        }

        /// <summary>
        /// Retuns a list containing all numeric configuration options that are considered in the learning process.
        /// </summary>
        /// <param name="blacklist">A list containing all numeric options that should not be considered in the learning process.</param>
        /// <returns>A list containing all numeric configuartion options that are considered in the learning process.</returns>
        public List<NumericOption> getNonBlacklistedNumericOptions(List<String> blacklist)
        {
            List<NumericOption> result = new List<NumericOption>();
            foreach (NumericOption opt in this.numericOptions)
            {
                if (blacklist != null)
                {
                    if (!blacklist.Contains(opt.Name.ToLower()))
                    {
                        result.Add(opt);
                    }
                }
                else
                {
                    result.Add(opt);
                }
            }

            return result;
        }

        /// <summary>
        /// Creastes a new variability model with a given name that consists only of a binary root option.
        /// </summary>
        /// <param name="name">The name of the variability model.</param>        

        public VariabilityModel(String name)
        {
            this.name = name;
            if (root == null)
                root = new BinaryOption(this, "root");
            this.BinaryOptions.Add(root);
        }

        /// <summary>
        /// Stores the current variability model into an XML file with the already stored path
        /// </summary>
        /// <returns>Returns true if sucessfully saved, false otherwise</returns>
        public bool saveXML()
        {
            if (this.path.Length > 0)
                return saveXML(this.path);
            else
                return false;
        }

        /// <summary>
        /// Stores the current variability model into an XML file
        /// </summary>
        /// <param name="path">Path to which the XML file is stored</param>
        /// <returns>Returns false if the saving was not successfull</returns>
        public bool saveXML(String path)
        {
            //Create XML Document
            XmlDocument doc = new XmlDocument();

            //Create an XML declaration. 
            XmlDeclaration xmldecl;
            xmldecl = doc.CreateXmlDeclaration("1.0", null, null);

            //Add a root node to the document.
            XmlNode xmlroot = doc.CreateNode(XmlNodeType.Element, "vm", ""); // ***

            XmlAttribute xmlattr = doc.CreateAttribute("name");
            xmlattr.Value = this.name;
            xmlroot.Attributes.Append(xmlattr);

            //Add binary options
            XmlNode xmlBin = doc.CreateNode(XmlNodeType.Element, "binaryOptions", "");
            foreach (BinaryOption binOpt in this.binaryOptions)
            {
                xmlBin.AppendChild(binOpt.saveXML(doc));
            }
            xmlroot.AppendChild(xmlBin);

            //Add numeric options
            XmlNode xmlNum = doc.CreateNode(XmlNodeType.Element, "numericOptions", "");
            foreach (NumericOption numOpt in this.numericOptions)
            {
                xmlNum.AppendChild(numOpt.saveXML(doc));
            }
            xmlroot.AppendChild(xmlNum);

            //Add boolean constraints
            XmlNode boolConstraints = doc.CreateNode(XmlNodeType.Element, "booleanConstraints", "");
            foreach (var constraint in this.binaryConstraints)
            {
                XmlNode conNode = doc.CreateNode(XmlNodeType.Element, "constraint", "");
                conNode.InnerText = constraint;
                boolConstraints.AppendChild(conNode);
            }
            xmlroot.AppendChild(boolConstraints);

            //Add non-boolean constraints
            XmlNode nonBooleanConstraints = doc.CreateNode(XmlNodeType.Element, "nonBooleanConstraints", "");
            foreach (var constraint in this.nonBooleanConstraints)
            {
                XmlNode conNode = doc.CreateNode(XmlNodeType.Element, "constraint", "");
                conNode.InnerText = constraint.ToString();
                nonBooleanConstraints.AppendChild(conNode);
            }
            xmlroot.AppendChild(nonBooleanConstraints);

            //Add mixed constraints
            XmlNode mixedConstraints = doc.CreateNode(XmlNodeType.Element, "mixedConstraints", "");
            foreach (var constraint in this.MixedConstraints)
            {
                XmlNode constrNode = doc.CreateNode(XmlNodeType.Element, "constraint", "");
                XmlAttribute attr = doc.CreateAttribute("req");
                XmlAttribute evaluation = doc.CreateAttribute("exprKind");
                string constraintAsString = constraint.ToString();
                if (constraintAsString.StartsWith("!:"))
                {
                    constraintAsString = constraintAsString.Replace("!:", "");
                    evaluation.Value = "neg";
                }
                else
                {
                    evaluation.Value = "pos";
                }
                attr.Value = constraintAsString.Split(new char[] { ':' })[0];
                constrNode.Attributes.Append(attr);
                constrNode.Attributes.Append(evaluation);
                constrNode.InnerText = constraintAsString.Split(new char[] { ':' })[1];
                mixedConstraints.AppendChild(constrNode);
            }
            xmlroot.AppendChild(mixedConstraints);

            doc.AppendChild(xmlroot);

            try
            {
                doc.Save(path);
            }
            catch (Exception e)
            {
                GlobalState.logError.logLine(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Read a Feature model in SXFM format and create a new VaribilityModel that 
        /// equals the Feature model.
        /// </summary>
        /// <param name="path">The path of the SXFM Feature model.</param>
        /// <returns>VaribilityModel object that equals the SXFM Feature model.</returns>
        public static VariabilityModel loadFromSXFM(string path)
        {
            VariabilityModel model = new VariabilityModel("to_change");
            if (model.loadSXFM(path))
            {
                return model;
            }
            else
            {
                return null;
            }
        }

        public static VariabilityModel loadFromDimacs(string path)
        {
            VariabilityModel model = new VariabilityModel("diamcs");
            if (model.loadDimacs(path))
            {
                return model;
            } else
            {
                return null;
            }
        }

        private bool loadDimacs(string path)
        {
            if (!File.Exists(path))
                return false;

            Dictionary<String, String> variableMapping = new Dictionary<string, string>();
            bool headerDefinition = true;

            StreamReader sr = new StreamReader(path);
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();

                if (line.StartsWith("c"))
                {
                    if (headerDefinition)
                    {
                        string[] parts = line.Split(new string[] { " " }, StringSplitOptions.None);
                        if (parts.Length == 3)
                        {
                            variableMapping[parts[1]] = parts[2];
                        }
                    } else
                    {
                        continue;
                    }
                }
                else if (line.StartsWith("p"))
                {
                    headerDefinition = false;

                    string[] header = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (!(header[1] == "cnf"))
                        throw new ArgumentException("Invalid dimacs format. Only cnf is supported");

                    int numberConfigurationOptions;
                    if (!Int32.TryParse(header[2], out numberConfigurationOptions) | numberConfigurationOptions < 1)
                        throw new ArgumentException("Invalid dimacs format. " +
                            "Expected number of configuration options.");

                    Enumerable.Range(1, numberConfigurationOptions).Select(x =>
                        {
                            string name = x.ToString();
                            
                            if (variableMapping.ContainsKey(name))
                            {
                                name = variableMapping[name];
                            } else
                            {
                                variableMapping[name] = name;
                            }

                            BinaryOption bin = new BinaryOption(this, name);
                            bin.Optional = true;
                            return bin;
                        })
                        .ToList().ForEach(opt => this.addConfigurationOption(opt));
                } else if (line != "")
                {
                    if (!line.EndsWith("0"))
                        throw new ArgumentException("Invalid dimacs format. Expected a cnf clause");

                    this.binaryConstraints.Add(
                        String.Join(" | ", line
                            .Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(variable => variable != "0")
                            .Select(variable => variable.Replace("-", "!"))
                            .Select(variable => variable.StartsWith("!") ? 
                                "!" + variableMapping[variable.Substring(1)] : variableMapping[variable])
                        )
                    );
                }
            }
            return true;
        }

        public bool saveSXFM()
        {
            if (this.path.Length > 0)
                return saveSXFM(this.path);
            else
                return false;
        }

        public bool saveSXFM(string path)
        {
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                return false;
            if (numericOptions.Count > 0)
                throw new ArgumentException("Variability Models with numeric options can not be converted to SXFM");
            StringBuilder sxfm = new StringBuilder();
            List<string> implicitConstraints;
            List<string> mixedConstraints = convertMixedConstraints();
            sxfm.AppendLine("<feature_model name=\"" + this.name + "\">");
            sxfm.AppendLine("<feature_tree>");
            sxfm.Append(convertFeatureTree(out implicitConstraints));
            sxfm.AppendLine("</feature_tree>");
            sxfm.AppendLine("<constraints>");
            sxfm.Append(convertConstraintsToSXFM(implicitConstraints, mixedConstraints));
            sxfm.AppendLine("</constraints>");
            sxfm.AppendLine("</feature_model>");
            StreamWriter sr = new StreamWriter(path);
            sr.Write(sxfm);
            sr.Flush();
            sr.Close();
            return true;
        }

        private List<string> convertMixedConstraints()
        {
            List<string> mixedConstraints = new List<string>();
            foreach(MixedConstraint mixed in this.mixedConstraints)
            {
                List<BinaryOption> participatingOptions = mixed.leftHandSide.participatingBoolOptions
                    .Union(mixed.rightHandSide.participatingBoolOptions).Distinct().ToList();
                List<List<BinaryOption>> cartProduct = new List<List<BinaryOption>>();
                cartProduct.Add(new List<BinaryOption>());
                foreach(BinaryOption binOpt in participatingOptions)
                {
                    List<List<BinaryOption>> additions = new List<List<BinaryOption>>();
                    cartProduct.ForEach(partialConfig =>
                    {
                        List<BinaryOption> withOption = new List<BinaryOption>(partialConfig);
                        withOption.Add(binOpt);
                        additions.Add(withOption);
                    });
                    cartProduct.AddRange(additions);
                }

                foreach(List<BinaryOption> configuration in cartProduct)
                {
                    if (!mixed.requirementsFulfilled(new Configuration(configuration)))
                    {
                        List<string> configNegationAsString = configuration.Select(x => "!" + x.Name)
                            .Union(participatingOptions.Except(configuration).Select(x => x.Name)).ToList();
                        mixedConstraints.Add(String.Join(" | ", configNegationAsString));
                    }
                }
            }
            return mixedConstraints;
        }

        private string convertFeatureTree(out List<string> implicitConstraints)
        {
            int currentDepth = 0;
            int abstractCounter = 0;
            char tab = '\t';
            StringBuilder treeBuilder = new StringBuilder();
            List<Tuple<int,BinaryOption>> queue = new List<Tuple<int,BinaryOption>>();
            queue.Add(Tuple.Create(currentDepth,this.Root));
            List<BinaryOption> noRoot = BinaryOptions.Where(opt => opt != this.Root).ToList();
            string optional = ":o ";
            string mandatory = ":m ";
            string groupChild = ": ";
            string exactlyOne = ":g [1,1] ";
            string atMostOne = ":g [0,1] ";
            string root = ":r ";

            implicitConstraints = new List<string>();

            List<BinaryOption> groupParents = new List<BinaryOption>();

            while (queue.Count > 0)
            {
                Tuple<int,BinaryOption> current = queue.Last();
                queue.RemoveAt(queue.Count - 1);
                BinaryOption currentBinOpt = current.Item2;
                currentDepth = current.Item1;

                int indentationOffset = 0;

                List<BinaryOption> allChildren = noRoot
                    .Where(opt => opt.Parent.Equals(currentBinOpt)).ToList();

                string indentation = new string(tab, currentDepth);
                
                BinaryOption first = allChildren.Count >  0 ? allChildren.First() : null;
                bool isAlternativeGroupParent = first != null && allChildren
                    .Where(x => !x.Equals(first)).ToList()
                    .TrueForAll(
                        x => first.Excluded_Options.Exists(group => group.Contains(x))
                    );

                if (currentBinOpt.Parent == null)
                {
                    treeBuilder.AppendLine(indentation + root + sxfmIdentifier(currentBinOpt));
                }
                else if (allChildren.Count == 0)
                {
                    if (groupParents.Contains(currentBinOpt.Parent)) 
                    {
                        treeBuilder.AppendLine(indentation + groupChild + sxfmIdentifier(currentBinOpt));
                    } else if (currentBinOpt.Optional)
                    {
                        treeBuilder.AppendLine(indentation + optional + sxfmIdentifier(currentBinOpt));
                    } else
                    {
                        treeBuilder.AppendLine(indentation + mandatory + sxfmIdentifier(currentBinOpt));
                    }
                }
                else
                {
                    if (currentBinOpt.Optional)
                    {
                        treeBuilder.Append(indentation + optional + sxfmIdentifier(currentBinOpt));
                        if (isAlternativeGroupParent &&
                            (allChildren.TrueForAll(x => !x.Optional) || allChildren.TrueForAll(x => x.Optional)))
                        {
                            groupParents.Add(currentBinOpt);
                            indentationOffset++;
                            string groupName = "Group_" + abstractCounter + "(Group_" + abstractCounter++ + ")";
                            string cardinality = allChildren.TrueForAll(x => !x.Optional) ? exactlyOne : atMostOne;
                            treeBuilder.AppendLine(tab + indentation + cardinality + groupName);
                        }
                    }
                    else if (isAlternativeGroupParent && allChildren.TrueForAll(x => !x.Optional))
                    {
                        groupParents.Add(currentBinOpt);
                        treeBuilder.AppendLine(indentation + exactlyOne + sxfmIdentifier(currentBinOpt));
                    }
                    else if (isAlternativeGroupParent && allChildren.TrueForAll(x => x.Optional))
                    {
                        groupParents.Add(currentBinOpt);
                        treeBuilder.AppendLine(indentation + atMostOne + sxfmIdentifier(currentBinOpt));
                    }
                    else
                    {
                        treeBuilder.AppendLine(indentation + mandatory + sxfmIdentifier(currentBinOpt));
                    }
                }

                queue.AddRange(allChildren
                      .Select(opt => Tuple.Create(currentDepth + 1 + indentationOffset, opt)));

                implicitConstraints.AddRange(handleCrossTreeConstraints(currentBinOpt));
            }

            return treeBuilder.ToString();
        }

        private List<string> handleCrossTreeConstraints(BinaryOption binOpt)
        {
            List<string> constraints = new List<string>();
            binOpt.Implied_Options.ForEach(implicationGroup =>
                {
                    constraints.Add("~" + binOpt.Name + " or " 
                        + String.Join(" or ", implicationGroup.Select(opt => opt.Name)));
                }
            );

            List<ConfigurationOption> trueExclusives = binOpt.Excluded_Options.SelectMany(i => i).ToList();
            List<ConfigurationOption> alternatives = trueExclusives
                .Where(x => x.Parent.Equals(binOpt.Parent)).ToList();

            if (!binOpt.Optional)
            {
                trueExclusives = trueExclusives.Where(x => !alternatives.Contains(x)).ToList();
            }
            trueExclusives.ForEach(x => constraints.Add("~" + binOpt.Name + " or ~" + x.Name));
            if (!alternatives.TrueForAll(x => ((BinaryOption)x).Optional) 
                && !alternatives.TrueForAll(x => !((BinaryOption)x).Optional))
            {
                alternatives.ForEach(x => constraints.Add("~" + binOpt.Name + " or ~" + x.Name));
            }

            return constraints;
        }

        private static string sxfmIdentifier(BinaryOption binOpt)
        {
            return binOpt.Name + "(" + binOpt.Name + ")";
        }

        private string convertConstraintsToSXFM(List<string> implicitConstraints, List<string> mixedConstraints)
        {
            int constraintCounter = 0;
            StringBuilder constraints = new StringBuilder();
            this.BinaryConstraints.Union(implicitConstraints).Union(mixedConstraints).ToList().ForEach(constraintExpr =>
            {
                constraintExpr.Split(new char[] { '&' }).ToList()
                .ForEach(orExpr => constraints.AppendLine("Constraint_" + constraintCounter++ + ":" 
                + orExpr.Replace("!", "~").Replace(" | ", " or ")));
            });
            return constraints.ToString();
        }

        /// <summary>
        /// Read a SXFM Feature Model and store all information in this VaribilityModel object.
        /// </summary>
        /// <param name="path">The path of the feature model.</param>
        /// <returns>True if reading the model was succesful false otherwise.</returns>
        public bool loadSXFM(string path)
        {
            if (!File.Exists(path))
                return false;

            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(path);
            }
            catch (XmlException)
            {
                return false;
            }

            XmlElement root = doc.DocumentElement;
            this.name = root.Attributes["name"].Value.ToString();

            XmlNode featureTree = root.SelectSingleNode("//feature_tree");
            string featureModel = featureTree.InnerText;

            string eol = "\n";
            if (featureModel.IndexOf('\n') > 0 && featureModel.ElementAt(featureModel.IndexOf('\n') - 1) == '\r')
            {
                eol = "\r\n";
            }

            string[] features = featureModel.Split(new string[] { eol }, StringSplitOptions.RemoveEmptyEntries);
            parseFeaturesSXFM(features);


            XmlNode constraints = root.SelectSingleNode("//constraints");
            string[] booleanConstraints = constraints.InnerText.Split(new string[] { eol }, StringSplitOptions.RemoveEmptyEntries);
            parseConstraintsSXFM(booleanConstraints);
            this.binaryConstraints.Remove("");

            return true;
        }

        private void parseFeaturesSXFM(string[] features)
        {
            int previousDepth = 0;
            int groupCounter = 0;
            BinaryOption previousOption = null;
            Dictionary<int, List<BinaryOption>> optionsInGroups = new Dictionary<int, List<BinaryOption>>();
            Dictionary<int, string> cardinalitiesInGroups = new Dictionary<int, string>();

            foreach (string feature in features)
            {
                int currentDepth = getDepth(feature);
                string[] information = splitWhiteSpaces(feature);

                if (previousDepth - currentDepth == 1 && optionsInGroups.ContainsKey(currentDepth))
                {
                    resolveGroup(optionsInGroups[currentDepth], cardinalitiesInGroups[currentDepth]);
                    optionsInGroups.Remove(currentDepth);
                    cardinalitiesInGroups.Remove(currentDepth);
                }

                if (feature.Contains(":r"))
                {
                    this.root.Name = "root";
                    this.root.OutputString = information[1];
                    previousOption = this.root;
                }
                else if (feature.Contains(":m"))
                {
                    BinaryOption mandatory = new BinaryOption(this, information[2]);
                    mandatory.Optional = false;
                    mandatory.OutputString = information[1];
                    this.binaryOptions.Add(mandatory);

                    setParent(mandatory, previousOption, currentDepth, previousDepth);

                    previousOption = mandatory;
                }
                else if (feature.Contains(":o"))
                {
                    BinaryOption optional = new BinaryOption(this, information[2]);
                    optional.Optional = true;
                    optional.OutputString = information[1];
                    this.binaryOptions.Add(optional);

                    setParent(optional, previousOption, currentDepth, previousDepth);

                    previousOption = optional;
                }
                else if (feature.Contains(":g"))
                {
                    optionsInGroups[currentDepth] = new List<BinaryOption>();

                    int offset = 0;
                    if (information[1].Contains("["))
                    {
                        offset = 1;
                    }

                    BinaryOption groupParent;

                    if (information.Length > 2)
                    {
                        groupParent = new BinaryOption(this, information[2 + offset]);
                        groupParent.OutputString = information[1 + offset];
                    } else
                    {
                        groupParent = new BinaryOption(this, "Group_" + groupCounter);
                        groupParent.OutputString = "Group_" + groupCounter++;
                    }
                    groupParent.Optional = false;
                    this.binaryOptions.Add(groupParent);

                    string cardinality = null;

                    if (information[0].Contains("["))
                    {
                        cardinality = information[0].Split(new string[] { ":g" }, StringSplitOptions.None)[1];
                    } else if (offset == 1)
                    {
                        cardinality = information[offset];
                    } else
                    {
                        cardinality = information[3];
                    }
                    cardinalitiesInGroups[currentDepth] = cardinality;

                    setParent(groupParent, previousOption, currentDepth, previousDepth);

                    previousOption = groupParent;
                }
                else if (feature.Contains(":"))
                {
                    BinaryOption mandatory = new BinaryOption(this, information[2]);
                    mandatory.Optional = false;
                    mandatory.OutputString = information[1];
                    this.binaryOptions.Add(mandatory);

                    setParent(mandatory, previousOption, currentDepth, previousDepth);

                    optionsInGroups[currentDepth - 1].Add(mandatory);
                    previousOption = mandatory;
                }

                previousDepth = currentDepth;
            }
            
            foreach (int key in optionsInGroups.Keys)
            {
                resolveGroup(optionsInGroups[key], cardinalitiesInGroups[key]);
            }
        }

        private void resolveGroup(List<BinaryOption> group, string cardinality)
        {
            if (cardinality == "[1,1]")
            {
                createExclusiveGroup(group);
            }
            else if (cardinality == "[1,*]" | cardinality == ("[1," + group.Count + "]"))
            {
                group.ForEach(opt => opt.Optional = true);
                StringBuilder sb = new StringBuilder();
                sb.Append("!" + group.First().ParentName + " | ");
                group.ForEach(option => sb.Append(option.Name + " | "));

                // remove trailing "| "
                sb.Length--;
                sb.Length--;

                this.BinaryConstraints.Add(sb.ToString());
            }
            else if (cardinality == "[0,1]")
            {
                group.ForEach(option => option.Optional = true);
                createExclusiveGroup(group);
            } else if (cardinality == "[0,*]")
            {
                group.ForEach(option => option.Optional = true);
            }
            else if (cardinality.StartsWith("[0,"))
            {
                group.ForEach(option => option.Optional = true);
                this.MixedConstraints.Add(new MixedConstraint(resolveUpperBounds(group, cardinality),
                    this, "none"));
            }
            else
            {
                group.ForEach(option => option.Optional = true);
                this.MixedConstraints.Add(new MixedConstraint(resolveLowerBounds(group, cardinality),
                    this, "none"));
                this.MixedConstraints.Add(new MixedConstraint(resolveUpperBounds(group, cardinality),
                    this, "none"));
            }
        }

        private string resolveLowerBounds(List<BinaryOption> group, string cardinality)
        {
            StringBuilder expression = new StringBuilder();
            char boundaryChar = cardinality.Split(new string[] { "," }, StringSplitOptions.None)[0].ElementAt(1);
            int boundary = (int)char.GetNumericValue(boundaryChar);

            expression.Append(group.First().ParentName + " * (");
            expression.Append(boundary);
            expression.Append(" - ");

            group.ForEach(opt => expression.Append(opt.ToString() + " - "));
            expression.Length--;
            expression.Length--;
            expression.Append(")");

            expression.Append(" < 1");

            return expression.ToString();
        }

        private string resolveUpperBounds(List<BinaryOption> group, string cardinality)
        {
            StringBuilder expression = new StringBuilder();
            char boundaryChar = cardinality.Split(new string[] { "," }, StringSplitOptions.None)[1].ElementAt(0);
            int boundary;

            if (boundaryChar == '*')
            {
                boundary = group.Count;
            }
            else
            {
                boundary = (int)char.GetNumericValue(boundaryChar);
                if (boundary > group.Count)
                    boundary = group.Count;
            }

            group.ForEach(opt => expression.Append(opt.ToString() + " + "));
            expression.Length--;
            expression.Length--;

            expression.Append(" < ");
            expression.Append(boundary + 1);

            return expression.ToString().Trim();
        }

        private void createExclusiveGroup(List<BinaryOption> group)
        {
            foreach (BinaryOption binOpt in group)
            {
                foreach (BinaryOption otherOption in group)
                {
                    if (otherOption.Name != binOpt.Name)
                    {
                        List<ConfigurationOption> excluded = new List<ConfigurationOption>();
                        excluded.Add(otherOption);
                        binOpt.Excluded_Options.Add(excluded);
                    }
                }
            }
        }

        private void setParent(BinaryOption current, BinaryOption previous, int currentDepth, int prevDepth)
        {
            if (currentDepth - 1 == prevDepth)
            {
                current.ParentName = previous.Name;
                current.Parent = previous;
            }
            else
            {
                BinaryOption tempParent = previous;
                while (prevDepth > currentDepth - 1)
                {
                    tempParent = (BinaryOption)tempParent.Parent;
                    prevDepth--;
                }
                current.ParentName = tempParent.Name;
                current.Parent = tempParent;
            }
        }

        private string[] splitWhiteSpaces(string line)
        {
            return line.Trim().Split(new string[] { " ", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void parseConstraintsSXFM(string[] booleanConstraints)
        {
            foreach (string constraint in booleanConstraints)
            {
                if (constraint.Contains(":"))
                {
                    string cleanedConstraint = constraint.Split(new char[] { ':' })[1];
                    cleanedConstraint = cleanedConstraint.Replace("~", "!");
                    cleanedConstraint = cleanedConstraint.Replace(" OR ", " | ");
                    cleanedConstraint = cleanedConstraint.Replace(" or ", " | ");
                    cleanedConstraint = cleanedConstraint.Replace(" Or ", " | ");
                    this.binaryConstraints.Add(cleanedConstraint.Trim());
                }
            }
        }

        private int getDepth(string feature)
        {
            // ident used to determine the depth of the feature
            string[] cleanedFeature = feature.Split(new string[] { "\t" }, StringSplitOptions.None);
            return cleanedFeature.Length - 1;
        }

        /// <summary>
        /// Static method that reads an xml file and constructs a variability model using the stored information
        /// </summary>
        /// <param name="path">Path to the XML File</param>
        /// <returns>The instantiated variability model or null if there is no variability model at the path or if the model could not be parsed.</returns>
        public static VariabilityModel loadFromXML(String path)
        {
            VariabilityModel vm = new VariabilityModel("temp");
            if (vm.loadXML(path))
                return vm;
            else
                return null;
        }

        /// <summary>
        /// Loads an XML file containing information about the variability model.
        /// </summary>
        /// <param name="path">Path to the XML file</param>
        /// <returns>True if the variability model could be parsed, false otherwise.</returns>
        public Boolean loadXML(String path)
        {
            XmlDocument dat = new System.Xml.XmlDocument();
            if (!File.Exists(path))
                return false;
            dat.Load(path);
            XmlElement currentElemt = dat.DocumentElement;

            // Test if old definition is used and print a warning if needed
            var childrenNode = currentElemt.SelectSingleNode("//children");
            if (childrenNode != null)
            {
                GlobalState.logInfo.logLine("Warning: Variability model contains outdated notation. " +
                    "Children nodes will no longer have effect on the model. Instead use the parent node " +
                    "to describe the parent of an configuration option. You can also use the converter provided" +
                    "by the Variability Model GUI in export>convert legacy model.");
            }
            var defaultValueNode = currentElemt.SelectSingleNode("//defaultValue");
            if (defaultValueNode != null)
            {
                GlobalState.logInfo.logLine("Warning: Default value nodes in models are no longer used" +
                    " and should be removed. You can also use the converter provided by the Variability" +
                    " Model GUI in export>convert legacy model to update your model.");
            }


            this.name = currentElemt.Attributes["name"].Value.ToString();
            foreach (XmlElement xmlNode in currentElemt.ChildNodes.OfType<XmlElement>())
            {
                switch (xmlNode.Name)
                {
                    case "binaryOptions":
                        loadBinaryOptions(xmlNode);
                        break;
                    case "numericOptions":
                        loadNumericOptions(xmlNode);
                        break;
                    case "booleanConstraints":
                        loadBooleanConstraints(xmlNode);
                        break;
                    case "nonBooleanConstraints":
                        loadNonBooleanConstraint(xmlNode);
                        break;
                    case "mixedConstraints":
                        loadMixedConstraints(xmlNode);
                        break;
                }
            }

            initOptions();
            return true;
        }

        /// <summary>
        /// After loading all options, we can replace the names for children, the parent, etc. with the actual objects.
        /// </summary>
        private void initOptions()
        {
            foreach (var binOpt in binaryOptions)
                binOpt.init();
            foreach (var numOpt in numericOptions)
                numOpt.init();

            foreach (var opt in getOptions())
                opt.updateChildren(this);
        }

        private void loadBooleanConstraints(XmlElement xmlNode)
        {
            foreach (XmlElement boolConstr in xmlNode.ChildNodes)
            {
                this.binaryConstraints.Add(boolConstr.InnerText);
            }
        }

        #region to replicate VM with a given subset

        /// <summary>
        /// Produce a reduced version of the variability model, containing only binary options
        /// and at least the considered options. Is a considered option, all parent option will be inlcuded.
        /// Constraints between options(alternative groups, implication etc), will be included if enough options
        /// are present to (e.g. both options for implications or at least 2 options in alternative groups).
        /// </summary>
        /// <param name="consideredOptions">The options that will be in the reduced model.</param>
        /// <returns>A reduced version of the model containing the considered options.</returns>
        public VariabilityModel reduce(List<BinaryOption> consideredOptions)
        {
            VariabilityModel reduced = new VariabilityModel(this.name);
            foreach (BinaryOption binOpt in consideredOptions)
            {
                BinaryOption child = new BinaryOption(reduced, binOpt.Name);
                replicateOption(binOpt, child);
                reduced.addConfigurationOption(child);
                BinaryOption parent = (BinaryOption)binOpt.Parent;
                bool parentExists = false;
                while ((parent != null && parent != this.root) && !parentExists)
                {
                    BinaryOption newParent = reduced.getBinaryOption(parent.Name);
                    if (newParent == null)
                    {
                        newParent = new BinaryOption(reduced, parent.Name);
                        replicateOption(parent, newParent);
                        reduced.addConfigurationOption(newParent);
                    }
                    else
                        parentExists = true;
                    child.Parent = newParent;
                    child = newParent;
                    parent = (BinaryOption)parent.Parent;
                }

                if (child.Parent == null)
                    child.Parent = reduced.root;
            }

            foreach (BinaryOption opt in consideredOptions)
            {

                List<List<ConfigurationOption>> impliedOptionsRepl = new List<List<ConfigurationOption>>();
                foreach (List<ConfigurationOption> implied in opt.Implied_Options)
                {
                    List<ConfigurationOption> implRepl = new List<ConfigurationOption>();

                    foreach (ConfigurationOption impliedOption in implied)
                    {
                        if (reduced.getOption(impliedOption.Name) != null)
                            implRepl.Add(reduced.getOption(impliedOption.Name));
                    }
                    impliedOptionsRepl.Add(implRepl);

                }

                reduced.getBinaryOption(opt.Name).Implied_Options = impliedOptionsRepl;

                List<List<ConfigurationOption>> excludedOptionsRepl = new List<List<ConfigurationOption>>();
                foreach (List<ConfigurationOption> excluded in opt.Excluded_Options)
                {
                    List<ConfigurationOption> exclRepl = new List<ConfigurationOption>();

                    foreach (ConfigurationOption excludedOption in excluded)
                    {
                        if (reduced.getOption(excludedOption.Name) != null)
                            exclRepl.Add(reduced.getOption(excludedOption.Name));
                    }
                    excludedOptionsRepl.Add(exclRepl);
                }

                reduced.getBinaryOption(opt.Name).Excluded_Options = excludedOptionsRepl;
            }

            return reduced;
        }

        private void replicateOption(BinaryOption original, BinaryOption replication)
        {
            replication.Optional = original.Optional;
            replication.OutputString = original.OutputString;
            replication.Postfix = original.Postfix;
            replication.Prefix = original.Prefix;
        }

        #endregion

        private void loadNonBooleanConstraint(XmlElement xmlNode)
        {
            foreach (XmlElement nonBoolConstr in xmlNode.ChildNodes)
            {
                this.nonBooleanConstraints.Add(new NonBooleanConstraint(nonBoolConstr.InnerText, this));
            }
        }

        private void loadMixedConstraints(XmlElement xmlNode)
        {
            foreach (XmlElement mixedConstraint in xmlNode.ChildNodes)
            {
                if (mixedConstraint.Attributes.Count == 1)
                {
                    this.mixedConstraints.Add(new MixedConstraint(mixedConstraint.InnerText.Replace("&lt;", "<").Replace("&gt;", ">"), this, mixedConstraint.Attributes[0].Value));
                }
                else
                {
                    this.mixedConstraints.Add(new MixedConstraint(mixedConstraint.InnerText.Replace("&lt;", "<").Replace("&gt;", ">"), this, mixedConstraint.Attributes[0].Value, mixedConstraint.Attributes[1].Value));
                }
            }
        }

        private void loadNumericOptions(XmlElement xmlNode)
        {
            foreach (XmlElement numOptNode in xmlNode.ChildNodes)
            {
                if (!addConfigurationOption(NumericOption.loadFromXML(numOptNode, this)))
                    GlobalState.logError.logLine("Could not add option to the variability model. Possible reasons: invalid name, option already exists.");
            }
        }

        private void loadBinaryOptions(XmlElement xmlNode)
        {
            foreach (XmlElement binOptNode in xmlNode.ChildNodes)
            {
                if (!addConfigurationOption(BinaryOption.loadFromXML(binOptNode, this)))
                    GlobalState.logError.logLine("Could not add option to the variability model. Possible reasons: invalid name, option already exists.");
            }
        }

        /// <summary>
        /// Adds a configuration option to the variability model.
        /// The method checks whether an option with the same name already exists and whether invalid characters are within the name
        /// </summary>
        /// <param name="option">The option to be added to the variability model.</param>
        /// <returns>True if the option was added to the model, false otherwise</returns>
        public bool addConfigurationOption(ConfigurationOption option)
        {
            if (option.Name.Contains('-') || option.Name.Contains('+'))
                return false;

            // the vitrual root configuration option does not have to be added to the variability model. 
            if (option.Name.Equals("root"))
                return true;

            foreach (var opt in binaryOptions)
            {
                if (opt.Name.Equals(option.Name))
                    return false;
            }
            foreach (var opt in numericOptions)
            {
                if (opt.Name.Equals(option.Name))
                    return false;
            }

            if (this.hasOption(option.ParentName))
                option.Parent = this.getOption(option.ParentName);


            //Every option must have a parent
            if (option.Parent == null)
                option.Parent = this.root;

            if (option is BinaryOption)
                this.binaryOptions.Add((BinaryOption)option);
            else
                this.numericOptions.Add((NumericOption)option);

            // create Index 
            optionToIndex.Add(optionToIndex.Count, option);
            indexToOption.Add(option, indexToOption.Count);


            return true;
        }

        /// <summary>
        /// Searches for a binary option with the given name.
        /// </summary>
        /// <param name="name">Name of the option</param>
        /// <returns>Either the binary option with the given name or NULL if not found</returns>
        public BinaryOption getBinaryOption(String name)
        {

            name = ConfigurationOption.removeInvalidCharsFromName(name);
            foreach (var binO in binaryOptions)
            {
                if (binO.Name.Equals(name))
                    return binO;
            }
            return null;
        }

        /// <summary>
        /// Searches for a numeric option with the given name.
        /// </summary>
        /// <param name="name">Name of the option</param>
        /// <returns>Either the numeric option with the given name or NULL if not found</returns>
        public NumericOption getNumericOption(String name)
        {
            name = ConfigurationOption.removeInvalidCharsFromName(name);
            foreach (var numO in numericOptions)
            {
                if (numO.Name.Equals(name))
                    return numO;
            }
            return null;
        }

        /// <summary>
        /// This method retuns the configuration with the given name.
        /// </summary>
        /// <param name="name">Name of the option under consideration.</param>
        /// <returns>The option with the given name or NULL of no option with the name is defined.</returns>
        public ConfigurationOption getOption(String name)
        {
            name = ConfigurationOption.removeInvalidCharsFromName(name);
            ConfigurationOption opt = getNumericOption(name);
            if (opt != null)
                return opt;
            opt = getBinaryOption(name);

            return opt;
        }

        /// <summary>
        /// Returns a list containing all configuration options of the variability model. 
        /// </summary>
        /// <returns>List of all options of the variability model.</returns>
        public List<ConfigurationOption> getOptions()
        {
            List<ConfigurationOption> options = new List<ConfigurationOption>();
            options.AddRange(binaryOptions);
            options.AddRange(numericOptions);
            return options;
        }

        /// <summary>
        /// Tests whether a configuration has only options that are in the Variability model.
        /// </summary>
        /// <param name="conf">The configuration to test.</param>
        /// <returns>True when all configuration options are valid in this model.</returns>
        public bool isInModel(Configuration conf)
        {
            if (conf.BinaryOptions.Keys.ToList().Except(this.BinaryOptions).Count() != 0)
                return false;

            double value;

            if (!conf.NumericOptions.Keys.ToList()
                .TrueForAll(numOpt => this.NumericOptions.Contains(numOpt)
                && conf.NumericOptions.TryGetValue(numOpt, out value)
                && this.NumericOptions.Where(opt => opt.ToString().Equals(numOpt.ToString())).First().getAllValues().Contains(value)))
                return false;

            if (!configurationIsValid(conf))
                return false;

            return true;
        }

        /// <summary>
        /// Tests whether a configuration is valid with respect to all non-boolean constraints.
        /// </summary>
        /// <param name="c">The configuration to test.</param>
        /// <returns>True if the configuration is valid.</returns>
        public bool configurationIsValid(Configuration c)
        {
            foreach (NonBooleanConstraint nonBC in this.nonBooleanConstraints)
            {
                if (!nonBC.configIsValid(c))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Removes a configuration from the variability model.
        /// </summary>
        /// <param name="toDelete"></param>
        public void deleteOption(ConfigurationOption toDelete)
        {
            // Removing all children
            List<ConfigurationOption> children = new List<ConfigurationOption>();

            foreach (ConfigurationOption opt in this.BinaryOptions)
                if (opt.Parent != null && opt.Parent.Equals(toDelete))
                {
                    children.Add(opt);
                }

            foreach (ConfigurationOption child in children)
                deleteOption(child);

            // Removing option from other options
            foreach (ConfigurationOption opt in getOptions())
            {
                for (int i = opt.Excluded_Options.Count - 1; i >= 0; i--)
                {
                    if (opt.Excluded_Options[i].Contains(toDelete))
                        opt.Excluded_Options.RemoveAt(i);
                }

                for (int i = opt.Implied_Options.Count - 1; i >= 0; i--)
                {
                    if (opt.Implied_Options[i].Contains(toDelete))
                        opt.Implied_Options.RemoveAt(i);
                }
            }

            // Removing option from constraints
            binaryConstraints.RemoveAll(x => x.Contains(toDelete.ToString()));
            nonBooleanConstraints.RemoveAll(x => x.ToString().Contains(toDelete.ToString()));

            if (toDelete is BinaryOption)
                binaryOptions.Remove((BinaryOption)toDelete);
            else if (toDelete is NumericOption)
                numericOptions.Remove((NumericOption)toDelete);
            else
                throw new Exception("An illegal option was found while deleting.");
        }

        internal bool hasOption(string name)
        {
            return (this.getBinaryOption(name) != null) || (this.getNumericOption(name) != null);
        }
    }
}
