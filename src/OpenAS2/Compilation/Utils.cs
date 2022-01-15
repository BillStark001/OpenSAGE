﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAS2.Base;
using OpenAS2.Runtime;
using OpenAS2.Compilation.Syntax;
using Value = OpenAS2.Runtime.Value;
using ValueType = OpenAS2.Runtime.ValueType;

namespace OpenAS2.Compilation
{
    using RawInstructionStorage = SortedList<int, RawInstruction>;

    public static class InstructionUtils
    {
        public static IList<Value> CreateConstantPool(IList<RawValue> parameters, IList<ConstantEntry> globalPool)
        {
            var pool = new List<Value>();

            // The first parameter is the constant count, omit it
            for (var i = 1; i < parameters.Count; ++i)
            {
                Value result;
                var entry = globalPool[parameters[i].Integer];
                switch (entry.Type)
                {
                    case ConstantType.String:
                        result = Value.FromString((string) entry.Value);
                        break;
                        // to solve it, consider expanding wrapped value in EC to wider usages
                        /*
                    case ConstantType.Register:
                        result = Value.FromRegister((uint) entry.Value);
                        break;
                        */
                    default:
                        throw new NotImplementedException();
                }
                pool.Add(result);
            }

            return pool.AsReadOnly();
        }
        /*
        public static Func<Value?, Value?> ParseValueWrapped(StatementCollection s)
        {
            return x => ParseValue(x, s.Constants.ElementAt, x => { var a = s.HasRegisterValue(x, out var b); return (a, b); });
        }

        public static Value? ParseValue(Value? v, Func<int, Value?> consts, Func<int, (bool, Value?)>? regs)
        {
            if (v == null)
                return v;
            while (v != null && (v.Type == ValueType.Constant || v.Type == ValueType.Register))
            {
                if (v.Type == ValueType.Constant)
                {
                    v = consts(v.ToInteger());
                    if (!Value.IsNull(v) && v.Type == ValueType.Constant)
                        throw new InvalidOperationException();
                }
                else
                {
                    if (regs != null)
                    {
                        var (hasval, val) = regs(v.ToInteger());
                        if (hasval)
                            v = val;
                        else
                            break;
                    }
                    else
                        break;
                }
            }
            return v;
        }
        */
        public static (string, List<string>) GetNameAndArguments(RawInstruction function)
        {
            string name = function.Parameters[0].String;
            List<string> args = new();
            int nrArgs = function.Parameters[1].Integer;
            for (int i = 0; i < nrArgs; ++i)
            {
                if (function.Type == InstructionType.DefineFunction2)
                    args.Add(function.Parameters[4 + i * 2 + 1].String);
                else
                    args.Add(function.Parameters[2 + i].String);
            }
            return (name, args);
        }
        public static string? ToStringWithIndent(this object obj, int indent = 0)
        {
            string ots = obj.ToString()!;
            if (ots == null)
                ots = "null";
            if (indent <= 0)
                return ots;
            var s = new StringBuilder();
            for (int i = 0; i < indent; ++i)
                s.Append(" ");
            s.Append(ots);
            return s.ToString();
        }

        public static string FormLabels(IEnumerable<string> l)
        {
            return string.Join(", ", l.ToArray());
        }

        public static string ReverseCondition(string s)
        {
            if (s == "True")
                return "False";
            else if (s == "False")
                return "True";
            else if (s == "true")
                return "false";
            else if (s == "false")
                return "true";
            if (s.StartsWith("!"))
            {
                s = s.Substring(1);
                if (s.StartsWith('(') && s.EndsWith(')'))
                    s = s.Substring(1, s.Length - 2);
            }
            else
            {
                s = $"!({s})";
            }
            return s;
        }

        public static (string, InstructionType) SimplifyCondition(string s, InstructionType c)
        {
            if (c != InstructionType.BranchIfTrue && c != InstructionType.EA_BranchIfFalse)
                throw new InvalidOperationException();
            while (s.StartsWith("!"))
            {
                s = s.Substring(1);
                c = c == InstructionType.BranchIfTrue ? InstructionType.EA_BranchIfFalse : InstructionType.BranchIfTrue;
                if (s.StartsWith('(') && s.EndsWith(')'))
                    s = s.Substring(1, s.Length - 2);
            }
            return (s, c);
        }

        public static string GetIncrementedName(string s, bool startFromZero = false)
        {
            var u = s.LastIndexOf('_');
            var add = u < 1 || u == s.Length - 1;
            int curNr = startFromZero ? -1 : 0;
            if (!add)
                add = !int.TryParse(s.Substring(u + 1), out curNr);
            return (add ? s + "_" : s.Substring(0, u + 1)) + (curNr + 1).ToString();
        }

        public static string JustifyName(
            string s,
            int maxLength = 20,
            string illegal = " ,./\\()[]<>`~+-*^=&|%$#@!'\":\t",
            string split = "{};\n",
            bool striptUnderscore = true
            )
        {
            foreach (var c in split)
                s = s.Split(c)[0];
            foreach (var c in illegal)
                s = s.Replace(c, '_');
            for (int i = 0; i < 16; ++i)
            {
                var s1 = s.Replace("__", "_");
                if (s1.Length == s.Length)
                    break;
                else
                    s = s1;
            }
            if (striptUnderscore)
            {
                if (s.StartsWith('_'))
                    s = s.Substring(1);
                if (s.EndsWith('_'))
                    s = s.Substring(0, s.Length - 1);
            }

            s = s.Replace("function", "func").Replace("prototype", "proto").Replace("system", "sys").Replace("object", "obj").Replace("variable", "var").Replace("initialize", "init");
            s = s.Replace("Function", "Func").Replace("Prototype", "Proto").Replace("System", "Sys").Replace("Object", "Obj").Replace("Variable", "Var").Replace("Initialize", "Init");

            if (int.TryParse(s.Substring(0, 1), out var _))
                s = "num_" + s;
            if (s.Length > maxLength)
                s = s.Substring(0, maxLength);

            if (s == "True" || s == "true" || s == "False" || s == "false")
                return "boolval";
            else if (s == "null")
                return "nullval";
            else if (s == "undefined")
                return "undefval";

            return s;
        }

        public static string ToCodingForm(this string str)
        {
            return $"\"{str.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\"", "\\\"")}\"";
        }




        public static InstructionGraph? Graphify(
            RawInstructionStorage? ci, 
            IList<ConstantEntry>? constSource = null,
            Dictionary<int, string>? regNames = null
            )
        {
            if (ci == null)
                return null;

            var g = new InstructionGraph(ci, 0, null, regNames);

            if (constSource == null) return g;

            g = InstructionGraph.OptimizeGraph(g);

            // wtf
            
            Console.WriteLine("\nGan Si Huang Xu Dong");

            var c = StructurizedBlockChain.Parse(g.BaseBlock);
            // TODO constpool & constsource

            // try form const pool
            var p = SyntaxNodePool.ConvertToAST(c, constSource, g.RegNames);

            // var c = BlockChainifyUtils.Parse(g.BaseBlock);
            // var p = new NodePool(g.ConstPool, g.RegNames);
            // p.PushBlock(c);

            var sc = new StatementCollection(p);
            var code = sc.Compile();
            Console.Write(code.ToString());

            return g;
        }
    }

}