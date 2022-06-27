/* =============================================================================================================================================
   Author: Al-Khafaji, Ali Kifah
   Date:   16.06.2022
   Description: An ASP.Net Core library that handles automated creation of the Database and the tables
    for models without the need to add migration and update database manually.
    
visit the repository on github:
    https://github.com/alikifah/Automated-migration-ASP.Net-Core
     =============================================================================================================================================
*/
using System;

namespace SqliteAutoMigration
{
    public static class SettingsProvider
    {
        /// <summary>
        /// Return data source for Sqlite
        /// </summary>
        /// <param name="connectionbString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetDataSource(string connectionbString)
        {
            string n = getValueFromAppsettings(connectionbString, "Data Source");
            if (n == null)
            {
                n = getValueFromAppsettings(connectionbString, "Filename");
                if (n == null)
                    throw new Exception("Sqlite data source couldn't be found in the appsettings.json file!\nMake sure that you have the" +
                    " connectionstring in the right format..\n" +
                    "For more information about Sqlite connectionstring see the following link: https://www.connectionstrings.com/sqlite/");
                return n;
            }            
            return n;
        }
        /// <summary>
        /// return Database name for the SqlServer
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetDatabaseName(string connectionString)
        {
            string n = getValueFromAppsettings(connectionString, "Database");
            if (n == null)
                throw new Exception("No Database specified in appsettings.json file!");
            return n;
        }
        /// <summary>
        /// return the name of the SqlServer
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GetServerName(string connectionString)
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
        public static string getValueFromAppsettings(string connectionString, string requiredValue)
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
        /// <summary>
        /// get starting index of the first occurance of a word in a text, returns -1 when word not exist
        /// </summary>
        /// <param name="text"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        private static int ContainsWord(string text, string word)
        {
            if (text == null || text.Length < word.Length)
                return -1;
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
        public static string GetNameWithoutNamespace(string name)
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
