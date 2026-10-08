using UnityEngine;
using static EnemyData;

/// <summary>
/// Factory class responsible for creating the initial state of an enemy based on its EnemyData.
/// </summary>
public static class EnemyStateFactory
{
    public static EnemyState CreateInitialState(EnemyStateMachine machine)
    {
        if (machine.Data == null)
        {
            Debug.LogError($"EnemyStateMachine on {machine.gameObject.name} has no EnemyData assigned!");
            return null;
        }

        return machine.Data.EnemyType switch
        {
            EnemyType.Zombie => new ZombieSpawnState(machine),
            EnemyType.ToyDoll => new DollSpawnState(machine),
            _ => new ZombieSpawnState(machine)
        };
    }
}