using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System;

namespace Com.AstaloGames.StreetHeat
{
    public class DBConnect : MonoBehaviour
    {
        public static SqliteConnection connectionToDataBase = null;

        public static void EnableDB()
        {
            if (connectionToDataBase == null)
            {
                string conn = "URI=file:" + Application.dataPath + "/Skenet/Testiskenet/Jori/StreetHeatDB.db";
                SqliteConnection dbconn;
                dbconn = new SqliteConnection(conn);
                dbconn.Open();
                connectionToDataBase = dbconn;
                Logger.Message("Database connection enabled");
            }
        }
        public static void DisableDB()
        {
            if (connectionToDataBase != null)
            {
                connectionToDataBase.Close();
                connectionToDataBase = null;
                Logger.Message("Database connection disabled");
            }
        }
    }
}