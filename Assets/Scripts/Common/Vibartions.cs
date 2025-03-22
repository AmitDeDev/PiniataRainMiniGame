using UnityEngine;
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
        Debug.Log("Hard vibrations");
        HapticFeedback.HeavyFeedback();
    }
}
