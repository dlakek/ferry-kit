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
        [SerializeField] private List<T> _dataList;

        public List<T> DataList => _dataList;

        public override bool Load(string source)
        {
            if (!TextParser.TryParse(source, _dataList, out string reason, _reserveLine, true))
            {
                DevLog.LogError(reason);
                return false;
            }
            return true;
        }
    }
}
