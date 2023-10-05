using System;
using Unity.BossRoom.Gameplay.GameplayObjects;
using UnityEngine;


    /// <summary>
    /// Shared Network logic for targetable, NPC, Interactable 
    /// </summary>
    public class InteractState : MonoBehaviour, ITargetable
    {
        public bool IsNpc => true;

        public bool IsValidTarget => true;
    }

