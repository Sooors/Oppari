using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System;
using System.Linq;
namespace Com.AstaloGames.StreetHeat.AI
{
    [ExecuteInEditMode]
    public class Datareader : MonoBehaviour
    {

        [System.Serializable]
        public class PlayerChoiceUI
        {
            [Header("Draw This Player?")]
            public bool drawThisPlayer;
            [Header("DO NOT TOUCH THESE")]
            public bool thisPlayerWasAI;
            public int playerID;
            public Cars playerCar;
            [Header("Choose laps to draw ")]
            public bool drawAllLaps;
            public int minimumLap;
            public int maximumLap;
            public List<float> laptimes = new List<float>();
            
        }

        [System.Serializable]
        public class UIColors
        {
            public Color playerColor = Color.white;
        }

        public DBSession ReturnCurrentDBSession
        {
            get
            {
                return currentlyLoadedSession;
            }
        }


        [Header("Drawing color choice")]
        public UIColors[] drawingColors = new UIColors[4];


        [Header("Load session FIRST!")]
        public int sessioID;
        public bool loadSession;

        [Header("Players to draw")]
        public bool drawAllPlayers = false;
        public List<PlayerChoiceUI> playersToDraw = null;

        [Header("Special Options")]
        public bool drawMaxBrakePoints = false;
        public float brakePointValueToDraw;
        public bool drawMaxGasPoints = false;
        public float gasPointValueToDraw;
        public bool drawEvents = false;
        public ErrorLevel errorLevelTodraw;

        [Header("Draw players by time")]
        public bool drawPlayersByTime = false;
        public float ingameTime;
        public GameObject[] autot = new GameObject[16];

        [Header("Draw the chosen stuff!")]
        public bool drawThisShit = false;

        private DBSession currentlyLoadedSession = null;
        private bool drawAllLaps;

        private void Awake()
        {
            if (Application.isPlaying)
                this.enabled = false;
        }
        // Use this for initialization
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (loadSession)
            {
                loadSession = false;
                Debug.Log("loading session");
                LoadSession(sessioID);

            }
        }

        private void LoadSession(int _sessionKey)
        {
            //
            //fill session data
            currentlyLoadedSession = SessioKysely(_sessionKey);
            Debug.Log(currentlyLoadedSession.ToString());
            //laskee nopeimmait kierrokset
            //käy jokainen pelaaja lävitse, max finished lap = max kierros jonka jälkeen löytyy vielä kierros
            //c# tai DB
            foreach(DBPlayers dbp2 in currentlyLoadedSession.playersInTheSession)
            {
                Debug.Log("datapoints "+ dbp2.playerDataPoints.Count);
                //etsitään max kierros
                int max = dbp2.playerDataPoints.Max(x => x.lapNumber) - 1;

                for(int i=1; i <= max; i++)
                {
                    //kyseisen kierroksen viimeinen piste
                    int lapMaxRunNum = dbp2.playerDataPoints.FindAll(x => x.lapNumber == i).Max(y => y.runNumber);

                    //etsi saman kierroksen ensimmäinen piste
                    int lapMinRunNum = dbp2.playerDataPoints.FindAll(x => x.lapNumber == i).Min(y => y.runNumber);
                    //laske erotus pisteistä ja niiden Time.timesta == kierrosaika
                    float lapTime = dbp2.playerDataPoints.Find(x => x.runNumber == lapMaxRunNum).timeStamp - dbp2.playerDataPoints.Find(x => x.runNumber == lapMinRunNum).timeStamp;
                    dbp2.playerLaps.Add(lapTime);

                }
            }
            playersToDraw.Clear();
            foreach(DBPlayers dbp in currentlyLoadedSession.playersInTheSession)
            {
                PlayerChoiceUI temp = new PlayerChoiceUI();
                temp.playerID = dbp.playerID;
                temp.minimumLap = 0;
                temp.maximumLap = dbp.playerDataPoints.Max(y => y.lapNumber);
                temp.thisPlayerWasAI = dbp.playerAI;
                temp.playerCar = dbp.carID;
                foreach(float f in dbp.playerLaps)
                {
                    temp.laptimes.Add(f);
                }
                playersToDraw.Add(temp);
            }

            
        }

        DBSession SessioKysely(int _sessionKey)
        {
            Debug.Log("Sessiokysely alkaa");
            DBSession temp = new DBSession();
            string conn = "URI=file:" + Application.dataPath + "/Skenet/Testiskenet/Jori/StreetHeatDB.db";
            IDbConnection dbconn;
            dbconn = (IDbConnection)new SqliteConnection(conn);
            dbconn.Open(); //Open connection to the database.

            //fill the session main
            IDbCommand dbcmd = dbconn.CreateCommand();
            IDataReader reader;
            dbcmd.CommandText = "SELECT * FROM TableSession WHERE SessionKey =" + _sessionKey + "";
            try
            {
                reader = dbcmd.ExecuteReader();
                while (reader.Read())
                {
                    temp.sessionKey = reader.GetInt32(0);
                    temp.mapID = reader.GetInt32(1);
                    temp.mapName = reader.GetString(2);
                    temp.sessionStartTime = reader.GetFloat(3);
                    temp.savingTime = reader.GetDateTime(4);
                    temp.lapAmount = reader.GetInt32(5);
                    temp.sessionDescription = reader.GetString(6);
                    temp.version = reader.GetString(7);

                }
                reader.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            reader = null;
            dbcmd.Dispose();
            dbcmd = null;

            //fill in the players
            dbcmd = dbconn.CreateCommand();
            dbcmd.CommandText = "SELECT * FROM TablePlayers WHERE SessionKey =" + temp.sessionKey + " order by PlayerID asc";
            try
            {
                reader = dbcmd.ExecuteReader();
                while (reader.Read())
                {
                    DBPlayers tempPlayer = new DBPlayers();
                    tempPlayer.playerKey = reader.GetInt32(0);
                    tempPlayer.playerID = reader.GetInt32(1);
                    tempPlayer.sessionKey = reader.GetInt32(2);
                   // tempPlayer.playerDeathTime = reader.GetFloat(3);
                    tempPlayer.playerAI = (reader.GetInt32(4) == 1 ? true : false);
                    tempPlayer.carID = (Cars)reader.GetInt32(5);

                    //player death time needs fixin' too!
                    temp.playersInTheSession.Add(tempPlayer);
                }
                reader.Close();
            }
            catch(Exception e)
            {
                Debug.LogError(e.Message);
            }

            reader = null;
            dbcmd.Dispose();
            dbcmd = null;

            //fill in the datapoints..
            foreach(DBPlayers dbp in temp.playersInTheSession)
            {
                dbcmd = dbconn.CreateCommand();
                dbcmd.CommandText = "SELECT LapNum, RunNum,X,Y,Z,VectorX,VectorY,VectorZ,TransformForwardX,TransformForwardY,TransformForwardZ,Gas,Brake,Steering,DataPointTime,EventLevelKey,EventTypeKey  FROM TableDataPoints WHERE PlayerKey=" + dbp.playerKey+" order by RunNum asc";
                try
                {
                    reader = dbcmd.ExecuteReader();
                    while (reader.Read())
                    {
                        DBDatapoint dbpoint = new DBDatapoint();
                        dbpoint.lapNumber = reader.GetInt32(0);
                        dbpoint.runNumber = reader.GetInt32(1);
                        dbpoint.position = new Vector3(reader.GetFloat(2), reader.GetFloat(3), reader.GetFloat(4));
                        dbpoint.velocity = new Vector3(reader.GetFloat(5), reader.GetFloat(6), reader.GetFloat(7));
                        try
                        {
                            dbpoint.transformForward = new Vector3(reader.GetFloat(8), reader.GetFloat(9), reader.GetFloat(10));
                        }
                        catch(Exception e)
                        {
                            dbpoint.transformForward = Vector3.zero;
                        }

                        dbpoint.gas = reader.GetFloat(11);
                        dbpoint.brake = reader.GetFloat(12);
                        dbpoint.steering = reader.GetFloat(13);
                        dbpoint.timeStamp = reader.GetFloat(14);
                        dbpoint.errorLevel = (ErrorLevel)reader.GetFloat(15);
                        dbpoint.errorType = (ErrorType)reader.GetFloat(16);
                        dbp.playerDataPoints.Add(dbpoint);
                    }
                    reader.Close();
                }
                catch(Exception e)
                {
                    Debug.LogError(e.Message);
                }
                reader = null;
                dbcmd.Dispose();
                dbcmd = null;
            }

            dbconn.Close();
            dbconn = null;

            return temp;
        }

        private void OnDrawGizmos()
        {
            if (!drawThisShit || currentlyLoadedSession == null || currentlyLoadedSession.playersInTheSession == null || currentlyLoadedSession.playersInTheSession.Count == 0 || Application.isPlaying)
                return;



            foreach (PlayerChoiceUI pcui in playersToDraw)
            {
                int PlayerID4Switch = (int)pcui.playerCar * 4 + pcui.playerID;
                if (drawAllPlayers)
                {
                    pcui.drawThisPlayer = true;
                }
                //if this player isn't wanted to be drawn, skip.
                if (!pcui.drawThisPlayer)
                {
                    autot[PlayerID4Switch].SetActive(false);
                    continue;
                }
                autot[PlayerID4Switch].SetActive(true); 
                //else, we'll find out his datapoints and start drawing a nice line
                //for convenience, lets get direct ref for the right points

                List<DBDatapoint> pointsToDraw;

                if (pcui.drawAllLaps)
                    //checkboxilla piirtää kuitenkin kaikki, jos maximumLap -arvo on sama kuin populoidessa.
                    pointsToDraw = currentlyLoadedSession.playersInTheSession.Find(x => x.playerID == pcui.playerID).playerDataPoints;
                else
                //tämä filtteröinti ei toimi vielä. Piirtää ainoastaan "maximumLap" -kohtaan syötetyn lapNumberin.
                pointsToDraw = currentlyLoadedSession.playersInTheSession.Find(x => x.playerID == pcui.playerID).playerDataPoints.FindAll(y => y.lapNumber <= pcui.maximumLap && y.lapNumber >= pcui.minimumLap );

                
                //lets switch to some nice color the user has choosen
                Gizmos.color = drawingColors[pcui.playerID].playerColor;

                for (int i = 0; i < pointsToDraw.Count; i++)
                {
                    if (i + 1 < pointsToDraw.Count)
                        Gizmos.DrawLine(pointsToDraw[i].position, pointsToDraw[i + 1].position);
                }

                if(drawPlayersByTime)
                {
                    int indexToDraw = 0;
                    for(int i=0; i < pointsToDraw.Count; i++)
                    {
                        if (pointsToDraw[i].timeStamp  < currentlyLoadedSession.sessionStartTime + ingameTime )
                            indexToDraw = i;
                        else
                            break;
                    }
                    Debug.Log(indexToDraw);
                    // Gizmos.DrawCube(pointsToDraw[indexToDraw].position, Vector3.one*4f);
                    // Gizmos.DrawMesh(mesh, pointsToDraw[indexToDraw].position,Quaternion.LookRotation(pointsToDraw[indexToDraw].transformForward), Vector3.one*100f);
                    
                    autot[PlayerID4Switch].transform.position = pointsToDraw[indexToDraw].position;
                    autot[PlayerID4Switch].transform.rotation = Quaternion.LookRotation(pointsToDraw[indexToDraw].transformForward);

                }
                else
                {
                    foreach(GameObject go in autot)
                    {
                        if (go.activeSelf)
                            go.SetActive(false);
                    }
                }

                //ok, we have now drawn the lines. Is there any special points the user wants to see?
                if (drawMaxBrakePoints)
                {
                    //lets see if there's max brake points to draw
                    foreach (DBDatapoint dpoint in pointsToDraw.FindAll(x => x.brake > brakePointValueToDraw))
                    {
                        Gizmos.DrawCube(dpoint.position, new Vector3(3, 3, 3));
                    }

                }

                if (drawMaxGasPoints)
                {
                    foreach (DBDatapoint dpoint in pointsToDraw.FindAll(x => x.gas > gasPointValueToDraw))
                    {
                        Gizmos.DrawSphere(dpoint.position, 3f);
                    }
                }
                if(drawEvents)
                {
                foreach(DBDatapoint dpoint in pointsToDraw.FindAll(x => x.errorLevel == errorLevelTodraw))
                    {
                        //Gizmos.DrawCube(dpoint.position, new Vector3(3, 3,3));
                            switch (errorLevelTodraw)
                            {
                                case ErrorLevel.MiniError:
                                    GizmoColor(dpoint);
                                    Gizmos.DrawSphere(dpoint.position, 3f);
                                    break;
                                case ErrorLevel.MinorError:
                                    GizmoColor(dpoint);
                                    Gizmos.DrawSphere(dpoint.position, 3f);
                                    break;
                                case ErrorLevel.MajorError:
                                    GizmoColor(dpoint);
                                    Gizmos.DrawCube(dpoint.position, new Vector3(3, 3,3 ));
                                    break;
                                case ErrorLevel.None:
                                    break;
                                default:
                                    break; 
                    }
                    }
                }
            }
        }
        void GizmoColor(DBDatapoint dpoint)
        {
            switch (dpoint.errorType)
            {
                case ErrorType.BrakeClamp:
                    Gizmos.color = Color.red;
                    break;
                case ErrorType.SpeedChange:
                    Gizmos.color = Color.green;
                    break;
                case ErrorType.Steering:
                    Gizmos.color = Color.white;
                    break;
                case ErrorType.SkipPort:
                    Gizmos.color = Color.black;
                    break;
                case ErrorType.None:
                    break;
                default:
                    break;
            }
        }
    }
}