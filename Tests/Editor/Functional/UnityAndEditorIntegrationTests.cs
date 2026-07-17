using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace FerryKit.Core.Tests.Functional
{
    public class UnityAndEditorIntegrationTests
    {
        [Test]
        public void DataTable_LoadsRowsAndReportsInvalidSource()
        {
            var table = ScriptableObject.CreateInstance<TestDataTable>();
            try
            {
                SetDataList(table, new List<TestDataRow>());
                Assert.That(table.Load("id,name\n1,one\n2,two"), Is.True);
                Assert.That(table.DataList.ConvertAll(x => x.Id), Is.EqualTo(new[] { 1, 2 }));

                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("parse failed"));
                Assert.That(table.Load("id,name\ninvalid,row"), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(table); }
        }

        [Test]
        public void DevLog_ForwardsEditorLogsAtEverySeverity()
        {
            LogAssert.Expect(LogType.Log, "log");
            DevLog.Log("log");
            LogAssert.Expect(LogType.Warning, "warning");
            DevLog.LogWarning("warning");
            LogAssert.Expect(LogType.Error, "error");
            DevLog.LogError("error");
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));
            DevLog.LogException(new InvalidOperationException("boom"));
        }

        [Test]
        public void SingletonStaticReset_ClearsCachedInstanceAndQuitFlag()
        {
            Type closedBase = typeof(SingletonBase<TestSingleton>);
            FieldInfo instance = closedBase.GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo quitting = closedBase.GetField("_isQuitting", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo reset = closedBase.GetMethod("ResetStatics", BindingFlags.Static | BindingFlags.NonPublic);
            var go = new GameObject("singleton-test");
            var component = go.AddComponent<TestSingleton>();
            try
            {
                instance.SetValue(null, component);
                quitting.SetValue(null, true);
                reset.Invoke(null, null);
                Assert.That(instance.GetValue(null), Is.Null);
                Assert.That(quitting.GetValue(null), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void DataImporterTypes_PreserveEditorAndSerializationContracts()
        {
            Assert.That(typeof(DataImporter).IsSubclassOf(typeof(EditorWindow)), Is.True);
            Assert.That(typeof(DataImporterSetting).GetCustomAttribute<CreateAssetMenuAttribute>(), Is.Not.Null);
            var setting = ScriptableObject.CreateInstance<DataImporterSetting>();
            var source = new TextAsset("header\nvalue");
            var table = ScriptableObject.CreateInstance<TestDataTable>();
            try
            {
                setting.infoList = new List<DataImporterSetting.Info>
                {
                    new DataImporterSetting.Info { source = source, dest = table }
                };
                var serialized = new SerializedObject(setting);
                Assert.That(serialized.FindProperty("infoList").arraySize, Is.EqualTo(1));
                Assert.That(setting.infoList[0].source, Is.SameAs(source));
                Assert.That(setting.infoList[0].dest, Is.SameAs(table));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
                UnityEngine.Object.DestroyImmediate(table);
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        private static void SetDataList(TestDataTable table, List<TestDataRow> data)
        {
            typeof(DataTable<TestDataRow>).GetField("_dataList", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(table, data);
        }
    }

    public sealed class TestSingleton : SingletonStatic<TestSingleton> { }

    public sealed class TestDataTable : DataTable<TestDataRow> { }

    [Serializable]
    public sealed class TestDataRow : ITryParsable
    {
        public int Id;
        public string Name;
        public bool TryParse(ref LineReader reader)
            => reader.TryRead(out Id) && reader.TryRead(out Name);
    }
}
