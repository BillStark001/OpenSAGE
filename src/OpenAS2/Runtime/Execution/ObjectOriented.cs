﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenAS2.Base;
using OpenAS2.Runtime;
using OpenAS2.Runtime.Library;

namespace OpenAS2.Runtime.Execution
{

    using OPR1 = Func<Value, Value>;
    using OPR2 = Func<Value, Value, Value>;
    using OPR1A = Action<Value>;
    using OPR2A = Action<Value, Value>;

    public static class ObjectOriented
    {

        static readonly OPR1 TypeOf = (v) => Value.FromString(v.GetStringType());
        static void DoCastOp (ExecutionContext context)
        {
            var obj = context.Pop().ToObject();
            var cst = context.Pop().ToFunction();
            var ins = obj.InstanceOf(context, cst);
            ins.AddRecallCode(ret => ESCallable.Return(Value.FromObject(ret.Value.ToBoolean() ? obj : null)));
            context.EnqueueResultCallback(ins);
        }
        static void DoExtends(ExecutionContext context) // this should work
        {
            var sup = context.Pop().ToFunction();
            var cls = context.Pop().ToFunction();
            var sproto = sup.IGet(context, "prototype");
            sproto.AddRecallCode(res =>
            {
                var v = res.Value.Type == ValueType.Object ? res.Value.ToObject() : null;
                var obj = new ESObject(null, true, v, null);
                cls.ConnectPrototype(obj);
                return null;
            });
            
        }
        static void DoInstanceOf(ExecutionContext context)
        {
            var constr = context.Pop().ToFunction();
            var obj = context.Pop().ToObject();
            var val = obj.InstanceOf(context, constr);
            context.EnqueueResultCallback(val);
        }

        public static void DoNewMethod(ExecutionContext context)
        {
            var nameVal = context.Pop();
            var name = nameVal.ToString();
            var obj = context.Pop();
            var args = FunctionUtils.GetArgumentsFromStack(context);

            if (nameVal.Type != ValueType.Undefined && name.Length != 0)
            {
                var res = obj.ToObject().IGet(context, name);
                res.AddRecallCode(res =>
                {
                    if (res.Value.IsCallable())
                        return res.Value.ToFunction().IConstruct(context, res.Value.ToFunction(), args);
                    else
                        return ESCallable.Throw(context.ConstrutError("TypeError"));
                });
                context.EnqueueResultCallback(res);
            }
            else
            {
                if (!obj.IsCallable())
                    context.EnqueueResultCallback(ESCallable.Throw(context.ConstrutError("TypeError")));
                else
                    context.EnqueueResultCallback(obj.ToFunction().IConstruct(context, obj.ToFunction(), args));
            }
        }
        public static void DoNewObject(ExecutionContext context)
        {
            var name = context.Pop().ToString();
            var args = FunctionUtils.GetArgumentsFromStack(context);
            var func = context.GetValueOnChain(name);
            if (func.IsCallable())
            {
                context.EnqueueResultCallback(func.ToFunction().IConstruct(context, func.ToFunction(), args));
            }
            else
                context.EnqueueResultCallback(ESCallable.Throw(context.ConstrutError("TypeError")));
        }
        public static void DoInitObject(ExecutionContext context)
        {
            var nArgs = context.Pop().ToInteger();
            var obj = new ESObject(context.Avm);
            for (int i = 0; i < nArgs; ++i)
            {
                var vi = context.Pop();
                var ni = context.Pop().ToString();
                obj.IPut(context, ni, vi);
            }

            context.Push(Value.FromObject(obj));
        }
        public static void DoInitArray(ExecutionContext context)
        {
            var args = FunctionUtils.GetArgumentsFromStack(context);
            context.Push(Value.FromArray(args, context.Avm));
        }

        public static void DoDefineLocal(ExecutionContext context)
        {
            var value = context.Pop();
            var varName = context.Pop().ToString();
            context.SetValueOnLocal(varName, value);
        }
        public static void DoDefineLocal2(ExecutionContext context)
        {
            var varName = context.Pop().ToString();
            if (context.HasValueOnLocal(varName))
                return;
            else
                context.SetValueOnLocal(varName, Value.Undefined());
        }
        public static void DoDelete2(ExecutionContext context)
        {
            var property = context.Pop().ToString();
            context.DeleteValueOnChain(property);
        }

        public static void DoEnumerate2(ExecutionContext context)
        {
            var obj = context.Pop().ToObject();
            context.Push(Value.FromObject(null));
            // Not sure if this is correct
            foreach (var slot in obj.GetAllProperties())
            {
                context.Push(Value.FromString(slot));
            }
        }

        public static void DoGetMember(ExecutionContext context)
        {
            var smem = context.Pop().ToString();
            var obj = context.Pop().ToObject();
            // arrays stay the same
            context.EnqueueResultCallback(obj.IGet(context, smem));
        }
        public static void DoSetMember(ExecutionContext context)
        {
            //pop the value
            var valueVal = context.Pop();
            //pop the member name
            var memberName = context.Pop().ToString();
            //pop the object
            var obj = context.Pop().ToObject();
            if (obj is null)
                throw new InvalidOperationException();
            else
                obj.IPut(context, memberName, valueVal);
        }
        public static void DoDelete(ExecutionContext context)
        {
            var property = context.Pop().ToString();
            var target = context.Pop();// TODO wtf? context.GetTarget(context.Pop().ToString());
            target.ToObject().IDeleteValue(context, property);
        }

        public static void DoCallFunction(ExecutionContext context)
        {
            var funcName = context.Pop().ToString();
            var args = FunctionUtils.GetArgumentsFromStack(context);
            var ret = FunctionUtils.TryExecuteFunction(funcName, args, context);
            context.EnqueueResultCallback(ret);

        }
        public static void DoCallMethod(ExecutionContext context)
        {
            var funcNameVal = context.Pop();
            var funcName = funcNameVal.ToString();
            ESCallable.Result ret;
            // If funcname is defined we need get the function from an object
            if (!funcNameVal.IsUndefined() && funcName.Length > 0)
            {
                var obj = context.Pop().ToObject();
                var args = FunctionUtils.GetArgumentsFromStack(context);
                ret = FunctionUtils.TryExecuteFunction(funcName, args, context, obj);
            }
            // Else the function is on the stack
            else
            {
                var funcVal = context.Pop();
                var args = FunctionUtils.GetArgumentsFromStack(context);
                ret = FunctionUtils.TryExecuteFunction(funcVal, args, context);
            }
            context.EnqueueResultCallback(ret);
        }
        public static void DoGetNamedMember(ExecutionContext context, int cid)
        {
            var member = context.ResolveConstant(cid).ToString();

            //pop the object
            var objectVal = context.Pop();
            var obj = objectVal.ToObject();

            if (obj != null)
                context.EnqueueResultCallback(obj.IGet(context, member));
            else
                context.Push(Value.Undefined());
        }
        public static void DoCallNamedFunc(ExecutionContext context, int cid)
        {
            var funcName = context.ResolveConstant(cid).ToString();
            var args = FunctionUtils.GetArgumentsFromStack(context);

            var ret = FunctionUtils.TryExecuteFunction(funcName, args, context);
            context.EnqueueResultCallback(ret);
        }
        public static void DoCallNamedMethod(ExecutionContext context, int cid, bool pop = false)
        {
            var funcName = context.ResolveConstant(cid).ToString();
            var obj = context.Pop().ToObject();
            var args = FunctionUtils.GetArgumentsFromStack(context);

            var ret0 = FunctionUtils.TryExecuteFunction(funcName, args, context, obj);
            context.EnqueueResultCallback(ret0);

            if (!pop)
            {
                throw new NotImplementedException("need check");
                var ret = FunctionUtils.TryExecuteFunction(funcName, args, context, obj);
                ret.AddRecallCode((ret2) =>
                {
                    var result = ret2.Value;
                    var varName = context.Pop();
                    context.SetValueOnLocal(varName.ToString(), result);
                    return null; // push nothing back
                });
                context.EnqueueResultCallback(ret);
            }
        }

        public static void DoGetStringVar(ExecutionContext context, RawInstruction inst)
        {
            var memberName = inst.Parameters[0].String;
            // check if this a special object, like _root, _parent etc.
            // this is automatically done by the built-in variables in the global object.
            var result = context.GetValueOnChain(memberName);
            if (result == null)
                throw new InvalidOperationException();
            context.Push(result);
        }
        public static void DoGetStringMember(ExecutionContext context, RawInstruction inst)
        {
            // pop member name???
            var memberName = inst.Parameters[0].String;
            //pop the object
            var objectVal = context.Pop();
            context.EnqueueResultCallback(objectVal.ToObject().IGet(context, memberName));
        }

        public static bool Execute(ExecutionContext context, RawInstruction inst)
        {
            switch (inst.Type)
            {

                case InstructionType.GetMember:
                    DoGetMember(context);
                    break;
                case InstructionType.SetMember:
                    DoSetMember(context);
                    break;
                case InstructionType.EA_ZeroVar:
                    context.This.IPut(context, context.Pop().ToString(), Value.FromInteger(0));
                    break;
                case InstructionType.Delete:
                    DoDelete(context);
                    break;


                case InstructionType.CallFunction:
                case InstructionType.EA_CallFuncPop:
                    DoCallFunction(context);
                    break;
                case InstructionType.EA_CallFunc:
                    return false;

                case InstructionType.CallMethod:
                // Since the execution (in original implementation)
                // is precisely the same as CallMethod, omit it
                // TODO Don't know if the word pop means discard the return value
                case InstructionType.EA_CallMethodPop:
                case InstructionType.EA_CallMethod:
                    DoCallMethod(context);
                    break;

                case InstructionType.EA_GetNamedMember:
                    DoGetNamedMember(context, inst.Parameters[0].Integer);
                    break;
                case InstructionType.EA_CallNamedFuncPop:
                case InstructionType.EA_CallNamedFunc:
                    DoCallNamedFunc(context, inst.Parameters[0].Integer);
                    break;
                case InstructionType.EA_CallNamedMethodPop:
                    DoCallNamedMethod(context, inst.Parameters[0].Integer, pop: true);
                    break;
                case InstructionType.EA_CallNamedMethod:
                    DoGetNamedMember(context, inst.Parameters[0].Integer); // TODO need check
                    break;


                case InstructionType.EA_GetStringVar:
                    DoGetStringVar(context, inst);
                    break;
                case InstructionType.EA_GetStringMember:
                    DoGetStringMember(context, inst);
                    break;
                case InstructionType.EA_SetStringVar:
                    context.This.IPut(context, context.Pop().ToString(), Value.FromRaw(inst.Parameters[0]));
                    break;
                case InstructionType.EA_SetStringMember:
                    var memberVal = context.Pop().ToString();
                    var objectVal = context.Pop().ToObject();
                    objectVal.IPut(context, memberVal, Value.FromRaw(inst.Parameters[0]));
                    break;

                case InstructionType.TypeOf:
                    context.ExecUnaryOprOnStack(TypeOf);
                    break;
                case InstructionType.CastOp:
                    DoCastOp(context);
                    break;
                case InstructionType.ImplementsOp:
                    return false; // TODO
                case InstructionType.Extends:
                    DoExtends(context);
                    break;
                case InstructionType.InstanceOf:
                    DoInstanceOf(context);
                    break;


                case InstructionType.NewMethod:
                    DoNewMethod(context);
                    break;
                case InstructionType.NewObject:
                    DoNewObject(context);
                    break;
                case InstructionType.InitObject:
                    DoInitObject(context);
                    break;
                case InstructionType.InitArray:
                    DoInitArray(context);
                    break;


                case InstructionType.DefineLocal:
                    DoDefineLocal(context);
                    break;
                case InstructionType.Var:
                    DoDefineLocal2(context);
                    break;
                case InstructionType.Delete2:
                    DoDelete2(context);
                    break;


                case InstructionType.Enumerate:
                    return false; // TODO
                case InstructionType.Enumerate2:
                    DoEnumerate2(context);
                    break;

                case InstructionType.EA_PushThis:
                    return false;
                case InstructionType.EA_PushGlobal:
                    return false;
                case InstructionType.EA_PushThisVar:
                    context.Push(Value.FromObject(context.This));
                    break;
                case InstructionType.EA_PushGlobalVar:
                    context.Push(Value.FromObject(context.Global));
                    break;
                case InstructionType.EA_PushValueOfVar:
                    var cid = context.ResolveConstant(inst.Parameters[0].Integer);
                    var cstr = cid.ToString();
                    context.Push(context.HasParameter(cstr) ? context.GetParameter(cstr) : context.GetValueOnChain(cstr));
                    break;

                default:
                    return false;
            }
            return true;
        }
    }
}