// hcq 2017/2/20
//用于构造sql字段

namespace UnitySQLite
{
    public class SQLField
    {
        string _name;
        public string name { get { return _name; } }

        string _value;
        public string value { get { return _value; } }

        bool _isPrimaryKey;
        public bool isPrimaryKey { get { return _isPrimaryKey; } }

        SQLFieldType _type = SQLFieldType.TEXT;
        public SQLFieldType type
        {
            get { return _type; }
        }

        protected internal string typeForSQL { get; set; }

        public SQLField(string name, bool isPrimaryKey)
        {
            _name = name;
            _isPrimaryKey = isPrimaryKey;
        }

        public SQLField(string name)
        {
            _name = name;
        }

        public SQLField SetPrimaryKey(bool isPrimaryKey)
        {
            _isPrimaryKey = isPrimaryKey;
            return this;
        }

        public SQLField SetValue(string value)
        {
            _value = value;
            return this;
        }

        public SQLField SetType(SQLFieldType type)
        {
            _type = type;

            switch (type)
            {
                case SQLFieldType.INT:
                    typeForSQL = "INTEGER";
                    break;
                case SQLFieldType.NULL:
                    typeForSQL = "NULL";
                    break;
                case SQLFieldType.FLOAT:
                    typeForSQL = "REAL";
                    break;
                case SQLFieldType.TEXT:
                    typeForSQL = "TEXT";
                    break;
                case SQLFieldType.BINARY:
                    typeForSQL = "BLOB";
                    break;
                case SQLFieldType.LONG:
                    typeForSQL = "BIGINT";
                    break;
            }

            return this;
        }

    }

}

namespace UnitySQLite
{
    //字段类型 http://blog.csdn.net/naturebe/article/details/6981843
    public enum SQLFieldType
    {
        INT, FLOAT, TEXT, BINARY, LONG, NULL
    }
}
