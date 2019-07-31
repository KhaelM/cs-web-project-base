using Michael.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Michael.Database
{
    public enum Case
    {
        camelCase,
        pascalCase,
        kebabCase
    }

    public class Reflection
    {
        public static DbColumn FindColumn(DbConnection connection, string tableName, string columnName)
        {
            DbCommand dbCommand = connection.CreateCommand();
            dbCommand.CommandText = "SELECT * FROM " + tableName;
            DbDataReader dbDataReader = dbCommand.ExecuteReader(CommandBehavior.KeyInfo);
            DataTable schemaTable = dbDataReader.GetSchemaTable();

            foreach (DataRow row in schemaTable.Rows)
            {
                if (row["ColumnName"].ToString().Equals(columnName))
                {
                    DbColumn result = new DbColumn();
                    object[] res = row.ItemArray;
                    result.ColumnName = (string)res[0];
                    result.ColumnOrdinal = Convert.ToInt32(res[1]);
                    result.ColumnSize = Convert.ToInt32(res[2]);
                    result.NumericPrecision = Convert.ToInt32(res[3]);
                    result.NumericScale = Convert.ToInt32(res[4]);
                    for (int j = 0; j < res.Length; j++)
                    {
                        if(res[j].GetType().FullName.Equals("System.RuntimeType"))
                        {
                            result.DataType = (Type)res[j];
                            break;
                        }
                    }
                    dbDataReader.Close();
                    return result;
                }
            }
            throw new Exception("Attribute " + columnName + " not found in table " + tableName+".");
        }

        public static int Delete(DbConnection connection, string tableName, Dictionary<string, object> where, Case @dbCase, char? dbEscapeCharacter = null)
        {
            if (dbCase != Case.kebabCase && dbEscapeCharacter == null)
                throw new ArgumentException("You must indicate database escaping character if it's not kebab case.");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be set.");

            if (where == null || where.Count == 0)
                throw new ArgumentException("Where clause must be set");

            DbCommand command = connection.CreateCommand();
            DbParameter dbParameter = null;
            string sql = "DELETE FROM " + tableName + " WHERE ";

            int i = 0;
            foreach (var item in where)
            {
                dbParameter = CreateParameter(connection, tableName, item.Key, command, item.Value, item.Key);
                command.Parameters.Add(dbParameter);

                if (dbEscapeCharacter != null)
                    sql += dbEscapeCharacter;
                sql += item.Key;
                if (dbEscapeCharacter != null)
                    sql += dbEscapeCharacter;

                if (dbParameter.Value != DBNull.Value)
                    sql += " = @" + item.Key;
                else
                    sql += " IS NULL";

                if (i != where.Count - 1)
                    sql += " AND ";
                i++;
            }

            command.CommandText = sql;
            command.Prepare();

            return command.ExecuteNonQuery();
        }

        public static int Update(DbConnection connection, string tableName, Dictionary<string, object> updates, Dictionary<string, object> where, Case @dbCase, char? dbEscapeCharacter = null)
        {
            if (dbCase != Case.kebabCase && dbEscapeCharacter == null)
                throw new ArgumentException("You must indicate database escaping character if it's not kebab case.");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be set.");

            if (updates.Count == 0 || updates == null)
                throw new ArgumentException("There is no attribute/values to update.");

            if (where.Count == 0 || where == null)
                throw new ArgumentException("No criteria for update.");

            DbCommand command = connection.CreateCommand();
            DbParameter dbParameter = null;
            string sql = "UPDATE "+ tableName.ToLower() + " SET ";


            int i = 0;
            foreach (var item in updates)
            {
                dbParameter = CreateParameter(connection, tableName, item.Key, command, item.Value, item.Key);
                command.Parameters.Add(dbParameter);

                if (dbEscapeCharacter != null)
                    sql += dbEscapeCharacter;
                sql += item.Key;
                if (dbEscapeCharacter != null)
                    sql += dbEscapeCharacter;

                sql += " = @" + item.Key;
                if (i != updates.Count - 1)
                    sql += ", ";
                else
                    sql += " WHERE ";
                i++;
            }

            i = 0;
            foreach (var item in where)
            {
                dbParameter = CreateParameter(connection, tableName, item.Key, command, item.Value, "where"+item.Key);
                command.Parameters.Add(dbParameter);

                if (dbEscapeCharacter != null)
                    sql += dbEscapeCharacter;
                sql += item.Key;
                if (dbEscapeCharacter != null)
                    sql += dbEscapeCharacter;

                if (dbParameter.Value != DBNull.Value)
                    sql += " = @where" + item.Key;
                else
                    sql += " IS NULL";

                if (i != where.Count - 1)
                    sql += " AND ";
                i++;
            }
            command.CommandText = sql;
            command.Prepare();

            return command.ExecuteNonQuery();
        }

        public static int Insert(DbConnection connection, string tableName, object source, Case @classCase, Case @dbCase, char? dbEscapeCharacter = null)
        {
            if (dbCase != Case.kebabCase && dbEscapeCharacter == null)
                throw new ArgumentException("You must indicate database escaping character if it's not kebab case.");

            if (source == null)
                throw new ArgumentException("Source cannot be null.");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be set.");

            Type objectType = source.GetType();
            List<PropertyInfo> commonDbProperties = FindCommonDbProperties(tableName, connection, objectType, classCase, dbCase);

            DbCommand command = connection.CreateCommand();
            DbParameter dbParameter = null;
            string sql1 = "INSERT INTO " + tableName + " (";
            string sql2 = "VALUES (";

            for (int i = 0; i < commonDbProperties.Count; i++)
            {
                if (dbEscapeCharacter != null)
                    sql1 += dbEscapeCharacter;


                if (classCase == dbCase)
                {
                    dbParameter = CreateParameter(connection, tableName, commonDbProperties[i].Name, command, commonDbProperties[i].GetValue(source), commonDbProperties[i].Name);
                    sql1 += commonDbProperties[i].Name;
                    sql2 += "@" + commonDbProperties[i].Name;
                }
                else
                {
                    if(classCase == Case.camelCase)
                    {
                        if(dbCase == Case.pascalCase)
                        {
                            dbParameter = CreateParameter(connection, tableName, StringUtility.FromCamelToPascal(commonDbProperties[i].Name), command, commonDbProperties[i].GetValue(source), StringUtility.FromCamelToPascal(commonDbProperties[i].Name));
                            sql1 += StringUtility.FromCamelToPascal(commonDbProperties[i].Name);
                            sql2 += "@" +StringUtility.FromCamelToPascal(commonDbProperties[i].Name);
                        }
                        else
                        {
                            dbParameter = CreateParameter(connection, tableName, StringUtility.FromCamelToKebab(commonDbProperties[i].Name), command, commonDbProperties[i].GetValue(source), StringUtility.FromCamelToKebab(commonDbProperties[i].Name));
                            sql1 += StringUtility.FromCamelToKebab(commonDbProperties[i].Name);
                            sql2 += "@" + StringUtility.FromCamelToKebab(commonDbProperties[i].Name);
                        }
                    }
                    else if(classCase == Case.pascalCase)
                    {
                        if (dbCase == Case.camelCase)
                        {
                            dbParameter = CreateParameter(connection, tableName, StringUtility.FromPascalToCamel(commonDbProperties[i].Name), command, commonDbProperties[i].GetValue(source), StringUtility.FromPascalToCamel(commonDbProperties[i].Name));
                            sql1 += StringUtility.FromPascalToCamel(commonDbProperties[i].Name);
                            sql2 += "@" + StringUtility.FromPascalToCamel(commonDbProperties[i].Name);
                        }
                        else
                        {
                            dbParameter = CreateParameter(connection, tableName, StringUtility.FromPascalToKebab(commonDbProperties[i].Name), command, commonDbProperties[i].GetValue(source), StringUtility.FromPascalToKebab(commonDbProperties[i].Name));
                            sql1 += StringUtility.FromPascalToKebab(commonDbProperties[i].Name);
                            sql2 += "@" + StringUtility.FromPascalToKebab(commonDbProperties[i].Name);
                        }
                    }
                    else
                    {
                        if (dbCase == Case.camelCase)
                        {
                            dbParameter = CreateParameter(connection, tableName, StringUtility.FromKebabToCamel(commonDbProperties[i].Name), command, commonDbProperties[i].GetValue(source), StringUtility.FromKebabToCamel(commonDbProperties[i].Name));
                            sql1 += StringUtility.FromKebabToCamel(commonDbProperties[i].Name);
                            sql2 += "@" + StringUtility.FromKebabToCamel(commonDbProperties[i].Name);
                        }
                        else
                        {
                            dbParameter = CreateParameter(connection, tableName, StringUtility.FromKebabToPascal(commonDbProperties[i].Name), command, commonDbProperties[i].GetValue(source), StringUtility.FromKebabToPascal(commonDbProperties[i].Name));
                            sql1 += StringUtility.FromKebabToPascal(commonDbProperties[i].Name);
                            sql2 += "@" + StringUtility.FromKebabToPascal(commonDbProperties[i].Name);
                        }
                    }
                }

                command.Parameters.Add(dbParameter);


                if (dbEscapeCharacter != null)
                    sql1 += dbEscapeCharacter;

                
                if (i != commonDbProperties.Count - 1)
                {
                    sql1 += ",";
                    sql2 += ",";
                }
                else
                {
                    sql1 += ") ";
                    sql2 += ")";
                }
            }

            command.CommandText = sql1 + sql2;
            command.Prepare();

            return command.ExecuteNonQuery();
        }

        public static object[] Select(DbConnection connection, string table, string fullClassName, Case @classCase, Case @dbCase, char? dbEscapeCharacter = null, string[] attributes = null, object[] values = null, string[] operators = null)
        {
            if (dbCase != Case.kebabCase && dbEscapeCharacter == null)
                throw new ArgumentException("You must indicate database escaping character if it's not kebab case.");

            if ((attributes != null && values == null) || (attributes == null && values != null))
                throw new ArgumentException("Attributes and values must be set together or null together");

            if (attributes != null && attributes.Length != values.Length)
                throw new ArgumentException("Number of attributes or values exceed. " + attributes.Length + " attributes and " + values.Length + " values.");

            Type type = Type.GetType(fullClassName);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(fullClassName);
                if (type != null)
                    break;
            }

            if (type == null)
                throw new Exception("Type not found even in assemblies - Type name : " + fullClassName);

            ConstructorInfo constructor = null;
            ParameterInfo[] ctorParameters = null;
            string[] ctorParamNames = null;
            List<PropertyInfo> readAndWriteProperties = new List<PropertyInfo>();
            List<PropertyInfo> readOnlyProperties = FindReadOnlyProperties(type);
            List<PropertyInfo> commonDbProperties = FindCommonDbProperties(table, connection, type, classCase, dbCase);
            string[] tableColumnsNames = GetTableColumnNames(table, connection);
            List<object> result = new List<object>();

            // Remove RO properties from RAW properties
            foreach (PropertyInfo readOnlyProperty in readOnlyProperties)
            {
                readAndWriteProperties.Remove(readOnlyProperty);
            }

            // Find the first constructor which use all of the RO Properties
            if (readOnlyProperties.Count != 0)
            {
                constructor = FirstConstructorUsingProperties(type, readOnlyProperties);
                ctorParameters = constructor.GetParameters();
                ctorParamNames = new string[ctorParameters.Length];
                for (int i = 0; i < ctorParameters.Length; i++)
                {
                    ctorParamNames[i] = ctorParameters[i].Name;
                }
            }

            DbCommand command = connection.CreateCommand();
            string sql = "SELECT * FROM "+ table;

            if (attributes != null)
            {
                sql += " WHERE ";
                DbParameter dbParameter = null;
                for (int i = 0; i < attributes.Length; i++)
                {
                    if (dbEscapeCharacter != null)
                        sql += dbEscapeCharacter;
                    sql += attributes[i];
                    if (dbEscapeCharacter != null)
                        sql += dbEscapeCharacter;

                    sql += " " + operators[i] + " @" + attributes[i];
                    if (i != attributes.Length - 1)
                        sql += " AND ";
                }

                for (int i = 0; i < attributes.Length; i++)
                {
                    dbParameter = CreateParameter(connection, table, attributes[i], command, values[i], attributes[i]);
                    command.Parameters.Add(dbParameter);
                }
            }
            command.CommandText = sql;
            command.Prepare();

            DbDataReader dataReader = command.ExecuteReader();
            object obj = null;

            if(readOnlyProperties.Count != 0)
            {
                List<object> argumentsValue = new List<object>();
                object value = null;
                
                while (dataReader.Read())
                {
                    argumentsValue = new List<object>();
                    for (int i = 0; i < ctorParamNames.Length; i++)
                    {
                        value = dataReader.GetValue(dataReader.GetOrdinal(ctorParamNames[i]));
                        if (!(value is System.DBNull))
                            argumentsValue.Add(Convert.ChangeType(value, Nullable.GetUnderlyingType(ctorParameters[i].ParameterType) ?? ctorParameters[i].ParameterType));
                        else
                            argumentsValue.Add(null);
                    }

                    obj = constructor.Invoke(argumentsValue.ToArray());

                    foreach (PropertyInfo common in commonDbProperties)
                    {
                        value = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.ToPascalCase(common.Name)));
                        if (!readOnlyProperties.Contains(common))
                        {
                            if (value is System.DBNull)
                                value = null;

                            common.SetValue(obj, value);
                        }
                    }

                    result.Add(obj);
                }
            }
            else
            {
                while (dataReader.Read())
                {
                    obj = Activator.CreateInstance(type);
                    foreach (PropertyInfo common in commonDbProperties)
                    {
                        object fromDatabaseValue = null;
                        if(classCase == Case.camelCase)
                        {
                            if(dbCase == Case.camelCase)
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(common.Name));
                            }
                            else if(dbCase == Case.pascalCase)
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.FromCamelToPascal(common.Name)));
                            }
                            else
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.FromCamelToKebab(common.Name)));
                            }
                        }
                        else if(classCase == Case.pascalCase)
                        {
                            if(dbCase == Case.camelCase)
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.FromPascalToCamel(common.Name)));
                            }
                            else if(dbCase == Case.pascalCase)
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(common.Name));
                            }
                            else
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.FromPascalToKebab(common.Name)));
                            }
                        }
                        else
                        {
                            if (dbCase == Case.camelCase)
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.FromKebabToCamel(common.Name)));
                            }
                            else if (dbCase == Case.pascalCase)
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(StringUtility.FromKebabToPascal(common.Name)));
                            }
                            else
                            {
                                fromDatabaseValue = dataReader.GetValue(dataReader.GetOrdinal(common.Name));
                            }
                        }
                        if (fromDatabaseValue != DBNull.Value)
                            common.SetValue(obj, fromDatabaseValue);
                        else
                            common.SetValue(obj, null);
                    }
                    result.Add(obj);
                }
            }
            dataReader.Close();

            return result.ToArray();
        }

        public static DbParameter CreateParameter(DbConnection connection, string tableName, string columnName, DbCommand dbCommand, object value, string parameterName)
        {
            DbColumn column = FindColumn(connection, tableName, columnName);

            DbParameter parameter = dbCommand.CreateParameter();
            parameter.ParameterName = "@" + parameterName;
            parameter.Size = column.ColumnSize.Value;
            parameter.Precision = (byte) column.NumericPrecision.Value;
            parameter.Scale = (byte) column.NumericScale.Value;

            if (column.DataType == typeof(string) ||column.DataType == typeof(char[]))
            {
                parameter.DbType = DbType.String;
                parameter.Value = (value == null) ? DBNull.Value : (object) (string)value;
            }
            else if (column.DataType == typeof(byte[]))
            {
                parameter.DbType = DbType.Binary;
                parameter.Value = (value == null) ? DBNull.Value : (object) (byte[])value;
            }
            else if(column.DataType == typeof(long) || column.DataType == typeof(long?))
            {
                parameter.DbType = DbType.Int64;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? long.Parse((string)value) : (object)Convert.ToInt64(value);

            }
            else if (column.DataType == typeof(int) || column.DataType == typeof(int?))
            {
                parameter.DbType = DbType.Int32;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? int.Parse((string)value) : (object)Convert.ToInt32(value);
            }
            else if (column.DataType == typeof(short) || column.DataType == typeof(short?))
            {
                parameter.DbType = DbType.Int16;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? short.Parse((string)value) : (object) Convert.ToInt16(value);
            }
            else if(column.DataType == typeof(bool) || column.DataType == typeof(bool?))
            {
                parameter.DbType = DbType.Boolean;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? bool.Parse((string)value) : (object) Convert.ToBoolean(value);
            }
            else if(column.DataType == typeof(byte) || column.DataType == typeof(byte?))
            {
                parameter.DbType = DbType.Byte;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? byte.Parse((string)value) : (object) Convert.ToByte(value);
            }
            else if(column.DataType == typeof(DateTime) || column.DataType == typeof(DateTime?))
            {
                parameter.DbType = DbType.DateTime;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? DateTime.Parse((string)value) : (object) Convert.ToDateTime(value);
            }
            else if (column.DataType == typeof(DateTimeOffset) || column.DataType == typeof(DateTimeOffset?))
            {
                parameter.DbType = DbType.DateTimeOffset;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? DateTimeOffset.Parse((string)value) : (object) (DateTimeOffset)value;
            }
            else if(column.DataType == typeof(decimal) || column.DataType == typeof(decimal?))
            {
                parameter.DbType = DbType.Decimal;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? decimal.Parse((string)value) : (object) Convert.ToDecimal(value);
            }
            else if(column.DataType == typeof(double) || column.DataType == typeof(double?))
            {
                parameter.DbType = DbType.Double;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? double.Parse((string)value) : (object) Convert.ToDouble(value);
            }
            else if(column.DataType == typeof(TimeSpan) || column.DataType == typeof(TimeSpan?))
            {
                parameter.DbType = DbType.Time;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? TimeSpan.Parse((string)value) : (object) (TimeSpan)value;
            }
            else if(column.DataType == typeof(sbyte) || column.DataType == typeof(sbyte?))
            {
                parameter.DbType = DbType.SByte;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? sbyte.Parse((string)value) : (object) Convert.ToSByte(value);
            }
            else if (column.DataType == typeof(float) || column.DataType == typeof(float?))
            {
                parameter.DbType = DbType.Single;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? float.Parse((string)value) : (object) (float)value;
            }
            else if (column.DataType == typeof(Guid) || column.DataType == typeof(Guid?))
            {
                parameter.DbType = DbType.Guid;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? Guid.Parse((string)value) : (object) (Guid)value;
            }
            else if (column.DataType == typeof(object))
            {
                parameter.DbType = DbType.Object;
                parameter.Value = (value == null) ? DBNull.Value : value;
            }
            else if (column.DataType == typeof(ulong) || column.DataType == typeof(ulong?))
            {
                parameter.DbType = DbType.UInt64;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? ulong.Parse((string)value) : (object) Convert.ToUInt64(value);
            }
            else if (column.DataType == typeof(uint) || column.DataType == typeof(uint?))
            {
                parameter.DbType = DbType.UInt32;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? uint.Parse((string)value) : (object) Convert.ToUInt32(value);
            }
            else if (column.DataType == typeof(ushort) || column.DataType == typeof(ushort?) || column.DataType == typeof(char) || column.DataType == typeof(char))
            {
                parameter.DbType = DbType.UInt16;
                parameter.Value = (value == null) ? DBNull.Value : value is string ? ushort.Parse((string)value) : (object) Convert.ToUInt16(value);
            }

            return parameter;
        }

        public static List<string> GetPrimaryKeyColumns(DbConnection connection, string tableName)
        {
            List<string> result = new List<string>();
            DbCommand command = connection.CreateCommand();
            string[] restrictions = new string[] { null, null, tableName };
            DataTable table = connection.GetSchema("IndexColumns", restrictions);

            if (string.IsNullOrEmpty(tableName))
                throw new Exception("Table name must be set.");

            foreach (DataRow row in table.Rows)
            {
                result.Add(row["column_name"].ToString());
            }

            return result;
        }

        public static List<string> GetPrimaryKeyColumns(DbConnection conn, string schema, string table)
        {
            DbCommand cmd = conn.CreateCommand();

            DbParameter p = cmd.CreateParameter();
            p.ParameterName = "@schema";
            p.Value = schema;
            p.DbType = DbType.String;
            p.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(p);

            p = cmd.CreateParameter();
            p.ParameterName = "@table";
            p.Value = table;
            p.DbType = DbType.String;
            p.Direction = ParameterDirection.Input;
            cmd.Parameters.Add(p);

            cmd.CommandText = @"SELECT kcu.COLUMN_NAME
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS as tc
                        LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE as  kcu
                            ON kcu.CONSTRAINT_CATALOG = tc.CONSTRAINT_CATALOG
                               AND kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
                               AND kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                               -- AND kcu.TABLE_CATALOG = tc.TABLE_CATALOG  doesn't work on MySQL
                               AND kcu.TABLE_SCHEMA = tc.TABLE_SCHEMA
                               AND kcu.TABLE_NAME = tc.TABLE_NAME
                        WHERE tc.CONSTRAINT_TYPE ='PRIMARY KEY'
                              AND tc.TABLE_SCHEMA = @schema
                              AND tc.TABLE_NAME = @table
                        ORDER BY ORDINAL_POSITION";

            DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);

            List<string> res = new List<string>();
            while (reader.Read())
            {
                var str = reader[0];
                if (str != DBNull.Value)
                    res.Add((string)str);
            }
            reader.Dispose();
            cmd.Dispose();
            return res;
        }

        public static List<PropertyInfo> FindCommonDbProperties(string table, DbConnection connection, Type type, Case @classCase,  Case @dbCase)
        {
            if(classCase < Case.camelCase || classCase > Case.kebabCase)
            {
                throw new ArgumentException("Incorrect case reference for class case.");
            }

            if (dbCase < Case.camelCase || dbCase > Case.kebabCase)
            {
                throw new ArgumentException("Incorrect case reference for database case.");
            }

            List<PropertyInfo> properties = new List<PropertyInfo>(type.GetProperties());
            List<PropertyInfo> commonDbProperties = new List<PropertyInfo>();
            string[] tableColumnNames = GetTableColumnNames(table, connection);

            foreach (PropertyInfo property in properties)
            {
                for (int i = 0; i < tableColumnNames.Length; i++)
                {
                    if(classCase == Case.camelCase)
                    {
                        if(dbCase == Case.camelCase)
                        {
                            if (property.Name.Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                        else if(dbCase == Case.pascalCase)
                        {
                            if (StringUtility.FromCamelToPascal(property.Name).Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                        else
                        {
                            if (StringUtility.FromCamelToKebab(property.Name).Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                    }
                    else if(classCase == Case.pascalCase)
                    {
                        if (dbCase == Case.camelCase)
                        {
                            if (StringUtility.FromPascalToCamel(property.Name).Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                        else if (dbCase == Case.pascalCase)
                        {
                            if (property.Name.Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                        else
                        {
                            if (StringUtility.FromPascalToKebab(property.Name).Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                    }
                    else
                    {
                        if (dbCase == Case.camelCase)
                        {
                            if (StringUtility.FromKebabToCamel(property.Name).Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                        else if (dbCase == Case.pascalCase)
                        {
                            if (StringUtility.FromKebabToPascal(property.Name).Equals(tableColumnNames[i]))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                        else
                        {
                            if (property.Name.Equals(tableColumnNames[i], StringComparison.InvariantCultureIgnoreCase))
                            {
                                commonDbProperties.Add(property);
                            }
                        }
                    }
                }
            }

            return commonDbProperties;
        }

        public static List<PropertyInfo> FindReadOnlyProperties(Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>(type.GetProperties());
            List<PropertyInfo> readOnlyProperties = new List<PropertyInfo>();
            MethodInfo setter = null;

            foreach (PropertyInfo property in properties)
            {
                setter = property.GetSetMethod();

                if (setter == null)
                    readOnlyProperties.Add(property);
            }
            return readOnlyProperties;
        }

        public static ConstructorInfo FirstConstructorUsingProperties(Type type, List<PropertyInfo> readOnlyProperties)
        {
            ConstructorInfo[] constructors = type.GetConstructors();
            ParameterInfo[] parameters = null;
            string[] propertyNames = new string[readOnlyProperties.Count];
            string[] parameterNames = null;

            for (int i = 0; i < readOnlyProperties.Count; i++)
            {
                propertyNames[i] = readOnlyProperties[i].Name;
            }
            
            foreach (ConstructorInfo ctor in constructors)
            {
                parameters = ctor.GetParameters();
                parameterNames = new string[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameterNames[i] = parameters[i].Name;
                }

                if (StringUtility.IsListIncluded(propertyNames, parameterNames))
                    return ctor;
            }
            return null;
        }

        public static string[] GetTableColumnNames(string table, DbConnection connection)
        {
            List<string> columnNames = new List<string>();
            DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME= @table";
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = "@table";
            parameter.Value = table.ToLower(); // Postgresql lower TABLE_NAME to table_name
            parameter.DbType = DbType.String;
            parameter.Size = 4000;
            command.Parameters.Add(parameter);
            command.Prepare();

            DbDataReader dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                columnNames.Add(dataReader.GetString(0));
            }

            dataReader.Close();
            return columnNames.ToArray();
        }
    }
}
