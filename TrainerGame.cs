using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using InteractMemory;

namespace TrainerGame
{
    public class Trainer
    {
        public struct InfoProcessGame
        {
            public int pId;
            public IntPtr hWnd, moduleBase;
        };

        public enum OFFSET
        {
            MAINPLAYER = 0x10F4F4,
            ENEMYPLAYER = 0x10F4F8

        }

        public struct InfoPlayer
        {
            public int health, armor;
            public float xPos, yPos, zPos, xMouse, yMouse;
        };
        public struct PositionMouse
        {
            public float xMouse, yMouse;
        }
        public static InfoProcessGame startTrainer(string nameProc)
        {
            Process[] processAC = Process.GetProcessesByName(nameProc);
            var _ = new InfoProcessGame { };
            if(processAC.Length != 0)
            {
                _.pId = processAC[0].Id;
                _.hWnd = Memory.OpenProcess(_.pId);
                _.moduleBase = Memory.GetModuleBaseAddress(processAC[0], nameProc + ".exe");
                return _;
            }else
            {
                return _;
            }
        }

        public static InfoPlayer getInfoPlayer(IntPtr hWnd, IntPtr moduleBase, int offset, uint offsetLv1Enemy = 0x4)
        {
            var _ = new InfoPlayer { };
            IntPtr baseAddress = IntPtr.Add(moduleBase, offset);
            if (offset == (int)(OFFSET.MAINPLAYER))
            {
                // main player
                _.health = Memory.ReadInt(hWnd, baseAddress, 0xF8);
                _.armor = Memory.ReadInt(hWnd, baseAddress, 0xFC);

                _.xPos = Memory.ReadFloat(hWnd, baseAddress, 0x34);
                _.yPos = Memory.ReadFloat(hWnd, baseAddress, 0x38);
                _.zPos = Memory.ReadFloat(hWnd, baseAddress, 0x3C);

                _.xMouse = Memory.ReadFloat(hWnd, baseAddress, 0x40);
                _.yMouse = Memory.ReadFloat(hWnd, baseAddress, 0x44);

                return _;
            }
            else
            {
                //enemy player
                IntPtr pointer = (IntPtr)(Memory.ReadInt(hWnd, baseAddress, offsetLv1Enemy));
                IntPtr resultPointer = IntPtr.Add(pointer, 0xF8);
                _.health = Memory.ReadPointer(hWnd, resultPointer);
                _.armor = 0;
                //get address position of enemy
                IntPtr xPosAddr = IntPtr.Subtract(resultPointer, 0xC4);
                IntPtr yPosAddr = IntPtr.Add(xPosAddr, 0x4);
                IntPtr zPosAddr = IntPtr.Add(yPosAddr, 0x4);
                // get byte from 3 address above
                byte[] _xPosAddr = BitConverter.GetBytes(Memory.ReadPointer(hWnd, xPosAddr));
                byte[] _yPosAddr = BitConverter.GetBytes(Memory.ReadPointer(hWnd, yPosAddr));
                byte[] _zPosAddr = BitConverter.GetBytes(Memory.ReadPointer(hWnd, zPosAddr));
                //convert byte to 4 float
                _.xPos = BitConverter.ToSingle(_xPosAddr, 0);
                _.yPos = BitConverter.ToSingle(_yPosAddr, 0);
                _.zPos = BitConverter.ToSingle(_zPosAddr, 0);
                _.xMouse = 0;
                _.yMouse = 0;

                return _;
            }

        }

        
        public static int getCountPlayer(IntPtr hWnd,IntPtr moduleBase, int offset) {
            IntPtr baseAddress = IntPtr.Add(moduleBase, offset);
            return Memory.ReadPointer(hWnd, baseAddress);
        }
        public static float calcDistance3D(InfoPlayer destination, InfoPlayer source)
        {
            return (float)(Math.Sqrt(Math.Pow(destination.xPos - source.xPos, 2)+ Math.Pow(destination.yPos - source.yPos, 2)+ Math.Pow(destination.zPos - source.zPos, 2)));
        }
        public static PositionMouse calcPosMouse(float distance3D,InfoPlayer destination, InfoPlayer source)
        {
            var _ = new PositionMouse { };
            float pitchX = (float)(Math.Asin((destination.zPos - source.zPos)/distance3D))* 180/ 3.14f;
            float yawY = -(float)(Math.Atan2((destination.xPos - source.xPos) , (destination.yPos - source.yPos))) / 3.14f * 180 + 180 ;
            _.xMouse = yawY;
            _.yMouse = pitchX;
            return _;
        }
        public static Dictionary<int, InfoPlayer> getAllListEnemy(IntPtr hWnd, IntPtr moduleBase, int countEnemy)
        {
            Dictionary<int, InfoPlayer> listEnemy = new Dictionary<int, InfoPlayer>();
            int j = 0;
            for (int i = 4; i <= 4* countEnemy; i = (int)(IntPtr.Add((IntPtr)i, 4)))
            {
                listEnemy.Add(i, getInfoPlayer(hWnd, moduleBase, 0x10F4F8, (uint)i));
                j++;
                if (j > 31 || (getInfoPlayer(hWnd, moduleBase, 0x10F4F8, (uint)i)).health > 100 || (getInfoPlayer(hWnd, moduleBase, 0x10F4F8, (uint)i)).xPos == 0)
                {
                    break;
                }
            }
            return listEnemy;
        }

        public static InfoPlayer findEnemyCloset(InfoPlayer source, Dictionary<int, InfoPlayer> destinations)
        {
            var enemyCloset = new InfoPlayer { };
            float minDistance3D = 9999999, currentDistance3D;
            foreach(KeyValuePair<int,InfoPlayer> ele in destinations)
            {
                currentDistance3D = calcDistance3D(ele.Value, source);
                if(currentDistance3D < minDistance3D)
                {
                    minDistance3D = currentDistance3D;
                }
            }
            foreach(KeyValuePair<int, InfoPlayer> ele in destinations)
            {
                if(calcDistance3D(ele.Value,source) == minDistance3D)
                {
                    enemyCloset =  ele.Value;
                }
            }
            return enemyCloset;
        }

        public static void setFull(IntPtr hWnd, IntPtr baseAddress)
        {
            Memory.WriteInt(hWnd, baseAddress, 0xF8, 9337);
            Memory.WriteInt(hWnd, baseAddress, 0xFC, 9337);
            Memory.WriteInt(hWnd, baseAddress, 0x150, 9337);
            Memory.WriteInt(hWnd, baseAddress, 0x13C, 9337);
            Memory.WriteInt(hWnd, baseAddress, 0x158, 9337);
        }
        public static void Aimbot(IntPtr hWnd, IntPtr baseAddress, float distance3D, InfoPlayer destination, InfoPlayer source)
        {
            var pMouseAim = new PositionMouse { };
            pMouseAim = calcPosMouse(distance3D, destination, source);
            Memory.WriteFloat(hWnd, baseAddress, 0x40,pMouseAim.xMouse);
            Memory.WriteFloat(hWnd, baseAddress, 0x44, pMouseAim.yMouse);
        }

        public static void Teleport(IntPtr hWnd, IntPtr baseAddress, InfoPlayer destination)
        {
            Memory.WriteFloat(hWnd, baseAddress, 0x34, destination.xPos);
            Memory.WriteFloat(hWnd, baseAddress, 0x38, destination.yPos - 2f);
            Memory.WriteFloat(hWnd, baseAddress, 0x3C, destination.zPos);
        }

        public static void GodMode(IntPtr hWnd, IntPtr baseAddress)
        {
            uint nop = 2341507216;
            Memory.WriteInt(hWnd, baseAddress, 0x29D1F, (int)(nop));
        }
    }
}
