﻿using OneOf;
using OneOf.Types;
using System.Runtime.InteropServices;

namespace EventSourcing;

public interface IEventStore
{
    Task<OneOf<Success, VersionMismatch, Unknown>> AppendToStreamAsync(string type, Guid streamId, IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
    Task<IEnumerable<IEvent>> ReadStreamAsync(string type, Guid streamId, CancellationToken cancellationToken = default);
    Task<IEnumerable<IEvent>> ReadStreamAsync(string type, Guid streamId, int fromVersion, CancellationToken cancellationToken = default);
    Task RemoveStreamAsync(string type, Guid streamId, CancellationToken cancellationToken = default);
}
