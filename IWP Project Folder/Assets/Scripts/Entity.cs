using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Photon.Bolt;

public class Entity : EntityBehaviour<IEntityState>
{
    public Image HPFill;
    public float MAX_HP;

    public override void Attached()
    {
        state.AddCallback("HP", OnHPChanged);

        if (BoltNetwork.IsServer)
        {
            state.HP = MAX_HP;
            state.MaxHP = MAX_HP;
            state.IsDead = false;
        }
    }

    void OnHPChanged()
    {
        float perc = state.HP / MAX_HP;
        HPFill.fillAmount = perc;
        Debug.LogWarning(gameObject.name + " HP set to " + perc);
    }

    public void UpdateHP(float delta_amount)
    {
        if (state.HP > 0 && state.HP <= MAX_HP)
            state.HP += delta_amount;

        if (state.HP <= 0)
            state.IsDead = true;
    }
}
