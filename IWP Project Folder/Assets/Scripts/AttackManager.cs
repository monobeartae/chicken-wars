using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;

public class AttackManager
{
    public static void Attack(GameObject go, float damage, int team_id, bool isPlayer, BoltEntity attacker) // attacker var is null for turret bullets, mainly used for players only
    {
        if (go.CompareTag("Breakable"))
        {
            BreakBoxEvent evnt = BreakBoxEvent.Create(GlobalTargets.Everyone);
            evnt.Box = go.GetComponent<BoltEntity>();
            evnt.BoxPosition = go.transform.position;
            evnt.Send();
            //if (isPlayer)
           //     SendCoins(GameSettings.BREAK_BOX_COINS, attacker);

        }
        else if (go.CompareTag("Player"))
        {
            BoltEntity entity = go.GetComponent<BoltEntity>();
            // Check if is player/same team
            IPlayerState playerState = entity.GetState<IPlayerState>();
            if (playerState.Team == team_id || playerState.IsDead)
                return;

            entity.gameObject.GetComponent<PlayerHP>().UpdateHP(-1 * damage);

            DamageTakenEvent ouch_evnt = DamageTakenEvent.Create(GlobalTargets.Everyone);
            ouch_evnt.PlayerEntity = entity;
            ouch_evnt.damage = damage;
            ouch_evnt.Send();
            if (isPlayer)
            {
                if (playerState.IsDead)
                {
                    SendCoins(GameSettings.PLAYER_KILL_COINS, attacker);
                    SendPlayerKillLog(attacker, entity);
                }
                SendDamageDealtEvent(attacker, damage, entity);
                SendToggleVisibilityEvent(attacker);
                SendToggleVisibilityEvent(entity);
            }

        }
        else if (go.CompareTag("MainCrystal"))
        {
            if (CompareEntityTeam(go, team_id))
                return;
           
            Entity go_entity = go.GetComponent<Entity>();
            go_entity.UpdateHP(-1 * damage);
            if (isPlayer)
            {
                SendDamageDealtEvent(attacker, damage, go.GetComponent<BoltEntity>());
            }
        }
        else if (go.CompareTag("Turret"))
        {
            if (CompareEntityTeam(go, team_id))
                return;

            Entity go_entity = go.GetComponent<Entity>();
            if (go_entity.state.IsDead)
                return;
            go_entity.UpdateHP(-1 * damage);
            if (isPlayer)
            {
                if (go_entity.state.IsDead)
                    SendCoins(GameSettings.TURRET_DESTROYED_COINS, attacker);
                SendDamageDealtEvent(attacker, damage, go.GetComponent<BoltEntity>());
            }
        }
        else if (go.CompareTag("Minion"))
        {
            if (CompareEntityTeam(go, team_id))
                return;

            Entity go_entity = go.GetComponent<Entity>();
            if (go_entity.state.IsDead)
                return;
            go_entity.UpdateHP(-1 * damage);
            if (isPlayer)
            {
                if (go_entity.state.IsDead)
                    SendCoins(GameSettings.MINION_KILL_COINS, attacker);
                SendDamageDealtEvent(attacker, damage, go.GetComponent<BoltEntity>());
            }
        }
        else if (go.CompareTag("Monster"))
        {
            BoltEntity goEntity = go.GetComponent<BoltEntity>();
           
            Entity go_entity = go.GetComponent<Entity>();
            if (go_entity.state.IsDead)
                return;
            go_entity.UpdateHP(-1 * damage);
            go_entity.gameObject.GetComponent<Metalon>().SetTargetEntity(attacker);
            if (isPlayer)
            {
                if (go_entity.state.IsDead)
                {
                    SendCoins(GameSettings.METALON_KILL_COINS, attacker);
                    AwardTeamMaterials(attacker.GetState<IPlayerState>().Team, Metalon.METAL_BAR_DROP_AMT, Metalon.CRYSTAL_ORE_DROP_AMOUNT);
                }
                SendDamageDealtEvent(attacker, damage, goEntity);
            }
        }
    }

    static void SendPlayerKillLog(BoltEntity killer, BoltEntity dead)
    {
        PlayerKillEvent evnt = PlayerKillEvent.Create(GlobalTargets.Everyone);
        evnt.KillerEntity = killer;
        evnt.DeadEntity = dead;
        evnt.Send();
    }

    static void SendCoins(int amt, BoltEntity receiver)
    {
        AwardCoinEvent evnt = AwardCoinEvent.Create(GlobalTargets.Everyone);
        evnt.Amount = amt;
        evnt.Player = receiver;
        evnt.Send();
    }

    static void SendDamageDealtEvent(BoltEntity player, float damage, BoltEntity target)
    {
        DamageDealtEvent yay_evnt = DamageDealtEvent.Create(GlobalTargets.Everyone);
        yay_evnt.PlayerEntity = player;
        yay_evnt.damage = damage;
        yay_evnt.EnemyEntity = target;
        yay_evnt.Send();
    }

    static bool CompareEntityTeam(GameObject go, int team_id)
    {
        BoltEntity goEntity = go.GetComponent<BoltEntity>();
        IEntityState entityState = goEntity.GetState<IEntityState>();
        // Check if is player/same team
        if (entityState.Team == team_id)
            return true;
        return false;
    }

    static void AwardTeamMaterials(int Team, int metalAmt, int crystalAmt)
    {
        UpdateTeamMaterialsEvent evnt = UpdateTeamMaterialsEvent.Create(GlobalTargets.Everyone);
        evnt.Team = Team;
        evnt.DeltaCrystalOres = crystalAmt;
        evnt.DeltaMetalBars = metalAmt;
        evnt.Send();
    }

    static void SendToggleVisibilityEvent(BoltEntity player)
    {
        ToggleVisibilityEvent evnt = ToggleVisibilityEvent.Create(GlobalTargets.Everyone);
        evnt.PlayerEntity = player;
        evnt.Interact = true;
        evnt.On = true;
        evnt.Send();
    }
}
