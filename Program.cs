using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Envanter3
{
    class Program
    {
        static void Main(string[] args)
        {
            int customerNo = 15000000;

            ProcessExecutor.ExecuteProcess("AccountingModule", "PaySalaries", new object[] { customerNo });

            ProcessExecutor.ExecuteProcess("AccountingModule", "ReceiptYearlyAmount", new object[] { customerNo });

            ProcessExecutor.ExecuteWaitingProcess();

            Console.ReadKey();
        }
    }

    public class Process
    {
        public string moduleName { get; private set; }
        public string methodName { get; private set; }
        public object[] inputs { get; private set; }

        public Process(string moduleName, string methodName, object[] inputs)
        {
            this.moduleName = moduleName;
            this.methodName = methodName;
            this.inputs = inputs;
        }
    }

    public class ProcessExecutor
    {
        private static readonly DBOperations DatabaseComponent = new DBOperations();

        private static readonly List<Process> PROCESSES = new List<Process>();

        private static readonly Dictionary<string, object> INITIALIZED_CLASSES = new Dictionary<string, object>();

        private static long cycle = 0;

        public static void ExecuteProcess(string moduleName, string methodName, object[] inputs)
        {
            PROCESSES.Add(new Process(moduleName, methodName, inputs));

            // Process buffer setted 1 in this case but it may get on property
            if(PROCESSES.Count == 1)
            {
                DatabaseComponent.addProcessOnDB(new List<Process>(PROCESSES), GetCycle());
                PROCESSES.Clear();
            }
        }

        public static void ExecuteWaitingProcess()
        {
            long cycleProcess = DatabaseComponent.getFirstNotProcessedCycleFromDB();

            while(cycleProcess != -1 && true)
            {
                List<Process> processes = DatabaseComponent.getWaitingProcessFromDB(cycleProcess);
                if(processes.Count == 0)
                {
                    break;
                }

                foreach(Process process in processes)
                {
                    object classObejct;
                    if (INITIALIZED_CLASSES.ContainsKey(process.moduleName))
                    {
                        classObejct = INITIALIZED_CLASSES[process.moduleName];
                    } else
                    {
                        Type type = Type.GetType("Envanter3." + process.moduleName);
                        ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
                        classObejct = constructor.Invoke(new object[] { });
                        INITIALIZED_CLASSES.Add(process.moduleName, classObejct);
                    }

                    Type[] argumentTypes;
                    if (process.inputs.Length > 0)
                    {
                        argumentTypes = new Type[process.inputs.Length];

                        for (int i = 0; i < argumentTypes.Length; i++)
                        {
                            argumentTypes[i] = process.inputs[i].GetType();
                        }
                    } else
                    {
                        argumentTypes = new Type[0];
                    }
                    

                    MethodInfo method = classObejct.GetType().GetMethod(process.methodName, BindingFlags.Instance | BindingFlags.NonPublic, null, argumentTypes, null);
                    method.Invoke(classObejct, process.inputs);
                }
                DatabaseComponent.updateProcessCycleStatus(cycleProcess, "SUCCESS");
                cycleProcess++;
            }
        }

        private static long GetCycle()
        {
            if(cycle == 0)
            {
                cycle = DatabaseComponent.getLastNotProcessedCycleFromDB();
            } else
            {
                cycle += 1;
            }

            return cycle;
        }
    }

    public class AccountingModule
    {
        private void PaySalaries(int customerNo)
        {
            // gerekli işlemler gerçekleştirilir.
            Console.WriteLine(string.Format("{0} numaralı müşterinin maaşı yatırıldı.", customerNo));
        }

        private void ReceiptYearlyAmount(int customerNo)
        {
            // gerekli işlemler gerçekleştirilir.
            Console.WriteLine("{0} numaralı müşteriden yıllık kart ücreti tahsil edildi.", customerNo);
        }

        private void ProcessAutomaticPayments(int customerNo)
        {
            // gerekli işlemler gerçekleştirilir.
            Console.WriteLine("{0} numaralı müşterinin otomatik ödemeleri gerçekleştirildi.", customerNo);
        }
    }

    public class DBOperations
    {
        //for DB simulate
        private static readonly Dictionary<KeyValuePair<long, string>, List<Process>> PROCESSES_DB = new Dictionary<KeyValuePair<long, string>, List<Process>>();
        private static long lastCycle = 1;

        public void addProcessOnDB (List<Process> process, long cycle)
        {
            PROCESSES_DB.Add(new KeyValuePair<long, string>(cycle, "WAITING"), process);
            lastCycle = cycle;
        }

        public List<Process> getWaitingProcessFromDB(long cycle)
        {
            // Fetch Waiting Process from db with indexed cycle
            if(PROCESSES_DB.ContainsKey(new KeyValuePair<long, string>(cycle, "WAITING")))
            {
                return PROCESSES_DB[new KeyValuePair<long, string>(cycle, "WAITING")];
            } else
            {
                return new List<Process>(0); 
            }
            
        }

        public long getLastNotProcessedCycleFromDB()
        {
            // Fetch last cycle
            return lastCycle;
        }

        public long getFirstNotProcessedCycleFromDB()
        {
            // Fetch first cycle
            List<KeyValuePair<long, string>> keys = PROCESSES_DB.Keys.OrderBy(item => item.Key).ToList();
            KeyValuePair<long, string> ?founded = keys.Find(item => item.Value.Equals("WAITING"));
            if(!founded.HasValue)
            {
                return -1;
            }

            return founded.Value.Key;

            //if all executions finished returns -1
        }

        public void updateProcessCycleStatus(long cycle, string status)
        {
            //update process cycle status
            List<Process> process = PROCESSES_DB[new KeyValuePair<long, string>(cycle, "WAITING")];
            PROCESSES_DB.Add(new KeyValuePair<long, string>(cycle, "SUCCESS"), process);
            PROCESSES_DB.Remove(new KeyValuePair<long, string>(cycle, "WAITING"));
        }
    }
}
