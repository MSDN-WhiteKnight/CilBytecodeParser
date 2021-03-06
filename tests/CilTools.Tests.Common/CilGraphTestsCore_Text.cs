﻿/* CIL Tools
 * Copyright (c) 2020,  MSDN.WhiteKnight (https://github.com/MSDN-WhiteKnight) 
 * License: BSD 2.0 */
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CilTools.BytecodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CilTools.Tests.Common
{
    public class CilGraphTestsCore_Text
    {
        public static void Test_CilGraph_ToString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test that ToString returns signature
            string str = graph.ToString();
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("public") });
            AssertThat.IsMatch(str, new MatchElement[] { new Literal(".method"), MatchElement.Any, new Literal("static") });

            AssertThat.IsMatch(str, new MatchElement[] { 
                new Literal(".method"), MatchElement.Any, new Literal("void"), MatchElement.Any, 
                new Literal("PrintHelloWorld"), MatchElement.Any, 
                new Literal("cil"), MatchElement.Any, new Literal("managed"), MatchElement.Any
            });

            Assert.IsFalse(str.Contains("call"), "The result of CilGraph.ToString should not contain instructions");
        }
    
        public static void Test_CilGraph_EmptyString(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();

            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, new Literal("ldstr"), MatchElement.Any, new Literal("\"\""), MatchElement.Any 
            });
        }
        
        public static void Test_CilGraph_OptionalParams(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            //Test correct empty string output
            string str = graph.ToText();
            
            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, new Literal(".method"),
                MatchElement.Any, new Literal("[opt]"),MatchElement.Any, new Literal("string"),
                MatchElement.Any, new Literal("[opt]"),MatchElement.Any, new Literal("int32"),
                MatchElement.Any, new Literal(".param"), MatchElement.Any, new Literal("[1]"),
                MatchElement.Any, new Literal("\"\""), 
                MatchElement.Any, new Literal(".param"), MatchElement.Any, new Literal("[2]"),
                MatchElement.Any, new Literal("int32(0)"), 
                MatchElement.Any 
            });
        }

        public static void Test_CilGraph_Locals(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);

            StringBuilder sb = new StringBuilder(100);
            StringWriter wr = new StringWriter(sb);
            graph.PrintHeader(wr);
            wr.Flush();
            string str = sb.ToString();

            AssertThat.IsMatch(str, new MatchElement[] { 
                MatchElement.Any, new Literal(".locals"),MatchElement.Any, new Literal("("),
                MatchElement.Any, new Literal("MyPoint"),MatchElement.Any, new Literal(")"),
                MatchElement.Any 
            });
        }

        public static void Test_CilGraph_ImplRuntime(MethodBase mi)
        {
            CilGraph graph = CilGraph.Create(mi);
            string str = graph.ToString();

            //.method   public hidebysig newslot virtual instance !0 Invoke() runtime managed

            AssertThat.IsMatch(str, new MatchElement[] {
                MatchElement.Any, new Literal(".method"),MatchElement.Any,
                new Literal("!"),MatchElement.Any,
                new Literal("Invoke"),MatchElement.Any,
                new Literal("("), MatchElement.Any,new Literal(")"),MatchElement.Any
                ,new Literal("runtime"),MatchElement.Any
                ,new Literal("managed"),MatchElement.Any
            });
        }
    }
}
