﻿/* CilBytecodeParser library 
 * Copyright (c) 2019,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace CilBytecodeParser
{
    /// <summary>
    /// Represents CIL instruction, a main structural element of the method body which consists of operation code and operand.
    /// </summary>
    /// <remarks>To retreive a collection of CIL instructions for the specified method, use methods of <see cref="CilReader"/> class.</remarks>
    public class CilInstruction
    {
        static bool ReferencesMethodToken(OpCode op)
        {
            return (op.Equals(OpCodes.Call) || op.Equals(OpCodes.Callvirt) || op.Equals(OpCodes.Newobj)
                || op.Equals(OpCodes.Ldftn));
        }

        static bool ReferencesFieldToken(OpCode op)
        {
            return (op.Equals(OpCodes.Stfld) || op.Equals(OpCodes.Stsfld) ||
                    op.Equals(OpCodes.Ldsfld) || op.Equals(OpCodes.Ldfld));
        }

        static bool ReferencesTypeToken(OpCode op)
        {
            return (op.Equals(OpCodes.Newarr) || op.Equals(OpCodes.Box) || op.Equals(OpCodes.Isinst)
                || op.Equals(OpCodes.Castclass) || op.Equals(OpCodes.Initobj)
                || op.Equals(OpCodes.Unbox) || op.Equals(OpCodes.Unbox_Any));
        }

        static bool ReferencesLocal(OpCode op)
        {
            return (op.Equals(OpCodes.Ldloc) || op.Equals(OpCodes.Ldloca) || op.Equals(OpCodes.Ldloc_S)
                || op.Equals(OpCodes.Ldloca_S) || op.Equals(OpCodes.Stloc) || op.Equals(OpCodes.Stloc_S));
        }

        /// <summary>
        /// Raised when error occurs in one of the methods in this class
        /// </summary>
        public static event EventHandler<CilErrorEventArgs> Error;

        /// <summary>
        /// Raises a 'Error' event
        /// </summary>
        /// <param name="sender">Object that caused this event</param>
        /// <param name="e">Event arguments</param>
        protected static void OnError(object sender, CilErrorEventArgs e)
        {
            EventHandler<CilErrorEventArgs> handler = Error;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        /// <summary>
        /// A reference to a method which this instruction belongs to
        /// </summary>
        protected MethodBase _Method;

        /// <summary>
        /// Opcode of this instruction
        /// </summary>
        protected OpCode _OpCode;

        /// <summary>
        /// Operand object of this instruction, if applicable
        /// </summary>
        protected object _Operand;

        /// <summary>
        /// Size, in bytes, of this instruction's operand
        /// </summary>
        protected uint _OperandSize;

        /// <summary>
        /// Byte offset of this instruction from the beginning of the method body
        /// </summary>
        protected uint _ByteOffset;

        /// <summary>
        /// Ordinal number of the place this instruction takes in method body, starting from one.
        /// </summary>
        protected uint _OrdinalNumber;

        /// <summary>
        /// Gets a reference to a method which this instruction belongs to
        /// </summary>
        public MethodBase Method { get { return this._Method; } }

        /// <summary>
        /// Gets the operation code (opcode) of this instruction
        /// </summary>
        public OpCode OpCode { get { return this._OpCode; } }

        /// <summary>
        /// Gets the operand object of this instruction
        /// </summary>
        public object Operand { get { return this._Operand; } }

        /// <summary>
        /// Gets the size, in bytes, of this instruction's operand
        /// </summary>
        public uint OperandSize { get { return this._OperandSize; } }

        /// <summary>
        /// Gets a byte offset of this instruction from the beginning of the method body
        /// </summary>
        public uint ByteOffset { get { return this._ByteOffset; } }

        /// <summary>
        /// Gets ordinal number of the place this instruction takes in method body, starting from one.
        /// </summary>
        public uint OrdinalNumber { get { return this._OrdinalNumber; } }
        
        /// <summary>
        /// Gets opcode of this instruction as a numerical value
        /// </summary>
        public short Code { get { return this.OpCode.Value; } }

        /// <summary>
        /// Gets a name of this instruction
        /// </summary>
        public string Name { get { return this.OpCode.Name; } }

        /// <summary>
        /// Gets total size, in bytes, that this instruction occupies in the method body
        /// </summary>
        public uint TotalSize
        {
            get
            {
                if (this.OpCode != null) return (uint)this.OpCode.Size + OperandSize;
                else return 0;
            }
        }

        /// <summary>
        /// Creates a new CilInstruction object initialized with specified field values (infrastructure)
        /// </summary>
        /// <param name="opc">Opcode</param>
        /// <param name="operand">Operand object</param>
        /// <param name="opsize">Operand size</param>
        /// <param name="byteoffset">Byte offset</param>
        /// <param name="ordinalnum">Ordinal number</param>
        /// <param name="mb">Owning method</param>
        /// <remarks>Do not use this constructor directly. To retreive a collection of CIL instructions for the specified method, use methods of <see cref="CilReader"/> class instead.</remarks>
        public CilInstruction(
            OpCode opc, object operand=null, uint opsize=0, uint byteoffset=0, uint ordinalnum=0, MethodBase mb=null
            )
        {
            this._OpCode = opc;
            this._Operand = operand;
            this._OperandSize = opsize;
            this._ByteOffset = byteoffset;
            this._OrdinalNumber = ordinalnum;
            this._Method = mb;
        }
        
        /// <summary>
        /// Creates new CilInstruction object that represents an empty instruction
        /// </summary>
        /// <param name="mb">Owning method</param>
        /// <returns>Empty CilInstruction object</returns>
        public static CilInstruction CreateEmptyInstruction(MethodBase mb)
        {
            CilInstruction instr = new CilInstruction(OpCodes.Nop,null,0,0,0,mb);            
            return instr;
        }
        
        /// <summary>
        /// Gets this instruction's operand type, or null if there's no operand
        /// </summary>
        public Type OperandType
        {
            get
            {
                if (Operand == null) return null;
                else return Operand.GetType();
            }
        }

        /// <summary>
        /// Gets a member (type, field or method) referenced by this instruction, if applicable
        /// </summary>
        public MemberInfo ReferencedMember
        {
            get
            {

                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                try
                {
                    if (ReferencesMethodToken(this.OpCode))
                    {
                        MethodBase method = Method.Module.ResolveMethod((int)Operand);
                        return method;
                    }
                    else if (ReferencesFieldToken(this.OpCode))
                    {

                        FieldInfo fi = Method.Module.ResolveField((int)Operand);
                        return fi;
                    }
                    else if (ReferencesTypeToken(this.OpCode))
                    {

                        Type t = Method.Module.ResolveType((int)Operand);
                        return t;
                    }
                    else return null;
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve member token.";
                    OnError(this, new CilErrorEventArgs(ex, error));   
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a type referenced by this instruction, if applicable
        /// </summary>
        public Type ReferencedType
        {
            get
            {
                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                try
                {
                    if (ReferencesTypeToken(this.OpCode))
                    {
                        Type t = Method.Module.ResolveType((int)Operand);
                        return t;
                    }
                    else return null;
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve type token.";
                    OnError(this, new CilErrorEventArgs(ex, error));  
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets a string literal referenced by this instruction, if applicable
        /// </summary>
        public string ReferencedString
        {
            get
            {
                if (this.Method == null) return null;
                if (this.Operand == null) return null;

                try
                {
                    if (this.OpCode.Equals(OpCodes.Ldstr))
                    {
                        string s = Method.Module.ResolveString((int)Operand);
                        return s;
                    }
                    else return null;
                }
                catch (Exception ex)
                {
                    string error = "Exception occured when trying to resolve string token.";
                    OnError(this, new CilErrorEventArgs(ex, error));  
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns a text representation of this instruction as a line of CIL code
        /// </summary>
        /// <returns>String containing text representation of this instruction</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();                        

            sb.Append(this.Name.PadRight(10));            

            if (Operand != null)
            {
                
                if (ReferencesMethodToken(this.OpCode) && this.Method != null)
                {           
                    //method                         

                    try
                    {
                        int token = (int)Operand;
                        MethodBase called_method;

                        called_method = Method.Module.ResolveMethod(token);
                        Type t = called_method.DeclaringType;
                        ParameterInfo[] pars = called_method.GetParameters();

                        MethodInfo mi = called_method as MethodInfo;
                        string rt = "";
                        if (mi != null) rt = " " + CilAnalysis.GetTypeName(mi.ReturnType);

                        if (!called_method.IsStatic) sb.Append(" instance");
                                                
                        sb.Append(rt);
                        sb.Append(' ');
                        
                        sb.Append(CilAnalysis.GetTypeNameInternal(t));
                        sb.Append("::");
                        sb.Append(called_method.Name);
                        sb.Append('(');

                        for (int i = 0; i < pars.Length; i++)
                        {
                            if (i >= 1) sb.Append(", ");
                            sb.Append(CilAnalysis.GetTypeName(pars[i].ParameterType));                            
                        }

                        sb.Append(')');
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve method token.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (ReferencesFieldToken(this.OpCode) && this.Method != null)
                {
                    //field                    

                    try
                    {
                        int token = (int)Operand;
                        FieldInfo fi;

                        fi = Method.Module.ResolveField(token);
                        Type t = fi.DeclaringType;
                                           
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeName(fi.FieldType));
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeNameInternal(t));
                        sb.Append("::");
                        sb.Append(fi.Name);
                        
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve field token.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (ReferencesTypeToken(this.OpCode) && this.Method != null)
                {
                    //type                    

                    try
                    {
                        int token = (int)Operand;
                        Type t;

                        t = Method.Module.ResolveType(token);  
                        
                        sb.Append(' ');
                        sb.Append(CilAnalysis.GetTypeNameInternal(t));
                        
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve type token.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (OpCode.Equals(OpCodes.Ldstr) && this.Method != null)
                {
                    //string literal

                    try
                    {
                        int token = (int)Operand;
                        string s = "";

                        s = Method.Module.ResolveString(token);

                        sb.Append(" \"");
                        sb.Append(s);
                        sb.Append('"');                        

                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve string.";
                        OnError(this, new CilErrorEventArgs(ex, error));  
                    }
                }
                else if (OpCode.Equals(OpCodes.Ldtoken) && this.Method != null)
                {
                    //metadata token
                    int token = (int)Operand;                    

                    try
                    {                        
                        MemberInfo mi;

                        mi = Method.Module.ResolveMember(token);
                        sb.Append(' ');
                        if (mi is Type) sb.Append(CilAnalysis.GetTypeNameInternal((Type)mi));
                        else sb.Append(mi.Name);
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to resolve token.";
                        OnError(this, new CilErrorEventArgs(ex, error));
                        sb.Append(" 0x"+token.ToString("X"));
                    }
                }
                else if (ReferencesLocal(this.OpCode))
                {
                    //local variable   

                    try
                    {  
                        sb.Append(" V_" + this.Operand.ToString());
                    }
                    catch (Exception ex)
                    {
                        string error = "Exception occured when trying to process local variable.";
                        OnError(this, new CilErrorEventArgs(ex, error));                        
                    }
                }
                else
                {
                    sb.Append(' ');
                    sb.Append(Operand.ToString());
                }
                
            }

            return sb.ToString();
        }

        /// <summary>
        /// Emits CIL code for this instruction into the specified IlGenerator
        /// </summary>
        /// <param name="ilg">Target IlGenerator object</param>
        public void EmitTo(ILGenerator ilg)
        {
            if (this.OpCode == OpCodes.Call || this.OpCode == OpCodes.Callvirt)
            {
                ilg.EmitCall(this.OpCode, (MethodInfo)this.ReferencedMember, null);
            }
            else if (this.Operand == null)
            {
                ilg.Emit(this.OpCode);
            }
            else if (this.OperandType == typeof(float))
            {
                ilg.Emit(this.OpCode, (float)this.Operand);
            }
            else if (this.OperandType == typeof(double))
            {
                ilg.Emit(this.OpCode, (double)this.Operand);
            }
            else if (this.OperandType == typeof(long))
            {
                ilg.Emit(this.OpCode, (long)this.Operand);
            }
            else if (this.OperandType == typeof(short))
            {
                ilg.Emit(this.OpCode, (short)this.Operand);
            }
            else if (this.OperandType == typeof(int))
            {
                if (this.OpCode.Equals(OpCodes.Ldstr))
                {
                    ilg.Emit(this.OpCode, this.ReferencedString);
                }
                else if (ReferencesFieldToken(this.OpCode))
                {
                    ilg.Emit(this.OpCode, (FieldInfo)this.ReferencedMember);
                }
                else if (ReferencesTypeToken(this.OpCode))
                {
                    ilg.Emit(this.OpCode, (Type)this.ReferencedMember);
                }
                else if (ReferencesMethodToken(this.OpCode) && (this.ReferencedMember as ConstructorInfo) != null)
                {
                    ilg.Emit(this.OpCode, (ConstructorInfo)this.ReferencedMember);
                }
                else throw new NotSupportedException("OpCode not supported: " + this.OpCode.ToString());
            }
            else if (ReferencesLocal(this.OpCode) && this.OperandType == typeof(sbyte))
            {
                ilg.Emit(this.OpCode, (sbyte)this.Operand);
            }
            else throw new NotSupportedException("OperandType not supported: " + this.OperandType.ToString());
        }

        //*** TEXT PARSER ***

        static Dictionary<string, OpCode> opcodes = new Dictionary<string, OpCode>();

        static void LoadOpCodes()
        {
            FieldInfo[] fields = typeof(OpCodes).GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(OpCode))
                {
                    OpCode opcode = (OpCode)fields[i].GetValue(null);                    
                    opcodes[opcode.Name] = opcode;
                }
            }
        }

        static OpCode FindOpCode(string name)
        {
            if (!opcodes.ContainsKey(name))
            {
                throw new NotSupportedException("Unknown opcode: "+name);
            }
            return opcodes[name];
        }

        static CilInstruction()
        {
            LoadOpCodes();
        }

        static uint GetOperandSize(OpCode opcode)
        {
            uint size=0;
            switch (opcode.OperandType)
            {
                case System.Reflection.Emit.OperandType.InlineBrTarget: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineField: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineMethod: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineSig: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineTok: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineType: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineI: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineI8: size = 8; break;
                case System.Reflection.Emit.OperandType.InlineNone: size = 0; break;
                case System.Reflection.Emit.OperandType.InlineR: size = 8; break;
                case System.Reflection.Emit.OperandType.InlineString: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineSwitch: size = 4; break;
                case System.Reflection.Emit.OperandType.InlineVar: size = 2; break;
                case System.Reflection.Emit.OperandType.ShortInlineBrTarget: size = 1; break;
                case System.Reflection.Emit.OperandType.ShortInlineI: size = 1; break;
                case System.Reflection.Emit.OperandType.ShortInlineR: size = 4; break;
                case System.Reflection.Emit.OperandType.ShortInlineVar: size = 1; break;
                default:                    
                    throw new NotSupportedException("Unsupported operand type: " + opcode.OperandType.ToString());
            }
            return size;
        }

        /// <summary>
        /// Converts CIL instruction textual representation into the corresponding CilInstruction object
        /// </summary>
        /// <param name="str">The line of CIL code representing instruction</param>
        /// <returns>CilInstruction object for the specified string</returns>
        public static CilInstruction Parse(string str)
        {
            if (String.IsNullOrEmpty(str)) throw new ArgumentException("str parameter can't be null or empty string");

            CilInstruction res=null;

            List<string> tokens = new List<string>(10);
            StringBuilder curr_token = new StringBuilder(100);
            bool IsInComment = false;
            bool IsInLiteral = false;
            bool IsInToken = false;
            char c;
            char c_next;

            for(int i=0;i<str.Length;i++)
            {
                c = str[i];
                if(i+1 < str.Length)c_next = str[i+1];
                else c_next=(char)0;                

                if (!IsInToken && !IsInLiteral && !IsInComment &&
                    (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsSymbol(c))
                    && c != '/')
                {
                    //start new token
                    IsInToken = true;

                    if (c == '"') IsInLiteral = true;
                    else IsInLiteral = false;

                    curr_token = new StringBuilder(100);
                    curr_token.Append(c);
                    continue;
                }

                if (IsInToken && !IsInLiteral && Char.IsWhiteSpace(c))
                {
                    //end token
                    IsInToken = false;                    
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);
                    continue;
                }

                if (IsInToken && !IsInLiteral && 
                    (c==':' && c_next!=':')
                    )
                {
                    //end token
                    curr_token.Append(c);
                    IsInToken = false;
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);
                    continue;
                }

                if (IsInToken && !IsInLiteral && c == '/' && (c_next == '/' || c_next == '*'))
                {
                    //end token
                    IsInToken = false;                    
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);

                    //start comment
                    IsInComment = true;
                    continue;
                }

                if (IsInToken && IsInLiteral && c=='"' && str[i-1]!='\\')
                {
                    //end token
                    curr_token.Append(c);
                    IsInToken = false;
                    IsInLiteral = false;
                    tokens.Add(curr_token.ToString());
                    curr_token = new StringBuilder(100);
                    continue;
                }

                if (!IsInComment && !IsInToken && !IsInLiteral && c == '/' && (c_next == '/' || c_next == '*'))
                {
                    //start comment
                    IsInComment = true;
                    continue;
                }

                if (IsInComment && c == '/' && str[i - 1] == '*')
                {
                    //end comment
                    IsInComment = false;
                    continue;
                }

                if (IsInToken && !IsInLiteral && (Char.IsLetterOrDigit(c) || Char.IsPunctuation(c) || Char.IsSymbol(c)))
                {
                    //append new char to the token
                    curr_token.Append(c);
                }

                if (IsInToken && IsInLiteral && !(c=='"' && str[i-1]!='\\'))
                {
                    //append new char to the token
                    curr_token.Append(c);
                }
            }//end for

            if (IsInToken)
            {
                tokens.Add(curr_token.ToString());
            }
            
            if (tokens.Count == 0) return null;
            int args_start;

            string opname = tokens[0].Trim();
            args_start=1;
            if (opname[opname.Length - 1] == ':')
            {
                //skip label
                if (tokens.Count == 1) return null;
                opname = tokens[1].Trim();
                args_start=2;
            }

            string args = "";

            for(int j=args_start;j<tokens.Count;j++) args+=tokens[j];
            
            args = args.Trim();

            OpCode op = FindOpCode(opname);
            uint opsize = GetOperandSize(op);
            object operand = null;

            var numstyle = System.Globalization.NumberStyles.Integer;
            if (args.StartsWith("0x"))
            {
                numstyle = System.Globalization.NumberStyles.HexNumber;
                args = args.Substring(2);
            }

            var fmt = System.Globalization.CultureInfo.InvariantCulture;

            sbyte byteval;
            short shortval;
            float floatval;
            int intval;
            double doubleval;
            long longval;

            switch (opsize)
            {

                case 1: 
                    if(SByte.TryParse(args,numstyle,fmt, out byteval)) operand = (object)byteval;                     
                    break;

                case 2:
                    if (Int16.TryParse(args,numstyle,fmt, out shortval)) operand = (object)shortval; 
                    break;

                case 4:
                    if (op.OperandType == System.Reflection.Emit.OperandType.ShortInlineR)
                    {
                        if (Single.TryParse(args, out floatval)) operand = (object)floatval;
                    }
                    else
                    {
                        if (Int32.TryParse(args, numstyle, fmt, out intval))
                        {
                            operand = (object)intval;
                        }                        
                    }
                    break;

                case 8:
                    if (op.OperandType == System.Reflection.Emit.OperandType.InlineR)
                    {
                        if (Double.TryParse(args, out doubleval)) operand = (object)doubleval;
                    }
                    else
                    {
                        if (Int64.TryParse(args, numstyle, fmt, out longval)) operand = (object)longval;
                    }
                    break;
            }

            res = new CilInstruction(op, operand, opsize, 0, 0, null);                                   
            
            return res;
        }
    }
}