using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Animation 
{
    static public bool AnimateUp(float parameter, float endValue, Action<float> animation)
    {
        animation.Invoke(parameter);

        if (parameter >= endValue)
        {
            return true;
        }

        return false;
    }
    static public bool AnimateDown(float parameter, float endValue, Action<float> animation)
    {
        animation.Invoke(parameter);

        if (parameter <= endValue)
        {
            return true;
        }

        return false;
    }
    static public bool TranslateUpTo(float parameter, float endValue, Vector3 velocity, Action<Vector3> animation)
    {
        animation.Invoke(velocity);

        if (parameter >= endValue)
        {
            return true;
        }

        return false;
    }
}
