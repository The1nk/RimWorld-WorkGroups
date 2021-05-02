using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace The1nk.WorkGroups.Models
{
    internal static class TickThreadQueue {
        private static readonly Queue<Action> _queue;

        static TickThreadQueue() {
            _queue = new Queue<Action>();
        }

        public static void EnqueueItem(Action item) {
            _queue.Enqueue(item);
        }

        public static void DoOne() {
            if (_queue.Any())
                _queue.Dequeue().Invoke();
        }
    }
}
