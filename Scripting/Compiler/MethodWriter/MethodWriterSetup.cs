using System;
using System.Diagnostics;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections.Generic;

namespace IronAHK.Scripting
{
    internal partial class MethodWriter
    {
        TypeBuilder Parent;
        CodeMemberMethod Member;
        ILGenerator Generator;

        MethodInfo SetEnv;
        MethodInfo GetEnv;

        MethodCollection Lookup;

        public bool IsEntryPoint = false;
        public MethodBuilder Method;

        int Depth = 0;

        Stack<LoopMetadata> Loops;
        Dictionary<string, LocalBuilder> Locals;

        public MethodWriter(TypeBuilder Parent, CodeMemberMethod Member, MethodCollection Lookup)
        {
            Loops = new Stack<LoopMetadata>();

            this.Parent = Parent;
            this.Member = Member;
            this.Lookup = Lookup;

            if(Member is CodeEntryPointMethod)
            {
                Method = Parent.DefineMethod("Main", MethodAttributes.Private | MethodAttributes.Static,
                                                          typeof(void), new Type[] { typeof(string[]) });
                IsEntryPoint = true;
            }
            else Method = Parent.DefineMethod(Member.Name, MethodAttributes.Static);

            Generator = Method.GetILGenerator();

            if(IsEntryPoint)
                GenerateEntryPointHeader();

            Type rusty = typeof(Rusty.Core);
            SetEnv = rusty.GetMethod("SetEnv");
            GetEnv = rusty.GetMethod("GetEnv");
            Locals = new Dictionary<string, LocalBuilder>();
        }

        void GenerateEntryPointHeader()
        {
            ConstructorInfo StatThreadConstructor = typeof(System.STAThreadAttribute).GetConstructor(Type.EmptyTypes);
            CustomAttributeBuilder Attribute = new CustomAttributeBuilder(StatThreadConstructor, new object[] {});
            Method.SetCustomAttribute(Attribute);

            //Assembly Winforms = Assembly.LoadWithPartialName("System.Windows.Forms");
            const string WinForms = "System.Windows.Forms";
            Assembly Winforms = Assembly.Load(WinForms + ", Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089");
            Type Application = Winforms.GetType(WinForms + ".Application");

            if (Application == null)
                return;

            MethodInfo Enable = Application.GetMethod("EnableVisualStyles");
            Generator.Emit(OpCodes.Call, Enable);
        }

        string ResolveName(CodeVariableReferenceExpression Var)
        {
            const string sep = ".";
            if (IsEntryPoint)
                return string.Concat(sep, Var.VariableName);
            else
                return string.Concat(Member.Name, sep, Var.VariableName);
        }

        [Conditional("DEBUG")]
        void Debug(string Message)
        {
            Console.Write(new string(' ', Depth));
            Console.WriteLine(Message);
        }
    }
}
