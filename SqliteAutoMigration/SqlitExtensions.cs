using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace SqliteAutoMigration
{
    // 1- create table for model that has the same name as the DbSet in the associated  context
    public static class WebApplicationBuilder_Extension
    {
        /// <summary>
        /// Add DbContext with model only and create Database and table automatically using Reflection 
        /// </summary>
        /// <typeparam name="Model">the model you want to store in the Database </typeparam>
        /// <typeparam name="Context">the DbContext class associated with your model</typeparam>
        /// <param name="builder"></param>
        /// <param name="connectionName">your prefered key in the ConnectionStrings from appsettings.json file</param>
        /// <exception cref="Exception"></exception>
        public static void AddContext<Model, Context>(this WebApplicationBuilder builder, string connectionName)
        where Model : class, new() where Context : DbContext
        {
            string contextname = SettingsProvider.GetNameWithoutNamespace(typeof(Context).ToString());
            string modelname = SettingsProvider.GetNameWithoutNamespace(typeof(Model).ToString());
            string tableName = GetTableName<Context>();
           // Console.WriteLine("tableName :" + tableName);
            if (tableName == null)
            {
                throw new Exception("No valid table in" + contextname +"!\n" +
                   "Suggested solution:\n" +
                   "write the following property in the " + contextname + " class:\n" +
                   "public DbSet<" + modelname +
                   "> " + modelname + "s { get; set; }");
            }
            string connectiobString = builder.Configuration.GetConnectionString(connectionName);
            //Data Source=books.db;Cache=Shared
            string dataSource = SettingsProvider.GetDataSource(connectiobString);
           // Console.WriteLine("----" + getdataSource(connectiobString));
            SqliteManager helper = new SqliteManager(dataSource);
            helper.CreateTable<Model>(tableName);

            AddDBContext<Context>(builder, connectionName);
        }

        /// <summary>
        /// Add DbContext with model and repository and create Database and table automatically using Reflection 
        /// </summary>
        /// <typeparam name="Model">the model you want to store in the Database </typeparam>
        /// <typeparam name="Context">the DbContext class associated with your model</typeparam>
        /// <typeparam name="IRepository">the Repository interface associated with your model</typeparam>
        /// <typeparam name="Repository">the Repository class associated with your model </typeparam>
        /// <param name="builder"></param>
        /// <param name="connectionName">your prefered key in the ConnectionStrings from appsettings.json file</param>
        /// <exception cref="Exception"></exception>
        public static void AddContext<Model, Context, IRepository, Repository>(this WebApplicationBuilder builder, string connectionName) 
        where Model : class, new() where Context : DbContext where IRepository : class where Repository : class, IRepository 
        {
            string contextname = SettingsProvider.GetNameWithoutNamespace(typeof(Context).ToString());
            string modelname = SettingsProvider.GetNameWithoutNamespace(typeof(Model).ToString());
            string tableName = GetTableName<Context>();
            Console.WriteLine("tableName :" + tableName);
            if (tableName == null)
            {
                throw new Exception("No valid table in" + contextname + "!\n" +
                   "Suggested solution:\n" +
                   "write the following property in the " + contextname + " class:\n" +
                   "public DbSet<" + modelname +
                   "> " + modelname + "s { get; set; }");
            }

            string connectiobString = builder.Configuration.GetConnectionString(connectionName);
            string dataSource = SettingsProvider.GetDataSource(connectiobString);

           // Console.WriteLine("----" + getdataSource( connectiobString));
            SqliteManager helper = new SqliteManager(dataSource);
            helper.CreateTable<Model>(tableName);

            AddDBContext<Context>(builder, connectionName);
            builder.Services.AddScoped<IRepository, Repository>();
        }
       
        /// <summary>
        /// A shortcut method to add DBContext in Program.cs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="builder"></param>
        /// <param name="connectionName">Connection name from appsettings.json</param>
        private static IServiceCollection AddDBContext<T>(WebApplicationBuilder builder, string connectionName) where T : DbContext
        {
            string connectiobString = builder.Configuration.GetConnectionString(connectionName);
            return builder.Services.AddDbContext<T>(o => o.UseSqlite(connectiobString));
        }

        private static string GetTableName<Context>()
        {
            string contextname = SettingsProvider.GetNameWithoutNamespace(typeof(Context).ToString());
            List<string> DbContextProperties = new List<string> { "Database", "ChangeTracker", "Model", "ContextId" };
            List<string> requiredProperties = new List<string>();
            PropertyInfo[] properties = typeof(Context).GetProperties();
            foreach (var p in properties)
            {
                if (!DbContextProperties.Contains(p.Name))
                    requiredProperties.Add(p.Name);
            }
            if (requiredProperties.Count == 1)
                return requiredProperties[0];
            else if (requiredProperties.Count > 1)
            {
                throw new Exception( contextname + " must contain one table!\n" +
                  "Suggested solution:\n" +
                  "Make sure you have in " + contextname + " only one public DbSet property!");
            }
            return null;
        }


    }





}