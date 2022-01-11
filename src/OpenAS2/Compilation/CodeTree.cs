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

    public class LogicalValue : Value
    {
        public readonly SyntaxNode N;
        public LogicalValue(SyntaxNode n) : base(ValueType.Undefined)
        {
            N = n;
        }

        public override string ToString()
        {
            return string.Empty;
        }
    }

    public class StatementCollection
    {
        public IEnumerable<SyntaxNode> Nodes { get; private set; }
        public IEnumerable<Value> Constants { get; }
        public Dictionary<int, string> RegNames { get; }
        public Dictionary<int, Value> Registers { get; }
        public Dictionary<string, Value?> NodeNames { get; }
        public Dictionary<Value, string> NodeNames2 { get; }

        private StatementCollection? _parent;

        public StatementCollection(NodePool pool) {
            Nodes = pool.PopNodes();
            Constants = pool.Constants;
            RegNames = new Dictionary<int, string>(pool.RegNames);
            Registers = new();
            NodeNames = new();
            NodeNames2 = new();
            foreach (var (reg, rname) in RegNames)
                NodeNames[rname] = null; // don't care overwriting
            _parent = null;
        }

        // nomination

        public bool HasValueName(Value val, out string? name, out bool canOverwrite)
        {
            var ans = NodeNames2.TryGetValue(val, out name);
            canOverwrite = ans;
            if (!ans && _parent != null)
                ans = _parent.HasValueName(val, out name, out var _);
            return ans;
        }

        public bool HasRegisterName(int id, out string? name, out bool canOverwrite)
        {
            var ans = RegNames.TryGetValue(id, out name);
            canOverwrite = ans;
            if (!ans && _parent != null)
                ans = _parent.HasRegisterName(id, out name, out var _);
            return ans;
        }

        public bool HasRegisterValue(int id, out Value? val)
        {
            var ans = Registers.TryGetValue(id, out val);
            if (!ans && _parent != null)
                ans = _parent.HasRegisterValue(id, out val);
            return ans;
        }

        public string NameRegister(int id, string? hint, bool forceOverwrite = false)
        {
            if (string.IsNullOrWhiteSpace(hint))
                hint = $"reg{id}";
            while ((!forceOverwrite) && NodeNames.ContainsKey(hint))
                hint = InstructionUtils.GetIncrementedName(hint);
            RegNames[id] = hint;
            if (!NodeNames.ContainsKey(hint))
                NodeNames[hint] = null;
            return hint;
        }

        public string NameVariable(Value val, string name, bool forceOverwrite = false)
        {
            while ((!forceOverwrite) && NodeNames.ContainsKey(name))
                name = InstructionUtils.GetIncrementedName(name);
            NodeNames[name] = val;
            NodeNames2[val] = name;
            return name;
        }

        // TODO fancier implementation of name-related things

        // code optimization

        public bool IsEmpty() { return Nodes.Count() == 0; } // TODO optimization may be needed

        // compilation
        public StringBuilder Compile(StringBuilder? sb = null,
            int startIndent = 0,
            int dIndent = 4,
            bool compileSubCollections = true,
            bool ignoreLastBranch = false,
            StatementCollection? parent = null)
        {
            sb = sb == null ? new() : sb;
            var curIndent = startIndent;

            var p = _parent;
            if (parent != null)
                _parent = parent;

            foreach (var node in Nodes)
            {
                if (node == null)
                    sb.Append("// null node\n".ToStringWithIndent(curIndent));
                else
                {
                    // TODO CSC
                    if (node is NodeControl nc && compileSubCollections)
                    {
                        nc.TryCompile2(this, sb, curIndent, dIndent, compileSubCollections);
                        continue;
                    }
                    node.TryCompile(this, !ignoreLastBranch || node != Nodes.Last());
                    if (node.Code == null)
                        sb.Append("// no compiling result\n".ToStringWithIndent(curIndent));
                    else
                    {
                        var code = node.Code.AddLabels(node.Labels);
                        if (node is SNExpression)
                        { 
                            if (node.Instruction.Type == InstructionType.PushData || node.Instruction.Type.ToString().StartsWith("EA_Push"))
                                code = $"// __push__({code})@";
                        }
                        if (string.IsNullOrWhiteSpace(code))
                            continue;
                        code = code.Replace("@; //", ", ");
                        if (code.EndsWith("@"))
                            sb.Append(code.Substring(0, code.Length - 1).ToStringWithIndent(curIndent));
                        else
                        {
                            sb.Append(code.ToStringWithIndent(curIndent));
                            sb.Append(";");
                        }
                        sb.Append("\n");
                    }
                }
            }

            _parent = p;
            return sb;
        }

        public string GetExpression(Value v)
        {
            var ret = string.Empty;
            if (v.Type == ValueType.Constant)
                if (Constants != null && v.ToInteger() >= 0 && v.ToInteger() < Constants.Count())
                {
                    v = Constants.ElementAt(v.ToInteger());
                }
                else
                {
                    ret = $"__const__[{v.ToInteger()}]";
                }

            if (v.Type == ValueType.Register)
                if (HasRegisterName(v.ToInteger(), out var reg, out var _))
                {
                    ret = reg;
                }
                else
                {
                    ret = $"__reg__[{v.ToInteger()}]";
                }

            if (string.IsNullOrEmpty(ret))
            {
                if (NodeNames2.TryGetValue(v, out var ret2))
                    ret = ret2;
                else
                    ret = v.ToString();
            }
                
            return ret;
        }

    }

    // AST
    public class NodePool
    {
        /*
         * This class is used to change StructurizedBlockChain's to some structure
         * that has the feature of AST. 
         * In principle, no calculation should be involved, but enumerate- and array-related 
         * actions are exceptions.
         * The following assumptions are used:
         * - ConstantPool is only executed at most once in one code piece.
         *   Consequences of violation is unclear yet. 
         * - Enumerate is only used in for...in statements.
         *   Consequences of violation is unclear yet. 
         * 
         */

        // change through process
        public List<SyntaxNode> NodeList { get; }
        private Dictionary<SyntaxNode, int> _special = new();
        public int ParentNodeDivision { get; private set; }

        // should not be modified
        public IEnumerable<Value> Constants { get; }

        // should not be modified
        public Dictionary<int, string> RegNames { get; }

        public override string ToString()
        {
            return $"NodePool({NodeList.Count} Nodes, {Constants.Count()} Constants, {RegNames.Count} Named Registers)";
        }


        // brand new pool
        public NodePool(IEnumerable<Value>? consts = null, IDictionary<int, string>? regNames = null)
        {
            NodeList = new();
            Constants = consts == null ? Enumerable.Empty<Value>() : new List<Value>(consts);
            if (regNames != null)
                RegNames = new(regNames);
            else
                RegNames = new();
        }

        // subpool of function
        public NodePool(LogicalFunctionContext defFunc, NodePool parent, IEnumerable<Value>? consts = null)
        {
            NodeList = new();
            if (parent != null)
            {
                Constants = parent.Constants;
                RegNames = defFunc.Instructions.RegNames == null ? new() : new(defFunc.Instructions.RegNames!);
            }
            else
            {
                Constants = consts == null ? Enumerable.Empty<Value>() : new List<Value>(consts);
                RegNames = new();
            }
        }

        // subpool of control
        public NodePool(NodePool parent, IEnumerable<Value>? consts = null)
        {
            if (parent != null)
            {
                NodeList = new(parent.NodeList.Where(x => x is SNExpression));
                ParentNodeDivision = NodeList.Count;
                Constants = parent.Constants;
                RegNames = new();
            }
            else
            {
                NodeList = new();
                Constants = consts == null ? Enumerable.Empty<Value>() : new List<Value>(consts);
                RegNames = new();
            }
        }

        public SNExpression? PopExpression(bool deleteIfPossible = true)
        {
            var ind = NodeList.FindLastIndex(n => n is SNExpression);
            SNExpression? ret = null;
            if (ind == -1)
                ret = null;
            else
            {
                var node = (SNExpression) NodeList[ind];

                // some special nodes shouldn't be deleted like Enumerate
                var ableToDelete = true; 
                if (node.Instruction.Type == InstructionType.Enumerate2 ||
                    node.Instruction.Type == InstructionType.Enumerate)
                    ableToDelete = false;

                if (ableToDelete && deleteIfPossible)
                {
                    NodeList.RemoveAt(ind);
                    if (ind < ParentNodeDivision)
                        ParentNodeDivision = ind;
                        // --ParentNodeDivision;
                }
                ret = node;
            }
            return ret;
        }
        public SNArray PopArray(bool readPair = false, bool ensureCount = true)
        {
            SNArray ans = new();
            var nexp = PopExpression();
            Value? countVal = null;
            // another trygetvalue()

            var flag = nexp == null ? false : nexp.TryGetValue(x => InstructionUtils.ParseValue(x, Constants.ElementAt, null), out countVal);
            if (flag)
            {
                var count = countVal!.ToInteger();
                if (readPair) count *= 2;
                for (int i = 0; i < count; ++i)
                {
                    var exp = PopExpression();
                    if (exp != null || (exp == null && ensureCount))
                        ans.Expressions.Add(exp);
                }
            }
            return ans;
        }
        public IEnumerable<SNStatement> PopStatements()
        {
            return NodeList.Skip(ParentNodeDivision).Where(x => x is SNStatement && !_special.ContainsKey(x)).Cast<SNStatement>();
        }
        public IEnumerable<SyntaxNode> PopNodes()
        {
            return NodeList.Skip(ParentNodeDivision).Where(x => !_special.ContainsKey(x));
        }

        public void PushInstruction(InstructionBase inst)
        {
            SyntaxNode n;
            if (inst is LogicalFunctionContext fc)
            {
                var subpool = new NodePool(fc, this);
                subpool.PushChain(fc.Chain!);
                StatementCollection sc = new(subpool);
                n = fc.IsStatement ? new NodeDefineFunction(fc, sc) : new NodeFunctionBody(fc, sc);
            }
            else if (inst.Type == InstructionType.Enumerate || inst.Type == InstructionType.Enumerate2)
            {
                var obj = PopExpression();
                // a null object and the enumerated objects are pushed
                // but due to the mechanism of this class, only the latter
                // is needed
                n = new SNEnumerate(inst);
                n.Expressions.Add(obj);
            }
            else if (inst.Type == InstructionType.InitArray)
            {
                var arr = PopArray(false, true);
                n = arr;
                if (n.Expressions.Any(x => x is NodeFunctionBody))
                    n = new NodeIncludeFunction(n);
            }
            else
            {
                n = inst.IsStatement ? new SNStatement(inst) : new SNExpression(inst);
                n.GetExpressions(this);
                // find if there are any functions, if there are functions, wrap n
                if (n.Expressions.Any(x => x is NodeFunctionBody))
                    n = new NodeIncludeFunction(n);
            }
            // maintain labels
            foreach (var ne in n.Expressions)
                if (ne != null)
                    n.Labels.AddRange(ne.Labels);
            if (inst is LogicalTaggedInstruction ltag)
                n.Labels.AddRange(ltag.GetLabels());
            NodeList.Add(n);
        }

        public void PushChainRaw(StructurizedBlockChain chain, bool ignoreBranch)
        {
            if (chain.Empty)
                return;
            var c = chain;
            var currentBlock = c.StartBlock;
            while (currentBlock != null && (c.EndBlock == null || currentBlock.Hierarchy <= c.EndBlock!.Hierarchy))
            {
                if (currentBlock.Labels.Count > 0 && currentBlock.Items.Count == 0)
                    // if this NIE is really triggered, consider adding a NodeTag: NodeStatement
                    // yielding Code = "" while receiving no input
                    throw new NotImplementedException();
                foreach (var (pos, inst) in currentBlock.Items)
                    PushInstruction(inst);
                // a temporary solution to the BranchAlways codes.
                if (currentBlock.HasConstantBranch &&
                    currentBlock.BranchCondition!.Parameters[0].ToInteger() >= 0 &&
                    (currentBlock.NextBlockDefault == null || currentBlock.NextBlockCondition!.Hierarchy > currentBlock.NextBlockDefault.Hierarchy))
                    currentBlock = currentBlock.NextBlockCondition;
                else
                    currentBlock = currentBlock.NextBlockDefault;
            }
        }

        // TODO clear judgement conditions
        public void PushChain(
            StructurizedBlockChain chain
            )
        {
            // TODO clear judgement conditions
            if (chain.Type == CodeType.Case)
            {
                PushChain(chain.AdditionalData[0]);
                var branch = chain.AdditionalData[0].EndBlock.BranchCondition;
                var bexp = NodeList.Last();
                if (bexp != null)
                {
                    NodeList.RemoveAt(NodeList.Count - 1);
                    bexp = bexp.Expressions[0];
                }

                // create node expression
                NodePool sub1 = new(this);
                NodePool sub2 = new(this);
                sub1.PushChain(chain.AdditionalData[1]);
                sub2.PushChain(chain.AdditionalData[2]);
                NodeCase n = new(branch!, bexp as SNExpression, new(sub1), new(sub2));
                NodeList.Add(n);
                // add expressions inside the loop
                // TODO more judgements
                foreach (var ns2 in sub2.PopNodes())
                {
                    if (ns2 is SNExpression)
                    {
                        _special[ns2] = 1;
                        NodeList.Add(ns2);
                    }
                }
            }
            else if (chain.Type == CodeType.Loop)
            {
                PushChain(chain.AdditionalData[0]);
                var branch = chain.AdditionalData[0].EndBlock.BranchCondition;
                var bexp = NodeList.Last();
                if (bexp != null)
                {
                    NodeList.RemoveAt(NodeList.Count - 1);
                    bexp = bexp.Expressions[0];
                }
                // create node expression
                // this one needs more than condition!!!
                NodePool sub1 = new(this);
                NodePool sub2 = new(this);
                sub1.PushChain(chain.AdditionalData[0]);
                sub2.PushChain(chain.AdditionalData[1]);
                NodeLoop n = new(branch!, bexp as SNExpression, new(sub1), new(sub2));
                NodeList.Add(n);
                // add expressions inside the loop?
                foreach (var ns2 in sub2.PopNodes())
                {
                    if (ns2 is SNExpression)
                    {
                        _special[ns2] = 1;
                        NodeList.Add(ns2);
                    }
                }
            }
            else if (chain.SubChainStart != null)
            {
                var c = chain.SubChainStart;
                while (c != null)
                {
                    PushChain(c); // bug
                    c = c.Next;
                }
            }
            else
                PushChainRaw(chain, false);
        }

        public static NodePool ConvertToAST(StructurizedBlockChain chain, IEnumerable<Value>? constants, IDictionary<int, string>? regNames)
        {
            NodePool pool = new(constants, regNames);
            pool.PushChain(chain);
            return pool;
        }

    }

    // Nodes

    public abstract class SyntaxNode
    {
        public readonly List<SNExpression?> Expressions;
        public readonly List<string> Labels;
        public readonly InstructionBase Instruction;

        public string? Code { get; protected set; }

        protected SyntaxNode(InstructionBase inst)
        {
            Instruction = inst;
            Expressions = new();
            Labels = new();
        }

        public abstract string TryComposeRaw();

        public void GetExpressions(NodePool pool)
        {
            Expressions.Clear();
            var instruction = Instruction;
            if (Instruction is LogicalTaggedInstruction itag)
                instruction = itag.MostInner;
            // special process and overriding regular process
            var flagSpecialProc = true;
            switch (instruction.Type)
            {
                // type 1: peek but no pop
                case InstructionType.SetRegister:
                    Expressions.Add(pool.PopExpression(false));
                    break;
                case InstructionType.PushDuplicate:
                    Expressions.Add(pool.PopExpression(false));
                    break;

                // type 2: need to read args
                case InstructionType.InitArray:
                    Expressions.Add(pool.PopArray());
                    break;
                case InstructionType.ImplementsOp:
                case InstructionType.CallFunction:
                case InstructionType.EA_CallFuncPop:
                case InstructionType.NewObject:
                    Expressions.Add(pool.PopExpression());
                    Expressions.Add(pool.PopArray());
                    break;
                case InstructionType.CallMethod:
                case InstructionType.EA_CallMethod:
                case InstructionType.EA_CallMethodPop:
                case InstructionType.NewMethod:
                    Expressions.Add(pool.PopExpression());
                    Expressions.Add(pool.PopExpression());
                    Expressions.Add(pool.PopArray());
                    break;
                case InstructionType.EA_CallNamedFuncPop:
                case InstructionType.EA_CallNamedFunc:
                    Expressions.Add(new SNLiteral(instruction));
                    Expressions.Add(pool.PopArray());
                    break;
                case InstructionType.EA_CallNamedMethodPop:
                    Expressions.Add(new SNLiteral(instruction));
                    Expressions.Add(pool.PopExpression());
                    Expressions.Add(pool.PopArray());
                    break;
                case InstructionType.EA_CallNamedMethod:
                    Expressions.Add(new SNLiteral(instruction)); 
                    Expressions.Add(pool.PopExpression());
                    Expressions.Add(pool.PopArray());
                    Expressions.Add(pool.PopExpression());
                    break;
                case InstructionType.InitObject:
                    Expressions.Add(pool.PopArray(true));
                    break;

                // type 3: constant resolve needed
                case InstructionType.EA_GetNamedMember:
                    Expressions.Add(new SNLiteral(instruction)); 
                    Expressions.Add(pool.PopExpression());
                    break;
                case InstructionType.EA_PushValueOfVar:
                    Expressions.Add(new SNLiteral(instruction)); 
                    break;
                case InstructionType.EA_PushGlobalVar:
                case InstructionType.EA_PushThisVar:
                case InstructionType.EA_PushGlobal:
                case InstructionType.EA_PushThis:
                    break; // nothing needed
                case InstructionType.EA_PushConstantByte:
                case InstructionType.EA_PushConstantWord:
                    Expressions.Add(new SNLiteral(instruction)); 
                    break;
                case InstructionType.EA_PushRegister:
                    Expressions.Add(new SNLiteral(instruction)); 
                    break;

                // type 4: variable output count
                case InstructionType.PushData:
                    // TODO
                    break;
                case InstructionType.Enumerate:
                    // TODO
                    break;
                case InstructionType.Enumerate2:
                    // TODO
                    break;

                // no hits
                default:
                    flagSpecialProc = false;
                    break;
            }
            if ((!flagSpecialProc) && instruction is InstructionEvaluable inst)
            {
                // TODO string output
                for (int i = 0; i < inst.StackPop; ++i)
                    Expressions.Add(pool.PopExpression());
            }
            else if (!flagSpecialProc) // not implemented instructions
            {
                throw new NotImplementedException(instruction.Type.ToString());
            }
        }

        public virtual void TryCompile(StatementCollection sta, bool compileBranches = false)
        {
            // get all values
            var valCode = new string[Expressions.Count];
            var val = new Value?[Expressions.Count]; // should never be constant or register type
            for (int i = 0; i < Expressions.Count; ++i)
            {
                // get value
                var ncur = Expressions[i];
                if (ncur == null)
                {
                    valCode[i] = $"__args__[{i}]";
                    val[i] = null;
                    continue;
                }
                else if (ncur.TryGetValue(InstructionUtils.ParseValueWrapped(sta), out var nval) && !(nval is LogicalValue))
                {
                    valCode[i] = sta.GetExpression(nval!);
                    val[i] = nval;
                    // if (string.IsNullOrEmpty(valCode[i]))
                    // {
                    //     ncur.TryCompile(sta);
                    //     valCode[i] = ncur.Code == null ? $"__args__[{i}]" : ncur.Code; // do not care empty string
                    // }
                }
                else
                {
                    ncur.TryCompile(sta);
                    valCode[i] = ncur.Code == null ? $"__args__[{i}]" : ncur.Code;
                    val[i] = null;
                    // fix precendence
                    // only needed for compiled codes
                    if (ncur.Instruction != null && Instruction.LowestPrecendence > ncur.Instruction.LowestPrecendence)
                        valCode[i] = $"({valCode[i]})";
                }
            }

            // fix string values
            // case 1: all strings are needed to fix
            if (Instruction.Type == InstructionType.Add2 ||
                Instruction.Type == InstructionType.GetURL ||
                Instruction.Type == InstructionType.GetURL2 ||
                Instruction.Type == InstructionType.StringConcat ||
                Instruction.Type == InstructionType.StringEquals)
            {
                for (int i = 0; i < Expressions.Count; ++i)
                {
                    if (val[i] != null && val[i]!.Type == ValueType.String)
                        valCode[i] = valCode[i].ToCodingForm();
                }
            }
            // case 2: only the first one
            else if (Instruction.Type == InstructionType.DefineLocal ||
                     // Instruction.Type == InstructionType.Var ||
                     Instruction.Type == InstructionType.ToInteger ||
                     Instruction.Type == InstructionType.ToString ||
                     Instruction.Type == InstructionType.SetMember ||
                     Instruction.Type == InstructionType.SetVariable ||
                     Instruction.Type == InstructionType.SetProperty ||
                     Instruction.Type == InstructionType.EA_PushString ||
                     // Instruction.Type == InstructionType.EA_SetStringMember ||
                     // Instruction.Type == InstructionType.EA_SetStringVar ||
                     Instruction.Type == InstructionType.EA_PushConstantByte ||
                     Instruction.Type == InstructionType.EA_PushConstantWord ||
                     Instruction.Type == InstructionType.Trace)
            {
                if (val[0] != null && val[0]!.Type == ValueType.String)
                    valCode[0] = valCode[0].ToCodingForm();
            }
            // case 3 special handling

            // start compile
            string ret = string.Empty;
            string tmp = string.Empty;
            switch (Instruction.Type)
            {
                // case 1: branches (break(1); continue(2); non-standatd codes(3))
                case InstructionType.BranchAlways:
                case InstructionType.BranchIfTrue:
                case InstructionType.EA_BranchIfFalse:
                    if (compileBranches)
                    {
                        var itmp = Instruction;
                        var ttmp = itmp.Type;
                        var lbl = $"[[{itmp.Parameters[0]}]]";
                        while (itmp is LogicalTaggedInstruction itag)
                        {
                            lbl = string.IsNullOrEmpty(itag.Label) && itag.TagType == TagType.GotoLabel ? lbl : itag.Label;
                            itmp = itag.Inner;
                        }
                        if (itmp.Type == InstructionType.BranchAlways)
                            if (itmp.Parameters[0].ToInteger() > 0)
                                ret = $"break; // __jmp__({lbl!.ToCodingForm()})@";
                            else
                                ret = $"continue; // __jmp__({lbl!.ToCodingForm()})@";
                        else
                        {
                            tmp = valCode[0];
                            (tmp, ttmp) = InstructionUtils.SimplifyCondition(tmp, ttmp);
                            ret = $"__{(ttmp == InstructionType.BranchIfTrue ? "jz" : "jnz")}__({lbl!.ToCodingForm()}, {tmp})";
                        }

                    }
                    break;
                // case 2: value assignment statements
                case InstructionType.SetRegister:
                    var nrReg = Instruction.Parameters[0].ToInteger();
                    if (val[0] == null || !sta.NodeNames2.ContainsKey(val[0]!))
                    {
                        var regSet = sta.HasRegisterName(nrReg, out var nReg, out var co);
                        var c = (val[0] != null ? 2 : 1) + (co ? 2 : 0);
                        if (!regSet || co)
                        {
                            nReg = sta.NameRegister(nrReg, InstructionUtils.JustifyName(valCode[0]));
                            if (val[0] != null)
                                sta.NameVariable(val[0]!, nReg, true);
                            ret = $"var {nReg} = {valCode[0]}; // [[register #{nrReg}]], case {c}@";
                        }
                        else
                        {
                            ret = $"{nReg} = {valCode[0]}; // [[register #{nrReg}]], case {c + 4}@";
                        }
                    }
                    else
                    {
                        sta.NameRegister(nrReg, sta.NodeNames2[val[0]!]);
                        ret = $"// [[register #{nrReg}]] <- {sta.NodeNames2[val[0]!]}@"; // do nothing
                    }
                    break;
                // NodeNames should be updated
                case InstructionType.SetMember: // val[1] is integer: [] else: .
                    if (val[1] == null || val[1]!.Type == ValueType.Integer)
                        ret = $"{valCode[2]}[{valCode[1]}] = {valCode[0]}";
                    else
                        ret = Instruction.ToString(valCode);
                    break;

                //case InstructionType.SetVariable:

                //  break;
                // case 3: omitted cases
                case InstructionType.Pop:
                    ret = $"// __pop__({valCode[0]})@";
                    break;
                case InstructionType.End:
                    ret = "// __end__()@";
                    break;
                case InstructionType.PushDuplicate:
                    ret = valCode[0];
                    break;

                // case 0: unhandled cases | handling is not needed
                default:
                    try
                    {
                        ret = Instruction.ToString(valCode);
                    }
                    catch
                    {
                        ret = Instruction.ToString2(valCode);
                    }
                    break;
            }
            Code = ret;
        }
        
    }


    public abstract class SNExpression : SyntaxNode
    {
        public virtual int LowestPrecendence => 18; // Just a meme, as long as it is a negative number it is okay

        private static Dictionary<InstructionType, int> NIE = new();

        public SNExpression(InstructionBase inst) : base(inst) { }

        public virtual bool TryGetValue(Func<Value?, Value?>? parse, out Value? ret)
        {
            Value = ret = null;
            var vals = new Value[Expressions.Count];
            for (int i = 0; i < Expressions.Count; ++i)
            {
                var node = Expressions[i];
                if (node == null)
                    return false;
                if (node.TryGetValue(parse, out var val))
                {
                    if (val!.IsSpecialType())
                    {
                        if (parse == null)
                            return false;
                        else
                        {
                            val = parse(val);
                            if (val == null || val!.IsSpecialType())
                                return false;
                        }
                    }
                    vals[i] = val;
                }
                else
                    return false;
            }
            
            try
            {
                if (Instruction is InstructionEvaluable inst && inst.PushStack)
                {
                    // NIE optimization
                    if (NIE.TryGetValue(Instruction.Type, out var c) && c > 4)
                        return false;
                    ret = inst.ExecuteWithArgs2(vals);
                    if (ret!.Type == ValueType.Constant || ret.Type == ValueType.Register)
                    {
                        if (parse == null)
                            return false;
                        else
                            ret = parse(ret);
                    }
                    Value = ret;
                }
                else
                {
                    //TODO
                    return false;
                }

            }
            catch (NotImplementedException)
            {
                NIE[Instruction.Type] = NIE.TryGetValue(Instruction.Type, out var c) ? c + 1 : 1;
                return false;
            }
            return ret != null;
        }

        public override void TryCompile(StatementCollection sta, bool compileBranches = false)
        {
            // TODO use sta
            string ret = string.Empty;
            if (TryGetValue(InstructionUtils.ParseValueWrapped(sta), out var val))
            {
                if (val == null)
                    ret = "null";
                else if (val.Type == ValueType.String)
                    ret = val.ToString().ToCodingForm();
                else
                    ret = val.ToString();
                Code = ret;
            }
            else
            {
                base.TryCompile(sta, compileBranches);
            }
        }
    }

    public class SNEnumerate : SNExpression
    {
        public SNExpression Node { get; protected set; }
        public SNEnumerate(SNExpression node) : base()
        {
            Node = node;
        }

        public override bool TryGetValue(Func<Value?, Value?>? parse, out Value? ret)
        {
            ret = null;
            return false;
        }

        public override string TryComposeRaw()
        {
            return "[[enumerate node]]";
        }
    }

    public class SNLiteral : SNExpression
    {
        public RawValue Value { get; protected set; }
        public bool IsStringLiteral => Value.Type == RawValueType.String;

        public SNLiteral(RawValue v) : base()
        {
            Value = v;
        }

        public override string TryComposeRaw()
        {
            switch (Value.Type)
            {
                case RawValueType.String:
                    return Value.String.ToCodingForm();
                case RawValueType.Integer:
                    return $"{Value.Integer}";
                case RawValueType.Float:
                    return $"{Value.Double}";
                case RawValueType.Boolean:
                    return Value.Boolean ? "true" : "false";
                case RawValueType.Constant:
                case RawValueType.Register:
                default:
                    throw new InvalidOperationException("Well...This situation is really weird to be reached.");
            }
        }

        public string GetRawString() { return Value.String; }
        
    }

    public class SNNominator: SNExpression
    {
        public string Name { get; set; }

        public SNNominator(string name): base()
        {
            Name = name;
        }

        public override string TryComposeRaw()
        {
            return Name;
        }

    }

    public class SNRegisterRef: SNExpression
    {
        public int RegId { get; set; }

        public SNRegisterRef(int rid): base()
        {
            RegId = rid;
        }
    }

    public class SNArray : SNExpression
    {
        private readonly LogicalValue _v;
        public SNArray() : base(new InitArray())
        {
            _v = new(this);
        }

        public override bool TryGetValue(Func<Value?, Value?>? parse, out Value? ret)
        {
            ret = _v;
            return true;
        }
        // TODO use sta
        public override void TryCompile(StatementCollection sta, bool compileBranches = false)
        {
            var vals = new string[Expressions.Count];
            for (int i = 0; i < Expressions.Count; ++i)
            {
                var node = Expressions[i];
                if (node == null)
                {
                    vals[i] = $"__args__[{i}]";
                    continue;
                }
                var flag = node.TryGetValue(InstructionUtils.ParseValueWrapped(sta), out var val);
                if (!flag || val is LogicalValue)
                {
                    node.TryCompile(sta, compileBranches);
                    vals[i] = node.Code == null ? $"__args__[{i}]" : node.Code;
                }
                else
                {
                    vals[i] = sta.GetExpression(val!);
                    //if (string.IsNullOrEmpty(vals[i]))
                    //{
                    //    node.TryCompile(sta);
                    //    vals[i] = node.Code == null ? $"__args__[{i}]" : node.Code; // do not care empty string
                    //}
                }
                if (node is SNArray)
                {
                    vals[i] = $"[{vals[i]}]";
                }
            }
            Code = string.Join(", ", vals);
        }
    }

    public class NodeFunctionBody : SNExpression
    {
        public StatementCollection Body;
        public static readonly string NoIndentMark = "/*@([{@%@)]}@*/";
        private readonly LogicalValue _v;
        public NodeFunctionBody(InstructionBase inst, StatementCollection body) : base(inst)
        {
            Body = body;
            _v = new(this);
        }

        public override bool TryGetValue(Func<Value?, Value?>? parse, out Value? ret)
        {
            ret = _v;
            return true;
        }

        public override void TryCompile(StatementCollection sta, bool compileBranches = false)
        {
            StringBuilder sb = new();
            var (name, args) = InstructionUtils.GetNameAndArguments(Instruction);
            var head = $"function({string.Join(", ", args.ToArray())})\n";
            // sb.Append(NoIndentMark);
            sb.Append(head);
            // sb.Append(NoIndentMark);
            sb.Append("{\n");
            Body.Compile(sb, 1, 1, true, false);
            // sb.Append(NoIndentMark);
            sb.Append("}");
            Code = sb.ToString();
        }
    }


    public abstract class SNStatement : SyntaxNode
    {
        public SNStatement() : base() { }
        public override void TryCompile(StatementCollection sta, bool compileBranches = false) { base.TryCompile(sta, compileBranches); }
    }

    public class SNAssignValue : SNStatement
    {
        public override string TryComposeRaw()
        {
            throw new NotImplementedException();
        }
    }


    public abstract class NodeControl : SNStatement
    {
        public NodeControl(InstructionBase inst) : base(inst) { }
        public override void TryCompile(StatementCollection sta, bool compileBranches = false) { base.TryCompile(sta, compileBranches); }

        public abstract void TryCompile2(StatementCollection sta, StringBuilder sb, int indent = 0, int dIndent = 4, bool compileSubCollections = true);
    }

    public class NodeIncludeFunction : NodeControl
    { 
        public readonly SyntaxNode n;
        public NodeIncludeFunction(SyntaxNode body) : base(body.Instruction)
        {
            n = body;
        }
        public override void TryCompile(StatementCollection sta, bool compileBranches = false) { n.TryCompile(sta, compileBranches); Code = n.Code; } 

        // using brute force ways to do so
        public override void TryCompile2(StatementCollection sta, StringBuilder sb, int indent = 0, int dIndent = 4, bool compileSubCollections = true)
        {
            TryCompile(sta, true);
            var lines = Code!.Split("\n");
            for (var i = 0; i < lines.Count(); ++i)
            {
                var l = lines[i];
                if (string.IsNullOrWhiteSpace(l))
                    continue;
                var tmpindent = 0;
                while (l.ElementAt(tmpindent) == ' ')
                    ++tmpindent;
                if (i == lines.Count() - 1)
                    if (l.EndsWith("@"))
                        l = l.Substring(0, l.Length - 1);
                    else if (!l.EndsWith(";"))
                        l = l + ";";
                sb.Append(l.Substring(tmpindent).ToStringWithIndent(indent + tmpindent * dIndent));
                sb.Append("\n");
            }
        }
    }

    public class NodeDefineFunction : NodeControl
    {
        public StatementCollection Body;
        public NodeDefineFunction(InstructionBase inst, StatementCollection body) : base(inst)
        {
            Body = body;
        }

        public override void TryCompile(StatementCollection sta, bool compileBranches = false) { throw new NotImplementedException(); }

        public override void TryCompile2(StatementCollection sta, StringBuilder sb, int indent = 0, int dIndent = 4, bool compileSubCollections = true)
        {
            var (name, args) = InstructionUtils.GetNameAndArguments(Instruction);
            var head = $"function {name}({string.Join(", ", args.ToArray())})\n";
            sb.Append(head.ToStringWithIndent(indent));
            sb.Append("{\n".ToStringWithIndent(indent));
            Body.Compile(sb, indent + dIndent, dIndent, compileSubCollections, false, sta);
            sb.Append("}\n".ToStringWithIndent(indent));
        }
    }

    public class NodeCase: NodeControl
    {

        public SNExpression? Condition;
        public StatementCollection Unbranch;
        public StatementCollection Branch;

        public NodeCase(
            InstructionBase inst,
            SNExpression? condition,
            StatementCollection unbranch, 
            StatementCollection branch
            ) : base(inst)
        {
            Condition = condition;
            Unbranch = unbranch;
            Branch = branch;
        }

        public static bool IsElseIfBranch(StatementCollection sta, out NodeCase? c)
        {
            var ret = false;
            c = null;
            if (sta.IsEmpty())
                return ret;
            else if (sta.Nodes.Count() == 1)
            {
                ret = sta.Nodes.ElementAt(0) is NodeCase;
                if (ret)
                    c = (NodeCase) sta.Nodes.ElementAt(0);
            }
            else
            {
                var cases = 0;
                var noncases = 0;
                foreach (var n in sta.Nodes)
                {
                    if (n is NodeCase)
                    {
                        c = (NodeCase) n;
                        ++cases;
                    }
                    else
                    {
                        // TODO may need fancier codelessness judgement
                        // n.TryCompile(sta);
                        // if (!string.IsNullOrEmpty(n.Code))
                        ++noncases;
                    }
                    if (cases > 1 || noncases > 0)
                    {
                        ret = false;
                        break;
                    }
                }
                return cases == 1 && noncases == 0;
            }
            return ret;
        }

        public override void TryCompile2(StatementCollection sta, StringBuilder sb, int indent = 0, int dIndent = 4, bool compileSubCollections = true)
        {
            TryCompile2(sta, sb, indent, dIndent, compileSubCollections, false);
        }

        public void TryCompile2(StatementCollection sta, StringBuilder sb, int indent, int dIndent, bool compileSubCollections, bool elseIfBranch)
        {
            var tmp = "[[null condition]]";
            if (Condition != null)
            {
                Condition.TryCompile(sta, true); // TODO turn the condition to real condition
                tmp = Condition.Code!;
            }

            var b1 = Unbranch;
            var b2 = Branch;
            var ei1 = IsElseIfBranch(b1, out var nc1); 
            var ei2 = IsElseIfBranch(b2, out var nc2); // these are ensured to compile

            if (Instruction.Type == InstructionType.BranchIfTrue)
                tmp = InstructionUtils.ReverseCondition(tmp);

            if (ei1 ^ ei2)
            {
                if (ei1)
                {
                    var b3 = b2;
                    b2 = b1;
                    b1 = b3;
                    var nc3 = nc2;
                    nc2 = nc1;
                    nc1 = nc3;
                    tmp = InstructionUtils.ReverseCondition(tmp);
                }
                var ifBranch = elseIfBranch ? $"if ({tmp})\n" : $"if ({tmp})\n".ToStringWithIndent(indent);
                sb.Append(ifBranch);
                sb.Append("{\n".ToStringWithIndent(indent));
                b1.Compile(sb, indent + dIndent, dIndent, compileSubCollections, false, sta);
                sb.Append("}\n".ToStringWithIndent(indent));
                sb.Append("else ".ToStringWithIndent(indent));
                nc2!.TryCompile2(sta, sb, indent, dIndent, compileSubCollections, true);
            }
            else
            {
                var ifBranch2 = elseIfBranch ? $"if ({tmp})\n" : $"if ({tmp})\n".ToStringWithIndent(indent);
                sb.Append(ifBranch2);
                sb.Append("{\n".ToStringWithIndent(indent));
                b1.Compile(sb, indent + dIndent, dIndent, compileSubCollections, false, sta);
                sb.Append("}\n".ToStringWithIndent(indent));
                if (!b2.IsEmpty())
                {
                    sb.Append("else\n".ToStringWithIndent(indent));
                    sb.Append("{\n".ToStringWithIndent(indent));
                    b2.Compile(sb, indent + dIndent, dIndent, compileSubCollections, false, sta);
                    sb.Append("}\n".ToStringWithIndent(indent));
                }
            }
        }
    }

    public class NodeLoop : NodeControl
    {

        public SNExpression? Condition;
        public StatementCollection Maintain;
        public StatementCollection Branch;

        public NodeLoop(
            InstructionBase inst,
            SNExpression? condition,
            StatementCollection maintain,
            StatementCollection branch
            ) : base(inst)
        {
            Condition = condition;
            Maintain = maintain;
            Branch = branch;
        }

        public override void TryCompile2(StatementCollection sta, StringBuilder sb, int indent = 0, int dIndent = 4, bool compileSubCollections = true)
        {
            var tmp = "[[null condition]]";
            if (Condition != null)
            {
                Condition.TryCompile(sta, true); // TODO turn the condition to real condition
                tmp = Condition.Code!;
            }
            var ttmp = Instruction.Type;
            if (ttmp == InstructionType.BranchIfTrue)
            {
                if (tmp.StartsWith("!"))
                {
                    tmp = tmp.Substring(1);
                    if (tmp.StartsWith('(') && tmp.EndsWith(')'))
                        tmp = tmp.Substring(1, tmp.Length - 2);
                }
                else
                {
                    tmp = $"!({tmp})";
                }
            }
            sb.Append("{ // loop maintain condition\n".ToStringWithIndent(indent));
            Maintain.Compile(sb, indent + dIndent, dIndent, compileSubCollections, true, sta);
            sb.Append("}\n".ToStringWithIndent(indent));
            sb.Append($"while ({tmp})\n".ToStringWithIndent(indent));
            sb.Append("{\n".ToStringWithIndent(indent));
            Branch.Compile(sb, indent + dIndent, dIndent, compileSubCollections, false, sta);
            sb.Append("}\n".ToStringWithIndent(indent));
        }
    }

}
