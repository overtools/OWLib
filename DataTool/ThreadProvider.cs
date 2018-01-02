using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static DataTool.Helper.IO;

namespace DataTool {
    // My current threading implmentation is bad and doesn't help as much as it can.
        
    // right now this is a proof of concept but it will eventually do something.
        
    // each 'task' is collected, work out everything we need to do.
    // the 'task's are spread equally across x threads and run.
        
    // have a CASCProvider(name is bad?). Each 'task' has to wait for this to get CASC files.
    // CASC queue: threads will do non-casc stuff while waiting for CASC
        
    // CASCProvisioner goes through each thread and works out which Streams it needs, then sends them over.
    // Thread checks it has all of the required streams before starting a task.
    
    // todo: for some reason every convert texture operation takes longer when using dynamic tasks.
    // (should be faster otherwise)
    // todo: maybe because of thread lock used on Process.Start
    
    public class ProviderRuntimeData {
        internal ConcurrentQueue<WorkTask> LooseTasks;
        
        public ProviderRuntimeData(HashSet<WorkTask> looseTasks) {
            LooseTasks = new ConcurrentQueue<WorkTask>();
            foreach (WorkTask looseTask in looseTasks) {
                looseTask.DynamicallyAssigned = true;
                looseTask.CASCStreams = new ConcurrentDictionary<ulong, Stream>();
                LooseTasks.Enqueue(looseTask);
            }
        }
    }
    
    public class ThreadProvider {
        private readonly HashSet<WorkTask> _tasks;

        private HashSet<WorkerThread> _threads;
        
        public ThreadProvider() {
            _tasks = new HashSet<WorkTask>();
        }

        public int AutoThreadCount() {
            return 4;
        }

        public void Run() {
            Run(AutoThreadCount());
        }

        internal Stream GetStream(ulong guid) {  // todo: private
            Stream fileStream = OpenFile(guid);
                            
            // MemoryStream memoryStream = new MemoryStream();
            // fileStream.CopyTo(memoryStream);
            // memoryStream.Position = 0;
            return fileStream;
            // fileStream.Dispose();
            
            // return memoryStream;
        }

        public void Run(int threadCount) {
            _threads = CreateThreads(threadCount);

            Dictionary<Type, List<WorkTask>> groups = GenerateTaskGroups();

            HashSet<WorkTask> looseTasks;
            DistributeWork(_threads, groups, out looseTasks);
            
            ProviderRuntimeData runtimeData = new ProviderRuntimeData(looseTasks);

            foreach (WorkerThread workerThread in _threads) {
                if (workerThread.Tasks.Count == 0) continue;
                workerThread.Start(runtimeData);
            }

            Dictionary<WorkerThread, HashSet<ulong>> threadFiles = GetThreadFiles();
            Dictionary<WorkerThread, List<HashSet<ulong>>> taskFiles = new Dictionary<WorkerThread, List<HashSet<ulong>>>();
            
            foreach (WorkerThread workerThread in _threads) {
                taskFiles[workerThread] = new List<HashSet<ulong>>();
                foreach (WorkTask workerThreadTask in workerThread.Tasks) {
                    if (workerThreadTask.RequiredCASCFiles.Count == 0) continue;
                    HashSet<ulong> workerTaskFiles = new HashSet<ulong>();
                    foreach (ulong requiredCASCFile in workerThreadTask.RequiredCASCFiles) {
                        workerTaskFiles.Add(requiredCASCFile);
                    }
                    taskFiles[workerThread].Add(workerTaskFiles);
                }
            }

            int taskIndex = 0;
            int maxTaskIndex = _threads.Select(x => x.Tasks.Count).Max();

            // distribute casc files
            while (true) {
                foreach (WorkerThread workerThread in _threads) {
                    if (taskFiles[workerThread].Count > taskIndex) {
                        HashSet<ulong> workerTaskFiles = taskFiles[workerThread][taskIndex];
                        foreach (ulong file in workerTaskFiles) {
                            Stream stream = GetStream(file);
                            workerThread.CASCStreams[file] = stream;
                        }
                    }
                }

                taskIndex++;

                if (taskIndex == maxTaskIndex || maxTaskIndex == 0) break;
            }

            foreach (WorkTask looseTask in runtimeData.LooseTasks) {
                foreach (ulong requiredCASCFile in looseTask.RequiredCASCFiles) {
                    Stream stream = GetStream(requiredCASCFile);
                    looseTask.CASCStreams[requiredCASCFile] = stream;
                }
            }
            
        }

        public void Wait() {
            _threads.WaitAll();
        }

        private Dictionary<WorkerThread, HashSet<ulong>> GetThreadFiles() {
            Dictionary<WorkerThread, HashSet<ulong>> threadFiles = new Dictionary<WorkerThread, HashSet<ulong>>();
            foreach (WorkerThread workerThread in _threads) {
                threadFiles[workerThread] = new HashSet<ulong>();
                foreach (WorkTask workerThreadTask in workerThread.Tasks) {
                    foreach (ulong requiredCASCFile in workerThreadTask.RequiredCASCFiles) {
                        threadFiles[workerThread].Add(requiredCASCFile);
                    }
                }
            }

            return threadFiles;
        }

        private void DistributeWork(IReadOnlyCollection<WorkerThread> threads, Dictionary<Type, List<WorkTask>> groups,
            out HashSet<WorkTask> looseTasks) {
            
            looseTasks = new HashSet<WorkTask>();
            foreach (KeyValuePair<Type,List<WorkTask>> group in groups) {
                int tasksPerThread = group.Value.Count / threads.Count;

                tasksPerThread = (int)(tasksPerThread/1.1);

                int taskIndex = 0;
                foreach (WorkerThread thread in threads.OrderBy(x => x.Tasks.Count)) {
                    for (int i = 0; i < tasksPerThread; i++) {
                        thread.Tasks.Add(group.Value[taskIndex]);
                        taskIndex++;
                    }
                }

                while (taskIndex != group.Value.Count) {
                    // all predetermined: 
                    // looseTasks.Add(group.Value[taskIndex]);
                    // taskIndex++;
                    
                    // assign last tasks dynamically:
                    WorkerThread lowestTaskThread = threads.OrderBy(x => x.Tasks.Count).First();
                    lowestTaskThread.Tasks.Add(group.Value[taskIndex]);
                    taskIndex++;
                }
            }
        }

        private Dictionary<Type, List<WorkTask>> GenerateTaskGroups() {
            Dictionary<Type, List<WorkTask>> groups = new Dictionary<Type, List<WorkTask>>();
            foreach (WorkTask task in _tasks) {
                Type taskType = task.GetType();
                if (!groups.ContainsKey(taskType)) {
                    groups[taskType] = new List<WorkTask>();
                }

                groups[taskType].Add(task);
            }

            return groups;
        }

        private HashSet<WorkerThread> CreateThreads(int count) {
            HashSet<WorkerThread> threads = new HashSet<WorkerThread>();
            for (int i = 0; i < count; i++) {
                WorkerThread workerThread = new WorkerThread();
                threads.Add(workerThread);
            }

            return threads;
        }

        public void AddTask(WorkTask task) {
            _tasks.Add(task);
        }
        
    }
    
    public class WorkTask {
        public HashSet<ulong> RequiredCASCFiles;
        private WorkerThread _thread;

        internal bool DynamicallyAssigned;
        internal ConcurrentDictionary<ulong, Stream> CASCStreams;

        protected WorkTask() {
            RequiredCASCFiles = new HashSet<ulong>();
        }
        
        public virtual void Run(WorkerThread thread) {
            throw new NotImplementedException();
        }

        public virtual bool IsReady(WorkerThread thread) {
            _thread = thread;
            foreach (ulong requiredCASCFile in RequiredCASCFiles) {
                if (!thread.CASCStreams.ContainsKey(requiredCASCFile)) return false;
            }

            return true;
        }

        protected Stream OpenFile(ulong file) {
            if (DynamicallyAssigned) {
                return CASCStreams[file];
            }
            return _thread.CASCStreams[file];
        }
    }

    public class WorkerThread {
        internal List<WorkTask> Tasks;
        private readonly Thread _thread;
        internal ConcurrentDictionary<ulong, Stream> CASCStreams;
        private ProviderRuntimeData _providerRuntimeData;
        
        public WorkerThread() {
            Tasks = new List<WorkTask>();
            CASCStreams = new ConcurrentDictionary<ulong, Stream>();
            _thread = new Thread(Run);
        }

        internal void Start(ProviderRuntimeData providerRuntimeData) {
            _providerRuntimeData = providerRuntimeData;
            _thread.Start();
        }

        private void Run() {
            // main run routine
            // todo

            List<WorkTask> notDone = new List<WorkTask>(Tasks);
            while (true) {
                foreach (WorkTask workTask in new List<WorkTask>(notDone)) {
                    if (!workTask.IsReady(this)) {
                        continue;
                    }
                    workTask.Run(this);
                    notDone.Remove(workTask);
                }

                if (notDone.Count == 0) {
                    break;
                }

                Thread.Sleep(200);
            }

            while (true) {
                if (_providerRuntimeData.LooseTasks.TryDequeue(out WorkTask looseTask)) {
                    looseTask.Run(this);
                } else {
                    break;
                }
            }
            
            Dispose();
        }

        internal void Join() {
            _thread.Join();
        }

        internal void Dispose() {
            foreach (Stream stream in CASCStreams.Values) {
                stream.Dispose();
            }
        }
    }

    public static class ThreadExtension {
        public static void WaitAll(this HashSet<WorkerThread> threads) {
            if (threads == null) return;
            foreach (WorkerThread workerThread in threads) {
                workerThread.Join();
            }
        }
    }
}