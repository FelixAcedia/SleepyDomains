using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;

namespace SleepyDomains.Utilites;

// Credits for this class goes to Mitch (zfolmt), I merely rewrote many parts to only get the essentials for me and not be dependent on classes they made
// GitHub: https://github.com/mfoltz/Bloodcraft/blob/main/Utilities/Buffs.cs
internal static class Buffs{
    static DebugEventsSystem debugEventsSystem => ServerUtilities.Server.GetExistingSystemManaged<DebugEventsSystem>();
    static ServerGameManager ServerGameManager => ServerUtilities.ServerScriptMapper.GetServerGameManager();

    public static bool TryApplyBuff(this Entity entity, PrefabGUID buffPrefab) {
        bool isbuffed = entity.HasBuff(buffPrefab);

        var buff = ServerUtilities.EntityManager.GetComponentData<Buff>(entity);
        if (isbuffed && entity.CanApplyStacks(buffPrefab, out Entity buffEntity, out byte stack)) {
            ServerGameManager.InstantiateBuffEntityImmediate(entity, entity, buffPrefab, null, stack);
        }
        else if (!isbuffed) {
            ApplyBuffDebugEvent applyBuffDebug = new() {
                BuffPrefabGUID = buffPrefab,
                Who = ServerUtilities.EntityManager.GetComponentData<NetworkId>(entity)
            };

            FromCharacter character = new() {
                Character = entity,
                User = ServerUtilities.EntityManager.TryGetComponentData<PlayerCharacter>(entity,
                    out PlayerCharacter playerCharacter)
                    ? playerCharacter.UserEntity
                    : entity
            };
            ServerGameManager.InstantiateBuffEntityImmediate(entity, entity, buffPrefab, null, 0);
            return true;
        }
        return false;
    }

    public static bool TryApplyAndGetBuff(this Entity entity, PrefabGUID buffPrefab, out Entity buffEntity) {
        buffEntity = Entity.Null;
        if (entity.TryApplyBuff(buffPrefab) && entity.TryGetBuff(buffPrefab, out buffEntity)) {
            return true;
        }
        return false;
    }

    public static bool TryApplyImmortalBuff(this Entity entity, PrefabGUID buffPrefab) {
        if (entity.TryApplyAndGetBuff(buffPrefab, out Entity buffEntity)) {
            ServerUtilities.EntityManager.AddComponent<Buff_Persists_Through_Death>(buffEntity);

            ServerUtilities.EntityManager.RemoveComponent<RemoveBuffOnGameplayEvent>(buffEntity);
            ServerUtilities.EntityManager.RemoveComponent<RemoveBuffOnGameplayEventEntry>(buffEntity);
            ServerUtilities.EntityManager.RemoveComponent<CreateGameplayEventsOnSpawn>(buffEntity);
            ServerUtilities.EntityManager.RemoveComponent<GameplayEventListeners>(buffEntity);
            ServerUtilities.EntityManager.RemoveComponent<DestroyOnGameplayEvent>(buffEntity);

            if (ServerUtilities.EntityManager.TryGetComponentData<LifeTime>(entity, out LifeTime lifeTime)) {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;

                ServerUtilities.EntityManager.SetComponentData<LifeTime>(buffEntity, lifeTime);
            }
            return true;
        }
        return false;
    }

    public static bool TryApplyNoLifeTimeBuff(this Entity entity, PrefabGUID buffPrefab) {
        if (entity.TryApplyAndGetBuff(buffPrefab, out Entity buffEntity)) {
            if (ServerUtilities.EntityManager.TryGetComponentData<LifeTime>(entity, out LifeTime lifeTime)) {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;

                ServerUtilities.EntityManager.SetComponentData<LifeTime>(buffEntity, lifeTime);

                return true;
            }
        }
        return false;
    }

    public static bool TryApplyLifeTimeBuff(this Entity entity, PrefabGUID buffPrefab, float duration) {
        if (entity.TryApplyAndGetBuff(buffPrefab, out Entity buffEntity)) {
            if (ServerUtilities.EntityManager.TryGetComponentData<LifeTime>(entity, out LifeTime lifeTime)) {
                lifeTime.Duration = duration;
                lifeTime.EndAction = LifeTimeEndAction.Destroy;

                ServerUtilities.EntityManager.SetComponentData<LifeTime>(buffEntity, lifeTime);

                return true;
            }
        }
        return false;
    }

    public static bool TryRemoveBuff(this Entity entity, PrefabGUID buffPrefab) {
        if (entity.TryGetBuff(buffPrefab, out Entity buffEntity)) {
            DestroyUtility.Destroy(ServerUtilities.EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
            return true;
        }
        return false;
    }

    public static bool TryGetBuff(this Entity entity, PrefabGUID buffPrefab, out Entity buffEntity) {
        return ServerGameManager.TryGetBuff(entity, buffPrefab, out buffEntity);
    }

    public static bool CanApplyStacks(this Entity enttiy, PrefabGUID buffPrefab, out Entity buffEntity, out byte stacks) {
        enttiy.TryGetBuff(buffPrefab, out buffEntity);

        var buff = ServerUtilities.EntityManager.GetComponentData<Buff>(buffEntity);
        stacks = buff.Stacks;
        if (buff.Stacks < buff.MaxStacks) {
            return true;
        }
        else {
            return false;
        }
    }

    public static bool HasBuff(this Entity entity, PrefabGUID buffPrefab) {
        return ServerGameManager.HasBuff(entity, buffPrefab.ToIdentifier());
    }
}