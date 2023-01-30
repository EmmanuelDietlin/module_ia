using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIModule.Events
{
    internal class QuestEvent : Event
    {
        public enum QuestStatus
        {
            finished,
            refused,
            abandoned
        }
        public string QuestId { get; set; }
        public QuestStatus Status { get; set; }
        public string PlayerId { get; set; }
        public string AIId { get; set; }

        public QuestEvent(string questId, QuestStatus status, string playerId, string aiId)
        {
            QuestId = questId;
            Status = status;
            PlayerId = playerId;
            AIId = aiId;
        }
    }
}
