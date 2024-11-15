﻿using System;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering.HighDefinition;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekServer : EnemyAI
{
    private ManualLogSource _mls;
    private string _dalekId;
    
    private enum States
    {
        Searching,
        InvestigatingTargetPosition,
        Shooting,
        Detain,
        Dead
    }
    
    [Header("AI and Pathfinding")] [Space(5f)]
    public AISearchRoutine searchForPlayers;
    
    [SerializeField] private float agentMaxAcceleration = 50f;
    [SerializeField] private float agentMaxSpeed = 0.3f;
    [SerializeField] private float maxSearchRadius = 100f;
    [SerializeField] private float viewWidth = 100f;
    [SerializeField] private int viewRange = 80;
    [SerializeField] private int proximityAwareness = 3;
    [SerializeField] private float shootDelay = 1f;
    
#pragma warning disable 0649
    [Header("Controllers")] [Space(5f)]
    [SerializeField] private Transform gun;
    [SerializeField] private DalekNetcodeController netcodeController;
#pragma warning restore 0649
    
    private float _agentMaxAcceleration;
    private float _agentMaxSpeed;
    private float _takeDamageCooldown;
    private float _shootTimer;
    private float _audioLineTimer;

    private bool _isShooting;
    
    private Vector3 _targetPosition;

    private void OnEnable()
    {
        netcodeController.OnSetIsShooting += HandleSetIsShooting;
    }

    private void OnDisable()
    {
        netcodeController.OnSetIsShooting -= HandleSetIsShooting;
    }

    public override void Start()
    {
        base.Start();
        if (!IsServer) return;

        _dalekId = Guid.NewGuid().ToString();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek {_dalekId}");

        netcodeController = GetComponent<DalekNetcodeController>();
        if (netcodeController == null) _mls.LogError("Netcode Controller is null");
        
        netcodeController.SyncDalekIdClientRpc(_dalekId);
        UnityEngine.Random.InitState(StartOfRound.Instance.randomMapSeed + _dalekId.GetHashCode());
        InitializeConfigValues();

        netcodeController.SpawnDalekLazerGunServerRpc(_dalekId);
        netcodeController.GrabDalekLazerGunClientRpc(_dalekId);
        SwitchBehaviourStateLocally(States.Searching);
        
        LogDebug("Dalek Spawned!");
    }

    public override void Update()
    {
        base.Update();
        if (!IsServer) return;

        _shootTimer -= Time.deltaTime;
        _takeDamageCooldown -= Time.deltaTime;
        _audioLineTimer -= Time.deltaTime;

        switch (currentBehaviourStateIndex)
        {
            case (int)States.Searching:
            {
                break;
            }

            case (int)States.InvestigatingTargetPosition:
            {
                break;
            }

            case (int)States.Shooting:
            {
                if (_isShooting) _shootTimer = shootDelay;
                break;
            }
        }

        CalculateAgentSpeed();
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (!IsServer) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)States.Searching:
            {
                PlayerControllerB tempTargetPlayer = CheckLineOfSightForClosestPlayer(viewWidth, viewRange, proximityAwareness);
                if (tempTargetPlayer != null)
                {
                    ChangeTargetPlayer(tempTargetPlayer.actualClientId);
                    SwitchBehaviourStateLocally(States.Shooting);
                    break;
                }

                if (_audioLineTimer < 0)
                {
                    netcodeController.PlayAudioClipTypeServerRpc(_dalekId, DalekNetcodeController.AudioClipTypes.Roaming);
                    _audioLineTimer = 20f;
                }
                
                break;
            }

            case (int)States.InvestigatingTargetPosition:
            {
                // Check for player in LOS
                PlayerControllerB tempTargetPlayer = CheckLineOfSightForClosestPlayer(viewWidth, viewRange, proximityAwareness);
                if (tempTargetPlayer != null)
                {
                    ChangeTargetPlayer(tempTargetPlayer.actualClientId);
                    SwitchBehaviourStateLocally(States.Shooting);
                    break;
                }
                
                // If player isn't in LOS and dalek has reached the player's last known position, then switch to state 0
                if (Vector3.Distance(transform.position, _targetPosition) <= 1)
                {
                    SwitchBehaviourStateLocally(States.Searching);
                }
                
                if (_audioLineTimer < 0)
                {
                    netcodeController.PlayAudioClipTypeServerRpc(_dalekId, DalekNetcodeController.AudioClipTypes.Roaming);
                    _audioLineTimer = 20f;
                }
                
                break;
            }

            case (int)States.Shooting:
            {
                PlayerControllerB playerControllerB = CheckLineOfSightForClosestPlayer(viewWidth, viewRange, proximityAwareness);
                if (playerControllerB == null)
                {
                    SwitchBehaviourStateLocally(States.InvestigatingTargetPosition);
                    break;
                }
                
                ChangeTargetPlayer(playerControllerB.playerClientId);
                
                // _targetPosition is the last seen position of a player before they went out of view
                _targetPosition = targetPlayer.transform.position;
                netcodeController.IncreaseTargetPlayerFearLevelClientRpc(_dalekId);
                
                if (stunNormalizedTimer > 0) break;
                AimAtPosition(targetPlayer.transform.position);
                
                // Check if the shoot timer is complete
                if (_shootTimer > 0 || _isShooting) break;
        
                if (IsGunAimingAtPlayer())
                {
                    LogDebug("Shooting player");
                    netcodeController.ShootGunClientRpc(_dalekId);
                    _shootTimer = shootDelay;
                }
                
                break;
            }
        }
    }

    private bool IsGunAimingAtPlayer()
    {
        Ray ray = new(gun.transform.position, gun.up);
        if (Physics.Raycast(ray, out UnityEngine.RaycastHit hit, 100))
        {
            return hit.collider.gameObject.tag == "Player";
        }

        return false;
    }
    
    private void AimAtPosition(Vector3 position)
    {
        Vector3 direction = (position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    private void ChangeTargetPlayer(ulong playerObjectId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[playerObjectId];
        if (player != targetPlayer) netcodeController.ChangeTargetPlayerClientRpc(_dalekId, playerObjectId);
        targetPlayer = player;
    }

    private void BeginChasingPlayer(ulong targetPlayerObjectId)
    {
        if (!IsServer) return;
        netcodeController.ChangeTargetPlayerClientRpc(_dalekId, targetPlayerObjectId);
        PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[targetPlayerObjectId];
        SetMovingTowardsTargetPlayer(player);
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitId = -1)
    {
        base.HitEnemy(force, playerWhoHit, playHitSFX, hitId);
        if (!IsServer) return;
        if (isEnemyDead) return;
        if (_takeDamageCooldown > 0.03f) return;
        
        _takeDamageCooldown = 0.03f;
        enemyHP -= force;

        if (enemyHP > 0)
        {
            if (playerWhoHit == null) return;
            netcodeController.ChangeTargetPlayerClientRpc(_dalekId, playerWhoHit.playerClientId);
            SwitchBehaviourStateLocally(States.Shooting);
            BeginChasingPlayer(playerWhoHit.playerClientId);
        }
        else
        {
            // Dalek is dead
            netcodeController.EnterDeathStateClientRpc(_dalekId);
            KillEnemyClientRpc(false);
            SwitchBehaviourStateLocally(States.Dead);
        }
    }

    public override void SetEnemyStunned(
        bool setToStunned,
        float setToStunTime = 1f,
        PlayerControllerB setStunnedByPlayer = null)
    {
        base.SetEnemyStunned(setToStunned, setToStunTime, setStunnedByPlayer);
        if (!IsServer) return;
        if (currentBehaviourStateIndex is (int)States.Dead || isEnemyDead) return;
        
        // Play noise

        if (setStunnedByPlayer != null)
        {
            netcodeController.ChangeTargetPlayerClientRpc(_dalekId, setStunnedByPlayer.playerClientId);
            SwitchBehaviourStateLocally(States.Shooting);
            BeginChasingPlayer(setStunnedByPlayer.playerClientId);
        }
    }

    private void InitializeState(int state)
    {
        if (!IsServer) return;
        switch (state)
        {
            case (int)States.Searching:
            {
                _agentMaxAcceleration = 2f;
                _agentMaxAcceleration = 20f;
                movingTowardsTargetPlayer = false;
                openDoorSpeedMultiplier = 6;
                
                netcodeController.ChangeTargetPlayerClientRpc(_dalekId, 69420);
                if (searchForPlayers.inProgress) StopSearch(searchForPlayers);
                
                if (_targetPosition != default)
                {
                    if (CheckForPath(_targetPosition))
                    {
                        searchForPlayers.searchWidth = 30f;
                        StartSearch(_targetPosition, searchForPlayers);
                    }
                }
                else
                {
                    // If there is no target player last seen position, just search from where the dalek is currently at
                    searchForPlayers.searchWidth = 100f;
                    StartSearch(transform.position, searchForPlayers);
                }
                
                break;
            }

            case (int)States.InvestigatingTargetPosition:
            {
                _agentMaxAcceleration = 2f;
                _agentMaxAcceleration = 25f;
                movingTowardsTargetPlayer = false;
                openDoorSpeedMultiplier = 2;
                
                // Set investigating position
                if (searchForPlayers.inProgress) StopSearch(searchForPlayers);
                if (_targetPosition == default) SwitchBehaviourStateLocally((int)States.Searching);
                else
                {
                    if (!SetDestinationToPosition(_targetPosition, true))
                    {
                        SwitchBehaviourStateLocally((int)States.Searching);
                    }
                }
                
                break;
            }

            case (int)States.Shooting:
            {
                _agentMaxSpeed = 0f;
                _agentMaxAcceleration = 0f;
                movingTowardsTargetPlayer = true;
                openDoorSpeedMultiplier = 1f;
                
                if (searchForPlayers.inProgress) StopSearch(searchForPlayers);
                
                netcodeController.PlayAudioClipTypeServerRpc(_dalekId, DalekNetcodeController.AudioClipTypes.SawPlayer, true);
                
                break;
            }

            case (int)States.Detain:
            {
                break;
            }

            case (int)States.Dead:
            {
                _agentMaxSpeed = 0f;
                _agentMaxAcceleration = 0f;
                movingTowardsTargetPlayer = false;
                moveTowardsDestination = false;
                agent.speed = 0;
                agent.enabled = false;
                isEnemyDead = true;
                
                if (searchForPlayers.inProgress) StopSearch(searchForPlayers);
                
                break;
            }
        }
    }

    private void SwitchBehaviourStateLocally(States state)
    {
        SwitchBehaviourStateLocally((int)state);
    }
    
    /// <summary>
    /// Switches to the given behaviour state
    /// </summary>
    /// <param name="state">The state to change to</param>
    private void SwitchBehaviourStateLocally(int state)
    {
        LogDebug($"Switching to behaviour state {state}!");
        InitializeState(state);
        if (!IsServer || currentBehaviourStateIndex == state) return;
        previousBehaviourStateIndex = currentBehaviourStateIndex;
        currentBehaviourStateIndex = state;
        LogDebug($"Switch to behaviour state {state} complete!");
    }
    
    public override void FinishedCurrentSearchRoutine()
    {
        base.FinishedCurrentSearchRoutine();
        if (!IsServer) return;
        if (searchForPlayers.inProgress)
            searchForPlayers.searchWidth = Mathf.Clamp(searchForPlayers.searchWidth + 10f, 1f, maxSearchRadius);
    }

    private void InitializeConfigValues()
    {
        if (!IsServer) return;
    }

    private void CalculateAgentSpeed()
    {
        if (!IsServer) return;
        if (stunNormalizedTimer > 0)
        {
            agent.speed = 0;
            agent.acceleration = _agentMaxAcceleration;
            
        }
        
        else if (currentBehaviourStateIndex == (int)States.Shooting)
        {
            if (CheckLineOfSightForPlayer(viewWidth, viewRange, proximityAwareness) || _isShooting)
            {
                agent.speed = 0;
                agent.acceleration = _agentMaxAcceleration;
            }
            else
            {
                MoveWithAcceleration();
            }
        }

        else if (currentBehaviourStateIndex != (int)States.Dead)
        {
            MoveWithAcceleration();
        }
    }
    
    /// <summary>
    /// Makes the agent move by using interpolation to make the movement smooth
    /// </summary>
    private void MoveWithAcceleration()
    {
        if (!IsServer) return;
        
        float speedAdjustment = Time.deltaTime / 2f;
        agent.speed = Mathf.Lerp(agent.speed, _agentMaxSpeed, speedAdjustment);
        
        float accelerationAdjustment = Time.deltaTime;
        agent.acceleration = Mathf.Lerp(agent.acceleration, _agentMaxAcceleration, accelerationAdjustment);
    }
    
    private bool CheckForPath(Vector3 position)
    {
        position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 1.75f);
        path1 = new NavMeshPath();
        
        // ReSharper disable once UseIndexFromEndExpression
        return agent.CalculatePath(position, path1) && !(Vector3.Distance(path1.corners[path1.corners.Length - 1], RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 2.7f)) > 1.5499999523162842);
    }

    private void HandleSetIsShooting(string receivedDalekId, bool setIsShooting)
    {
        if (_dalekId != receivedDalekId) return;
        _isShooting = setIsShooting;
    }
    
    private void LogDebug(string msg)
    {
        #if DEBUG
        _mls?.LogInfo($"State:{currentBehaviourStateIndex}, {msg}");
        #endif
    }
}