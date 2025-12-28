using System;
using System.Collections.Generic;
using UnityEngine;

namespace FerryKit
{
    [CreateAssetMenu(fileName = nameof(DataImporterSetting), menuName = "DataImporter/" + nameof(DataImporterSetting))]
    public class DataImporterSetting : ScriptableObject
    {
        [Serializable]
        public class Info
        {
            public TextAsset source;
            public DataTableBase dest;
        }

        public List<Info> infoList;
    }
}
