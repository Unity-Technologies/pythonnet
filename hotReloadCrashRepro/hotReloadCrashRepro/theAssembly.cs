using System;
using Python.Runtime;

class PythonRunner
{
    static AppDomain myDomain = null;
    static public void Init()
    {
        System.Console.WriteLine("[theAssembly] PythonRunner.Init");
        System.Console.WriteLine(string.Format("[theAssembly]   current domain = {0}",AppDomain.CurrentDomain.FriendlyName));

        // Make sure we shut down properly on app domain reload
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
    }

    private static void OnDomainUnload(object sender, EventArgs e)
    {
        System.Console.WriteLine("[theAssembly] In OnDomainUnload, calling PythonEngine.Shutdown()");
        System.Console.WriteLine(string.Format("[theAssembly]   current domain = {0}",AppDomain.CurrentDomain.FriendlyName));
    }

    public static void RunPython() {
        System.Console.WriteLine("[theAssembly] In PythonRunner.RunPython");
        using (Py.GIL()) {
            try {
                var pyScript =
                    "import clr\n" +
                    "clr.DummyMethod()\n" +
                    "import System\n" +
                    "currDomain = System.AppDomain.CurrentDomain.FriendlyName\n" +
                    "print('[Python] current domain = %s'%str(currDomain))\n";
                PythonEngine.Exec(pyScript);
            } catch(Exception e) {
                System.Console.WriteLine(string.Format("Caught exception: {0}",e));
            }   
        }   
    }
}
