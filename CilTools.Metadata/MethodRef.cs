﻿/* CIL Tools 
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata.Ecma335;
using System.Diagnostics;
using CilTools.BytecodeAnalysis;
using CilTools.Reflection;


namespace CilTools.Metadata
{
    class MethodRef : CustomMethod
    {
        MetadataAssembly assembly;
        MemberReferenceHandle mrefh;
        MemberReference mref;
        Signature sig;
        CustomMethod impl;
        Type decltype;

        internal MethodRef(MemberReference m, MemberReferenceHandle mh, MetadataAssembly owner)
        {
            Debug.Assert(m.GetKind() == MemberReferenceKind.Method, "MemberReference passed to ExternalMethod ctor should be a method");

            this.assembly = owner;
            this.mref = m;
            this.mrefh = mh;

            byte[] sigbytes = assembly.MetadataReader.GetBlobBytes(mref.Signature);

            try
            {
                this.sig = new Signature(sigbytes, this.assembly,this);
            }
            catch (NotSupportedException) { }

            //init declaring type
            EntityHandle eh = mref.Parent;

            if (!eh.IsNil && eh.Kind == HandleKind.TypeReference)
            {
                this.decltype = new TypeRef(
                    assembly.MetadataReader.GetTypeReference((TypeReferenceHandle)eh), (TypeReferenceHandle)eh, this.assembly
                    );
            }
            else if (!eh.IsNil && eh.Kind == HandleKind.TypeSpecification)
            {
                //TypeSpec is either complex type (array etc.) or generic instantiation

                TypeSpecification ts = assembly.MetadataReader.GetTypeSpecification(
                    (TypeSpecificationHandle)eh
                    );

                TypeSpec encoded = TypeSpec.ReadFromArray(assembly.MetadataReader.GetBlobBytes(ts.Signature),
                    this.assembly,
                    this);

                if (encoded != null) this.decltype = encoded.Type;
                else this.decltype = UnknownType.Value;
            }
            else if (!eh.IsNil && eh.Kind == HandleKind.MethodDefinition)
            {
                MethodDefinition mdef = assembly.MetadataReader.GetMethodDefinition((MethodDefinitionHandle)eh);
                TypeDefinitionHandle tdefh = mdef.GetDeclaringType();

                if (!tdefh.IsNil)
                {
                    this.decltype = new TypeDef(
                        assembly.MetadataReader.GetTypeDefinition(tdefh), tdefh, this.assembly
                        );
                }
                else this.decltype = UnknownType.Value;
            }
            else this.decltype = UnknownType.Value;
        }

        void LoadImpl()
        {
            //loads actual implementation method referenced by this instance

            if (this.impl != null) return;//already loaded
            if(this.assembly.AssemblyReader == null) return;

            Type et = this.DeclaringType;

            if (et == null) return;

            Type t = this.assembly.AssemblyReader.LoadType(et);

            if (t == null) return;

            MemberInfo[] members = t.GetMember(this.Name,
                BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance
                );

            //if there's only one method, pick it
            if(members.Length == 1 && members[0] is CustomMethod) 
            {
                this.impl = (CustomMethod)members[0];
                return; 
            }

            //if there are multiple methods with the same name, match by signature
            ParameterInfo[] pars_match = this.GetParameters_Sig();
            bool isstatic_match = false;
            int genargs_match = 0;

            if (this.sig != null)
            {
                isstatic_match = !this.sig.HasThis;
                genargs_match = this.sig.GenericArgsCount;
            }

            bool match;

            for (int i = 0; i < members.Length; i++)
            {
                if (!(members[i] is CustomMethod)) continue;

                CustomMethod m = (CustomMethod)members[i];
                ParameterInfo[] pars_i = m.GetParameters();

                if (m.IsStatic != isstatic_match) continue;

                if (pars_i.Length != pars_match.Length) continue;

                //compare generic args count
                Type[] ga = m.GetGenericArguments();
                int genargs = 0;

                if (ga != null) genargs = ga.Length;
                else genargs = 0;

                if (genargs != genargs_match) continue;

                //compare parameter types
                match = true;

                for (int j = 0; j < pars_i.Length; j++)
                {
                    string s1 = "";
                    string s2 = "";
                    Type pt = pars_i[j].ParameterType;
                    if (pt!=null) s1 = pt.Name;
                    pt = pars_match[j].ParameterType;
                    if (pt!=null) s2 = pt.Name;

                    if (!String.Equals(s1, s2, StringComparison.InvariantCulture))
                    {
                        match = false; 
                        break;
                    }
                }

                if (match)
                {
                    this.impl = m;
                    return;
                }
            }//end for
        }

        /// <summary>
        /// Gets the method's returned type
        /// </summary>
        public override Type ReturnType
        {
            get
            {
                if (String.Equals(this.Name, ".ctor", StringComparison.InvariantCulture)) return null;

                if (this.sig == null) return UnknownType.Value;
                else return this.sig.ReturnType.Type;
            }
        }

        /// <inheritdoc/>
        public override ITokenResolver TokenResolver
        {
            get 
            {
                if (this.impl != null) return this.impl.TokenResolver;
                else return this.assembly; 
            }
        }

        /// <inheritdoc/>
        public override byte[] GetBytecode()
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetBytecode();
            else throw new CilParserException("Failed to load method implementation");
        }

        /// <inheritdoc/>
        public override int MaxStackSize
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return this.impl.MaxStackSize;
                else return 0;
            }
        }

        /// <inheritdoc/>
        public override bool MaxStackSizeSpecified
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return this.impl.MaxStackSizeSpecified;
                else return false;
            }
        }

        /// <inheritdoc/>
        public override byte[] GetLocalVarSignature()
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetLocalVarSignature();
            else return new byte[] { };
        }

        /// <inheritdoc/>
        public override ExceptionBlock[] GetExceptionBlocks()
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetExceptionBlocks();
            else return new ExceptionBlock[] { };
        }

        /// <inheritdoc/>
        public override MethodAttributes Attributes
        {
            get
            {
                try
                {
                    this.LoadImpl();
                }
                catch (TypeLoadException) { }

                MethodAttributes ret = (MethodAttributes)0;

                if (this.impl != null)
                {
                    ret = this.impl.Attributes;
                }
                else if (this.sig != null)
                {
                    //if failed to load impl, we can at least get static/instance attribute from signature
                    if (!this.sig.HasThis) ret |= MethodAttributes.Static;
                }

                return ret;
            }
        }

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            MethodImplAttributes ret = (MethodImplAttributes)0;

            if (this.impl != null)
            {
                ret = this.impl.GetMethodImplementationFlags();
            }

            return ret;
        }

        ParameterInfo[] GetParameters_Sig()
        {
            if (this.sig == null) return new ParameterInfo[0];

            ParameterInfo[] pars = new ParameterInfo[this.sig.ParamsCount];

            for (int i = 0; i < pars.Length; i++)
            {
                pars[i] = new ParameterSpec(this.sig.GetParamType(i), i, this);
            }

            return pars;
        }

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters()
        {
            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            if (this.impl != null)
            {
                //reading parameters from impl gives more data (names, default values)
                return this.impl.GetParameters();
            }
            else
            {
                //even if failed to load impl, we can read parameters from signature
                return this.GetParameters_Sig();
            }
        }

        /// <inheritdoc/>
        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters,
            System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("Cannot invoke methods on type loaded into reflection-only context");
        }

        /// <inheritdoc/>
        public override RuntimeMethodHandle MethodHandle
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override Type DeclaringType
        {
            get 
            {
                return this.decltype;
            }
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetCustomAttributes(attributeType,inherit);
            else return new object[] { };
        }

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit)
        {
            this.LoadImpl();

            if (this.impl != null) return this.impl.GetCustomAttributes(inherit);
            else return new object[] { };
        }

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (this.impl != null) return this.impl.IsDefined(attributeType, inherit);
            else return false;
        }

        /// <inheritdoc/>
        public override MemberTypes MemberType
        {
            get { return MemberTypes.Method; }
        }

        /// <inheritdoc/>
        public override string Name
        {
            get
            {
                return assembly.MetadataReader.GetString(mref.Name);
            }
        }

        /// <inheritdoc/>
        public override Type ReflectedType
        {
            get { throw new NotImplementedException(); }
        }

        /// <inheritdoc/>
        public override int MetadataToken
        {
            get
            {
                return assembly.MetadataReader.GetToken(this.mrefh);
            }
        }

        /// <inheritdoc/>
        public override bool InitLocals
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return this.impl.InitLocals;
                else return false;
            }
        }

        /// <inheritdoc/>
        public override bool InitLocalsSpecified
        {
            get
            {
                this.LoadImpl();

                if (this.impl != null) return this.impl.InitLocalsSpecified;
                return false;
            }
        }

        public override bool IsGenericMethod
        {
            get
            {
                try
                {
                    this.LoadImpl();
                }
                catch (TypeLoadException) { }

                if (this.impl != null) return this.impl.IsGenericMethod;
                else return false;
            }
        }

        public override Type[] GetGenericArguments()
        {
            try
            {
                this.LoadImpl();
            }
            catch (TypeLoadException) { }

            if (this.impl != null) return this.impl.GetGenericArguments();
            else return new Type[0];
        }
    }
}

