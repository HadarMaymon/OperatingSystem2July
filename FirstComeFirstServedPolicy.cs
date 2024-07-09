using System;
using System.Collections.Generic;
using System.Linq;

namespace Scheduling {


    class FirstComeFirstServedPolicy : SchedulingPolicy
    {
        private Queue<int> processQueue = new Queue<int>();

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            int nextProcessId = -1;

            for (int i = 0; i < processQueue.Count; i++) {
                int processId = processQueue.Dequeue();
                ProcessTableEntry entry = dProcessTable[processId];
                if (!entry.Done && !entry.Blocked) { 
                    nextProcessId = processId;
                }
                processQueue.Enqueue(processId);
            }

            return nextProcessId;
        }

        public override void AddProcess(int iProcessId)
        {   
            processQueue.Enqueue(iProcessId);
        }

        public override bool RescheduleAfterInterrupt()
        {
            return false;
        }
    }

}
