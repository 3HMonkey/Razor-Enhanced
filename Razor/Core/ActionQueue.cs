using Priority_Queue;
using RazorEnhanced;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Assistant
{
    internal class DragDropManager
    {
        public static void Initialize()
        {

        }


        internal static Task DragDrop(Item i, Serial to)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Serial to)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, i.Amount, to, Point3D.MinusOne);
        }

        internal static Task DragDrop(Item i, int amount, Serial to)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, int amount, Serial to)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, amount, to, Point3D.MinusOne);
        }

        internal static Task DragDrop(Item i, Item to)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Item to)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, i.Amount, to.Serial, Point3D.MinusOne);
        }

        internal static Task DragDrop(Item i, Point3D dest)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Point3D dest)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, i.Amount, Serial.MinusOne, dest);
        }

        internal static Task DragDrop(Item i, Point3D dest, int amount)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Point3D dest, int amount)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, amount, Serial.MinusOne, dest);
        }

        internal static Task DragDrop(Item i, int amount, Item to)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, int amount, Item to)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, amount, to.Serial, Point3D.MinusOne);
        }

        internal static Task DragDrop(Item i, int amount, Item to, Point3D dest)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, int amount, Item to, Point3D dest)");
            //======================================================
            return ActionQueue.EnqueueDragDrop(i.Serial, amount, to.Serial, dest);
        }

        internal static Task DragDrop(Item i, Mobile to, Layer layer, int amount)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Mobile to, Layer layer, int amount)");
            //======================================================
            return ActionQueue.EnqueueEquip(i.Serial, amount, to, layer);
        }

        internal static Task DragDrop(Item i, Mobile to, Layer layer, bool doLast)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Mobile to, Layer layer, bool doLast)");
            //======================================================
            return ActionQueue.EnqueueEquip(i.Serial, i.Amount, to, layer);
        }

        internal static Task DragDrop(Item i, Mobile to, Layer layer)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DragDrop(Item i, Mobile to, Layer layer)");
            //======================================================
            return ActionQueue.EnqueueEquip(i.Serial, i.Amount, to, layer);
        }

        internal static Task Drag(Item i, int amount, bool fromClient)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing Drag(Item i, int amount, bool fromClient)");
            //======================================================
            return Drag(i, amount, fromClient, false);
        }

        internal static Task Drag(Item i, int amount)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing Drag(Item i, int amount)");
            //======================================================
            return ActionQueue.EnqueueDrag(i.Serial, amount);
        }

        internal static Task Drag(Item i, int amount, bool fromClient, bool doLast)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing Drag(Item i, int amount, bool fromClient, bool doLast)");
            //======================================================
            return ActionQueue.EnqueueDrag(i.Serial, amount);

        }

        internal static Task Drop(Item i, Serial dest, Point3D pt)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing Drop(Item i, Serial dest, Point3D pt)");
            //======================================================
            return ActionQueue.EnqueueDropRelative(i.Serial, dest, pt);
        }

        internal static Task Drop(Item i, Item to)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing Drop(Item i, Item to)");
            //======================================================
            return ActionQueue.EnqueueDropContainer(i.Serial, to.Serial);
        }

        internal static Task DoubleClick(Serial s)
        {
            //======================================================
            // DEBUG
            Misc.SendMessage($"Executing DoubleClick(Serial s)");
            //======================================================
            return ActionQueue.EnqueueDoubleClick(s);
        }

    }

    public enum QueuePriority
    {
        Immediate,
        High,
        Medium,
        Low
    }

    public class ActionPacketQueueItem
    {
        public ActionPacketQueueItem(Packet packet, bool delaySend)
        {
            Packet = packet;
            DelaySend = delaySend;
            WaitHandle = new AutoResetEvent(false);
        }

        public bool DelaySend { get; set; }
        public Packet Packet { get; set; }
        public EventWaitHandle WaitHandle { get; set; }
    }

    public class ThreadPriorityQueue<T> : IDisposable
    {
        private readonly Action<T> _onAction;
        private readonly SimplePriorityQueue<T> _queue = new SimplePriorityQueue<T>();
        private readonly EventWaitHandle _wh = new AutoResetEvent(false);
        private readonly Thread _workerThread;

        public ThreadPriorityQueue(Action<T> onAction)
        {
            _onAction = onAction;
            _workerThread = new Thread(ProcessQueue) { IsBackground = true };
            _workerThread.Start();
        }

        public void Dispose()
        {
            StopThread();
        }

        public int Count()
        {
            return _queue.Count;
        }

        public int Count(Predicate<T> predicate)
        {
            return _queue.Count(predicate.Invoke);
        }

        public void Clear()
        {
            _queue.Clear();
        }

        private void ProcessQueue()
        {
            while (_workerThread.IsAlive)
            {
                if (_queue.TryDequeue(out T queueItem))
                {
                    if (queueItem == null)
                    {
                        return;
                    }
                    _onAction(queueItem);
                }
                else
                {
                    _wh.WaitOne();
                }
            }
        }

        public void Enqueue(T queueItem, QueuePriority priority)
        {
            _queue.Enqueue(queueItem, (float)priority);

            try
            {
                _wh.Set();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void StopThread()
        {
            _queue.Enqueue(default, (float)QueuePriority.Immediate);

            try
            {
                _wh.Set();
            }
            catch (ObjectDisposedException)
            {
            }

            _workerThread.Join();
            _wh.Close();
        }
    }

    public static class TaskExtension
    {
        public static Task ToTask(this EventWaitHandle waitHandle)
        {
            if (waitHandle == null)
            {
                throw new ArgumentNullException(nameof(waitHandle));
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            RegisteredWaitHandle rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
                delegate { tcs.TrySetResult(true); }, null, -1, true);

            Task<bool> t = tcs.Task;

            t.ContinueWith(antecedent => rwh.Unregister(null));

            return t;
        }

        public static Task ToTask(this IEnumerable<EventWaitHandle> waitHandles)
        {
            List<Task> tasks = waitHandles.Select(waitHandle => waitHandle.ToTask()).ToList();

            return Task.WhenAll(tasks);
        }
    }

    internal class ActionQueue
    {

        private static readonly ThreadPriorityQueue<ActionPacketQueueItem> _actionPacketQueue = new ThreadPriorityQueue<ActionPacketQueueItem>(ProcessActionPacketQueue);
        private static readonly object _actionQueueLock = new object();

        private static void ProcessActionPacketQueue(ActionPacketQueueItem queueItem)
        {
            lock (_actionQueueLock)
            {
                if (queueItem.DelaySend)
                {
                    while (Engine.LastActionPacket +
                            TimeSpan.FromMilliseconds(Settings.General.ReadInt("ObjectDelay")) >
                            DateTime.Now)
                    {
                        Thread.Sleep(1);
                    }
                }


                Engine.LastActionPacket = DateTime.Now;
                Client.Instance.SendToServerWait(queueItem.Packet);

                queueItem.WaitHandle.Set();
            }
        }

        public static Task EnqueueActionPackets(IEnumerable<Packet> packets, QueuePriority priority = QueuePriority.Low, bool delaySend = true)
        {
            lock (_actionQueueLock)
            {
                List<EventWaitHandle> handles = new List<EventWaitHandle>();

                foreach (Packet packet in packets)
                {
                    ActionPacketQueueItem queueItem = new ActionPacketQueueItem(packet, delaySend);
                    handles.Add(queueItem.WaitHandle);
                    _actionPacketQueue.Enqueue(queueItem, priority);
                }

                return handles.ToTask();
            }
        }

        public static Task EnqueueDrag(int serial, int amount, QueuePriority priority = QueuePriority.Low,
            bool delaySend = true)
        {
            return EnqueueActionPackets(new Packet[] { new LiftRequest(serial, amount) }, priority, delaySend);

        }

        public static Task EnqueueDropContainer(int serial, int containerSerial, QueuePriority priority = QueuePriority.Low,
            bool delaySend = true)
        {
            return EnqueueActionPackets(new Packet[] { new DropRequest(World.FindItem(serial), World.FindItem(containerSerial)) }, priority, delaySend);

        }

        public static Task EnqueueDropRelative(int serial, int containerSerial, Point3D point3d, QueuePriority priority = QueuePriority.Low,
            bool delaySend = true)
        {
            return EnqueueActionPackets(new Packet[] { new DropRequest(serial, point3d, containerSerial) }, priority, delaySend);

        }

        public static Task EnqueueDragDrop(int serial, int amount, int containerSerial, Point3D point3d, QueuePriority priority = QueuePriority.Low, bool delaySend = true)
        {
            return EnqueueActionPackets(
                new Packet[] { new LiftRequest(serial, amount), new DropRequest(serial, point3d, containerSerial) },
                priority, delaySend);

        }

        public static Task EnqueueEquip(int serial, int amount, Mobile mobile, Layer layer, QueuePriority priority = QueuePriority.Low, bool delaySend = true)
        {
            return EnqueueActionPackets(
                new Packet[] { new LiftRequest(serial, amount), new EquipRequest(serial, mobile, layer) },
                priority, delaySend);
            
        }

        public static Task EnqueueDoubleClick(Serial s, QueuePriority priority = QueuePriority.Low, bool delaySend = true)
        {
            return EnqueueActionPackets(new Packet[] { new DoubleClick(s) }, priority, delaySend);
        }

        public static void Stop()
        {
            _actionPacketQueue.Clear();
        }

    }
}
