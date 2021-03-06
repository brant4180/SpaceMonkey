﻿using System;
using System.Runtime.InteropServices;

namespace MonsterGamesAPI
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
    public class MonsterGamesData
    {
        public float m11;
        public float m12;
        public float m13;
        public float m14;

        public float m21;
        public float m22;
        public float m23;
        public float m24;

        public float m31;
        public float m32;
        public float m33;
        public float m34;

        public float m41;
        public float m42;
        public float m43;
        public float m44;

        public bool paused;
        public float dt;

        public int gear;
        public int gears;
        public float engineRPM;

        public byte[] ToByteArray()
        {
            MonsterGamesData packet = this;
            int num = Marshal.SizeOf<MonsterGamesData>(packet);
            byte[] array = new byte[num];
            IntPtr intPtr = Marshal.AllocHGlobal(num);
            Marshal.StructureToPtr<MonsterGamesData>(packet, intPtr, false);
            Marshal.Copy(intPtr, array, 0, num);
            Marshal.FreeHGlobal(intPtr);
            return array;
        }
    }
}