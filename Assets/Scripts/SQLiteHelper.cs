// hcq 2017/2/15
using Mono.Data.Sqlite;
using UnityEngine;
using System.Text;
using System;

#pragma warning disable XS0001 // Find usages of mono todo items

namespace UnitySQLite
{
    public class SQLiteHelper
    {
        SqliteConnection dbConnection;

        /// <summary>
        /// 构造函数    
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        public SQLiteHelper(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                return;
            }

            try
            {
                string dataPath, protocol;

                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        protocol = "URI=file:";
                        dataPath = Application.persistentDataPath;
                        break;
                    case RuntimePlatform.IPhonePlayer:
                        protocol = "data source=";
                        dataPath = Application.persistentDataPath;
                        break;
                    default:
                        protocol = "data source=";
                        dataPath = Application.dataPath;
                        break;
                }

                string sql = string.Format("{0}{1}/{2}.db", protocol, dataPath, dbName);

                //构造数据库连接
                dbConnection = new SqliteConnection(sql);
                //打开数据库
                dbConnection.Open();

                Debug.Log("hcq2 db connect sql " + sql);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        /// <summary>
        /// 执行SQL命令
        /// </summary>
        /// <param name="sql">SQL命令字符串</param>
        public SqliteDataReader Execute(string sql)
        {
            Debug.Log("hcq db execute sql " + sql);

            if (string.IsNullOrEmpty(sql))
            {
                return null;
            }

            using (SqliteCommand dbCommand = dbConnection.CreateCommand())
            {
                dbCommand.CommandText = sql;
                return dbCommand.ExecuteReader();
            }
        }

        public void CloseReader(SqliteDataReader reader)
        {
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }
        }

        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        public void CloseConnection()
        {
            //销毁Connection
            if (dbConnection != null)
            {
                dbConnection.Close();
                dbConnection.Dispose();
                dbConnection = null;
            }
        }

        /// <summary>
        /// 创建数据表
        /// </summary>
        public SqliteDataReader CreateTable(SQLStatement sqlStatement)
        {
            if (sqlStatement == null)
            {
                return null;
            }

            string sql = string.Format(
                "CREATE TABLE {0} ( {1} ) ",
                sqlStatement.tableName, sqlStatement.GetCreateFields());

            return Execute(sql);
        }


        /// <summary>
        /// 读取整张数据表
        /// </summary>
        /// <returns>The full table.</returns>
        /// <param name="tableName">数据表名称</param>
        public SqliteDataReader ReadTable(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return null;
            }

            string sql = string.Format("SELECT * FROM {0}", tableName);
            return Execute(sql);
        }

        #region 增

        public SqliteDataReader Insert(string tableName, string[] fieldValues)
        {
            if (string.IsNullOrEmpty(tableName) || (fieldValues == null || fieldValues.Length <= 0))
            {
                return null;
            }

            StringBuilder fields = new StringBuilder();
            for (int i = 1; i < fieldValues.Length; i++)
            {
                fields.Append(", '");
                fields.Append(fieldValues[i]);
                fields.Append("'");
            }

            string sql = string.Format(
                "INSERT INTO {0} VALUES ( '{1}'{2} )"
                , tableName, fieldValues[0], fields);

            return Execute(sql);
        }

        public SqliteDataReader Insert(string tableName, SQLField[] fields)
        {

            if (string.IsNullOrEmpty(tableName) || (fields == null || fields.Length <= 0))
            {
                return null;
            }

            StringBuilder fieldNames = new StringBuilder();
            for (int i = 1; i < fields.Length; ++i)
            {
                fieldNames.Append(", ");
                fieldNames.Append(fields[i].name);
            }

            StringBuilder fieldValues = new StringBuilder();
            for (int i = 1; i < fields.Length; ++i)
            {
                fieldValues.Append(", '");
                fieldValues.Append(fields[i].value);
                fieldValues.Append("' ");
            }

            string sql = string.Format(
                "INSERT INTO {0} ( {1}{2} ) VALUES ( '{3}'{4} ) "
                , tableName, fields[0].name, fieldNames, fields[0].value, fieldValues);

            return Execute(sql);

        }

        #endregion

        #region 删

        public SqliteDataReader Delete(SQLStatement sqlStatement)
        {
            if (sqlStatement == null)
            {
                return null;
            }

            string sql = string.Format(
                "DELETE FROM {0} WHERE {1}"
                 , sqlStatement.tableName, sqlStatement.GetConditions());

            return Execute(sql);
        }

        public SqliteDataReader DeleteTable(string tableName, bool isDrop = false)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                return null;
            }

            string sql;
            if (isDrop)
            {
                sql = string.Format("DROP TABLE {0}", tableName);
            }
            else
            {
                sql = string.Format("DELETE FROM {0}", tableName);
            }

            return Execute(sql);
        }

        #endregion

        #region 改

        public SqliteDataReader Update(SQLStatement sqlStatement)
        {

            if (sqlStatement == null)
            {
                return null;
            }

            string sql = string.Format(
                "UPDATE {0} SET {1} WHERE {2}"
                , sqlStatement.tableName, sqlStatement.GetUpdateFields(), sqlStatement.GetConditions());

            return Execute(sql);
        }

        #endregion

        #region 查

        public SqliteDataReader Query(SQLStatement sqlStatement)
        {

            if (sqlStatement == null)
            {
                return null;
            }

            StringBuilder sql = new StringBuilder();

            sql.Append(string.Format(
                "SELECT {0} FROM {1}"
                , sqlStatement.GetSelectFields(), sqlStatement.tableName));

            string conditions = sqlStatement.GetConditions();
            if (!string.IsNullOrEmpty(conditions))
            {
                sql.Append(" WHERE ");
                sql.Append(conditions);
            }

            string groupBy = sqlStatement.GetGroupByFields();
            if (!string.IsNullOrEmpty(groupBy))
            {
                sql.Append(" GROUP BY ");
                sql.Append(groupBy);
            }

            string orderBy = sqlStatement.GetOrderByFields();
            if (!string.IsNullOrEmpty(orderBy))
            {
                sql.Append(" ORDER BY ");
                sql.Append(orderBy);
            }

            string limit = sqlStatement.GetLimit();
            if (!string.IsNullOrEmpty(limit))
            {
                sql.Append(" LIMIT ");
                sql.Append(limit);
            }

            return Execute(sql.ToString());

        }

        public bool Query(string tableName, SQLField field)
        {
            if (string.IsNullOrEmpty(tableName) || field == null)
            {
                return false;
            }

            SQLStatement statement = new SQLStatement(tableName);
            statement.SelectFields("*").AddConditions(SQLOperation.EQUAL, SQLUnion.NONE, field);

            SqliteDataReader reader = Query(statement);
            bool hasRows = reader.HasRows;

            CloseReader(reader);

            return hasRows;
        }

        public bool IsTableExist(string tableName)
        {
            string sql = string.Format(
                "SELECT name FROM sqlite_master WHERE type='table' and name='{0}'"
                , tableName);

            SqliteDataReader reader = Execute(sql);
            bool hasRows = reader.HasRows;

            CloseReader(reader);
            return hasRows;
        }

        public bool IsFieldExist(string tableName, string fieldName)
        {
            string sql = string.Format(
                "SELECT * FROM sqlite_master WHERE name='{0}' and sql LIKE '%{1}%'"
                , tableName, fieldName);

            SqliteDataReader reader = Execute(sql);
            bool hasRows = reader.HasRows;

            CloseReader(reader);
            return hasRows;
        }

        #endregion

    }
}

#pragma warning restore XS0001 // Find usages of mono todo items