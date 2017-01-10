using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BotFactory.Interface;
using BotFactory.Common.Tools;
using BotFactory.Models;

namespace BotFactory.Factories
{
    public class UnitFactory : ReportingUnit, IUnitFactory
    {
        public event FactoryProgressDelegate FactoryProgress;
        public void OnProgresse(FactoryProgressEventArgs origin)
        {
            FactoryProgress?.Invoke(this, origin);
        }
        private int _queueCapacity;
        private int _storageCapacity;

        public TimeSpan QueueTime{ get; set; }
        public int QueueCapacity
        {
            get
            {
                return _queueCapacity;
            }
        }
        public int StorageCapacity
        {
            get
            {
                return _storageCapacity;
            }
        }
        public int QueueFreeSlots
        {
            get
            {
                return QueueCapacity - Queue.Count;
            }
        }
        public int StorageFreeSlots
        {
            get
            {
                return StorageCapacity - Storage.Count;
            }
        }
        public UnitFactory(int queueCapacity, int storageCapacity)
        {
            _queueCapacity = queueCapacity;
            _storageCapacity = storageCapacity;
            Queue = new Queue<IFactoryQueueElement>();
            Storage = new List<ITestingUnit>();
        }
        public List<ITestingUnit> Storage { get; set; }
        public Queue<IFactoryQueueElement> Queue { get; set; }
        public bool CanBuild()
        {
            if ((Queue.Count < QueueCapacity) && (Storage.Count + Queue.Count +1 < StorageCapacity))
            {
                return true;
            }
            return false;
        }
        public bool AddWorkableUnitToQueue(Type model, string name, Coordinates parkingPos, Coordinates workingPos)
        {
            if(CanBuild())
            {
                IFactoryQueueElement constructing = new FactoryQueueElement(model, name, parkingPos, workingPos);
                WorkingUnit ConstructingUnit = Activator.CreateInstance((constructing.Model), new object[] { constructing.Name }) as WorkingUnit;
                TimeSpan TimeToCreate = TimeSpan.FromSeconds(ConstructingUnit.BuildTime);
                Queue.Enqueue(constructing);
                QueueTime += TimeToCreate;
                FactoryProgressEventArgs unity_in = new FactoryProgressEventArgs();
                unity_in.returnUnit = unity_in;
                FactoryProgress(this, unity_in);
                StartBuilding();
                return true;
            }
            
            return false;
        }
        public void AddStorage(ITestingUnit BuiltUnity)
        {
            //ITestingUnit Unity = Activator.CreateInstance((model), new object[] { name }) as ITestingUnit;
            Storage.Add(BuiltUnity);
        }
        public async Task<bool> BuildWorkableUnit()
        {
            IFactoryQueueElement unity_out = Queue.Peek();
            WorkingUnit BuildingUnit = Activator.CreateInstance((unity_out.Model), new object[] { unity_out.Name }) as WorkingUnit;
            TimeSpan TimeToCreate = TimeSpan.FromSeconds(BuildingUnit.BuildTime);
            await Task.Delay(TimeToCreate);
            Queue.Dequeue();
            QueueTime -= TimeToCreate;
            AddStorage(BuildingUnit);
            FactoryProgressEventArgs args_unity_out = new FactoryProgressEventArgs();
            args_unity_out.returnUnit = unity_out;
            FactoryProgress(this, args_unity_out);
            return true;
        }

        private bool IsFactoryBuildingFlag = false;
        public void StartBuilding()
        {
            if (IsFactoryBuildingFlag)
            {
                return;
            }
            else
            {
                Task.Run(async () =>
                {
                    IsFactoryBuildingFlag = true;
                    while (Queue.Count != 0 && StorageFreeSlots>0)
                    {
                        bool builded = await BuildWorkableUnit();
                    }
                    OnStatusChanged(new StatusChangedEventArgs { NewStatus = "Moving" });
                    IsFactoryBuildingFlag = false;
                });
            }

        }
        public bool RemoveUnit()
        {
            return true;
        }
    }
}
