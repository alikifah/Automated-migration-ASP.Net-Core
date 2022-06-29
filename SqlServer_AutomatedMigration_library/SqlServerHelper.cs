// ============================================================================
//    Author: Al-Khafaji, Ali Kifah
//    Date:   16.06.2022
//    Description: A class, that manages SQL queries to Database
// ============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;

namespace SqlServerHelper
{
    public enum ColumnType
    {
        INTEGER,
        TEXT,
        BLOB,
        REAL,
        NUMERIC,
        DATETIME2,
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

    /// <summary>
    /// this class manages SQL queries to Database
    /// </summary>
    public class SqlServerManager
    {
        private string _connectionString = "";
        private string _server = "";
        private string _dataBase = "";

        public SqlServerManager(string connectionString)
        {
            if (connectionString == null)
            {
                throw new Exception("No Connection string was found in the appsettings.json file!\nConnection to SQL Server is not possible!");
            }
            this._connectionString = connectionString;
            _server = getServerName(connectionString);
            _dataBase = getDatabaseName(connectionString);
             CreateDataBase();
        }
        private void DeleteColumn(string tablename, string columnName) 
        {
            if (!isTableExist(tablename)) return;
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = @"ALTER TABLE " + tablename + " " + "DROP COLUMN " + columnName;
                cmd.ExecuteNonQuery();
            }
        }
        private void CreateTable(string tablename, string key, List<Column> columns)
        {
           
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var cmd = con.CreateCommand();
                if (!isTableExist(tablename))
                {
                    cmd.CommandText = @"CREATE TABLE " + tablename + " (" + key + " INTEGER PRIMARY KEY IDENTITY (1, 1) NOT NULL)";
                    cmd.ExecuteNonQuery();
                }
                
                foreach (Column col in columns)
                {
                    if (!isColumnExist(tablename, col.Name))
                    {
                        string varType = "";
                        if (col.Type == ColumnType.TEXT)
                            varType = "NVARCHAR (MAX)";
                        else 
                            varType = col.Type.ToString();

                        cmd.CommandText = @"ALTER TABLE " + tablename + " " + "ADD " + col.Name + " " + varType + " " + col.NotNull;
                        cmd.ExecuteNonQuery();
                    }
                }
                
            }
        }
        private bool isTableExist(string tableName)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT * FROM SYS.TABLES";
                    var reader = cmd.ExecuteReader();
                    int nameIndex = reader.GetOrdinal("Name");
                    while (reader.Read())
                    {
                        if (reader.GetString(nameIndex) == tableName) 
                            return true;
                    }
                    return false;
                }
                catch
                {
                }
            }
            return false;
        }
        private string[] GetTables(string tableName) 
        {
            List<string> columns = new List<string>();
            using (var con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT * FROM SYS.TABLES";
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
        private bool isColumnExist(string tableName, string columnName) 
        {
            
            using (var con = new SqlConnection(_connectionString))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('" + tableName + "')" ;
                    var reader = cmd.ExecuteReader();
                    int nameIndex = reader.GetOrdinal("Name");
                    while (reader.Read())
                    {
                        if ( reader.GetString(nameIndex) == columnName) 
                            return true;
                    }
                    return false;
                }
                catch
                {
                }
            }

            return false;
        }
        public bool is_DB_exist( string databaseName) 
        {
            string c = @"Data Source=(localdb)\MSSQLLocalDB";
            using (var connection = new SqlConnection(c))
            {
                using (var command = new SqlCommand("SELECT db_id(@databaseName)", connection))
                {
                    command.Parameters.Add(new SqlParameter("databaseName", databaseName));
                    connection.Open();
                    return command.ExecuteScalar() != DBNull.Value;
                }
            }
        }
        private void CreateDataBase()
        {
            if (!is_DB_exist(_dataBase))
            {
                string c = @"Server=" + _server;
                using (SqlConnection conn = new SqlConnection(c))
                {
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        conn.Open();
                        cmd.Connection = conn;
                        cmd.CommandText = "Create database " + _dataBase;
                        cmd.ExecuteNonQuery();
                        Console.WriteLine("------ data base created succesfully!");
                    }
                }
            }

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
            string key = GetKeyProperty<Model>();// if no key this will throw an exception
            // get required from model
            List<string> required =  GetRequiredProperties<Model>() ;            
            // get required from DB, not null = required
            List<string> notNullableColumnsDB = new List<string>();
            List<string> columnsAlreadyInDBTable = new List<string>();
            if (isTableExist(tablename))
            {
                columnsAlreadyInDBTable = GetColumns(tablename);
                List<string> nullableColumnsDB = GetNullColumns(tablename);
                
                foreach (string col in columnsAlreadyInDBTable)
                {
                    if (!nullableColumnsDB.Contains(col))
                        notNullableColumnsDB.Add(col);
                }
            }
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
                    if (columnsAlreadyInDBTable.Contains(n))  // if property in DB
                    {
                        if (notNullableColumnsDB.Contains(n)) // if required in DB
                        {
                            if (!required.Contains(n)) // if not required in model
                                MakeColumnNullable(tablename, new Column(n, ColumnType.TEXT)); // make not required in DB
                        }
                        else // if not required in DB
                        {
                            if (required.Contains(n)) // if required in model
                                MakeColumnNotNullable(tablename, new Column(n, ColumnType.TEXT)); // make required in DB
                        }
                    }
                    else // if not in DB (new column)
                    {
                        if (required.Contains(n))
                            notNull = "NOT NULL";
                        else
                            notNull = "";
                    }
                }
                else if (t == typeof(DateTime))
                {
                    ct = ColumnType.DATETIME2;
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
        private List<string> GetColumns(string tableName)
        {
            List<string> columns = new List<string>();
            using (var con = new SqlConnection( _connectionString))
            {
                try
                {
                    con.Open();
                    var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('" + tableName + "')";
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
            return columns;
        }
        private void DeleteTable(string tablename)
        {
            string cs = @"Data Source=" + _connectionString;
            using (var con = new SqlConnection(cs))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = "DROP TABLE IF EXISTS " + tablename;
                cmd.ExecuteNonQuery();
            }
        }
        //#######################################################################################################################################
        private void MakeColumnNullable(string table, Column column)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = "ALTER TABLE " + table + " ALTER COLUMN " + column.Name + " " + column.Type + " NULL";
                cmd.ExecuteNonQuery();
            }

        }
        private void MakeColumnNotNullable(string table, Column column)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = "ALTER TABLE " + table + " ALTER COLUMN " + column.Name + " " + column.Type + " NOT NULL";
                cmd.ExecuteNonQuery();
            }

        }
        private List<string> GetNullColumns(string tableName) 
        {
            List<string> columns = new List<string>();
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();
                var cmd = con.CreateCommand();
                cmd.CommandText = "SELECT * FROM sys.columns WHERE object_id = object_id('" + tableName + "')";
                var reader = cmd.ExecuteReader();
                int nameIndex = reader.GetOrdinal("name");
                int nullIndex = reader.GetOrdinal("is_nullable");

                while (reader.Read())
                {
                    string name = reader.GetString(nameIndex);
                    bool isNull = reader.GetBoolean(nullIndex);
                    if (isNull)
                        columns.Add(reader.GetString(nameIndex));
                }

            }
            return columns;
        }


        //#############################################################################################


            // get required properties from model
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
                throw new Exception("No valid key in the entity of type " + typeof( Model).ToString() +"!\n" +
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
        public static Type GetPropertyType<Model>(PropertyInfo p) where Model : new()
        {
            return p.PropertyType;
        }


        //################################################################################################################################
        //###########   String helper methods #################################################################################################
        //################################################################################################################################

        private static string getDatabaseName(string connectionString)
        {
            string n = getValueFromAppsettings(connectionString, "Database");
            if (n == null)
                throw new Exception("No Database specified in appsettings.json file!");
            return n;
        }
        private static string getServerName(string connectionString)
        {
            string n1 = getValueFromAppsettings(connectionString, "Server");
            if (n1 == null)
            {
                string n2 = getValueFromAppsettings(connectionString, "Data Source");
                if (n2 == null)
                    throw new Exception("No Server name specified in appsettings.json file!");
                return n2;
            }
            return n1;
        }
        private static string getValueFromAppsettings(string connectionString, string requiredValue)
        {
            int dataBaseWordIndex = ContainsWord(connectionString, requiredValue);
            if (dataBaseWordIndex < 0)
                return null;           
            string dataBasename = "";
            int start = dataBaseWordIndex + requiredValue.Length + 1;
            int searchlimit = connectionString.Length;

            for (int i = start; i < searchlimit; i++)
            {
                if (connectionString[i] == ';')
                    break;
                dataBasename += connectionString[i];
            }
            return dataBasename.Trim();
        }
        // get index of the first occurance of a word in a text, returns -1 when word not exist
        private static int ContainsWord(string text, string word)
        {
            int textLength = text.Length;
            int wordLength = word.Length;
            int searchlimit = textLength - wordLength;
            int wordLettersChecked = 0;
            int wordIndex = -1;
            for (int i = 0; i < searchlimit; i++)
            {
                if (wordIndex >= 0)
                {
                    if (wordLettersChecked == wordLength)
                        break;
                    if (text[i] == word[wordLettersChecked])
                        wordLettersChecked++;
                    else
                    {
                        wordLettersChecked = 0;
                        wordIndex = -1;
                    }
                }
                else
                {
                    if (text[i] == word[0])
                    {
                        wordLettersChecked++;
                        wordIndex = i;
                    }
                }
            }
            return wordIndex;
        }





    }
}
