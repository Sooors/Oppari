using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Com.AstaloGames.StreetHeat.AI
{
    public enum ErrorType {  BrakeClamp, SpeedChange, Steering, SkipPort, None = 99 };
    public enum ErrorLevel { MiniError, MinorError, MajorError, None = 99 };
    public struct ErrorContainer
    {
        public ErrorType errorType;
        public ErrorLevel errorLevel;
        public float randomLowerLimit;
        public float randomUpperLimit;
        public float LowerLimitForMultiplier;
        public float UpperLimitForMultiplier;
        public float portSkippingTime;
    }
    public class CarValueHolder
    {
        public float limit;
        public float brakeClamp;
        public float lookDistanceToLookOut;
        public float distanceSpeedMultiplier;
        public float steeringOnLastUpdate;
        public int skippedPortCount = 0;
    }

    public class AIEvent
    {
        public bool SkippingPort
        {
            get
            {
                if (Time.time - portSkippingStartTime < currentErrorContainer.portSkippingTime)
                    return true;
                else
                    return false;
            }
        }
        public static List<ErrorContainer> allErrors = new List<ErrorContainer> {
        //speedChange********************
        new ErrorContainer() {
        errorType = ErrorType.SpeedChange,
        errorLevel = ErrorLevel.MiniError,
        randomLowerLimit = 0.85f,
        randomUpperLimit = 1.15f },

        new ErrorContainer() {
        errorType = ErrorType.SpeedChange,
        errorLevel = ErrorLevel.MinorError,
        randomLowerLimit = 1f,
        randomUpperLimit = 1.75f },

        new ErrorContainer() {
        errorType = ErrorType.SpeedChange,
        errorLevel = ErrorLevel.MajorError,
        randomLowerLimit = 1.5f,
        randomUpperLimit = 2f },

        //BrakeClamp ************************************
        new ErrorContainer() {
        errorType = ErrorType.BrakeClamp,
        errorLevel = ErrorLevel.MiniError,
        randomLowerLimit = 0f,
        randomUpperLimit = 0f },

        new ErrorContainer() {
        errorType = ErrorType.BrakeClamp,
        errorLevel = ErrorLevel.MinorError,
        randomLowerLimit = 0f,
        randomUpperLimit = 0f },

        new ErrorContainer() {
        errorType = ErrorType.BrakeClamp,
        errorLevel = ErrorLevel.MajorError,
        randomLowerLimit = 0.0f,
        randomUpperLimit = 0.0f },

        //SkipPort **********************************************
        new ErrorContainer() {
        errorType = ErrorType.SkipPort,
        errorLevel = ErrorLevel.MajorError,
        portSkippingTime = 2f, },

        //Steering error *******************************************
        new ErrorContainer() {
        errorType = ErrorType.Steering,
        errorLevel = ErrorLevel.MiniError,
        randomLowerLimit = -0.2f,
        randomUpperLimit = 0.2f },

        new ErrorContainer() {
        errorType = ErrorType.Steering,
        errorLevel = ErrorLevel.MinorError,
        randomLowerLimit = -0.4f,
        randomUpperLimit = 0.4f },

        new ErrorContainer() {
        errorType = ErrorType.Steering,
        errorLevel = ErrorLevel.MajorError,
        randomLowerLimit = -0.75f,
        randomUpperLimit = 0.75f },

        };

        public ErrorType eventErrortype;
        public ErrorLevel eventErrorLevel;
        private CarValueHolder backedUpCarValues = new CarValueHolder();
        private CarAIControl master;
        private ErrorContainer currentErrorContainer;
        private float steeringError;
        private float portSkippingStartTime;
        private float steeringSpeedMultiplier;

        public AIEvent(ErrorType _errorType, ErrorLevel _errorLevel, CarAIControl _master)
        {
            eventErrorLevel = _errorLevel;
            eventErrortype = _errorType;
            master = _master;
            currentErrorContainer = allErrors.Find(x => x.errorType == eventErrortype && x.errorLevel == eventErrorLevel);
            //event starting, backup the old values
            backedUpCarValues = master.ReturnCurrentCarValues();
            if(eventErrortype == ErrorType.Steering)
            {
                int coinFlip = Random.Range(0, 2);
                if (coinFlip == 1)
                    steeringSpeedMultiplier = currentErrorContainer.LowerLimitForMultiplier;
                else
                    steeringSpeedMultiplier = currentErrorContainer.UpperLimitForMultiplier;
            }
        }
        public void ExecuteEvent()
        {
            Logger.Message("AI Event starts for player " + master.GetComponent<CarControl>().CarMapPlayer.playerName+" EventType: " + eventErrortype.ToString() + " EventLevel:" + eventErrorLevel.ToString());
            switch (eventErrortype)
            {
                case ErrorType.SpeedChange:
                  //  Debug.Log("speedLimit before change:" + master.Limit);
                   
                    //    Debug.Log("speedLimit after change:" + master.Limit);
                    if (eventErrorLevel == ErrorLevel.MajorError)
                    {
                        master.BrakeClamp = 0f;
                        master.Limit = 500f;
                    }
                    else
                        master.Limit = master.Limit * Random.Range(currentErrorContainer.randomLowerLimit, currentErrorContainer.randomUpperLimit);
                    break;
                case ErrorType.BrakeClamp:
                  //  Debug.Log("brake clamp before change:" + master.BrakeClamp);
                    master.BrakeClamp = master.BrakeClamp * Random.Range(currentErrorContainer.randomLowerLimit, currentErrorContainer.randomUpperLimit);
                 //   Debug.Log("brake clamp after change:" + master.BrakeClamp);
                    break;
            
                case ErrorType.SkipPort:
                    portSkippingStartTime = Time.time;
                    break;
                case ErrorType.Steering:
                    if (eventErrorLevel == ErrorLevel.MinorError)
                    {
                        master.Limit *= 0.75f;
                        master.BrakeClamp = 0.01f;
                    }
                    else if(eventErrorLevel == ErrorLevel.MiniError)
                    {
                        currentErrorContainer.randomLowerLimit = Random.Range(currentErrorContainer.randomLowerLimit, currentErrorContainer.randomUpperLimit);
                    }
                    break;
               
                default:
                    break;
            }
        
        }
        public SpeedChangeInfo NewGate(SpeedChangeInfo _change)
        {
            //update stored values
            UpdateSavedValues(_change);

            switch (eventErrortype)
            {
                case ErrorType.SpeedChange:
                   // Debug.Log("Port speedlimit before change:" + _change.SpeedChange);
                   
                    if (eventErrorLevel == ErrorLevel.MajorError)
                    {
                        _change.brakeClamp = 0f;
                        _change.SpeedChange = 500f;
                    }
                    else
                        _change.SpeedChange = _change.SpeedChange * Random.Range(currentErrorContainer.randomLowerLimit, currentErrorContainer.randomUpperLimit);
                    //  Debug.Log("Port speedlimit after change:" + _change.SpeedChange);
                    break;
                case ErrorType.BrakeClamp:
                    //Debug.Log("brake clamp before change:" + master.BrakeClamp);
                    _change.brakeClamp = _change.brakeClamp * Random.Range(currentErrorContainer.randomLowerLimit, currentErrorContainer.randomUpperLimit);
             
                  //  Debug.Log("brake clamp after change:" + master.BrakeClamp);
                    break;
             
                case ErrorType.SkipPort:
                    break;
                case ErrorType.Steering:
                    if (eventErrorLevel == ErrorLevel.MinorError)
                    {
                        _change.SpeedChange *= 0.75f;
                        _change.brakeClamp = 0.1f;
                    }
                        break;
           
                default:
                    break;
            }
            return _change;
        }
        public ControlValueHolder EventFixedUpdate(ControlValueHolder _values)
        {
            switch (eventErrortype)
            {
                case ErrorType.SpeedChange:
                    if (eventErrorLevel == ErrorLevel.MajorError)
                        _values.steer = Mathf.Clamp(_values.steer, -0.7f, 0.7f);
                    break;
                case ErrorType.BrakeClamp:
                   _values.brake = currentErrorContainer.randomLowerLimit;
                    break;
                case ErrorType.Steering:
                    if (currentErrorContainer.errorLevel == ErrorLevel.MiniError)
                    {
                        _values.steer += currentErrorContainer.randomLowerLimit;
                    }
                    else
                    {
                        if (_values.steer > 0.01f)
                        {
                            _values.steer += currentErrorContainer.randomLowerLimit;
                            // Debug.Log("Steer after change:" + _steer);
                        }
                        else if (_values.steer < -0.01f)
                        {
                            _values.steer += currentErrorContainer.randomUpperLimit;
                            // Debug.Log("Steer after change:" + _steer);
                        }
                    }
                    break;
                case ErrorType.SkipPort:
                    break;
                default:
                    break;
            }

            return _values;
           

        }
        private void UpdateSavedValues(SpeedChangeInfo _change)
        {
            backedUpCarValues.limit = _change.SpeedChange;
            backedUpCarValues.lookDistanceToLookOut = _change.distanceToLookOut;
            backedUpCarValues.distanceSpeedMultiplier = _change.distanceSpeedMultiplier;
            backedUpCarValues.brakeClamp = _change.brakeClamp;
        }
        public void EventEnds()
        {
            master.EventEndsUpdateValues(backedUpCarValues);
            Logger.Message("AI Event:"+eventErrortype+" "+eventErrorLevel+" ends. Player: " + master.GetComponent<CarControl>().CarMapPlayer.playerName);
        }
    }
}