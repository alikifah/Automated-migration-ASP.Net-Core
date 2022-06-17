# Automated-migration-library
An ASP.Net Core library that handles automated creation of the Database and the tables for models without the need to add migration and update database manually.

   This library contains extension method for WebApplicationBuilder.
   the extension method 'AddContext' will create through the magic of reflection the Database and the Table for the Model that
   is given as a generic parameter for this method.
   this process will be done on Application startup automatically without the need to add migration and update database manually
   
   the extension method AddContext will make use of the SqlServerManager/Sqlitemanager class in the SqlServerHelper/SqliteHelper namespace, which will handle    all the SQL queries to create the database and the tables for the models.
   there are 2 versions of AddContext method , the first one takes only the model class and the DbContext as generic types,
   the second one takes also the repository class and the repository interface that can be used in repository pattern.
   
   the use of this library is so easy all that is required is to add a single line of code in the program.cs for each model we want to create the database      table for.  
   Example: builder.AddContext<User, UserDbContext>("DefaultConnection")
         or builder.AddContext<User, UserDbContext, IUserRepository, UserRepository >("DefaultConnection") for repository pattern

   # Important:
   this library requires the model to have a single key property that is marked with Key attribute...
   Example:
  ```
   public class User
   {
        [Key] // Key attribute must be used only once before the ID property
        public int Id { get; set; } // this property can be named any thing as long as it is marked with [Key]
        public string Name { get; set; }
   }
  ```
  
    Prerequisites for Sqlite_AutomatedMigration_library: 
    1- Microsoft.EntityFrameworkCore
    2- Microsoft.Data.Sqlite
    3- System.ComponentModel.DataAnnotations

    Prerequisites for SqlServer_AutomatedMigration_library: 
    1- Microsoft.EntityFrameworkCore
    2- Microsoft.Data.SqlClient
    3- System.ComponentModel.DataAnnotations
