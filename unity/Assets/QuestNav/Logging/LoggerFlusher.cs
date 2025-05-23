using QuestNav.Utils;
using UnityEngine;

namespace QuestNav.Logging
{
    public class LoggerFlusher : MonoBehaviour
    {
        void Update()
        {
            QueuedLogger.Flush();
        }
    }
}