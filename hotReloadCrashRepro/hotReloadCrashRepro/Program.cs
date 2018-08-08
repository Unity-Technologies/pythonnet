using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace hotReloadCrashRepro
{
    class Program
    {
        static void Main(string[] args)
        {
             for(int i = 0; i < 2; ++i) {
                System.Console.WriteLine(string.Format("{0} {0} {0} {0} {0}",i));

                // Create the domain
                System.Console.WriteLine(string.Format("[Main] Creating the domain \"My Domain {0}\"",i));
                var domain = AppDomain.CreateDomain(string.Format("My Domain {0}",i));


                // Build the assembly only once
                // Commented to avoid loading the assembly in the program domain
                // Simply uncomment this block if you need to regenerate the assembly dll
                if (i == 0)
                {
                    System.Console.WriteLine("[Main] Building the assembly");
                    var theCompiledAssembly = BuildAssembly("D:\\projects\\pythonnet\\hotReloadCrashRepro\\hotReloadCrashRepro\\theAssembly.cs",
                                                            string.Format("TheCompiledAssembly.dll",i));
                    System.Console.WriteLine(string.Format("[Main]   theCompiledAssembly = {0}",theCompiledAssembly));
                }
              
                // Create a Proxy object in the new domain, where we want the
                // assembly (and Python .NET) to reside
                Type type = typeof(Proxy);
                var theProxy = (Proxy)domain.CreateInstanceAndUnwrap(
                    type.Assembly.FullName,
                    type.FullName);

                // From now on use the Proxy to call into the new assembly
                theProxy.InitAssembly(string.Format(@"D:\projects\pythonnet\hotReloadCrashRepro\hotReloadCrashRepro\bin\x64\Debug\TheCompiledAssembly.dll",i));
                theProxy.RunPython();

                System.Console.WriteLine("[Main] Before Domain Unload");
                AppDomain.Unload(domain);
                System.Console.WriteLine("[Main] After Domain Unload");

                // Validate that the assembly does not exist anymore
                try
                {
                    System.Console.WriteLine(string.Format("[Main] The Proxy object is valid ({0})",theProxy));
                }
                catch (Exception)
                {
                    System.Console.WriteLine("[Main] The Proxy object is not valid anymore");
                }
            }
        }

        public class Proxy : MarshalByRefObject
        {
            static Assembly theAssembly = null;

            public void InitAssembly(string assemblyPath)
            {
                System.Console.WriteLine(string.Format("[Proxy] In InitAssembly"));

                theAssembly = Assembly.LoadFile(assemblyPath);
                var pythonrunner = theAssembly.GetType("PythonRunner");
                var initMethod = pythonrunner.GetMethod("Init");
                initMethod.Invoke(null, new object[] {});
            }
            public void RunPython()
            {
                System.Console.WriteLine(string.Format("[Proxy] In RunPython"));

                // Call into the new assembly. Will execute Python code
                var pythonrunner = theAssembly.GetType("PythonRunner");
                var runPythonMethod = pythonrunner.GetMethod("RunPython");
                runPythonMethod.Invoke(null, new object[] { });
            }
        }

        static System.Reflection.Assembly BuildAssembly(string csfilename, string outputAssemblyName)
        {   
            var provider = CodeDomProvider.CreateProvider("CSharp");
            var compilerparams = new CompilerParameters(new string [] {"Python.Runtime.dll"});

            compilerparams.GenerateExecutable = false;
            compilerparams.GenerateInMemory = false;
            compilerparams.IncludeDebugInformation = true;
            compilerparams.OutputAssembly = outputAssemblyName;

            var results = 
                provider.CompileAssemblyFromFile(compilerparams, csfilename);
            if (results.Errors.HasErrors) {   
                StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                foreach (CompilerError error in results.Errors )
                {   
                    errors.AppendFormat("Line {0},{1}\t: {2}\n", 
                            error.Line, error.Column, error.ErrorText);
                }
                throw new Exception(errors.ToString());
            } else {   
                return results.CompiledAssembly;
            }
        }
    }
}
