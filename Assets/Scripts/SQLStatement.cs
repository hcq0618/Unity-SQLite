// hcq 2017/2/20
//用于构造sql语句
using System.Text;
using System;

#pragma warning disable XS0001 // Find usages of mono todo items

namespace UnitySQLite
{
    public class SQLStatement
    {
        public const int LIMIT_MAX_COUNT = -1;

        string _tableName;
        public string tableName { get { return _tableName; } }

        //create table
        readonly StringBuilder createFields = new StringBuilder();

        //select
        readonly StringBuilder selectFields = new StringBuilder();

        //update
        readonly StringBuilder updateFields = new StringBuilder();

        //条件
        readonly StringBuilder conditions = new StringBuilder();

        //order by
        readonly StringBuilder orderByFields = new StringBuilder();

        //group by
        readonly StringBuilder groupByFields = new StringBuilder();

        //limit
        string limit;

        public SQLStatement(string tableName)
        {
            _tableName = tableName;
        }

        #region create table

        public SQLStatement CreateFields(params SQLField[] fields)
        {
            if (!(fields == null || fields.Length <= 0))
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(createFields.ToString()))
                    {
                        createFields.Append(",");
                    }

                    createFields.Append(fields[i].name);
                    createFields.Append(" ");
                    createFields.Append(fields[i].typeForSQL);

                    if (fields[i].isPrimaryKey)
                    {
                        createFields.Append(" PRIMARY KEY");
                    }
                }
            }

            return this;
        }

        protected internal string GetCreateFields()
        {
            return createFields.ToString();
        }

        #endregion

        #region select

        public SQLStatement SelectFields(params string[] fieldNames)
        {
            if (fieldNames != null)
            {
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(selectFields.ToString()))
                    {
                        selectFields.Append(",");
                    }

                    selectFields.Append(fieldNames[i]);
                }
            }

            return this;
        }

        public SQLStatement SelectFields(params SQLField[] fields)
        {
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(selectFields.ToString()))
                    {
                        selectFields.Append(",");
                    }

                    selectFields.Append(fields[i].name);
                }
            }

            return this;
        }

        protected internal string GetSelectFields()
        {
            return selectFields.ToString();
        }

        protected internal string[] GetSelectFieldArray()
        {
            string fields = selectFields.ToString();
            if (!string.IsNullOrEmpty(fields))
            {
                return fields.Trim().Split(',');
            }

            return null;
        }

        #endregion

        #region update

        public SQLStatement UpateFields(params SQLField[] fields)
        {
            if (!(fields == null || fields.Length <= 0))
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(updateFields.ToString()))
                    {
                        updateFields.Append(",");
                    }

                    updateFields.Append(fields[i].name);
                    updateFields.Append(" = '");
                    updateFields.Append(fields[i].value);
                    updateFields.Append("'");
                }
            }

            return this;
        }

        protected internal string GetUpdateFields()
        {
            return updateFields.ToString();
        }

        #endregion

        #region condition
        //添加条件语句
        public SQLStatement AddConditions(SQLOperation operation, SQLUnion union, params SQLField[] fields)
        {
            if (fields != null)
            {

                string unionStatement;
                switch (union)
                {
                    case SQLUnion.OR:
                        unionStatement = " OR ";
                        break;
                    case SQLUnion.AND:
                        unionStatement = " AND ";
                        break;
                    default:
                        unionStatement = "";
                        break;
                }

                string operateStatement;
                switch (operation)
                {
                    case SQLOperation.BETWEEN:
                        operateStatement = " BETWEEN ";
                        break;
                    case SQLOperation.LIKE:
                        operateStatement = " LIKE ";
                        break;
                    case SQLOperation.UNEQUAL:
                        operateStatement = " <> ";
                        break;
                    case SQLOperation.GREATER:
                        operateStatement = " > ";
                        break;
                    case SQLOperation.LESS:
                        operateStatement = " < ";
                        break;
                    case SQLOperation.GREATER_EQUAL:
                        operateStatement = " >= ";
                        break;
                    case SQLOperation.LESS_EQUAL:
                        operateStatement = " <= ";
                        break;
                    default:
                        operateStatement = " = ";
                        break;
                }

                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(conditions.ToString()))
                    {
                        conditions.Append(unionStatement);
                    }

                    conditions.Append(fields[i].name);
                    conditions.Append(operateStatement);

                    if (operation == SQLOperation.LIKE)
                    {
                        conditions.Append("'%");
                    }
                    else
                    {
                        conditions.Append("'");
                    }

                    conditions.Append(fields[i].value);

                    if (operation == SQLOperation.LIKE)
                    {
                        conditions.Append("%'");
                    }
                    else
                    {
                        conditions.Append("'");
                    }
                }

            }

            return this;
        }

        protected internal string GetConditions()
        {
            return conditions.ToString();
        }

        #endregion

        #region order by
        //添加排序语句
        public SQLStatement OrderByFields(SQLOrderBy orderBy, params SQLField[] fields)
        {
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(orderByFields.ToString()))
                    {
                        orderByFields.Append(",");
                    }

                    orderByFields.Append(fields[i].name);

                    switch (orderBy)
                    {
                        case SQLOrderBy.DESC:
                            orderByFields.Append(" DESC");
                            break;
                        default:
                            orderByFields.Append(" ASC");
                            break;
                    }
                }
            }

            return this;
        }

        protected internal string GetOrderByFields()
        {
            return orderByFields.ToString();
        }

        #endregion

        #region group by
        //添加分组语句
        public SQLStatement GroupByFields(params SQLField[] fields)
        {
            if (fields != null)
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!string.IsNullOrEmpty(groupByFields.ToString()))
                    {
                        groupByFields.Append(",");
                    }

                    groupByFields.Append(fields[i].name);
                }
            }

            return this;
        }

        protected internal string GetGroupByFields()
        {
            return groupByFields.ToString();
        }

        #endregion

        #region limit
        //添加限制语句 
        //count传-1或LIMIT_MAX_COUNT 表示为了检索从某一个偏移量到记录集的结束所有的记录行
        public SQLStatement Limit(int offset, int count)
        {
            limit = string.Format("{0},{1}", offset, count);
            return this;
        }

        public SQLStatement Limit(int count)
        {
            limit = Convert.ToString(count);
            return this;
        }

        protected internal string GetLimit()
        {
            return limit;
        }

        #endregion
    }
}

namespace UnitySQLite
{
    public enum SQLOperation
    {
        //=
        EQUAL,
        //<>不等于
        UNEQUAL,
        //大于
        GREATER,
        //小于
        LESS,
        //大于等于
        GREATER_EQUAL,
        //小于等于
        LESS_EQUAL,
        //在某个范围内
        BETWEEN,
        //like
        LIKE

    }
}

namespace UnitySQLite
{
    public enum SQLUnion
    {
        OR, AND, NONE
    }
}

namespace UnitySQLite
{
    public enum SQLOrderBy
    {
        //倒序
        DESC,
        //顺序
        ASC
    }
}

#pragma warning restore XS0001 // Find usages of mono todo items