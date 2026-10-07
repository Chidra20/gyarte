using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

// Runs EditMode tests from code and writes the results to the console and to Temp/test-results.txt,
// so the tests can be started through the Unity MCP (RunCommand) and read back from outside Unity.
public static class TestRunLogger
{
    public const string ResultsPath = "Temp/test-results.txt";

    // name: a test class name (e.g. "HealthTests"), or "" for every EditMode test.
    // synchronous: run inside this call, so the results file is complete when it returns. Queued
    // (asynchronous) runs could be cut off or never start when the editor reloaded meanwhile
    public static void Run(string name, bool synchronous = true)
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var callbacks = new Callbacks(string.IsNullOrEmpty(name) ? "all" : name, api);
        api.RegisterCallbacks(callbacks);

        var filter = new Filter { testMode = TestMode.EditMode };
        if (!string.IsNullOrEmpty(name)) filter.groupNames = new[] { name };
        api.Execute(new ExecutionSettings(filter) { runSynchronously = synchronous });
    }

    class Callbacks : ICallbacks
    {
        readonly string label;
        readonly TestRunnerApi api;
        readonly StringBuilder report = new StringBuilder();
        int passed, failed;

        public Callbacks(string label, TestRunnerApi api)
        {
            this.label = label;
            this.api = api;
        }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            File.WriteAllText(ResultsPath, "RUNNING " + label + "\n");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            string summary = "TESTS " + label + ": " + passed + " passed, " + failed + " failed";
            Debug.Log(summary);
            report.AppendLine(summary);
            File.WriteAllText(ResultsPath, report.ToString());

            // Registered callbacks are shared by every TestRunnerApi, so a listener left behind
            // would also count the next run
            api.UnregisterCallbacks(this);
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.HasChildren) return;

            if (result.TestStatus == TestStatus.Passed)
            {
                passed++;
                report.AppendLine("pass " + result.FullName);
            }
            else
            {
                failed++;
                Debug.LogWarning("TEST FAILED " + result.FullName + ": " + result.Message);
                report.AppendLine("FAIL " + result.FullName + ": " + result.Message);
            }
        }
    }
}
