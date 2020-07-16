﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LiveSplit.ComponentUtil;
using LiveSplit.SourceSplit.GameSpecific;

namespace LiveSplit.SourceSplit
{
    // change back to struct if we ever need to give a copy of the state
    // to the ui thread
    class GameState
    {
        public const int ENT_INDEX_PLAYER = 1;

        public Process GameProcess;
        public GameOffsets GameOffsets;

        public HostState HostState;
        public HostState PrevHostState;

        public SignOnState SignOnState;
        public SignOnState PrevSignOnState;

        public ServerState ServerState;
        public ServerState PrevServerState;

        public string CurrentMap;
        public string GameDir;

        public float IntervalPerTick;
        public int RawTickCount;
        public int TickBase;
        public int TickCount;
        public float TickTime;

        public FL PlayerFlags;
        public FL PrevPlayerFlags;
        public Vector3f PlayerPosition;
        public Vector3f PrevPlayerPosition;
        public int PlayerViewEntityIndex;
        public int PrevPlayerViewEntityIndex;
        public int PlayerParentEntityHandle;
        public int PrevPlayerParentEntityHandle;

        public CEntInfoV2 PlayerEntInfo;
        public GameSupport GameSupport;
        public int UpdateCount;

        public GameState(Process game, GameOffsets offsets)
        {
            this.GameProcess = game;
            this.GameOffsets = offsets;
        }

        public CEntInfoV2 GetEntInfoByIndex(int index)
        {
            Debug.Assert(this.GameOffsets.EntInfoSize > 0);

            CEntInfoV2 ret;

            IntPtr addr = this.GameOffsets.GlobalEntityListPtr + ((int)this.GameOffsets.EntInfoSize * index);

            if (this.GameOffsets.EntInfoSize == CEntInfoSize.HL2)
            {
                CEntInfoV1 v1;
                this.GameProcess.ReadValue(addr, out v1);
                ret = CEntInfoV2.FromV1(v1);
            }
            else
            {
                this.GameProcess.ReadValue(addr, out ret);
            }

            return ret;
        }

        // warning: expensive -  7ms on i5
        // do not call frequently!
        public IntPtr GetEntityByName(string name)
        {
            const int MAX_ENTS = 2048; // TODO: is portal2's max higher?

            for (int i = 0; i < MAX_ENTS; i++) 
            {
                CEntInfoV2 info = this.GetEntInfoByIndex(i);
                if (info.EntityPtr == IntPtr.Zero)
                    continue;

                IntPtr namePtr;
                this.GameProcess.ReadPointer(info.EntityPtr + this.GameOffsets.BaseEntityTargetNameOffset, false, out namePtr);
                if (namePtr == IntPtr.Zero)
                    continue;

                string n;
                this.GameProcess.ReadString(namePtr, ReadStringType.ASCII, 32, out n);  // TODO: find real max len
                if (n == name)
                    return info.EntityPtr;
            }

            return IntPtr.Zero;
        }

        // warning: expensive -  7ms on i5
        // do not call frequently!
        public CEntInfoV2 GetEntInfoByName(string name)
        {
            const int MAX_ENTS = 2048; // TODO: is portal2's max higher?

            for (int i = 0; i < MAX_ENTS; i++)
            {
                CEntInfoV2 info = this.GetEntInfoByIndex(i);
                if (info.EntityPtr == IntPtr.Zero)
                    continue;

                IntPtr namePtr;
                this.GameProcess.ReadPointer(info.EntityPtr + this.GameOffsets.BaseEntityTargetNameOffset, false, out namePtr);
                if (namePtr == IntPtr.Zero)
                    continue;

                string n;
                this.GameProcess.ReadString(namePtr, ReadStringType.ASCII, 32, out n);  // TODO: find real max len
                if (n == name)
                    return info;
            }

            return new CEntInfoV2();
        }

        public int GetEntIndexByName(string name)
        {
            const int MAX_ENTS = 2048; // TODO: is portal2's max higher?

            for (int i = 0; i < MAX_ENTS; i++)
            {
                CEntInfoV2 info = this.GetEntInfoByIndex(i);
                if (info.EntityPtr == IntPtr.Zero)
                    continue;

                IntPtr namePtr;
                this.GameProcess.ReadPointer(info.EntityPtr + this.GameOffsets.BaseEntityTargetNameOffset, false, out namePtr);
                if (namePtr == IntPtr.Zero)
                    continue;

                string n;
                this.GameProcess.ReadString(namePtr, ReadStringType.ASCII, 32, out n);  // TODO: find real max len
                if (n == name)
                    return i;
            }

            return -1;
        }
    }

    struct GameOffsets
    {
        public IntPtr CurTimePtr;
        public IntPtr TickCountPtr => this.CurTimePtr + 12;
        public IntPtr IntervalPerTickPtr => this.TickCountPtr + 4;
        public IntPtr SignOnStatePtr;
        public IntPtr CurMapPtr;
        public IntPtr GlobalEntityListPtr;
        public IntPtr GameDirPtr;
        public IntPtr HostStatePtr;
        // note: only valid during host states: NewGame, ChangeLevelSP, ChangeLevelMP
        public IntPtr HostStateLevelNamePtr => this.HostStatePtr + (4*8); // note: this may not work pre-ep1 (ancient engine)
        public IntPtr ServerStatePtr;

        public CEntInfoSize EntInfoSize;

        public int BaseEntityFlagsOffset;
        public int BaseEntityEFlagsOffset => this.BaseEntityFlagsOffset > 0 ? this.BaseEntityFlagsOffset - 4 : -1;
        public int BaseEntityAbsOriginOffset;
        public int BaseEntityTargetNameOffset;
        public int BaseEntityParentHandleOffset;
        public int BasePlayerViewEntity;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CEntInfoV1
    {
        public int m_pEntity;
        public int m_SerialNumber;
        public int m_pPrev;
        public int m_pNext;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct CEntInfoV2
    {
        public int m_pEntity;
        public int m_SerialNumber;
        public int m_pPrev;
        public int m_pNext;
        public int m_targetname;
        public int m_classname;

        public IntPtr EntityPtr => (IntPtr)this.m_pEntity;

        public static CEntInfoV2 FromV1(CEntInfoV1 v1)
        {
            var ret = new CEntInfoV2();
            ret.m_pEntity = v1.m_pEntity;
            ret.m_SerialNumber = v1.m_SerialNumber;
            ret.m_pPrev = v1.m_pPrev;
            ret.m_pNext = v1.m_pNext;
            return ret;
        }
    }
}
