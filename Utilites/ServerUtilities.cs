using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Entities;

namespace SleepyDomains.Utilites;

internal class ServerUtilities{
    private static World? _serverWorld;
    public static EntityManager EntityManager => Server.EntityManager;
    public static ServerScriptMapper ServerScriptMapper => Server.GetExistingSystemManaged<ServerScriptMapper>();
    public static World Server {
        get {
            if (_serverWorld != null) return _serverWorld;
            _serverWorld = GetWorld("Server") ?? throw new System.Exception("There is no server.");
            return _serverWorld;
        }
    }
    private static World GetWorld(string name) {
        foreach (var world in World.s_AllWorlds) {
            if (world.Name == name) {
                return world;
            }
        }
        return null;
    }
    
    
}