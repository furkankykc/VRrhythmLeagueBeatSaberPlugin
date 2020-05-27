﻿using Lidgren.Network;
using UnityEngine;

namespace VRrhythmLeague.Misc
{
    static class Serialization
    {
        public static void AddToMessage(this Vector3 vect, NetOutgoingMessage msg)
        {
            msg.Write(vect.x);
            msg.Write(vect.y);
            msg.Write(vect.z);
        }

        public static void AddToMessage(this Quaternion vect, NetOutgoingMessage msg)
        {
            msg.Write(vect.x);
            msg.Write(vect.y);
            msg.Write(vect.z);
            msg.Write(vect.w);
        }

        public static Vector3 ReadVector3(this NetIncomingMessage msg)
        {
            Vector3 vect = new Vector3(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
            return vect;
        }

        public static Quaternion ReadQuaternion(this NetIncomingMessage msg)
        {
            Quaternion vect = new Quaternion(msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat(), msg.ReadFloat());
            return vect;
        }


    }
}
