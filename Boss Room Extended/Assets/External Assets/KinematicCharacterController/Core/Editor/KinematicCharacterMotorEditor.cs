using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace KinematicCharacterController
{
    [CustomEditor(typeof(KinematicCharacterMotor))]
    public class KinematicCharacterMotorEditor : Editor
    {
        protected virtual void OnSceneGUI()
        {            
            KinematicCharacterMotor motor = (target as KinematicCharacterMotor);
            if (motor)
            {
                Vector3 characterBottom = motor.transform.position + (motor.Capsule.center + (-Vector3.up * (motor.Capsule.height * 0.5f)));

                Handles.color = Color.yellow;
                Handles.CircleHandleCap(
                    0, 
                    characterBottom + (motor.transform.up * motor.MaxStepHeight), 
                    Quaternion.LookRotation(motor.transform.up, motor.transform.forward), 
                    motor.Capsule.radius + 0.1f, 
                    EventType.Repaint);
            }
        }
    }
}
#endif
