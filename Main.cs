using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;
using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(AllSeeking.Main), "All Seeking Projectiles", "2.0.0", "Myself")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AllSeeking;

public class Main : BloonsTD6Mod
{
    public static readonly ModSettingBool SeekEnabled = new(true)
    {
        description = "Master toggle for all seeking projectiles.",
        button = true,
        enabledText = "ENABLED",
        disabledText = "DISABLED",
    };

    public static readonly ModSettingHotkey ToggleKey = new(KeyCode.F9)
    {
        description = "Toggle all seeking projectiles on or off.",
    };

    public static readonly ModSettingDouble SeekAggressiveness = new(270)
    {
        description = "How fast projectiles turns toward targets.",
        slider = true,
        min = 1,
        max = 720,
    };

    public static readonly ModSettingBool InfiniteRange = new(false)
    {
        description = "Projectiles travel indefinitely instead of expiring.",
    };

    public static readonly ModSettingBool BypassWalls = new(false)
    {
        description = "Projectiles pass through walls and obstacles.",
    };

    public static readonly ModSettingInt PierceBonus = new(0)
    {
        displayName = "Additional Pierce",
        description = "Extra pierce added to all seeking projectiles.",
        slider = true,
        min = 0,
        max = 100,
    };

    static bool _seeking;

    static void ApplyToProjectile(ProjectileModel proj)
    {
        if (proj.HasBehavior<TrackTargetModel>()) return;
        if (proj.HasBehavior<ArriveAtTargetModel>()) return;
        if (!proj.HasBehavior<TravelStraitModel>()) return;

        proj.AddBehavior(new TrackTargetModel("TrackTargetModel_",
            999999f, true, true, 360f, true,
            (float)(double)SeekAggressiveness,
            true, false, false));

        if (InfiniteRange)
        {
            proj.GetBehavior<TravelStraitModel>().Lifespan = 9999999f;
            if (proj.HasBehavior<AgeModel>(out var age))
                age.Lifespan = 9999999f;
        }

        if (BypassWalls)
        {
            proj.ignoreBlockers = true;
            proj.canCollisionBeBlockedByMapLos = false;
        }

        if ((long)PierceBonus > 0)
            proj.pierce += (long)PierceBonus;
    }

    static void ApplySeeking(TowerModel tm)
    {
        if (BypassWalls)
            foreach (var attack in tm.GetDescendants<AttackModel>().ToList())
                attack.attackThroughWalls = true;

        foreach (var proj in tm.GetDescendants<ProjectileModel>().ToList())
            ApplyToProjectile(proj);
    }

    static void ReapplyToAllTowers()
    {
        if (InGame.instance == null) return;

        var gm = InGame.instance.GetGameModel();
        if (gm == null) return;

        _seeking = true;
        foreach (var tower in InGame.instance.GetTowers())
        {
            var baseModel = gm.GetTowerFromId(tower.towerModel.name);
            if (baseModel == null) continue;

            var modified = baseModel.Duplicate();
            if (SeekEnabled)
                ApplySeeking(modified);

            tower.UpdateRootModel(modified);
        }
        _seeking = false;
    }

    public override void OnTowerModelChanged(Tower tower, Model newModel)
    {
        if (!SeekEnabled || _seeking) return;

        _seeking = true;
        var modified = tower.towerModel.Duplicate();
        ApplySeeking(modified);
        tower.UpdateRootModel(modified);
        _seeking = false;
    }

    public override void OnSaveSettings(JObject settings)
    {
        ReapplyToAllTowers();
    }

    public override void OnUpdate()
    {
        if (!ToggleKey.JustPressed()) return;
        SeekEnabled.SetValueAndSave(!SeekEnabled);
        MelonLogger.Msg($"All Seeking {(SeekEnabled ? "ENABLED" : "DISABLED")}");
    }
}
