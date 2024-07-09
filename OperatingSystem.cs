using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{

    class OperatingSystem
    {
        public Disk Disk { get; private set; }
        public CPU CPU { get; private set; }
        private Dictionary<int, ProcessTableEntry> m_dProcessTable;
        private List<ReadTokenRequest> m_lReadRequests;
        private int m_cProcesses;
        private SchedulingPolicy m_spPolicy;
        private static int IDLE_PROCESS_ID = 0;

        public OperatingSystem(CPU cpu, Disk disk, SchedulingPolicy sp)
        {
            CPU = cpu;
            Disk = disk;
            m_dProcessTable = new Dictionary<int, ProcessTableEntry>();
            m_lReadRequests = new List<ReadTokenRequest>();
            cpu.OperatingSystem = this;
            disk.OperatingSystem = this;
            m_spPolicy = sp;

            //create an "idle" process here
            CreateIdleProcess();
        }

        public void CreateIdleProcess()
        {
            IdleCode idleCode = new IdleCode();  // Directly instantiate IdleCode
            ProcessTableEntry entry = new ProcessTableEntry(IDLE_PROCESS_ID, "IdleProcess", idleCode);
            entry.Priority = int.MinValue;  // Assuming minimum priority for simplicity
            entry.StartTime = CPU.TickCount;

            m_dProcessTable[IDLE_PROCESS_ID] = entry;
            m_spPolicy.AddProcess(IDLE_PROCESS_ID);
            m_cProcesses++;
        }


        public void CreateProcess(string sCodeFileName)
        {
            Code code = new Code(sCodeFileName);
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;
        }
        public void CreateProcess(string sCodeFileName, int iPriority)
        {
            Code code = new Code(sCodeFileName);
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
            m_dProcessTable[m_cProcesses].Priority = iPriority;
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;
        }

        public void ProcessTerminated(Exception e)
        {
            if (e != null)
                Console.WriteLine("Process " + CPU.ActiveProcess + " terminated unexpectedly. " + e);
            m_dProcessTable[CPU.ActiveProcess].Done = true;
            m_dProcessTable[CPU.ActiveProcess].Console.Close();
            m_dProcessTable[CPU.ActiveProcess].EndTime = CPU.TickCount;
            ActivateScheduler();
        }

        public void TimeoutReached()
        {
            ActivateScheduler();
        }

        public void ReadToken(string sFileName, int iTokenNumber, int iProcessId, string sParameterName)
        {
            ReadTokenRequest request = new ReadTokenRequest();
            request.ProcessId = iProcessId;
            request.TokenNumber = iTokenNumber;
            request.TargetVariable = sParameterName;
            request.Token = null;
            request.FileName = sFileName;
            m_dProcessTable[iProcessId].Blocked = true;
            if (Disk.ActiveRequest == null)
                Disk.ActiveRequest = request;
            else
                m_lReadRequests.Add(request);
            CPU.ProgramCounter = CPU.ProgramCounter + 1;
            ActivateScheduler();
        }

        public void Interrupt(ReadTokenRequest rFinishedRequest)
        {
            //implement an "end read request" interrupt handler.
            //translate the returned token into a value (double). 
            //when the token is null, EOF has been reached.
            //write the value to the appropriate address space of the calling process.
            //activate the next request in queue on the disk.
            double value = double.NaN;
            if (rFinishedRequest.Token != null)
            {
                value = double.Parse(rFinishedRequest.Token);
            }

            ProcessTableEntry requestProcess = m_dProcessTable[rFinishedRequest.ProcessId];
            requestProcess.AddressSpace[rFinishedRequest.TargetVariable] = value;
            requestProcess.Blocked = false;
            requestProcess.LastCPUTime = CPU.TickCount;
            m_dProcessTable[rFinishedRequest.ProcessId] = requestProcess;

            if (m_lReadRequests.Count > 0)
            {
                ReadTokenRequest nextRequest = m_lReadRequests[0];
                m_lReadRequests.RemoveAt(0);
                Disk.ActiveRequest = nextRequest;
            }

            if (m_spPolicy.RescheduleAfterInterrupt())
            {
                ActivateScheduler();
            }
        }

        private ProcessTableEntry ContextSwitch(int iEnteringProcessId) //HADAR
        {
            //your code here
            //implement a context switch, switching between the currently active process on the CPU to the process with pid iEnteringProcessId
            //You need to switch the following: ActiveProcess, ActiveAddressSpace, ActiveConsole, ProgramCounter.
            //All values are stored in the process table (m_dProcessTable)
            //Our CPU does not have registers, so we do not store or switch register values.
            //returns the process table information of the outgoing process
            //After this method terminates, the execution continues with the new process
            // Check if there is a current active process and it exists in the dictionary
            ProcessTableEntry oldEntry = null;
            if (CPU.ActiveProcess != -1 && m_dProcessTable.ContainsKey(CPU.ActiveProcess))
            {
                m_dProcessTable[CPU.ActiveProcess].ProgramCounter = CPU.ProgramCounter;
                m_dProcessTable[CPU.ActiveProcess].AddressSpace = CPU.ActiveAddressSpace;
                m_dProcessTable[CPU.ActiveProcess].Console = CPU.ActiveConsole;
                m_dProcessTable[CPU.ActiveProcess].LastCPUTime = CPU.TickCount;
                oldEntry = m_dProcessTable[CPU.ActiveProcess];
            }

            // Verify that the entering process ID exists in the dictionary
            if (!m_dProcessTable.ContainsKey(iEnteringProcessId))
            {
                throw new ArgumentException($"The entering process ID {iEnteringProcessId} does not exist in the process table.");
            }

            ProcessTableEntry newEntry = m_dProcessTable[iEnteringProcessId];
            int maxTime = newEntry.MaxStarvation;
            if (maxTime < CPU.TickCount - newEntry.LastCPUTime) {
                m_dProcessTable[iEnteringProcessId].MaxStarvation = CPU.TickCount - newEntry.LastCPUTime;
            }

            // Switch to the new process
            CPU.ActiveProcess = iEnteringProcessId;
            CPU.ActiveAddressSpace = newEntry.AddressSpace;
            CPU.ActiveConsole = newEntry.Console;
            CPU.ProgramCounter = newEntry.ProgramCounter;
            CPU.RemainingTime = newEntry.Quantum;

            return oldEntry; // Return the old process entry, which may be null if there was no active process
        }

        public void ActivateScheduler()
        {
            int iNextProcessId = m_spPolicy.NextProcess(m_dProcessTable);
            if (iNextProcessId == -1)
            {
                Console.WriteLine("All processes terminated or blocked.");
                CPU.Done = true;
            }
            else
            {
              //  bool bOnlyIdleRemains = false;
                //add code here to check if only the Idle process remains
                bool bOnlyIdleRemains = m_dProcessTable.All(p => p.Value.Done || p.Key == IDLE_PROCESS_ID);
                if (bOnlyIdleRemains)
                {
                    Console.WriteLine("Only idle process remains.");
                    CPU.Done = true;
                }
                else
                {
                    ContextSwitch(iNextProcessId);
                }
            }
        }
        public double AverageTurnaround()
        {
            //Compute the average time from the moment that a process enters the system until it terminates.
            int completedProcesses = 0;
            double totalTurnaroundTime = 0;

            foreach (var entry in m_dProcessTable)
            {
                if (entry.Value.EndTime != -1)
                { // Check if the process has completed
                    totalTurnaroundTime += (entry.Value.EndTime - entry.Value.StartTime);
                    completedProcesses++;
                }
            }

            if (completedProcesses > 0)
            {
                return totalTurnaroundTime / completedProcesses;
            }
            return 0; // Return 0 if no processes have completed to avoid division by zero
        }


        public int MaximalStarvation()
        {
            //Compute the maximal time that some project has waited in a ready stage without receiving CPU time.
            int maxStarvation = 0;

            foreach (var entry in m_dProcessTable)
            {
                if (entry.Value.MaxStarvation > maxStarvation)
                {
                    maxStarvation = entry.Value.MaxStarvation;
                }
            }

            return maxStarvation;
        }
    }
}
      