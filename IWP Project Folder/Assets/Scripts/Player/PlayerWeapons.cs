using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class PlayerWeapons : EntityBehaviour<IPlayerState>
{
    public AudioSource SwitchWeaponSFX;
    public AudioSource BreakWeaponSFX;

    public Transform[] weaponTransform;
    private Weapon[] weapons = new Weapon[2];
    private Image[] weaponSlotIcon = new Image[2];
    private Image[] weaponDurabilityFill = new Image[2];

    private PlayerController playerController = null;

    private GameObject interact_weapon = null;
    private Guid popupID;
    private int currIndex = 0;

    private WaitForSeconds SWITCH_TIMER = new WaitForSeconds(0.25f);
    private bool b_inAnim = false;

    private Color defaultDurabilityColor = new Color32(255, 255, 255, 200);
    private Color lowDurabilityColor = new Color32(255, 100, 100, 200);

    public delegate void WeaponsChanged(int index);
    public static event WeaponsChanged OnWeaponsChanged;

    public override void Attached()
    {
        playerController = GetComponent<PlayerController>();

        // State Callbacks
        state.AddCallback("WeaponIndex", SetCurrentIndex);
        state.AddCallback("Weapons[]", NewWeapon);

        // Disable until game start
        this.enabled = false;
    }

    public void Init()
    {
        StartCoroutine(InitPlayerWeapons());
    }

    IEnumerator InitPlayerWeapons()
    {
 
        if (entity.HasControl)
        {
            // Wait for Local UIManager to load
            while (UIManager.instance == null)
                yield return null;

            // Assign Weapon UI
            weaponSlotIcon[0] = UIManager.instance.WeaponSlotIcons[0];
            weaponSlotIcon[1] = UIManager.instance.WeaponSlotIcons[1];

            weaponDurabilityFill[0] = UIManager.instance.WeaponDurabilityFillBars[0];
            weaponDurabilityFill[1] = UIManager.instance.WeaponDurabilityFillBars[1];
        }

   
        // Wait for everyone to have loaded up til here
        while (GlobalGameState.gameStage != GAME_STAGE.START_GAME_TIMER)
        {
            yield return null;
        }

        if (entity.IsOwner)
        {
            // Give player Melee Starting Basic Attack
          
            InstantiateMelee(0);
            InstantiateMelee(1);

            state.WeaponIndex = 0;
        }


        // wait for local var weapons to finish being assigned from callback from server
        while (weapons[1] == null)
            yield return null;


        SetCurrentIndex(0);
        UIManager.instance.weaponSlots[1].alpha = 0.5f;
        weapons[1].gameObject.SetActive(false);

    }

    void Update()
    {


        if (GlobalGameState.FREEZE_UPDATES || playerController.LOCK_PLAYER_MOVEMENT)
            return;

     
        if (!entity.HasControl)
            return;
        
        if (weapons[currIndex] != null)
        {
            UpdateCurrentWeapon();
        }

        // Mouse Scroll - Switch Weapon
        if (CanSwitchWeapon(Input.mouseScrollDelta.y != 0))
        {
            // Send Event to Server
            SwitchWeaponEvent evnt = SwitchWeaponEvent.Create(GlobalTargets.OnlyServer);
            evnt.PlayerEntity = entity;
            evnt.Send();
            // Set Bool to true to disable interaction
            b_inAnim = true;
        }
        // Interact Key Clicked
        if (Input.GetKeyDown(GameSettings.INTERACT_KEY))
            InteractKeyClicked();
        // Drop Weapon
        if (Input.GetKeyDown(GameSettings.DROPWEAPON_KEY) && weapons[currIndex].ID != WeaponID.MELEE)
        {
            DropWeaponEvent evnt = DropWeaponEvent.Create(GlobalTargets.OnlyServer);
            evnt.Player = entity;
            evnt.Index = currIndex;
            evnt.Send();
        }
         
        if (interact_weapon != null && !interact_weapon.gameObject.activeSelf)
            UIManager.instance.DisableInteractPopUp(popupID);

    }

    void UpdateCurrentWeapon()
    {
        if (!weapons[currIndex].gameObject.activeSelf)
            return;
        if (weapons[currIndex].state.IsBroken)
            return;

        weapons[currIndex].UpdateWeapon();
        if (weapons[currIndex].GetDurability() <= 0)
        {
            // Play Weapon Break SFX
            BreakWeaponSFX.Play();
            // Send BreakWeaponEvent for Server
            BreakWeaponEvent evnt = BreakWeaponEvent.Create(GlobalTargets.OnlyServer);
            evnt.WeaponEntity = weapons[currIndex].entity;
            evnt.PlayerEntity = entity;
            evnt.Index = currIndex;
            evnt.Send();
            // Set Weapon things
            weapons[currIndex].state.IsBroken = true;
            return;
        }
        UpdateWeaponUI(currIndex);
    }

    public bool CanSwitchWeapon(bool input)
    {
        if (!input)
            return false;

        //if (weapons[0] == null || weapons[0] == null || state.Weapons[0].Entity == null || state.Weapons[1].Entity == null)
        //    return false;

        return !b_inAnim;

    }

    public int SwitchWeapon()
    {
        int newIndex = 0;
        switch (currIndex)
        {
            case 0:
                newIndex = 1;
                break;
            case 1:
                newIndex = 0;
                break;
        }
        StartCoroutine(StartSwitchWeapon(currIndex, newIndex));

        currIndex = newIndex;
        state.WeaponIndex = currIndex;

        return currIndex;
    }

    IEnumerator StartSwitchWeapon(int currIndex, int newIndex)
    {
        while (weaponTransform[currIndex].childCount == 0)
        {
            yield return null;
        }

        SwitchWeaponSFX.Play();

        // UNEQUIP CURRENT WEAPON
        Weapon weapon;
        weapon = weaponTransform[currIndex].GetChild(0).gameObject.GetComponent<Weapon>();
        // Set UI - show its uninteractable,  and Trigger Anim
        if (entity.HasControl)
            UIManager.instance.weaponSlots[currIndex].alpha = 0.5f;
        weapon.TriggerAnimation("UnEquip");
        // Wait for Anim to End
        yield return SWITCH_TIMER;
        // Deactivate weapon
        weapon.gameObject.SetActive(false);


        while (weaponTransform[newIndex].childCount == 0)
        {
            yield return null;
        }

        // EQUIP NEW WEAPON
        weapon = weaponTransform[newIndex].GetChild(0).gameObject.GetComponent<Weapon>();
        // Active GO and start anim
        weapon.gameObject.SetActive(true);
        weapon.TriggerAnimation("Equip");
        // Wait for Anim to End
        yield return SWITCH_TIMER;
        // Update UI - show its interactable
        if (entity.HasControl)
            UIManager.instance.weaponSlots[newIndex].alpha = 1.0f;
        // Set Bool back to false
        b_inAnim = false;
    }

    // Used by server to force set a player's weapon index
    public void SetCurrentIndex(int index)
    {
        if (currIndex == index)
            return;

        StartCoroutine(StartSwitchWeapon(currIndex, index));

        currIndex = index;

    }

    // Callback from server when a player's current weapon index has changed
    void SetCurrentIndex()
    {
        if (currIndex == state.WeaponIndex)
            return;

        StartCoroutine(StartSwitchWeapon(currIndex, state.WeaponIndex));

        currIndex = state.WeaponIndex;
    }

    // When Interact Key is Pressed
    void InteractKeyClicked()
    {
        if (interact_weapon == null)
            return;

        // Pick Up Weapon

        PickUpWeaponEvent evnt = PickUpWeaponEvent.Create(GlobalTargets.OnlyServer);
        evnt.WeaponEntity = interact_weapon.GetComponent<Weapon>().entity;
        evnt.Player = PlayerData.myPlayer;
        evnt.Index = state.WeaponIndex;
        evnt.Send();

    }

    // Callback when a player's weapon has changed
    public void NewWeapon(IState state, string propertyPath, ArrayIndices arrayIndices)
    {
        int index = arrayIndices[0];

        StartCoroutine(WaitWeaponSet(index));
    }

    IEnumerator WaitWeaponSet(int index)
    {
        while (state.Weapons[index].Entity == null)
        {
            yield return null;
        }
        BoltEntity newWeapon = state.Weapons[index].Entity;
        PickUpWeapon(index, newWeapon);
    }

    public void PickUpWeapon(int index, BoltEntity weapon)
    {
        //// Equip Weapon
        Weapon newWeapon = weapon.gameObject.GetComponent<Weapon>();
        weapons[index] = newWeapon;
        if (entity.HasControl)
        {
            UpdateWeaponUI(index);
            OnWeaponsChanged?.Invoke(index);
        }

        newWeapon.AttachToPlayer(weaponTransform[index]);

        // Disable Interatc with Weapon UI
        if (entity.HasControl &&
            interact_weapon == weapon.gameObject)
        {
            interact_weapon = null;
            UIManager.instance.DisableInteractPopUp(popupID);
        }

        // SetCurrentIndex(index);
    }

    public BoltEntity DropWeapon(int index, bool fill_melee)
    {
        Weapon currentWeapon = weapons[index];
        if (currentWeapon == null)
        {
            return null;
        }
      
        // Drop Current Weapon
        if (currentWeapon.ID == WeaponID.MELEE)
        {
            // If Melee, no drop just destroy
            BoltNetwork.Destroy(currentWeapon.gameObject);
            weapons[index] = null;
            return null;
        }
        else 
        {
            // Drop Weapon
            currentWeapon.state.Detach();
            if (fill_melee)
            {
                InstantiateMelee(index);
            }
            weapons[index] = null;
            return currentWeapon.entity;
        }
       
    }


    void UpdateWeaponUI(int index)
    {
        Weapon weapon = weapons[index];
        weaponSlotIcon[index].sprite = weapon.weaponIconSprite;
        weaponDurabilityFill[index].fillAmount = weapon.GetDurability() / weapon.GetMaxDurability();
        if (weaponDurabilityFill[index].fillAmount <= 0.2f)
            weaponDurabilityFill[index].color = lowDurabilityColor;
        else
            weaponDurabilityFill[index].color = defaultDurabilityColor;

        UIManager.instance.SetWeaponAttackInterval(index, weapon.GetAttackIntervalPerc());
        int other_index = (index + 1) % 2;
        if (weapons[other_index] != null)
        {
            weapons[other_index].CustomUpdateTimer();
            UIManager.instance.SetWeaponAttackInterval(other_index, weapons[other_index].GetAttackIntervalPerc());
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if (!entity.HasControl)
            return; 

        if (IsPickableWeapon(other.gameObject))
        {
            interact_weapon = other.gameObject;
            popupID = UIManager.instance.SetInteractPopUp("Pick Up " + other.gameObject.name);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!entity.HasControl || interact_weapon != null)
            return;

        if (IsPickableWeapon(other.gameObject))
        {
            interact_weapon = other.gameObject;
            popupID = UIManager.instance.SetInteractPopUp("Pick Up " + other.gameObject.name);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (interact_weapon == other.gameObject
            && entity.HasControl)
        {
            interact_weapon = null;
            UIManager.instance.DisableInteractPopUp(popupID);
        }
    }

    private bool IsPickableWeapon(GameObject go)
    {
        if (!go.CompareTag("Weapon"))
            return false;

        Weapon weapon = go.GetComponent<Weapon>();
        return weapon.is_stray;
    }

    public void InstantiateMelee(int slot_index)
    {
        BoltEntity meleeEntity = BoltNetwork.Instantiate(BoltPrefabs.Melee, Vector3.zero, Quaternion.identity);
       
        state.Weapons[slot_index].Entity = meleeEntity;
    }

    public Weapon GetWeapon(int index)
    {
        return weapons[index];
    }
}
