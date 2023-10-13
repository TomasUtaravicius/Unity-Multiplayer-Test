using System;
using Unity.Netcode;


    public struct DoorStateChangedEventMessage : INetworkSerializeByMemcpy
    {
        public bool IsDoorOpen;
    }

