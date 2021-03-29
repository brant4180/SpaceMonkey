﻿using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace WarplanesWW1Telemetry
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public class WarplanesWW1Data
    {
        public uint packetId;

        public float posX;
        public float posY;
        public float posZ;

        public float pitch;
        public float yaw;
        public float roll;

        public float velX;
        public float velY;
        public float velZ;

        public float accelX;
        public float accelY;
        public float accelZ;

        public float pitchVel;
        public float yawVel;
        public float rollVel;

        public float pitchAccel;
        public float yawAccel;
        public float rollAccel;

        public bool paused;
        public float dt;

        public float engineRPM;


        public byte[] ToByteArray()
        {
            WarplanesWW1Data packet = this;
            int num = Marshal.SizeOf<WarplanesWW1Data>(packet);
            byte[] array = new byte[num];
            IntPtr intPtr = Marshal.AllocHGlobal(num);
            Marshal.StructureToPtr<WarplanesWW1Data>(packet, intPtr, false);
            Marshal.Copy(intPtr, array, 0, num);
            Marshal.FreeHGlobal(intPtr);
            return array;

        }
    }
}
