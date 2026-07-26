using FerryKit.Core;
using System.Collections.Generic;
using UnityEngine;

namespace FerryKit
{
    public abstract class DataTableBase : ScriptableObject
    {
        protected const string _menuPath = "DataTables/";
        protected const int _reserveLine = 100;

        public abstract bool Load(string source);
    }

    public abstract class DataTable<T> : DataTableBase where T : ITryParsable, new()
    {
        [SerializeField] private List<T> _dataList = new();

        public int Count => _dataList.Count;
        public T this[int index] => _dataList[index];
        public List<T>.Enumerator GetEnumerator() => _dataList.GetEnumerator();

        public override bool Load(string source)
        {
            if (!TextParser.TryParse(source, out List<T> parsed, out string reason, _reserveLine, true))
            {
                DevLog.LogError(reason);
                return false;
            }
            _dataList = parsed;
            return true;
        }
    }
}
