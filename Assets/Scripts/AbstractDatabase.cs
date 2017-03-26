// hcq 2017/2/17
using System;
using Mono.Data.Sqlite;
using System.Collections.Generic;
using System.Collections;

namespace UnitySQLite
{
    public abstract class AbstractDatabase : IDisposable
    {
        public const string COL_ID = "_id";

        SQLiteHelper sqlite;
        readonly Dictionary<string, SQLField> allFields = new Dictionary<string, SQLField>();

        protected internal string tableName;

        //要操作的数据库表名
        public abstract string GetTableName();
        //要操作的数据库表中的字段
        public abstract SQLField[] GetAllFields();

        //打开或创建数据库及数据表
        void OpenOrCreateIfNeed()
        {
            if (sqlite == null)
            {
                sqlite = new SQLiteHelper(GetDatabaseName());

                tableName = GetTableName();

                SQLField[] fields = GetAllFields();

                bool isExistIdField = false;

                for (int i = 0; i < fields.Length; i++)
                {
                    if (string.Equals(fields[i].name, COL_ID))
                    {
                        isExistIdField = true;
                    }

                    allFields.Add(fields[i].name, fields[i]);
                }

                if (!isExistIdField)
                {
                    //默认创建integer字段_id 并作为主键
                    allFields.Add(COL_ID, new SQLField(COL_ID).SetType(SQLFieldType.INT).SetPrimaryKey(true));
                }
            }

            if (!sqlite.IsTableExist(tableName))
            {
                SQLStatement statement = new SQLStatement(tableName);
                foreach (KeyValuePair<string, SQLField> kvp in allFields)
                {
                    statement.CreateFields(kvp.Value);
                }

                SqliteDataReader reader = sqlite.CreateTable(statement);
                sqlite.CloseReader(reader);
            }
        }

        //资源销毁 可以用using关键字
        public void Dispose()
        {
            if (sqlite != null)
            {
                sqlite.CloseConnection();
                sqlite = null;
            }
        }

        //数据库名
        public virtual string GetDatabaseName()
        {
            return "db";
        }

        #region 增
        /// <param name="fields">要插入的字段</param>
        public void Insert(params SQLField[] fields)
        {
            if (IsCountAndValueEmpty(fields))
            {
                return;
            }

            OpenOrCreateIfNeed();

            SqliteDataReader reader = sqlite.Insert(tableName, fields);
            sqlite.CloseReader(reader);
        }

        #endregion

        #region 删

        public void DeleteTable(bool isDrop = false)
        {
            OpenOrCreateIfNeed();

            SqliteDataReader reader = sqlite.DeleteTable(tableName, isDrop);
            sqlite.CloseReader(reader);
        }

        public void Delete(SQLStatement statement)
        {
            if (statement == null)
            {
                return;
            }

            OpenOrCreateIfNeed();

            SqliteDataReader reader = sqlite.Delete(statement);
            sqlite.CloseReader(reader);
        }

        /// <param name="field">要删除的字段</param>
        /// <param name="operation">条件语句中的操作符 比如 = 或 like</param>
        public void Delete(SQLField field, SQLOperation operation = SQLOperation.EQUAL)
        {
            if (field == null)
            {
                return;
            }

            SQLStatement statement = new SQLStatement(GetTableName());
            statement.AddConditions(operation, SQLUnion.NONE, field);

            Delete(statement);
        }

        /// <param name="union">条件语句中的unoin关键字 比如 OR 或 AND</param>
        public void Delete(SQLField[] fields, SQLUnion union, SQLOperation operation = SQLOperation.EQUAL)
        {
            if (fields == null)
            {
                return;
            }

            SQLStatement statement = new SQLStatement(GetTableName());
            statement.AddConditions(operation, union, fields);

            Delete(statement);
        }

        #endregion

        #region 改

        public void Upate(SQLStatement statement)
        {
            if (statement == null)
            {
                return;
            }

            OpenOrCreateIfNeed();

            SqliteDataReader reader = sqlite.Update(statement);
            sqlite.CloseReader(reader);
        }

        /// <param name="fields">要更新的字段</param>
        /// <param name="union">条件语句中的unoin关键字 比如 OR 或 AND</param>
        /// <param name="operation">条件语句中的操作符 比如 = 或 like</param>
        /// <param name="conditionFields">条件字段</param>
        public void Update(SQLField[] fields, SQLUnion union, SQLOperation operation, params SQLField[] conditionFields)
        {
            if ((fields == null || fields.Length <= 0) || IsCountAndValueEmpty(conditionFields))
            {
                return;
            }

            SQLStatement statement = new SQLStatement(GetTableName());
            statement.UpateFields(fields).AddConditions(operation, union, conditionFields);

            Upate(statement);
        }

        public void Update(SQLField[] fields, SQLUnion union, params SQLField[] conditionFields)
        {
            Update(fields, union, SQLOperation.EQUAL, conditionFields);
        }

        public void Update(SQLField[] fields, SQLField conditionField)
        {
            Update(fields, SQLUnion.NONE, SQLOperation.EQUAL, conditionField);
        }

        public void Update(SQLField field, SQLUnion union, SQLOperation operation, params SQLField[] conditionFields)
        {
            Update(new SQLField[] { field }, union, operation, conditionFields);
        }

        public void Update(SQLField field, SQLUnion union, params SQLField[] conditionFields)
        {
            Update(field, union, SQLOperation.EQUAL, conditionFields);
        }

        public void Update(SQLField field, SQLField conditionField)
        {
            Update(field, SQLUnion.NONE, SQLOperation.EQUAL, conditionField);
        }

        #endregion

        #region 查

        public List<SQLField[]> Query(SQLStatement statement)
        {
            if (statement == null)
            {
                return null;
            }

            string[] fieldNames = statement.GetSelectFieldArray();
            if ((fieldNames == null || fieldNames.Length <= 0))
            {
                return null;
            }

            OpenOrCreateIfNeed();

            List<SQLField[]> result = new List<SQLField[]>();

            SqliteDataReader reader = sqlite.Query(statement);

            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    SQLField[] resultFields = new SQLField[fieldNames.Length];

                    for (int i = 0; i < fieldNames.Length; i++)
                    {
                        string fieldName = fieldNames[i].Trim();

                        int ordinal = reader.GetOrdinal(fieldName);

                        if (reader.IsDBNull(ordinal))
                        {
                            resultFields[i] = new SQLField(fieldName);
                        }
                        else
                        {
                            SQLFieldType fieldType = allFields[fieldName].type;

                            switch (fieldType)
                            {
                                case SQLFieldType.BINARY:
                                    resultFields[i] = new SQLField(fieldName).SetValue(Convert.ToString((byte[])reader.GetValue(ordinal)));
                                    break;
                                case SQLFieldType.FLOAT:
                                    resultFields[i] = new SQLField(fieldName).SetValue(Convert.ToString(reader.GetFloat(ordinal)));
                                    break;
                                case SQLFieldType.INT:
                                    resultFields[i] = new SQLField(fieldName).SetValue(Convert.ToString(reader.GetInt32(ordinal)));
                                    break;
                                case SQLFieldType.LONG:
                                    resultFields[i] = new SQLField(fieldName).SetValue(Convert.ToString(reader.GetInt64(ordinal)));
                                    break;
                                case SQLFieldType.TEXT:
                                    resultFields[i] = new SQLField(fieldName).SetValue(reader.GetString(ordinal));
                                    break;
                                default:
                                    resultFields[i] = new SQLField(fieldName);
                                    break;
                            }

                            resultFields[i].SetType(fieldType);
                        }
                    }

                    result.Add(resultFields);
                }

            }

            sqlite.CloseReader(reader);

            return result;
        }

        /// <returns>返回多条查询结果 每条结果包含多个字段的值</returns>
        /// <param name="fieldNames">要查询的字段名</param>
        /// <param name="union">条件语句中的unoin关键字 比如 OR 或 AND</param>
        /// <param name="operation">条件语句中的操作符 比如 = 或 like</param>
        /// <param name="conditionFields">条件字段</param>
        public List<SQLField[]> Query(string[] fieldNames, SQLUnion union, SQLOperation operation, params SQLField[] conditionFields)
        {
            if (fieldNames == null || fieldNames.Length <= 0)
            {
                return null;
            }

            SQLStatement statement = new SQLStatement(GetTableName());
            statement.SelectFields(fieldNames);

            if (!IsCountAndValueEmpty(conditionFields))
            {
                statement.AddConditions(operation, union, conditionFields);
            }

            return Query(statement);
        }

        public List<SQLField[]> Query(string[] fieldNames, SQLUnion union, params SQLField[] conditionFields)
        {
            return Query(fieldNames, union, SQLOperation.EQUAL, conditionFields);
        }

        public List<SQLField[]> Query(string[] fieldNames, SQLField conditionField)
        {
            return Query(fieldNames, SQLUnion.NONE, SQLOperation.EQUAL, conditionField);
        }

        public List<SQLField[]> Query(string fieldName, SQLUnion union, SQLOperation operation, params SQLField[] conditionFields)
        {
            return Query(new string[] { fieldName }, union, operation, conditionFields);
        }

        public List<SQLField[]> Query(string fieldName, SQLUnion union, params SQLField[] conditionFields)
        {
            return Query(fieldName, union, SQLOperation.EQUAL, conditionFields);
        }

        public List<SQLField[]> Query(string fieldName, SQLField conditionField)
        {
            return Query(fieldName, SQLUnion.NONE, SQLOperation.EQUAL, conditionField);
        }

        public bool Query(SQLField field)
        {
            if (field == null)
            {
                return false;
            }

            OpenOrCreateIfNeed();

            return sqlite.Query(tableName, field);
        }

        #endregion

        public bool IsTableExist()
        {
            OpenOrCreateIfNeed();

            return sqlite.IsTableExist(tableName);
        }

        public bool IsFieldExist(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                return false;
            }

            OpenOrCreateIfNeed();

            return sqlite.IsFieldExist(tableName, fieldName);
        }

        //数量和值都是空
        static bool IsCountAndValueEmpty(ICollection collection)
        {
            if (!(collection == null || collection.Count <= 0))
            {
                IEnumerator e = collection.GetEnumerator();
                while (e.MoveNext())
                {
                    object obj = e.Current;
                    if (obj != null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

    }
}