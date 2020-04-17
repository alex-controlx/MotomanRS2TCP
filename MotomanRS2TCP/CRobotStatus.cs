using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotomanRS2TCP
{
    public class CRobotStatus
    {
        private short m_oldSW1 = -1;
        private short m_oldSW2 = -1;

        public CRobotStatus() {
            SetAndCheckSWs(-1, -1);
        }

        public bool isStep { get; set; }
        public bool is1Cycle { get; set; }
        public bool isAuto { get; set; }
        public bool isOperating { get; set; }
        public bool isSafeSpeed { get; set; }
        public bool isTeach { get; set; }
        public bool isPlay { get; set; }
        public bool isCommandRemote { get; set; }
        public bool isPlaybackBoxHold { get; set; }
        public bool isPPHold { get; set; }
        public bool isExternalHold { get; set; }
        public bool isCommandHold { get; set; }
        public bool isAlarm { get; set; }
        public bool isError { get; set; }
        public bool isServoOn { get; set; }
        public short sw1 { get => m_oldSW1; }
        public short sw2 { get => m_oldSW2; }

        public bool SetAndCheckSWs(short sw1, short sw2)
        {

            if (sw1 == m_oldSW1 && sw2 == m_oldSW2) return false;

            isStep = (sw1 & (1 << 0)) > 0 ? true : false;
            is1Cycle = (sw1 & (1 << 1)) > 0 ? true : false;
            isAuto = (sw1 & (1 << 2)) > 0 ? true : false;
            isOperating = (sw1 & (1 << 3)) > 0 ? true : false;
            isSafeSpeed = (sw1 & (1 << 4)) > 0 ? true : false;
            isTeach = (sw1 & (1 << 5)) > 0 ? true : false;
            isPlay = (sw1 & (1 << 6)) > 0 ? true : false;
            isCommandRemote = (sw1 & (1 << 7)) > 0 ? true : false;

            isPlaybackBoxHold = (sw2 & (1 << 0)) > 0 ? true : false;
            isPPHold = (sw2 & (1 << 1)) > 0 ? true : false;
            isExternalHold = (sw2 & (1 << 2)) > 0 ? true : false;
            isCommandHold = (sw2 & (1 << 3)) > 0 ? true : false;
            isAlarm = (sw2 & (1 << 4)) > 0 ? true : false;
            isError = (sw2 & (1 << 5)) > 0 ? true : false;
            isServoOn = (sw2 & (1 << 6)) > 0 ? true : false;

            m_oldSW1 = sw1;
            m_oldSW2 = sw2;

            return true;
        }

    }
}
