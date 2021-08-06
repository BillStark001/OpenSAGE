﻿using System;
using OpenSage.Data.Apt.Characters;
using OpenSage.Gui.Apt.ActionScript.Library;

namespace OpenSage.Gui.Apt.ActionScript.Opcodes
{
    /// <summary>
    /// Pop an object from stack and retrieve the member name from the pool. Push the member of
    /// the object to stack.
    /// </summary>
    public sealed class GetNamedMember : InstructionMonoPushPop
    {
        public override InstructionType Type => InstructionType.EA_GetNamedMember;
        public override uint Size => 1;
        public override bool PushStack => true;
        public override bool PopStack => true;

        public override void Execute(ActionContext context)
        {
            var id = Parameters[0].ToInteger();
            var member = context.Constants[id].ToString();

            //pop the object
            var objectVal = context.Pop();
            var obj = objectVal.ToObject();

            if (obj != null)
            {
                context.Push(obj.GetMember(member));
            }
            else
            {
                context.Push(Value.Undefined());
            }
        }
    }

    /// <summary>
    /// set the member of a specific object (everything on stack)
    /// </summary>
    public sealed class SetMember : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.SetMember;
        public override uint StackPop => 3;

        public override void Execute(ActionContext context)
        {
            //pop the value
            var valueVal = context.Pop();
            //pop the member name
            var memberName = context.Pop().ToString();
            //pop the object
            var obj = context.Pop().ToObject();
            if (obj is null)
                throw new NotImplementedException("Do not know what to do in this situation");
            else
                obj.SetMember(memberName, valueVal);

        }
    }

    /// <summary>
    /// Get a variable from the current object and push it to the stack
    /// </summary>
    public sealed class GetStringVar : InstructionMonoPushPop
    {
        public override InstructionType Type => InstructionType.EA_GetStringVar;
        public override uint Size => 4;
        public override bool PushStack => true;

        public override void Execute(ActionContext context)
        {
            var str = Parameters[0].ToString();

            // check if this a special object, like _root, _parent etc.
            // this is automatically done by the built-in variables in the global object.
            var result = context.GetValueOnChain(str);

            if (result == null)
                throw new InvalidOperationException();

            context.Push(result);
        }
    }

    /// <summary>
    /// Set a string variable in the current scope
    /// </summary>
    public sealed class SetStringVar : InstructionMonoPushPop
    {
        public override InstructionType Type => InstructionType.EA_SetStringVar;
        public override uint Size => 4;
        public override bool PopStack => true;

        public override void Execute(ActionContext context)
        {
            var name = context.Pop().ToString();
            context.This.SetMember(name, Parameters[0]);
        }
    }

    /// <summary>
    /// Pops variable name and pushes the corresponding variable back to stack
    /// </summary>
    public sealed class GetVariable : InstructionMonoPushPop
    {
        public override InstructionType Type => InstructionType.GetVariable;
        public override bool PopStack => true;
        public override bool PushStack => true;

        public override void Execute(ActionContext context)
        {
            //pop the value
            var variableName = context.Pop().ToString();
            Value variable = context.This.GetMember(variableName);
            context.Push(variable);
        }
    }

    /// <summary>
    /// Pops variable name and value from the stack. Then set the variable to that value.
    /// </summary>
    public sealed class SetVariable : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.SetVariable;
        public override uint StackPop => 2;

        public override void Execute(ActionContext context)
        {
            var valueVal = context.Pop();
            var memberName = context.Pop().ToString();
            context.This.SetMember(memberName, valueVal);
        }
    }

    /// <summary>
    /// get the member of a specific object. Result will be pushed to stack
    /// </summary>
    public sealed class GetMember : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.GetMember;
        public override uint StackPop => 2;
        public override bool PushStack => true;

        public override void Execute(ActionContext context)
        {
            var member = context.Pop();
            var obj = context.Pop().ToObject();

            context.Push(obj.GetMember(member.ToString()));
        }
    }

    /// <summary>
    /// get a property and push it to the stack
    /// </summary>
    public sealed class GetProperty : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.GetProperty;
        public override uint StackPop => 2;
        public override bool PushStack => true;

        public override void Execute(ActionContext context)
        {
            var property = context.Pop().ToEnum<PropertyType>();
            var target = context.GetTarget(context.Pop().ToString());

            var prop = ((StageObject) target.ToObject()).GetProperty(property);
            context.Push(prop);
        }
    }

    /// <summary>
    /// set a property. Get value, name and object from stack
    /// </summary>
    public sealed class SetProperty : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.SetProperty;
        public override uint StackPop => 3;

        public override void Execute(ActionContext context)
        {
            var value = context.Pop();
            var property = context.Pop().ToEnum<PropertyType>();
            var target = context.GetTarget(context.Pop().ToString());

             target.ToObject<StageObject>().SetProperty(property, value);
        }
    }

    /// <summary>
    /// clones a sprite to assigned depth and target.
    /// </summary>
    public sealed class CloneSprite : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.CloneSprite;
        public override uint StackPop => 3;
        public override void Execute(ActionContext context)
        {
            var depth = context.Pop();
            var target = context.Pop();
            var source = context.Pop();
            throw new NotImplementedException();            
        }
    }

    /// <summary>
    /// removes a sprite.
    /// </summary>
    public sealed class RemoveSprite : InstructionMonoPushPop
    {
        public override InstructionType Type => InstructionType.RemoveSprite;
        public override bool PopStack => true;

        public override void Execute(ActionContext context)
        {
            var target = context.Pop();
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Pops an object from the stack and retrieves a member variable which is pushed to stack
    /// </summary>
    public sealed class GetStringMember : InstructionMonoPushPop
    {
        public override InstructionType Type => InstructionType.EA_GetStringMember;
        public override uint Size => 4;
        public override bool PopStack => true;
        public override bool PushStack => true;

        public override void Execute(ActionContext context)
        {
            //pop the member name
            var memberVal = Parameters[0];

            //pop the object
            var objectVal = context.Pop();

            var valueVal = objectVal.ToObject().GetMember(memberVal.ToString());

            context.Push(valueVal);
        }
    }

    public sealed class SetStringMember : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.EA_SetStringMember;
        public override uint Size => 4;
        public override uint StackPop => 2;

        public override void Execute(ActionContext context)
        {
            var memberVal = context.Pop().ToString();
            var objectVal = context.Pop().ToObject();

            objectVal.SetMember(memberVal, Parameters[0]);
        }
    }

    /// <summary>
    /// Create a new object by calling it's constructor
    /// </summary>
    public sealed class NewObject : InstructionBase
    {
        public override InstructionType Type => InstructionType.NewObject;
        public override bool IsStatement => false;

        public override void Execute(ActionContext context)
        {
            var name = context.Pop().ToString();
            var args = FunctionCommon.GetArgumentsFromStack(context);

            context.ConstructObjectAndPush(name, args);
        }
    }

    /// <summary>
    /// From its name, looks like it corresponds to the method definition of ECMAScript.
    /// Since it doesn't have any parameters, it should be using the stack.
    /// </summary>
    public sealed class NewMethod : InstructionBase
    {
        public override InstructionType Type => InstructionType.NewMethod;
        public override bool IsStatement => false;

        public override void Execute(ActionContext context)
        {
            var name = context.Pop().ToString();
            var obj = context.Pop();
            var args = FunctionCommon.GetArgumentsFromStack(context);
            
            if (name.Length != 0) obj = obj.ToObject().GetMember(name);

            context.ConstructObjectAndPush(obj, args);
            
        }
    }

    /// <summary>
    /// Initializes an object from the stack
    /// </summary>
    public sealed class InitObject : InstructionBase
    {
        public override InstructionType Type => InstructionType.InitObject;
        public override bool IsStatement => false;

        public override void Execute(ActionContext context)
        {
            var nArgs = context.Pop().ToInteger();
            var obj = new ObjectContext(context.Apt.Avm);
            for (int i = 0; i < nArgs; ++i)
            {
                var vi = context.Pop();
                var ni = context.Pop().ToString();
                obj.SetMember(ni, vi);
            }

            context.Push(Value.FromObject(obj));
        }
    }

    /// <summary>
    /// Pops a value from the stack pushes it's type as a string to stack
    /// </summary>
    public sealed class TypeOf : InstructionMonoOperator
    {
        public override Func<Value, Value> Operator => (v) => Value.FromString(v.GetStringType());
        public override InstructionType Type => InstructionType.TypeOf;
    }

    /// <summary>
    /// Set the inheritance structure of a class.
    /// See https://www.adobe.com/content/dam/acom/en/devnet/pdf/swf-file-format-spec.pdf p.114 "ActionExtends"
    /// </summary>
    public sealed class Extends : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.Extends;
        public override uint StackPop => 2;

        public override void Execute(ActionContext context)
        {
            // TODO This will override functions in the old prototype of cls.
            // I followed the document, but don't know if will cause issues.
            var sup = context.Pop().ToFunction();
            var cls = context.Pop().ToFunction();
            var obj = new ObjectContext(context.Apt.Avm);
            obj.__proto__ = sup.prototype;
            obj.constructor = sup;
            cls.prototype = obj;
        }
    }

    /// <summary>
    /// Corresponds to the `instanceof` keyword of ECMAScript.
    /// </summary>
    public sealed class InstanceOf : InstructionMonoPush
    {
        public override InstructionType Type => InstructionType.InstanceOf;
        public override uint StackPop => 2;
        public override bool PushStack => true;

        public override void Execute(ActionContext context)
        {
            var constr = context.Pop().ToFunction();
            var obj = context.Pop().ToObject();
            var val = obj.InstanceOf(constr);
            context.Push(Value.FromBoolean(val));
        }
    }

}
