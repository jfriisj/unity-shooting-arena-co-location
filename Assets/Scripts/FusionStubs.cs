// Comprehensive Fusion stubs to prevent Meta Building Blocks compilation errors
// This file provides empty Fusion types when Fusion 2 is not installed

#if !FUSION2

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion
{
    // Core interfaces that Fusion Building Blocks expect
    public interface INetworkRunnerCallbacks 
    {
        void OnPlayerJoined(NetworkRunner runner, PlayerRef player);
        void OnPlayerLeft(NetworkRunner runner, PlayerRef player);
        void OnInput(NetworkRunner runner, NetworkInput input);
        void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input);
        void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason);
        void OnConnectedToServer(NetworkRunner runner);
        void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason);
        void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs request);
        void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason);
        void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message);
        void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList);
        void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data);
        void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken);
        void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, ArraySegment<byte> data);
        void OnSceneLoadDone(NetworkRunner runner);
        void OnSceneLoadStart(NetworkRunner runner);
    }
    
    // Core types
    public class NetworkRunner : MonoBehaviour { }
    public struct NetworkRunnerCallbackArgs { }
    public struct PlayerRef { }
    public struct NetworkInput { }
    public struct HostMigrationToken { }
    public struct SessionInfo { }
    public enum ShutdownReason { }
    public struct SimulationMessagePtr { }
    public class NetworkObject : MonoBehaviour { }
    public enum NetDisconnectReason { }
    public struct ReliableKey { }
    public struct NetAddress { }
    public enum NetConnectFailedReason { }
    
    // Additional types Meta Building Blocks might expect
    public class NetworkBehaviour : MonoBehaviour { }
    public interface INetworkTickReceiver { }
    public interface INetworkUpdateLoopReceiver { }
    public class SimulationBehaviour : NetworkBehaviour { }
}

// Photon Voice namespace stubs
namespace Photon.Voice
{
    public class VoiceConnection { }
    public class AudioSource { }
}

namespace Photon.Voice.Fusion
{
    public class FusionVoiceClient { }
}

namespace Photon.Realtime
{
    public class AppSettings { }
    public class Room { }
    public class Player { }
}

namespace Fusion.Addons.Physics
{
    public class NetworkPhysicsSimulation { }
}

#endif