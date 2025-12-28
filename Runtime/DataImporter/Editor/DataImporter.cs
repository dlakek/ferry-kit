using System;
using System.Collections.ObjectModel;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FerryKit
{
    public class DataImporter : EditorWindow
    {
        private const string _name = "Data Importer";
        private const string _defaultSettingPath = "Assets/Datas/Editor/";
        private const string _defaultSettingName = "DataImporterSetting.asset";
        private const string _defaultSettingFullName = _defaultSettingPath + _defaultSettingName;

        private DataImporterSetting _settingAsset;
        private SerializedObject _setting;
        private ReorderableList _infoList;
        private Vector2 _scrollPos;

        [MenuItem("FerryKit/" + _name)]
        public static void ShowWindow() => GetWindow<DataImporter>(_name);

        private void OnEnable()
        {
            LoadSettingAsset();
        }

        private void OnGUI()
        {
            GUILayout.Label("📦 " + _name, EditorStyles.boldLabel);
            if (ReadySettingAsset())
            {
                UpdateSetting();
                DrawInfoList();
                DrawButtons();
            }
        }

        private void LoadSettingAsset()
        {
            if (_settingAsset != null)
                return;

            var guids = AssetDatabase.FindAssets($"t:{nameof(DataImporterSetting)}");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settingAsset = AssetDatabase.LoadAssetAtPath<DataImporterSetting>(path);
            }
        }

        private bool ReadySettingAsset()
        {
            if (_settingAsset == null)
            {
                EditorGUILayout.HelpBox("There is no configuration file (.asset).\nYou can create a file by clicking the Create button and manage it with Git.", MessageType.Warning);
                if (GUILayout.Button("Create Setting Asset", GUILayout.Height(30)))
                {
                    CreateSettingsAsset();
                }
            }
            _settingAsset = (DataImporterSetting)EditorGUILayout.ObjectField("Setting Asset", _settingAsset, typeof(DataImporterSetting), false);
            return _settingAsset != null;
        }

        private void CreateSettingsAsset()
        {
            if (!Directory.Exists(_defaultSettingPath))
            {
                Directory.CreateDirectory(_defaultSettingPath);
            }
            _settingAsset = CreateInstance<DataImporterSetting>();
            AssetDatabase.CreateAsset(_settingAsset, _defaultSettingFullName);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"📄 Setting Asset created: {_defaultSettingFullName}");
        }

        private void UpdateSetting()
        {
            if (_setting == null || _setting.targetObject != _settingAsset)
            {
                _setting = new SerializedObject(_settingAsset);
                InitializeInfoList();
            }
            _setting.Update();
        }

        private void InitializeInfoList()
        {
            var prop = _setting.FindProperty(nameof(DataImporterSetting.infoList));
            _infoList = new(_setting, prop)
            {
                drawHeaderCallback = static rect =>
                {
                    EditorGUI.LabelField(rect, "Import Info List");
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    rect.y += 2;
                    var e = prop.GetArrayElementAtIndex(index);
                    var w = rect.width;
                    var h = EditorGUIUtility.singleLineHeight;
                    var s = new Rect(rect.x, rect.y, w * 0.4f, h);
                    var l = new Rect(rect.x + s.width + 2.5f, rect.y, 15, h);
                    var d = new Rect(rect.x + s.width + l.width + 5, rect.y, w * 0.6f - l.width - 5, h);
                    EditorGUI.PropertyField(s, e.FindPropertyRelative(nameof(DataImporterSetting.Info.source)), GUIContent.none);
                    EditorGUI.LabelField(l, "to");
                    EditorGUI.PropertyField(d, e.FindPropertyRelative(nameof(DataImporterSetting.Info.dest)), GUIContent.none);
                },
                multiSelect = true,
            };
        }

        private void DrawInfoList()
        {
            EditorGUILayout.Space(5);
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            _infoList.DoLayoutList();
            _setting.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);
        }

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            var buttonHeight = GUILayout.Height(30);
            var indices = _infoList.selectedIndices;
            GUI.enabled = indices.Count > 0;
            if (GUILayout.Button($"Import Selects ({indices.Count})", buttonHeight))
            {
                ProcessImport(indices);
            }
            GUI.enabled = true;
            if (GUILayout.Button("Import All", buttonHeight))
            {
                ProcessImport(null);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);
        }

        private void ProcessImport(ReadOnlyCollection<int> selectedIndices = null)
        {
            try
            {
                if (selectedIndices != null)
                {
                    int count = selectedIndices.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        if (!Import(selectedIndices[i], i, count))
                            break;
                    }
                }
                else
                {
                    int count = _settingAsset.infoList.Count;
                    for (int i = 0; i < count; ++i)
                    {
                        if (!Import(i, i, count))
                            break;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
            }
        }

        private bool Import(int index, int i, int totalCount)
        {
            var setting = _settingAsset.infoList[index];
            if (setting.source == null || setting.dest == null)
            {
                Debug.LogWarning($"❌ Failed: source or dest is none. index: {index}");
                return true;
            }
            if (EditorUtility.DisplayCancelableProgressBar("Importing Data", $"Processing {setting.source.name} ({i + 1}/{totalCount})...", (float)i / totalCount))
            {
                Debug.Log("🚫 Import Cancelled by user.");
                return false;
            }
            try
            {
                if (setting.dest.Load(setting.source.text))
                {
                    EditorUtility.SetDirty(setting.dest);
                    Debug.Log($"✅ Imported: {setting.source.name} to {setting.dest.name}. index: {index}");
                }
                else
                {
                    Debug.LogError($"❌ Failed: {setting.source.name} to {setting.dest.name}. index: {index}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return true;
        }
    }
}
