using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using SqliteHelper;


namespace SqliteExtensions
{
    //  create table for model that has the same name as the DbSet in the associated  context
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
            string contextname = GetnameWithoutnameSpace(typeof(Context).ToString());
            string modelname = GetnameWithoutnameSpace(typeof(Model).ToString());
            string tableName = GetTableName<Context>();
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
            string dataSource = getdataSource(connectiobString);
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
            string contextname = GetnameWithoutnameSpace(typeof(Context).ToString());
            string modelname = GetnameWithoutnameSpace(typeof(Model).ToString());
            string tableName = GetTableName<Context>();
            if (tableName == null)
            {
                throw new Exception("No valid table in" + contextname + "!\n" +
                   "Suggested solution:\n" +
                   "write the following property in the " + contextname + " class:\n" +
                   "public DbSet<" + modelname +
                   "> " + modelname + "s { get; set; }");
            }
            string connectiobString = builder.Configuration.GetConnectionString(connectionName);
            string dataSource = getdataSource(connectiobString);
            SqliteManager helper = new SqliteManager(dataSource);
            helper.CreateTable<Model>(tableName);
            AddDBContext<Context>(builder, connectionName);
            builder.Services.AddScoped<IRepository, Repository>();
        }
        private static string getdataSource(string connectiobString)
        {
            int start = 0;
            int length = 0;
            int n = connectiobString.Length;
            for (int i =0; i< n; i++)
            {
                if (connectiobString[i] == '=')
                {
                    start = i +1;
                    break;
                }
            }
            for (int i = start; i < n; i++)
            {
                if (connectiobString[i] == ';')
                    break;
                length++;
            }
            return connectiobString.Substring(start, length);
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
            string contextname = GetnameWithoutnameSpace(typeof(Context).ToString());
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
        private static string GetnameWithoutnameSpace(string name)
        {
            int pointPosition = 0;
            int n = name.Length;
            for (int i = n - 1; i >= 0; i--)
            {
                if (name[i] == '.')
                {
                    pointPosition = i + 1;
                    break;
                }
            }
            if (pointPosition > 0)
                return name.Substring(pointPosition, n - pointPosition);
            else
                return name;
        }
    }

}
