﻿using System;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Numerics;
using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.MemoryMappedFiles;
using MonsterGamesAPI;
using GenericTelemetryProvider.Properties;
using System.Threading.Tasks;

namespace GenericTelemetryProvider
{
    class MonsterGamesTelemetryProvider : GenericProviderBase
    {
        Thread t;

        public MonsterGamesUI ui;
        MonsterGamesData data;
        int readPort = 13371;
        private IPEndPoint senderIP;                   // IP address of the sender for the udp connection used by the worker thread
        uint lastPacketId = 0;

        public override void Run()
        {
            base.Run();

            updateDelay = 18;
            maxAccel2DMagSusp = 6.0f;
            telemetryPausedTime = 1.5f;

            t = new Thread(ReadTelemetry);
            t.Start();
        }

        public override void Stop()
        {
            base.Stop();


        }

        void PerformInjection()
        {
            AutoResetEvent injectionEvent = new AutoResetEvent(false);
            string[] processNames = new string[] { "NascarHeat5", "NascarHeat4", "AllAmericanRacing", "SprintCarRacing" };
                
            foreach(string processName in processNames)
            {
                //processName
                Task.Run(delegate ()
                {
                    InjectionManager.Monitor(processName, Resources.MonsterGamesTelemetry, injectionEvent);
                });
            }

            while (true)
            {
                injectionEvent.WaitOne();
                ui.StatusTextChanged(InjectionManager.GetStatus());

                if (InjectionManager.GetState() == InjectionManager.State.Failed || InjectionManager.GetState() == InjectionManager.State.Success)
                    break;
            }
        }

        void ReadTelemetry()
        {
            PerformInjection();

            if (InjectionManager.GetState() == InjectionManager.State.Failed)
                return;

            UdpClient socket = new UdpClient();
            socket.ExclusiveAddressUse = false;
            socket.Client.Bind(new IPEndPoint(IPAddress.Any, readPort));

            StartSending();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            Stopwatch processSW = new Stopwatch();
            processSW.Start();

             //read and process
            while (!IsStopped)
            {
                try
                {
                    //wait for telemetry
                    if (socket.Available == 0)
                    {
                        using (var sleeper = new ManualResetEvent(false))
                        {
                            sleeper.WaitOne(1);
                        }
                        continue;
                    }

                    Byte[] received = socket.Receive(ref senderIP);

                    //                    var alloc = GCHandle.Alloc(received, GCHandleType.Pinned);
                    //                    data = (MonsterGamesData)Marshal.PtrToStructure(alloc.AddrOfPinnedObject(), typeof(MonsterGamesData));
                    //                    alloc.Free();


                    if (socket.Available == 0)
                    {
                        data = JsonConvert.DeserializeObject<MonsterGamesData>(System.Text.Encoding.UTF8.GetString(received));

                        if (data.packetId < lastPacketId && Math.Abs((long)data.packetId - (long)lastPacketId) < 1000)
                        {
                            continue;
                        }

                        lastPacketId = data.packetId;

                        float frameDT = (float)sw.Elapsed.TotalSeconds;
                        Console.WriteLine("FrameDT: " + frameDT);
                        frameDT = data.dt;
                        //                        float updateDelaySecs = (updateDelay / 1000.0f);
                        //                        frameDT = updateDelaySecs - (updateDelaySecs - frameDT);// 0.02f;
                        //frameDT = data.dt;
                        sw.Restart();
                        if (!data.paused)
                        {
                            ProcessMonsterGamesData(frameDT);
                        }
                        /*
                                                int processTime = (int)processSW.ElapsedMilliseconds;
                                                Thread.Sleep(Math.Max(0, 20 - processTime));
                                                processSW.Restart();
                                                */
                                                /*
                        int processTime = (int)processSW.ElapsedMilliseconds;
                        Console.WriteLine("ProcessTime: " + processTime);

                        using (var sleeper = new ManualResetEvent(false))
                        {

                            //updateDelay = Math.Min(updateDelay, processTime);
                            sleeper.WaitOne(Math.Max(0, updateDelay - processTime), false);
//                            sleeper.WaitOne(updateDelay, false);
                        }
                        processSW.Restart();
                        */
                    }
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000);
                }

            }

            StopSending();
        }

            

        void ProcessMonsterGamesData(float _dt)
        {
            if (data == null)
                return;

            transform = new Matrix4x4();
            transform.M11 = data.m11;
            transform.M12 = data.m12;
            transform.M13 = data.m13;
            transform.M14 = 0.0f;// data.m14;
            transform.M21 = data.m21;
            transform.M22 = data.m22;
            transform.M23 = data.m23;
            transform.M24 = 0.0f;// data.m24;
            transform.M31 = data.m31;
            transform.M32 = data.m32;
            transform.M33 = data.m33;
            transform.M34 = 0.0f;// data.m34;
            transform.M41 = data.m41;
            transform.M42 = data.m42;
            transform.M43 = data.m43;
            transform.M44 = 1.0f;// data.m44;

            ProcessTransform(transform, _dt);// data.dt);
        }

        public override bool ProcessTransform(Matrix4x4 newTransform, float inDT)
        {
            if (!base.ProcessTransform(newTransform, inDT))
                return false;

            ui.DebugTextChanged("dt: " + inDT + "\n" + JsonConvert.SerializeObject(filteredData, Formatting.Indented) + "\n steer: " + InputModule.Instance.controller.leftThumb.X + "\n accel: " + InputModule.Instance.controller.rightTrigger + "\n brake: " + InputModule.Instance.controller.leftTrigger);

            SendFilteredData();

            return true;
        }

        public override bool ExtractFwdUpRht()
        {            
            return base.ExtractFwdUpRht();

        }

        public override bool CheckLastFrameValid()
        {
            return base.CheckLastFrameValid();
        }

        public override void FilterDT()
        {
            if (dt <= 0)
                dt = 0.000001f;
        }

        public override bool CalcPosition()
        {

            Vector3 currRawPos = new Vector3(transform.M41, transform.M42, transform.M43);

            rawData.position_x = currRawPos.X;
            rawData.position_y = currRawPos.Y;
            rawData.position_z = currRawPos.Z;

            lastRawPos = currRawPos;

            //filter position
            FilterModuleCustom.Instance.Filter(rawData, ref filteredData, posKeyMask, true);

            worldPosition = new Vector3((float)filteredData.position_x, (float)filteredData.position_y, (float)filteredData.position_z);

            return true;
        }

        public override void CalcVelocity()
        {
            Vector3 worldVelocity = (worldPosition - lastPosition) / dt;
            lastWorldVelocity = worldVelocity;

            lastPosition = transform.Translation = worldPosition;

            Matrix4x4 rotation = new Matrix4x4();
            rotation = transform;
            rotation.M41 = 0.0f;
            rotation.M42 = 0.0f;
            rotation.M43 = 0.0f;
            rotation.M44 = 1.0f;

            Matrix4x4 rotInv = new Matrix4x4();
            Matrix4x4.Invert(rotation, out rotInv);

            //transform world velocity to local space
            Vector3 localVelocity = Vector3.Transform(worldVelocity, rotInv);

//            localVelocity.X = -localVelocity.X;

            rawData.local_velocity_x = localVelocity.X;
            rawData.local_velocity_y = localVelocity.Y;
            rawData.local_velocity_z = localVelocity.Z;

//            rawData.local_velocity_x = -(float)rawData.local_velocity_x;
        }

        public override void FilterVelocity()
        {
            //filter local velocity
            FilterModuleCustom.Instance.Filter(rawData, ref filteredData, velKeyMask, false);
        }

        public override void CalcAcceleration()
        {
            //            base.CalcAcceleration();

            rawData.gforce_lateral = data.accelX;
            rawData.gforce_vertical = data.accelY;
            rawData.gforce_longitudinal = data.accelZ;

            FilterModuleCustom.Instance.Filter(rawData, ref filteredData, accelKeyMask, false);
        }

        public override void CalcAngles()
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(transform);

            Vector3 pyr = Utils.GetPYRFromQuaternion(quat);

            rawData.pitch = pyr.X;
            rawData.yaw = pyr.Y;
            rawData.roll = Utils.LoopAngleRad(pyr.Z, (float)Math.PI * 0.5f);
        }

        public override void SimulateEngine()
        {
            rawData.max_rpm = 6000;
            rawData.max_gears = data.gears;
            rawData.gear = data.gear;
            rawData.idle_rpm = 700;

            Vector3 localVelocity = new Vector3((float)filteredData.local_velocity_x, (float)filteredData.local_velocity_y, (float)filteredData.local_velocity_z);

            filteredData.speed = localVelocity.Length();
        }

        public override void ProcessInputs()
        {
            base.ProcessInputs();

            filteredData.engine_rate = Math.Max(700, Math.Min(6000, 700 + (data.engineRPM * (6000-700))));
        }
    }
}
