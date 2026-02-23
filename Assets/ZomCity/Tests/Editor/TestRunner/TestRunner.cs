using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using ApiTestMode = UnityEditor.TestTools.TestRunner.Api.TestMode;
using UnityEngine;
using UnityEngine.TestTools;

namespace ZomCity.Tests.Editor
{
    public static class TestRunner
    {
        private const string EditModeAssemblyName = "ZomCity.M05.Tests.Editor";
        private const string PlayModeAssemblyName = "ZomCity.M05.Tests.PlayMode";
        private const string CategoryM05 = "M0.5";
        private const string CategoryM06 = "M0.6";
        private const string CategoryM07 = "M0.7";

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.5 All")]
        public static void RunM05AllAcceptance()
        {
            RunWithFilters(
                "M0.5_All",
                new Filter
                {
                    testMode = ApiTestMode.EditMode,
                    assemblyNames = new[] { EditModeAssemblyName },
                    categoryNames = new[] { CategoryM05 },
                },
                new Filter
                {
                    testMode = ApiTestMode.PlayMode,
                    assemblyNames = new[] { PlayModeAssemblyName },
                    categoryNames = new[] { CategoryM05 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.5 EditMode")]
        public static void RunM05EditModeAcceptance()
        {
            RunWithFilters(
                "M0.5_EditMode",
                new Filter
                {
                    testMode = ApiTestMode.EditMode,
                    assemblyNames = new[] { EditModeAssemblyName },
                    categoryNames = new[] { CategoryM05 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.5 PlayMode")]
        public static void RunM05PlayModeAcceptance()
        {
            RunWithFilters(
                "M0.5_PlayMode",
                new Filter
                {
                    testMode = ApiTestMode.PlayMode,
                    assemblyNames = new[] { PlayModeAssemblyName },
                    categoryNames = new[] { CategoryM05 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.6 All")]
        public static void RunM06AllAcceptance()
        {
            RunWithFilters(
                "M0.6_All",
                new Filter
                {
                    testMode = ApiTestMode.EditMode,
                    assemblyNames = new[] { EditModeAssemblyName },
                    categoryNames = new[] { CategoryM06 },
                },
                new Filter
                {
                    testMode = ApiTestMode.PlayMode,
                    assemblyNames = new[] { PlayModeAssemblyName },
                    categoryNames = new[] { CategoryM06 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.6 EditMode")]
        public static void RunM06EditModeAcceptance()
        {
            RunWithFilters(
                "M0.6_EditMode",
                new Filter
                {
                    testMode = ApiTestMode.EditMode,
                    assemblyNames = new[] { EditModeAssemblyName },
                    categoryNames = new[] { CategoryM06 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.6 PlayMode")]
        public static void RunM06PlayModeAcceptance()
        {
            RunWithFilters(
                "M0.6_PlayMode",
                new Filter
                {
                    testMode = ApiTestMode.PlayMode,
                    assemblyNames = new[] { PlayModeAssemblyName },
                    categoryNames = new[] { CategoryM06 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.7 All")]
        public static void RunM07AllAcceptance()
        {
            RunWithFilters(
                "M0.7_All",
                new Filter
                {
                    testMode = ApiTestMode.EditMode,
                    assemblyNames = new[] { EditModeAssemblyName },
                    categoryNames = new[] { CategoryM07 },
                },
                new Filter
                {
                    testMode = ApiTestMode.PlayMode,
                    assemblyNames = new[] { PlayModeAssemblyName },
                    categoryNames = new[] { CategoryM07 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.7 EditMode")]
        public static void RunM07EditModeAcceptance()
        {
            RunWithFilters(
                "M0.7_EditMode",
                new Filter
                {
                    testMode = ApiTestMode.EditMode,
                    assemblyNames = new[] { EditModeAssemblyName },
                    categoryNames = new[] { CategoryM07 },
                });
        }

        [MenuItem("Tools/ZomCity/TestRunner/Run M0.7 PlayMode")]
        public static void RunM07PlayModeAcceptance()
        {
            RunWithFilters(
                "M0.7_PlayMode",
                new Filter
                {
                    testMode = ApiTestMode.PlayMode,
                    assemblyNames = new[] { PlayModeAssemblyName },
                    categoryNames = new[] { CategoryM07 },
                });
        }

        private static void RunWithFilters(string runLabel, params Filter[] filters)
        {
            var introductions = CollectTestIntroductions();
            EnsureAllTestsHaveChineseDescription(introductions);
            PrintIntroductions(runLabel, introductions);

            var callback = new AcceptanceCallbacks(runLabel);
            TestRunnerApi.RegisterTestCallback(callback);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var settings = new ExecutionSettings(filters);
            settings.runSynchronously = false;

            Debug.Log("[TestRunner] Start run: " + runLabel);
            api.Execute(settings);
        }

        private static List<TestIntroduction> CollectTestIntroductions()
        {
            var introductions = new List<TestIntroduction>();
            var testTypes = new[]
            {
                typeof(M05EditorAcceptanceTests),
                typeof(ZomCity.Tests.PlayMode.M05PlayModeAcceptanceTests),
            };

            for (var typeIndex = 0; typeIndex < testTypes.Length; typeIndex++)
            {
                var testType = testTypes[typeIndex];
                var methods = testType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (var methodIndex = 0; methodIndex < methods.Length; methodIndex++)
                {
                    var method = methods[methodIndex];
                    var hasNUnitTest = method.GetCustomAttribute<TestAttribute>() != null;
                    var hasUnityTest = method.GetCustomAttribute<UnityTestAttribute>() != null;
                    if (!hasNUnitTest && !hasUnityTest)
                    {
                        continue;
                    }

                    var description = TryGetDescriptionFromAttributes(method);

                    introductions.Add(new TestIntroduction
                    {
                        FullName = testType.FullName + "." + method.Name,
                        Description = description,
                    });
                }
            }

            introductions.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
            return introductions;
        }

        private static string TryGetDescriptionFromAttributes(MemberInfo member)
        {
            var attributes = CustomAttributeData.GetCustomAttributes(member);
            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                var attributeName = attribute.AttributeType.FullName;
                if (!string.Equals(attributeName, "NUnit.Framework.DescriptionAttribute", StringComparison.Ordinal) &&
                    !string.Equals(attributeName, "System.ComponentModel.DescriptionAttribute", StringComparison.Ordinal))
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Count > 0)
                {
                    var firstArgument = attribute.ConstructorArguments[0];
                    if (firstArgument.ArgumentType == typeof(string) &&
                        firstArgument.Value is string textFromCtor &&
                        !string.IsNullOrWhiteSpace(textFromCtor))
                    {
                        return textFromCtor;
                    }
                }

                for (var argIndex = 0; argIndex < attribute.NamedArguments.Count; argIndex++)
                {
                    var namedArg = attribute.NamedArguments[argIndex];
                    if (!string.Equals(namedArg.MemberName, "Description", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (namedArg.TypedValue.ArgumentType == typeof(string) &&
                        namedArg.TypedValue.Value is string textFromNamedArg &&
                        !string.IsNullOrWhiteSpace(textFromNamedArg))
                    {
                        return textFromNamedArg;
                    }
                }
            }

            return string.Empty;
        }

        private static void EnsureAllTestsHaveChineseDescription(List<TestIntroduction> introductions)
        {
            for (var i = 0; i < introductions.Count; i++)
            {
                var intro = introductions[i];
                if (string.IsNullOrWhiteSpace(intro.Description) || !ContainsChineseCharacter(intro.Description))
                {
                    throw new InvalidOperationException("Test missing Chinese description: " + intro.FullName);
                }
            }
        }

        private static bool ContainsChineseCharacter(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '\u4E00' && c <= '\u9FFF')
                {
                    return true;
                }
            }

            return false;
        }

        private static void PrintIntroductions(string runLabel, List<TestIntroduction> introductions)
        {
            Debug.Log("[TestRunner] " + runLabel + " has " + introductions.Count + " cases");
            for (var i = 0; i < introductions.Count; i++)
            {
                var intro = introductions[i];
                Debug.Log("[TestRunner] " + intro.FullName + "\n" + intro.Description);
            }
        }

        private sealed class TestIntroduction
        {
            public string FullName;
            public string Description;
        }

        private sealed class AcceptanceCallbacks : ICallbacks
        {
            private readonly string m_RunLabel;
            private readonly List<TestCaseResultSnapshot> m_TestCaseResults = new List<TestCaseResultSnapshot>();

            public AcceptanceCallbacks(string runLabel)
            {
                m_RunLabel = runLabel;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("[TestRunner] Run started: " + m_RunLabel + " cases=" + testsToRun.TestCaseCount);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var report = new TestRunReport
                {
                    RunLabel = m_RunLabel,
                    GeneratedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ResultState = result.ResultState,
                    PassCount = result.PassCount,
                    FailCount = result.FailCount,
                    SkipCount = result.SkipCount,
                    InconclusiveCount = result.InconclusiveCount,
                    DurationSeconds = result.Duration,
                    Cases = m_TestCaseResults,
                };

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var reportDirectory = Path.Combine(projectRoot, ZomCityProjectConstants.Paths.ReportsOutDir.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(reportDirectory);

                var reportFileName = ZomCityProjectConstants.Reports.FormatJson(BuildReportPrefix(), DateTime.UtcNow);
                var reportPath = Path.Combine(reportDirectory, reportFileName);
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + Environment.NewLine);

                Debug.Log("[TestRunner] Run finished: " + m_RunLabel + " Pass=" + result.PassCount + " Fail=" + result.FailCount + " Report=" + reportPath);
                TestRunnerApi.UnregisterTestCallback(this);
            }

            private string BuildReportPrefix()
            {
                var milestone = m_RunLabel;
                var split = m_RunLabel.IndexOf('_');
                if (split > 0)
                {
                    milestone = m_RunLabel.Substring(0, split);
                }

                milestone = milestone.Replace('.', '_');
                return "AcceptanceReport_" + milestone;
            }

            public void TestStarted(ITestAdaptor test)
            {
                if (test.IsSuite)
                {
                    return;
                }

                Debug.Log("[TestRunner] Case started: " + test.FullName);
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.Test == null || result.Test.IsSuite)
                {
                    return;
                }

                var description = "NoDescription";
                if (!string.IsNullOrWhiteSpace(result.Test.Description))
                {
                    description = result.Test.Description;
                }

                var message = string.Empty;
                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    message = result.Message;
                }

                m_TestCaseResults.Add(new TestCaseResultSnapshot
                {
                    FullName = result.FullName,
                    Description = description,
                    ResultState = result.ResultState,
                    DurationSeconds = result.Duration,
                    Message = message,
                });
            }
        }

        [Serializable]
        private sealed class TestRunReport
        {
            public string RunLabel;
            public string GeneratedAtUtc;
            public string ResultState;
            public int PassCount;
            public int FailCount;
            public int SkipCount;
            public int InconclusiveCount;
            public double DurationSeconds;
            public List<TestCaseResultSnapshot> Cases;
        }

        [Serializable]
        private sealed class TestCaseResultSnapshot
        {
            public string FullName;
            public string Description;
            public string ResultState;
            public double DurationSeconds;
            public string Message;
        }
    }
}
