using System;
using System.Collections.Generic;
using System.Numerics;

namespace SPLConqueror_Core
{
    public enum Operation 
    {
         ADD,
         MULT
    }
    public enum SymbolKind 
    {
         TERMINAL,
         NONTERMINAL
    }
    public class Grammar
    {

        private readonly FMTree VMTree;

        /* Dictonary: key: NT name
                      value: Tuple Item1, Item2, Item3
                            Item1: List with key, the base, if it is ignored in the config
                            Item2: If the rule is for Terminals
                            Item3: If bases are to multiply or to add
        */
        private Dictionary<string, Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>> grammar = new Dictionary<string, Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>>();

        private string root = "";

        public string Root { get => root; }

        public Grammar(FMTree tree)
        {
            VMTree = tree;
            generateGrammar();

        }

        public Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation> GetRule(string key)
        {
            return grammar[key];
        }

        private void generateGrammar()
        {
            FMNode root = VMTree.Root;
            if (root.Children.Count > 0)
            {
                generateGrammar(root);
                this.root = root.Name;
            }
            else
            {   
                throw new InvalidOperationException();
            }

        }

        private void generateGrammar(FMNode node)
        {
            List<Tuple<string, BigInteger, Boolean>> rules = new List<Tuple<string, BigInteger, Boolean>>();
            Operation op = Operation.MULT;
            SymbolKind symbolKind = SymbolKind.NONTERMINAL;
            foreach (FMNode child in node.Children)
            {
                if (child.Children.Count > 0)
                {
                    if (node.Children.TrueForAll(x => x.ExcludedOptions.Count == 0))
                    {
                        rules.Add(new Tuple<string, BigInteger, Boolean>(child.Name, -1, false));
                    }
                    else 
                    { 
                        op = Operation.ADD;
                        rules.Add(new Tuple<string, BigInteger, Boolean>(child.Name, -1, false));
                    }
                } 
                else
                { 
                    if (child.Optional)
                    {
                        rules.Add(new Tuple<string, BigInteger, Boolean>(child.Name, -1, true));
                        List<Tuple<string, BigInteger, Boolean>> tmpList = new List<Tuple<string, BigInteger, Boolean>>();
                        tmpList.Add(new Tuple<string, BigInteger, Boolean>("\u03B5", 1, true));
                        tmpList.Add(new Tuple<string, BigInteger, Boolean> (child.Name, 1, false));
                        grammar.Add(child.Name, new Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>(tmpList, SymbolKind.TERMINAL, Operation.ADD));
                    }
                    else if (!node.Children.TrueForAll(x => x.Children.Count == 0))
                    {
                        rules.Add(new Tuple<string, BigInteger, Boolean>(child.Name, -1, true));
                        if (child.ExcludedOptions.Count > 0) 
                        { 
                            op = Operation.ADD;
                        }
                        List<Tuple<string, BigInteger, Boolean>> tmpList = new List<Tuple<string, BigInteger, Boolean>>();
                        tmpList.Add(new Tuple<string, BigInteger, Boolean> (child.Name, 1, false));
                        grammar.Add(child.Name, new Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>(tmpList, SymbolKind.TERMINAL, Operation.ADD));
                    }
                    else
                    {
                        if (child.ExcludedOptions.Count > 0)
                        {
                            op = Operation.ADD;
                        }
                        if (node.Children.TrueForAll(x => x.Children.Count == 0) && node.Children.TrueForAll(x => !x.Optional))
                        {
                            symbolKind = SymbolKind.TERMINAL;
                            rules.Add(new Tuple<string, BigInteger, Boolean>(child.Name, 1, false));
                        }
                        else
                        {

                            rules.Add(new Tuple<string, BigInteger, Boolean>(child.Name, -1, true));
                            List<Tuple<string, BigInteger, Boolean>> tmpList = new List<Tuple<string, BigInteger, Boolean>>();
                            tmpList.Add(new Tuple<string, BigInteger, Boolean>(child.Name, 1, false));
                            grammar.Add(child.Name, new Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>(tmpList, SymbolKind.TERMINAL, op));
                        }
                    }
                }
            }
            if (node.Optional)
            {
                string nodeName = node.Name + "Invis"; 
                rules.Add(new Tuple<string, BigInteger, Boolean>(nodeName, -1, true));
                List<Tuple<string, BigInteger, Boolean>> tmpList = new List<Tuple<string, BigInteger, Boolean>>();
                tmpList.Add(new Tuple<string, BigInteger, Boolean>("\u03b5", 1, true));
                grammar.Add(nodeName, new Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>(tmpList, SymbolKind.TERMINAL, Operation.ADD));

            }
            if (!grammar.ContainsKey(node.Name)) { 
                grammar.Add(node.Name, new Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation> (rules, symbolKind, op));
            }
            foreach (FMNode child in node.Children)
            {
                if (child.Children.Count > 0) generateGrammar(child);
            }

        }

        /*private bool checkOptional(FMNode node)
        {
            if (node.Optional) return true;
            else if (node.Parent == null) return false;
            else return checkOptional(node.Parent);
        }*/

        public override string ToString ()
        {
            string result = "";
            foreach (KeyValuePair<string, Tuple<List<Tuple<string, BigInteger, Boolean>>, SymbolKind, Operation>> obj in grammar)
            {
                result += obj.Key + " ::= ";
                foreach (Tuple<string, BigInteger, Boolean> value in obj.Value.Item1)
                {
                    string delim = "";
                    if (obj.Value.Item3 == Operation.ADD) delim = " | ";
                    else delim = "   ";
                    result += "(" + value.Item1 + ", " + value.Item2 + ")" + delim;
                }
                result = result.Substring(0, result.Length - 3);
                result += " Symbolkind: " + obj.Value.Item2 + "\n";
            }
            return result;
        }

        public void print()
        {
            Console.WriteLine(this.ToString());
        }

    }
}
