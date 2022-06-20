using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Reflection;

namespace SqliteAutoMigration
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

        public Column(string name, ColumnType type)
        {
            Name = name;
            Type = type;
        }
    }

    public class SqliteManager
    {
        private string datasource = "";
        public SqliteManager(string datasource)
        {
            this.datasource = datasource;
        }

        public void test()
        {
            //DeleteTable("cars" , @"D:\hello.db");

            //CreateTable();

            //RemoveColumn( "cars", @"D:\hello.db", "COLUMNyear");
            /*
             List<Column> columns = new List<Column>()
             {
                 new Column("Name", ColumnType.TEXT ),
                 new Column("Age", ColumnType.INT ),
                 new Column("Email", ColumnType.TEXT )
             };
             CreateTable("users", columns);
            AddColumn("users","hhh",ColumnType.NUMERIC);
            */
            // Console.WriteLine(" -------------- " + isColumnExist("users", "id" ));
            // CreateTable<User>("ss");


           // CreateTable<User>("Users");

        }
        /// <summary>
        /// Main method to Create a Table For a model automatically using Reflection
        /// </summary>
        /// <typeparam name="Model">the entity you want to create the table for, this class must contain only one [Key] attribute</typeparam>
        /// <param name="tablename">name of the table for the entity stored in the data source, this must be the same name as the DbSet property in the DbContext class associated with the model</param>
        public void CreateTable<Model>(string tablename) where Model : new() 
        {
            ColumnType ct;
            string key = ReflectionProvider.GetKeyProperty<Model>();
            List<string> columnsAlreadyInDBTable = new List<string>();
            if (isTableExist(tablename))
                columnsAlreadyInDBTable.AddRange(GetColumns(tablename));

            var m = new Model();
            List<Column> columnsInModel = new List<Column>();
            PropertyInfo[] properties = m.GetType().GetProperties();
            foreach (var p in properties)
            {
                var t = ReflectionProvider.GetPropertyType<Model>(p);
                var n = ReflectionProvider.GetPropertyName<Model>(p);
                var v = ReflectionProvider.GetPropertyValue<Model>(p);

                if (t == typeof(string))
                    ct = ColumnType.TEXT;
                else if (t == typeof(DateTime))
                    ct = ColumnType.TEXT;

                else if (t == typeof(int) || t == typeof(long) || t == typeof(byte))
                    ct = ColumnType.INTEGER;

                else if (t == typeof(float) || t == typeof(double))
                    ct = ColumnType.REAL;

                else if (t == typeof(bool))
                    ct = ColumnType.NUMERIC;
                    
                else ct = ColumnType.UNDEFINED;

                if (ct!= ColumnType.UNDEFINED)
                    columnsInModel.Add(new Column(n, ct) );
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
                    cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='" + tablename + "'; ";
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

        /*
        private void AddColumn(string tablename, string columnName, ColumnType type)
        {
            if (isColumnExist(columnName, tablename)) return;

            string cs = @"Data Source=D:\hello.db";
            using (var con = new SqliteConnection(cs))
            {
                con.Open();
                var cmd = con.CreateCommand();
//                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS " + tablename + "(Id INTEGER PRIMARY KEY NOT NULL," + columnName + " " + type + ")";
  //              cmd.ExecuteNonQuery();
                cmd.CommandText = @"ALTER TABLE " + tablename + " " + "ADD " + columnName + " " + type;
                cmd.ExecuteNonQuery();
            }
        }

        */


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
                        cmd.CommandText = @"ALTER TABLE " + tablename + " " + "ADD " + col.Name + " " + col.Type + " NOT NULL";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        //#############################################################################################


    }
}
