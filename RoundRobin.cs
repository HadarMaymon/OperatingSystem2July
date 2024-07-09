using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Scheduling
{
    class RoundRobin : SchedulingPolicy
    {
        private Queue<int> processQueue = new Queue<int>();
        private int quantum;

        // Constructor properly named
        public RoundRobin(int iQuantum)
        {
            this.quantum = iQuantum;
        }

        // Implementing abstract method NextProcess
        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            int nextProcessId = -1;

            for (int i = 0; i < processQueue.Count; i++)
            {
                int processId = processQueue.Dequeue();
                ProcessTableEntry entry = dProcessTable[processId];
                dProcessTable[processId].Quantum = quantum;
                if (!entry.Done && !entry.Blocked)
                {
                    nextProcessId = processId;
                }
                processQueue.Enqueue(processId);
            }

            return nextProcessId;
        }

        // Implementing abstract method AddProcess
        public override void AddProcess(int iProcessId)
        {
            processQueue.Enqueue(iProcessId);
        }

        // Implementing abstract method RescheduleAfterInterrupt
        public override bool RescheduleAfterInterrupt()
        {
            return true;
        }
    }
}
