﻿using Michael.Utility;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Michael.Database
{
    public class Reflection
    {
        public static int Delete(DbConnection connection, string tableName, object source)
        {
            if (source == null)
                throw new ArgumentException("Source cannot be null.");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be set.");

            Type objectType = source.GetType();
            List<string> primaryKeys = GetPrimaryKeyColumns(connection, tableName);

            DbCommand dbCommand = connection.CreateCommand();
            DbParameter dbParameter = null;
            PropertyInfo tempProperty = null;
            string sql = "DELETE FROM \"" + tableName + "\" WHERE ";

            for (int i = 0; i < primaryKeys.Count; i++)
            {
                dbParameter = CreateParameter(objectType, primaryKeys[i], dbCommand);
                tempProperty = objectType.GetProperty(primaryKeys[i]);
                dbParameter.Value = tempProperty.GetValue(source);
                dbCommand.Parameters.Add(dbParameter);
                sql += "\"" + primaryKeys[i] + "\" =  @" + primaryKeys[i] + " ";
            }

            dbCommand.CommandText = sql;
            dbCommand.Prepare();

            return dbCommand.ExecuteNonQuery();
        }

        public static int Update(DbConnection connection, string tableName, object source)
        {
            if (source == null)
                throw new ArgumentException("Source cannot be null.");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be set.");

            Type objectType = source.GetType();
            List<string> primaryKeys = GetPrimaryKeyColumns(connection, tableName);
            List<PropertyInfo> commonDbProperties = FindCommonDbProperties(tableName, connection, objectType);
            List<PropertyInfo> propsWithoutPrimKeys = new List<PropertyInfo>();

            foreach (PropertyInfo property in commonDbProperties)
            {
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    if (!property.Name.Equals(primaryKeys[i], StringComparison.InvariantCultureIgnoreCase))
                        propsWithoutPrimKeys.Add(property);
                }
            }

            DbCommand command = connection.CreateCommand();
            DbParameter dbParameter = null;
            string sql = "UPDATE \""+ tableName + "\" SET ";

            for (int i = 0; i < propsWithoutPrimKeys.Count; i++)
            {
                dbParameter = CreateParameter(objectType, propsWithoutPrimKeys[i].Name, command);
                dbParameter.Value = propsWithoutPrimKeys[i].GetValue(source);
                command.Parameters.Add(dbParameter);

                sql += propsWithoutPrimKeys[i].Name + " = @" + propsWithoutPrimKeys[i].Name;
                if (i != propsWithoutPrimKeys.Count - 1)
                    sql += ", ";
                else
                    sql += " WHERE ";
            }

            PropertyInfo tempProperty = null;
            for (int i = 0; i < primaryKeys.Count; i++)
            {
                dbParameter = CreateParameter(objectType, primaryKeys[i], command);
                tempProperty = objectType.GetProperty(primaryKeys[i]);
                dbParameter.Value = tempProperty.GetValue(source);
                command.Parameters.Add(dbParameter);

                sql += "\"" + primaryKeys[i] + "\" = @" + primaryKeys[i];
                if (i != primaryKeys.Count - 1)
                    sql += ",";
            }
            command.CommandText = sql;
            command.Prepare();

            return command.ExecuteNonQuery();
        }



        public static int Insert(DbConnection connection, string tableName, object source)
        {
            if (source == null)
                throw new ArgumentException("Source cannot be null.");

            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentException("Table name must be set.");

            Type objectType = source.GetType();
            List<string> primaryKeys = GetPrimaryKeyColumns(connection, tableName);
            List<PropertyInfo> commonDbProperties = FindCommonDbProperties(tableName, connection, objectType);
            List<PropertyInfo> propsWithoutPrimKeys = new List<PropertyInfo>();

            foreach (PropertyInfo property in commonDbProperties)
            {
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    if (!property.Name.Equals(primaryKeys[i], StringComparison.InvariantCultureIgnoreCase))
                        propsWithoutPrimKeys.Add(property);
                }
            }

            DbCommand command = connection.CreateCommand();
            DbParameter dbParameter = null;
            string sql1 = "INSERT INTO \"" + tableName + "\" (";
            string sql2 = "VALUES (";

            for (int i = 0; i < propsWithoutPrimKeys.Count; i++)
            {
                dbParameter = CreateParameter(objectType, propsWithoutPrimKeys[i].Name, command);
                dbParameter.Value = propsWithoutPrimKeys[i].GetValue(source) ?? DBNull.Value;
                command.Parameters.Add(dbParameter);

                sql1 += "\""+ propsWithoutPrimKeys[i].Name + "\"";
                sql2 += "@" + propsWithoutPrimKeys[i].Name;
                if (i != propsWithoutPrimKeys.Count - 1)
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

        public static object[] Select(DbConnection connection, string table, string fullClassName, string[] attributes, string[] values, string[] operators)
        {
            if((attributes != null && values == null) || (attributes == null && values != null))
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
            List<PropertyInfo> commonDbProperties = FindCommonDbProperties(table, connection, type);
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
            string sql = "SELECT * FROM \"" + table + "\"";

            if (attributes != null)
            {
                sql += " WHERE ";
                DbParameter dbParameter = null;
                for (int i = 0; i < attributes.Length; i++)
                {
                    sql += "\""+ attributes[i] + "\" " + operators[i] + " @" + attributes[i];
                    if (i != attributes.Length - 1)
                        sql += " AND ";
                }

                for (int i = 0; i < attributes.Length; i++)
                {
                    dbParameter = CreateParameter(type, attributes[i], command);
                    dbParameter.Value = values[i];
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
                        common.SetValue(obj,  dataReader.GetValue(dataReader.GetOrdinal(StringUtility.ToPascalCase(common.Name))));
                    }
                    result.Add(obj);
                }
            }

            return result.ToArray();
        }

        public static DbParameter CreateParameter(Type objectType, string propertyName, DbCommand dbCommand)
        {
            PropertyInfo property = objectType.GetProperty(propertyName);

            if (property == null)
                throw new Exception("Property '" + propertyName + "' not found.");

            DbParameter parameter = dbCommand.CreateParameter();
            parameter.ParameterName = "@" + propertyName;

            if(property.PropertyType == typeof(string) || property.PropertyType == typeof(char[]))
            {
                parameter.DbType = DbType.String;
                parameter.Size = 4000;
            }
            else if (property.PropertyType == typeof(byte[]))
            {
                parameter.DbType = DbType.Binary;
            }
            else if(property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
            {
                parameter.DbType = DbType.Int64;
            }
            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
            {
                parameter.DbType = DbType.Int32;
            }
            else if (property.PropertyType == typeof(short) || property.PropertyType == typeof(short?))
            {
                parameter.DbType = DbType.Int16;
            }
            else if(property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                parameter.DbType = DbType.Boolean;
            }
            else if(property.PropertyType == typeof(byte) || property.PropertyType == typeof(byte?))
            {
                parameter.DbType = DbType.Byte;
            }
            else if(property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
                parameter.DbType = DbType.DateTime;
            }
            else if (property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?))
            {
                parameter.DbType = DbType.DateTimeOffset;
            }
            else if(property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?) || property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                parameter.DbType = DbType.Decimal;
                parameter.Precision = 18;
                parameter.Scale = 3;
            }
            else if(property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
            {
                parameter.DbType = DbType.Double;
            }
            else if(property.PropertyType == typeof(TimeSpan) || property.PropertyType == typeof(TimeSpan?))
            {
                parameter.DbType = DbType.Time;
            }
            else if(property.PropertyType == typeof(sbyte) || property.PropertyType == typeof(sbyte?))
            {
                parameter.DbType = DbType.SByte;
            }
            else if (property.PropertyType == typeof(float) || property.PropertyType == typeof(float?))
            {
                parameter.DbType = DbType.Single;
            }
            else if (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?))
            {
                parameter.DbType = DbType.Guid;
            }
            else if (property.PropertyType == typeof(object))
            {
                parameter.DbType = DbType.Object;
            }
            else if (property.PropertyType == typeof(ulong) || property.PropertyType == typeof(ulong?))
            {
                parameter.DbType = DbType.UInt64;
            }
            else if (property.PropertyType == typeof(uint) || property.PropertyType == typeof(uint?))
            {
                parameter.DbType = DbType.UInt32;
            }
            else if (property.PropertyType == typeof(ushort) || property.PropertyType == typeof(ushort?) || property.PropertyType == typeof(char) || property.PropertyType == typeof(char))
            {
                parameter.DbType = DbType.UInt16;
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

        public static List<PropertyInfo> FindCommonDbProperties(string table, DbConnection connection, Type type)
        {
            List<PropertyInfo> properties = new List<PropertyInfo>(type.GetProperties());
            List<PropertyInfo> commonDbProperties = new List<PropertyInfo>();
            string[] tableColumnNames = GetTableColumnNames(table, connection);

            foreach (PropertyInfo property in properties)
            {
                for (int i = 0; i < tableColumnNames.Length; i++)
                {
                    if(property.Name.Equals(tableColumnNames[i], StringComparison.InvariantCultureIgnoreCase))
                    {
                        commonDbProperties.Add(property);
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
            parameter.Value = table;
            parameter.DbType = System.Data.DbType.String;
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
