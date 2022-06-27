# Automated migration library for ASP.Net Core
An ASP.Net Core library that handles automated creation of the Database and the tables for models without the need to add migration and update database manually.

   This library contains extension method for WebApplicationBuilder.
   the extension method 'AddContext' will create through the magic of reflection the Database and the Table for the Model that
   is given as a generic parameter for this method.
   this process will be done on Application startup automatically without the need to add migration and update database manually
   
   the extension method AddContext will make use of the SqlServerManager/Sqlitemanager class in the SqlServerHelper/SqliteHelper namespace, which will handle    all the SQL queries to create the database and the tables for the models.
   there are 2 versions of AddContext method , the first one takes only the model class and the DbContext as generic types,
   the second one takes also the repository class and the repository interface that can be used in repository pattern.

   ## How to use?
   the use of this library is so easy. All that is required is to include the Sqlite_AutomatedMigration_library or the SqlServer_AutomatedMigration_library      folder in your project and add a single line of code in the program.cs after creating the WebApplicationbuilder for each model you want to create the database table for.  
   Example:
   ```c#
      using SqlServerExtensions;
      using myWebApp.Models;
   
      var builder = WebApplication.CreateBuilder(args);
      builder.AddContext<User, UserDbContext>("DefaultConnection")
   ```
   
   or in case of using repository pattern:
   ```c#
   using SqlServerExtensions;
   using myWebApp.Models;
   
   var builder = WebApplication.CreateBuilder(args);
   builder.AddContext<User, UserDbContext, IUserRepository, UserRepository >("DefaultConnection") 
   ```
   ## Important:
   this library requires the model to have a single key property that is marked with Key attribute...
   Example:
  ```c#
  using System.ComponentModel.DataAnnotations;
   public class User
   {
        [Key] // Key attribute must be used only once before the ID property
        public int Id { get; set; } // this property can be named any thing as long as it is marked with [Key]
        public string Name { get; set; }
   }
  ```
## Limitation:
this library migrates only view models.. the migration of identity information is not implemented.
## Prerequisites

    Prerequisites for SqliteAutoMigration library: 
    1- Microsoft.EntityFrameworkCore
    2- Microsoft.EntityFrameworkCore.Sqlite
    3- Microsoft.Data.Sqlite
    4- System.ComponentModel.DataAnnotations

    Prerequisites for SqlServer_AutomatedMigration_library: 
    1- Microsoft.EntityFrameworkCore
    2- Microsoft.Data.SqlClient
    3- System.ComponentModel.DataAnnotations
    




