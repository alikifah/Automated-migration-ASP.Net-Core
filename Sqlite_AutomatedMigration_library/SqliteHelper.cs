//=============================================================================================================================================
//  Author: Al-Khafaji, Ali Kifah
//  Date:   16.06.2022
//  Description: A class, that manages SQL queries to Database throught Sqlite database-engine
//  =============================================================================================================================================
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

namespace SqliteHelper
{
    public enum ColumnType
    {
        INTEGER,
        TEXT,
        BLOB,
        REAL,
        NUMERIC,
        UNDEFINED
    }
    public class Column
    {
        public string Name { get; set; }
        public ColumnType Type { get; set; }
        public string NotNull { get; set; }
        public Column(string name, ColumnType type, string notNull = "NOT NULL")
        {
            Name = name;
            Type = type;
            NotNull = notNull;
        }
    }

    public class SqliteManager
    {
        private string datasource = "";
        public SqliteManager(string datasource)
        {
            this.datasource = datasource;
        }

        /// <summary>
        /// Main method to Create a Table For a model automatically using Reflection
        /// </summary>
        /// <typeparam name="Model">the entity you want to create the table for, this class must contain only one [Key] attribute</typeparam>
        /// <param name="tablename">name of the table for the entity stored in the data source, this must be the same name as the DbSet property in the DbContext class associated with the model</param>
        public void CreateTable<Model>(string tablename) where Model : new() 
        {
            string notNull = "";
            ColumnType ct;
            string key = GetKeyProperty<Model>();
            List<string> required =  GetRequiredProperties<Model>() ;
            List<string> columnsAlreadyInDBTable = new List<string>();
            if (isTableExist(tablename))
                columnsAlreadyInDBTable.AddRange(GetColumns(tablename));

            var m = new Model();
            List<Column> columnsInModel = new List<Column>();
            PropertyInfo[] properties = m.GetType().GetProperties();
            foreach (var p in properties)
            {
                var t = GetPropertyType<Model>(p);
                var n = GetPropertyName<Model>(p);
                var v = GetPropertyValue<Model>(p);

                if (t == typeof(string))
                {
                    ct = ColumnType.TEXT;
                    if (required.Contains(n) )
                        notNull = "NOT NULL";
                    else
                        notNull = "";
                }
                else if (t == typeof(DateTime))
                {
                    ct = ColumnType.TEXT;
                    notNull = "NOT NULL";
                }
                else if (t == typeof(int) || t == typeof(long) || t == typeof(byte))
                {
                    ct = ColumnType.INTEGER;
                    notNull = "NOT NULL";
                }
                else if (t == typeof(float) || t == typeof(double))
                {
                    ct = ColumnType.REAL;
                    notNull = "NOT NULL";
                }
                else if (t == typeof(bool))
                {
                    ct = ColumnType.NUMERIC;
                    notNull = "NOT NULL";
                }
                else ct = ColumnType.UNDEFINED;

                if (ct!= ColumnType.UNDEFINED)
                    columnsInModel.Add(new Column(n, ct, notNull) );
            }

            // delete the columns that are not found in models
            List<string> columnsNotInModel = new List<string>();
            List<string> namesOfColumnsInModel = new List<string>();
            foreach (var p in columnsInModel)
                namesOfColumnsInModel.Add(p.Name);
            foreach (string columnInDB in  columnsAlreadyInDBTable)
            {
                if (!namesOfColumnsInModel.Contains(columnInDB) )
                    columnsNotInModel.Add (columnInDB);
            }
            foreach (var vvv in columnsNotInModel)
                DeleteColumn(tablename, vvv);

            CreateTable(tablename, key, columnsInModel);
        }

        //################################################################################################################
        public bool isTableExist(string tablename)
        {
            using (var con = new SqliteConnection(@"Data Source=" + datasource))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT count(*) FROM " + "sqlite_master" + " WHERE type='table' AND name='" + tablename + "'; ";
                    long result = (long)cmd.ExecuteScalar();
                    if (result == 1)
                        return true;
                }
                catch
                {
                }
                return false;
            }
        }
        private bool isColumnExist(string tableName, string columnName)
        {
            using (var con = new SqliteConnection(@"Data Source=" + datasource))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = string.Format("PRAGMA table_info({0})", tableName);
                    var reader = cmd.ExecuteReader();
                    int nameIndex = reader.GetOrdinal("Name");
                    while (reader.Read())
                    {
                        if ((reader.GetString(nameIndex).ToLower()).Equals(columnName.ToLower()) )
                            return true;
                    }
                }
                catch
                {
                }
            }
            return false;
        }
        private string[] GetColumns(string tableName)
        {
            List<string> columns = new List<string>();
            using (var con = new SqliteConnection(@"Data Source=" + datasource))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = string.Format("PRAGMA table_info({0})", tableName);
                    var reader = cmd.ExecuteReader();
                    int nameIndex = reader.GetOrdinal("Name");
                    while (reader.Read())
                    {
                        columns.Add(reader.GetString(nameIndex));
                    }
                }
                catch
                {
                }
            }
            return columns.ToArray();
        }
        private void DeleteTable(string tablename)
        {
            string cs = @"Data Source=" + datasource;
            using (var con = new SqliteConnection(cs))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = "DROP TABLE IF EXISTS " + tablename;
                cmd.ExecuteNonQuery();
            }
        }


        private void DeleteColumn(string tablename, string columnName)
        {
            if (!isTableExist(tablename)) return;
            string cs = @"Data Source=" + datasource;
            using (var con = new SqliteConnection(cs))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = @"ALTER TABLE " + tablename + " " + "DROP " + columnName ;
                    cmd.ExecuteNonQuery();

                }
                catch
                {
                    Console.WriteLine("error ");
                }
            }
        }

        private void CreateTable(string tablename, string key, List<Column> columns)
        {
            string cs = @"Data Source=" + datasource;
            using (var con = new SqliteConnection(cs))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS " + tablename + "(" +  key + " INTEGER PRIMARY KEY NOT NULL)";
                cmd.ExecuteNonQuery();

                foreach (Column col in columns)
                {
                    if (!isColumnExist(tablename, col.Name))
                    {
                        cmd.CommandText = @"ALTER TABLE " + tablename + " " + "ADD " + col.Name + " " + col.Type + " " + col.NotNull;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        //#############################################################################################

        private static List<string> GetRequiredProperties<Model>() where Model : new()
        {
            Model instance = new Model();
            var attrType = typeof(RequiredAttribute);
            List<string> requiredProperties = new List<string>();
            PropertyInfo[] properties = instance.GetType().GetProperties();
            foreach (var p in properties)
            {
                var at = p.GetCustomAttributes(attrType, false);
                foreach (var a in at)
                {
                    if (a.ToString() == attrType.ToString())
                        requiredProperties.Add(p.Name);
                }
            }
            return requiredProperties;
        }
        /// <summary>
        /// Get only one property with key attribute (throws error if key attributes used = 0 or > 1)
        /// </summary>
        /// <typeparam name="Model"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private string GetKeyProperty<Model>() where Model : new()
        {
            Model instance = new Model();
            var attrType = typeof(KeyAttribute);
            List<string> KeyProperties = new List<string>();
            PropertyInfo[] properties = instance.GetType().GetProperties();
            foreach (var p in properties)
            {
                var at = p.GetCustomAttributes(attrType, false);
                foreach (var a in at)
                {
                    if (a.ToString() == attrType.ToString())
                        KeyProperties.Add(p.Name);
                }
            }
            if (KeyProperties.Count == 0)
                throw new Exception("No valid key in the entity of type" + typeof( Model).ToString() +"!\n" +
                    "Solution:\n" +
                    "1- add 'using System.ComponentModel.DataAnnotations;'\n" +
                    "2- write the attribute '[Key]' before an ID Property");
            else if (KeyProperties.Count > 1)
                throw new Exception("only one key attribute in the entity is allowed!");

            return KeyProperties[0];
        }



        //#############################################################################################
        private string GetPropertyName<Model>(PropertyInfo p)// where Model :  new()
        {
            return p.Name;
        }
        private object GetPropertyValue<Model>(PropertyInfo p) where Model : new()
        {
            var m = new Model();
            return p.GetValue(m, null);
        }
        private Type GetPropertyType<Model>(PropertyInfo p) where Model : new()
        {
            var m = new Model();
            var v = p.GetValue(m);
            if (v != null)
            {
                return v.GetType();
            }
            else
            {
                try
                {
                    p.SetValue(m, "");
                    return typeof(string);
                }
                catch (ArgumentException ex1)
                {
                    try
                    {
                        p.SetValue(m, new DateTime());
                        return typeof(DateTime);
                    }
                    catch (ArgumentException ex2)
                    {
                        try
                        {
                            p.SetValue(m, 0d);
                            return typeof(double);
                        }
                        catch (ArgumentException ex3)
                        {
                            // Console.WriteLine(ex3.Message);
                            try
                            {
                                p.SetValue(m, 0.0f);
                                return typeof(float);
                            }
                            catch (ArgumentException ex4)
                            {
                                try
                                {
                                    p.SetValue(m, 0L);
                                    return typeof(long);
                                }
                                catch (ArgumentException ex5)
                                {
                                    try
                                    {
                                        p.SetValue(m, default(int));
                                        return typeof(int);
                                    }
                                    catch (ArgumentException ex6)
                                    {
                                        try
                                        {
                                            p.SetValue(m, false);
                                            return typeof(bool);
                                        }
                                        catch (ArgumentException ex7)
                                        {
                                            try
                                            {
                                                p.SetValue(m, default(byte));
                                                return typeof(byte);
                                            }
                                            catch (ArgumentException ex8)
                                            {
                                                return typeof(Nullable);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


    }
}
