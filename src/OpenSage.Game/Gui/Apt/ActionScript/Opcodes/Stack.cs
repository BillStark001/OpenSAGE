﻿using System;
using System.Collections.Generic;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{
    /// <summary>
    /// Push a string to the stack
    /// </summary>
    public sealed class PushString : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushString;
        public override uint Size => 4;

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0]);
        }
    }

    /// <summary>
    /// Push a float to the stack
    /// </summary>
    public sealed class PushFloat : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushFloat;
        public override uint Size => 4;

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0]);
        }
    }

    /// <summary>
    /// Read a constant from the pool and push it to stack
    /// </summary>
    public sealed class PushConstantByte : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushConstantByte;
        public override uint Size => 1;

        public override void Execute(ActionContext context)
        {
            var id = Parameters[0].ToInteger();
            context.Push(context.Constants[id]);
        }
    }

    /// <summary>
    /// Read a byte and push it to the stack
    /// </summary>
    public sealed class PushByte : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushByte;
        public override uint Size => 1;

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0]);
        }
    }

    /// <summary>
    /// Read a short and push it to the stack
    /// </summary>
    public sealed class PushShort : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushShort;
        public override uint Size => 2;

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0]);
        }
    }

    /// <summary>
    /// Read an int32 and push it to the stack (although claimed to be long)
    /// </summary>
    public sealed class PushLong : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushLong;
        public override uint Size => 4;

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0]);
        }
    }

    /// <summary>
    /// Read the variable name from the pool and push that variable's value to the stack
    /// </summary>
    public sealed class PushValueOfVar : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushValueOfVar;
        public override uint Size => 1;

        public override void Execute(ActionContext context)
        {
            var id = Parameters[0].ToInteger();
            var str = context.Constants[id].ToString();

            Value result;

            if (context.CheckParameter(str))
            {
                result = context.GetParameter(str);
            }
            else
            {
                result = context.GetValueOnChain(str);
            }
            context.Push(result);
        }
    }

    /// <summary>
    /// Push an undefined value to the stack
    /// </summary>
    public sealed class PushUndefined : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushUndefined;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.Undefined());
        }
    }

    /// <summary>
    /// Push a boolean with value false to the stack
    /// </summary>
    public sealed class PushFalse : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushFalse;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromBoolean(false));
        }
    }

    /// <summary>
    /// Push a null value false to the stack
    /// </summary>
    public sealed class PushNull : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushNull;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromObject(null));
        }
    }

    /// <summary>
    /// Push an integer with value zero to the stack
    /// </summary>
    public sealed class PushZero : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushZero;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromInteger(0));
        }
    }

    /// <summary>
    /// Push the current object to the stack
    /// </summary>
    public sealed class PushThis : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushThis;

        public override void Execute(ActionContext context)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Push the current object to the stack
    /// </summary>
    public sealed class PushThisVar : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushThisVar;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromObject(context.This));
        }
    }

    /// <summary>
    /// Push an integer with value one to the stack
    /// </summary>
    public sealed class PushOne : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushOne;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromInteger(1));
        }
    }

    /// <summary>
    /// Push a boolean with value true to the stack
    /// </summary>
    public sealed class PushTrue : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushTrue;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromBoolean(true));
        }
    }

    /// <summary>
    /// Get multiple variables from the pool and push them to the stack
    /// </summary>
    public sealed class PushData : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.PushData;
        public override uint Size => 8;

        public override void Execute(ActionContext context)
        {
            foreach (var constant in Parameters)
            {
                context.Push(constant.ResolveConstant(context));
            }
        }
    }

    /// <summary>
    /// Push a zero variable to the stack
    /// </summary>
    public sealed class ZeroVar : InstructionMonoPushPop
    {
        public override bool PopStack => true;
        public override InstructionType Type => InstructionType.EA_ZeroVar;

        public override void Execute(ActionContext context)
        {
            // TODO: check if this is correct
            var name = context.Pop();
            context.This.SetMember(name.ToString(), Value.FromInteger(0));
        }
    }

    /// <summary>
    /// Push the global object to the stack
    /// </summary>
    public sealed class PushGlobalVar : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushGlobalVar;

        public override void Execute(ActionContext context)
        {
            context.Push(Value.FromObject(context.Apt.Avm.GlobalObject));
        }
    }

    /// <summary>
    /// Pop a value from the stack and push it twice
    /// </summary>
    public sealed class PushDuplicate : InstructionBase
    {
        public override InstructionType Type => InstructionType.PushDuplicate;
        public override bool IsStatement => false;

        public override void Execute(ActionContext context)
        {
            var val = context.Peek();
            context.Push(val);
        }
    }

    /// <summary>
    /// Push a register's value to the stack
    /// </summary>
    public sealed class PushRegister : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushRegister;
        public override uint Size => 1;

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0].ResolveRegister(context));
        }
    }

    /// <summary>
    /// (Just guessing) Similiar to PushConstantByte,
    /// read a constant from the pool and push it to stack,
    /// but it reads a Int16 instead of byte as constant index
    /// </summary>
    public sealed class PushConstantWord : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override InstructionType Type => InstructionType.EA_PushConstantWord;
        public override uint Size => 2;

        public override void Execute(ActionContext context)
        {
            var id = Parameters[0].ToInteger();
            context.Push(context.Constants[id]);
        }
    }

    /// <summary>
    /// Pop a value from the stack
    /// </summary>
    public sealed class Pop : InstructionMonoPushPop
    {
        public override bool PopStack => true;
        public override InstructionType Type => InstructionType.Pop;

        public override void Execute(ActionContext context)
        {
            context.Pop();
        }
    }

    /// <summary>
    /// Pop a value from the stack and convert it to number push it back
    /// </summary>
    public sealed class ToNumber : InstructionMonoPushPop
    {
        public override bool PushStack => true;
        public override bool PopStack => true;
        public override InstructionType Type => InstructionType.ToNumber;

        public override void Execute(ActionContext context)
        {
            var strVal = context.Pop().ToString();
            context.Push(Value.FromInteger(int.Parse(strVal)));
        }
    }

    public sealed class PushValue: InstructionBase
    {
        public override InstructionType Type => throw new InvalidOperationException("Should not be called since this is not a standard instruction");

        public PushValue(Value v): base() { Parameters = new List<Value> { v }; }

        public override void Execute(ActionContext context)
        {
            context.Push(Parameters[0]);
        }
    }
}
