using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Python.Runtime
{


    public class DummyClass
    {
        static public DummyClass instance = new DummyClass();
    }

    public class CLRObjTracker
    {
        // The tracked type
        static public IntPtr trackedPyHandle;
        static public IntPtr trackedTPHandle;

        // Will log if the given CLRObject is of the tracked type
        static internal void Log(CLRObject clrObj, string trace)
        {
            if (clrObj.tpHandle == trackedTPHandle)
            {
                Log(string.Format("[tpHandle = {0}]: {1}", clrObj.tpHandle, trace));
            }
        }

        // Will log if the given pyhandle is the tracked one
        static internal unsafe void Log(IntPtr pyHandle, string trace)
        {
            if (pyHandle == trackedPyHandle)
            {
                Log(string.Format("[pyHandle = {0}, refCnt = {1}]: {2}", pyHandle, Runtime.Refcount(pyHandle), trace));
            }
        }

        // Logs to the file, no condition
        static internal void Log(string trace)
        {
            using (StreamWriter sw = File.AppendText(@"d:\temp\CLRLog.txt"))
            {
                sw.WriteLine(string.Format("[{0}]{1}", System.DateTime.Now, trace));
            }
        }

            
    }
    internal class CLRObject : ManagedType
    {
        internal object inst;

        internal CLRObject(object ob, IntPtr tp)
        {
            IntPtr py = Runtime.PyType_GenericAlloc(tp, 0);

            long flags = Util.ReadCLong(tp, TypeOffset.tp_flags);
            if ((flags & TypeFlags.Subclass) != 0)
            {
                IntPtr dict = Marshal.ReadIntPtr(py, ObjectOffset.DictOffset(tp));
                if (dict == IntPtr.Zero)
                {
                    dict = Runtime.PyDict_New();
                    Marshal.WriteIntPtr(py, ObjectOffset.DictOffset(tp), dict);
                }
            }

            GCHandle gc = GCHandle.Alloc(this);
            Marshal.WriteIntPtr(py, ObjectOffset.magic(tp), (IntPtr)gc);
            tpHandle = tp;
            pyHandle = py;
            gcHandle = gc;
            inst = ob;

            // Fix the BaseException args (and __cause__ in case of Python 3)
            // slot if wrapping a CLR exception
            Exceptions.SetArgsAndCause(py);

            /////////////////dltrace////////////////////////
            CLRObjTracker.Log(this, "In CLRObject::CLRObject");
            /////////////////dltrace////////////////////////
        }


        internal static CLRObject GetInstance(object ob, IntPtr pyType)
        {
            return new CLRObject(ob, pyType);
        }


        internal static CLRObject GetInstance(object ob)
        {
            ClassBase cc = ClassManager.GetClass(ob.GetType());
            return GetInstance(ob, cc.tpHandle);
        }


        internal static IntPtr GetInstHandle(object ob, IntPtr pyType)
        {
            CLRObject co = GetInstance(ob, pyType);
            return co.pyHandle;
        }

        internal static IntPtr GetInstHandle(object ob, Type type)
        {
            ClassBase cc = ClassManager.GetClass(type);

            /////////////////dltrace////////////////////////
            if (type == typeof(DummyClass))
            {
                CLRObjTracker.trackedTPHandle = cc.tpHandle;
            }
            /////////////////dltrace////////////////////////

            CLRObject co = GetInstance(ob, cc.tpHandle);

            /////////////////dltrace////////////////////////
            if (type == typeof(DummyClass))
            {
                CLRObjTracker.trackedPyHandle = co.pyHandle;
                CLRObjTracker.Log(co.pyHandle, "In CLRObject.GetInstHandle (start tracking pyHandle)");
            }
            /////////////////dltrace////////////////////////

            return co.pyHandle;
        }


        internal static IntPtr GetInstHandle(object ob)
        {
            CLRObject co = GetInstance(ob);
            return co.pyHandle;
        }
    }
}
