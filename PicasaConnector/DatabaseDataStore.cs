﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Google.Apis.Json;
using Google.Apis.Util.Store;

namespace PicasaConnector
{
    public class DatabaseDataStore : IDataStore
    {
        readonly string _tableName;

        public string TableName { get { return _tableName; } }

        private Boolean _ConnectionExists { get; set; }
        public Boolean connectionExists { get { return _ConnectionExists; } }


        /// <summary>
        /// Constructs a new file data store with the specified folder. This folder is created (if it doesn't exist 
        /// yet) under the current directory
        /// </summary>
        /// <param name="folder">Folder name</param>
        public DatabaseDataStore(string tableName)
        {
            _tableName = tableName;

            SqlConnection myConnection = this.ConnectToDatabase();   // Opens a connection to the database.

            if (_ConnectionExists)
            {
                // check if the Table Exists;
                try
                {
                    SqlDataReader myReader = null;
                    SqlCommand myCommand = new SqlCommand("select 1 from GoogleUser where 1 = 0", myConnection);
                    myReader = myCommand.ExecuteReader();
                    while (myReader.Read())
                    {
                        var hold = myReader["Column1"];
                    }
                }
                catch
                {
                    // table doesn't exist we create it
                    SqlCommand myCommand = new SqlCommand("CREATE TABLE [dbo].[GoogleUser]( " +
                                                          " [username] [nvarchar](4000) NOT NULL," +
                                                          " [RefreshToken] [nvarchar](4000) NOT NULL," +
                                                          " [Userid] [nvarchar](4000) NOT NULL" +
                                                           " ) ON [PRIMARY]", myConnection);
                    myCommand.ExecuteNonQuery();
                }
            }

            myConnection.Close();
        }

        /// <summary>
        /// Stores the given value for the given key. It creates a new file (named <see cref="GenerateStoredKey"/>) in 
        /// <see cref="FolderPath"/>.
        /// </summary>
        /// <typeparam name="T">The type to store in the data store</typeparam>
        /// <param name="key">The key</param>
        /// <param name="value">The value to store in the data store</param>
        public Task StoreAsync<T>(string key, T value)
        {

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }
            var serialized = NewtonsoftJsonSerializer.Instance.Serialize(value);

            SqlConnection myConnection = this.ConnectToDatabase();
            if (!_ConnectionExists)
            {
                throw new Exception("Not connected to the database");
            }

            // Try and find the Row in the DB.
            using (SqlCommand command = new SqlCommand("select Userid from GoogleUser where UserName = @username", myConnection))
            {
                command.Parameters.AddWithValue("@username", key);

                string hold = null;
                SqlDataReader myReader = command.ExecuteReader();
                while (myReader.Read())
                {
                    hold = myReader["Userid"].ToString();
                }
                myReader.Close();


                if (hold == null)
                {
                    try
                    {
                        // New User we insert it into the database
                        string insertString = "INSERT INTO [dbo].[GoogleUser]  ([username],[RefreshToken],[Userid]) " +
                                              " VALUES (@key,@value,'1' )";

                        SqlCommand commandins = new SqlCommand(insertString, myConnection);
                        commandins.Parameters.AddWithValue("@key", key);
                        commandins.Parameters.AddWithValue("@value", serialized);
                        commandins.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                        throw new Exception("Error inserting new row: " + ex.Message);

                    }


                }
                else
                {
                    try
                    {
                        // Existing User We update it                        
                        string insertString = "update [dbo].[GoogleUser] " +
                                              " set  [RefreshToken] = @value  " +
                                              " where username = @key";

                        SqlCommand commandins = new SqlCommand(insertString, myConnection);
                        commandins.Parameters.AddWithValue("@key", key);
                        commandins.Parameters.AddWithValue("@value", serialized);
                        commandins.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {

                        throw new Exception("Error updating user: " + ex.Message);

                    }
                }
            }


            myConnection.Close();
            return Task.Delay(0);
        }

        /// <summary>
        /// Deletes the given key. It deletes the <see cref="GenerateStoredKey"/> named file in <see cref="FolderPath"/>.
        /// </summary>
        /// <param name="key">The key to delete from the data store</param>
        public Task DeleteAsync<T>(string key)
        {

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }
            SqlConnection myConnection = this.ConnectToDatabase();
            if (!_ConnectionExists)
            {
                throw new Exception("Not connected to the database");
            }

            // Deletes the users data.                        
            string deleteString = "delete from [dbo].[GoogleUser] where username = @key";
            SqlCommand commandins = new SqlCommand(deleteString, myConnection);
            commandins.Parameters.AddWithValue("@key", key);
            commandins.ExecuteNonQuery();


            myConnection.Close();
            return Task.Delay(0);
        }

        /// <summary>
        /// Returns the stored value for the given key or <c>null</c> if the matching file (<see cref="GenerateStoredKey"/>
        /// in <see cref="FolderPath"/> doesn't exist.
        /// </summary>
        /// <typeparam name="T">The type to retrieve</typeparam>
        /// <param name="key">The key to retrieve from the data store</param>
        /// <returns>The stored object</returns>
        public Task<T> GetAsync<T>(string key)
        {
            //Key is the user string sent with AuthorizeAsync
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key MUST have a value");
            }
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            // Note: create a method for opening the connection.
            var myConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["ShareYourJourney"] + "");

            myConnection.Open();

            // Try and find the Row in the DB.
            using (SqlCommand command = new SqlCommand("select RefreshToken from GoogleUser where UserName = @username;", myConnection))
            {
                command.Parameters.AddWithValue("@username", key);

                string RefreshToken = null;
                SqlDataReader myReader = command.ExecuteReader();
                while (myReader.Read())
                {
                    RefreshToken = myReader["RefreshToken"].ToString();
                }

                if (RefreshToken == null)
                {
                    // we don't have a record so we request it of the user.
                    tcs.SetResult(default(T));
                }
                else
                {

                    try
                    {
                        // we have it we use that.
                        tcs.SetResult(NewtonsoftJsonSerializer.Instance.Deserialize<T>(RefreshToken));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }

                }
            }

            return tcs.Task;
        }

        /// <summary>
        /// Clears all values in the data store. This method deletes all files in <see cref="FolderPath"/>.
        /// </summary>
        public Task ClearAsync()
        {

            SqlConnection myConnection = this.ConnectToDatabase();
            if (!_ConnectionExists)
            {
                throw new Exception("Not connected to the database");
            }

            // Removes all data from the Table.
            string truncateString = "truncate table [dbo].[GoogleUser] ";
            SqlCommand commandins = new SqlCommand(truncateString, myConnection);
            commandins.ExecuteNonQuery();

            myConnection.Close();
            return Task.Delay(0);
        }

        /// <summary>Creates a unique stored key based on the key and the class type.</summary>
        /// <param name="key">The object key</param>
        /// <param name="t">The type to store or retrieve</param>
        public static string GenerateStoredKey(string key, Type t)
        {
            return string.Format("{0}-{1}", t.FullName, key);
        }



        //Handel's creating the connection to the database
        private SqlConnection ConnectToDatabase()
        {
            SqlConnection myConnection = null;
            try
            {
                myConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["ShareYourJourney"] + "");
                try
                {
                    myConnection.Open();
                    // ensuring that we are able to make a connection to the database.
                    if (myConnection.State == System.Data.ConnectionState.Open)
                    {
                        _ConnectionExists = true;
                    }
                    else
                    {
                        throw new ArgumentException("Error unable to open connection to the database.");
                    }
                }
                catch (Exception ex)
                {

                    throw new ArgumentException("Error opening Connection to the database: " + ex.Message);
                }

            }
            catch (Exception ex)
            {

                throw new ArgumentException("Error creating Database Connection: " + ex.Message);
            }

            return myConnection;
        }
    }
}