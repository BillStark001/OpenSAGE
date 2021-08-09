﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenSage.FileFormats.Apt.ActionScript;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{
    

    /// <summary>
    /// The baseclass used for all Instructions. Set default variables accordingly
    /// </summary>
    public abstract class InstructionBase
    {
        public abstract InstructionType Type { get; }
        //the size in bytes for this instruction (not including the opcode size)
        public virtual uint Size { get { return 0; } set { } }
        public virtual List<Value> Parameters { get; set; }
        public virtual bool Breakpoint { get; set; }
        public virtual bool IsStatement => true;
        public virtual int Precendence => IsStatement ? 114-514-1919-810 : 18; // Just a meme, as long as it is a negative number it is okay

        public abstract void Execute(ActionContext context);
        public override string ToString()
        {
            return ToString((ActionContext) null);
        }
        public virtual string GetParameterDesc(ActionContext context)
        {
            if (Parameters == null) return "";
            string[] pv;// = { };
            var param_val = Parameters.Take(5).ToArray();
            pv = param_val.Select(x => x.ToStringWithType(context)).ToArray();
            return string.Join(", ", pv);
        }
        public virtual string ToString(ActionContext context)
        {
            return $"{Type}({GetParameterDesc(context)})";//: {Size}";
        }

        public virtual string ToString2(string[] parameters)
        {
            var t = (Parameters == null || Parameters.Count == 0) ? Type.ToString() : ToString();
            return $"{t}({string.Join(", ", parameters)})";
        }
        public virtual string ToString(string[] p) { return ToString2(p); }
    }

    public abstract class InstructionFixedStackOpr: InstructionBase
    {
        public virtual uint StackPush => 0;
        public virtual uint StackPop => 0;
        public override bool IsStatement => StackPush == 0;

        public override void Execute(ActionContext context)
        {
            var push = ExecuteWithArgs(context.Pop(StackPop));
            if ((push == null && StackPush > 0) || StackPush != push.Length)
                throw new InvalidOperationException("Argument count is wrong!");
            context.Push(push);
        }

        public abstract Value[] ExecuteWithArgs(Value[] poppedVals);
    }

    public abstract class InstructionMonoPush : InstructionFixedStackOpr
    {
        public override uint StackPush => PushStack ? 1u : 0u;
        public override uint StackPop => 0;
        public virtual bool PushStack => false;

        public override void Execute(ActionContext context)
        {
            var push = ExecuteWithArgs2(context.Pop(StackPop));
            if ((push == null && PushStack) || (push != null && !PushStack))
                throw new InvalidOperationException("Argument count is wrong!");
            context.Push(push);
        }

        public override Value[] ExecuteWithArgs(Value[] poppedVals)
        {
            var ret = ExecuteWithArgs2(poppedVals);
            if (!PushStack) return null;
            var ans = new Value[1];
            ans[0] = ret;
            return ans;
        }

        public virtual Value ExecuteWithArgs2(Value[] poppedVals)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class InstructionMonoPushPop : InstructionMonoPush
    {
        public override uint StackPop => PopStack ? 1u : 0u;
        public virtual bool PopStack => false;

        public override void Execute(ActionContext context)
        {
            var push = ExecuteWithArgs2(PopStack ? context.Pop() : null);
            if ((push == null && PushStack) || (push != null && !PushStack))
                throw new InvalidOperationException("Argument count is wrong!");
            context.Push(push);
        }

        public override Value[] ExecuteWithArgs(Value[] poppedVals)
        {
            var ret = ExecuteWithArgs2(poppedVals);
            if (!PushStack) return null;
            var ans = new Value[1];
            ans[0] = ret;
            return ans;
        }

        public override Value ExecuteWithArgs2(Value[] poppedVals)
        {
            return ExecuteWithArgs2(PopStack ? poppedVals[0] : null);
        }

        public virtual Value ExecuteWithArgs2(Value poppedVal)
        {
            throw new NotImplementedException();
        }
    }
    public class InstructionPushValue : InstructionMonoPushPop
    {
        public override InstructionType Type => throw new InvalidOperationException("Should not be called since this is not a standard instruction");
        public override bool PushStack => true;
        public InstructionPushValue() : base() { }
        public InstructionPushValue(Value v) : base() { Parameters = new List<Value> { v }; }
        public virtual Value ValueToPush => Parameters[0];

        public override void Execute(ActionContext context)
        {
            context.Push(ValueToPush);
        }
        public override Value ExecuteWithArgs2(Value poppedVal)
        {
            return ValueToPush;
        }
    }

    public abstract class InstructionMonoOperator: InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override bool PopStack => true;
        public virtual Func<Value, Value> Operator => throw new NotImplementedException();

        public override void Execute(ActionContext context)
        {
            context.Push(Operator(context.Pop()));
        }
        public override Value ExecuteWithArgs2(Value poppedVal)
        {
            return Operator(poppedVal);
        }
    }

    public abstract class InstructionDiOperator : InstructionMonoPush
    {
        public override bool PushStack => true;
        public override uint StackPop => 2;
        public virtual Func<Value, Value, Value> Operator => throw new NotImplementedException();

        public override void Execute(ActionContext context)
        {
            var a = context.Pop();
            var b = context.Pop();
            context.Push(Operator(a, b));
        }
        public override Value ExecuteWithArgs2(Value[] poppedVals)
        {
            return Operator(poppedVals[0], poppedVals[1]);
        }
    }

    /// <summary>
    /// This padding does exist in the InstructionStream because of alignment requirements of some instructions
    /// </summary>
    public sealed class Padding : InstructionBase
    {
        public override InstructionType Type => InstructionType.Padding;
        public override uint Size { get; set; }

        public override List<Value> Parameters
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override void Execute(ActionContext context)
        {
            throw new NotImplementedException();
        }

        public Padding(uint size)
        {
            Size = size;
        }
    }
}
