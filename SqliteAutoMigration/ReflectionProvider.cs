/* =============================================================================================================================================
   Author: Al-Khafaji, Ali Kifah
   Date:   16.06.2022
   Description: An ASP.Net Core library that handles automated creation of the Database and the tables
    for models without the need to add migration and update database manually.
    
visit the repository on github:
    https://github.com/alikifah/Automated-migration-ASP.Net-Core
     =============================================================================================================================================
*/

using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SqliteAutoMigration
{
    public static class ReflectionProvider
    {

        public static List<string> GetNotMappedProperties<Model>() where Model : new()
        {
            Model instance = new Model();
            var attrType = typeof(NotMappedAttribute);
            List<string> notMappedProperties = new List<string>();
            PropertyInfo[] properties = instance.GetType().GetProperties();
            foreach (var p in properties)
            {
                var at = p.GetCustomAttributes(attrType, false);
                foreach (var a in at)
                {
                    if (a.ToString() == attrType.ToString())
                        notMappedProperties.Add(p.Name);
                }
            }
            return notMappedProperties;
        }
        public static List<string> GetRequiredProperties<Model>() where Model : new()
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
        public static string GetKeyProperty<Model>() where Model : new()
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
                throw new Exception("No valid key in the entity of type" + typeof(Model).ToString() + "!\n" +
                    "Solution:\n" +
                    "1- add 'using System.ComponentModel.DataAnnotations;'\n" +
                    "2- write the attribute '[Key]' before an ID Property");
            else if (KeyProperties.Count > 1)
                throw new Exception("only one key attribute in the entity is allowed!");

            return KeyProperties[0];
        }


        public static string GetPropertyName<Model>(PropertyInfo p)
        {
            return p.Name;
        }
        public static object GetPropertyValue<Model>(PropertyInfo p) where Model : new()
        {
            var m = new Model();
            return p.GetValue(m, null);
        }
        public static Type GetPropertyType<Model>(PropertyInfo p) where Model : new()
        {
            return p.PropertyType;
        }




    }
}
