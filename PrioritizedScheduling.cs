using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Generic; // Importing the package for the Dictionary data structure

namespace Scheduling
{
    class PrioritizedScheduling : SchedulingPolicy
    {
        private Queue<int> processQueue = new Queue<int>();
        private int quantum;

        public PrioritizedScheduling(int iQuantum)
        {
            quantum = iQuantum;
        }

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            int nextProcessId = -1;

            Queue<int> sortedPriorityQueue = new Queue<int>(processQueue.OrderByDescending((processId) => dProcessTable[processId].Priority));

            for (int i = 0; i < sortedPriorityQueue.Count; i++)
            {
                int processId = sortedPriorityQueue.Dequeue();
                sortedPriorityQueue.Enqueue(processId);    
                ProcessTableEntry entry = dProcessTable[processId];
                dProcessTable[processId].Quantum = quantum;
                if (!entry.Done && !entry.Blocked)
                {
                    nextProcessId = processId;
                    break;
                }
            }

            return nextProcessId;
        }

        public override void AddProcess(int iProcessId)
        {
            processQueue.Enqueue(iProcessId);
        }

        public override bool RescheduleAfterInterrupt()
        {
            return true; 
        }
    }
}
