using System;
using System.Collections.Generic;
using System.Text;

namespace ClearPulse.Core.Entities
{
    public class LogEvent : BaseLogEntity
    {
        // No extra fields needed — MetadataJson handles dynamic attributes
        // Inherits: Id, SourceSystem, EventType, CircleCode, Status, EventTime, TenantId, MetadataJson
    }

}
