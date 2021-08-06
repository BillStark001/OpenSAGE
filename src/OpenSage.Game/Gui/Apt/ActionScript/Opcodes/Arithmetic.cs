﻿using System;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{
    /// <summary>
    /// An instruction that pops two values and adds them. Result on stack
    /// </summary>
    public sealed class Add : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) => Value.FromFloat(b.ToFloat() + a.ToFloat());
        public override InstructionType Type => InstructionType.Add;
    }

    /// <summary>
    /// An instruction that pops two values and subtracts them. Result on stack
    /// </summary>
    public sealed class Subtract : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) => Value.FromFloat(b.ToFloat() - a.ToFloat());
        public override InstructionType Type => InstructionType.Subtract;
    }

    /// <summary>
    /// Pop two values from stack and add them. Can concatenate strings. Result on stack
    /// The additive operator follows https://262.ecma-international.org/5.1/#sec-11.6.1
    /// </summary>
    public sealed class Add2 : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) =>
            (a.IsNumericType() && b.IsNumericType()) ?
            Value.FromFloat(b.ToFloat() + a.ToFloat()) :
            Value.FromString(b.ToString() + a.ToString());
        public override InstructionType Type => InstructionType.Add2;
    }

    /// <summary>
    /// Pop two values from stack, convert them to float and then multiply them. Result on stack
    /// </summary>
    public sealed class Multiply : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) => Value.FromFloat(b.ToFloat() * a.ToFloat());
        public override InstructionType Type => InstructionType.Multiply;
    }

    /// <summary>
    /// Pop two values from stack, convert them to float and then divide them. Result on stack
    /// </summary>
    public sealed class Divide : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) =>
            {
                var af = a.ToFloat();
                var bf = b.ToFloat();

                var val_to_push = Value.FromFloat(float.NaN);

                if (af != 0) { val_to_push = Value.FromFloat(bf / af); }

                return val_to_push;
            };
        public override InstructionType Type => InstructionType.Divide;
    }

    /// <summary>
    /// Pop two values from stack, convert them to float and then divide them. Result on stack
    /// </summary>
    public sealed class Modulo : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) => Value.FromFloat(a.ToFloat() % b.ToFloat());
        public override InstructionType Type => InstructionType.Modulo;
    }

    /// <summary>
    /// Pop a value from stack, increments it and pushes it back
    /// </summary>
    public sealed class Increment : InstructionMonoOperator
    {
        public override Func<Value, Value> Operator =>
            (a) => Value.FromInteger(a.ToInteger() + 1);
        public override InstructionType Type => InstructionType.Increment;
    }

    /// <summary>
    /// Pop a value from stack, increments it and pushes it back
    /// </summary>
    public sealed class Decrement : InstructionMonoOperator
    {
        public override Func<Value, Value> Operator =>
            (a) => Value.FromInteger(a.ToInteger() - 1);
        public override InstructionType Type => InstructionType.Decrement;
    }

    public sealed class ShiftLeft : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) =>
            {
                var count = a.ToInteger() & 0b11111;
                var val = b.ToInteger();
                return Value.FromInteger(val << count);
            };
        public override InstructionType Type => InstructionType.ShiftLeft;
    }

    public sealed class ShiftRight : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) =>
            {
                var count = a.ToInteger() & 0b11111;
                var val = b.ToInteger();
                return Value.FromInteger(val >> count);
            };
        public override InstructionType Type => InstructionType.ShiftRight;
    }

    // shift right as uint
    public sealed class ShiftRight2 : InstructionDiOperator
    {
        public override Func<Value, Value, Value> Operator =>
            (a, b) =>
            {
                var count = a.ToInteger() & 0b11111;
                var val = (uint) b.ToInteger();
                return Value.FromInteger((int) (val >> count));
            };
        public override InstructionType Type => InstructionType.ShiftRight2;
    }
}
