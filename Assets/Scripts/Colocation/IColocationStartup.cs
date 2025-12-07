// Copyright (c) Meta Platforms, Inc. and affiliates.

#if FUSION2
using System;

namespace MRMotifs.ColocatedExperiences.Colocation
{
    /// <summary>
    /// Interface for colocation startup managers.
    /// Implemented by ColocationStartup to provide a clean contract for 
    /// other scripts to depend on without tight coupling.
    /// </summary>
    public interface IColocationStartup
    {
        /// <summary>
        /// Returns true if this device is the host/master.
        /// </summary>
        bool IsHost { get; }
        
        /// <summary>
        /// Returns true when colocation is fully ready (aligned, room loaded, networked).
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// The group UUID used for colocation (from Bluetooth discovery).
        /// </summary>
        Guid GroupUuid { get; }
        
        /// <summary>
        /// Fired when colocation is ready and gameplay can begin.
        /// </summary>
        event Action OnColocationReady;
        
        /// <summary>
        /// Fired when colocation fails with an error message.
        /// </summary>
        event Action<string> OnColocationFailed;
    }
}
#endif
