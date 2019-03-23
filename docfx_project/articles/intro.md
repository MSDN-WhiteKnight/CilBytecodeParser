# CIL Bytecode Parser

![logo](../images/il.png)

**License:** BSD 2.0  
**Requirements:** .NET Framework 3.5+  

CIL Bytecode Parser reads .NET methods' Common Intermediate Language (CIL) bytecode and converts it into high-level objects or textual CIL representation so they can be easily studied and programmatically processed.

*Usage*

Add reference to CilBytecodeParser.dll, import CilBytecodeParser namespace. Use <xref:CilBytecodeParser.CilReader.GetInstructions(System.Reflection.MethodBase)> to get the collection of instructions from method, <xref:CilBytecodeParser.CilAnalysis.GetGraph(System.Reflection.MethodBase)> to get a a graph that represents a flow of control between method's instructions, or <xref:CilBytecodeParser.CilAnalysis.MethodToText(System.Reflection.MethodBase)> when you need to output method's CIL code as text. <xref:CilBytecodeParser.Extensions> namespace provides an alternative syntax via extension methods.

*Example*

```
using System;
using System.Collections.Generic;
using CilBytecodeParser;
using CilBytecodeParser.Extensions;

namespace CilBytecodeParserTest
{
    class Program
    {
        public static void Hello()
        {
            int a = 1;
            int b = 2;
            Console.WriteLine("Hello, World");
            Console.WriteLine("{0} + {1} = {2}",a,b,a+b);
        }

        static void Main(string[] args)
        {
            IEnumerable<CilInstruction> instructions = typeof(Program).GetMethod("Hello").GetInstructions();

            foreach (CilInstruction instr in instructions)
            {
                Console.WriteLine(instr.ToString());
            }
            Console.ReadKey();
        }

    }
}

/* Output:

nop
ldc.i4.1
stloc.0
ldc.i4.2
stloc.1
ldstr "Hello, World"
call void [mscorlib]System.Console::WriteLine(string)
nop
ldstr "{0} + {1} = {2}"
ldloc.0
box [mscorlib]System.Int32
ldloc.1
box [mscorlib]System.Int32
ldloc.0
ldloc.1
add
box [mscorlib]System.Int32
call void [mscorlib]System.Console::WriteLine(string, System.Object, System.Object, System.Object)
nop
ret
*/
```

Copyright (c) 2019, MSDN.WhiteKnight