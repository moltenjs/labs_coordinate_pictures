﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace labs_coordinate_pictures
{
    public class CoordinatePicturesTestException : ApplicationException
    {
        public CoordinatePicturesTestException(string message) : base(message) { }
    }

    public static class TestUtil
    {
        public static void AssertEqual(object expected, object actual)
        {
            if (!expected.Equals(actual))
            {
                throw new Exception("Assertion failure, expected " + expected + " but got " + actual);
            }
        }

        public static void AssertTrue(bool actual)
        {
            AssertEqual(true, actual);
        }

        public static void AssertExceptionMessage(Action fn, string strExpect)
        {
            string strException = null;
            try
            {
                fn();
            }
            catch (Exception exc)
            {
                strException = exc.ToString();
            }
            if (strException == null || !strException.Contains(strExpect))
            {
                throw new CoordinatePicturesTestException("Testing.AssertExceptionMessageIncludes expected " + strExpect + " but got " + strException + ".");
            }
        }

        public static void AssertStringArrayEqual(IList<string> expected, IList<string> actual)
        {
            AssertEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                AssertEqual(expected[i], actual[i]);
            }
        }

        public static void CallAllTestMethods(Type t, object[] arParams)
        {
            MethodInfo[] methodInfos = t.GetMethods(BindingFlags.Static | BindingFlags.NonPublic);
            foreach (MethodInfo methodInfo in methodInfos)
            {
                if (methodInfo.Name.StartsWith("TestMethod_"))
                {
                    Debug.Assert(methodInfo.GetParameters().Length == 0);
                    methodInfo.Invoke(null, arParams);
                }
            }
        }

        public static string GetTestWriteDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "test_labs_coordinate_pictures");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    public static class CoordinatePicturesTests
    {
        static void TestMethod_Asserts_EqualIntsShouldCompareEqual()
        {
            TestUtil.AssertEqual(1, 1);
        }
        static void TestMethod_Asserts_EqualStringsShouldCompareEqual()
        {
            TestUtil.AssertEqual("abcd", "abcd");
        }
        static void TestMethod_Asserts_EqualBoolsShouldCompareEqual()
        {
            TestUtil.AssertEqual(true, true);
            TestUtil.AssertEqual(false, false);
        }
        static void TestMethod_Asserts_CheckAssertMessage()
        {
            Action fn = delegate () { throw new CoordinatePicturesTestException("test123"); };
            TestUtil.AssertExceptionMessage(fn, "test123");
        }
        static void TestMethod_Asserts_NonEqualIntsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.AssertEqual(1, 2), "expected 1 but got 2");
        }
        static void TestMethod_Asserts_NonEqualStrsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.AssertEqual("abcd", "abce"), "expected abcd but got abce");
        }
        static void TestMethod_Asserts_NonEqualBoolsShouldCompareNonEqual()
        {
            TestUtil.AssertExceptionMessage(() => TestUtil.AssertEqual(true, false), "expected True but got False");
        }

        static void TestMethod_OsHelpersCombineProcessArguments()
        {
            TestUtil.AssertEqual("\"\"", OsHelpers.CombineProcessArguments(new string[] { "" }));
            TestUtil.AssertEqual("\"\\\"\"", OsHelpers.CombineProcessArguments(new string[] { "\"" }));
            TestUtil.AssertEqual("\"\\\"\\\"\"", OsHelpers.CombineProcessArguments(new string[] { "\"\"" }));
            TestUtil.AssertEqual("\"\\\"a\\\"\"", OsHelpers.CombineProcessArguments(new string[] { "\"a\"" }));
            TestUtil.AssertEqual("\\", OsHelpers.CombineProcessArguments(new string[] { "\\" }));
            TestUtil.AssertEqual("\"a b\"", OsHelpers.CombineProcessArguments(new string[] { "a b" }));
            TestUtil.AssertEqual("a \" b\"", OsHelpers.CombineProcessArguments(new string[] { "a", " b" }));
            TestUtil.AssertEqual("a\\\\b", OsHelpers.CombineProcessArguments(new string[] { "a\\\\b" }));
            TestUtil.AssertEqual("\"a\\\\b c\"", OsHelpers.CombineProcessArguments(new string[] { "a\\\\b c" }));
            TestUtil.AssertEqual("\" \\\\\"", OsHelpers.CombineProcessArguments(new string[] { " \\" }));
            TestUtil.AssertEqual("\" \\\\\\\"\"", OsHelpers.CombineProcessArguments(new string[] { " \\\"" }));
            TestUtil.AssertEqual("\" \\\\\\\\\"", OsHelpers.CombineProcessArguments(new string[] { " \\\\" }));
            TestUtil.AssertEqual("\"C:\\Program Files\\\\\"", OsHelpers.CombineProcessArguments(new string[] { "C:\\Program Files\\" }));
            TestUtil.AssertEqual("\"dafc\\\"\\\"\\\"a\"", OsHelpers.CombineProcessArguments(new string[] { "dafc\"\"\"a" }));
        }

        static void TestMethod_ClassConfigsPersistedCommonUsage()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "test.ini");
            ClassConfigs cfg = new ClassConfigs(path);
            cfg.LoadPersisted();
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.EnablePersonalFeatures));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathTrash));
            cfg.Set(ConfigsPersistedKeys.EnablePersonalFeatures, "data=with=equals=");
            cfg.Set(ConfigsPersistedKeys.FilepathPython, " data\twith\t tabs");
            
            /* from memory */
            TestUtil.AssertEqual("data=with=equals=", cfg.Get(ConfigsPersistedKeys.EnablePersonalFeatures));
            TestUtil.AssertEqual(" data\twith\t tabs", cfg.Get(ConfigsPersistedKeys.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathTrash));
            
            /* from disk */
            cfg = new ClassConfigs(path);
            cfg.LoadPersisted();
            TestUtil.AssertEqual("data=with=equals=", cfg.Get(ConfigsPersistedKeys.EnablePersonalFeatures));
            TestUtil.AssertEqual(" data\twith\t tabs", cfg.Get(ConfigsPersistedKeys.FilepathPython));
            TestUtil.AssertEqual("", cfg.Get(ConfigsPersistedKeys.FilepathTrash));
        }

        static void TestMethod_ClassConfigsPersistedBools()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "testbools.ini");
            ClassConfigs cfg = new ClassConfigs(path);
            TestUtil.AssertEqual(false, cfg.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures));
            cfg.SetBool(ConfigsPersistedKeys.EnablePersonalFeatures, true);
            TestUtil.AssertEqual(true, cfg.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures));
            cfg.SetBool(ConfigsPersistedKeys.EnablePersonalFeatures, false);
            TestUtil.AssertEqual(false, cfg.GetBool(ConfigsPersistedKeys.EnablePersonalFeatures));
        }

        static void TestMethod_ClassConfigsNewlinesShouldNotBeAccepted()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "test.ini");
            ClassConfigs cfg = new ClassConfigs(path);
            TestUtil.AssertExceptionMessage(
                () => cfg.Set(ConfigsPersistedKeys.FilepathPython, "data\rnewline"), "cannot contain newline");
            TestUtil.AssertExceptionMessage(
                () => cfg.Set(ConfigsPersistedKeys.FilepathPython, "data\nnewline"), "cannot contain newline");
        }

        static void TestMethod_ClassConfigsInMemory()
        {
            string path = Path.Combine(TestUtil.GetTestWriteDirectory(), "test.ini");
            ClassConfigs cfg = new ClassConfigs(path);
            TestUtil.AssertEqual("", cfg.GetTemporary(ConfigsMemoryKeys.SupressDialogs));
            cfg.SetTemporary(ConfigsMemoryKeys.SupressDialogs, "data=with=equals=");
            TestUtil.AssertEqual("data=with=equals=", cfg.GetTemporary(ConfigsMemoryKeys.SupressDialogs));
            cfg.SetTemporary(ConfigsMemoryKeys.SupressDialogs, "");
            TestUtil.AssertEqual("", cfg.GetTemporary(ConfigsMemoryKeys.SupressDialogs));
        }
        
        public static void RunTests()
        {
            string dir = TestUtil.GetTestWriteDirectory();
            Directory.Delete(dir, true);
            ClassConfigs.Current.SetTemporary(ConfigsMemoryKeys.SupressDialogs, "true");
            try
            {
                TestUtil.CallAllTestMethods(typeof(CoordinatePicturesTests), null);
            }
            finally
            {
                ClassConfigs.Current.SetTemporary(ConfigsMemoryKeys.SupressDialogs, "false");
            }
        }
    }
}
