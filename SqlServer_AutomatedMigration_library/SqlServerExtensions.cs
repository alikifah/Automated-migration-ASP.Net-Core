/* =============================================================================================================================================
   Author: Al-Khafaji, Ali Kifah
   Date:   16.06.2022
   Description: An ASP.Net Core library that contains extension method for WebApplicationBuilder.
   the extension method 'AddContext' will create through the magic of reflection the Database and the Table for the Model that
   is given as generic parameter for this method.
   this process will be done on Application startup automatically without the need to add migration and update database manually
   through package manager console.
   the extension method AddContext will make use of the SqlServerManager class in the SqlServerHelper namespace, which will handle all the SQL queries to create
   the database and the tables for the models.
   there are 2 versions of AddContext method , the first one takes only the model class and the DbContext as generic typs,
   the second one takes also the repository class and the repository interface that can be used in repository pattern.
   the use of this library is so easy all that is required is to add a single line of code in the program.cs for each model we want to create the database table for.
   Example: builder.AddContext<User, UserDbContext>("DefaultConnection")
         or builder.AddContext<User, UserDbContext, IUserRepository, UserRepository >("DefaultConnection") for repository pattern

   Important: this library requires the model to have a single key property that is marked with Key attribute,
   Example:
   public class User
   {
        [Key]
        public int Id { get; set; } // this property can be named any thing as long as it is marked with [Key]
        public string Name { get; set; }
   }

    Prerequisites: Microsoft.EntityFrameworkCore, Microsoft.Data.SqlClient, System.ComponentModel.DataAnnotations. 
    Notice: this library works only with SQL Server. For Sqlite there is another library to be added to the project
     =============================================================================================================================================
*/
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using SqlServerHelper;

namespace SqlServerExtensions
{
    // 1- create table for model that has the same name as the DbSet in the associated  context
    public static class WebApplicationBuilder_Extension
    {
        /// <summary>
        /// Add DbContext with model and create Database and table automatically using Reflection 
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
            SqlServerManager helper = new SqlServerManager(connectiobString);
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
            SqlServerManager helper = new SqlServerManager(connectiobString);
            helper.CreateTable<Model>(tableName);
            AddDBContext<Context>(builder, connectionName);
            builder.Services.AddScoped<IRepository, Repository>();
        }
        private static IServiceCollection AddDBContext<T>(WebApplicationBuilder builder, string connectionName) where T : DbContext
        {
            string connectiobString = builder.Configuration.GetConnectionString(connectionName);
            return builder.Services.AddDbContext<T>(o => o.UseSqlServer(connectiobString));
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