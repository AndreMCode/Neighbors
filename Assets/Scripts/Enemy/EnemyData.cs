using UnityEngine;

public enum EnemyType
{
    Zombie,
    ToyDoll
}

[CreateAssetMenu(fileName = "EnemyData_", menuName = "Engine/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Movement Speeds")]
    [SerializeField] private float wanderSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Damage Value")]
    [SerializeField] private float damage = 10f;

    [Header("Detection Ranges")]
    [SerializeField] private float aggroRadius = 6f;
    [SerializeField] private float deAggroRadius = 8f; // Slightly larger to prevent border stutter

    [Header("Doll Specific Settings")]
    [SerializeField] private float stalkSpeed = 1.5f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float alignmentThreshold = 0.4f; // Generous threshold for X/Y alignment

    [Header("Despawn & Pooling Behavior")]
    [SerializeField] private bool allowOffscreenDespawn = true;
    [Tooltip("Extra viewport-space padding beyond the screen edges before despawning (0 = exactly at screen edge, 0.5 = half a screen beyond edge).")]
    [SerializeField] private float offscreenDespawnPadding = 0.2f; // Viewport-space padding past camera viewport boundary

    [Header("Optimizations & Layers")]
    [SerializeField] private float checkInterval = 0.2f; // 5 checks per second
    [SerializeField] private LayerMask obstacleLayerMask;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Enemy Classification")]
    [SerializeField] private EnemyType enemyType;
    public EnemyType EnemyType => enemyType;

    // Public Getters
    public float WanderSpeed => wanderSpeed;
    public float ChaseSpeed => chaseSpeed;

    public float Damage => damage;

    public float AggroRadius => aggroRadius;
    public float DeAggroRadius => deAggroRadius;
    public float SqrAggroRadius => aggroRadius * aggroRadius;
    public float SqrDeAggroRadius => deAggroRadius * deAggroRadius;

    public float StalkSpeed => stalkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float AlignmentThreshold => alignmentThreshold;

    public bool AllowOffscreenDespawn => allowOffscreenDespawn;
    public float OffscreenDespawnPadding => offscreenDespawnPadding;

    public float CheckInterval => checkInterval;
    public LayerMask ObstacleLayerMask => obstacleLayerMask;
    public LayerMask PlayerLayerMask => playerLayerMask;
}