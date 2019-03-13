using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using Mono.Data.Sqlite;
using System.Data;
using System;



namespace Com.AstaloGames.StreetHeat.AI
{/// <summary>
/// Datapoint collector in each car, passes information for DBCollector for saving..
/// </summary>
    public class Datacollector : MonoBehaviour
    {
        int counter = 0;
        // Use this for initialization
        void Start()
        {
         
        }
        // Update is called once per frame
        void Update()
        {
            //chekc if we actually have the right prefab in the scene, if not, spam error and return
            if(GameObject.FindObjectOfType<DBCollector>() == null)
            {
                Logger.Message("Missing database information collector prefab!");
                return;
            }

            DBDatapoint temp = new DBDatapoint();
            temp.brake = GetComponent<CarDriving>().ReturnControlValues().returnbrake;
            temp.gas = GetComponent<CarDriving>().ReturnControlValues().returngas;
            temp.steering = GetComponent<CarDriving>().ReturnControlValues().returnsteering;
            temp.velocity = GetComponent<Rigidbody>().velocity;
            temp.transformForward = GetComponent<Rigidbody>().transform.forward;
            temp.position = transform.position;
            temp.lapNumber = GetComponent<CarControl>().CarMapPlayer.currentLap;
            temp.runNumber = counter;
            AIEvent possibleEvent = GetComponent<CarAIControl>().EventInProcess;
            temp.errorLevel = possibleEvent.eventErrorLevel;
            temp.errorType = possibleEvent.eventErrortype;
            counter++;
            GameObject.FindObjectOfType<DBCollector>().AddDataPointToBuffer(temp, GetComponent<CarControl>().CarMapPlayer.PlayerInternalID);
        }
    }
}