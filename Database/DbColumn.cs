using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Michael.Database
{
    public class DbColumn
    {
        public string ColumnName { get; set; }
        public int? ColumnOrdinal { get; set; }
        public int? ColumnSize { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
        public Type DataType { get; set; }
    }
}
