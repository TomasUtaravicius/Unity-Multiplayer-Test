using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using KinematicCharacterController;

#if UNITY_EDITOR
public class PauseStateHandler
{
    [RuntimeInitializeOnLoadMethod()]
    public static void Init()
    {
        EditorApplication.pauseStateChanged += HandlePauseStateChange;
    }

    private static void HandlePauseStateChange(PauseState state)
    {
        foreach(KinematicCharacterMotor motor in KinematicCharacterSystem.CharacterMotors)
        {
            motor.SetPositionAndRotation(motor.Transform.position, motor.Transform.rotation, true);
        }
    }
}
#endif
