using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


using Photon.Bolt;

public class PlayerData : EntityEventListener<IPlayerState>
{
    // Local Player Vars
    public static BoltEntity myPlayer = null;
    public static string PlayerName = "Unknown";

    public static int PlayerCoins = 0;
    public AudioSource CoinsEarnedSFX;


    // Display Name
    public Text playerName_text;
    // Minimap UI
    public Image MinimapIcon;

    public delegate void PlayerCoinsChanged();
    public static event PlayerCoinsChanged OnPlayerCoinsChanged;

    public delegate void AttackSpeedChanged();
    public event AttackSpeedChanged OnAttackSpeedChanged;

    // Minimap Visibility - Used by Server Only
    private bool is_publicly_visible = false;
    private List<TeamVisibilityArea> inPublicSightList = new List<TeamVisibilityArea>();
    private float INTERACT_VISIBILITY_TIMER = 3.0f;
    private float visibility_timer = 0.0f;

    public override void Attached()
    {

        state.AddCallback("Name", SetNameUI);
        state.AddCallback("AttackSpeedMultiplier", AttackSpeedChangedCallback);

        if (BoltNetwork.IsServer)
        {
            state.TeamSet = false;
            state.BaseDamage = 5;
            state.AttackSpeedMultiplier = 1.0f;
            state.MovementSpeedMultiplier = 1.0f;
        }

        PlayerCoins = 300;

        if (entity.HasControl)
        {
            InitPlayerData();
        }
        else
        {
            SetNameUI();
        }

        StartCoroutine(SetTeamInfo());

        OnPlayerCoinsChanged += PlayCoinsSFX;
    }

    // Called by Server because Attached is called before Control can be taken on server side
    public void InitPlayerData()
    {

        myPlayer = this.entity;
        playerName_text.gameObject.SetActive(false);
        gameObject.GetComponent<AudioListener>().enabled = true;

        SendPlayerData();

    }

    IEnumerator SetTeamInfo()
    {
        while (myPlayer == null)
            yield return null;

        while (!myPlayer.GetState<IPlayerState>().TeamSet)
        {
            yield return null;
        }

        while (!state.TeamSet)
        {
            yield return null;
        }

        // Set Display Name and HP Bar Color
        PlayerHP playerHP = gameObject.GetComponent<PlayerHP>();
        IPlayerState myState = myPlayer.GetState<IPlayerState>();
        if (myState.Team == state.Team)
        {
            // ALLY
            playerName_text.color = Color.white;
            playerHP.InitHPBarColor(true);
            MinimapIcon.color = MinimapIconSettings.AllyIconColour;
        }
        else
        {
            // ENEMY
            playerName_text.color = Color.red;
            playerHP.InitHPBarColor(false);
            MinimapIcon.color = MinimapIconSettings.EnemyIconColour;
            MinimapIcon.gameObject.SetActive(false);
        }
    
    }

    void Update()
    {
        if (!BoltNetwork.IsServer)
            return;

        if (visibility_timer > 0.0f)
        {
            visibility_timer -= Time.deltaTime;
            if (visibility_timer <= 0.0f)
            {
                visibility_timer = 0.0f;
                // Trigger Off Visibility if not in public area
                if (inPublicSightList.Count == 0)
                {
                    ToggleVisibilityEvent evnt = ToggleVisibilityEvent.Create(GlobalTargets.Everyone);
                    evnt.PlayerEntity = entity;
                    evnt.Interact = true;
                    evnt.On = false;
                    evnt.Send();
                }
            }
        }
    }

    // Set Player Name UI
    public void SetNameUI()
    {
        playerName_text.text = state.Name;
    }

    public void SendPlayerData()
    {
        
        if (BoltNetwork.IsClient)
        {
            SendPlayerDataEvent evnt = SendPlayerDataEvent.Create(GlobalTargets.OnlyServer);
            evnt.Name = PlayerName;
            evnt.PlayerDataEntity = this.entity;
            evnt.Send();
        }
        else
        {
            state.Name = PlayerName;
        }
    }

    public static void UpdatePlayerCoins(int delta_amount)
    {
        PlayerCoins += delta_amount;
        OnPlayerCoinsChanged?.Invoke();
        NotificationsManager.instance.AddCurrencyEarnedUI(CURRENCY_TYPE.COINS, delta_amount);
    }

    private void PlayCoinsSFX()
    {
        CoinsEarnedSFX.Play();
    }

    public void ToggleMapVisibility(bool on) // Event Callback for All Clients when this player enters opposing team's sight
    {
        if (on)
        {
            if (BoltNetwork.IsServer)
                visibility_timer = INTERACT_VISIBILITY_TIMER;

            MinimapIcon.gameObject.SetActive(true);
        }
        else if (state.Team != myPlayer.GetState<IPlayerState>().Team)
            MinimapIcon.gameObject.SetActive(false);
    }

    public void ToggleMapVisibility(TeamVisibilityArea area, bool on) // Event Callback for All Clients when this player enters/exits opposing team's sight
    {
        if (BoltNetwork.IsServer)
        {
            if (on)
                inPublicSightList.Add(area);
            else if (inPublicSightList.Contains(area))
                inPublicSightList.Remove(area);
        }

        if (on)
            MinimapIcon.gameObject.SetActive(true);
        else if (state.Team != myPlayer.GetState<IPlayerState>().Team)
            MinimapIcon.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;

        if (other.gameObject.CompareTag("VisibilityMarker")) // Player Entity entered public team area
        {
            TeamVisibilityArea teamArea = other.gameObject.GetComponent<TeamVisibilityArea>();
            if (teamArea.TeamID != state.Team) // team area belongs to opponent
            {
                ToggleVisibilityEvent evnt = ToggleVisibilityEvent.Create(GlobalTargets.Everyone);
                evnt.AreaID = teamArea.AreaID;
                evnt.PlayerEntity = entity;
                evnt.Interact = false;
                evnt.On = true;
                evnt.Send();

            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!BoltNetwork.IsServer)
            return;

        if (other.gameObject.CompareTag("VisibilityMarker")) // Player Entity entered public team area
        {
            TeamVisibilityArea teamArea = other.gameObject.GetComponent<TeamVisibilityArea>();
            if (teamArea.TeamID != state.Team // team area belongs to opponent
                && visibility_timer <= 0.0f)  // not alr in public sight
            {
                ToggleVisibilityEvent evnt = ToggleVisibilityEvent.Create(GlobalTargets.Everyone);
                evnt.AreaID = teamArea.AreaID;
                evnt.PlayerEntity = entity;
                evnt.Interact = false;
                evnt.On = false;
                evnt.Send();

            }
        }
    }

    public float GetVisibilityTimer()
    {
        return visibility_timer;
    }

    public void UpgradePlayerHP(float delta_perc)
    {
        int hp_bonus = (int)(delta_perc * state.MaxHP);

        state.MaxHP += hp_bonus;
        state.HP += hp_bonus;
        if (state.HP > state.MaxHP)
        {
            state.HP = state.MaxHP;
        }
    }

    public void UpgradePlayerMovementSpeed(float delta_perc)
    {
        state.MovementSpeedMultiplier *= (1.0f + delta_perc);
    }

    public void UpgradePlayerAttackSpeed(float delta_perc)
    {
        state.AttackSpeedMultiplier *= (1.0f + delta_perc);
    }

    private void AttackSpeedChangedCallback()
    {
        if (entity.HasControl)
            OnAttackSpeedChanged?.Invoke();
    }
    public void UpgradePlayerBaseDamage(int delta_amt)
    {
        state.BaseDamage += delta_amt;
    }
}
