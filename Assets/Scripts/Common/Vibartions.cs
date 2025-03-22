using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CandyCoded;
using CandyCoded.HapticFeedback;

public class Vibartions 
{
    public void DefaultVibration()
    {
        Debug.Log("Default Vibrations");
        Handheld.Vibrate();
    }

    public void MediumVibration()
    {
        Debug.Log("Medium Vibrations");
        HapticFeedback.MediumFeedback();
    }

    public void HardVibration()
    {
        Debug.Log("Hard vibrations");
        HapticFeedback.HeavyFeedback();
    }
}
