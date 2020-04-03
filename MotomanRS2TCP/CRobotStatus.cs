using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotomanRS2TCP
{
    class CRobotStatus
    {
        private bool m_IsStep;
        private bool m_Is1Cycle;
        private bool m_IsAuto;
        private bool m_IsOperating;
        private bool m_IsSafeSpeed;
        private bool m_IsTeach;
        private bool m_IsPlay;
        private bool m_IsCommandRemote;
        private bool m_IsPlaybackBoxHold;
        private bool m_IsPPHold;
        private bool m_IsExternalHold;
        private bool m_IsCommandHold;
        private bool m_IsAlarm;
        private bool m_IsError;
        private bool m_IsServoOn;
        private short m_oldSW1 = -1;
        private short m_oldSW2 = -1;

        public CRobotStatus() {
            this.SetAndCheckSWs(-1, -1);
        }

        public bool IsStep { get => m_IsStep; set => m_IsStep = value; }
        public bool Is1Cycle { get => m_Is1Cycle; set => m_Is1Cycle = value; }
        public bool IsAuto { get => m_IsAuto; set => m_IsAuto = value; }
        public bool IsOperating { get => m_IsOperating; set => m_IsOperating = value; }
        public bool IsSafeSpeed { get => m_IsSafeSpeed; set => m_IsSafeSpeed = value; }
        public bool IsTeach { get => m_IsTeach; set => m_IsTeach = value; }
        public bool IsPlay { get => m_IsPlay; set => m_IsPlay = value; }
        public bool IsCommandRemote { get => m_IsCommandRemote; set => m_IsCommandRemote = value; }
        public bool IsPlaybackBoxHold { get => m_IsPlaybackBoxHold; set => m_IsPlaybackBoxHold = value; }
        public bool IsPPHold { get => m_IsPPHold; set => m_IsPPHold = value; }
        public bool IsExternalHold { get => m_IsExternalHold; set => m_IsExternalHold = value; }
        public bool IsCommandHold { get => m_IsCommandHold; set => m_IsCommandHold = value; }
        public bool IsAlarm { get => m_IsAlarm; set => m_IsAlarm = value; }
        public bool IsError { get => m_IsError; set => m_IsError = value; }
        public bool IsServoOn { get => m_IsServoOn; set => m_IsServoOn = value; }
        public short SW1 { get => m_oldSW1; }
        public short SW2 { get => m_oldSW2; }

        public bool SetAndCheckSWs(short sw1, short sw2)
        {

            if (sw1 == m_oldSW1 && sw2 == m_oldSW2) return false;

            IsStep = (sw1 & (1 << 0)) > 0 ? true : false;
            Is1Cycle = (sw1 & (1 << 1)) > 0 ? true : false;
            IsAuto = (sw1 & (1 << 2)) > 0 ? true : false;
            IsOperating = (sw1 & (1 << 3)) > 0 ? true : false;
            IsSafeSpeed = (sw1 & (1 << 4)) > 0 ? true : false;
            IsTeach = (sw1 & (1 << 5)) > 0 ? true : false;
            IsPlay = (sw1 & (1 << 6)) > 0 ? true : false;
            IsCommandRemote = (sw1 & (1 << 7)) > 0 ? true : false;

            IsPlaybackBoxHold = (sw2 & (1 << 0)) > 0 ? true : false;
            IsPPHold = (sw2 & (1 << 1)) > 0 ? true : false;
            IsExternalHold = (sw2 & (1 << 2)) > 0 ? true : false;
            IsCommandHold = (sw2 & (1 << 3)) > 0 ? true : false;
            IsAlarm = (sw2 & (1 << 4)) > 0 ? true : false;
            IsError = (sw2 & (1 << 5)) > 0 ? true : false;
            IsServoOn = (sw2 & (1 << 6)) > 0 ? true : false;

            m_oldSW1 = sw1;
            m_oldSW2 = sw2;

            return true;
        }

    }
}
