using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System;
namespace Com.AstaloGames.StreetHeat.AI
{
    /// <summary>
    /// holder class for managing information - both reading and writing
    /// </summary>
    public class DBSession
    {
        public int sessionKey; //key for database session identification
        public int mapID; //internal ID number which map was used
        public string mapName; //name of the map
        public int lapAmount; // number of TARGET laps in session - ie how many laps needed for finishing
        public string version; //version number of the build used to save the data
        public DateTime savingTime; //automatic stamp from db
        public string sessionDescription; // description of the session 
        public float sessionStartTime; // unity time stamp from the start
        

        public List<DBPlayers> playersInTheSession = new List<DBPlayers>(); //players in the session
        public override string ToString()
        {
            string retval = "";
            retval += "Session: " + sessionDescription +"Session Date" + savingTime + " started: " + sessionStartTime + " map: " + mapName + " map ID: " + mapID + " laps: " + lapAmount + " Game Version: " + version + "\n";
            retval += "Players in session: " + playersInTheSession.Count+"\n";
            foreach(DBPlayers dbp in playersInTheSession)
            {
                retval += "PlayerID: " + dbp.playerID + " PlayerAI?: " + dbp.playerAI + " PlayerCar: " + (Cars)dbp.carID + " number of Datapoints: " + dbp.playerDataPoints.Count +"\n";
            }
            return retval;
        }
    }
    /// <summary>
    /// holder class for managing saved information
    /// </summary>
    public class DBPlayers
    {
        public int playerKey; //key for database playerID
        public int sessionKey; //key for the session this player exists in
        public int playerID; // playerID for the game
        public bool playerAI; //was the player AI
        
        //needs implementation //change in the database
        public float playerDeathTime; // player death time from unity time
        public Cars carID; // player car -- can be stored as integer in the db, just convert enum <-> int when displaying

        public List<float> playerLaps = new List<float>();
        public List<DBDatapoint> playerDataPoints = new List<DBDatapoint>();
    }
    /// <summary>
    /// holder class for datapoint information
    /// </summary>
    public class DBDatapoint
    {
        public int datapointKey; //key for database datapoint
        public int playerKey; //key for database player

        public int lapNumber; // lap number player is on currently
        public int runNumber; // ascending numbering of datapoint
        public Vector3 position; //position to be saved
        public Vector3 velocity; //velocity currently
        public Vector3 transformForward; // transfrom forward of the car 

        public ErrorLevel errorLevel;
        public ErrorType errorType;
        public float gas; //current gas value from player
        public float brake; //current brake value from player
        public float steering; // current steering value
        public float timeStamp; // unity time when datapoint was saved
    }

    /// <summary>
    /// master db-handler -- datacollectors give information for this class for buffering and eventually saving
    /// </summary>
    /// 
    public class DBCollector : MonoBehaviour
    {
        private DBSession currentBufferedSession;
        public float[] lastTimesSaved = new float[] { -1.0f, -1.0f, -1.0f, -1.0f};
        bool raceStarted = false;

        // Use this for initialization
        void Start()
        {
  
        }

        // Update is called once per frame
        void Update()
        {
            if(AllKnowingMind.currentGameMode.countdownDone && raceStarted == false)
            {
                raceStarted = true;
                InitializeSessionForSaving();
            }
        }
        private void InitializeSessionForSaving()
        {
            currentBufferedSession = new DBSession();
            currentBufferedSession.mapID = AllKnowingMind.currentGameMode.ReturnNextLevel().mapIDNumber;
            currentBufferedSession.mapName = AllKnowingMind.currentGameMode.ReturnNextLevel().mapName;
            currentBufferedSession.version = GameObject.FindGameObjectWithTag("paavi").GetComponent<AllKnowingMind>().displayVersion;
            currentBufferedSession.lapAmount = (int)AllKnowingMind.currentGameMode.currentLevelPackage.lapAmount;
            currentBufferedSession.sessionDescription = FeederA.SESSION_DESCRIPTION;
            currentBufferedSession.sessionStartTime = Time.time; 

            //initialize players
            PlayerInformationList currentPlayers = AllKnowingMind.currentGameMode.currentPlayerList; //get the players
            for(int i = 0; i < currentPlayers.playerInformationList.Count; i++)
            {
                DBPlayers temp = new DBPlayers(); //create new temp 

                //setup infos that we can atm
                temp.carID = currentPlayers.playerInformationList[i].CarID; 
                temp.playerID = (int)currentPlayers.playerInformationList[i].playerID;
                temp.playerAI = currentPlayers.playerInformationList[i].playerAI;

                //add created player to buffer
                currentBufferedSession.playersInTheSession.Add(temp);
            }
            Debug.Log(currentBufferedSession.ToString());
        }
        public void AddDataPointToBuffer(DBDatapoint _incoming, int _internalPlayerID)
        {
            if (!raceStarted) //if race isn't yet started, we aren't interested in the datapoint, we simply discard it
                return;

            //check if we have saved this players db at all, or if theres enough time passed since the last one
            if (lastTimesSaved[_internalPlayerID] > 0f && Time.time - lastTimesSaved[_internalPlayerID] < FeederA.SAVING_FREQUENCY)
                return;

            _incoming.timeStamp = Time.time;
            lastTimesSaved[_internalPlayerID] = _incoming.timeStamp;
            currentBufferedSession.playersInTheSession.Find(x => x.playerID == _internalPlayerID).playerDataPoints.Add(_incoming);
        }
        public void OnDisable()
        {
             Logger.Message(currentBufferedSession.ToString());
            if (FeederA.SAVING_SESSION_DATA)
            {
                Logger.Message("Saving session to database..");
                SaveWholeSession();
            }
            else
                Logger.Message("Discarding collected data..");


        }
        private void SaveWholeSession()
        {
            DBConnect.EnableDB();
            //save the session information 
            string sqlQuery = "INSERT INTO TableSession(MapID,MapName,VersionNum,LapAmount,SessionComment,SessionStartTime) VALUES(" + currentBufferedSession.mapID + ",'" + currentBufferedSession.mapName + "','" + currentBufferedSession.version + "'," + currentBufferedSession.lapAmount + ",'" + currentBufferedSession.sessionDescription + "'," + currentBufferedSession.sessionStartTime + ")";
            TietokantaLisaus(sqlQuery);

            //get the current sessionID
            sqlQuery = "SELECT max(SessionKey) FROM TableSession ";
            currentBufferedSession.sessionKey = SessioLuku(sqlQuery);

            //save players and their datapoints
            foreach(DBPlayers dbp in currentBufferedSession.playersInTheSession)
            {
                
                //save the player                             
                sqlQuery = "INSERT INTO TablePlayers(PlayerID,SessionKey,Ai,CarID) VALUES(" + dbp.playerID + " , " + currentBufferedSession.sessionKey + ", " + (dbp.playerAI ? 1: 0) + ","+(int)dbp.carID+")";
                TietokantaLisaus(sqlQuery);

                //get the playerkey
                sqlQuery = "SELECT PlayerKey FROM TablePlayers WHERE SessionKey=" + currentBufferedSession.sessionKey + " AND PlayerID=" + dbp.playerID + " ";
                dbp.playerKey = SessioLuku(sqlQuery);
                SqliteTransaction trans = DBConnect.connectionToDataBase.BeginTransaction();
                //save players datapoints
                SqliteCommand cmd = DBConnect.connectionToDataBase.CreateCommand();
                cmd.Transaction = trans;

                foreach (DBDatapoint dbpoint in dbp.playerDataPoints)
                {
                    sqlQuery = "INSERT INTO TableDataPoints(PlayerKey,LapNum,RunNum,X,Y,Z,VectorX,VectorY,VectorZ,TransformForwardX,TransformForwardY,TransformForwardZ,Gas,Brake,Steering,DataPointTime,EventLevelKey,EventTypeKey) VALUES(" + dbp.playerKey + " ,  " + dbpoint.lapNumber + ", " + dbpoint.runNumber + "," + dbpoint.position.x + "," + dbpoint.position.y + "," + dbpoint.position.z + "," + dbpoint.velocity.x + "," + dbpoint.velocity.y + 
                        "," + dbpoint.velocity.z + "," + dbpoint.transformForward.x + "," + dbpoint.transformForward.y + "," + dbpoint.transformForward.z + "," + dbpoint.gas + "," + dbpoint.brake + "," + dbpoint.steering + "," +dbpoint.timeStamp+ ","+(int)dbpoint.errorLevel+ ","+(int)dbpoint.errorType +")";
                  //  Debug.Log(sqlQuery + "<-sql tf->" + dbpoint.transformForward);
                    cmd.CommandText = sqlQuery;
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
            }
            DBConnect.DisableDB();
        }

        public void TietokantaLisaus(string sqlQuery)
        {
            IDbCommand dbcmd = DBConnect.connectionToDataBase.CreateCommand();
            dbcmd.CommandText = sqlQuery;
            try
            {
                dbcmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            dbcmd.Dispose();  // hävitetään toimeksipanija
            dbcmd = null;
        }
        public int SessioLuku(string sqlQuery)
        {
            IDbCommand dbcmd = DBConnect.connectionToDataBase.CreateCommand();
            dbcmd.CommandText = sqlQuery;
            IDataReader reader = dbcmd.ExecuteReader();   // avataan lukija       
            int SessioID = 0;
            while (reader.Read())                  // luetaan tietkoanta
            {
                int luku1 = reader.GetInt32(0);
                SessioID = luku1;

            }
            reader.Close(); // suljetaan lukija
            reader = null;
            dbcmd.Dispose();  // hävitetään toimeksipanija
            dbcmd = null;


            return SessioID;

        }

    }
}
