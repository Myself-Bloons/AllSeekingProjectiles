using System.Linq;

using MelonLoader;
using HarmonyLib;

using BTD_Mod_Helper;
using BTD_Mod_Helper.Api.ModOptions;
using BTD_Mod_Helper.Extensions;

using UnityEngine;

using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Towers;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Attack;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities;
using Il2CppAssets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Il2CppAssets.Scripts.Models.Towers.Projectiles;
using Il2CppAssets.Scripts.Models.Towers.Projectiles.Behaviors;

using Il2CppAssets.Scripts.Simulation.Towers;
using Il2CppAssets.Scripts.Simulation.Objects;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;

[assembly: MelonInfo(typeof(AllSeeking.Main), "All Seeking Projectiles", "1.0.0", "Myself")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace AllSeeking
{
    public class Main : BloonsTD6Mod
    {
        public static readonly ModSettingBool SeekEnabled = new(true)
        {
            displayName = "All Seeking Enabled",
            button = true,
            enabledText = "ENABLED",
            disabledText = "DISABLED"
        };

        public static readonly ModSettingHotkey ToggleKey = new(KeyCode.F9)
        {
            displayName = "Toggle All Seeking ON/OFF",
            description = "Enable/disable adding seeking to straight-travel projectiles"
        };

        private static TrackTargetWithinTimeModel? _seekTemplate;

        private static readonly string[] _towerNameExclusions =
        {
            "icemonkey", "sniper", "mortar", "spike"
        };

        private static readonly string[] _attackNameExclusions =
        {
            "TargetTrack", "TargetFriendly", "BrewTargetting", "RandomPosition"
        };

        private static void EnsureSeekTemplate(GameModel? gm)
        {
            if (_seekTemplate != null || gm == null) return;
            try
            {
                var ninja = gm.GetTowerFromId("NinjaMonkey-001");
                var attack = ninja.GetBehavior<AttackModel>();
                var proj = attack.weapons[0].projectile;
                _seekTemplate = proj.GetBehavior<TrackTargetWithinTimeModel>().Duplicate();
            }
            catch
            {
                MelonLogger.Warning("[AllSeeking] Could not acquire Ninja seek template.");
            }
        }

        private static void MakeAttackSeeking(AttackModel attack)
        {
            if (attack == null) return;

            if (_attackNameExclusions.Any(excl => attack.name.Contains(excl)))
                return;

            foreach (var proj in attack.GetDescendants<ProjectileModel>().ToIl2CppList())
            {
                if (proj.HasBehavior<ArriveAtTargetModel>() ||
                    proj.HasBehavior<TrackTargetWithinTimeModel>())
                    continue;

                if (proj.HasBehavior<TravelStraitModel>() && _seekTemplate != null)
                    proj.AddBehavior(_seekTemplate.Duplicate());
            }
        }

        private static void MakeTowerSeeking(TowerModel tm)
        {
            if (tm == null) return;

            var lower = tm.name.ToLowerInvariant();
            if (_towerNameExclusions.Any(excl => lower.Contains(excl)))
                return;

            foreach (var attack in tm.GetAttackModels())
                MakeAttackSeeking(attack);

            foreach (var ability in tm.GetAbilities())
            {
                foreach (var activateAttack in ability.GetBehaviors<ActivateAttackModel>())
                {
                    foreach (var attack in activateAttack.attacks)
                        MakeAttackSeeking(attack);
                }
            }
        }
        public override void OnTowerCreated(Tower tower, Entity target, Model modelToUse)
        {
            if (!SeekEnabled) return;

            var gm = InGame.instance?.GetGameModel() ?? Game.instance?.model;
            EnsureSeekTemplate(gm);

            var modified = tower.towerModel.Duplicate();
            MakeTowerSeeking(modified);
            tower.UpdateRootModel(modified);
        }

        public override void OnTowerUpgraded(Tower tower, string upgradeName, TowerModel newBaseTowerModel)
        {
            if (!SeekEnabled) return;

            var gm = InGame.instance?.GetGameModel() ?? Game.instance?.model;
            EnsureSeekTemplate(gm);

            var modified = tower.towerModel.Duplicate();
            MakeTowerSeeking(modified);
            tower.UpdateRootModel(modified);
        }

        public override void OnGameModelLoaded(GameModel model)
        {
            MelonLogger.Msg("[AllSeeking] Game model loaded (v50.2).");
        }


        public override void OnUpdate()
        {
            if (ToggleKey.JustPressed())
            {
                SeekEnabled.SetValueAndSave(!SeekEnabled);
                MelonLogger.Msg($"[AllSeeking] {(SeekEnabled ? "ENABLED" : "DISABLED")}");
            }
        }
    }
}
