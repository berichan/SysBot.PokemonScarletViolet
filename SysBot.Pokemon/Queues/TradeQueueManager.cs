using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon
{
    public class TradeQueueManager<T> where T : PKM, new()
    {
        private readonly PokeTradeHub<T> Hub;

        private readonly PokeTradeQueue<T> Trade = new(PokeTradeType.Specific);
        private readonly PokeTradeQueue<T> Seed = new(PokeTradeType.Seed);
        private readonly PokeTradeQueue<T> Clone = new(PokeTradeType.Clone);
        private readonly PokeTradeQueue<T> Dump = new(PokeTradeType.Dump);
        public readonly TradeQueueInfo<T> Info;
        public readonly PokeTradeQueue<T>[] AllQueues;

        public TradeQueueManager(PokeTradeHub<T> hub)
        {
            Hub = hub;
            Info = new TradeQueueInfo<T>(hub);
            AllQueues = new[] { Seed, Dump, Clone, Trade };

            foreach (var q in AllQueues)
                q.Queue.Settings = hub.Config.Favoritism;
        }

        public PokeTradeQueue<T> GetQueue(PokeRoutineType type) => type switch
        {
            PokeRoutineType.SeedCheck => Seed,
            PokeRoutineType.Clone => Clone,
            PokeRoutineType.Dump => Dump,
            _ => Trade,
        };

        public void ClearAll()
        {
            foreach (var q in AllQueues)
                q.Clear();
        }

        public bool TryDequeueLedy(out PokeTradeDetail<T> detail, bool force = false)
        {
            detail = default!;
            var cfg = Hub.Config.Distribution;
            if (!cfg.DistributeWhileIdle && !force)
                return false;

            if (Hub.Ledy.Pool.Count == 0)
                return false;

            var random = Hub.Ledy.Pool.GetRandomPoke();
            var code = cfg.RandomCode ? Hub.Config.Trade.GetRandomTradeCode() : cfg.TradeCode;
            var trainer = new PokeTradeTrainerInfo("Random Distribution");
            detail = new PokeTradeDetail<T>(random, trainer, PokeTradeHub<T>.LogNotifier, PokeTradeType.Random, code, false);
            return true;
        }

        public bool TryDequeue(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
        {
            if (type == PokeRoutineType.FlexTrade)
                return GetFlexDequeue(out detail, out priority);

            return TryDequeueInternal(type, out detail, out priority);
        }

        private bool TryDequeueInternal(PokeRoutineType type, out PokeTradeDetail<T> detail, out uint priority)
        {
            var queue = GetQueue(type);
            return queue.TryDequeue(out detail, out priority);
        }

        private bool GetFlexDequeue(out PokeTradeDetail<T> detail, out uint priority)
        {
            var cfg = Hub.Config.Queues;
            if (cfg.FlexMode == FlexYieldMode.LessCheatyFirst)
                return GetFlexDequeueOld(out detail, out priority);
            else if (cfg.FlexMode == FlexYieldMode.Weighted)
                return GetFlexDequeueWeighted(cfg, out detail, out priority);
            return GetFlexDequeuePrioritiseWeb(cfg, out detail, out priority);
        }

        private bool GetFlexDequeueWeighted(QueueSettings cfg, out PokeTradeDetail<T> detail, out uint priority)
        {
            PokeTradeQueue<T>? preferredQueue = null;
            long bestWeight = 0; // prefer higher weights
            uint bestPriority = uint.MaxValue; // prefer smaller
            foreach (var q in AllQueues)
            {
                var peek = q.TryPeek(out detail, out priority);
                if (!peek)
                    continue;

                // priority queue is a min-queue, so prefer smaller priorities
                if (priority > bestPriority)
                    continue;

                var count = q.Count;
                var time = detail.Time;
                var weight = cfg.GetWeight(count, time, q.Type);

                if (priority >= bestPriority && weight <= bestWeight)
                    continue; // not good enough to be preferred over the other.

                // this queue has the most preferable priority/weight so far!
                bestWeight = weight;
                bestPriority = priority;
                preferredQueue = q;
            }

            if (preferredQueue == null)
            {
                detail = default!;
                priority = default;
                return false;
            }

            SendReminders(preferredQueue);
            return preferredQueue.TryDequeue(out detail, out priority);
        }

        private bool GetDequeueLinear(QueueSettings cfg, out PokeTradeDetail<T> detail, out uint priority)
        {
            double longestWait = 0;
            DateTime now = DateTime.Now;
            PokeTradeQueue<T>? preferredQueue = null;
            foreach (var q in AllQueues)
            {
                if (q.Count < 1)
                    continue;

                var peek = q.TryPeek(out detail, out priority);
                if (!peek)
                    continue;

                var time = detail.Time;
                var thisTime = (now - time).TotalMinutes;
                if (thisTime > longestWait)
                {
                    longestWait = thisTime;
                    preferredQueue = q;
                }
            }

            if (preferredQueue == null)
            {
                detail = default!;
                priority = default;
                return false;
            }

            SendReminders(preferredQueue);
            return preferredQueue.TryDequeue(out detail, out priority);
        }

        private bool GetFlexDequeueOld(out PokeTradeDetail<T> detail, out uint priority)
        {
            if (TryDequeueInternal(PokeRoutineType.SeedCheck, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.Clone, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.Dump, out detail, out priority))
                return true;
            if (TryDequeueInternal(PokeRoutineType.LinkTrade, out detail, out priority))
                return true;
            return false;
        }

        private DateTime LastPrioTime = DateTime.Now;
        private Random rand = new Random();
        private object _syncPrio = new object();
        private bool GetFlexDequeuePrioritiseWeb(QueueSettings cfg, out PokeTradeDetail<T> detail, out uint priority)
        {
            lock (_syncPrio)
            {
                if ((DateTime.Now - LastPrioTime).TotalMinutes > 1.3 || rand.Next(0,4) == 1)
                {
                    if (GetFlexDequeueOld(out detail, out priority) != false)
                    {
                        LastPrioTime = DateTime.Now;
                        return true;
                    }
                }
            }

            return GetDequeueLinear(cfg, out detail, out priority);
        }

        public void Enqueue(PokeRoutineType type, PokeTradeDetail<T> detail, uint priority)
        {
            var queue = GetQueue(type);
            queue.Enqueue(detail, priority);
        }

        // hook in here if you want to forward the message elsewhere???
        public readonly List<Action<PokeRoutineExecutorBase, PokeTradeDetail<T>>> Forwarders = new();

        public void StartTrade(PokeRoutineExecutorBase b, PokeTradeDetail<T> detail)
        {
            foreach (var f in Forwarders)
                f.Invoke(b, detail);
        }

        public void SendReminders(PokeTradeQueue<T> queue)
        {
            foreach (var v in queue.Queue)
            {
                var posInfo = Info.CheckPosition(v.Value.Trainer.ID);
                if (v.Value.Notifier.QueueSizeEntry >= Hub.Config.Queues.ReminderQueueCountStart && posInfo.Position <= Hub.Config.Queues.ReminderAtPosition)
                    v.Value.Notifier.SendReminder(posInfo.Position, string.Empty);
            }
        }
    }
}
