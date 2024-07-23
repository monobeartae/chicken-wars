using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon.Bolt;
public class GameCallbacks : GlobalEventListener
{

    public override void OnEvent(AwardCoinEvent evnt)
    {
        if (evnt.Player == PlayerData.myPlayer)
            PlayerData.UpdatePlayerCoins(evnt.Amount);
    }

    public override void OnEvent(UpdateTeamMaterialsEvent evnt)
    {
        if (PlayerData.myPlayer.GetState<IPlayerState>().Team == evnt.Team)
        {
            TeamInventory.UpdateCrystalOresOwned(evnt.DeltaCrystalOres);
            TeamInventory.UpdateMetalBarsOwned(evnt.DeltaMetalBars);
        }
    }

    public override void OnEvent(CraftWeaponEvent evnt)
    {
        if (BoltNetwork.IsServer)
        {
            BoltEntity weapon = BoltNetwork.Instantiate(evnt.WeaponPrefabID);
            weapon.gameObject.SetActive(false);
            weapon.GetState<IWeaponState>().Durability = evnt.Durability;
            weapon.GetState<IWeaponState>().MaxDurability = evnt.Durability;
            AddToTeamInventoryEvent add_evnt = AddToTeamInventoryEvent.Create(GlobalTargets.Everyone);
            add_evnt.Team = evnt.Team;
            add_evnt.WeaponEntity = weapon;
            add_evnt.PlayerName = evnt.PlayerName;
            add_evnt.Send();
        }
        if (PlayerData.myPlayer.GetState<IPlayerState>().Team == evnt.Team)
            NotificationsManager.instance.AddTeamCraftUI(evnt.PlayerName, Weapon.GetSpriteByID((WeaponID)evnt.WeaponID));
    }

    public override void OnEvent(AddToTeamInventoryEvent evnt)
    {
        if (PlayerData.myPlayer.GetState<IPlayerState>().Team != evnt.Team)
            return;

        Weapon weapon = evnt.WeaponEntity.gameObject.GetComponent<Weapon>();
        TeamInventory.AddToInventory(weapon);
        NotificationsManager.instance.AddPlaceIntoInventoryUI(evnt.PlayerName, weapon.weaponIconSprite);
    }

    public override void OnEvent(TakeFromTeamInventoryEvent evnt)
    {
        if (PlayerData.myPlayer.GetState<IPlayerState>().Team != evnt.Team)
            return;

        Weapon weapon = evnt.Weapon.gameObject.GetComponent<Weapon>();
        TeamInventory.RemoveFromInventory(weapon);
        NotificationsManager.instance.AddTakeFromInventoryUI(evnt.PlayerName, weapon.weaponIconSprite);
    }

    public override void OnEvent(BreakBoxEvent evnt)
    {
        AudioManager.instance.Play3DAudio(SFX_ID.BREAK_BOX, evnt.BoxPosition);
        if (BoltNetwork.IsServer)
        {
            BoltEntity boxEntity = evnt.Box;
            Breakable box = boxEntity.gameObject.GetComponent<Breakable>();
            box.Break();
        }
    }

    public override void OnEvent(PickUpWeaponEvent evnt)
    {
        BoltEntity weaponEntity = evnt.WeaponEntity;
        IPlayerState playerState = evnt.Player.GetState<IPlayerState>();
        PlayerWeapons player = evnt.Player.GetComponent<PlayerWeapons>();
        // Get Rid of/Replace Current Weapon
        player.DropWeapon(evnt.Index, false);
        // Update Player's Weapon State - Fire Callback, everyone incl controller changes weapons
        playerState.Weapons[evnt.Index].Entity = weaponEntity;
    }

    public override void OnEvent(SwitchWeaponEvent evnt)
    {
        PlayerWeapons playerWeapons = evnt.PlayerEntity.gameObject.GetComponent<PlayerWeapons>();
        playerWeapons.SwitchWeapon();
    }
    public override void OnEvent(DropWeaponEvent evnt)
    {
        PlayerWeapons player = evnt.Player.GetComponent<PlayerWeapons>();
        player.DropWeapon(evnt.Index, true);
    }

    public override void OnEvent(BreakWeaponEvent evnt)
    {
        if (evnt.WeaponEntity == null)
        {
            return;
        }
        if (evnt.WeaponEntity.gameObject.GetComponent<Weapon>().ID == WeaponID.MELEE)
            return;

        PlayerWeapons player = evnt.PlayerEntity.GetComponent<PlayerWeapons>();
        player.DropWeapon(evnt.Index, true);
     
        BoltNetwork.Destroy(evnt.WeaponEntity.gameObject);
    }

    public override void OnEvent(WeaponAttackEvent evnt)
    {
        Weapon weapon = evnt.WeaponEntity.gameObject.GetComponent<Weapon>();
        weapon.Attack(evnt.Target);
    }

    public override void OnEvent(LockPlayerRotationEvent evnt)
    {
        evnt.Player.GetComponent<PlayerMotor>().LockRotation(evnt.TargetRotation, evnt.Timer);
    }

    public override void OnEvent(DamageDealtEvent evnt)
    {
        if (!evnt.PlayerEntity.HasControl)
            return;

        UIManager.instance.AddDamageDealtText(evnt.damage, evnt.EnemyEntity.transform.Find("Canvas"));
    }

    public override void OnEvent(DamageTakenEvent evnt)
    {
        if (!evnt.PlayerEntity.HasControl)
            return;

        Transform myTransform = PlayerData.myPlayer.transform.Find("Canvas");
        UIManager.instance.AddDamageTakenText(evnt.damage, myTransform);
    }

    public override void OnEvent(PlayerKillEvent evnt)
    {
        IPlayerState killerState = evnt.KillerEntity.GetState<IPlayerState>();
        IPlayerState deadState = evnt.DeadEntity.GetState<IPlayerState>();
        bool isAllyKill = killerState.Team == PlayerData.myPlayer.GetState<IPlayerState>().Team;
        NotificationsManager.instance.AddKillNotif(isAllyKill, killerState.Name, deadState.Name);
    }

    public override void OnEvent(PlayerRespawnEvent evnt)
    {
        PlayerHP playerHP = evnt.PlayerEntity.gameObject.GetComponent<PlayerHP>();
        playerHP.Respawn();
    }

    public override void OnEvent(PlayerRecallEvent evnt)
    {
        evnt.PlayerEntity.GetState<IPlayerState>().StartRecall();
    }

    public override void OnEvent(PlayerEndRecallEvent evnt)
    {
        evnt.PlayerEntity.gameObject.GetComponent<PlayerRecall>().EndPlayerRecall(evnt.IsComplete);
    }

    public override void OnEvent(ToggleVisibilityEvent evnt)
    {
        PlayerData playerData = evnt.PlayerEntity.gameObject.GetComponent<PlayerData>();
        if (evnt.Interact)
            playerData.ToggleMapVisibility(evnt.On);
        else 
            playerData.ToggleMapVisibility(TeamVisibilityArea.FindArea(evnt.AreaID), evnt.On);

    }

    public override void OnEvent(UpgradePlayerStatsEvent evnt)
    {
        PlayerData playerData = evnt.PlayerEntity.gameObject.GetComponent<PlayerData>();
        if (evnt.DeltaAttackSpeed > 0.0f)
            playerData.UpgradePlayerAttackSpeed(evnt.DeltaAttackSpeed);
        if (evnt.DeltaMovementSpeed > 0.0f)
            playerData.UpgradePlayerMovementSpeed(evnt.DeltaMovementSpeed);
        if (evnt.DeltaHP > 0.0f)
            playerData.UpgradePlayerHP(evnt.DeltaHP);
        if (evnt.DeltaBaseDamage > 0)
            playerData.UpgradePlayerBaseDamage(evnt.DeltaBaseDamage);
    }
    public override void OnEvent(TeamUpgradeEvent evnt)
    {
        
        int myTeam = PlayerData.myPlayer.GetState<IPlayerState>().Team;
        if (myTeam != evnt.TeamID)
            return;
        if (evnt.Buyer != PlayerData.myPlayer)
            ShopManager.instance.Find((ITEM_TYPE)evnt.UpgradeID).MarkUnlocked(evnt.Buyer.GetState<IPlayerState>().Name);

        switch ((ITEM_TYPE)evnt.UpgradeID)
        {
            case ITEM_TYPE.WEAPON_UPGRADE_0:
                TeamInventory.UpdateWeaponDamageMultiplier(0.15f);
                TeamInventory.UpdateWeaponDurability(0.2f);
                break;
            case ITEM_TYPE.WEAPON_UPGRADE_1:
                TeamInventory.UpdateWeaponDamageMultiplier(0.2f);
                TeamInventory.UpdateWeaponDurability(0.2f);
                break;
            case ITEM_TYPE.WEAPON_UPGRADE_21:
                TeamInventory.UpdateWeaponDamageMultiplier(0.35f);
                break;
            case ITEM_TYPE.WEAPON_UPGRADE_22:
                TeamInventory.UpdateWeaponDurability(1.0f);
                break;
            case ITEM_TYPE.CRAFTING_UPGRADE_0:
                TeamInventory.UpdateCraftingCostReduction(0.1f);
                TeamInventory.UpdateMaterialsGatheredMultiplier(0.1f);
                break;
            case ITEM_TYPE.CRAFTING_UPGRADE_10:
                //TeamInventory.UpdateCraftingCostReduction(0.1f);
                //TeamInventory.UpdateCraftingLuck(0.5f);
                TeamInventory.UpdateMaterialsGatheredMultiplier(0.35f);
                break;
            case ITEM_TYPE.CRAFTING_UPGRADE_11:
                TeamInventory.UpdateCraftingCostReduction(0.35f);
                break;
           
        }
    }

}
