using System;
using System.Linq;
using BepInEx.Logging;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.AI;
using Logger = BepInEx.Logging.Logger;

namespace LethalCompanyDalek;

public class DalekServer : EnemyAI
{
    private ManualLogSource _mls;
    private string _dalekId;
    
    [Header("AI and Pathfinding")] [Space(5f)]
    public AISearchRoutine searchForPlayers;
    
    [SerializeField] private float agentMaxAcceleration = 50f;
    [SerializeField] private float agentMaxSpeed = 0.3f;
    [SerializeField] private float maxSearchRadius = 100f;
    [SerializeField] private float attackCooldown = 6f;
    [SerializeField] private float viewWidth = 70f;
    [SerializeField] private int viewRange = 80;
    [SerializeField] private int proximityAwareness = 3;
    
    private float _agentMaxAcceleration;
    private float _agentMaxSpeed;
    private float _takeDamageCooldown;

    private bool _hasBegunInvestigating;

    private Vector3 _targetPosition;
    
    #pragma warning disable 0649
    [Header("Controllers")] [Space(5f)] 
    [SerializeField] private DalekNetcodeController netcodeController;
    #pragma warning restore 0649

    private enum States
    {
        Searching,
        InvestigatingTargetPosition,
        Chasing,
        Detain,
        Dead
    }

    public override void Start()
    {
        base.Start();
        if (!IsServer) return;

        _dalekId = Guid.NewGuid().ToString();
        _mls = Logger.CreateLogSource($"{DalekPlugin.ModGuid}|Dalek {_dalekId}");

        netcodeController = GetComponent<DalekNetcodeController>();
        if (netcodeController == null) _mls.LogError("Netcode Controller is null");
        
        netcodeController.UpdateDalekIdClientRpc(_dalekId);
        UnityEngine.Random.InitState(StartOfRound.Instance.randomMapSeed + _dalekId.GetHashCode());
        
        LogDebug("Dalek Spawned!");
    }

    public override void Update()
    {
        base.Update();
        if (!IsServer) return;

        _takeDamageCooldown -= Time.deltaTime;

        switch (currentBehaviourStateIndex)
        {
            case (int)States.Searching:
            {
                break;
            }
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();
        if (!IsServer) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)States.Searching:
            {
                PlayerControllerB tempTargetPlayer = CheckLineOfSightForClosestPlayer(viewWidth, viewRange, Mathf.Clamp(proximityAwareness, -1, int.MaxValue));
                if (tempTargetPlayer != null)
                {
                    SwitchBehaviourStateLocally((int)States.Chasing);
                    break;
                }

                if (!searchForPlayers.inProgress)
                {
                    if (_targetPosition != default)
                    {
                        if (CheckForPath(_targetPosition))
                        {
                            searchForPlayers.searchWidth = 30f;
                            StartSearch(_targetPosition, searchForPlayers);
                            break;
                        }
                    }
                    
                    // If there is no target player last seen position, just search from where the dalek is currently at
                    searchForPlayers.searchWidth = 100f;
                    StartSearch(transform.position, searchForPlayers);
                }
                
                break;
            }

            case (int)States.InvestigatingTargetPosition:
            {
                if (searchForPlayers.inProgress) StopSearch(searchForPlayers);
                
                // Check for player in LOS
                PlayerControllerB tempTargetPlayer = CheckLineOfSightForClosestPlayer(viewWidth, viewRange, proximityAwareness);
                if (tempTargetPlayer != null)
                {
                    SwitchBehaviourStateLocally((int)States.Chasing);
                    break;
                }
                
                // If player isn't in LOS and dalek has reached the player's last known position, then switch to state 0
                if (Vector3.Distance(transform.position, _targetPosition) <= 1)
                {
                    SwitchBehaviourStateLocally((int)States.Searching);
                }
                
                break;
            }

            case (int)States.Chasing:
            {
                if (searchForPlayers.inProgress) StopSearch(searchForPlayers);
                
                // Check for players in LOS
                PlayerControllerB[] playersInLineOfSight = GetAllPlayersInLineOfSight(viewWidth, viewRange, eye, proximityAwareness,
                    layerMask: StartOfRound.Instance.collidersAndRoomMaskAndDefault);

                // Check if our target is in LOS
                bool ourTargetFound = false;
                if (playersInLineOfSight is { Length: > 0 })
                {
                    ourTargetFound = targetPlayer != null && playersInLineOfSight.Any(playerControllerB => playerControllerB == targetPlayer && playerControllerB != null);
                }
                // If no players were found, switch to state 2
                else
                {
                    SwitchBehaviourStateLocally((int)States.InvestigatingTargetPosition);
                    break;
                }
                
                // If our target wasn't found, switch target
                if (!ourTargetFound)
                {
                    // Get the closest player and set them as target
                    PlayerControllerB playerControllerB = CheckLineOfSightForClosestPlayer(viewWidth, viewRange, proximityAwareness);
                    if (playerControllerB == null)
                    {
                        SwitchBehaviourStateLocally((int)States.InvestigatingTargetPosition);
                        break;
                    }
                    
                    BeginChasingPlayer(playerControllerB.playerClientId);
                }
                
                _targetPosition = targetPlayer.transform.position;
                netcodeController.IncreaseTargetPlayerFearLevelClientRpc(_dalekId);
                
                // Check if a player is in attack area and attack
                // if (Vector3.Distance(transform.position, targetPlayer.transform.position) < 8) AttackPlayerIfClose();
                break;
            }
        }
    }

    private void BeginChasingPlayer(ulong targetPlayerObjectId)
    {
        if (!IsServer) return;
        
    }

    private void InitializeState(int state)
    {
        if (!IsServer) return;
        switch (state)
        {
            case (int)States.Searching:
            {
                break;
            }

            case (int)States.InvestigatingTargetPosition:
            {
                
                // Set investigating position
                if (_targetPosition == default) SwitchBehaviourStateLocally((int)States.Searching);
                else
                {
                    if (!SetDestinationToPosition(_targetPosition, true))
                    {
                        SwitchBehaviourStateLocally((int)States.Searching);
                        break;
                    }
                    _hasBegunInvestigating = true;
                }
                
                break;
            }

            case (int)States.Chasing:
            {
                break;
            }

            case (int)States.Detain:
            {
                break;
            }

            case (int)States.Dead:
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// Switches to the given behaviour state
    /// </summary>
    /// <param name="state">The state to change to</param>
    private void SwitchBehaviourStateLocally(int state)
    {
        if (!IsServer || currentBehaviourStateIndex == state) return;
        LogDebug($"Switched to behaviour state {state}!");
        previousBehaviourStateIndex = currentBehaviourStateIndex;
        currentBehaviourStateIndex = state;
        InitializeState(state);
        LogDebug($"Switch to behaviour state {state} complete!");
    }
    
    public override void FinishedCurrentSearchRoutine()
    {
        base.FinishedCurrentSearchRoutine();
        if (!IsServer) return;
        if (searchForPlayers.inProgress)
            searchForPlayers.searchWidth = Mathf.Clamp(searchForPlayers.searchWidth + 10f, 1f, maxSearchRadius);
    }
    
    private bool CheckForPath(Vector3 position)
    {
        position = RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 1.75f);
        path1 = new NavMeshPath();
        
        // ReSharper disable once UseIndexFromEndExpression
        return agent.CalculatePath(position, path1) && !(Vector3.Distance(path1.corners[path1.corners.Length - 1], RoundManager.Instance.GetNavMeshPosition(position, RoundManager.Instance.navHit, 2.7f)) > 1.5499999523162842);
    }
    
    private void LogDebug(string msg)
    {
        #if DEBUG
        _mls?.LogInfo($"State:{currentBehaviourStateIndex}, {msg}");
        #endif
    }
}